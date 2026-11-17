// 职责：真实Play验证冷启动、菜单导航、临时载具隔离、基地选择和保存失败保护。
// 入口：Run()；存档注入临时目录，偏好备份恢复，报告及截图仅写Temp/MainMenu。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MainMenuPlayChecks
{
    private const string Pending = "HY.MainMenuPlayChecks";
    private static IEnumerator scenario;
    private static Report report;
    private static double deadline;
    private static bool expectedCargoError;
    private static readonly Dictionary<string, string> preferences = new Dictionary<string, string>();
    private sealed class HistoryProbe : IBlockAction
    {
        public string ActionName => "MenuHistoryProbe";
        public bool Undone
        {
            get; private set;
        }

        public void Undo()
        {
            Undone = true;
        }

        public void Redo()
        {
            Undone = false;
        }
    }

    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    static MainMenuPlayChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
    }

    [MenuItem("Tools/HY-Sandbox/Main Menu/Validate menu navigation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != WorkshopNavigation.MenuPath || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Open only saved MainMenu in Edit Mode before validation.");
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
            deadline = EditorApplication.timeSinceStartup + 240;
            Application.logMessageReceived += LogError;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && scenario != null)
        {
            report.errors.Add("Play exited before completion.");
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
                throw new TimeoutException("Menu checks exceeded 240 seconds.");
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
        BackupPreferences();
        MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
        Check(menu != null && menu.MenuVisible && !menu.Busy, "cold menu is interactive");
        Check(
            SceneManager.sceneCount == 1 && GameManager.instance == null,
            "cold menu does not load Main or a default save");
        Check(
            Object.FindObjectsByType<WorldSessionManager>(FindObjectsInactive.Include).Length == 0,
            "cold menu does not generate a world");
        Check(
            EditorBuildSettings.scenes.First(scene => scene.enabled).path == WorkshopNavigation.MenuPath,
            "MainMenu is the Player startup scene");
        Check(ActiveEvents() == 1, "cold menu has one EventSystem");
        menu.Quit();
        Check(EditorApplication.isPlaying && menu.Status.Contains("disabled"), "Editor quit is a safe message");
        Directory.CreateDirectory("Temp/MainMenu");
        ScreenCapture.CaptureScreenshot("Temp/MainMenu/home.png");
        menu.ShowBattleAttract();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            menu.AttractVisible && SceneManager.sceneCount == 2 && GameManager.instance == null,
            "cold attract does not initialize the workshop or saves");
        float coldUntil = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < coldUntil)
        {
            yield return null;
        }

        using (var escape = (IDisposable)EscapeAttract(menu))
        {
            while (((IEnumerator)escape).MoveNext())
            {
                yield return null;
            }
        }

        Check(
            menu.MenuVisible && SceneManager.sceneCount == 1 && PlayManager.instance == null && DestroyManager.instance == null && DebugManager.instance == null && VisualEffectsManager.instance == null,
            "cold attract ESC releases standalone services");
        menu.ShowBattleAttract();
        menu.CancelNavigation();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            menu.MenuVisible && !menu.AttractVisible && SceneManager.sceneCount == 1,
            "cancelled attract load returns to the cold menu");
        Check(
            ActiveEvents() == 1 && ActiveScreenCameras() == 1,
            "cold attract cancellation keeps one menu camera and EventSystem");
        menu.Choose(MenuDestination.Siege);
        Dropdown picker = (Dropdown)new SerializedObject(Object.FindAnyObjectByType<MainMenuView>()).FindProperty("blueprints").objectReferenceValue;
        yield return null;
        yield return null;
        picker.Show();
        for (int frame = 0; frame < 6; frame++)
        {
            yield return null;
        }

        Text option = picker.GetComponentsInChildren<Text>().First(t => t.name == "Item Label");
        Check(
            option.rectTransform.rect.height >= option.fontSize + 4 && option.cachedTextGenerator.vertexCount > 4,
            "expanded dropdown renders visible unclipped option text");
        ScreenCapture.CaptureScreenshot("Temp/MainMenu/dropdown-fixed.png");
        yield return null;
        picker.Hide();
        // Exercise the saved button binding, not only the controller API.
        menu.ShowHome();
        Button siegeButton = (Button)new SerializedObject(Object.FindAnyObjectByType<MainMenuView>()).FindProperty("siege").objectReferenceValue;
        siegeButton.onClick.Invoke();
        Check(menu.Selected.Building, "authored Siege button opens the base picker");
        Check(
            menu.Selected.IsExample && menu.Selected.Building,
            "Siege picker offers read-only starter without Main");
        menu.LaunchSelected();
        menu.LaunchSelected();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            PlayManager.instance.Modes.State == GameModeState.Siege && SceneManager.sceneCount == 3,
            "cold menu launches exactly one Siege scene");
        Check(GameManager.instance.blocksParent == null, "direct Siege does not load an unrelated vehicle");
        Check(Object.FindObjectsByType<SiegeSession>().Length == 0, "legacy Siege stays unloaded");
        Check(
            ActiveEvents() == 1 && ActiveScreenCameras() == 1,
            "Siege has one EventSystem and one screen camera");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        Scene main = SceneManager.GetSceneByPath(MainMenuController.MainPath);
        Check(
            menu.MenuVisible && SceneManager.sceneCount == 2 && main.GetRootGameObjects().All(r => !r.activeSelf),
            "return unloads gameplay and parks Main roots");
        menu.ReturnToMenu();
        menu.ResumeWorkshop();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            main.GetRootGameObjects().First(r => r.name == "Managers").activeSelf && !menu.MenuVisible,
            "repeated menu return does not corrupt root active states");
        Check(ActiveEvents() == 1, "resume keeps the single menu EventSystem");
        string directory = Path.Combine(Application.temporaryCachePath, "MainMenuChecks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        InjectStore("_playerFiles", Path.Combine(directory, "Saves"));
        InjectStore("_baseFiles", Path.Combine(directory, "Bases"));
        InjectStore("_buildingFiles", Path.Combine(directory, "Buildings"));
        BlockDataList vehicle = Vehicle();
        File.WriteAllText(SaveManager.instance.GetSavePath("Menu Vehicle A"), JsonUtility.ToJson(vehicle));
        File.WriteAllText(SaveManager.instance.GetSavePath("Menu Vehicle B"), JsonUtility.ToJson(vehicle));
        string broken = SaveManager.instance.GetSavePath("Broken");
        File.WriteAllText(broken, "{}");
        string invalid = SaveManager.instance.GetSavePath("Invalid");
        File.WriteAllText(invalid, "{\"blocks\":[{\"resourcePath\":\"missing\",\"x\":1,\"y\":1,\"z\":1}]}");
        string basePath = SaveManager.instance.GetBaseBlueprintPath("Menu Base");
        string starter = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Siege/Construction/StarterBase.json").text;
        File.WriteAllText(basePath, starter);
        BlockDataList alias = JsonUtility.FromJson<BlockDataList>(starter);
        alias.foundation.width++;
        File.WriteAllText(
            SaveManager.instance.GetBaseBlueprintPath("Starter Bastion (read only)"),
            JsonUtility.ToJson(alias));
        // A deliberately slow legacy interval makes the production menu's fast path observable.
        BuildManager.instance.BlockLoadIntervalSeconds = 1f;
        BuildManager.instance.moveCameraDuringBlockLoad = false;
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.Choose(MenuDestination.VehicleWorkshop);
        Select(menu, "Broken");
        menu.LaunchSelected();
        Check(
            !menu.Busy && GameManager.instance.blocksParent == null,
            "missing blocks array is rejected without replacing the draft");
        Select(menu, "Invalid");
        menu.LaunchSelected();
        while (menu.Busy || BuildManager.instance.IsLoadingBlocks)
        {
            yield return null;
        }

        Check(
            GameManager.instance.blocksParent != null && File.ReadAllText(invalid).Contains("missing"),
            "repairable blueprint opens in workshop without deleting missing module snapshots");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.Choose(MenuDestination.VehicleWorkshop);
        Select(menu, "Menu Vehicle A");
        string removed = SaveManager.instance.GetSavePath("Menu Vehicle A");
        string originalJson = File.ReadAllText(removed);
        File.Delete(removed);
        menu.LaunchSelected();
        Check(
            !menu.Busy && menu.MenuVisible && !File.Exists(removed),
            "deleted selection fails safely without recreating a file");
        File.WriteAllText(removed, originalJson);
        menu.RefreshLibrary();
        Select(menu, "Menu Vehicle A");
        double loadStarted = Time.realtimeSinceStartupAsDouble;
        menu.LaunchSelected();
        while (menu.Busy)
        {
            yield return null;
        }

        Transform draft = GameManager.instance.blocksParent;
        Check(
            Time.realtimeSinceStartupAsDouble - loadStarted < 1f,
            "menu blueprint restoration bypasses the one-second legacy interval");
        BlockDataList originalCache = SaveManager.instance.cachedData;
        draft.name = "UNSAVED MENU MARKER";
        Vector3 cameraPosition = CameraController.instance.transform.position;
        Check(
            !menu.MenuVisible && draft.GetComponentsInChildren<Block>().Length == 2,
            "vehicle workshop restores selected modules");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.ResumeWorkshop();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            GameManager.instance.blocksParent == draft && draft.name == "UNSAVED MENU MARKER",
            "resume preserves original object and unsaved in-memory changes");
        Check(
            CameraController.instance.transform.position == cameraPosition,
            "menu resume preserves camera pose");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.Choose(MenuDestination.Exploration);
        Select(menu, "Menu Vehicle B");
        menu.LaunchSelected();
        menu.CancelNavigation();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            menu.MenuVisible && SceneManager.sceneCount == 2 && GameManager.instance.blocksParent == draft,
            "cancel navigation restores original and parks safely after blueprint load");
        menu.Choose(MenuDestination.Exploration);
        Select(menu, "Menu Vehicle B");
        menu.LaunchSelected();
        while (menu.Busy && PlayManager.instance.Modes.State == GameModeState.Building)
        {
            yield return null;
        }

        menu.CancelNavigation();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            menu.MenuVisible && SceneManager.sceneCount == 2 && PlayManager.instance.WorldSession == null,
            "cancel during exploration preparation fully unloads and unbinds");
        PlayerPrefs.SetString("HY.Menu.RecentName", "Deleted recent design");
        PlayerPrefs.SetInt("HY.Menu.RecentBuilding", 0);
        menu.OpenRecent();
        Check(
            !menu.Busy && menu.MenuVisible && GameManager.instance.blocksParent == draft,
            "missing recent design preserves current workshop");
        var modeData = new SerializedObject(PlayManager.instance.Modes);
        string explorationScene = modeData.FindProperty("explorationScene").stringValue;
        modeData.FindProperty("explorationScene").stringValue = "Assets/Scenes/UnavailableMenuCheck.unity";
        modeData.ApplyModifiedPropertiesWithoutUndo();
        menu.Choose(MenuDestination.Exploration);
        Select(menu, "Menu Vehicle B");
        menu.LaunchSelected();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            menu.MenuVisible && SceneManager.sceneCount == 2 && GameManager.instance.blocksParent == draft && !draft.gameObject.activeSelf,
            "failed gameplay scene load restores draft and parked Main before retry");
        modeData.Update();
        modeData.FindProperty("explorationScene").stringValue = explorationScene;
        modeData.ApplyModifiedPropertiesWithoutUndo();
        menu.Choose(MenuDestination.Siege);
        Select(menu, "Menu Base");
        menu.LaunchSelected();
        while (menu.Busy)
        {
            yield return null;
        }

        SiegeGameplayStage stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
        Check(
            (string)typeof(SiegeGameplayStage).GetField("selectedName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stage) == "Menu Base",
            "Siege receives the menu-selected saved base");
        Check(
            stage.Base != null && stage.Base.IsAlive,
            "menu-selected base deploys automatically through production validation");
        Check(stage.BeginWave(), "menu-selected base starts a real wave");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            GameManager.instance.blocksParent == draft && File.ReadAllText(basePath) == starter,
            "Siege return restores vehicle draft and never writes the base design");
        Check(
            SceneManager.sceneCount == 2 && ActiveEvents() == 1,
            "wave return cleans gameplay and duplicate input services");
        menu.Choose(MenuDestination.Exploration);
        var historyProbe = new HistoryProbe();
        ActionManager.instance.Push(historyProbe);
        int undoCount = ActionManager.instance.GetUndoCount();
        Select(menu, "Menu Vehicle B");
        menu.LaunchSelected();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            PlayManager.instance.Modes.State == GameModeState.Exploring && !menu.MenuVisible,
            "menu-selected alternative vehicle enters exploration");
        Check(
            draft != null && !draft.gameObject.activeSelf && GameManager.instance.blocksParent != draft,
            "alternative exploration parks original workshop objects");
        string exploringPath = SaveManager.instance.GetSavePath("Menu Vehicle B");
        string exploringJson = File.ReadAllText(exploringPath);
        File.Delete(exploringPath);
        expectedCargoError = true;
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        expectedCargoError = false;
        Check(
            PlayManager.instance.Modes.State == GameModeState.Exploring && !menu.MenuVisible,
            "cargo-save failure keeps exploration alive and refuses menu parking");
        Text notice = (Text)new SerializedObject(SceneObjectQuery.FindInScene<WorkshopNavigation>(main)).FindProperty("notice").objectReferenceValue;
        Check(
            notice.gameObject.activeInHierarchy && notice.text.Contains("Cargo"),
            "cargo-save failure is visible in the active gameplay UI");
        File.WriteAllText(exploringPath, exploringJson);
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            menu.MenuVisible && GameManager.instance.blocksParent == draft && draft.name == "UNSAVED MENU MARKER",
            "exploration return restores exact previous workshop draft");
        Check(
            SaveManager.instance.cachedData == originalCache && BuildManager.instance.CurrentBuildName == "Menu Vehicle A",
            "exploration restores editor context and cache");
        Check(
            !draft.gameObject.activeSelf && SceneManager.sceneCount == 2,
            "original is parked by menu after full exploration cleanup");
        Check(
            ActionManager.instance.GetUndoCount() == undoCount,
            "temporary play selection restores the original Undo stack");
        ActionManager.instance.Undo();
        Check(historyProbe.Undone, "restored Undo stack contains the same action objects");
        ActionManager.instance.Redo();
        Check(!historyProbe.Undone, "restored Redo stack remains functional");
        menu.Choose(MenuDestination.BaseWorkshop);
        Select(menu, "Menu Base");
        menu.LaunchSelected();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            BuildManager.instance.IsEditingBuildingBlueprint && BuildManager.instance.Foundation != null,
            "base workshop opens with authored foundation");
        Check(
            BuildManager.instance.IsEditingBaseBlueprint && SaveManager.instance.GetBaseBlueprintPath("Menu Base") != SaveManager.instance.GetBuildingBlueprintPath("Menu Base"),
            "base editor uses its independent storage context");
        SaveManager.instance.GetAllBuildingBlueprintNames();
        Check(
            !SaveManager.instance.buildingBlueprints.Contains("Menu Base"),
            "base list never leaks into the general building library");
        string ordinaryPath = SaveManager.instance.GetBuildingBlueprintPath("Menu Base");
        File.WriteAllText(ordinaryPath, "ordinary-building-sentinel");
        Check(
            SaveManager.instance.RenameBuildingBlueprint("Menu Base", "Renamed Base") && File.ReadAllText(ordinaryPath) == "ordinary-building-sentinel",
            "renaming an active base preserves the same-name building");
        Check(
            SaveManager.instance.RenameBuildingBlueprint("Renamed Base", "Menu Base"),
            "renamed base restores its selected identity");
        SaveManager.instance.DuplicateSave("Menu Base");
        Check(
            File.Exists(SaveManager.instance.GetBaseBlueprintPath("Menu Base Copy")) && !File.Exists(SaveManager.instance.GetBuildingBlueprintPath("Menu Base Copy")),
            "duplicating a base writes only to the base library");
        SaveManager.instance.DeleteBuildingBlueprint("Menu Base Copy");
        Check(
            !File.Exists(SaveManager.instance.GetBaseBlueprintPath("Menu Base Copy")) && File.ReadAllText(ordinaryPath) == "ordinary-building-sentinel",
            "deleting a base preserves general buildings");
        BuildingEditorPanel foundationPanel = Object.FindAnyObjectByType<BuildingEditorPanel>();
        Check(
            ((RectTransform)foundationPanel.transform).rect.height < 150 && !foundationPanel.GetComponent<BuildingOutpostEditor>().enabled,
            "base editor hides general outpost controls and their raycast area");
        Transform baseDraft = GameManager.instance.blocksParent;
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.OpenRecent();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            GameManager.instance.blocksParent == baseDraft,
            "recent current design resumes without reloading or destroying changes");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        int before = Directory.GetFiles(Path.GetDirectoryName(basePath), "*.json").Length;
        menu.Choose(MenuDestination.BaseWorkshop);
        menu.SelectBlueprint(0);
        Check(
            Directory.GetFiles(Path.GetDirectoryName(basePath), "*.json").Length == before,
            "browsing examples is read-only");
        menu.LaunchSelected();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            Directory.GetFiles(Path.GetDirectoryName(basePath), "*.json").Length == before + 1 && BuildManager.instance.CurrentBuildName.StartsWith("Starter Bastion"),
            "explicit example edit creates a unique personal copy");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.Choose(MenuDestination.VehicleWorkshop);
        menu.CreateBlueprint();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            !BuildManager.instance.IsEditingDeveloperBlueprint && BuildManager.instance.CurrentBuildName.StartsWith("New Vehicle"),
            "new vehicle flow opens a unique editable save");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.Choose(MenuDestination.BaseWorkshop);
        menu.CreateBlueprint();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            BuildManager.instance.IsEditingBuildingBlueprint && BuildManager.instance.Foundation != null && SaveManager.instance.cachedData.blocks.Count == 0,
            "new base flow opens an empty foundation with no implicit core");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.Choose(MenuDestination.BaseWorkshop);
        menu.OpenRecent();
        while (menu.Busy)
        {
            yield return null;
        }

        BuildManager buildProbe = BuildManager.instance;
        Check(
            buildProbe.IsEditingBaseBlueprint && buildProbe.enabled && InputManager.instance.lockView,
            "Vehicle to Base restores construction input");
        Camera buildCamera = buildProbe.mainCamera;
        CameraController.instance.StopCameraMotion();
        Vector3 aim = buildProbe.Foundation.transform.TransformPoint(new Vector3(.5f, 0f, .5f));
        buildCamera.transform.position = aim + new Vector3(0f, 36f, -12f);
        buildCamera.transform.LookAt(aim);
        buildProbe.SetCurrentBlockResource("Blocks/2x2x2");
        Vector2 screenAim = buildCamera.WorldToScreenPoint(aim);
        Mouse physicalMouse = Mouse.current;
        InputSystem.DisableDevice(physicalMouse);
        Mouse mouse = InputSystem.AddDevice<Mouse>("BasePlacementCheck");
        var previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        try
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenAim });
            for (int frame = 0; frame < 8; frame++)
            {
                yield return null;
            }

            Check(buildProbe.currentGhost == null, "base placement beyond the 30m construction range has no ghost");
            buildCamera.transform.position = aim + new Vector3(0f, 22f, -8f);
            buildCamera.transform.LookAt(aim);
            screenAim = buildCamera.WorldToScreenPoint(aim);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenAim });
            for (int frame = 0; frame < 8; frame++)
            {
                yield return null;
            }

            Check(
                buildProbe.currentGhost != null && buildProbe.hoveredConnector != null,
                "base foundation offers a placement ghost beyond the vehicle's 15m range");
            int blocksBeforeClick = SaveManager.instance.cachedData.blocks.Count;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenAim }.WithButton(MouseButton.Left));
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
            }

            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenAim });
            yield return null;
            Check(
                SaveManager.instance.cachedData.blocks.Count == blocksBeforeClick + 1,
                "real mouse click places and saves a block after Vehicle to Base navigation");
            ScreenCapture.CaptureScreenshot("Temp/MainMenu/base-placement-fixed.png");
            yield return null;
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.EnableDevice(physicalMouse);
            InputSystem.settings.backgroundBehavior = previousBackground;
        }

        IEnumerator surface = SurfacePlacement();
        try
        {
            while (surface.MoveNext())
            {
                yield return surface.Current;
            }
        }
        finally
        {
            (surface as IDisposable)?.Dispose();
        }

        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        menu.ShowHome();
        Object.FindAnyObjectByType<MainMenuView>().Settings();
        MenuSettings settings = Object.FindAnyObjectByType<MenuSettings>();
        var settingData = new SerializedObject(settings);
        Button preferenceTab = (Button)settingData.FindProperty("preferenceTab").objectReferenceValue;
        Button generalTab = (Button)settingData.FindProperty("generalTab").objectReferenceValue;
        Toggle autoRotate = (Toggle)settingData.FindProperty("autoRotate").objectReferenceValue;
        GameObject preferencePage = (GameObject)settingData.FindProperty("preferencePage").objectReferenceValue;
        GameObject generalPage = (GameObject)settingData.FindProperty("generalPage").objectReferenceValue;
        yield return null;
        PointerClick(preferenceTab);
        yield return null;
        Check(
            preferencePage.activeInHierarchy && !generalPage.activeInHierarchy,
            "Preference navigation accepts an unobstructed pointer click");
        bool oldAutoRotate = autoRotate.isOn;
        PointerClick(autoRotate);
        Check(
            MenuSettings.AutoRotatePlacement == !oldAutoRotate,
            "auto-rotate toggle pointer click changes persisted placement preference");
        PointerClick(generalTab);
        yield return null;
        Check(
            generalPage.activeInHierarchy && !preferencePage.activeInHierarchy,
            "General navigation restores the settings controls");
        PointerClick(preferenceTab);
        yield return null;
        Check(autoRotate.isOn == !oldAutoRotate, "placement preference survives page navigation");
        PointerClick(generalTab);
        yield return null;
        Slider volume = (Slider)new SerializedObject(settings).FindProperty("volume").objectReferenceValue;
        Slider sensitivity = (Slider)new SerializedObject(settings).FindProperty("sensitivity").objectReferenceValue;
        volume.value = .37f;
        sensitivity.value = 3.25f;
        Check(
            Mathf.Approximately(AudioListener.volume, .37f) && Mathf.Approximately(PlayerPrefs.GetFloat("HY.Menu.Volume"), .37f),
            "master volume applies and persists");
        Check(
            Mathf.Approximately(CameraController.instance.mouseSensitivity, 3.25f),
            "mouse sensitivity applies to the parked workshop camera");
        settings.Initialize();
        Check(Mathf.Approximately(sensitivity.value, 3.25f), "settings initialization is idempotent");
        menu.ShowHome();
        ScreenCapture.CaptureScreenshot("Temp/MainMenu/home-final.png");
        Check(
            ActiveEvents() == 1 && ActiveScreenCameras() == 1,
            "final menu has one EventSystem and one screen camera");
        PlayManager workshopPlay = PlayManager.instance;
        DestroyManager workshopDestroy = DestroyManager.instance;
        DebugManager workshopDebug = DebugManager.instance;
        VisualEffectsManager workshopEffects = VisualEffectsManager.instance;
        Transform workshopDraft = GameManager.instance.blocksParent;
        BlockDataList workshopCache = SaveManager.instance.cachedData;
        Button attractButton = (Button)new SerializedObject(Object.FindAnyObjectByType<MainMenuView>()).FindProperty("battleAttract").objectReferenceValue;
        yield return null;
        Check(attractButton.interactable, "attract entry is usable without a recent blueprint");
        PointerClick(attractButton);
        while (menu.Busy)
        {
            yield return null;
        }

        Check(menu.AttractVisible && !menu.MenuVisible, "menu opens the independent attract scene");
        float attractUntil = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < attractUntil)
        {
            yield return null;
        }

        BattleAttractStage attract = Object.FindAnyObjectByType<BattleAttractStage>();
        Check(
            attract != null && attract.SpawnedShips >= 8 && attract.BaseAlive,
            "attract restores the real base and eight ships");
        ControlUnit autonomous = ControlUnit.RuntimeUnits.FirstOrDefault(unit => unit != null && unit.gameObject.scene.path == MainMenuController.AttractPath && unit.Building == null && unit.faction == UnitFaction.Player);
        Check(
            autonomous != null && autonomous.cockpit.ControlMode == UnitControlMode.Autonomous,
            "attract friendly is autonomous");
        DestroyManager.instance.DestroyGameObject(autonomous.cockpit.gameObject);
        yield return null;
        Check(
            !MainUIPanels.instance.deathPanel.activeSelf,
            "autonomous friendly loss does not open the player death panel");
        using (var escape = (IDisposable)EscapeAttract(menu))
        {
            while (((IEnumerator)escape).MoveNext())
            {
                yield return null;
            }
        }

        Check(!menu.AttractVisible && menu.MenuVisible, "attract unloads and returns to the menu");
        Check(
            PlayManager.instance == workshopPlay && DestroyManager.instance == workshopDestroy && DebugManager.instance == workshopDebug && VisualEffectsManager.instance == workshopEffects,
            "attract restores all workshop service references");
        menu.ResumeWorkshop();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            GameManager.instance.blocksParent == workshopDraft && ReferenceEquals(SaveManager.instance.cachedData, workshopCache),
            "attract roundtrip preserves the exact unsaved workshop draft");
        Check(
            CameraController.instance.isActiveAndEnabled && InputManager.instance.keyInput,
            "workshop input and camera resume after attract");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(
            ActiveEvents() == 1 && ActiveScreenCameras() == 1,
            "attract roundtrip leaves one menu camera and EventSystem");
        Check(
            File.ReadAllText(broken) == "{}" && File.ReadAllText(invalid).Contains("missing"),
            "rejected source files stay byte-for-byte untouched");
    }

    private static IEnumerator SurfacePlacement()
    {
        BuildManager build = BuildManager.instance;
        Camera camera = build.mainCamera;
        PlayerPrefs.SetInt("HY.Menu.AutoRotatePlacement", 1);
        Mouse physical = Mouse.current;
        InputSystem.DisableDevice(physical);
        Mouse mouse = InputSystem.AddDevice<Mouse>("Surface placement fixture");
        InputSettings.BackgroundBehavior previous = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        Vector3[] normals =
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };
        try
        {
            for (int face = 0; face < normals.Length; face++)
            {
                Vector3 center = new Vector3(1200 + face * 30, 1200, 1200);
                build.CreateBlock(Resources.Load<GameObject>("Blocks/2x2x2"), "Blocks/2x2x2", center, Quaternion.identity);
                yield return null;
                build.SetCurrentBlockResource("Blocks/Turret");
                camera.transform.position = center + normals[face] * 9;
                camera.transform.rotation = Quaternion.LookRotation(-normals[face], Mathf.Abs(normals[face].y) > .9f ? Vector3.forward : Vector3.up);
                Vector2 point = camera.WorldToScreenPoint(center);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                for (int frame = 0; frame < 4; frame++)
                {
                    yield return null;
                }

                Check(
                    build.currentGhost != null && build.hoveredConnector != null,
                    "restricted turret has a production preview on face " + face);
                Quaternion rotation = build.currentGhost.transform.rotation;
                Vector3 position = build.currentGhost.transform.position;
                Vector3 cameraBefore = camera.transform.position;
                Vector2 mouseBefore = mouse.position.ReadValue();
                Vector3 connectorBefore = build.hoveredConnector.localPos;
                int frameBeforePlacement = Time.frameCount;
                Check(
                    Vector3.Dot(rotation * Vector3.up, normals[face]) > .999f,
                    "turret attachment face follows surface " + face);
                int count = SaveManager.instance.cachedData.blocks.Count;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null;
                Check(
                    SaveManager.instance.cachedData.blocks.Count == count + 1,
                    "real mouse places restricted turret on face " + face);
                Check(
                    Vector3.Distance(cameraBefore, camera.transform.position) < .0001f,
                    "placement feedback preserves the workshop aim on face " + face);
                Block placed = build.lastSaveBlock;
                float positionError = placed != null ? Vector3.Distance(placed.transform.position, position) : float.PositiveInfinity;
                float rotationError = placed != null ? Quaternion.Angle(placed.transform.rotation, rotation) : float.PositiveInfinity;
                Check(
                    placed != null && positionError < .001f && rotationError < .1f,
                    "placed turret matches preview pose on face " + face + " (preview=" + position.ToString("F4") + ", actual=" + placed?.transform.position.ToString("F4") + ", position error=" + positionError + ", angle=" + rotationError + ", camera=" + cameraBefore.ToString("F6") + " -> " + camera.transform.position.ToString("F6") + ", mouse=" + mouseBefore.ToString("F6") + " -> " + mouse.position.ReadValue().ToString("F6") + ", connector=" + connectorBefore.ToString("F6") + ", frame=" + frameBeforePlacement + " -> " + Time.frameCount + ")");
            }
        }
        finally
        {
            build.SetCurrentBlockResource(string.Empty);
            InputSystem.RemoveDevice(mouse);
            InputSystem.EnableDevice(physical);
            InputSystem.settings.backgroundBehavior = previous;
        }
    }

    private static IEnumerator EscapeAttract(MainMenuController menu)
    {
        Keyboard physical = Keyboard.current;
        bool enabled = physical != null && physical.enabled;
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        Keyboard probe = null;
        try
        {
            if (enabled)
            {
                InputSystem.DisableDevice(physical);
            }

            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            probe = InputSystem.AddDevice<Keyboard>("Attract escape probe");
            probe.MakeCurrent();
            InputSystem.QueueStateEvent(probe, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(probe, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(probe, new KeyboardState());
            yield return null;
            while (menu.Busy)
            {
                yield return null;
            }

            Check(!menu.AttractVisible && menu.MenuVisible, "real ESC returns from attract");
        }
        finally
        {
            if (probe != null && probe.added)
            {
                InputSystem.RemoveDevice(probe);
            }

            if (enabled && physical != null && physical.added)
            {
                InputSystem.EnableDevice(physical);
                physical.MakeCurrent();
            }

            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        }
    }

    private static void PointerClick(Selectable control)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform rect = control.GetComponent<RectTransform>();
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(
            hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Selectable>() == control,
            control.name + " is topmost pointer target");
        ExecuteEvents.Execute(control.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static void Select(MainMenuController menu, string name)
    {
        int index = menu.Entries.ToList().FindIndex(e => !e.IsExample && e.Name == name);
        Check(index >= 0, "library contains " + name);
        menu.SelectBlueprint(index);
    }

    private static BlockDataList Vehicle()
    {
        var data = new BlockDataList();
        foreach (string name in new[]
        {
            "Cockpit",
            "CargoHold"
        }

        )
        {
            GameObject obj = Object.Instantiate(Resources.Load<GameObject>("Blocks/" + name));
            obj.transform.position = name == "Cockpit" ? Vector3.zero : Vector3.right * 2f;
            Block block = obj.GetComponent<Block>();
            block.resourcePath = "Blocks/" + name;
            block.uniqueId = Guid.NewGuid().ToString("N");
            data.blocks.Add(new BlockData(block));
            Object.Destroy(obj);
        }

        return data;
    }

    private static void InjectStore(string field, string directory)
    {
        Type type = typeof(SaveManager).Assembly.GetType("BlueprintFileStore", true);
        typeof(SaveManager).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(SaveManager.instance, Activator.CreateInstance(type, new object[] { directory }));
    }

    private static int ActiveEvents()
    {
        return Object.FindObjectsByType<EventSystem>().Length;
    }

    private static int ActiveScreenCameras()
    {
        return Object.FindObjectsByType<Camera>().Count(c => c.enabled && c.targetTexture == null);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }

        report.checks.Add(message);
    }

    private static void LogError(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !(expectedCargoError && message.StartsWith("Cargo return could not be saved")))
        {
            report.errors.Add(message + "\n" + trace);
        }
    }

    private static void BackupPreferences()
    {
        preferences.Clear();
        foreach (string key in new[]
        {
            "HY.Menu.RecentName",
            "HY.Menu.RecentBuilding",
            "HY.Menu.AutoRotatePlacement",
            "HY.Menu.Volume",
            "HY.Menu.Sensitivity"
        }

        )
        {
            string kind = key.EndsWith("Name") ? "s" : key.EndsWith("Building") || key.EndsWith("AutoRotatePlacement") ? "i" : "f";
            preferences[key] = !PlayerPrefs.HasKey(key) ? null : kind + (kind == "s" ? PlayerPrefs.GetString(key) : kind == "i" ? PlayerPrefs.GetInt(key).ToString() : PlayerPrefs.GetFloat(key).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        preferences["AudioListener.volume"] = AudioListener.volume.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void Finish()
    {
        Application.logMessageReceived -= LogError;
        EditorApplication.update -= Tick;
        (scenario as IDisposable)?.Dispose();
        scenario = null;
        foreach (var preference in preferences)
        {
            string value = preference.Value;
            if (preference.Key == "AudioListener.volume")
            {
                AudioListener.volume = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (value == null)
            {
                PlayerPrefs.DeleteKey(preference.Key);
            }
            else if (value[0] == 's')
            {
                PlayerPrefs.SetString(preference.Key, value.Substring(1));
            }
            else if (value[0] == 'i')
            {
                PlayerPrefs.SetInt(preference.Key, int.Parse(value.Substring(1)));
            }
            else
            {
                PlayerPrefs.SetFloat(preference.Key, float.Parse(value.Substring(1), System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        PlayerPrefs.Save();
        expectedCargoError = false;
        SessionState.SetBool(Pending, false);
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Temp/MainMenu");
        File.WriteAllText("Temp/MainMenu/play-checks.json", JsonUtility.ToJson(report, true));
        Debug.Log("Main menu checks: " + report.status + " / " + report.checks.Count);
        EditorApplication.isPlaying = false;
    }
}
