// 职责：验证增量重组保留驾驶主体、世界姿态、源点速度、阵营/货物归属及局部刷新边界。
// 入口：Run()；要求全新Siege Play，受控断链夹具不代表自然战斗性能。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeRegroupChecks
{
    public static string Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0 || session.Fleet.Ships.Any(entry => entry != null))
        {
            throw new InvalidOperationException("Requires fresh Siege Play without deployed ships.");
        }

        var checks = new List<string>();
        Check(
            checks,
            "two ships deploy for regroup fixture",
            session.TryDeploy(0, out _) && session.TryDeploy(1, out _));
        SiegeFleetEntry ship = session.Fleet.Ships[0];
        ControlUnit source = ship.Unit;
        Rigidbody originalBody = source.GetComponent<Rigidbody>();
        Transform originalRoot = source.transform;
        string originalId = source.runtimeUnitId;
        HoverThruster[] unrelatedThrusters = session.Fleet.Ships[1].Unit.hoverThrusters;
        List<Block> blocks = source.GetComponentsInChildren<Block>().ToList();
        originalBody.position += Vector3.up * 25f;
        originalBody.rotation = Quaternion.Euler(5f, 37f, 2f);
        source.transform.SetPositionAndRotation(originalBody.position, originalBody.rotation);
        originalBody.isKinematic = false;
        originalBody.linearVelocity = new Vector3(2f, 3f, 4f);
        originalBody.angularVelocity = new Vector3(0.4f, 0.2f, 0.1f);
        Physics.SyncTransforms();
        var poses = blocks.ToDictionary(block => block, block => new Pose(block.transform.position, block.transform.rotation));
        PlayManager.instance.RefreshGroup(source);
        Check(
            checks,
            "intact regroup retains root rigidbody and runtime identity",
            ship.Unit == source && source.transform == originalRoot && source.GetComponent<Rigidbody>() == originalBody && source.runtimeUnitId == originalId);
        Check(checks, "intact regroup retains world poses", PosesUnchanged(poses));
        Check(
            checks,
            "intact regroup preserves linear and angular motion",
            Near(originalBody.linearVelocity, new Vector3(2f, 3f, 4f)) && Near(originalBody.angularVelocity, new Vector3(0.4f, 0.2f, 0.1f)));
        Check(
            checks,
            "unrelated ship component caches are not globally refreshed",
            ReferenceEquals(unrelatedThrusters, session.Fleet.Ships[1].Unit.hoverThrusters));
        Block detached = blocks.First(block => block.GetComponent<Cockpit>() == null && block.GetComponent<CargoHold>() == null);
        detached.DisConnectAllConnectors(false);
        List<List<Block>> expectedGroups = BlockGroupManager.GroupBlocks(blocks);
        List<Block> cockpitGroup = expectedGroups.Single(group => group.Contains(ship.Cockpit.GetComponent<Block>()));
        Vector3 sourceCenter = originalBody.worldCenterOfMass;
        Vector3 sourceVelocity = originalBody.linearVelocity;
        Vector3 sourceAngular = originalBody.angularVelocity;
        PlayManager.instance.RefreshGroup(source);
        Check(
            checks,
            "split keeps cockpit component on original rigidbody",
            ship.Unit == source && cockpitGroup.All(block => block.GetComponentInParent<ControlUnit>() == source));
        Check(
            checks,
            "split produces exactly the expected physical components",
            blocks.Select(block => block.GetComponentInParent<ControlUnit>()).Distinct().Count() == expectedGroups.Count);
        Check(
            checks,
            "only separated modules move to new roots",
            blocks.Count(block => block.GetComponentInParent<ControlUnit>() != source) == blocks.Count - cockpitGroup.Count);
        Check(checks, "split preserves every surviving module world pose", PosesUnchanged(poses));
        Check(
            checks,
            "all fragments preserve original deployment ownership",
            blocks.All(block => block.GetComponent<RuntimeUnitMember>().ownerUnitId == ship.Id));
        Check(
            checks,
            "manual input and camera remain on cockpit component",
            source.AcceptsPlayerInput && PlayManager.instance.blocksParent == originalRoot && CameraController.instance.playerBody == originalRoot);
        Check(
            checks,
            "each component has exact module mass",
            blocks.Select(block => block.GetComponentInParent<ControlUnit>()).Distinct().All(unit => Mathf.Abs(unit.GetComponent<Rigidbody>().mass - unit.GetComponentsInChildren<Block>().Sum(block => block.mass)) < 0.001f));
        var settings = new SerializedObject(PlayManager.instance);
        float tangentLimit = settings.FindProperty("_maximumInheritedTangentialSpeed").floatValue;
        float angularLimit = settings.FindProperty("_maximumInheritedAngularSpeed").floatValue;
        Check(
            checks,
            "all fragment motion comes from immutable pre-split point velocity",
            blocks.Select(block => block.GetComponentInParent<ControlUnit>()).Distinct().All(unit =>
        {
            Rigidbody body = unit.GetComponent<Rigidbody>();
            return Near(
                body.linearVelocity,
                sourceVelocity + Vector3.ClampMagnitude(Vector3.Cross(sourceAngular, body.worldCenterOfMass - sourceCenter), tangentLimit)) && Near(body.angularVelocity, Vector3.ClampMagnitude(sourceAngular, angularLimit));
        }));
        Check(
            checks,
            "all newly created fragments are registered immediately",
            blocks.Select(block => block.GetComponentInParent<ControlUnit>()).Distinct().All(unit => PlayManager.instance.GetControlUnits().Contains(unit)));
        Check(
            checks,
            "refit recovers split modules onto retained body",
            session.TryRepair(false) && ship.Unit == source && source.GetComponentsInChildren<Block>().Length == ship.Blueprint.blocks.Count && BlockGroupManager.GroupBlocks(source.GetComponentsInChildren<Block>().ToList()).Count == 1);
        CheckSingleBlock(checks);
        CheckEmptyRoot(checks);
        CheckCockpitlessSplit(checks);
        Directory.CreateDirectory("Temp/Siege");
        string json = JsonUtility.ToJson(new Report { checks = checks }, true);
        File.WriteAllText("Temp/Siege/regroup-checks.json", json);
        return json;
    }

    private static void CheckSingleBlock(List<string> checks)
    {
        GameObject root = Object.Instantiate(
            Resources.Load<GameObject>("BlocksParent/BlocksParent"),
            new Vector3(6000, 1500, 0),
            Quaternion.identity);
        try
        {
            Block block = Object.Instantiate(
                Resources.Load<GameObject>("Blocks/1x1x1"),
                root.transform.position,
                Quaternion.identity,
                root.transform).GetComponent<Block>();
            block.RefreshRuntimeData(true);
            ControlUnit unit = root.GetComponent<ControlUnit>();
            root.GetComponent<Rigidbody>().mass = 123f;
            unit.RefreshChildren();
            PlayManager.instance.RefreshGroup(unit);
            Check(
                checks,
                "single surviving module updates mass without replacing its body",
                block.GetComponentInParent<ControlUnit>() == unit && Mathf.Approximately(root.GetComponent<Rigidbody>().mass, block.mass));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CheckEmptyRoot(List<string> checks)
    {
        GameObject root = Object.Instantiate(
            Resources.Load<GameObject>("BlocksParent/BlocksParent"),
            new Vector3(6200, 1500, 0),
            Quaternion.identity);
        GameObject cockpit = null;
        try
        {
            cockpit = Object.Instantiate(
                Resources.Load<GameObject>("Blocks/Cockpit"),
                root.transform.position,
                Quaternion.identity,
                root.transform);
            ControlUnit unit = root.GetComponent<ControlUnit>();
            unit.RefreshChildren();
            bool initiallyValid = unit.HasValidCockpit;
            cockpit.transform.SetParent(null, true);
            PlayManager.instance.RefreshGroup(unit);
            Check(
                checks,
                "empty source clears stale cockpit authority immediately",
                initiallyValid && !unit.HasValidCockpit && !unit.HasAnyCockpit);
        }
        finally
        {
            Object.DestroyImmediate(cockpit);
            Object.DestroyImmediate(root);
        }
    }

    private static void CheckCockpitlessSplit(List<string> checks)
    {
        GameObject root = Object.Instantiate(
            Resources.Load<GameObject>("BlocksParent/BlocksParent"),
            new Vector3(6500, 1500, 0),
            Quaternion.identity);
        var blocks = new List<Block>();
        try
        {
            ControlUnit unit = root.GetComponent<ControlUnit>();
            unit.faction = UnitFaction.Enemy;
            for (int index = 0; index < 6; index++)
            {
                Block block = Object.Instantiate(
                    Resources.Load<GameObject>("Blocks/1x1x1"),
                    root.transform.position + Vector3.right * index,
                    Quaternion.identity,
                    root.transform).GetComponent<Block>();
                block.RefreshRuntimeData(true);
                blocks.Add(block);
            }

            unit.RefreshChildren();
            unit.AssignRuntimeOwnershipToBlocks(true);
            blocks[2].DisConnectAllConnectors(false);
            PlayManager.instance.RefreshGroup(unit);
            Check(
                checks,
                "cockpitless split retains largest component",
                root.GetComponentsInChildren<Block>().Length == 3 && blocks[5].GetComponentInParent<ControlUnit>() == unit);
            Check(
                checks,
                "new cockpitless fragments inherit source faction",
                blocks.All(block => block.GetComponentInParent<ControlUnit>().faction == UnitFaction.Enemy));
        }
        finally
        {
            var roots = new HashSet<GameObject>
            {
                root
            };
            foreach (Block block in blocks)
            {
                if (block != null)
                {
                    roots.Add(block.GetComponentInParent<ControlUnit>().gameObject);
                }
            }

            foreach (GameObject owned in roots)
            {
                Object.DestroyImmediate(owned);
            }
        }
    }

    private static bool PosesUnchanged(Dictionary<Block, Pose> poses)
    {
        return poses.All(pair => Vector3.Distance(pair.Key.transform.position, pair.Value.position) < 0.001f && Quaternion.Angle(pair.Key.transform.rotation, pair.Value.rotation) < 0.01f);
    }

    private static bool Near(Vector3 left, Vector3 right)
    {
        return Vector3.Distance(left, right) < 0.001f;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Regroup check failed: " + name);
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}
