// 职责：按连接分量建立运行组，迁移方块并恢复受限的源刚体运动。
// 入口：Rebuild()；由 PlayManager.AssignBlocksToParentGroups() 调用。
// 约束：先捕获源刚体；重组保留 RuntimeUnitMember 的原始单位归属。
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
        Dictionary<Block, Rigidbody> sourceBodies = CaptureSourceBodies(blocks);
        List<List<Block>> groups = BlockGroupManager.GroupBlocks(blocks);
        GameObject parentPrefab = Resources.Load<GameObject>("BlocksParent/BlocksParent");
        int groupIndex = 1;
        foreach (List<Block> group in groups)
        {
            // Initialize the interpolated Rigidbody at its final pose. Moving only Transform
            // after Instantiate leaves the prefab pose in the interpolation history until a physics step.
            GameObject groupParent = Object.Instantiate(parentPrefab, BlockGroupManager.CalculateGroupCenter(group), Quaternion.identity);
            groupParent.name = $"Group_{groupIndex++}";
            float mass = 0f;
            bool anchored = false;
            ControlUnit groupControl = groupParent.GetComponent<ControlUnit>();
            foreach (Block block in group)
            {
                Cockpit cockpit = block.GetComponent<Cockpit>();
                if (cockpit != null)
                {
                    anchored |= cockpit.Anchored;
                    groupControl.faction = cockpit.faction;
                    if (cockpit.faction == UnitFaction.Player && cockpit.ControlMode == UnitControlMode.Player)
                    {
                        groupParent.name = !string.IsNullOrEmpty(SaveManager.instance?.currentSaveName) ? SaveManager.instance.currentSaveName : "Player Fleet";
                        _play.blocksParent = groupParent.transform;
                        GameManager.instance.blocksParent = groupParent.transform;
                        Debug.Log($"Change new block parent {_play.blocksParent}.");
                    }
                }

                block.transform.SetParent(groupParent.transform);
                mass += block.mass;
                block.showConnectors = _play.showConnectors;
                block.showLabel = _play.showLabel;
            }

            Debug.Log($"{groupParent.name} mass: {mass}");
            Rigidbody body = groupParent.GetComponent<Rigidbody>();
            body.mass = mass;
            body.linearDamping = Mathf.Max(0f, settings.LinearDamping);
            body.angularDamping = Mathf.Max(0f, settings.AngularDamping);
            body.maxDepenetrationVelocity = Mathf.Max(0f, settings.MaxDepenetrationVelocity);
            body.isKinematic = anchored;
            if (!anchored)
            {
                RestoreGroupPhysics(body, group, sourceBodies, settings);
            }

            groupControl.RefreshChildren();
            groupControl.AssignRuntimeOwnershipToBlocks(false);
        }

        if (Camera.main != null && Camera.main.TryGetComponent(out CameraController camera))
        {
            camera.playerBody = _play.blocksParent;
        }
    }

    private static Dictionary<Block, Rigidbody> CaptureSourceBodies(List<Block> blocks)
    {
        Dictionary<Block, Rigidbody> sourceBodies = new Dictionary<Block, Rigidbody>();
        foreach (Block block in blocks)
        {
            if (block == null)
            {
                continue;
            }

            ControlUnit sourceUnit = block.GetComponentInParent<ControlUnit>();
            if (sourceUnit != null && !sourceBodies.ContainsKey(block))
            {
                sourceBodies[block] = sourceUnit.GetComponent<Rigidbody>();
            }
        }

        return sourceBodies;
    }

    private static void RestoreGroupPhysics(
        Rigidbody targetBody,
        List<Block> group,
        Dictionary<Block, Rigidbody> sourceBodies,
        GroupPhysicsSettings settings)
    {
        if (targetBody == null || group == null || group.Count == 0)
        {
            return;
        }

        Rigidbody sourceBody = null;
        foreach (Block block in group)
        {
            if (block == null)
            {
                continue;
            }

            if (sourceBodies.TryGetValue(block, out sourceBody))
            {
                break;
            }
        }

        if (sourceBody == null)
        {
            return;
        }

        // Limit only the rotational contribution, preserving the construct's existing linear motion.
        Vector3 sourceVelocity = sourceBody.linearVelocity;
        Vector3 tangentialVelocity = sourceBody.GetPointVelocity(targetBody.worldCenterOfMass) - sourceVelocity;
        targetBody.linearVelocity = sourceVelocity + Vector3.ClampMagnitude(tangentialVelocity, settings.MaximumTangentialSpeed);
        targetBody.angularVelocity = Vector3.ClampMagnitude(sourceBody.angularVelocity, settings.MaximumAngularSpeed);
        targetBody.WakeUp();
    }
}
