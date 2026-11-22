// 职责：管理蓝图结构的实例、地基失效通知与残骸生命周期。
// 入口：Place() / RestoreBuilding() / RestoreBuildingRoutine()。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BlueprintStructureRegistry : MonoBehaviour
{
    [SerializeField]
    private ProceduralVoxelWorld world;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("initializeOnStart")]
    private bool useIndividualRenderers;
    [SerializeField, Range(0f, 1f)]
    private float minimumSupportRatio = 0.5f;
    [SerializeField]
    private bool collapseUnsupported = true;
    [SerializeField, Range(1, 64)]
    private int maximumBuildings = 64;
    [SerializeField, Range(0, 24)]
    private int maximumDebrisPieces = 12;
    [SerializeField, Range(1, 64), Tooltip("碎片超过此模块数时提前安排回收，仍先生成物理实体并等待共用最短保留期。")]
    private int maximumBlocksPerDebris = 32;
    [SerializeField, Min(1f)]
    private float debrisLifetime = 12f;
    private readonly List<BlueprintStructureRuntime> buildings = new List<BlueprintStructureRuntime>();
    private readonly List<GameObject> debris = new List<GameObject>();
    private readonly HashSet<BlueprintStructureRuntime> pendingSupport = new HashSet<BlueprintStructureRuntime>();
    private readonly ExplosionDamageSession damage = new ExplosionDamageSession();
    private readonly BlueprintStructureRenderCache renderCache = new BlueprintStructureRenderCache();
    public bool SimulationEnabled { get; internal set; } = true;
    internal float LoadingFrameBudgetMilliseconds { get; set; } = 2f;
    internal ExplosionDamageSession DamageSession => damage;
    public ProceduralVoxelWorld World => world;
    public IReadOnlyList<BlueprintStructureRuntime> Buildings => buildings;
    public string LastMessage { get; private set; } = "Select a blueprint to place a building.";

    private void OnEnable()
    {
        foreach (BlueprintStructureRuntime building in buildings)
        {
            if (building != null && building.NeedsActivation)
            {
                building.Activate();
            }
        }

        if (world != null)
        {
            world.SupportRegionChanged += TerrainChanged;
        }
    }

    private void OnDisable()
    {
        if (world != null)
        {
            world.SupportRegionChanged -= TerrainChanged;
        }

        ClearDebris();
    }

    public bool CanPlace(BlockDataList blueprint, Vector3 position, Quaternion rotation, out string reason)
    {
        blueprint = HangarShellBlueprint.Expand(blueprint);
        if (buildings.Count >= maximumBuildings)
        {
            reason = "World building instance limit reached.";
            return false;
        }

        return BlueprintStructurePlacement.Check(world, blueprint, position, rotation, out reason);
    }

    public BlueprintStructureRuntime Place(BlockDataList blueprint, string name, Vector3 position, Quaternion rotation, UnitFaction faction)
    {
        BlueprintStructureValidation.ValidateBlueprint(blueprint);
        if (!CanPlace(blueprint, position, rotation, out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        var health = new float[blueprint.blocks.Count];
        for (int i = 0; i < health.Length; i++)
        {
            health[i] = float.MaxValue;
        }

        var state = new BlueprintStructureState
        {
            instanceId = Guid.NewGuid().ToString("N"),
            blueprintName = name,
            faction = faction,
            position = position,
            rotation = rotation,
            blueprint = blueprint,
            durability = health
        };
        BlueprintStructureRuntime building = RestoreBuilding(state);
        LastMessage = "Placed " + name;
        return building;
    }

    /// <summary>恢复一个已校验快照；禁用世界中仅准备层级，启用世界后才执行激活和耐久恢复。</summary>
    public BlueprintStructureRuntime RestoreBuilding(BlueprintStructureState state)
    {
        BlueprintStructureValidation.ValidateState(state);
        state = HangarShellBlueprint.ExpandState(state);
        BlueprintStructureValidation.ValidateState(state);
        if (!HasCapacity(state.blueprint.blocks.Count))
        {
            throw new InvalidOperationException("Active world building instance limit exceeded.");
        }

        BlueprintStructureRuntime building = Stage(state, transform);
        buildings.Add(building);
        if (isActiveAndEnabled)
        {
            building.Activate();
        }

        return building;
    }

    /// <summary>流式恢复逐批创建/启用模块；取消时回滚未完成实例，不发布半栋建筑。</summary>
    internal IEnumerator RestoreBuildingRoutine(BlueprintStructureState state, Action<BlueprintStructureRuntime> restored)
    {
        BlueprintStructureValidation.ValidateState(state);
        state = HangarShellBlueprint.ExpandState(state);
        BlueprintStructureValidation.ValidateState(state);
        if (!HasCapacity(state.blueprint.blocks.Count))
        {
            throw new InvalidOperationException("Active world building instance limit exceeded.");
        }

        BlueprintStructureRuntime building = Instantiate(RuntimeObjectTemplates.Instance.Building, transform);
        var root = building.gameObject;
        root.name = "Building " + state.blueprintName;
        root.SetActive(false);
        bool complete = false;
        IEnumerator work = building.InitializeRoutine(this, state);
        try
        {
            while (work.MoveNext())
            {
                yield return work.Current;
            }

            (work as IDisposable)?.Dispose();
            if (isActiveAndEnabled)
            {
                work = building.ActivateRoutine();
                while (work.MoveNext())
                {
                    yield return work.Current;
                }
            }

            buildings.Add(building);
            restored(building);
            complete = true;
        }
        finally
        {
            (work as IDisposable)?.Dispose();
            if (!complete && root != null)
            {
                buildings.Remove(building);
                root.SetActive(false);
                Destroy(root);
            }
        }
    }

    internal void OptimizeVisuals(Block block)
    {
        // 世界实例不生成接缝装饰，连接检测本身与所有其他结构相同。
        block.connectorPrefab = null;
        if (block.TryGetComponent(out HangarModule hangar))
        {
            hangar.UseExpandedShell();
        }

        if (!useIndividualRenderers)
        {
            renderCache.Apply(block);
        }
    }

    public bool HasCapacity(int additionalBlocks)
    {
        return buildings.Count < maximumBuildings && CanAddBlocks(additionalBlocks);
    }

    public bool CanAddBlocks(int additionalBlocks)
    {
        // Module count is not a gameplay budget. Staged loading still owns cancellation
        // and rollback; this rejects only invalid requests, not large intact designs.
        return additionalBlocks >= 0;
    }

    public void RemoveBuilding(BlueprintStructureRuntime building)
    {
        if (building == null || !buildings.Remove(building))
        {
            return;
        }

        pendingSupport.Remove(building);
        building.PrepareForUnload();
        building.gameObject.SetActive(false);
        Destroy(building.gameObject);
    }

    public void PrepareForUnload()
    {
        foreach (BlueprintStructureRuntime building in buildings)
        {
            if (building != null)
            {
                building.PrepareForUnload();
            }
        }
    }

    public void ClearSession()
    {
        ClearBuildings();
        ClearDebris();
        renderCache.Clear();
    }

    private void OnDestroy()
    {
        renderCache.Clear();
    }

    private BlueprintStructureRuntime Stage(BlueprintStructureState state, Transform parent)
    {
        BlueprintStructureRuntime building = Instantiate(RuntimeObjectTemplates.Instance.Building, parent);
        var root = building.gameObject;
        root.name = "Building " + state.blueprintName;
        root.SetActive(false);
        try
        {
            building.Initialize(this, state);
            return building;
        }
        catch
        {
            Destroy(root);
            throw;
        }
    }

    internal void ReplaceStructures(IReadOnlyList<BlueprintStructureState> states, Action commitEnvironment)
    {
        if (states.Count > maximumBuildings)
        {
            throw new InvalidOperationException("Building count exceeds the instance limit.");
        }

        var identities = new HashSet<string>();
        var restoredStates = new List<BlueprintStructureState>();
        foreach (BlueprintStructureState original in states)
        {
            BlueprintStructureValidation.ValidateState(original);
            BlueprintStructureState state = HangarShellBlueprint.ExpandState(original);
            BlueprintStructureValidation.ValidateState(state);
            restoredStates.Add(state);
            if (!identities.Add(state.instanceId))
            {
                throw new InvalidOperationException("Duplicate building identity.");
            }
        }

        var staged = new List<BlueprintStructureRuntime>();
        try
        {
            foreach (BlueprintStructureState state in restoredStates)
            {
                staged.Add(Stage(state, transform));
            }

            commitEnvironment();
        }
        catch
        {
            foreach (BlueprintStructureRuntime building in staged)
            {
                Destroy(building.gameObject);
            }

            throw;
        }

        ClearBuildings();
        ClearDebris();
        foreach (BlueprintStructureRuntime building in staged)
        {
            buildings.Add(building);
            building.Activate();
            pendingSupport.Add(building);
        }
    }

    private void ClearBuildings()
    {
        pendingSupport.Clear();
        foreach (BlueprintStructureRuntime building in buildings)
        {
            if (building != null)
            {
                building.PrepareForUnload();
                building.gameObject.SetActive(false);
                Destroy(building.gameObject);
            }
        }

        buildings.Clear();
    }

    private void TerrainChanged(Bounds changed)
    {
        foreach (BlueprintStructureRuntime building in buildings)
        {
            if (building != null && building.Anchored && BlueprintStructurePlacement.Footprint(building).Intersects(changed))
            {
                pendingSupport.Add(building);
            }
        }
    }

    private void FixedUpdate()
    {
        foreach (BlueprintStructureRuntime building in pendingSupport)
        {
            if (building != null && building.Anchored)
            {
                building.RefreshTerrainSupport();
                building.SupportRatio = BlueprintStructurePlacement.Support(world, building.Footprint, building.transform);
                if (collapseUnsupported && building.SupportRatio < minimumSupportRatio)
                {
                    building.ReleaseAnchor();
                }
            }
        }

        pendingSupport.Clear();
    }

    public void DamageSphere(Vector3 center, float radius, float damageAmount)
    {
        // Snapshot targets before a lethal hit modifies the structure's save membership.
        var targets = new List<Durability>();
        foreach (BlueprintStructureRuntime building in buildings)
        {
            if (building == null)
            {
                continue;
            }

            foreach (Block block in building.Blocks)
            {
                if (block != null && block.TryGetComponent(out Durability health))
                {
                    targets.Add(health);
                }
            }
        }

        damage.ApplyTargets(targets, center, radius, damageAmount);
    }

    internal void TrackFragment(ControlUnit fragment, int blockCount)
    {
        // 物理组由共享重组器创建，世界只持有生命周期，退出/卸载时可显式回收。
        debris.RemoveAll(piece => piece == null);
        bool overBudget = debris.Count >= maximumDebrisPieces || blockCount > maximumBlocksPerDebris;
        GameObject piece = fragment.gameObject;
        if (debris.Contains(piece))
        {
            return;
        }

        fragment.DamageSession = damage;
        fragment.FragmentCreated += TrackFragment;
        piece.transform.SetParent(transform, true);
        FragmentLifetime.Mark(piece);
        debris.Add(piece);
        // 预算是退场请求，不再跳过实体生成；即使是大型结构也先完整下落。
        StartCoroutine(RetireFragment(fragment, Mathf.Max(FragmentLifetime.Remaining(piece), overBudget ? 0f : debrisLifetime)));
    }

    private IEnumerator RetireFragment(ControlUnit fragment, float delay)
    {
        yield return new WaitForSeconds(delay);
        // 清场/读档先清除列表；只有正常退场才结算当前残骸里的货物与掉落。
        if (fragment != null && debris.Remove(fragment.gameObject))
        {
            fragment.FragmentCreated -= TrackFragment;
            WreckSalvage.ConvertGroup(fragment);
            Destroy(fragment.gameObject);
        }
    }

    private void ClearDebris()
    {
        foreach (GameObject piece in debris)
        {
            if (piece != null)
            {
                piece.GetComponent<ControlUnit>().FragmentCreated -= TrackFragment;
                piece.SetActive(false);
                Destroy(piece);
            }
        }

        debris.Clear();
    }
}
