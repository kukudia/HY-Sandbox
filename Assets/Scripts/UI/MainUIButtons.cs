using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainUIButtons : MonoBehaviour
{
    public Button undoButton;
    public Button redoButton;
    public Button deleteButton;
    public Button defaultButton;
    public Button moveButton;
    public Button rotateButton;
    public Button showCreateButton;
    public Button confirmButton;
    public Button cancelButton;
    public List<BlockButton> blockButtons = new List<BlockButton>();

    private MainUIPanels mainUIPanels;

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
        mainUIPanels = GetComponent<MainUIPanels>();

        undoButton.onClick.AddListener(() => ActionManager.instance.Undo());
        redoButton.onClick.AddListener(() => ActionManager.instance.Redo());
        deleteButton.onClick.AddListener(() => BuildManager.instance.DeleteBlock());
        defaultButton.onClick.AddListener(SetDefault);
        moveButton.onClick.AddListener(SetDefault);
        rotateButton.onClick.AddListener(SetDefault);
        moveButton.onClick.AddListener(SetMove);
        rotateButton.onClick.AddListener(SetRotate);

        showCreateButton.onClick.AddListener(mainUIPanels.ShowCreatePanel);
        confirmButton.onClick.AddListener(mainUIPanels.OnConfirm);
        cancelButton.onClick.AddListener(mainUIPanels.OnCancel);

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
