using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Converts keyboard input into camera/build/play state transitions in one scene-level entry point.
    public static InputManager instance;
    public bool lockView = false;
    public bool DeveloperToolsAvailable
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
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

        ApplyCursorState();
    }

    private void Update()
    {
        // Guard scene dependencies first so startup and teardown frames remain no-op instead of throwing.
        if (BuildManager.instance == null || PlayManager.instance == null || CameraController.instance == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (BuildManager.instance.IsLoadingBlocks)
        {
            return;
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

        if (!PlayManager.instance.playMode)
        {
            if (keyboard.bKey.wasPressedThisFrame)
            {
                lockView = !lockView;

                CameraController.instance.currentMode = lockView ? CameraMode.Lock : CameraMode.FreeFly;
                BuildManager.instance.SetBuildMode(lockView);
                ApplyCursorState();
            }

            if (DeveloperToolsAvailable
                && keyboard.leftCtrlKey.isPressed
                && keyboard.leftShiftKey.isPressed
                && keyboard.eKey.wasPressedThisFrame)
            {
                BuildManager.instance.ToggleEnemyBlueprintBuildMode();
            }
        }
        else
        {
            lockView = keyboard.altKey.isPressed;
            CameraController.instance.currentMode = lockView ? CameraMode.ThirdPersonLock : CameraMode.ThirdPerson;
            PlayManager.instance.SetPlayMode(lockView);
            ApplyCursorState();
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

        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.visible = lockView;
        Cursor.lockState = lockView ? CursorLockMode.Confined : CursorLockMode.Locked;
    }
}
