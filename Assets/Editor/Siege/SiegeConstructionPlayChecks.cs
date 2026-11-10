// 职责：在真实Main Play Mode验证副场景切换、取消/重试、建造原稿隔离和旧Siege不被启动。
// 入口：Run()；只修改运行时夹具，报告写Temp/BaseConstruction，不写玩家存档或场景。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SiegeConstructionPlayChecks
{
    private const string Pending = "HY.SiegeConstructionPlayChecks";
    private const string ReportPath = "Temp/BaseConstruction/play-checks.json";
    private static IEnumerator scenario;
    private static Report report;
    private static double deadline;
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    static SiegeConstructionPlayChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
    }

    [MenuItem("Tools/HY-Sandbox/Siege/Validate base construction")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != GameModeAuthoring.MainPath || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Open saved Main in Edit Mode before running mode checks.");
        }

        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            report = new Report
            {
                status = "running"
            };
            scenario = Scenario();
            deadline = EditorApplication.timeSinceStartup + 240d;
            Application.logMessageReceived += LogError;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && scenario != null)
        {
            report.errors.Add("Play Mode exited before completion.");
            Finish();
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Mode checks exceeded 240 seconds.");
            }

            if (!scenario.MoveNext())
            {
                Finish();
            }
        }
        catch (Exception error)
        {
            report.errors.Add(error.ToString());
            Finish();
        }
    }

    private static IEnumerator Scenario()
    {
        yield return null;
        while (BuildManager.instance.IsLoadingBlocks)
        {
            yield return null;
        }

        var settings = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>(SiegeConstructionAuthoring.SettingsPath);
        BlockDataList original = JsonUtility.FromJson<BlockDataList>(settings.Starter.text);
        BlockDataList Copy() => JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(original));
        Check(settings.Validate(original, out _), "Starter passes design validation");
        var invalid = Copy();
        invalid.siegeCoreId = null;
        Check(!settings.Validate(invalid, out _), "Missing core blocked");
        invalid = Copy();
        invalid.blocks[1].id = invalid.blocks[0].id;
        Check(!settings.Validate(invalid, out _), "Duplicate identities blocked");
        invalid = Copy();
        invalid.blocks[0].posY = -4;
        Check(settings.Validate(invalid, out _), "Design validation does not enforce ground-plane layout");
        invalid = Copy();
        invalid.foundation.width = 64;
        Check(settings.Validate(invalid, out _), "Foundation size no longer blocks deployment");
        invalid = Copy();
        invalid.blocks[0].y = 64;
        Check(settings.Validate(invalid, out _), "Height and budget no longer block deployment");
        invalid = Copy();
        invalid.blocks[1].posY = invalid.blocks[0].posY;
        Check(settings.Validate(invalid, out _), "Internal layout no longer blocks deployment");
        invalid = Copy();
        invalid.blocks[0].rotY = .2f;
        Check(!settings.Validate(invalid, out _), "Invalid rotation blocked");
        // Inject a disposable file store rather than writing into the user's blueprint library.
        string folder = Path.GetFullPath("Temp/BaseConstruction/Library-" + Guid.NewGuid().ToString("N"));
        var storeField = typeof(SaveManager).GetField("_baseFiles", BindingFlags.Instance | BindingFlags.NonPublic);
        storeField.SetValue(SaveManager.instance, Activator.CreateInstance(storeField.FieldType, new object[] { folder }));
        var small = Copy();
        small.blocks = small.blocks.Take(2).ToList();
        small.foundation.width = small.foundation.length = 4;
        small.siegeCoreId = small.blocks[0].id;
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "Probe.json");
        File.WriteAllText(path, JsonUtility.ToJson(small, true));
        BuildManager.instance.BlockLoadIntervalSeconds = 0;
        BuildManager.instance.EnterBaseBlueprintBuildMode("Probe");
        while (BuildManager.instance.IsLoadingBlocks)
        {
            yield return null;
        }

        Check(BuildManager.instance.HasBuildingBlueprint, "Main opens building editor");
        BuildManager.instance.selectedBlock = GameManager.instance.blocksParent.GetComponentsInChildren<Block>().Single(b => b.uniqueId == small.blocks[1].id);
        Object.FindFirstObjectByType<SiegeBaseWorkshop>().DesignateCore();
        Check(
            SaveManager.instance.cachedData.siegeCoreId == small.blocks[1].id,
            "Selected real module becomes unique core");
        Check(
            SaveManager.instance.cachedData.foundation != null && SaveManager.instance.cachedData.foundation.IsValid,
            "Automatic footprint available in normal construction workflow");
        Check(
            JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path)).siegeCoreId == small.blocks[1].id,
            "Automatic footprint preserves core metadata on disk");
        BuildManager.instance.ToggleBaseBlueprintBuildMode();
        while (BuildManager.instance.IsLoadingBlocks)
        {
            yield return null;
        }

        var airborne = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
        foreach (BlockData block in airborne.blocks)
        {
            block.posY += 10;
        }

        File.WriteAllText(Path.Combine(folder, "Airborne.json"), JsonUtility.ToJson(airborne, true));
        string designBefore = File.ReadAllText(path);
        Transform draft = GameManager.instance.blocksParent;
        var modes = PlayManager.instance.Modes;
        modes.LaunchSiege("Airborne");
        while (modes.State == GameModeState.Loading || modes.State == GameModeState.Returning)
        {
            yield return null;
        }

        Check(
            modes.State == GameModeState.Failed && draft.gameObject.activeSelf && SceneManager.sceneCount == 1,
            "Unsupported preselected base rejects entry and restores the draft");
        modes.ReturnToBuild();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        modes.LaunchSiege("Missing Base");
        while (modes.State == GameModeState.Loading || modes.State == GameModeState.Returning)
        {
            yield return null;
        }

        Check(
            modes.State == GameModeState.Failed && Object.FindFirstObjectByType<SiegeGameplayStage>() == null,
            "Missing preselected base never silently falls back to the starter");
        modes.Retry();
        while (modes.State == GameModeState.Loading || modes.State == GameModeState.Returning)
        {
            yield return null;
        }

        Check(
            modes.State == GameModeState.Failed && modes.Status.Contains("Missing Base"),
            "Retry keeps the originally selected base instead of launching a different design");
        modes.ReturnToBuild();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        modes.ShowSelection();
        modes.SelectSiege();
        while (modes.State == GameModeState.Loading)
        {
            yield return null;
        }

        Check(modes.State == GameModeState.Siege, "Main opens authored additive Siege stage: " + modes.Status);
        var stage = Object.FindFirstObjectByType<SiegeGameplayStage>();
        Check(
            stage != null && Object.FindFirstObjectByType<SiegeSession>() == null,
            "New stage has no prototype session");
        Check(!draft.gameObject.activeSelf, "Main construction parked before deployment");
        Check(
            stage.Base != null && stage.Base.IsAlive,
            "Starter automatically deploys with real foundation support");
        Check(
            stage.GetComponentsInChildren<UnityEngine.UI.Dropdown>(true).Length == 0 && stage.GetComponentsInChildren<UnityEngine.UI.InputField>(true).Length == 0,
            "Combat UI has no base selection or pose inputs");
        Transform spawn = stage.transform.Find("Base Spawn");
        Vector3 foundationCentre = stage.Base.Building.transform.TransformPoint(BuildingFootprint.Calculate(original.blocks, 1).Center);
        Check(
            Vector2.Distance(
                new Vector2(foundationCentre.x, foundationCentre.z),
                new Vector2(spawn.position.x, spawn.position.z)) < .01f,
            "Automatic base footprint is centred on the authored spawn");
        Directory.CreateDirectory("Temp/BaseConstruction");
        ScreenCapture.CaptureScreenshot("Temp/BaseConstruction/automatic-spawn.png");
        Check(
            stage.Base.Building.Blocks.Count == original.blocks.Count && stage.Base.IsAlive,
            "Real building and non-cockpit core created");
        Check(
            stage.Base.Building.gameObject.scene == stage.gameObject.scene,
            "Building belongs to gameplay scene");
        Check(stage.Base.Core.GetComponent<Cockpit>() == null, "Core is independent of cockpit");
        Check(!ReferenceEquals(stage.Base.Blueprint, original), "Combat owns isolated blueprint snapshot");
        yield return null;
        Block damaged = stage.Base.Building.Blocks.First(b => b != stage.Base.Core && b.GetComponent<Durability>() != null);
        var health = damaged.GetComponent<Durability>();
        health.currentDurability -= 10;
        long price = stage.Base.Quote(), coins = stage.Match.Coins;
        var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstruction.transform.position = damaged.transform.position;
        Check(
            !stage.Repair() && stage.Match.Coins == coins && health.currentDurability < health.maxDurability,
            "Obstructed refit preserves funds and live damage");
        obstruction.SetActive(false);
        Object.Destroy(obstruction);
        stage.Match.TrySpend(coins);
        Check(!stage.Repair() && stage.Match.Coins == 0, "Insufficient repair funds leave state unchanged");
        typeof(SiegeMatch).GetProperty("Coins").SetValue(stage.Match, coins);
        Check(
            price > 0 && stage.Repair() && stage.Match.Coins == coins - price,
            "Repair charges exact quote after success");
        Check(health.currentDurability == health.maxDurability, "Surviving module durability restored");
        Block removed = stage.Base.Building.Blocks.Last(b => b != stage.Base.Core);
        string removedId = removed.uniqueId;
        stage.Base.Building.DestroyBlock(removed);
        yield return null;
        Check(
            stage.Base.IsAlive && stage.Base.Quote() > 0,
            "Non-core destruction preserves match and creates replacement quote");
        Check(stage.Repair(), "Missing modules rebuilt from original design: " + stage.Status);
        Check(
            stage.Base.Building.Blocks.Any(b => b != null && b.uniqueId == removedId && b.gameObject.activeSelf),
            "Rebuilt module keeps its design identity");
        Check(stage.BeginWave(), "Real wave starts");
        Check(!stage.Repair(), "Battle repairs blocked");
        while (stage.Match.ActiveAttackers == 0 && stage.Match.Phase == SiegePhase.Battle)
        {
            yield return null;
        }

        Check(stage.Match.ActiveAttackers > 0, "Real enemy spawned and registered");
        var spawner = SceneObjectQuery.FindInScene<EnemySpawner>(draft.gameObject.scene);
        Check(
            !(bool)typeof(EnemySpawner).GetProperty("IsAutomaticSpawningAllowed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner),
            "Main automatic spawner cannot contaminate Siege waves");
        Check(
            ControlUnit.RuntimeUnits.Any(u => u != null && u.faction == UnitFaction.Enemy && u.gameObject.scene == stage.gameObject.scene),
            "Enemy scene ownership matches base");
        while (stage.Match.Phase == SiegePhase.Battle)
        {
            foreach (Cockpit cockpit in Object.FindObjectsByType<Cockpit>())
            {
                if (cockpit.faction == UnitFaction.Enemy && cockpit.gameObject.scene == stage.gameObject.scene)
                {
                    DestroyManager.instance.DestroyGameObject(cockpit.gameObject);
                }
            }

            yield return null;
        }

        Check(
            stage.Match.Phase == SiegePhase.Intermission && stage.Match.DefeatedAttackers > 0,
            "Real enemy deaths clear wave and award rewards");
        Check(!PlayManager.instance.playMode, "Intermission suspends simulation");
        stage.Base.Building.DestroyBlock(stage.Base.Core);
        yield return null;
        Check(stage.Settled && stage.Match.Phase == SiegePhase.Defeat, "Core destruction settles immediately");
        Check(!stage.BeginWave() && !stage.Repair(), "Defeat prevents further combat/refit");
        modes.ReturnToBuild();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        Check(
            draft != null && draft.gameObject.activeSelf && GameManager.instance.blocksParent == draft,
            "Return restores original Main construction");
        Check(
            SceneManager.sceneCount == 1 && !PlayManager.instance.playMode,
            "Gameplay scene and battle state cleaned up");
        Check(File.ReadAllText(path) == designBefore, "Combat and return never mutate saved base design");
        modes.ShowSelection();
        var modePanel = Object.FindFirstObjectByType<GameModePanel>();
        var picker = (UnityEngine.UI.Dropdown)new SerializedObject(modePanel).FindProperty("baseDesigns").objectReferenceValue;
        Check(
            picker != null && picker.gameObject.activeInHierarchy && picker.options.Any(option => option.text == "Probe"),
            "Simulate offers the independent base library before entering Siege");
        picker.value = picker.options.FindIndex(option => option.text == "Probe");
        modes.SelectSiege();
        while (modes.State == GameModeState.Loading)
        {
            yield return null;
        }

        stage = Object.FindFirstObjectByType<SiegeGameplayStage>();
        Check(stage.Base != null && stage.Base.IsAlive, "Player-authored core blueprint automatically deploys");
        stage.Retreat();
        Check(stage.Settled && stage.Status.Contains("Retreated"), "Voluntary retreat displays settlement");
        modes.ReturnToBuild();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        Check(File.ReadAllText(path) == designBefore, "Repeated entry preserves design and original draft");
    }

    private static void Check(bool success, string description)
    {
        if (!success)
        {
            throw new InvalidOperationException(description);
        }

        report.checks.Add(description);
    }

    private static void LogError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            report.errors.Add(message + "\n" + stack);
        }
    }

    private static void Finish()
    {
        Application.logMessageReceived -= LogError;
        EditorApplication.update -= Tick;
        (scenario as IDisposable)?.Dispose();
        scenario = null;
        SessionState.SetBool(Pending, false);
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Temp/BaseConstruction");
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log("Base construction checks: " + report.status + " / " + report.checks.Count);
        EditorApplication.isPlaying = false;
    }
}
