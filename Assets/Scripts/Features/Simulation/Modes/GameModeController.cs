// 职责：由Main拥有模式选择、Additive场景加载、探索绑定和返回；共享服务始终留在Main活动场景。
// 入口：ShowSelection() / SelectExploration() / SelectSiege() / ReturnToBuild()；同一时间只持有一个玩法场景。
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameModeState
{
    Building,
    Choosing,
    Loading,
    Exploring,
    Siege,
    Returning,
    Failed
}

public sealed class GameModeController : MonoBehaviour
{
    [SerializeField]
    private string explorationScene = "Assets/Scenes/Exploration.unity";
    [SerializeField]
    private string siegeScene = "Assets/Scenes/SiegeGameplay.unity";
    [SerializeField]
    private GameModePanel panel;
    [SerializeField]
    private WorldLoadingPanel worldLoading;
    [SerializeField]
    private GameObject[] buildEnvironment = Array.Empty<GameObject>();
    [SerializeField]
    private OutpostHud outpostHud;
    private readonly PlayDraft draft = new PlayDraft();
    private Scene modeScene;
    private Coroutine transition;
    private WorldSessionManager world;
    private SiegeGameplayStage siege;
    private bool cancelRequested;
    private bool explorationSelected;
    private bool previousKeyInput;
    private bool previousBuildEnabled;
    private bool inputCaptured;
    private string requestedSiegeDesign;
    public GameModeState State
    {
        get; private set;
    }
    public string Status { get; private set; } = string.Empty;
    public bool HasDraft => draft.HasDraft;
    public bool BlocksBuildInput => State != GameModeState.Building && State != GameModeState.Exploring;

    private void Start()
    {
        panel.Bind(this);
        panel.Show(false);
        worldLoading.Bind(null);
    }

    public void ShowSelection()
    {
        if (State != GameModeState.Building)
        {
            return;
        }

        if (BuildManager.instance.IsLoadingBlocks || BuildManager.instance.IsEditingDeveloperBlueprint)
        {
            return;
        }

        CaptureInput();
        State = GameModeState.Choosing;
        Status = "Choose a play mode. Your construction stays in Main.";
        panel.Show(true);
    }

    public void SelectExploration()
    {
        Select(true);
    }

    public void SelectSiege()
    {
        Select(false);
    }

    public void LaunchSiege(string blueprintName)
    {
        if (State != GameModeState.Building || transition != null || BuildManager.instance.IsLoadingBlocks)
        {
            return;
        }

        requestedSiegeDesign = blueprintName;
        CaptureInput();
        State = GameModeState.Choosing;
        Select(false);
    }

    private void Select(bool exploration)
    {
        if (transition != null || (State != GameModeState.Choosing && State != GameModeState.Failed))
        {
            return;
        }

        if (exploration && !PlayManager.instance.CanStartPlay(out string reason))
        {
            Status = reason;
            return;
        }

        explorationSelected = exploration;
        cancelRequested = false;
        State = GameModeState.Loading;
        Status = exploration ? "Loading exploration..." : "Loading Siege...";
        transition = StartCoroutine(GuardTransition(LoadMode()));
    }

    private IEnumerator LoadMode()
    {
        string path = explorationSelected ? explorationScene : siegeScene;
        if (!Application.CanStreamedLevelBeLoaded(path))
        {
            throw new InvalidOperationException("Scene is not enabled in Build Settings: " + path);
        }

        AsyncOperation loading = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
        while (!loading.isDone)
        {
            yield return null;
        }

        modeScene = SceneManager.GetSceneByPath(path);
        if (cancelRequested)
        {
            yield break;
        }

        // Main remains active: authored shared pools/services resolve there; world and ship roots have explicit ownership.
        if (explorationSelected)
        {
            world = SceneObjectQuery.FindInScene<WorldSessionManager>(modeScene);
            if (world == null)
            {
                throw new InvalidOperationException("Exploration scene has no WorldSessionManager.");
            }

            PlayManager.instance.BindWorldSession(world);
            outpostHud?.Bind(world);
            world.BindHost(buildEnvironment, worldLoading, this);
            world.BeginPreparation();
            world.RequestPlay();
            panel.Show(false);
            while (world.State != WorldSessionState.Failed && (!PlayManager.instance.playMode || world.IsEntering))
            {
                if (cancelRequested)
                {
                    yield break;
                }

                yield return null;
            }

            if (world.State == WorldSessionState.Failed)
            {
                throw new InvalidOperationException(world.Status);
            }

            State = GameModeState.Exploring;
            InputManager.instance.keyInput = previousKeyInput;
        }
        else
        {
            siege = SceneObjectQuery.FindInScene<SiegeGameplayStage>(modeScene);
            if (siege == null)
            {
                throw new InvalidOperationException("Siege gameplay scene has no stage entry.");
            }

            draft.Park();
            MainUIPanels.instance.buildPanel.SetActive(false);
            siege.Enter(this, buildEnvironment);
            if (!string.IsNullOrEmpty(requestedSiegeDesign))
            {
                siege.SelectDesignByName(requestedSiegeDesign);
            }

            panel.Show(false);
            State = GameModeState.Siege;
        }
    }

