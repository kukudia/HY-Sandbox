// 职责：用连接图重组所有模块；地基作为共享连接点，普通根保留驾驶舱或最大分量。
// 入口：Rebuild()；先捕获所有源状态，再规划/迁移，最后局部刷新并一次执行外层清理。
// 约束：保留已有根的姿态与运行组件；新碎片不覆盖RuntimeUnitMember原始单位身份。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class RuntimeGroupBuilder
{
    private readonly PlayManager _play;
    internal RuntimeGroupBuilder(PlayManager play)
    {
        _play = play;
    }

    internal static void RefreshUnit(ControlUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        PlayManager play = PlayManager.instance;
        if (play != null && play.gameObject.scene.GetPhysicsScene() == unit.gameObject.scene.GetPhysicsScene())
        {
            play.RefreshGroup(unit);
            return;
        }

        new RuntimeGroupBuilder(null).Refresh(unit, GroupPhysicsSettings.FromBody(unit.GetComponent<Rigidbody>()));
    }

    internal void Refresh(ControlUnit unit, GroupPhysicsSettings settings)
    {
        var blocks = new List<Block>();
        unit.GetComponentsInChildren(false, blocks);
        blocks.RemoveAll(block => !block.isActiveAndEnabled || block.GetComponentInParent<ControlUnit>() != unit);
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.Refresh, blocks.Count);
        if (blocks.Count == 0)
        {
            if (unit.TryGetComponent(out IBlockFoundation foundation))
            {
                unit.GetComponent<Rigidbody>().mass = foundation.RootMass;
            }

            unit.RefreshChildren();
            return;
        }

        unit.Connections.Prepare(blocks, unit.transform);
        Rebuild(blocks, settings, BlockGroupManager.GroupPreparedBlocks(blocks));
    }

    internal void Rebuild(List<Block> blocks, GroupPhysicsSettings settings, List<List<Block>> preparedGroups = null)
    {
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BodyRebuild, blocks?.Count ?? 0);
        if (blocks == null || blocks.Count == 0)
        {
            return;
        }

        Dictionary<Block, SourceSnapshot> sources = CaptureSourceBodies(blocks);
        List<List<Block>> groups = preparedGroups ?? BlockGroupManager.GroupRuntimeBlocks(blocks);
        List<GroupPlan> plans = PlanGroups(groups, sources);
        MergeFoundationGroups(plans);
        Dictionary<SourceSnapshot, GroupPlan> retained = SelectRetainedGroups(plans, sources);
        GameObject parentPrefab = null;
        foreach (GroupPlan plan in plans)
        {
            bool reuse = plan.Source != null && retained.TryGetValue(plan.Source, out GroupPlan selected) && selected == plan;
            if (reuse)
            {
                plan.Unit = plan.Source.Unit;
            }
            else
            {
                parentPrefab ??= Resources.Load<GameObject>("BlocksParent/BlocksParent");
                // 新插值刚体直接出生在最终姿态，避免先Instantiate再只移动Transform产生拖影。
                GameObject parent = Object.Instantiate(parentPrefab, BlockGroupManager.CalculateGroupCenter(plan.Blocks), Quaternion.identity);
                parent.name = "Detached Group";
                // 先迁移空根，模块始终留在原PhysicsScene，避免转场重启耐久生命周期或丢失接触。
                Scene sourceScene = plan.Source != null ? plan.Source.Unit.gameObject.scene : plan.Blocks[0].gameObject.scene;
                if (parent.scene != sourceScene)
                {
                    SceneManager.MoveGameObjectToScene(parent, sourceScene);
                }

                plan.Unit = parent.GetComponent<ControlUnit>();
                plan.Unit.faction = plan.Source?.Faction ?? UnitFaction.Player;
            }

            foreach (Block block in plan.Blocks)
            {
                if (block.GetComponentInParent<ControlUnit>() != plan.Unit)
                {
                    block.transform.SetParent(plan.Unit.transform, true);
                }

                plan.Mass += block.mass;
                if (_play != null && plan.Source?.Foundation == null)
                {
                    block.showConnectors = _play.showConnectors;
                    block.showLabel = _play.showLabel;
                }

                Cockpit cockpit = block.GetComponent<Cockpit>();
                if (cockpit == null)
                {
                    continue;
                }

                plan.Anchored |= cockpit.Anchored;
                plan.Unit.faction = cockpit.faction;
                if (_play != null && cockpit.faction == UnitFaction.Player && cockpit.ControlMode == UnitControlMode.Player)
                {
                    _play.blocksParent = plan.Unit.transform;
                    GameManager.instance.blocksParent = plan.Unit.transform;
                }
            }
        }

        // 所有迁移完成后再读取新质心，避免先更新主体影响其他碎片的速度继承。
        var refreshed = new HashSet<ControlUnit>();
        foreach (GroupPlan plan in plans)
        {
            Rigidbody body = plan.Unit.GetComponent<Rigidbody>();
            body.mass = Mathf.Max(0.001f, plan.Mass + (plan.OnFoundation ? plan.Source.Foundation.RootMass : 0f));
            settings.ApplyTo(body);
            body.isKinematic = plan.Anchored;
            if (!plan.Anchored)
            {
                if (plan.Source != null)
                {
                    plan.Source.Motion.Restore(body, settings);
                }
            }

            plan.Unit.RefreshChildren();
            if (!plan.OnFoundation && !plan.Unit.HasAnyCockpit)
            {
                FragmentLifetime.Mark(plan.Unit.gameObject);
            }

            if (plan.Source != null)
            {
                plan.Unit.ProtectExplosionContacts(plan.Source.ProtectedUntil - Time.time);
            }

            plan.Unit.AssignRuntimeOwnershipToBlocks(false);
            foreach (Block block in plan.Blocks)
            {
                block.RefreshRuntimeConnectionPose();
            }

            if (plan.Source?.Foundation != null && !plan.OnFoundation)
            {
                plan.Source.Foundation.OnFragmentDetached(plan.Blocks, plan.Unit);
            }

            if (plan.Source != null && plan.Unit != plan.Source.Unit)
            {
                plan.Source.Unit.NotifyFragmentCreated(plan.Unit, plan.Blocks.Count);
            }

            if (!plan.OnFoundation)
            {
                _play?.RegisterControlUnitWithoutCleanup(plan.Unit);
            }

            refreshed.Add(plan.Unit);
        }

        foreach (SourceSnapshot source in sources.Values)
        {
            // 合并/部分输入时，未被保留的源根也可能还有其他模块；只刷新真正参与本次迁移的根。
            if (refreshed.Add(source.Unit))
            {
                if (source.Foundation != null && !retained.ContainsKey(source))
                {
                    source.Unit.GetComponent<Rigidbody>().mass = source.Foundation.RootMass;
                }

                source.Unit.RefreshChildren();
            }
        }

        if (_play != null && Camera.main != null && Camera.main.TryGetComponent(out CameraController camera))
        {
            camera.playerBody = _play.blocksParent;
        }
    }

    private static Dictionary<Block, SourceSnapshot> CaptureSourceBodies(List<Block> blocks)
    {
        var sourceBodies = new Dictionary<Block, SourceSnapshot>();
        var units = new Dictionary<ControlUnit, SourceSnapshot>();
        foreach (Block block in blocks)
        {
            if (block == null || !block.isActiveAndEnabled || sourceBodies.ContainsKey(block))
            {
                continue;
            }

            ControlUnit unit = block.GetComponentInParent<ControlUnit>();
            if (unit == null || unit.GetComponent<Rigidbody>() == null)
            {
                continue;
            }

            if (!units.TryGetValue(unit, out SourceSnapshot snapshot))
            {
                snapshot = new SourceSnapshot(unit);
                units.Add(unit, snapshot);
            }

            sourceBodies.Add(block, snapshot);
        }

        return sourceBodies;
    }

    private static List<GroupPlan> PlanGroups(List<List<Block>> groups, Dictionary<Block, SourceSnapshot> sources)
    {
        var plans = new List<GroupPlan>(groups.Count);
        foreach (List<Block> group in groups)
        {
            var plan = new GroupPlan
            {
                Blocks = group
            };
            foreach (Block block in group)
            {
                if (!sources.TryGetValue(block, out SourceSnapshot source))
                {
                    continue;
                }

                bool cockpit = block.GetComponent<Cockpit>() != null;
                if (plan.Source == null || (cockpit && !plan.ContainsCockpit))
                {
                    plan.Source = source;
                }

                plan.ContainsCockpit |= cockpit;
            }

            plans.Add(plan);
        }

        return plans;
    }

    // 两个不相邻的接地分量经同一地基相连；没有接地成员的分量独立生成刚体。
    private static void MergeFoundationGroups(List<GroupPlan> plans)
    {
        var roots = new Dictionary<SourceSnapshot, GroupPlan>();
        foreach (GroupPlan plan in plans)
        {
            IBlockFoundation foundation = plan.Source?.Foundation;
            if (foundation == null || !plan.Blocks.Exists(foundation.Supports))
            {
                continue;
            }

            if (roots.TryGetValue(plan.Source, out GroupPlan root))
            {
                root.Blocks.AddRange(plan.Blocks);
                root.ContainsCockpit |= plan.ContainsCockpit;
                plan.Blocks.Clear();
            }
            else
            {
                roots.Add(plan.Source, plan);
                plan.OnFoundation = true;
                plan.Anchored = plan.Source.WasKinematic;
            }
        }

        plans.RemoveAll(plan => plan.Blocks.Count == 0);
    }

    private static Dictionary<SourceSnapshot, GroupPlan> SelectRetainedGroups(List<GroupPlan> plans, Dictionary<Block, SourceSnapshot> sources)
    {
        var retained = new Dictionary<SourceSnapshot, GroupPlan>();
        foreach (GroupPlan plan in plans)
        {
            SourceSnapshot source = plan.Source;
            if (source == null || (source.Foundation != null && !plan.OnFoundation))
            {
                continue;
            }

            if (!source.RetentionChecked)
            {
                source.RetentionChecked = true;
                source.CanRetain = true;
                foreach (Block child in source.Unit.GetComponentsInChildren<Block>(true))
                {
                    if (child.isActiveAndEnabled && !sources.ContainsKey(child))
                    {
                        source.CanRetain = false;
                        break;
                    }
                }
            }

            if (!source.CanRetain)
            {
                continue;
            }

            if (!retained.TryGetValue(source, out GroupPlan previous) || (plan.ContainsCockpit && !previous.ContainsCockpit) || (plan.ContainsCockpit == previous.ContainsCockpit && plan.Blocks.Count > previous.Blocks.Count))
            {
                retained[source] = plan;
            }
        }

        return retained;
    }

    private sealed class GroupPlan
    {
        internal List<Block> Blocks;
        internal SourceSnapshot Source;
        internal bool ContainsCockpit;
        internal bool Anchored;
        internal bool OnFoundation;
        internal float Mass;
        internal ControlUnit Unit;
    }

    private sealed class SourceSnapshot
    {
        internal readonly ControlUnit Unit;
        internal readonly UnitFaction Faction;
        internal readonly RigidbodyMotion Motion;
        internal readonly float ProtectedUntil;
        internal readonly IBlockFoundation Foundation;
        internal readonly bool WasKinematic;
        internal bool RetentionChecked;
        internal bool CanRetain;
        internal SourceSnapshot(ControlUnit unit)
        {
            Unit = unit;
            Faction = unit.faction;
            ProtectedUntil = unit.ExplosionCollisionProtectedUntil;
            Rigidbody body = unit.GetComponent<Rigidbody>();
            Foundation = unit.GetComponent<IBlockFoundation>();
            WasKinematic = body.isKinematic;
            Motion = new RigidbodyMotion(body);
        }
    }
}

// A shared anchor contributes connectivity and owns the lifetime of fragments leaving it.
internal interface IBlockFoundation
{
    float RootMass
    {
        get;
    }

    bool Supports(Block block);
    void OnFragmentDetached(List<Block> blocks, ControlUnit fragment);
}
