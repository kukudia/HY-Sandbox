// 职责：保存阵地战阶段、波次名额、金币和失败状态；不依赖场景对象或物理引擎。
// 入口：BeginNextWave() / RegisterAttacker() / ResolveAttacker() / TrySpend() / LoseBase()。
// 约束：生成成功才消耗名额；同一敌机只结算一次；维修和部署只能发生在整备阶段。
using System;
using System.Collections.Generic;

public enum SiegePhase
{
    Deployment,
    Battle,
    Intermission,
    Defeat
}

public sealed class SiegeMatch
{
    private readonly HashSet<string> attackers = new HashSet<string>(StringComparer.Ordinal);
    private readonly int firstWaveSize;
    private readonly int waveGrowth;
    private readonly int maximumWaveSize;
    private readonly int killReward;
    private readonly int completionReward;
    public SiegePhase Phase { get; private set; } = SiegePhase.Deployment;
    public long Coins
    {
        get; private set;
    }
    public int Wave
    {
        get; private set;
    }
    public int PendingAttackers
    {
        get; private set;
    }
    public int ActiveAttackers => attackers.Count;
    public int DefeatedAttackers
    {
        get; private set;
    }
    public bool CanRefit => Phase == SiegePhase.Deployment || Phase == SiegePhase.Intermission;

    public SiegeMatch(
        int initialCoins,
        int firstWaveSize,
        int waveGrowth,
        int maximumWaveSize,
        int killReward,
        int completionReward)
    {
        if (initialCoins < 0 || firstWaveSize < 1 || waveGrowth < 0 || maximumWaveSize < firstWaveSize || killReward < 0 || completionReward < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialCoins),
                "Siege economy and wave settings must be nonnegative, with a positive bounded wave size.");
        }

        Coins = initialCoins;
        this.firstWaveSize = firstWaveSize;
        this.waveGrowth = waveGrowth;
        this.maximumWaveSize = maximumWaveSize;
        this.killReward = killReward;
        this.completionReward = completionReward;
    }

    public bool BeginNextWave()
    {
        if (!CanRefit || Wave == int.MaxValue)
        {
            return false;
        }

        Wave++;
        PendingAttackers = (int)Math.Min(maximumWaveSize, firstWaveSize + (long)(Wave - 1) * waveGrowth);
        Phase = SiegePhase.Battle;
        return true;
    }

    public bool CanSpawn(int maximumActive)
    {
        return Phase == SiegePhase.Battle && PendingAttackers > 0 && maximumActive > 0 && attackers.Count < maximumActive;
    }

    public bool RegisterAttacker(string id, int maximumActive)
    {
        if (string.IsNullOrWhiteSpace(id) || !CanSpawn(maximumActive) || !attackers.Add(id))
        {
            return false;
        }

        PendingAttackers--;
        return true;
    }

    /// <summary>只结算登记过的敌机；卸载或消失允许结束波次，但不给击杀收益。</summary>
    public bool ResolveAttacker(string id, bool defeated)
    {
        if (Phase != SiegePhase.Battle || string.IsNullOrEmpty(id) || !attackers.Remove(id))
        {
            return false;
        }

        if (defeated)
        {
            Coins += killReward;
            DefeatedAttackers++;
        }

        if (PendingAttackers == 0 && attackers.Count == 0)
        {
            Coins += completionReward;
            Phase = SiegePhase.Intermission;
        }

        return true;
    }

    public bool CanAfford(long price)
    {
        return CanRefit && price >= 0 && Coins >= price;
    }

    public bool TrySpend(long price)
    {
        if (!CanAfford(price))
        {
            return false;
        }

        Coins -= price;
        return true;
    }

    public void LoseBase()
    {
        Phase = SiegePhase.Defeat;
        PendingAttackers = 0;
        attackers.Clear();
    }
}
