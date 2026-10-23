// 职责：拥有一栋世界建筑的方块、虚拟支撑范围与刚体；处理损坏、断裂和脱锚。
// 入口：Initialize() / Activate() / DestroyBlock() / ReleaseAnchor() / Capture()。
// 生命周期：世界存档只捕获主体；断裂残骸由 WorldBuildingManager 限量管理。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ControlUnit), typeof(Rigidbody))]
public sealed class BuildingRuntime : MonoBehaviour
{
    private WorldBuildingManager manager;
    private string blueprintName;
    private string instanceId;
    private readonly List<Block> blocks = new List<Block>();
    private readonly List<Block> neighbors = new List<Block>();
    private readonly BuildingConnectionGraph connections = new BuildingConnectionGraph();
    private BuildingFoundationData foundationData;
    private WorldBuildingState pendingState;
    private EnemyOutpostData outpost;
    public EnemyOutpostData Outpost => outpost;
    public string BlueprintName => blueprintName;

    private Rigidbody body;
    private int damageBatchDepth;
    private bool structureDirty;
    public Vector2Int FootprintSize => new Vector2Int(foundationData.width, foundationData.length);
    public bool Anchored => body != null && body.isKinematic;
    public bool IsOperational => isActiveAndEnabled && pendingState == null && blocks.Count > 0;
    public bool IsSimulationActive => manager != null && manager.SimulationEnabled && manager.isActiveAndEnabled && IsOperational;
    public float SupportRatio { get; internal set; } = 1f;
    public IReadOnlyList<Block> Blocks => blocks;
    public string InstanceId => instanceId;
    public bool NeedsActivation => pendingState != null;

    public event Action<Block> BlockDestroyed;
    internal bool TryRefit(BlockDataList design, out string reason)
    {
        reason = "The building cannot be refitted.";
        if (!IsOperational || !BuildingRefit.Apply(this, blocks, manager, design, out reason))
        {
            return false;
        }

        body.mass = TotalMass();
        GetComponent<ControlUnit>().RefreshChildren();
        Physics.SyncTransforms();
        return true;
    }

    internal void RefreshStructure()
    {
        Physics.SyncTransforms();
        SplitDisconnected();
        body.mass = TotalMass();
        GetComponent<ControlUnit>().RefreshChildren();
    }

    internal void BeginDamageBatch()
    {
        damageBatchDepth++;
    }

    internal void EndDamageBatch()
    {
        damageBatchDepth--;
        if (damageBatchDepth == 0 && structureDirty)
        {
            structureDirty = false;
            RefreshStructure();
        }
    }

    public void Initialize(WorldBuildingManager owner, WorldBuildingState state)
    {
        IEnumerator work = InitializeRoutine(owner, state);
        while (work.MoveNext())
        {
        }
    }

    internal IEnumerator InitializeRoutine(WorldBuildingManager owner, WorldBuildingState state)
    {
        manager = owner;
        pendingState = state;
        outpost = state.blueprint.IsOutpost ? state.blueprint.outpost : null;
        instanceId = state.instanceId;
        blueprintName = state.blueprintName;
        foundationData = new BuildingFoundationData
        {
            width = state.blueprint.foundation.width,
            length = state.blueprint.foundation.length
        };
        body = GetComponent<Rigidbody>();
        body.isKinematic = state.anchored;
        body.useGravity = true;
        ControlUnit unit = GetComponent<ControlUnit>();
        unit.BindBuilding(this);
        unit.runtimeUnitId = instanceId;
        unit.faction = state.faction;
        transform.SetPositionAndRotation(state.position, state.rotation);
        var budget = System.Diagnostics.Stopwatch.StartNew();
        foreach (BlockData data in state.blueprint.blocks)
        {
            if (!BlueprintBlockFactory.TryRestore(data, transform, state.faction, out Block block))
            {
                throw new InvalidOperationException("Could not instantiate " + data.resourcePath);
            }

            block.transform.SetLocalPositionAndRotation(
                new Vector3(data.posX, data.posY, data.posZ),
                new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW));
            block.uniqueId = instanceId + ":" + data.id;
            block.showConnectors = false;
            block.showLabel = false;
            block.gameObject.SetActive(false);
            blocks.Add(block);
            RuntimeUnitMember.Ensure(block.gameObject, instanceId, state.faction);
            owner.OptimizeVisuals(block);
            if (budget.Elapsed.TotalMilliseconds >= owner.LoadingFrameBudgetMilliseconds)
            {
                yield return null;
                budget.Restart();
            }
        }

