using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlueprintUIPanel : MonoBehaviour
{
    public static BlueprintUIPanel instance;

    public Text currentSaveName;
    public Text totalNumber;
    public Text totalMass;
    public Text totalRequiredPower;
    public Text totalGeneratorOutput;

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
            UpdateTotalRequiredPower(0f);
            UpdateTotalGeneratorOutput(0f);
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
        float requiredPower = 0f;
        float generatorOutput = 0f;
        Transform blocksParent = GameManager.instance != null ? GameManager.instance.blocksParent : null;
        if (blocksParent != null)
        {
            Block[] loadedBlocks = blocksParent.GetComponentsInChildren<Block>(true);
            foreach (Block block in loadedBlocks)
            {
                if (block != null && cachedBlockIds.Contains(block.uniqueId))
                {
                    mass += block.mass;

                    Power power = block.GetComponent<Power>();
                    if (power != null)
                    {
                        requiredPower += Mathf.Max(0f, power.standardWorkingPower);
                    }

                    PowerGeneratingUnit generator = block.GetComponent<PowerGeneratingUnit>();
                    if (generator != null)
                    {
                        generatorOutput += Mathf.Max(0f, generator.outputPower);
                    }
                }
            }
        }

        UpdateTotalNumber(cachedBlocks.Count);
        UpdateTotalMass(mass);
        UpdateTotalRequiredPower(requiredPower);
        UpdateTotalGeneratorOutput(generatorOutput);
    }

    public void UpdateCurrentSaveName(string newName)
    {
        if (currentSaveName != null)
        {
            currentSaveName.text = newName;
        }
    }

    public void UpdateStatistics(int blockCount, float mass, float requiredPower, float generatorOutput)
    {
        UpdateTotalNumber(blockCount);
        UpdateTotalMass(mass);
        UpdateTotalRequiredPower(requiredPower);
        UpdateTotalGeneratorOutput(generatorOutput);
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

    public void UpdateTotalRequiredPower(float newRequiredPower)
    {
        if (totalRequiredPower != null)
        {
            totalRequiredPower.text = $"Required power: {newRequiredPower:0.##}";
        }
    }

    public void UpdateTotalGeneratorOutput(float newGeneratorOutput)
    {
        if (totalGeneratorOutput != null)
        {
            totalGeneratorOutput.text = $"Generator output: {newGeneratorOutput:0.##}";
        }
    }
}
