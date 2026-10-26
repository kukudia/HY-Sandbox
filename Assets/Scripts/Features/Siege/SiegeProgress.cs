// 职责：记录阵地战的跨启动战果（最高波次与累计击毁），只写本地PlayerPrefs，不参与单局规则。
// 入口：TryRecordResult() / BestWave / TotalKills；由SiegeSession在失败时调用。
using UnityEngine;

public static class SiegeProgress
{
    private const string BestWaveKey = "siege.records.bestWave";
    private const string TotalKillsKey = "siege.records.totalKills";
    private const string TotalRunsKey = "siege.records.runs";
    public static int BestWave => PlayerPrefs.GetInt(BestWaveKey, 0);
    public static int TotalKills => PlayerPrefs.GetInt(TotalKillsKey, 0);
    public static int TotalRuns => PlayerPrefs.GetInt(TotalRunsKey, 0);

    /// <summary>失败时登记一次战果；只在波次或累计值提高时写盘，返回是否刷新了最高波次。</summary>
    public static bool TryRecordResult(int wave, int kills)
    {
        if (wave < 0 || kills < 0)
        {
            return false;
        }

        bool newRecord = wave > BestWave;
        if (newRecord)
        {
            PlayerPrefs.SetInt(BestWaveKey, wave);
        }

        PlayerPrefs.SetInt(TotalKillsKey, TotalKills + kills);
        PlayerPrefs.SetInt(TotalRunsKey, TotalRuns + 1);
        PlayerPrefs.Save();
        return newRecord;
    }

    /// <summary>仅供显式验证使用：把记录恢复到给定值，避免测试留下假成绩。</summary>
    public static void RestoreForProbe(int bestWave, int totalKills, int totalRuns)
    {
    PlayerPrefs.SetInt(BestWaveKey, Mathf.Max(0, bestWave));
        PlayerPrefs.SetInt(TotalKillsKey, Mathf.Max(0, totalKills));
        PlayerPrefs.SetInt(TotalRunsKey, Mathf.Max(0, totalRuns));
        PlayerPrefs.Save();
    }
}
