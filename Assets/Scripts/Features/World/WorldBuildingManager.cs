// 职责：拥有 WorldLab 建筑实例、初始布局、地形支撑通知和有界残骸。
// 入口：Place() / Capture() / Restore() / ResetBuildings()；不调用玩家 SaveManager。
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WorldBuildingManager : MonoBehaviour
{
    [SerializeField]
    private ProceduralVoxelWorld world;
    [SerializeField]
    private WorldBuildingCatalog catalog;
    [SerializeField, Tooltip("WorldLab启动时恢复初始布局；流式世界由会话管理器控制。")]
    private bool initializeOnStart = true;
    [SerializeField, Range(0f, 1f)]
    private float minimumSupportRatio = 0.5f;
    [SerializeField]
    private bool collapseUnsupported = true;
    [SerializeField, Range(1, 64)]
    private int maximumBuildings = 64;
    [SerializeField, Range(0, 24)]
    private int maximumDebrisPieces = 12;
    [SerializeField, Range(1, 64)]
    private int maximumBlocksPerDebris = 32;
    [SerializeField, Min(1f)]
    private float debrisLifetime = 12f;
    private readonly List<BuildingRuntime> buildings = new List<BuildingRuntime>();
    private readonly List<GameObject> debris = new List<GameObject>();
    private readonly HashSet<BuildingRuntime> pendingSupport = new HashSet<BuildingRuntime>();
    private readonly WorldBuildingDamage damage = new WorldBuildingDamage();
    public ProceduralVoxelWorld World => world;
    public WorldBuildingCatalog Catalog => catalog;
    public IReadOnlyList<BuildingRuntime> Buildings => buildings;
    public string LastMessage { get; private set; } = "Select a blueprint to place a building.";

    private void OnEnable()
    {
        foreach (BuildingRuntime building in buildings)
        {
            if (building != null && building.NeedsActivation)
            {
                building.Activate();
            }
        }

        if (world != null)
        {
            world.TerrainChanged += TerrainChanged;
            world.Regenerated += ResetBuildings;
        }
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            ResetBuildings();
        }
    }

    private void OnDisable()
    {
        if (world != null)
        {
            world.TerrainChanged -= TerrainChanged;
            world.Regenerated -= ResetBuildings;
        }

        ClearDebris();
    }

    public bool CanPlace(BlockDataList blueprint, Vector3 position, Quaternion rotation, out string reason)
    {
        int count = blueprint?.blocks?.Count ?? 0;
        foreach (BuildingRuntime building in buildings)
        {
            if (building != null)
            {
                count += building.Blocks.Count;
            }
        }

        if (buildings.Count >= maximumBuildings || count > WorldBuildingValidation.MaximumWorldBlocks)
        {
            reason = "World building or block limit reached.";
            return false;
        }

        return WorldBuildingPlacement.Check(world, blueprint, position, rotation, out reason);
    }

    public BuildingRuntime Place(BlockDataList blueprint, string name, Vector3 position, Quaternion rotation, UnitFaction faction)
    {
        WorldBuildingValidation.ValidateBlueprint(blueprint);
        if (!CanPlace(blueprint, position, rotation, out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        var health = new float[blueprint.blocks.Count];
        for (int i = 0; i < health.Length; i++)
        {
            health[i] = float.MaxValue;
        }

        var state = new WorldBuildingState
        {
            instanceId = Guid.NewGuid().ToString("N"),
            blueprintName = name,
            faction = faction,
            position = position,
            rotation = rotation,
            blueprint = blueprint,
            durability = health
        };
        BuildingRuntime building = RestoreBuilding(state);
        LastMessage = "Placed " + name;
        return building;
    }

    /// <summary>恢复一个已校验快照；禁用世界中仅准备层级，启用世界后才执行激活和耐久恢复。</summary>
    public BuildingRuntime RestoreBuilding(WorldBuildingState state)
    {
        WorldBuildingValidation.ValidateState(state);
        if (!HasCapacity(state.blueprint.blocks.Count))
        {
            throw new InvalidOperationException("Active world building budget exceeded.");
        }

        BuildingRuntime building = Stage(state, transform);
        buildings.Add(building);
        if (isActiveAndEnabled)
        {
            building.Activate();
        }

        return building;
    }

    public bool HasCapacity(int additionalBlocks)
    {
        int count = additionalBlocks;
        foreach (BuildingRuntime current in buildings)
        {
            count += current != null ? current.Blocks.Count : 0;
        }

        return buildings.Count < maximumBuildings && count <= WorldBuildingValidation.MaximumWorldBlocks;
    }

    public void RemoveBuilding(BuildingRuntime building)
    {
        if (building == null || !buildings.Remove(building))
        {
            return;
        }

        pendingSupport.Remove(building);
        building.gameObject.SetActive(false);
        Destroy(building.gameObject);
    }

    public void ClearSession()
    {
        ClearBuildings();
        ClearDebris();
    }

    private BuildingRuntime Stage(WorldBuildingState state, Transform parent)
    {
        var root = new GameObject("Building " + state.blueprintName);
        root.SetActive(false);
        root.transform.SetParent(parent, false);
        try
        {
            var building = root.AddComponent<BuildingRuntime>();
            building.Initialize(this, state);
            return building;
        }
        catch
        {
            Destroy(root);
            throw;
        }
    }

    public WorldLabSnapshot Capture()
    {
        var states = new List<WorldBuildingState>();
        foreach (BuildingRuntime building in buildings)
        {
            if (building != null)
            {
                states.Add(building.Capture());
            }
        }

        return new WorldLabSnapshot
        {
            cellSize = world.CellSize,
            terrain = world.CaptureEdits(),
            buildings = states.ToArray()
        };
    }

    /// <summary>先校验完整存档并建立禁用的建筑层级；成功后才替换地形与旧实例。</summary>
    public void Restore(WorldLabSnapshot snapshot)
    {
        if (snapshot == null || snapshot.version != 2 || !Mathf.Approximately(snapshot.cellSize, world.CellSize) || snapshot.terrain == null || snapshot.buildings == null || snapshot.buildings.Length > maximumBuildings)
        {
            throw new InvalidOperationException("World snapshot version, cell size or building count is invalid.");
        }

        snapshot.terrain.Restore(world.Seed, world.Data.Size);
        var identities = new HashSet<string>();
        int blockCount = 0;
        foreach (WorldBuildingState state in snapshot.buildings)
        {
            WorldBuildingValidation.ValidateState(state);
            blockCount += state.blueprint.blocks.Count;
            if (!identities.Add(state.instanceId) || blockCount > WorldBuildingValidation.MaximumWorldBlocks)
            {
                throw new InvalidOperationException("Duplicate building identity or world block budget exceeded.");
            }
        }

        var staged = new List<BuildingRuntime>();
        try
        {
            foreach (WorldBuildingState state in snapshot.buildings)
            {
                staged.Add(Stage(state, transform));
            }
        }
        catch
        {
            foreach (BuildingRuntime building in staged)
            {
                Destroy(building.gameObject);
            }

            throw;
        }

        world.RestoreEdits(snapshot.terrain);
        ClearBuildings();
        ClearDebris();
        foreach (BuildingRuntime building in staged)
        {
            buildings.Add(building);
            building.Activate();
            pendingSupport.Add(building);
        }

        // 不让旧地形碰撞影响恢复后的动态建筑第一物理步。
        world.FlushDirty();
        LastMessage = "Restored terrain and " + buildings.Count + " buildings.";
    }

    public void ResetBuildings()
    {
        ClearBuildings();
        ClearDebris();
        if (catalog == null)
        {
            return;
        }

        foreach (WorldBuildingCatalog.InitialBuilding initial in catalog.initialBuildings)
        {
            try
            {
                if (initial == null || initial.blueprint == null)
                {
                    LastMessage = "Initial building skipped: missing blueprint.";
                    continue;
                }

                BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(initial.blueprint.text);
                Place(
                    blueprint,
                    initial.blueprint.name,
                    world.transform.TransformPoint(initial.worldLocalPosition),
                    world.transform.rotation * Quaternion.Euler(0f, initial.quarterTurns * 90f, 0f),
                    initial.faction);
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                LastMessage = "Initial building skipped: " + error.Message;
            }
        }
    }

    private void ClearBuildings()
    {
        pendingSupport.Clear();
        foreach (BuildingRuntime building in buildings)
        {
            if (building != null)
            {
                building.gameObject.SetActive(false);
                Destroy(building.gameObject);
            }
        }

        buildings.Clear();
    }

    private void TerrainChanged(Bounds changed)
    {
        foreach (BuildingRuntime building in buildings)
        {
            if (building != null && building.Anchored && WorldBuildingPlacement.Footprint(building).Intersects(changed))
            {
                pendingSupport.Add(building);
            }
        }
    }

    private void FixedUpdate()
    {
        foreach (BuildingRuntime building in pendingSupport)
        {
            if (building != null && building.Anchored)
            {
                building.SupportRatio = WorldBuildingPlacement.Support(world, building.FootprintSize, building.transform);
                if (collapseUnsupported && building.SupportRatio < minimumSupportRatio)
                {
                    building.ReleaseAnchor();
                }
            }
        }

        pendingSupport.Clear();
    }

    public void DamageSphere(Vector3 center, float radius, float damage)
    {
        this.damage.Apply(buildings, center, radius, damage);
    }

    internal void CreateDebris(List<Block> group, Rigidbody source)
    {
        debris.RemoveAll(piece => piece == null);
        if (debris.Count >= maximumDebrisPieces || group.Count > maximumBlocksPerDebris)
        {
            foreach (Block block in group)
            {
                block.gameObject.SetActive(false);
                Destroy(block.gameObject);
            }

            return;
        }

        var piece = new GameObject("Building debris");
        piece.transform.SetParent(transform, false);
        piece.transform.position = group[0].transform.position;
        float mass = 0f;
        foreach (Block block in group)
        {
            block.transform.SetParent(piece.transform, true);
            mass += block.mass;
            foreach (MonoBehaviour behaviour in block.GetComponentsInChildren<MonoBehaviour>())
            {
                behaviour.enabled = false;
            }
        }

        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.mass = Mathf.Max(0.1f, mass);
        body.linearVelocity = source.isKinematic ? Vector3.zero : source.GetPointVelocity(body.worldCenterOfMass);
        body.angularVelocity = source.isKinematic ? Vector3.zero : source.angularVelocity;
        debris.Add(piece);
        Destroy(piece, debrisLifetime);
    }

    private void ClearDebris()
    {
        foreach (GameObject piece in debris)
        {
            if (piece != null)
            {
                piece.SetActive(false);
                Destroy(piece);
            }
        }

        debris.Clear();
    }
}
