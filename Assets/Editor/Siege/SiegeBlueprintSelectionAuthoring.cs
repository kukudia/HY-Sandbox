// 职责：保留旧蓝图选择菜单并委托当前界面迁移，避免恢复固定Bays布局。
// 入口：Install()。
using UnityEditor;

public static class SiegeBlueprintSelectionAuthoring
{
    [MenuItem("HY-Sandbox/Siege/Install Blueprint Selection")]
    public static void Install()
    {
        SiegeInterfaceAuthoring.Apply();
    }
}
