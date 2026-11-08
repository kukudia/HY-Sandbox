// 职责：在真实Main Play Mode验证副场景切换、取消/重试、建造原稿隔离和旧Siege不被启动。
// 入口：Run()；只修改运行时夹具，报告写Temp/GameModes，不写玩家存档或场景。
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
public static class GameModePlayChecks
{
    private const string Pending = "HY.GameModePlayChecks";
    private const string ReportPath = "Temp/GameModes/play-checks.json";
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

    static GameModePlayChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
    }

    [MenuItem("Tools/HY-Sandbox/Modes/Validate Main mode transitions")]
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

        GameModeController modes = PlayManager.instance.Modes;
        Check(modes != null && modes.State == GameModeState.Building, "Main owns mode entry");
        Check(
            Object.FindObjectsByType<WorldSessionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
            "Main does not prewarm exploration");
        Transform loaded = GameManager.instance.blocksParent;
        if (loaded != null)
        {
            loaded.gameObject.SetActive(false);
            PlayManager.instance.allControlUnits.Remove(loaded.GetComponent<ControlUnit>());
        }

        GameObject draft = Object.Instantiate(Resources.Load<GameObject>("BlocksParent/BlocksParent"));
        draft.name = "Unsaved mode-check draft";
        GameObject cockpit = Object.Instantiate(Resources.Load<GameObject>("Blocks/Cockpit"), draft.transform);
        cockpit.name = "Unsaved cockpit marker";
        GameObject cargo = Object.Instantiate(Resources.Load<GameObject>("Blocks/CargoHold"), draft.transform);
        cargo.transform.localPosition = Vector3.right * 2f;
        CargoHold originalHold = cargo.GetComponent<CargoHold>();
        GameManager.instance.blocksParent = draft.transform;
        PlayManager.instance.blocksParent = draft.transform;
        string directory = Path.Combine(Application.temporaryCachePath, "ModeReturnChecks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        typeof(SaveManager).GetField("_playerFiles", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
            SaveManager.instance,
            Activator.CreateInstance(typeof(SaveManager).Assembly.GetType("BlueprintFileStore", true), new object[] { directory }));
        SaveManager.instance.currentSaveName = "isolated-mode-return";
        var saved = new BlockDataList();
        saved.blocks.Add(new BlockData(cockpit.GetComponent<Block>()));
        saved.blocks.Add(new BlockData(cargo.GetComponent<Block>()));
        string savePath = SaveManager.instance.GetSavePath(SaveManager.instance.currentSaveName);
        File.WriteAllText(savePath, JsonUtility.ToJson(saved));
        yield return null;
        Vector3 cameraPosition = CameraController.instance.transform.position;
        MainUIPanels.instance.PlayStart();
        Check(modes.State == GameModeState.Choosing, "Simulate opens selection");
        modes.ReturnToBuild();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        Check(
            GameManager.instance.blocksParent == draft.transform && draft.activeSelf,
            "closing selection preserves unsaved original");
        modes.ShowSelection();
        modes.SelectExploration();
        modes.SelectSiege();
        modes.ReturnToBuild();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        Check(
            SceneManager.sceneCount == 1 && PlayManager.instance.WorldSession == null,
            "cancel during scene load fully unloads and unbinds");
        for (int cycle = 0; cycle < 2; cycle++)
        {
            modes.ShowSelection();
            modes.SelectSiege();
            while (modes.State == GameModeState.Loading)
            {
                yield return null;
            }

            Check(modes.State == GameModeState.Siege, "new Siege entered from Main, cycle " + cycle);
            Check(
                Object.FindObjectsByType<SiegeSession>(FindObjectsSortMode.None).Length == 0,
                "legacy Siege session stays unloaded");
            Check(
                !draft.activeSelf && SceneManager.sceneCount == 2,
                "Siege parks original and loads exactly one scene");
            Check(
                Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1,
                "one shared EventSystem");
            Check(
                Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled) == 1,
                "one shared active camera");
            modes.ReturnToBuild();
            while (modes.State != GameModeState.Building)
            {
                yield return null;
            }

            Check(
                GameManager.instance.blocksParent == draft.transform && draft.activeSelf,
                "Siege return restores original object");
            Check(
                Vector3.Distance(CameraController.instance.transform.position, cameraPosition) < .001f,
                "Siege return restores build camera");
        }

        // Invalid configuration must offer retry without retaining a scene; changes are runtime-only.
        var configuration = new SerializedObject(modes);
        configuration.FindProperty("explorationScene").stringValue = "Assets/Scenes/MissingModeCheck.unity";
        configuration.ApplyModifiedPropertiesWithoutUndo();
        modes.ShowSelection();
        modes.SelectExploration();
        while (modes.State != GameModeState.Failed)
        {
            yield return null;
        }

        Check(SceneManager.sceneCount == 1 && draft.activeSelf, "failed load restores build and offers retry");
        configuration.Update();
        configuration.FindProperty("explorationScene").stringValue = GameModeAuthoring.ExplorationPath;
        configuration.ApplyModifiedPropertiesWithoutUndo();
        modes.Retry();
        while (modes.State == GameModeState.Loading)
        {
            yield return null;
        }

        Check(
            modes.State == GameModeState.Exploring,
            "retry enters exploration through preparation and activation");
        Transform runtime = GameManager.instance.blocksParent;
        Check(runtime != draft.transform && !draft.activeSelf, "exploration uses runtime copy");
        Check(
            runtime.gameObject.scene.path == GameModeAuthoring.ExplorationPath,
            "runtime ship belongs to exploration scene");
        Check(
            SceneManager.GetActiveScene().path == GameModeAuthoring.MainPath,
            "Main stays active for shared effect pools");
        Check(
            PlayManager.instance.WorldSession.World.LoadedColumns.Count > 0,
            "real world generated and enabled");
        Check(
            SceneObjectQuery.FindInScene<TurretProjectilePool>(runtime.gameObject.scene) != null,
            "exploration owns authored projectile pool");
        VfxEffectPool effects = SceneObjectQuery.FindInScene<VfxEffectPool>(runtime.gameObject.scene);
        Check(effects != null, "exploration owns authored detached smoke pool");
        VfxEffect smoke = VfxEffectPool.Rent(
            BlockVfxLibrary.Instance.DetachedSmoke,
            runtime.position,
            Quaternion.identity,
            runtime.gameObject.scene);
        Check(smoke != null && effects.ActiveCount == 1, "detached smoke can rent from real exploration scene");
        runtime.GetComponentInChildren<CargoHold>().RestoreContents(new List<CargoItem> { new CargoItem { kind = CargoKind.SpecialPart, resourcePath = "Blocks/UniversalThruster", amount = 1 } });
        string originalFile = File.ReadAllText(savePath);
        File.Delete(savePath);
        // This deliberate file failure is asserted below; other unexpected engine errors remain captured.
        Application.logMessageReceived -= LogError;
        MainUIPanels.instance.PlayEnd();
        Application.logMessageReceived += LogError;
        Check(
            modes.State == GameModeState.Exploring && PlayManager.instance.playMode && SceneManager.sceneCount == 2,
            "failed cargo save keeps runtime session alive");
        File.WriteAllText(savePath, originalFile);
        MainUIPanels.instance.PlayEnd();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        Check(
            GameManager.instance.blocksParent == draft.transform && cockpit != null && cockpit.name == "Unsaved cockpit marker",
            "return preserves unsaved draft identity and geometry");
        Check(
            originalHold.Used == 1 && JsonUtility.FromJson<BlockDataList>(File.ReadAllText(savePath)).blocks.Single(b => b.id == cargo.GetComponent<Block>().uniqueId).cargo.Count == 1,
            "normal return migrates cargo to original and isolated file");
        Check(
            SceneManager.sceneCount == 1 && PlayManager.instance.WorldSession == null,
            "exploration return unloads and unbinds");
        Check(
            BuildManager.instance.enabled && InputManager.instance.keyInput && Time.timeScale == 1f,
            "return restores input, building and time scale");
        modes.ShowSelection();
        modes.SelectExploration();
        while (PlayManager.instance.WorldSession == null)
        {
            yield return null;
        }

        PlayManager.instance.WorldSession.CancelPlayRequest();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        Check(
            SceneManager.sceneCount == 1 && draft.activeSelf,
            "cancel during world preparation clears pending work");
        modes.ShowSelection();
        modes.SelectExploration();
        while (modes.State == GameModeState.Loading)
        {
            yield return null;
        }

        Check(
            modes.State == GameModeState.Exploring && GameManager.instance.blocksParent.GetComponentInChildren<CargoHold>().Used == 1,
            "second departure keeps returned cargo");
        DestroyManager.instance.DestroyGameObject(GameManager.instance.blocksParent.GetComponentInChildren<Cockpit>().gameObject);
        yield return null;
        Check(
            !PlayManager.instance.playMode && MainUIPanels.instance.deathPanel.activeSelf,
            "real cockpit death opens death interface");
        MainUIButtons.instance.respawnButton.onClick.Invoke();
        while (modes.State != GameModeState.Building)
        {
            yield return null;
        }

        Check(
            GameManager.instance.blocksParent == draft.transform && cockpit != null && originalHold.Used == 0,
            "death returns original geometry and clears cargo");
        Check(
            JsonUtility.FromJson<BlockDataList>(File.ReadAllText(savePath)).blocks.All(b => b.cargo == null || b.cargo.Count == 0),
            "death clears persisted cargo in isolated file");
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
        Directory.CreateDirectory("Temp/GameModes");
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log("Game mode checks: " + report.status + " / " + report.checks.Count);
        EditorApplication.isPlaying = false;
    }
}
