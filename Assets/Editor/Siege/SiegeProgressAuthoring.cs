// 职责：提供清除本机阵地战战果记录的显式菜单，便于测试或重新开始统计。
// 入口：ClearRecords()；只写本地PlayerPrefs，不触碰存档蓝图或场景。
using UnityEditor;
using UnityEngine;

public static class SiegeProgressAuthoring
{
    [MenuItem("HY-Sandbox/Siege/Clear Local Siege Records")]
    public static void ClearRecords()
    {
        int best = SiegeProgress.BestWave;
        int kills = SiegeProgress.TotalKills;
        int runs = SiegeProgress.TotalRuns;
        SiegeProgress.RestoreForProbe(0, 0, 0);
        Debug.Log("Cleared local siege records: best wave " + best + ", kills " + kills + ", runs " + runs + ".");
    }
}
