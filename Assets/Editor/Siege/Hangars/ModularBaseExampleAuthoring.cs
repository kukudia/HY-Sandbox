// 职责：以可复现配方重做官方机库示例；统一更新三套真实示例资产并保留带时间戳备份。
// 入口：Generate()；不清空个人基地库，核心ID与官方菜单引用保持稳定。
using UnityEditor;

public static class ModularBaseExampleAuthoring
{
    [MenuItem("HY-Sandbox/Siege/Hangars/Rebuild Modular Base Example")]
    public static string Generate()
    {
        return SiegeBaseExamplesAuthoring.Generate();
    }
}
