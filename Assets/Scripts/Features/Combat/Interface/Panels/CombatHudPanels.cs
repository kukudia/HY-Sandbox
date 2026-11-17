// 职责：协调调试/推进器抽屉互斥、可取消过渡和调试状态恢复。
// 入口：Bind() / Restore() / ToggleDebug() / ToggleThrusters()；不修改战斗规则。
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class CombatHudPanels : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup debugWindow;
    [SerializeField]
    private Toggle[] debugToggles;
    [SerializeField]
    private ThrusterInfoPanel thrusters;
    [SerializeField]
    private Button debugButton;
    [SerializeField]
    private Button thrusterButton;
    [SerializeField]
    private CombatNotifications notifications;
    [SerializeField]
    private Dropdown factionFilter;
    private readonly bool[] previous = new bool[6];
    private int previousFilter;
    private bool captured;
    private bool debugOpen;
    private float debugAlpha;
    public bool IsOpen => debugOpen || (thrusters != null && thrusters.IsOpen);
    public bool BlocksCamera => IsOpen;

    private void Awake()
    {
        debugButton.onClick.AddListener(ToggleDebug);
        thrusterButton.onClick.AddListener(ToggleThrusters);
        for (int i = 0; i < debugToggles.Length; i++)
        {
            int index = i;
            debugToggles[i].onValueChanged.AddListener(value => SetDebug(index, value));
        }

        CloseAll();
        if (factionFilter != null)
        {
            factionFilter.onValueChanged.AddListener(value => DebugManager.instance?.SetFactionFilter(value));
        }
    }

    public void Bind()
    {
        if (!captured && DebugManager.instance != null)
        {
            for (int i = 0; i < previous.Length; i++)
            {
                previous[i] = GetDebug(i);
                debugToggles[i].SetIsOnWithoutNotify(previous[i]);
            }

            captured = true;
            previousFilter = (int)DebugManager.instance.Filter;
            factionFilter?.SetValueWithoutNotify(previousFilter);
        }
    }

    public void Restore()
    {
        CloseAll();
        if (captured && DebugManager.instance != null)
        {
            for (int i = 0; i < previous.Length; i++)
            {
                SetDebug(i, previous[i]);
            }

            DebugManager.instance.SetFactionFilter(previousFilter);
        }

        captured = false;
    }

    public void ToggleDebug()
    {
        Bind();
        debugOpen = !debugOpen;
        thrusters.Close();
    }

    public void ToggleThrusters()
    {
        debugOpen = false;
        thrusters.Toggle();
    }

    public void ClearDebug()
    {
        for (int i = 0; i < debugToggles.Length; i++)
        {
            debugToggles[i].isOn = false;
            SetDebug(i, false);
        }
    }

    public void CloseAll()
    {
        debugOpen = false;
        debugAlpha = 0f;
        debugWindow.alpha = 0f;
        debugWindow.blocksRaycasts = debugWindow.interactable = false;
        thrusters.Close();
    }

    private void OnDisable()
    {
        Restore();
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            return;
        }

        notifications.SetToolsOpen(IsOpen);
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            ToggleThrusters();
        }

        // Siege disables the legacy InputManager during observation; own this shortcut here for every mode.
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
        {
            ToggleDebug();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseAll();
        }

        debugAlpha = Mathf.MoveTowards(debugAlpha, debugOpen ? 1f : 0f, Time.unscaledDeltaTime / .18f);
        debugWindow.alpha = debugAlpha;
        debugWindow.blocksRaycasts = debugWindow.interactable = debugOpen;
        ((RectTransform)debugWindow.transform).anchoredPosition = new Vector2(-20f + (1f - debugAlpha) * 30f, -65f);
        for (int i = 0; i < debugToggles.Length; i++)
        {
            debugToggles[i].SetIsOnWithoutNotify(GetDebug(i));
        }
    }

    private bool GetDebug(int index)
    {
        DebugManager manager = DebugManager.instance;
        if (manager == null)
        {
            return false;
        }

        switch (index)
        {
            case 0:
                return manager.showPowerRange;
            case 1:
                return manager.showPowerConnections;
            case 2:
                return manager.showConnectionStatus;
            case 3:
                return manager.showDurabilityStatus;
            case 4:
                return manager.showPowerStatus;
            case 5:
                return manager.showFlightTrajectories;
            default:
                return false;
        }
    }

    private void SetDebug(int index, bool value)
    {
        DebugManager manager = DebugManager.instance;
        if (manager == null || GetDebug(index) == value)
        {
            return;
        }

        switch (index)
        {
            case 0:
                manager.TogglePowerRange();
                break;
            case 1:
                manager.TogglePowerConnections();
                break;
            case 2:
                manager.ToggleConnectionStatus();
                break;
            case 3:
                manager.ToggleDurabilityStatus();
                break;
            case 4:
                manager.TogglePowerStatus();
                break;
            case 5:
                manager.showFlightTrajectories = value;
                break;
        }
    }
}
