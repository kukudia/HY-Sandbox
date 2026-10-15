// 职责：在真实Main游玩中隔离蓝图文件仓，验证返航写盘失败保留会话、成功保留几何和备份。
// 入口：Run()；同步操作仅限唯一Temp目录，finally恢复文件仓/名称/缓存，不覆盖用户存档。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CargoReturnFileChecks
{
    [Serializable]
    private sealed class Report
    {
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying || SaveManager.instance == null || !PlayManager.instance.playMode)
        {
            throw new InvalidOperationException("Requires Main in a real playing world session.");
        }

        var report = new Report();
        SaveManager saves = SaveManager.instance;
        FieldInfo files = typeof(SaveManager).GetField("_playerFiles", BindingFlags.Instance | BindingFlags.NonPublic);
        object previousFiles = files.GetValue(saves);
        string previousName = saves.currentSaveName;
        BlockDataList previousData = saves.cachedData;
        string source = File.ReadAllText(saves.GetSavePath(previousName));
        Transform player = GameManager.instance.blocksParent;
        WorldSessionManager session = PlayManager.instance.WorldSession;
        if (session == null || session.State != WorldSessionState.Playing || session.IsEntering)
        {
            throw new InvalidOperationException("World entry must finish before fault injection.");
        }

        string directory = Path.GetFullPath(Path.Combine("Temp", "CargoReturnFiles", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        Type store = typeof(SaveManager).Assembly.GetType("BlueprintFileStore", true);
        Action<string> useDirectory = path => files.SetValue(saves, Activator.CreateInstance(store, new object[] { path }));
        try
        {
            useDirectory(directory);
            saves.currentSaveName = "isolated-return";
            string path = saves.GetSavePath(saves.currentSaveName);
            ExpectRetained(report, "missing named blueprint refuses return and retains real session", player, session);
            File.WriteAllText(path, "{\"blocks\":[");
            ExpectRetained(report, "malformed blueprint refuses return without overwriting source", player, session);
            Check(report, "malformed file remains unchanged", File.ReadAllText(path) == "{\"blocks\":[");
            File.WriteAllText(path, source);
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                ExpectRetained(report, "locked blueprint refuses return and retains real session", player, session);
            }

            Check(report, "locked file remains unchanged", File.ReadAllText(path) == source);
            string blockedDirectory = Path.Combine(directory, "not-a-directory");
            File.WriteAllText(blockedDirectory, "blocking file");
            useDirectory(blockedDirectory);
            ExpectRetained(report, "file-path resolution failure is contained by return boundary", player, session);
            useDirectory(directory);
            Check(report, "valid isolated return writes successfully", CargoPersistence.SaveReturnCargo(true));
            Check(
                report,
                "atomic backup preserves original isolated blueprint",
                File.ReadAllText(path + ".cargo.bak") == source);
            BlockDataList returned = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            BlockDataList original = JsonUtility.FromJson<BlockDataList>(source);
            Check(report, "return preserves every original module and geometry", SameGeometry(original, returned));
            Check(report, "return does not leave temporary write file", !File.Exists(path + ".cargo.tmp"));
            ReturnStatusPanel status = MainUIPanels.instance.ReturnStatus;
            Check(
                report,
                "failure status is visible without blocking player controls",
                status != null && status.IsVisible && !status.GetComponent<CanvasGroup>().blocksRaycasts);
            status.Clear();
            Check(
                report,
                "status can clear without altering current session",
                !status.IsVisible && PlayManager.instance.playMode);
        }
        catch (Exception exception)
        {
            report.errors.Add(exception.ToString());
        }
        finally
        {
            files.SetValue(saves, previousFiles);
            saves.currentSaveName = previousName;
            saves.cachedData = previousData;
        }

        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Temp/cargo-return-files.json", json);
        return json;
    }

    private static void ExpectRetained(Report report, string label, Transform player, WorldSessionManager session)
    {
        int errors = 0;
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error && message.StartsWith("Cargo return could not be saved; current session kept: ", StringComparison.Ordinal))
            {
                errors++;
            }
        };
        Application.logMessageReceived += capture;
        try
        {
            MainUIPanels.instance.PlayEnd();
            ReturnStatusPanel status = MainUIPanels.instance.ReturnStatus;
            Check(
                report,
                label + " displays retry guidance",
                status != null && status.IsVisible && status.Message.Contains("retry Return") && status.Message.Contains("session is still open"));
            Check(
                report,
                label,
                errors == 1 && PlayManager.instance.playMode && GameManager.instance.blocksParent == player && session.State == WorldSessionState.Playing);
        }
        finally
        {
            Application.logMessageReceived -= capture;
        }
    }

    private static bool SameGeometry(BlockDataList before, BlockDataList after)
    {
        if (before.blocks.Count != after.blocks.Count)
        {
            return false;
        }

        for (int i = 0; i < before.blocks.Count; i++)
        {
            before.blocks[i].cargo = after.blocks[i].cargo;
        }

        return JsonUtility.ToJson(before) == JsonUtility.ToJson(after);
    }

    private static void Check(Report report, string label, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(label);
        }

        report.checks.Add(label);
    }
}
