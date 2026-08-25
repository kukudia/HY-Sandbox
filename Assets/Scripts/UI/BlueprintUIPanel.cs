using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlueprintUIPanel : MonoBehaviour
{
    public static BlueprintUIPanel instance;

    public Text currentSaveName;
    public Text totalNumber;
    public Text totalMass;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Refresh()
    {
        if (BuildManager.instance == null || SaveManager.instance == null)
        {
            return;
        }

        UpdateCurrentSaveName(BuildManager.instance.CurrentBuildName);

        List<BlockData> cachedBlocks = SaveManager.instance.cachedData?.blocks;
        if (cachedBlocks == null)
        {
            UpdateTotalNumber(0);
            UpdateTotalMass(0f);
            return;
        }

        HashSet<string> cachedBlockIds = new HashSet<string>();
        foreach (BlockData data in cachedBlocks)
        {
            if (data != null && !string.IsNullOrEmpty(data.id))
            {
                cachedBlockIds.Add(data.id);
            }
        }

        float mass = 0f;
        Transform blocksParent = GameManager.instance != null ? GameManager.instance.blocksParent : null;
        if (blocksParent != null)
        {
            Block[] loadedBlocks = blocksParent.GetComponentsInChildren<Block>(true);
            foreach (Block block in loadedBlocks)
            {
                if (block != null && cachedBlockIds.Contains(block.uniqueId))
                {
                    mass += block.mass;
                }
            }
        }

        UpdateTotalNumber(cachedBlocks.Count);
        UpdateTotalMass(mass);
    }

    public void UpdateCurrentSaveName(string newName)
    {
        if (currentSaveName != null)
        {
            currentSaveName.text = newName;
        }
    }

    public void UpdateTotalNumber(int newNumber)
    {
        if (totalNumber != null)
        {
            totalNumber.text = $"Total number: {newNumber} blocks";
        }
    }

    public void UpdateTotalMass(float newMass)
    {
        if (totalMass != null)
        {
            totalMass.text = $"Total mass: {newMass:0.##} kg";
        }
    }
}
