// 职责：采样输入并协调建造/游玩模式、快捷键和光标状态。
// 入口：EnterBuildMode()。
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Converts keyboard input into camera/build/play state transitions in one scene-level entry point.
    public static InputManager instance;
    public bool keyInput = true;
    public bool lockView = false;
    public bool DeveloperToolsAvailable
    {
        get
        {
#if UNITY_EDITOR
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        lockView = true;
        if (CameraController.instance != null)
        {
            CameraController.instance.currentMode = CameraMode.Lock;
        }

        ApplyCursorState(lockView);
    }

    private void Update()
    {
        if (!keyInput)
        {
            return;
        }

        // Guard scene dependencies first so startup and teardown frames remain no-op instead of throwing.
        //if (BuildManager.instance == null || PlayManager.instance == null || CameraController.instance == null)
        //{
        //    return;
        //}
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        SiegeSession siege = PlayManager.instance != null ? PlayManager.instance.SiegeSession : null;
        if (PlayManager.instance?.Modes?.State == GameModeState.Siege && siege == null)
        {
            // The production Siege stage owns spectator/pilot switching; do not cycle camera modes with Tab.
            bool released = keyboard.altKey.isPressed || MainUIPanels.instance.CombatHud.Panels.IsOpen;
            lockView = released;
            CameraController.instance.currentMode = released ? CameraMode.ThirdPersonLock : CameraMode.ThirdPerson;
            ApplyCursorState(released);
            return;
        }

        if (siege != null && !siege.IsEditingBlueprint)
        {
            PollSiege(siege, keyboard);
            return;
        }

        WorldSessionManager worldSession = PlayManager.instance != null ? PlayManager.instance.WorldSession : null;
        if (worldSession != null && worldSession.IsPlayRequested)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                worldSession.CancelPlayRequest();
            }

            return;
        }

        if (BuildManager.instance.IsLoadingBlocks)
        {
            return;
        }

        if (!PlayManager.instance.playMode && DeveloperToolsAvailable && keyboard.ctrlKey.isPressed && keyboard.altKey.isPressed)
        {
            if (keyboard.eKey.wasPressedThisFrame)
            {
                BuildManager.instance.ToggleEnemyBlueprintBuildMode();
                return;
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                BuildManager.instance.ToggleBuildingBlueprintBuildMode();
                return;
            }
        }

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            if (CameraController.instance.currentMode == CameraMode.FirstPerson)
            {
                CameraController.instance.currentMode = CameraMode.ThirdPerson;
            }
            else if (CameraController.instance.currentMode == CameraMode.ThirdPerson)
            {
                CameraController.instance.currentMode = CameraMode.FreeFly;
            }
            else
            {
                CameraController.instance.currentMode = CameraMode.FirstPerson;
            }
        }

        if (keyboard.fKey.wasPressedThisFrame)
        {
            if (BuildManager.instance?.selectedBlock != null)
            {
                CameraController.instance.FocusCameraOnBlock(BuildManager.instance.selectedBlock.gameObject);
            }
            else if (PlayManager.instance.blocksParent != null)
            {
                CameraController.instance.FocusCameraOnBlock(PlayManager.instance.blocksParent.gameObject);
            }
        }

        if (keyboard.pKey.wasPressedThisFrame)
        {
            if (!PlayManager.instance.playMode)
            {
                MainUIPanels.instance.PlayStart();
            }
            else
            {
                MainUIPanels.instance.PlayEnd();
            }
        }

        if (!PlayManager.instance.playMode)
        {
            // Camera navigation temporarily suspends editing; release restores the same selection/tool.
            bool editing = Mouse.current == null || !Mouse.current.rightButton.isPressed;
            if (lockView != editing)
            {
                lockView = editing;
                CameraController.instance.currentMode = lockView ? CameraMode.Lock : CameraMode.FreeFly;
                BuildManager.instance.SetBuildMode(lockView);
                ApplyCursorState(lockView);
            }
        }
        else
        {
            bool altHeld = keyboard.altKey.isPressed;
            bool toolsOpen = MainUIPanels.instance != null && MainUIPanels.instance.CombatHud != null && MainUIPanels.instance.CombatHud.Panels.IsOpen;
            lockView = altHeld || toolsOpen;
            CameraController.instance.currentMode = lockView ? CameraMode.ThirdPersonLock : CameraMode.ThirdPerson;
            PlayManager.instance.SetPlayMode(lockView);
            ApplyCursorState(lockView);
        }
    }

    public void EnterBuildMode()
    {
        lockView = true;
        if (CameraController.instance != null)
        {
            CameraController.instance.currentMode = CameraMode.Lock;
        }

        if (BuildManager.instance != null)
        {
            BuildManager.instance.SetBuildMode(true);
        }

        ApplyCursorState(lockView);
    }

    private void PollSiege(SiegeSession siege, Keyboard keyboard)
    {
        if (keyboard.bKey.wasPressedThisFrame)
        {
            siege.Workshop?.Open();
            return;
        }

        if (keyboard.pKey.wasPressedThisFrame)
        {
            siege.BeginNextWave();
        }

        if (keyboard.fKey.wasPressedThisFrame && PlayManager.instance.blocksParent != null)
        {
            CameraController.instance.FocusCameraOnBlock(PlayManager.instance.blocksParent.gameObject);
        }

        bool toolsOpen = MainUIPanels.instance != null && MainUIPanels.instance.CombatHud != null && MainUIPanels.instance.CombatHud.Panels.IsOpen;
        bool release = toolsOpen || siege.Match == null || siege.Match.CanRefit || siege.Fleet.Driven == null || keyboard.altKey.isPressed;
        lockView = release;
        CameraController.instance.currentMode = release ? CameraMode.ThirdPersonLock : CameraMode.ThirdPerson;
        PlayManager.instance.SetPlayMode(release);
        ApplyCursorState(release);
    }

    private void ApplyCursorState(bool showCursor)
    {
        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.Confined : CursorLockMode.Locked;
    }
}
