// 职责：独立菜单启动、蓝图选择、工坊/玩法导航；Main按需加载并暂停，保留当前原稿。
// 入口：Choose() / LaunchSelected() / ResumeWorkshop() / ReturnToMenu()。
// 生命周期：菜单服务常驻自身场景，单一协程拥有切换；先结束玩法/保存货仓，再暂停Main。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MenuDestination
{
    Exploration,
    Siege,
    VehicleWorkshop,
    BaseWorkshop
}

public sealed class MainMenuController : MonoBehaviour
{
    public const string MainPath = "Assets/Scenes/Main.unity";
    private const string RecentName = "HY.Menu.RecentName";
    private const string RecentBuilding = "HY.Menu.RecentBuilding";
    [SerializeField]
    private MainMenuView view;
    [SerializeField]
    private MenuSettings settings;
    [SerializeField]
    private MenuBlueprintPreview preview;
    [SerializeField]
    private SiegeConstructionSettings siegeSettings;
    [SerializeField]
    private TextAsset vehicleExample;
    [SerializeField]
    private GameObject presentation;
    private readonly Dictionary<GameObject, bool> parkedRoots = new Dictionary<GameObject, bool>();
    private readonly MenuPlayDraft playSelection = new MenuPlayDraft();
    private List<MenuBlueprintEntry> entries = new List<MenuBlueprintEntry>();
    private MenuDestination destination;
    private int selected;
    private bool readable;
    private bool playable;
    private bool started;
    private bool cancelNavigation;
    public bool Busy
    {
        get; private set;
    }
    public bool MenuVisible => presentation.activeSelf;
    public string Status
    {
        get; private set;
    }
    public IReadOnlyList<MenuBlueprintEntry> Entries => entries;
    public MenuBlueprintEntry Selected => selected >= 0 && selected < entries.Count ? entries[selected] : null;
    private Scene Main => SceneManager.GetSceneByPath(MainPath);
    private bool Editing => destination == MenuDestination.VehicleWorkshop || destination == MenuDestination.BaseWorkshop;
    private bool Building => destination == MenuDestination.BaseWorkshop || destination == MenuDestination.Siege;
    private GameModeController Modes => PlayManager.instance.Modes;

    private void Start()
    {
        started = true;
        view.Bind(this);
        settings.Initialize();
        ShowHome();
        if (Main.isLoaded)
        {
            ReturnToMenu();
        }
    }

    public void ShowHome()
    {
        preview.Clear();
        view.Home(
            Main.isLoaded && GameManager.instance != null && GameManager.instance.blocksParent != null,
            PlayerPrefs.GetString(RecentName, string.Empty));
        SetStatus("Your workshop stays in memory when returning here. Saved blueprints are shared by both entrypoints.");
    }

    private void Update()
    {
        if (!Busy && !MenuVisible && playSelection.Parked && Main.isLoaded && Modes.State == GameModeState.Building)
        {
            playSelection.Restore();
        }
    }

    public void Choose(MenuDestination target)
    {
        if (Busy)
        {
            return;
        }

        destination = target;
        view.Picker(
            target == MenuDestination.Exploration ? "EXPLORATION / SELECT VEHICLE" : target == MenuDestination.Siege ? "SIEGE / SELECT BASE" : Building ? "BASE WORKSHOP" : "VEHICLE WORKSHOP",
            Editing);
        RefreshLibrary();
    }

    public void RefreshLibrary()
    {
        try
        {
            string previous = Selected?.Name;
            entries = MenuBlueprintLibrary.Read(Building, Building ? siegeSettings.Starter : vehicleExample);
            int index = entries.FindIndex(e => e.Name == previous);
            selected = Mathf.Max(0, index);
            view.SetChoices(entries.Select(e => e.Label).ToList(), selected);
            SelectBlueprint(selected);
            SetStatus(Editing ? "Editing an example creates a personal copy. Opening another design replaces the workshop view; existing saved designs remain available." : "Play uses a separate session. Your workshop draft is restored when you return.");
        }
        catch (Exception error) when (IsDataError(error))
        {
            entries.Clear();
            view.SetChoices(new List<string>(), 0);
            view.Selection("Library unavailable. Check file permissions and retry Refresh.", false, "UNAVAILABLE");
            SetStatus(error.Message);
        }
    }

