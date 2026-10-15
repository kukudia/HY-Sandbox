// 职责：会话内管理据点有限驻军、击败记录及远距离休眠；退出探索即丢弃，不写永久存档。
// 入口：Tick() / Clear()；击败只由驾驶舱显式死亡通知，卸载不计击败。
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class OutpostEncounterDirector
{
    private readonly Dictionary<string, Encounter> encounters = new Dictionary<string, Encounter>();
    private readonly WorldBuildingManager buildings;
    private readonly ProceduralVoxelWorld world;
    private int activeCount;
    public string Status { get; private set; } = "";
    public int ClearedCount
    {
        get; private set;
    }
    public int ActiveCount => activeCount;

    public OutpostEncounterDirector(WorldBuildingManager owner)
    {
        buildings = owner;
        world = owner.World;
    }

    public bool IsDefeated(string site, string guard)
    {
        return encounters.TryGetValue(site, out Encounter encounter) && encounter.Guards.Exists(item => item.Data.id == guard && item.Defeated);
    }

    public bool TryGetDefender(string site, string guardId, out ControlUnit unit)
    {
        unit = null;
        if (encounters.TryGetValue(site, out Encounter encounter))
        {
            Guard guard = encounter.Guards.Find(item => item.Data.id == guardId);
            if (guard != null)
            {
                RefreshOwner(guard);
            }

            unit = guard != null ? guard.Unit : null;
        }

        return unit != null;
    }

    public void Tick(Vector3 player)
    {
        foreach (BuildingRuntime building in buildings.Buildings)
        {
            if (building == null || building.Outpost == null || !building.IsOperational)
            {
                continue;
            }

            if (encounters.TryGetValue(building.InstanceId, out Encounter existing))
            {
                existing.Building = building;
                continue;
            }

            var encounter = new Encounter
            {
                Id = building.InstanceId,
                Name = building.BlueprintName,
                Position = building.transform.position,
                Rotation = building.transform.rotation,
                Data = building.Outpost,
                Building = building
            };
            foreach (OutpostGarrisonData guard in encounter.Data.garrison)
            {
                encounter.Guards.Add(new Guard { Data = guard });
            }

            encounters.Add(encounter.Id, encounter);
        }

        activeCount = 0;
        foreach (Encounter encounter in encounters.Values)
        {
            foreach (Guard guard in encounter.Guards)
            {
                RefreshOwner(guard);
                if (guard.Unit != null)
                {
                    activeCount++;
                }
            }
        }

        float nearest = 225f * 225f;
        Status = "";
        ClearedCount = 0;
        bool spawned = false;
        foreach (Encounter encounter in encounters.Values)
        {
            if (encounter.Building != null)
            {
                encounter.Emplacements = 0;
                foreach (Block block in encounter.Building.Blocks)
                {
                    if (block != null && block.GetComponent<Turret>() != null)
                    {
                        encounter.Emplacements++;
                    }
                }
            }

            int remaining = 0;
            foreach (Guard guard in encounter.Guards)
            {
                if (!guard.Defeated)
                {
                    remaining++;
                }

                if (guard.Unit != null && ((guard.Unit.transform.position - player).sqrMagnitude > 180f * 180f || !world.HasColumn(world.ColumnAt(guard.Unit.transform.position))))
                {
                    Hibernate(guard);
                    activeCount--;
                }

                Vector3 position = guard.Snapshot != null ? guard.Position : encounter.Position + encounter.Rotation * guard.Data.position;
                // A pursuing guard can leave its original site. Resume near its last position,
                // with a gap below the 180m hibernation threshold to avoid boundary churn.
                bool deploymentNear = (position - player).sqrMagnitude <= 160f * 160f;
                bool near = deploymentNear && (guard.Snapshot != null || (encounter.Position - player).sqrMagnitude <= encounter.Data.activationDistance * encounter.Data.activationDistance);
                if (!spawned && activeCount < 16 && !guard.Defeated && guard.Unit == null && near && world.HasColumn(world.ColumnAt(position)))
                {
                    spawned = Spawn(encounter, guard, position);
                    activeCount += guard.Unit != null ? 1 : 0;
                }
            }

            bool cleared = remaining == 0 && encounter.Emplacements == 0;
            if (cleared)
            {
                ClearedCount++;
            }

            float distance = (encounter.Position - player).sqrMagnitude;
            if (distance < nearest)
            {
                nearest = distance;
                Status = cleared ? encounter.Name + " | CLEARED" : encounter.Name + " | Defenders " + remaining + " / " + encounter.Guards.Count + " | Emplacements " + encounter.Emplacements;
            }
        }
    }

    private bool Spawn(Encounter encounter, Guard guard, Vector3 position)
    {
        BlockDataList blueprint = guard.Snapshot ?? guard.Data.CreateBlueprint();
        Quaternion rotation = guard.Snapshot != null ? guard.Rotation : encounter.Rotation * Quaternion.Euler(0f, guard.Data.yaw, 0f);
        Bounds deployment = DeploymentBounds(blueprint, position, rotation);
        Vector2Int minimum = world.ColumnAt(deployment.min);
        Vector2Int maximum = world.ColumnAt(deployment.max);
        for (int z = minimum.y; z <= maximum.y; z++)
        {
            for (int x = minimum.x; x <= maximum.x; x++)
            {
                if (!world.HasColumn(new Vector2Int(x, z)))
                {
                    return false;
                }
            }
        }

        guard.Unit = EnemyBlueprintFactory.Spawn(blueprint, guard.Data.name, position, rotation);
        if (guard.Unit == null)
        {
            return false;
        }

        Bounds bounds = new Bounds(position, Vector3.zero);
        foreach (Collider collider in guard.Unit.GetComponentsInChildren<Collider>())
        {
            if (!collider.isTrigger)
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        Vector3Int minCell = world.WorldToCell(bounds.min);
        Vector3Int maxCell = world.WorldToCell(bounds.max);
        float highest = float.MinValue;
        for (int z = minCell.z; z <= maxCell.z; z++)
        {
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                highest = Mathf.Max(
                    highest,
                    world.transform.TransformPoint(new Vector3(x * world.CellSize, world.SurfaceHeight(x, z), z * world.CellSize)).y);
            }
        }

        float rise = highest + 2f - bounds.min.y;
        if (rise > 0f)
        {
            guard.Unit.transform.position += Vector3.up * rise;
        }

        if (guard.Health != null)
        {
            foreach (Block block in guard.Unit.GetComponentsInChildren<Block>())
            {
                if (guard.Health.TryGetValue(block.uniqueId, out float health) && block.TryGetComponent(out Durability durability))
                {
                    durability.currentDurability = Mathf.Min(health, durability.maxDurability);
                }
            }
        }

        EnemyIdentity identity = guard.Unit.cockpit.GetComponent<EnemyIdentity>();
        guard.Identity = identity;
        identity.Defeated += () => guard.Defeated = true;
        return true;
    }

    private static Bounds DeploymentBounds(BlockDataList blueprint, Vector3 position, Quaternion rotation)
    {
        var bounds = new Bounds(position, Vector3.zero);
        foreach (BlockData block in blueprint.blocks)
        {
            Vector3 center = new Vector3(block.posX, block.posY, block.posZ);
            Quaternion local = new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? -block.x * 0.5f : block.x * 0.5f,
                    (i & 2) == 0 ? -block.y * 0.5f : block.y * 0.5f,
                    (i & 4) == 0 ? -block.z * 0.5f : block.z * 0.5f);
                bounds.Encapsulate(position + rotation * (center + local * corner));
            }
        }

        return bounds;
    }

    private static void Hibernate(Guard guard)
    {
        // Preserve surviving modules and health, including damage and cargo, without death/destruction effects.
        guard.Snapshot = new BlockDataList();
        guard.Health = new Dictionary<string, float>();
        guard.Position = guard.Unit.transform.position;
        guard.Rotation = guard.Unit.transform.rotation;
        foreach (Block block in guard.Unit.GetComponentsInChildren<Block>())
        {
            var data = new BlockData(block);
            Vector3 local = block.transform.localPosition;
            Quaternion rotation = block.transform.localRotation;
            data.posX = local.x;
            data.posY = local.y;
            data.posZ = local.z;
            data.rotX = rotation.x;
            data.rotY = rotation.y;
            data.rotZ = rotation.z;
            data.rotW = rotation.w;
            guard.Snapshot.blocks.Add(data);
            if (block.TryGetComponent(out Durability durability))
            {
                guard.Health[data.id] = durability.currentDurability;
            }
        }

        foreach (Bot bot in guard.Unit.GetComponentsInChildren<Bot>(true))
        {
            bot.PrepareForHomeDestruction();
        }

        guard.Unit.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(guard.Unit.gameObject);
        guard.Unit = null;
        guard.Identity = null;
    }

    private static void RefreshOwner(Guard guard)
    {
        // Connectivity rebuilding replaces the rigidbody root; the surviving cockpit owns identity.
        if (!guard.Defeated && guard.Identity != null)
        {
            guard.Unit = guard.Identity.GetComponentInParent<ControlUnit>();
        }
    }

    public void Clear()
    {
        foreach (Encounter encounter in encounters.Values)
        {
            foreach (Guard guard in encounter.Guards)
            {
                RefreshOwner(guard);
                if (guard.Unit != null)
                {
                    foreach (Bot bot in guard.Unit.GetComponentsInChildren<Bot>(true))
                    {
                        bot.PrepareForHomeDestruction();
                    }

                    guard.Unit.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(guard.Unit.gameObject);
                }
            }
        }

        encounters.Clear();
        activeCount = ClearedCount = 0;
        Status = "";
    }

    private sealed class Encounter
    {
        internal string Id;
        internal string Name;
        internal Vector3 Position;
        internal Quaternion Rotation;
        internal EnemyOutpostData Data;
        internal BuildingRuntime Building;
        internal int Emplacements;
        internal readonly List<Guard> Guards = new List<Guard>();
    }

    private sealed class Guard
    {
        internal OutpostGarrisonData Data;
        internal ControlUnit Unit;
        internal EnemyIdentity Identity;
        internal bool Defeated;
        internal BlockDataList Snapshot;
        internal Dictionary<string, float> Health;
        internal Vector3 Position;
        internal Quaternion Rotation;
    }
}
