// 职责：验证逻辑机库展开、预算拒绝、独立墙体损伤、快照缺口与原设计维修。
// 入口：Run()；只使用隔离建筑实例，不读写玩家存档。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class HangarShellChecks
{
    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public List<string> checks = new List<string>();
        public string error;
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Run the shell checks in Play Mode.");
        }

        var report = new Report();
        GameObject owner = null;
        try
        {
            Block prefab = Resources.Load<GameObject>("Hangars/Hangar").GetComponent<Block>();
            var logical = new BlockDataList
            {
                foundation = new BuildingFoundationData
                {
                    width = 24,
                    length = 28
                }
            };
            logical.blocks.Add(new BlockData(prefab) { id = "bay", posX = 0, posY = 5, posZ = 0, rotX = 0, rotY = 0, rotZ = 0, rotW = 1 });
            BlockData legacy = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(logical.blocks[0]));
            legacy.y = 9;
            legacy.posY = 4.5f;
            BlockData normalized = HangarShellBlueprint.Normalize(legacy);
            Check(
                report,
                "odd legacy height becomes even without lifting its floor",
                normalized.y == 10 && normalized.posY == 5 && legacy.y == 9);
            legacy.z = 21;
            legacy.posZ = 43;
            normalized = HangarShellBlueprint.Normalize(legacy);
            Check(
                report,
                "odd horizontal legacy bay remains aligned to foundation connectors",
                normalized.z == 22 && normalized.posZ == 43 && legacy.posZ == 43);
            string original = JsonUtility.ToJson(logical);
            BlockDataList design = HangarShellBlueprint.Expand(logical);
            Check(report, "expansion preserves the logical source", JsonUtility.ToJson(logical) == original);
            Check(report, "20x10x24 bay creates 372 ordinary wall blocks", design.blocks.Count == 373);
            Check(
                report,
                "wall blocks retain real 2x2x1 geometry and metal material",
                design.blocks.Skip(1).All(tile => tile.x == 2 && tile.y == 2 && tile.z == 1 && tile.materialId == "metal" && tile.hangarOwnerId == "bay"));
            Check(
                report,
                "expanded design is idempotent",
                ReferenceEquals(design, HangarShellBlueprint.Expand(design)));
            BlockDataList roofSource = JsonUtility.FromJson<BlockDataList>(original);
            roofSource.blocks[0].hangarEntrance = HangarEntrance.Roof;
            BlockDataList roof = HangarShellBlueprint.Expand(roofSource);
            Check(
                report,
                "roof layout count and price share the actual tiling",
                roof.blocks.Count == 1 + HangarShellBlueprint.TileCount(new Vector3Int(20, 10, 24), HangarEntrance.Roof) && HangarRules.ShellCost(roofSource.blocks[0]) > 0);
            Check(
                report,
                "roof expansion retains direction and does not edit the source",
                roof.blocks[0].hangarEntrance == HangarEntrance.Roof && roofSource.blocks.Count == 1);
            Check(
                report,
                "old JSON defaults to a front entrance",
                JsonUtility.FromJson<BlockData>("{}").hangarEntrance == HangarEntrance.Front);
            roofSource.blocks[0].hangarEntrance = (HangarEntrance)99;
            Check(
                report,
                "unsupported entrance enum is rejected before deployment",
                !HangarRules.Validate(roofSource, out _));
            BlockDataList excessive = JsonUtility.FromJson<BlockDataList>(original);
            excessive.blocks[0].x = 64;
            excessive.blocks[0].y = 64;
            excessive.blocks[0].z = 64;
            BlockDataList largeShell = HangarShellBlueprint.Expand(excessive);
            WorldBuildingValidation.ValidateBlueprint(largeShell);
            Check(
                report,
                "large valid shell expands beyond the former2048 cap",
                largeShell.blocks.Count > 2048 && largeShell.blocks.Count == 1 + HangarShellBlueprint.TileCount(new Vector3Int(64, 64, 64)));
            owner = new GameObject("Isolated modular hangar checks");
            owner.SetActive(false);
            WorldBuildingManager manager = owner.AddComponent<WorldBuildingManager>();
            var config = new SerializedObject(manager);
            config.FindProperty("initializeOnStart").boolValue = false;
            config.ApplyModifiedPropertiesWithoutUndo();
            BuildingRuntime building = manager.RestoreBuilding(new WorldBuildingState { instanceId = "shell-probe", blueprintName = "Shell probe", faction = UnitFaction.Player, position = new Vector3(12000, 2000, 12000), rotation = Quaternion.identity, blueprint = logical, durability = new[] { 1190f }, anchored = true });
            owner.SetActive(true);
            building.Activate();
            Check(
                report,
                "production restore expands every wall into building ownership",
                building.Blocks.Count == 373);
            Check(
                report,
                "legacy root damage survives shell migration",
                Mathf.Approximately(building.Blocks[0].GetComponent<Durability>().currentDurability, 1190f));
            Check(
                report,
                "combat has no duplicate preview colliders",
                !building.GetComponentInChildren<HangarModule>().transform.Find("Shell Preview (Editor)").gameObject.activeSelf);
            Check(report, "the complete shell has foundation support", building.HasFullFoundationSupport());
            HangarModule module = building.GetComponentInChildren<HangarModule>();
            Block controller = module.GetComponent<Block>();
            Check(
                report,
                "combat logical controller connects onto the real floor upper face",
                controller.connectors.All(connector => Mathf.Approximately(connector.localPos.y, -controller.y * .5f + 1f)));
            module.SetCombat(false);
            Check(
                report,
                "editing keeps the original underside placement connectors",
                controller.connectors.All(connector => Mathf.Approximately(connector.localPos.y, -controller.y * .5f)));
            module.SetCombat(true);
            Check(
                report,
                "switching floor mode invalidates the cached connection graph",
                !(bool)typeof(Block).GetProperty("HasValidRuntimeConnections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller));
            Check(
                report,
                "switching back to combat rebuilds real floor support",
                building.HasFullFoundationSupport());
            FieldInfo peer = typeof(Connector).GetField("runtimePeerBlock", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(
                report,
                "logical controller has a matched owned floor peer",
                controller.connectors.Any(connector => peer.GetValue(connector) is Block attached && attached.hangarOwnerId == "bay"));
            Block target = building.Blocks.Last();
            string removedId = target.uniqueId;
            DestroyManager.DestroyTarget(target.gameObject);
            Check(
                report,
                "one wall is independently destructible while the hangar survives",
                building.Blocks.Count == 372 && building.Blocks[0].GetComponent<HangarModule>().IsOperational);
            WorldBuildingState damaged = building.Capture();
            Check(
                report,
                "damaged capture preserves version and the missing tile",
                damaged.blueprint.hangarShellVersion == 1 && damaged.blueprint.blocks.Count == 372);
            Check(
                report,
                "loading a damaged snapshot never regenerates holes",
                HangarShellBlueprint.ExpandState(damaged).blueprint.blocks.Count == 372);
            object[] arguments =
            {
                design,
                null
            };
            bool repaired = (bool)typeof(BuildingRuntime).GetMethod("TryRefit", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(building, arguments);
            Check(
                report,
                "production refit restores the original wall identity",
                repaired && building.Blocks.Any(block => block.uniqueId == removedId));
            Check(report, "repaired shell returns to full original count", building.Blocks.Count == 373);
            BlockDataList elevated = JsonUtility.FromJson<BlockDataList>(original);
            elevated.blocks[0].posY += 6f;
            BlockData platform = JsonUtility.FromJson<BlockData>("{}");
            platform.id = "platform";
            platform.resourcePath = "Blocks/CombatStructures/Armor28x6x30";
            platform.x = 28;
            platform.y = 6;
            platform.z = 30;
            platform.posY = 3f;
            platform.rotW = 1f;
            elevated.blocks.Add(platform);
            elevated.foundation = BuildingFootprint.Calculate(elevated.blocks, 1);
            BuildingRuntime raised = manager.RestoreBuilding(new WorldBuildingState { instanceId = "raised-shell-probe", blueprintName = "Raised shell probe", faction = UnitFaction.Player, position = new Vector3(12200, 2000, 12000), rotation = Quaternion.identity, blueprint = elevated, durability = new[] { 1190f, float.MaxValue }, anchored = true });
            // RestoreBuilding stages an inactive root; explicit activation makes this
            // assertion independent of the manager's isolated hierarchy enable state.
            raised.Activate();
            Check(
                report,
                "elevated modular bay remains supported through platform then real floor",
                raised.HasFullFoundationSupport());
            report.passed = true;
        }
        catch (Exception error)
        {
            report.error = error.ToString();
        }
        finally
        {
            if (owner != null)
            {
                owner.SetActive(false);
                Object.Destroy(owner);
            }
        }

        Directory.CreateDirectory("Temp/Hangars");
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Temp/Hangars/shell-checks.json", json);
        return json;
    }

    private static void Check(Report report, string name, bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add(name);
    }
}
