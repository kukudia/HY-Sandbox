using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlockSelection : MonoBehaviour
{
    public Button defaultButton;
    public List<BlockButton> blockButtons = new List<BlockButton>();

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
        defaultButton.onClick.AddListener(SetDefault);

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
