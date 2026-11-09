// 职责：在临时目录验证基地库迁移的冲突保护、备份和重复执行，不触碰玩家文件。
// 入口：Run()；验证生产迁移入口，而非复制迁移算法。
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

public static class BaseLibraryChecks
{
    [MenuItem("Tools/Building Editor/Validate base library migration")]
    public static string Run()
    {
        string root = Path.GetFullPath("Temp/BaseMigration/" + Guid.NewGuid().ToString("N"));
        string buildings = Path.Combine(root, "Buildings");
        string bases = Path.Combine(root, "Bases");
        Directory.CreateDirectory(buildings);
        Directory.CreateDirectory(bases);
        const string original = "{\"blocks\":[],\"siegeCoreId\":\"core\"}";
        const string collision = "{\"blocks\":[],\"siegeCoreId\":\"another-core\"}";
        File.WriteAllText(Path.Combine(buildings, "Base.json"), original);
        File.WriteAllText(Path.Combine(bases, "Base.json"), collision);
        File.WriteAllText(Path.Combine(buildings, "Ordinary.json"), "{\"blocks\":[]}");
        File.WriteAllText(
            Path.Combine(buildings, "Outpost.json"),
            "{\"siegeCoreId\":\"guard\",\"outpost\":{\"enabled\":true}}");
        MethodInfo migrate = typeof(SaveManager).Assembly.GetType("BaseBlueprintMigration").GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
        migrate.Invoke(null, new object[] { buildings, bases });
        Require(File.ReadAllText(Path.Combine(bases, "Base.json")) == collision, "Existing base was overwritten.");
        Require(
            File.ReadAllText(Path.Combine(bases, "Base (migrated 1).json")) == original,
            "Conflicting base was not migrated intact.");
        Require(
            !File.Exists(Path.Combine(buildings, "Base.json")),
            "Base remains mixed into the building library.");
        Require(
            File.Exists(Path.Combine(buildings, "Ordinary.json")) && File.Exists(Path.Combine(buildings, "Outpost.json")),
            "General buildings or outposts were migrated.");
        Require(
            Directory.GetFiles(Path.Combine(root, "BlueprintMigrationBackups")).Any(p => File.ReadAllText(p) == original),
            "Original bytes were not archived.");
        // Recreate the source to model interruption after copy, before archive/removal.
        File.WriteAllText(Path.Combine(buildings, "Base.json"), original);
        migrate.Invoke(null, new object[] { buildings, bases });
        migrate.Invoke(null, new object[] { buildings, bases });
        Require(
            Directory.GetFiles(bases).Length == 2 && !File.Exists(Path.Combine(buildings, "Base.json")),
            "Repeated migration duplicated or lost a design.");
        return "Passed 6 migration checks: collision, byte preservation, separate library, ordinary/outpost exclusion, backup, restart safety.";
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition)
        {
            throw new InvalidOperationException(reason);
        }
    }
}
