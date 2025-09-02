using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        SaveUIPanel.instance.RefreshList();
        if (SaveManager.instance.saves.Count > 0)
        {
            SaveManager.instance.LoadSave(SaveManager.instance.saves[0]);
        }
    }
}