    public void SelectBlueprint(int index)
    {
        selected = index;
        readable = false;
        playable = false;
        preview.Clear();
        if (Selected == null)
        {
            view.Selection("No saved designs yet. Create a design in the workshop.", false, "NO BLUEPRINT");
            return;
        }

        try
        {
            BlockDataList data = MenuBlueprintLibrary.Inspect(Selected, siegeSettings, out playable, out string reason, Editing);
            readable = true;
            preview.Show(data);
            string action = Editing ? (Selected.IsExample ? "CREATE COPY & EDIT" : "OPEN WORKSHOP") : Selected.IsExample && !Building ? "CREATE COPY & EXPLORE" : "START " + (Building ? "SIEGE" : "EXPLORATION");
            view.Selection(Selected.Label + "\n\n" + data.blocks.Count + " modules\n\n" + reason, Editing || playable, action);
        }
        catch (Exception error) when (IsDataError(error))
        {
            view.Selection(
                Selected.Label + "\n\nCannot open blueprint: " + error.Message + "\n\nThe file is unchanged. Restore a backup or choose another design.",
                false,
                "UNAVAILABLE");
        }
    }

    public void LaunchSelected()
    {
        if (Busy || Selected == null || !readable || (!Editing && !playable))
        {
            return;
        }

        Run(Launch(Selected, destination));
    }

    public void CreateBlueprint()
    {
        if (!Busy && Editing)
        {
            Run(CreateAndEdit(Building));
        }
    }

