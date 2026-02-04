using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        Init();
    }

    public static void Init()
    {
        SaveUIPanel.instance.RefreshList();
        if (SaveManager.instance.saves.Count == 0) return;
        if (string.IsNullOrEmpty(SaveManager.instance.currentSaveName))
        {
            SaveManager.instance.LoadSave(SaveManager.instance.saves[0]);
        }
        else
        {
            SaveManager.instance.LoadSave(SaveManager.instance.currentSaveName);
        }
    }
}
