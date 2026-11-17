// 职责：拥有暂停期间的时间、音频和输入快照，恢复后经既有导航主链返回或重新开始。
// 入口：Toggle() / Resume() / Restart()；预挂于工坊服务，界面由Editor保存。
using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;
    [SerializeField]
    private Button resume;
    [SerializeField]
    private Button restart;
    [SerializeField]
    private Button mainMenu;
    [SerializeField]
    private WorkshopNavigation navigation;
    private float previousTimeScale;
    private bool previousAudioPause;
    private bool previousInput;
    private bool previousBuild;
    private bool previousCamera;
    private CursorLockMode previousLock;
    private bool previousCursor;
    public static bool IsPaused
    {
        get; private set;
    }

    private void Awake()
    {
        resume.onClick.AddListener(Resume);
        restart.onClick.AddListener(Restart);
        mainMenu.onClick.AddListener(ReturnToMenu);
        panel.SetActive(false);
    }

    public void Toggle()
    {
        if (IsPaused)
        {
            Resume();
            return;
        }

        if (BuildManager.instance.IsLoadingBlocks || PlayManager.instance.Modes.State == GameModeState.Loading || PlayManager.instance.Modes.State == GameModeState.Returning)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousInput = InputManager.instance.keyInput;
        previousBuild = BuildManager.instance.enabled;
        previousCamera = CameraController.instance.enabled;
        previousLock = Cursor.lockState;
        previousCursor = Cursor.visible;
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        InputManager.instance.keyInput = false;
        BuildManager.instance.enabled = false;
        CameraController.instance.StopCameraMotion();
        CameraController.instance.enabled = false;
        MainUIPanels.instance.CombatHud?.Panels.CloseAll();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        restart.gameObject.SetActive(PlayManager.instance.Modes.State == GameModeState.Exploring || PlayManager.instance.Modes.State == GameModeState.Siege);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        InputManager.instance.keyInput = previousInput;
        BuildManager.instance.enabled = previousBuild;
        CameraController.instance.enabled = previousCamera;
        Cursor.lockState = previousLock;
        Cursor.visible = previousCursor;
        panel.SetActive(false);
    }

    public void Restart()
    {
        Resume();
        PlayManager.instance.Modes.Restart();
    }

    private void ReturnToMenu()
    {
        Resume();
        navigation.OpenMenu();
    }

    private void OnDisable()
    {
        Resume();
    }
}
