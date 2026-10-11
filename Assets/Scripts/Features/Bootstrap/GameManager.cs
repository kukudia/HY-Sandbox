// 职责：保存当前方块父对象，启动时初始化界面与默认存档加载。
// 入口：Init()。
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Transform blocksParent;
    private void Awake()
    {
        instance = this;
        VisualEffectsManager.EnsureInstance();
    }

    private void Start()
    {
        Init();
    }

    public static void Init()
    {
        SaveUIPanel.instance.RefreshList();
        if (SaveManager.instance.saves.Count == 0)
        {
            return;
        }

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
