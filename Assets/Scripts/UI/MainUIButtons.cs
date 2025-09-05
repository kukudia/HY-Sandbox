using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainUIButtons : MonoBehaviour
{
    public static MainUIButtons instance;
    public Button undoButton;
    public Button redoButton;
    public Button actionClearButton;
    public Button deleteButton;
    public Button defaultButton;
    public Button moveButton;
    public Button rotateButton;
    public Button playButton;
    public Button exitButton;
    public Button showCreateButton;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;
    public List<BlockButton> blockButtons = new List<BlockButton>();

    private void Awake()
    {
        instance = this;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        foreach (BlockButton blockButton in blockButtons)
        {
            if (blockButton != null)
            {
                blockButton.name = blockButton.block.name;
                blockButton.button.GetComponentInChildren<Text>().text = blockButton.block.name;
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
        moveButton.onClick.AddListener(SetDefault);
        rotateButton.onClick.AddListener(SetDefault);
        moveButton.onClick.AddListener(SetMove);
        rotateButton.onClick.AddListener(SetRotate);
        playButton.onClick.AddListener(MainUIPanels.instance.PlayStart);
        exitButton.onClick.AddListener(MainUIPanels.instance.PlayEnd);
        showCreateButton.onClick.AddListener(MainUIPanels.instance.ShowCreatePanel);
        confirmCreateButton.onClick.AddListener(MainUIPanels.instance.OnConfirmCreate);
        cancelCreateButton.onClick.AddListener(MainUIPanels.instance.HideCreatePanel);
        cancelDeleteButton.onClick.AddListener(MainUIPanels.instance.HideDeletePanel);

        foreach (BlockButton blockButton in blockButtons)
        {
            if (blockButton != null)
            {
                if (blockButton.button != null)
                {
                    blockButton.button.onClick.AddListener(() => SetCurrentBlock(blockButton.block.name));
                }
            }
        }
    }

    public void SetDefault()
    {
        BuildManager.instance.currentBlockResourcePath = string.Empty;
    }

    public void SetMove()
    {
        BuildManager.instance.currentSelectType = SelectType.Move;
    }

    public void SetRotate()
    {
        BuildManager.instance.currentSelectType = SelectType.Rotate;
    }

    public void SetCurrentBlock(string fileName)
    {
        string resourcePath = "Blocks/" + fileName;
        BuildManager.instance.currentBlockResourcePath = resourcePath;
        if (BuildManager.instance.currentGhost != null)
        {
            Destroy(BuildManager.instance.currentGhost);
        }
        Debug.Log($"当前建造方块切换为 {resourcePath}");
    }
}

[System.Serializable]
public class BlockButton
{
    public string name;
    public Button button;
    public GameObject block;
}
