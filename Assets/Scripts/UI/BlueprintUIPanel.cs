using UnityEngine;
using UnityEngine.UI;

public class BlueprintUIPanel : MonoBehaviour
{
    public Text currentSaveName;
    public Text totalNumber;
    public Text totalMass;

    public void UpdateCurrentSaveName(string newName)
    {
        currentSaveName.text = newName;
    }

    public void UpdateTotalNumber(int newNumber)
    {
        totalNumber.text = $"Total number: {newNumber} blocks";
    }

    public void UpdateTotalMass(float newMass)
    {
        totalMass.text = $"Total mass: {newMass} kg";
    }
}
