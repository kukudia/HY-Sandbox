// 职责：在真实Play验证模块免伤、体素破坏、冲量隔离，以及舰船与共享地基的概率断链。
// 入口：Run()；只创建自有夹具，不写玩家存档，报告Temp/Combat/explosion-integration.json。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ExplosionIntegrationChecks
{
    [Serializable]
    private sealed class Report
    {
        public List<string> checks = new List<string>();
        public string error;
    }

    public static string Run()
    {
        if (!Application.isPlaying || PlayManager.instance == null || DestroyManager.instance == null)
        {
            throw new InvalidOperationException("Run in Main Play Mode.");
        }

        var report = new Report();
        Scene scene = SceneManager.CreateScene("Explosion integration owned", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        Scene other = SceneManager.CreateScene("Explosion isolation owned", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        bool previous = PlayManager.instance.playMode;
        try
        {
            PlayManager.instance.playMode = true;
            PhysicsSandboxProbe.PrepareSceneEffects(scene);
            WorldBuildingManager manager = Manager(scene);
            Vector3 center = new Vector3(7000f, 1000f, 7000f);
            Block attacker = Ship(scene, center + Vector3.right * 50f);
            ControlUnit attackerUnit = attacker.GetComponentInParent<ControlUnit>();
            attackerUnit.faction = UnitFaction.Player;
            Block source = Ship(scene, center);
            Block shipTarget = Ship(scene, center + Vector3.right * 2f);
            BuildingRuntime building = Building(manager, center + Vector3.forward * 2f);
            Block isolated = Ship(other, center + Vector3.right * 2f);
            Durability health = shipTarget.GetComponent<Durability>();
            health.ProtectFromExplosionCollisions(3f);
            shipTarget.gameObject.AddComponent<BoxCollider>();
            Configure(source, 120f, 6f);
            Physics.SyncTransforms();
            source.GetComponent<Durability>().ApplyDamage(200f, attackerUnit);
            Check(
                report,
                "ship blast preserves nearby ship despite multiple colliders",
                Mathf.Approximately(health.currentDurability, 100f));
            Check(report, "blast does not damage modules", health.currentDurability == 100f);
            Check(
                report,
                "ship blast preserves anchored building",
                Mathf.Approximately(building.Blocks[0].GetComponent<Durability>().currentDurability, 100f));
            Check(
                report,
                "blast does not manufacture collateral attribution",
                health.LastAttacker == null && building.Blocks[0].GetComponent<Durability>().LastAttacker == null);
            Check(
                report,
                "another PhysicsScene at identical coordinates receives no damage",
                Mathf.Approximately(isolated.GetComponent<Durability>().currentDurability, 100f));
            scene.GetPhysicsScene().Simulate(.02f);
            other.GetPhysicsScene().Simulate(.02f);
            Check(
                report,
                "another PhysicsScene receives no impulse",
                isolated.GetComponentInParent<Rigidbody>().linearVelocity.sqrMagnitude < .000001f);
            Check(
                report,
                "cross-unit impulse follows the source PhysicsScene",
                shipTarget.GetComponentInParent<Rigidbody>().linearVelocity.magnitude > .001f);
            float once = health.currentDurability;
            DestroyManager.instance.ExplodeBlock(source);
            Check(
                report,
                "repeated source request cannot detonate twice",
                Mathf.Approximately(health.currentDurability, once));
            center += Vector3.right * 100f;
            building = Building(manager, center);
            source = building.Blocks[0];
            shipTarget = Ship(scene, center + Vector3.right * 2f);
            Configure(source, 120f, 6f);
            source.GetComponent<Durability>().ApplyDamage(200f, attackerUnit);
            Check(
                report,
                "building blast preserves a nearby ship",
                Mathf.Approximately(shipTarget.GetComponent<Durability>().currentDurability, 100f));
            Check(report, "building death removes its source before damage scanning", building.Blocks.Count == 0);
            center += Vector3.right * 100f;
            source = Ship(scene, center);
            Block chain = Ship(scene, center + Vector3.right * 2f);
            shipTarget = Ship(scene, center + Vector3.right * 5f);
            Configure(source, 120f, 4f);
            Configure(chain, 100f, 4f);
            chain.GetComponent<Durability>().currentDurability = 50f;
            source.GetComponent<Durability>().ApplyDamage(200f, attackerUnit);
            Check(
                report,
                "nearby explosive source does not chain detonate",
                chain.GetComponent<Durability>().currentDurability == 50f);
            Check(
                report,
                "ship outside the blast remains undamaged",
                shipTarget.GetComponent<Durability>().currentDurability == 100f);
            Check(
                report,
                "no secondary blast attribution is manufactured",
                shipTarget.GetComponent<Durability>().LastAttacker == null);
            TerrainCheck(scene, report);
            foreach (float probability in new[]
            {
                0f,
                1f
            }

            )
            {
                Vector3 origin = center + Vector3.forward * (100f + 20f * probability);
                ShipConnections(scene, origin, probability, report);
                FoundationConnections(manager, origin + Vector3.right * 40f, probability, report);
            }

            return Save(report);
        }
        catch (Exception error)
        {
            report.error = error.ToString();
            Save(report);
            throw;
        }
        finally
        {
            PlayManager.instance.playMode = previous;
            foreach (Scene owned in new[]
            {
                scene,
                other
            }

            )
            {
                foreach (GameObject root in owned.GetRootGameObjects())
                {
                    root.SetActive(false);
                    Object.DestroyImmediate(root);
                }

                SceneManager.UnloadSceneAsync(owned);
            }
        }
    }

    private static void TerrainCheck(Scene scene, Report report)
    {
        var root = new GameObject("Explosion voxel fixture");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = new Vector3(7500f, 1000f, 7000f);
        ProceduralVoxelWorld world = root.AddComponent<ProceduralVoxelWorld>();
        var data = new SerializedObject(world);
        data.FindProperty("dimensions").vector3IntValue = new Vector3Int(16, 16, 16);
        data.FindProperty("cellSize").floatValue = 1f;
        data.FindProperty("chunkSize").intValue = 16;
        data.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
        Vector3Int cell = new Vector3Int(8, 8, 8);
        world.SetCell(cell, 2);
        Block source = Ship(scene, world.CellCenter(cell));
        Configure(source, 150f, 3f);
        source.GetComponent<Durability>().ApplyDamage(200f, null);
        Check(
            report,
            "ship blast damages voxel terrain using the shared blast",
            world.Data.Get(cell) == 0 && world.RemovedCells > 0);
    }

    private static void ShipConnections(Scene scene, Vector3 origin, float probability, Report report)
    {
        Block source = Ship(scene, origin);
        Transform owner = source.transform.parent;
        Block near = ShipPart(owner, Vector3.right);
        Block far = ShipPart(owner, Vector3.right * 2f);
        Block tail = ShipPart(owner, Vector3.right * 3f);
        Block foreign = Ship(scene, origin + Vector3.forward);
        Block foreignTail = ShipPart(foreign.transform.parent, Vector3.forward);
        foreach (Block block in new[]
        {
            source,
            near,
            far,
            tail,
            foreign,
            foreignTail
        }

        )
        {
            block.CheckConnection();
        }

        Check(
            report,
            $"ship fixture starts connected at p={probability}",
            near.Neighbors().Contains(far) && far.Neighbors().Contains(tail) && foreign.Neighbors().Contains(foreignTail));
        Configure(source, 0f, 1.1f);
        source.explosionDisconnectProbability = probability;
        source.GetComponent<Durability>().ApplyDamage(200f, null);
        Check(
            report,
            $"in-range block separates only at p=1 (p={probability})",
            (near.GetComponentInParent<Rigidbody>() != far.GetComponentInParent<Rigidbody>()) == (probability == 1f));
        Check(
            report,
            $"outside-radius connection survives at p={probability}",
            far.GetComponentInParent<Rigidbody>() == tail.GetComponentInParent<Rigidbody>() && far.Neighbors().Contains(tail));
        Check(
            report,
            $"another unit inside the radius keeps its connection at p={probability}",
            foreign.Neighbors().Contains(foreignTail) && foreign.GetComponentInParent<Rigidbody>() == foreignTail.GetComponentInParent<Rigidbody>());
    }

    private static void FoundationConnections(WorldBuildingManager manager, Vector3 origin, float probability, Report report)
    {
        Vector3 foot = Vector3.one * .5f;
        BuildingRuntime building = Building(
            manager,
            origin,
            foot,
            foot + Vector3.up,
            foot + 2f * Vector3.up,
            foot - 2f * Vector3.right);
        Block floor = building.Blocks[0], middle = building.Blocks[1], source = building.Blocks[2], secondWall = building.Blocks[3];
        Check(
            report,
            $"two independent columns have full foundation support at p={probability}",
            building.HasFullFoundationSupport());
        int losses = 0;
        building.BlockDestroyed += lost =>
        {
            if (lost == source)
            {
                losses++;
            }
        };
        Configure(source, 0f, 1.1f);
        source.explosionDisconnectProbability = probability;
        source.GetComponent<Durability>().ApplyDamage(200f, null);
        Check(
            report,
            $"separate grounded walls retain their original base at p={probability}",
            building.Anchored && floor.GetComponentInParent<Rigidbody>() == building.GetComponent<Rigidbody>() && secondWall.GetComponentInParent<Rigidbody>() == building.GetComponent<Rigidbody>());
        Rigidbody middleBody = middle.GetComponentInParent<Rigidbody>();
        Check(
            report,
            $"base uses the same probability to release an unsupported wall at p={probability}",
            middleBody != null && (middleBody == building.GetComponent<Rigidbody>()) == (probability == 0f) && middleBody.isKinematic == (probability == 0f));
        Check(
            report,
            $"base snapshot tracks surviving supported members and one loss event at p={probability}",
            losses == 1 && building.Blocks.Contains(floor) && building.Blocks.Contains(secondWall) && building.Blocks.Contains(middle) == (probability == 0f) && !building.Blocks.Contains(source) && building.Capture().blueprint.blocks.Count == (probability == 0f ? 3 : 2));
    }

    private static WorldBuildingManager Manager(Scene scene)
    {
        var root = new GameObject("Explosion building owner");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
        WorldBuildingManager manager = root.AddComponent<WorldBuildingManager>();
        var data = new SerializedObject(manager);
        data.FindProperty("initializeOnStart").boolValue = false;
        data.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
        return manager;
    }

    private static BuildingRuntime Building(WorldBuildingManager manager, Vector3 center, params Vector3[] parts)
    {
        if (parts.Length == 0)
        {
            parts = new[]
            {
                Vector3.one * .5f
            };
        }

        var blueprint = new BlockDataList
        {
            foundation = new BuildingFoundationData
            {
                width = 4,
                length = 4
            },
            blocks = parts.Select(position => new BlockData(Resources.Load<GameObject>("Blocks/1x1x1").GetComponent<Block>()) { id = Guid.NewGuid().ToString("N"), resourcePath = "Blocks/1x1x1", x = 1, y = 1, z = 1, posX = position.x, posY = position.y, posZ = position.z, rotW = 1f }).ToList()
        };
        BuildingRuntime building = manager.RestoreBuilding(new WorldBuildingState { instanceId = Guid.NewGuid().ToString("N"), blueprintName = "Explosion fixture", blueprint = blueprint, faction = UnitFaction.Enemy, anchored = true, position = center - Vector3.one * .5f, rotation = Quaternion.identity, durability = Enumerable.Repeat(100f, parts.Length).ToArray() });
        foreach (Block block in building.Blocks)
        {
            block.canExplode = false;
            block.GetComponent<Durability>().maxDurability = block.GetComponent<Durability>().currentDurability = 100f;
        }

        return building;
    }

    private static Block Ship(Scene scene, Vector3 position)
    {
        GameObject root = Object.Instantiate(Resources.Load<GameObject>("BlocksParent/BlocksParent"), position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(root, scene);
        ControlUnit unit = root.GetComponent<ControlUnit>();
        unit.enabled = false;
        unit.faction = UnitFaction.Enemy;
        Rigidbody body = root.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = false;
        body.linearDamping = 0f;
        body.mass = 10f;
        Block block = ShipPart(root.transform, Vector3.zero);
        unit.RefreshChildren();
        unit.EnsureRuntimeUnitId();
        unit.AssignRuntimeOwnershipToBlocks(true);
        return block;
    }

    private static Block ShipPart(Transform owner, Vector3 position)
    {
        GameObject part = Object.Instantiate(Resources.Load<GameObject>("Blocks/1x1x1"), owner);
        part.transform.localPosition = position;
        Block block = part.GetComponent<Block>();
        block.canExplode = false;
        Durability health = part.GetComponent<Durability>();
        health.maxDurability = health.currentDurability = 100f;
        return block;
    }

    private static void Configure(Block block, float damage, float radius)
    {
        block.canExplode = true;
        block.explosionDamage = damage;
        block.explosionRadius = radius;
        block.explosionForce = 500f;
        block.explosionDisconnectProbability = 0f;
    }

    private static void Check(Report report, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add("PASS " + name);
    }

    private static string Save(Report report)
    {
        string text = JsonUtility.ToJson(report, true);
        Directory.CreateDirectory("Temp/Combat");
        File.WriteAllText("Temp/Combat/explosion-integration.json", text);
        return text;
    }
}
