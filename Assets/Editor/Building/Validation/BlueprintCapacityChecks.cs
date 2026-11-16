// 职责：验证解除建筑模块硬上限后仍保留身份/数值校验及损坏蓝图审计边界。
// 入口：Run()；纯数据和Temp隔离文件，不加载大型实体、不访问个人蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlueprintCapacityChecks
{
    public static string Run()
    {
        var checks = new List<string>();
        var blueprint = new BlockDataList
        {
            foundation = new BuildingFoundationData()
        };
        for (int i = 0; i < 9001; i++)
        {
            BlockData block = JsonUtility.FromJson<BlockData>("{}");
            block.id = "capacity-" + i;
            block.resourcePath = "Blocks/2x2x1";
            block.x = 2;
            block.y = 2;
            block.z = 1;
            block.posX = i % 95 * 2;
            block.posY = 1;
            block.posZ = i / 95;
            block.rotW = 1;
            blueprint.blocks.Add(block);
        }

        WorldBuildingValidation.ValidateBlueprint(blueprint);
        checks.Add("9001 valid building modules pass without former2048/8192 caps");
        blueprint.blocks[1].id = blueprint.blocks[0].id;
        bool refused = false;
        try
        {
            WorldBuildingValidation.ValidateBlueprint(blueprint);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }

        Require(refused, "duplicate identities remain rejected", checks);
        blueprint.blocks[1].id = "capacity-1";
        GameObject root = new GameObject("Inactive capacity check");
        root.SetActive(false);
        try
        {
            WorldBuildingManager manager = root.AddComponent<WorldBuildingManager>();
            Require(
                manager.HasCapacity(9001) && manager.CanAddBlocks(int.MaxValue),
                "manager does not silently replace removed caps",
                checks);
            Require(!manager.CanAddBlocks(-1), "invalid negative capacity request remains rejected", checks);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        string directory = Path.GetFullPath(Path.Combine("Temp", "BlueprintAuditChecks", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        string malformed = Path.Combine(directory, "malformed.json");
        File.WriteAllText(malformed, "not json");
        Require(
            BlueprintIntegrityAudit.Inspect(malformed, "Saves").status == "invalid",
            "malformed JSON is diagnosed",
            checks);
        string draft = Path.Combine(directory, "draft.json");
        File.WriteAllText(draft, "{\"blocks\":[]}");
        Require(
            BlueprintIntegrityAudit.Inspect(draft, "BaseBlueprints").status == "draft",
            "empty editable draft is retained",
            checks);
        blueprint.blocks.RemoveRange(1, blueprint.blocks.Count - 1);
        string valid = Path.Combine(directory, "valid.json");
        File.WriteAllText(valid, JsonUtility.ToJson(blueprint));
        Require(
            BlueprintIntegrityAudit.Inspect(valid, "BaseBlueprints").status == "draft",
            "unfinished core designation is retained as draft",
            checks);
        blueprint.blocks[0].resourcePath = "Blocks/does-not-exist";
        File.WriteAllText(valid, JsonUtility.ToJson(blueprint));
        Require(
            BlueprintIntegrityAudit.Inspect(valid, "Saves").status == "invalid",
            "missing resource is diagnosed before loading",
            checks);
        string report = "PASS\n" + string.Join("\n", checks);
        File.WriteAllText(Path.Combine(directory, "checks.txt"), report);
        return report;
    }

    private static void Require(bool value, string name, List<string> checks)
    {
        if (!value)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}
