using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
            if (blockButton != null && blockButton.block != null && blockButton.button != null)
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
            if (blockButton?.button != null && blockButton.block != null)
            {
                string blockName = blockButton.block.name;
                blockButton.button.onClick.AddListener(() => SetCurrentBlock(blockName));
            }
        }

        RegisterDiscoveredBlockButtons();
    }

    private void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
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
        Debug.Log($"Current build block changed to {resourcePath}");
    }

    private void RegisterDiscoveredBlockButtons()
    {
        if (blockButtons.Count == 0 || blockButtons[0].button == null) return;

        Button template = blockButtons[0].button;
        Transform parent = template.transform.parent;
        HashSet<string> existingNames = new HashSet<string>();

        foreach (BlockButton blockButton in blockButtons)
        {
            if (blockButton != null && !string.IsNullOrEmpty(blockButton.name))
            {
                existingNames.Add(blockButton.name);
            }
        }

        GameObject[] prefabs = Resources.LoadAll<GameObject>("Blocks");
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null || existingNames.Contains(prefab.name)) continue;

            Button button = Instantiate(template, parent);
            button.name = prefab.name + "Button";

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = prefab.name;
            }

            string blockName = prefab.name;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SetCurrentBlock(blockName));

            blockButtons.Add(new BlockButton
            {
                name = blockName,
                button = button,
                block = prefab
            });

            existingNames.Add(blockName);
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
