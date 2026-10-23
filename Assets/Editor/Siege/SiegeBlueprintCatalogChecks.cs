// 职责：验证攻守蓝图目录隔离、坏文件容错、身份复制与源文件只读边界。
// 入口：Run()；使用Temp内独立目录，不读写用户存档。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SiegeBlueprintCatalogChecks
{
    public static string Run()
    {
        string root = Path.Combine("Temp", "Siege", "catalog-" + Guid.NewGuid().ToString("N"));
        string saves = Path.Combine(root, "Saves");
        string enemies = Path.Combine(root, "EnemyBlueprints");
        Directory.CreateDirectory(saves);
        Directory.CreateDirectory(enemies);
        string json = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch").text;
        File.WriteAllText(Path.Combine(saves, "Defender.json"), json);
        File.WriteAllText(Path.Combine(enemies, "Attacker.json"), json);
        File.WriteAllText(Path.Combine(saves, "Invalid.json"), "{invalid");
        File.WriteAllText(Path.Combine(saves, "Empty.json"), "{\"blocks\":[]}");
        var checks = new List<string>();
        SiegeShipOption[] defenders = SiegeBlueprintCatalog.Load(saves, 5, out string message);
        SiegeShipOption[] attackers = SiegeBlueprintCatalog.Load(enemies, 5, out _);
        Check(
            checks,
            "Saves excludes malformed and empty files",
            defenders.Length == 1 && message.Contains("skipped 2"));
        Check(
            checks,
            "defender and attacker catalogs stay separate",
            defenders[0].DisplayName == "Defender" && attackers.Length == 1 && attackers[0].DisplayName == "Attacker");
        BlockDataList first = SiegeFleet.PrepareBlueprint(defenders[0].BlueprintJson, out string firstId);
        BlockDataList second = SiegeFleet.PrepareBlueprint(defenders[0].BlueprintJson, out string secondId);
        Check(
            checks,
            "deploy snapshots have distinct identities",
            firstId != secondId && first.blocks[0].id != second.blocks[0].id);
        Check(checks, "price uses module count", defenders[0].Price == first.blocks.Count * 5);
        Check(
            checks,
            "source JSON remains byte-equivalent",
            File.ReadAllText(Path.Combine(saves, "Defender.json")) == json && defenders[0].BlueprintJson == json);
        Check(
            checks,
            "missing catalog is empty without creating files",
            SiegeBlueprintCatalog.Load(Path.Combine(root, "Absent"), 5, out _).Length == 0 && !Directory.Exists(Path.Combine(root, "Absent")));
        return string.Join("\n", checks);
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Catalog check failed: " + name);
        }

        checks.Add("PASS " + name);
    }
}