    private IEnumerator CreateAndEdit(bool building)
    {
        var files = MenuBlueprintLibrary.Files(building);
        string name = building ? "New Base" : "New Vehicle";
        string stem = name;
        for (int n = 2; File.Exists(files.GetPath(name)); n++)
        {
            name = stem + " " + n;
        }

        var data = new BlockDataList();
        if (building)
        {
            data.foundation = new BuildingFoundationData();
        }

        using (var stream = new FileStream(files.GetPath(name), FileMode.CreateNew, FileAccess.Write))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonUtility.ToJson(data, true));
            }
        }

        yield return Launch(
            new MenuBlueprintEntry(name, building),
            building ? MenuDestination.BaseWorkshop : MenuDestination.VehicleWorkshop);
    }

    private IEnumerator Launch(MenuBlueprintEntry entry, MenuDestination target)
    {
        bool editing = target == MenuDestination.VehicleWorkshop || target == MenuDestination.BaseWorkshop;
        // Re-read immediately before switching: removed/corrupt files never replace an in-memory draft.
        MenuBlueprintLibrary.Inspect(entry, siegeSettings, out bool valid, out string reason, editing);
        if (!editing && !valid)
        {
            throw new InvalidDataException(reason);
        }

        string name = entry.IsExample && (editing || !entry.Building) ? MenuBlueprintLibrary.CopyExample(entry) : entry.Name;
        yield return EnsureMain();
        if (cancelNavigation)
        {
            yield return Return();
            yield break;
        }

        if (Modes.State != GameModeState.Building)
        {
            throw new InvalidOperationException("Return from the current session before opening another design.");
        }

        if (target == MenuDestination.Siege)
        {
            Modes.LaunchSiege(entry.IsExample ? null : name);
        }
        else
        {
            BuildManager build = BuildManager.instance;
            bool same = GameManager.instance.blocksParent != null && build.CurrentBuildTarget == (entry.Building ? BuildTargetKind.BaseBlueprint : BuildTargetKind.PlayerSave) && build.CurrentBuildName == name;
            if (!same)
            {
                if (!editing)
                {
                    playSelection.Park();
                }

                OpenBlueprint(entry.Building, name);
                yield return WaitForBlueprint();
                if (cancelNavigation)
                {
                    yield return Return();
                    yield break;
                }

                if (GameManager.instance.blocksParent == null)
                {
                    throw new InvalidOperationException("The selected blueprint could not be restored.");
                }
            }

            if (editing)
            {
                Remember(entry.Building, name);
            }
            else
            {
                Modes.ShowSelection();
                Modes.SelectExploration();
            }
        }

        if (!editing)
        {
            float deadline = Time.realtimeSinceStartup + 120f;
            while (Modes.State == GameModeState.Loading || Modes.State == GameModeState.Returning)
            {
                if (cancelNavigation)
                {
                    Modes.ReturnToBuild();
                }

                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new TimeoutException("Mode loading timed out. Use the workshop Menu button to cancel and return.");
                }

                yield return null;
            }

            if (cancelNavigation)
            {
                yield return Return();
                yield break;
            }

            if (Modes.State != GameModeState.Exploring && Modes.State != GameModeState.Siege)
            {
                throw new InvalidOperationException(Modes.Status);
            }
        }

        SetPresentation(false);
    }

    private static void OpenBlueprint(bool building, string name)
    {
        MainUIPanels.instance.HideCreatePanel();
        if (building)
        {
            SaveManager.instance.LoadBaseBlueprint(name);
        }
        else
        {
            SaveManager.instance.LoadSave(name);
        }

        SaveUIPanel.instance.RefreshList();
    }

    public void ResumeWorkshop()
    {
        if (!Busy && Main.isLoaded)
        {
            Run(Resume());
        }
    }

    private IEnumerator Resume()
    {
        yield return EnsureMain();
        if (cancelNavigation)
        {
            yield return Return();
            yield break;
        }

        SetPresentation(false);
    }

    public void OpenRecent()
    {
        string name = PlayerPrefs.GetString(RecentName, string.Empty);
        bool building = PlayerPrefs.GetInt(RecentBuilding, 0) != 0;
        if (!Busy && !string.IsNullOrEmpty(name))
        {
            Run(Launch(
                new MenuBlueprintEntry(name, building),
                building ? MenuDestination.BaseWorkshop : MenuDestination.VehicleWorkshop));
        }
    }

    public void ReturnToMenu()
    {
        if (!Busy && started)
        {
            if (parkedRoots.Count > 0 && MenuVisible)
            {
                ShowHome();
                return;
            }

            Run(Return());
        }
    }

    private IEnumerator Return()
    {
        if (Main.isLoaded)
        {
            yield return WaitForBlueprint();
            Modes.ReturnToBuild();
            // Exploring remains active when cargo persistence fails. Do not park or unload that session.
            yield return null;
            if (Modes.State == GameModeState.Exploring)
            {
                SetStatus("Cargo could not be saved. Resolve the save error in the active session, then retry Menu.");
                SceneObjectQuery.FindInScene<WorkshopNavigation>(Main).ShowNotice(Status);
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + 120f;
            while (Modes.State != GameModeState.Building)
            {
                if (Modes.State == GameModeState.Failed)
                {
                    Modes.ReturnToBuild();
                }

                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new TimeoutException("Waiting for gameplay cleanup. Retry after the current transition finishes.");
                }

                yield return null;
            }

            playSelection.Restore();
            SceneObjectQuery.FindInScene<WorkshopNavigation>(Main).UseMenuEvents();
            BuildManager.instance.DeselectBlock();
            CameraController.instance.StopCameraMotion();
            parkedRoots.Clear();
            foreach (GameObject root in Main.GetRootGameObjects())
            {
                parkedRoots.Add(root, root.activeSelf);
                root.SetActive(false);
            }
        }

        SceneManager.SetActiveScene(gameObject.scene);
        SetPresentation(true);
        ShowHome();
    }

    private IEnumerator EnsureMain()
    {
        if (!Main.isLoaded)
        {
            SetStatus("Loading workshop services...");
            yield return SceneManager.LoadSceneAsync(MainPath, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(Main);
            // Awake and Start must run before the explicit request; GameManager skips its default-save startup.
            yield return null;
        }
        else
        {
            foreach (KeyValuePair<GameObject, bool> root in parkedRoots)
            {
                if (root.Key != null)
                {
                    root.Key.SetActive(root.Value);
                }
            }

            parkedRoots.Clear();
            SceneManager.SetActiveScene(Main);
        }

        SceneObjectQuery.FindInScene<WorkshopNavigation>(Main).UseMenuEvents();
        settings.ApplyToWorkshop();
        yield return WaitForBlueprint();
    }

    private IEnumerator WaitForBlueprint()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (BuildManager.instance != null && BuildManager.instance.IsLoadingBlocks)
        {
            SetStatus($"Restoring blueprint... {BuildManager.instance.BlueprintLoadProgress:P0}");
            if (Time.realtimeSinceStartup > deadline)
            {
                BuildManager.instance.CancelBlueprintLoad();
                throw new TimeoutException("Blueprint loading timed out. The current objects were preserved.");
            }

            yield return null;
        }
    }

    private static void Remember(bool building, string name)
    {
        PlayerPrefs.SetString(RecentName, name);
        PlayerPrefs.SetInt(RecentBuilding, building ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetPresentation(bool visible)
    {
        presentation.SetActive(visible);
        if (visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Quit()
    {
        if (Busy)
        {
            return;
        }

        PlayerPrefs.Save();
        if (Application.isEditor)
        {
            SetStatus("Quit is disabled inside the Editor. Stop Play Mode to exit.");
        }
        else
        {
            Application.Quit();
        }
    }

    private void Run(IEnumerator work)
    {
        cancelNavigation = false;
        Busy = true;
        view.SetBusy(true);
        StartCoroutine(Guard(work));
    }

    public void CancelNavigation()
    {
        if (Busy)
        {
            cancelNavigation = true;
            SetStatus("Cancelling after the current blueprint load and gameplay cleanup finish...");
        }
    }

    private IEnumerator Guard(IEnumerator work)
    {
        // Advance nested routines here so file/load failures always release the menu's single transition owner.
        var stack = new Stack<IEnumerator>();
        stack.Push(work);
        Exception failure = null;
        bool recovering = false;
        while (stack.Count > 0)
        {
            object current = null;
            try
            {
                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    (stack.Pop() as IDisposable)?.Dispose();
                    continue;
                }

                current = top.Current;
            }
            catch (Exception error)
            {
                failure ??= error;
                foreach (IEnumerator pending in stack)
                {
                    (pending as IDisposable)?.Dispose();
                }

                stack.Clear();
                if (!recovering && Main.isLoaded && parkedRoots.Count == 0 && PlayManager.instance != null)
                {
                    recovering = true;
                    stack.Push(Return());
                    continue;
                }

                break;
            }

            if (current is IEnumerator nested)
            {
                stack.Push(nested);
            }
            else
            {
                yield return current;
            }
        }

        foreach (IEnumerator pending in stack)
        {
            (pending as IDisposable)?.Dispose();
        }

        if (failure != null)
        {
            Debug.LogWarning("Menu navigation failed: " + failure.Message);
            if (Main.isLoaded && Modes.State == GameModeState.Building)
            {
                playSelection.Restore();
            }

            SetStatus(failure.Message + "  Choose another design or retry.");
            if (!MenuVisible && Main.isLoaded)
            {
                SceneObjectQuery.FindInScene<WorkshopNavigation>(Main).ShowNotice(Status);
            }
        }

        Busy = false;
        view.SetBusy(false);
    }

    private void SetStatus(string message)
    {
        Status = message;
        view.Message(message);
    }

    private static bool IsDataError(Exception error)
    {
        return error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException || error is InvalidOperationException || error is Newtonsoft.Json.JsonException;
    }
}
