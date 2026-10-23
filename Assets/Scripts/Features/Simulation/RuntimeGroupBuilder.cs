// 职责：保留含驾驶舱或最大分量的源刚体，仅迁移分离模块，并恢复捕获的源运动与归属。
// 入口：Rebuild()；先捕获所有源状态，再规划/迁移，最后局部刷新并一次执行外层清理。
// 约束：保留已有根的姿态与运行组件；新碎片不覆盖RuntimeUnitMember原始单位身份。
using System.Collections.Generic;
using UnityEngine;

internal sealed class RuntimeGroupBuilder
{
    private readonly PlayManager _play;
    internal RuntimeGroupBuilder(PlayManager play)
    {
        _play = play;
    }

    internal void Rebuild(List<Block> blocks, GroupPhysicsSettings settings)
    {
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BodyRebuild, blocks?.Count ?? 0);
        if (blocks == null || blocks.Count == 0)
        {
            return;
        }

        Dictionary<Block, SourceSnapshot> sources = CaptureSourceBodies(blocks);
        List<List<Block>> groups = BlockGroupManager.GroupBlocks(blocks);
        List<GroupPlan> plans = PlanGroups(groups, sources);
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
                block.showConnectors = _play.showConnectors;
                block.showLabel = _play.showLabel;
                Cockpit cockpit = block.GetComponent<Cockpit>();
                if (cockpit == null)
                {
                    continue;
                }

                plan.Anchored |= cockpit.Anchored;
                plan.Unit.faction = cockpit.faction;
                if (cockpit.faction == UnitFaction.Player && cockpit.ControlMode == UnitControlMode.Player)
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
            body.mass = Mathf.Max(0.001f, plan.Mass);
            body.linearDamping = Mathf.Max(0f, settings.LinearDamping);
            body.angularDamping = Mathf.Max(0f, settings.AngularDamping);
            body.maxDepenetrationVelocity = Mathf.Max(0f, settings.MaxDepenetrationVelocity);
            body.isKinematic = plan.Anchored;
            if (!plan.Anchored)
            {
                RestoreGroupPhysics(body, plan.Source, settings);
            }

            plan.Unit.RefreshChildren();
            plan.Unit.AssignRuntimeOwnershipToBlocks(false);
            _play.RegisterControlUnitWithoutCleanup(plan.Unit);
            refreshed.Add(plan.Unit);
        }

        foreach (SourceSnapshot source in sources.Values)
        {
            // 合并/部分输入时，未被保留的源根也可能还有其他模块；只刷新真正参与本次迁移的根。
            if (refreshed.Add(source.Unit))
            {
                source.Unit.RefreshChildren();
            }
        }

        if (Camera.main != null && Camera.main.TryGetComponent(out CameraController camera))
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

    private static Dictionary<SourceSnapshot, GroupPlan> SelectRetainedGroups(List<GroupPlan> plans, Dictionary<Block, SourceSnapshot> sources)
    {
        var retained = new Dictionary<SourceSnapshot, GroupPlan>();
        foreach (GroupPlan plan in plans)
        {
            SourceSnapshot source = plan.Source;
            if (source == null)
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

    private static void RestoreGroupPhysics(Rigidbody target, SourceSnapshot source, GroupPhysicsSettings settings)
    {
        if (source == null)
        {
            return;
        }

        Vector3 tangential = Vector3.Cross(source.AngularVelocity, target.worldCenterOfMass - source.WorldCenterOfMass);
        target.linearVelocity = source.LinearVelocity + Vector3.ClampMagnitude(tangential, settings.MaximumTangentialSpeed);
        target.angularVelocity = Vector3.ClampMagnitude(source.AngularVelocity, settings.MaximumAngularSpeed);
        target.WakeUp();
    }

    private sealed class GroupPlan
    {
        internal List<Block> Blocks;
        internal SourceSnapshot Source;
        internal bool ContainsCockpit;
        internal bool Anchored;
        internal float Mass;
        internal ControlUnit Unit;
    }

    private sealed class SourceSnapshot
    {
        internal readonly ControlUnit Unit;
        internal readonly UnitFaction Faction;
        internal readonly Vector3 LinearVelocity;
        internal readonly Vector3 AngularVelocity;
        internal readonly Vector3 WorldCenterOfMass;
        internal bool RetentionChecked;
        internal bool CanRetain;
        internal SourceSnapshot(ControlUnit unit)
        {
            Unit = unit;
            Faction = unit.faction;
            Rigidbody body = unit.GetComponent<Rigidbody>();
            LinearVelocity = body.linearVelocity;
            AngularVelocity = body.angularVelocity;
            WorldCenterOfMass = body.worldCenterOfMass;
        }
    }
}
