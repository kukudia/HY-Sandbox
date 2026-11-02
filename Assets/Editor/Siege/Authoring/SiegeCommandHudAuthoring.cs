// 职责：保留旧菜单入口，统一委托当前Siege界面制作工具，避免重新生成已移除的Bays。
// 入口：Apply()。
using UnityEditor;

public static class SiegeCommandHudAuthoring
{
    [MenuItem("HY-Sandbox/Siege/Apply Command HUD")]
    public static void Apply()
    {
        SiegeInterfaceAuthoring.Apply();
    }
}