    // Called only after world/construct validation; failed selection never parks or alters the draft.
    public void CreateExplorationCopy()
    {
        draft.CreateRuntimeCopy(modeScene);
    }

    public void CaptureReturnCargo(bool survived)
    {
        draft.CaptureReturnCargo(survived);
    }

    public void ReturnToBuild()
    {
        if (State == GameModeState.Building || State == GameModeState.Returning)
        {
            return;
        }

        if (State == GameModeState.Exploring)
        {
            MainUIPanels.instance.PlayEnd();
            return;
        }

        cancelRequested = true;
        if (transition == null)
        {
            BeginReturn();
        }
    }

    public void ExplorationEnded()
    {
        cancelRequested = true;
        if (transition == null)
        {
            BeginReturn();
        }
    }

    public void Retry()
    {
        Select(explorationSelected);
    }

    private IEnumerator GuardTransition(IEnumerator work)
    {
        // Always yield once so the caller stores the coroutine before a synchronous failure starts cleanup.
        yield return null;
        // Every potentially throwing step is advanced here; yields are engine operations or frames, never nested iterators.
        Exception failure = null;
        while (true)
        {
            object current = null;
            bool more;
            try
            {
                more = work.MoveNext();
                if (more)
                {
                    current = work.Current;
                }
            }
            catch (Exception error)
            {
                failure = error;
                break;
            }

            if (!more)
            {
                break;
            }

            yield return current;
        }

        (work as IDisposable)?.Dispose();
        transition = null;
        if (failure != null)
        {
            Status = failure.Message;
            Debug.LogWarning("Mode entry failed: " + Status);
            cancelRequested = true;
        }

        if (cancelRequested)
        {
            BeginReturn(failure != null);
        }
    }

    private void BeginReturn(bool failed = false)
    {
        State = GameModeState.Returning;
        panel.Show(false);
        transition = StartCoroutine(ReturnRoutine(failed));
    }

    private IEnumerator ReturnRoutine(bool failed)
    {
        yield return null;
        // Do not allow unloading to destroy Main's draft or keep world preparation alive after cancellation.
        world?.StopSession();
        worldLoading.Bind(null);
        outpostHud?.Bind(null);
        siege?.Exit();
        PlayManager.instance.BindWorldSession(null);
        if (draft.HasDraft && (siege != null || PlayManager.instance.blocksParent != null || PlayManager.instance.playMode))
        {
            PlayManager.instance.PlayEnd();
        }

        if (modeScene.IsValid() && modeScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(modeScene);
        }

        SceneObjectQuery.FindInScene<VfxEffectPool>(gameObject.scene)?.ClearActive();
        SceneObjectQuery.FindInScene<TransientEffectPool>(gameObject.scene)?.ClearActive();
        world = null;
        siege = null;
        requestedSiegeDesign = null;
        modeScene = default;
        draft.Restore();
        MainUIPanels.instance.RestoreBuildPanels();
        RestoreInput();
        transition = null;
        cancelRequested = false;
        State = failed ? GameModeState.Failed : GameModeState.Building;
        panel.Show(failed);
        if (failed)
        {
            CaptureInput();
        }
    }

    private void CaptureInput()
    {
        if (!inputCaptured)
        {
            previousKeyInput = InputManager.instance.keyInput;
            previousBuildEnabled = BuildManager.instance.enabled;
            inputCaptured = true;
        }

        BuildManager.instance.DeselectBlock();
        BuildManager.instance.enabled = false;
        InputManager.instance.keyInput = false;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private void RestoreInput()
    {
        if (inputCaptured)
        {
            InputManager.instance.EnterBuildMode();
            InputManager.instance.keyInput = previousKeyInput;
            BuildManager.instance.enabled = previousBuildEnabled;
            inputCaptured = false;
        }
    }

    private void OnDestroy()
    {
        // Stopping Play Mode may destroy Main before its additive scenes; no scene loads or UI work during teardown.
        world?.StopSession();
        siege?.Exit();
    }
}
