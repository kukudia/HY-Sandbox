// 职责：保存当前方块父对象；直接Main启动默认存档，菜单启动时由显式蓝图请求接管初始化。
// 入口：Init()。
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Transform blocksParent;
    [SerializeField]
    private WorkshopNavigation navigation;
    private void Awake()
    {
        instance = this;
        VisualEffectsManager.EnsureInstance();
    }

    private void Start()
    {
        if (navigation == null || !navigation.MenuHosted)
        {
            Init();
        }
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
