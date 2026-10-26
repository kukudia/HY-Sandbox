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
    private readonly int eliteEveryNWaves;
    private readonly int eliteRewardMultiplier;
    private readonly int swarmEveryNWaves;
    private readonly int swarmSizeMultiplier;
    /// <summary>精英波次按固定间隔出现；0表示关闭，保持既有线性波次。</summary>
    public bool IsEliteWave => eliteEveryNWaves > 0 && Wave > 0 && Wave % eliteEveryNWaves == 0;
    /// <summary>距离下一次精英波次还剩几波；0表示关闭精英或本身就是精英波。</summary>
    public int EliteInWaves => eliteEveryNWaves <= 0 || IsEliteWave ? 0 : eliteEveryNWaves - Wave % eliteEveryNWaves;
    /// <summary>蜂群波次：数量翻倍、生成更密；与精英波重叠时精英优先，避免两个倍率叠加。</summary>
    public bool IsSwarmWave => swarmEveryNWaves > 0 && Wave > 0 && Wave % swarmEveryNWaves == 0 && !IsEliteWave;
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

    /// <summary>整备阶段给UI使用的下一波规模预览；包含蜂群倍率，避免低估下一波。</summary>
    public int NextWaveSize
    {
        get
        {
            long baseSize = firstWaveSize + (long)Wave * waveGrowth;
            return (int)Math.Min(maximumWaveSize, NextWaveIsSwarm ? baseSize * swarmSizeMultiplier : baseSize);
        }
    }

    /// <summary>下一波是否为蜂群波；与精英重叠时按精英处理。</summary>
    public bool NextWaveIsSwarm => swarmEveryNWaves > 0 && (Wave + 1) % swarmEveryNWaves == 0 && !NextWaveIsElite;
    /// <summary>下一波是否为精英波。</summary>
    public bool NextWaveIsElite => eliteEveryNWaves > 0 && (Wave + 1) % eliteEveryNWaves == 0;
    public int ActiveAttackers => attackers.Count;
    public int DefeatedAttackers
    {
        get; private set;
    }
    /// <summary>HUD只读的单次击毁奖励；精英波次按倍数放大，供横幅与提示显示。</summary>
    public int KillReward => IsEliteWave ? killReward * eliteRewardMultiplier : killReward;
    public bool CanRefit => Phase == SiegePhase.Deployment || Phase == SiegePhase.Intermission;

    public SiegeMatch(
        int initialCoins,
        int firstWaveSize,
        int waveGrowth,
        int maximumWaveSize,
        int killReward,
        int completionReward,
        int eliteEveryNWaves = 0,
        int eliteRewardMultiplier = 2,
        int swarmEveryNWaves = 0,
        int swarmSizeMultiplier = 2)
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
        this.eliteEveryNWaves = Math.Max(0, eliteEveryNWaves);
        this.eliteRewardMultiplier = Math.Max(1, eliteRewardMultiplier);
        this.swarmEveryNWaves = Math.Max(0, swarmEveryNWaves);
        this.swarmSizeMultiplier = Math.Max(1, swarmSizeMultiplier);
    }

    public bool BeginNextWave()
    {
        if (!CanRefit || Wave == int.MaxValue)
        {
            return false;
        }

        Wave++;
        long baseSize = firstWaveSize + (long)(Wave - 1) * waveGrowth;
        // 蜂群波在整除后的波次把名额放大；精英波不加量，只换更重的蓝图与更高奖励。
        long scaledSize = swarmEveryNWaves > 0 && Wave % swarmEveryNWaves == 0 && (eliteEveryNWaves <= 0 || Wave % eliteEveryNWaves != 0) ? baseSize * swarmSizeMultiplier : baseSize;
        PendingAttackers = (int)Math.Min(maximumWaveSize, scaledSize);
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
            Coins += KillReward;
            DefeatedAttackers++;
        }

        if (PendingAttackers == 0 && attackers.Count == 0)
        {
            Coins += IsEliteWave ? completionReward * eliteRewardMultiplier : completionReward;
            Phase = SiegePhase.Intermission;
        }

        return true;
    }

    /// <summary>战斗与波次结算瞬间的额外奖励（连击）；部署阶段与失败后不发放。</summary>
    public bool GrantBonus(int coins)
    {
        if ((Phase != SiegePhase.Battle && Phase != SiegePhase.Intermission) || coins <= 0)
        {
            return false;
        }

        Coins += coins;
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