        body.mass = TotalMass();
    }

    public void Activate()
    {
        IEnumerator work = ActivateRoutine();
        while (work.MoveNext())
        {
        }
    }

    internal IEnumerator ActivateRoutine()
    {
        if (pendingState == null || !transform.parent.gameObject.activeInHierarchy)
        {
            yield break;
        }

        body.isKinematic = true;
        gameObject.SetActive(true);
        var budget = System.Diagnostics.Stopwatch.StartNew();
        // 所有子块 OnEnable 会重置耐久；必须在整个层级启用完成后恢复状态。
        for (int i = 0; i < blocks.Count; i++)
        {
            BlockData data = pendingState.blueprint.blocks[i];
            Block block = blocks[i];
            block.gameObject.SetActive(true);
            // 禁用层级的 CSV Awake 延后到此时；实例尺寸必须在其应用模板参数后重建。
            bool changed = block.x != data.x || block.y != data.y || block.z != data.z;
            block.x = data.x;
            block.y = data.y;
            block.z = data.z;
            block.RefreshRuntimeData(changed);
            Durability health = blocks[i].GetComponent<Durability>();
            if (health != null)
            {
                health.currentDurability = Mathf.Min(health.maxDurability, pendingState.durability[i]);
            }

            if (budget.Elapsed.TotalMilliseconds >= manager.LoadingFrameBudgetMilliseconds)
            {
                yield return null;
                budget.Restart();
            }
        }

        body.isKinematic = pendingState.anchored;
        pendingState = null;
        body.mass = TotalMass();
        GetComponent<ControlUnit>().RefreshChildren();
        Physics.SyncTransforms();
    }

    /// <summary>停用层级前取消运输，允许携带物在正常活动层级中安全脱离；不转换残骸或释放仓内货物。</summary>
    public void PrepareForUnload()
    {
        foreach (Bot bot in GetComponentsInChildren<Bot>(true))
        {
            bot.PrepareForHomeDestruction();
        }
    }

    public WorldBuildingState Capture()
    {
        if (pendingState != null)
        {
            return pendingState;
        }

        var blueprint = new BlockDataList
        {
            outpost = outpost,
            foundation = new BuildingFoundationData
            {
                width = foundationData.width,
                length = foundationData.length
            }
        };
        var health = new List<float>();
        foreach (Block block in blocks)
        {
            if (block == null || !block.gameObject.activeSelf)
            {
                continue;
            }

            var data = new BlockData(block);
            data.id = block.uniqueId.Substring(instanceId.Length + 1);
            Vector3 local = block.transform.localPosition;
            Quaternion rotation = block.transform.localRotation;
            data.posX = local.x;
            data.posY = local.y;
            data.posZ = local.z;
            data.rotX = rotation.x;
            data.rotY = rotation.y;
            data.rotZ = rotation.z;
            data.rotW = rotation.w;
            blueprint.blocks.Add(data);
            Durability durability = block.GetComponent<Durability>();
            health.Add(durability != null ? durability.currentDurability : 100f);
        }

        return new WorldBuildingState
        {
            instanceId = instanceId,
            blueprintName = blueprintName,
            faction = GetComponent<ControlUnit>().faction,
            position = transform.position,
            rotation = transform.rotation,
            anchored = Anchored,
            blueprint = blueprint,
            durability = health.ToArray()
        };
    }

    public void DestroyBlock(Block block)
    {
        if (block == null || !blocks.Remove(block))
        {
            return;
        }

        BeginDamageBatch();
        try
        {
            bool explode = block.canExplode && !(block.GetComponent<Durability>()?.DestroyedByCollision ?? false);
            Vector3 center = block.transform.position;
            float radius = block.explosionRadius;
            float force = block.explosionForce;
            if (explode)
            {
                VisualEffectsManager.TryPlayBlockExplosion(block);
            }
            else
            {
                VisualEffectsManager.TryPlayBlockRemoved(block);
            }

            foreach (Bot bot in block.GetComponentsInChildren<Bot>(true))
            {
                bot.PrepareForHomeDestruction();
            }

            block.GetComponent<CargoHold>()?.ReleaseContents(GetComponent<ControlUnit>().faction);
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
            structureDirty = true;
            if (explode)
            {
                manager.DamageSphere(center, radius, force);
                manager.World?.DamageSphere(center, radius, force);
            }

            BlockDestroyed?.Invoke(block);
        }
        finally
        {
            EndDamageBatch();
        }
    }

    private void SplitDisconnected()
    {
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingSupport, blocks.Count);
        connections.Prepare(blocks, transform);
        // 编辑底座只提供虚拟连接点，世界中不生成模型、碰撞体或底座方块。
        // 从与这些点匹配的朝下 Connector 出发，再沿真实模块连接遍历。
        var connected = new HashSet<Block>();
        using (SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingGround, blocks.Count))
        {
            foreach (Block block in blocks)
            {
                if (HasGroundConnector(block))
                {
                    connected.Add(block);
                }
            }
        }

        var queue = new Queue<Block>(connected);
        using (SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingTraversal, blocks.Count))
        {
            while (queue.Count > 0)
            {
                connections.CollectNeighbors(queue.Dequeue(), neighbors);
                foreach (Block neighbor in neighbors)
                {
                    // 大多数边指向已遍历的模块；只为新候选查询Unity层级归属。
                    if (!connected.Contains(neighbor) && neighbor.GetComponentInParent<BuildingRuntime>() == this)
                    {
                        connected.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        var detached = new HashSet<Block>();
        foreach (Block block in blocks)
        {
            if (block != null && !connected.Contains(block))
            {
                detached.Add(block);
            }
        }

        while (detached.Count > 0)
        {
            Block first = null;
            foreach (Block block in detached)
            {
                first = block;
                break;
            }

            var group = new List<Block>();
            queue.Enqueue(first);
            detached.Remove(first);
            using (SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingTraversal, detached.Count + 1))
            {
                while (queue.Count > 0)
                {
                    Block block = queue.Dequeue();
                    group.Add(block);
                    connections.CollectNeighbors(block, neighbors);
                    foreach (Block neighbor in neighbors)
                    {
                        if (detached.Remove(neighbor))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            foreach (Block block in group)
            {
                blocks.Remove(block);
            }

            using (SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingDebris, group.Count))
            {
                manager.CreateDebris(group, body);
            }
        }
    }

    private bool HasGroundConnector(Block block)
    {
        foreach (Connector connector in block.connectors)
        {
            if (!connector.canConnect || Vector3.Dot(block.GetConnectorWorldNormal(connector), -transform.up) <= 0.75f)
            {
                continue;
            }

            Vector3 local = transform.InverseTransformPoint(block.GetConnectorWorldPosition(connector));
            int x = Mathf.RoundToInt(local.x + 1.5f);
            int z = Mathf.RoundToInt(local.z + 1.5f);
            if (x >= 0 && x < foundationData.width && z >= 0 && z < foundationData.length && (local - WorldBuildingPlacement.LocalAnchor(x, z)).sqrMagnitude < 0.25f * 0.25f)
            {
                return true;
            }
        }

        return false;
    }

    public void ReleaseAnchor()
    {
        if (Anchored)
        {
            // 必须先移除体素源碰撞，再让建筑参加下一物理步。
            manager.World?.FlushDirty();
            body.isKinematic = false;
            body.WakeUp();
        }
    }

    private float TotalMass()
    {
        float mass = 1f;
        foreach (Block block in blocks)
        {
            if (block != null)
            {
                mass += Mathf.Max(0.01f, block.mass);
            }
        }

        return mass;
    }
}
