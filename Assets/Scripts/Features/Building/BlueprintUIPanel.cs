// 职责：展示蓝图名称、数量、质量和功率；业务统计由 BlueprintStatistics 提供。
// 入口：Refresh() / DisplayStatistics() / UpdateCurrentSaveName()。
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

    /// <summary>刷新当前蓝图显示；统计业务由 Building 功能负责。</summary>
    public void Refresh()
    {
        if (BuildManager.instance == null || SaveManager.instance == null)
        {
            return;
        }

        UpdateCurrentSaveName(BuildManager.instance.CurrentBuildName);
        DisplayStatistics(BuildManager.instance.GetBlueprintStatistics());
    }

    public void DisplayStatistics(BlueprintStatistics statistics)
    {
        UpdateStatistics(statistics.BlockCount, statistics.Mass, statistics.RequiredPower, statistics.GeneratorOutput);
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
        float realMass = newMass / 10;
        if (totalMass != null)
        {
            totalMass.text = $"Total mass: {realMass:0.##} t";
        }
    }

    public void UpdateTotalRequiredPower(float newRequiredPower)
    {
        float realPower = newRequiredPower / 10;
        if (totalRequiredPower != null)
        {
            totalRequiredPower.text = $"Required power: {realPower:0.##} MJ";
        }
    }

    public void UpdateTotalGeneratorOutput(float newGeneratorOutput)
    {
        float realPower = newGeneratorOutput / 10;
        if (totalGeneratorOutput != null)
        {
            totalGeneratorOutput.text = $"Output power: {realPower:0.##} MJ";
        }
    }
}
