// 职责：绑定主界面按钮到建造、游玩和存档动作。
// 入口：SetDefault() / SetMove() / SetRotate()。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainUIButtons : MonoBehaviour
{
    public static MainUIButtons instance;
    [Header("Action Buttons")]
    public Button undoButton;
    public Button redoButton;
    public Button actionClearButton;
    public Button deleteButton;
    public Button defaultButton;
    public Button moveButton;
    public Button rotateButton;
    [Header("Build Mode Colors")]
    [SerializeField]
    private Color _inactiveBuildModeColor = Color.white;
    [SerializeField]
    private Color _activeBuildModeColor = new Color(0.25f, 0.85f, 1f, 1f);
    [Header("Debug Buttons")]
    public Button showPowerRangeButton;
    public Button showPowerConnectionsButton;
    public Button showConnectionStatusButton;
    public Button showDurabilityStatusButton;
    public Button showPowerStatusButton;
    [Header("Play Buttons")]
    public Button playButton;
    public Button exitButton;
    [Header("Edit Buttons")]
    public Button showCreateButton;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;
    [Header("Death Buttons")]
    public Button backToMenuButton;
    public Button respawnButton;
    public List<BlockButton> blockButtons = new List<BlockButton>();
    private BuildType _displayedBuildType;
    private bool _hasDisplayedBuildType;
    private void OnEnable()
    {
        _hasDisplayedBuildType = false;
    }

    private void LateUpdate()
    {
        if (BuildManager.instance != null && (!_hasDisplayedBuildType || _displayedBuildType != BuildManager.instance.currentBuildType))
        {
            RefreshBuildModeButtons(BuildManager.instance.currentBuildType);
        }
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        foreach (BlockButton blockButton in blockButtons)
        {
            if (blockButton != null && blockButton.block != null && blockButton.button != null)
            {
                blockButton.name = blockButton.block.name;
                blockButton.button.name = blockButton.block.name + "Button";
            }
        }
#endif
    }

    private void Start()
    {
        undoButton.onClick.AddListener(ActionManager.instance.Undo);
        redoButton.onClick.AddListener(ActionManager.instance.Redo);
        actionClearButton.onClick.AddListener(ActionManager.instance.Clear);
        deleteButton.onClick.AddListener(BuildManager.instance.DeleteBlock);
        defaultButton.onClick.AddListener(SetDefault);
        moveButton.onClick.AddListener(SetMove);
        rotateButton.onClick.AddListener(SetRotate);
        showPowerRangeButton.onClick.AddListener(ShowPowerRange);
        showPowerConnectionsButton.onClick.AddListener(ShowPowerConnections);
        showConnectionStatusButton.onClick.AddListener(ShowConnectionStatus);
        showDurabilityStatusButton.onClick.AddListener(ShowDurabilityStatus);
        showPowerStatusButton.onClick.AddListener(ShowPowerStatus);
        playButton.onClick.AddListener(MainUIPanels.instance.PlayStart);
        exitButton.onClick.AddListener(MainUIPanels.instance.PlayEnd);
        showCreateButton.onClick.AddListener(MainUIPanels.instance.ShowCreatePanel);
        confirmCreateButton.onClick.AddListener(MainUIPanels.instance.OnConfirmCreate);
        cancelCreateButton.onClick.AddListener(MainUIPanels.instance.HideCreatePanel);
        cancelDeleteButton.onClick.AddListener(MainUIPanels.instance.HideDeletePanel);
        //backToMenuButton;
        respawnButton.onClick.AddListener(MainUIPanels.instance.PlayEnd);
        foreach (BlockButton blockButton in blockButtons)
        {
            if (blockButton?.button != null && blockButton.block != null)
            {
                string blockName = blockButton.block.name;
                blockButton.button.onClick.AddListener(() => SetCurrentBlock(blockName));
            }
        }
    }

    public void SetDefault()
    {
        BuildManager.instance.SetCurrentBlockResource(string.Empty);
        BuildManager.instance.SetBuildType(BuildType.Select);
    }

    public void SetMove()
    {
        BuildManager.instance.SetCurrentBlockResource(string.Empty);
        BuildManager.instance.SetBuildType(BuildType.Move);
    }

    public void SetRotate()
    {
        BuildManager.instance.SetCurrentBlockResource(string.Empty);
        BuildManager.instance.SetBuildType(BuildType.Rotate);
    }

    public void RefreshBuildModeButtons(BuildType buildType)
    {
        _displayedBuildType = buildType;
        _hasDisplayedBuildType = true;
        SetBuildButtonColor(defaultButton, buildType == BuildType.Select);
        SetBuildButtonColor(moveButton, buildType == BuildType.Move);
        SetBuildButtonColor(rotateButton, buildType == BuildType.Rotate);
    }

    private void SetBuildButtonColor(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Color color = selected ? _activeBuildModeColor : _inactiveBuildModeColor;
        // Selectable 的焦点/悬停状态不能把模式色换成另一个模式的反馈。
        // 模式色交给 ColorTint，底图保持白色以避免两次相乘导致颜色变暗。
        if (button.transition == Selectable.Transition.ColorTint && button.targetGraphic != null)
        {
            button.targetGraphic.color = Color.white;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.selectedColor = color;
            colors.pressedColor = new Color(color.r * 0.85f, color.g * 0.85f, color.b * 0.85f, color.a);
            button.colors = colors;
        }
        else if (button.targetGraphic != null)
        {
            button.targetGraphic.color = color;
        }
    }

    public void SetCurrentBlock(string fileName)
    {
        string resourcePath = "Blocks/" + fileName;
        BuildManager.instance.SetCurrentBlockResource(resourcePath);
        Debug.Log($"Current build block changed to {resourcePath}");
    }

    private void ShowPowerRange()
    {
        if (DebugManager.instance == null)
        {
            return;
        }

        DebugManager.instance.TogglePowerRange();
        if (showPowerRangeButton != null)
        {
            showPowerRangeButton.GetComponent<Image>().color = DebugManager.instance.showPowerRange ? Color.green : Color.white;
        }
    }

    private void ShowPowerConnections()
    {
        if (DebugManager.instance == null)
        {
            return;
        }

        DebugManager.instance.TogglePowerConnections();
        if (showPowerConnectionsButton != null)
        {
            showPowerConnectionsButton.GetComponent<Image>().color = DebugManager.instance.showPowerConnections ? Color.green : Color.white;
        }
    }

    private void ShowConnectionStatus()
    {
        if (DebugManager.instance == null)
        {
            return;
        }

        DebugManager.instance.ToggleConnectionStatus();
        SetDebugButtonState(showConnectionStatusButton, DebugManager.instance.showConnectionStatus);
    }

    private void ShowDurabilityStatus()
    {
        if (DebugManager.instance == null)
        {
            return;
        }

        DebugManager.instance.ToggleDurabilityStatus();
        SetDebugButtonState(showDurabilityStatusButton, DebugManager.instance.showDurabilityStatus);
    }

    private void ShowPowerStatus()
    {
        if (DebugManager.instance == null)
        {
            return;
        }

        DebugManager.instance.TogglePowerStatus();
        SetDebugButtonState(showPowerStatusButton, DebugManager.instance.showPowerStatus);
    }

    private static void SetDebugButtonState(Button button, bool enabled)
    {
        if (button != null && button.TryGetComponent(out Image image))
        {
            image.color = enabled ? Color.green : Color.white;
        }
    }
}

[System.Serializable]
public class BlockButton
{
    public string name;
    public Button button;
    public GameObject block;
}
