// 职责：限制阵地战场景中敌方无驾驶舱碎片的存量和寿命，分批沿既有回收路径退场。
// 入口：Tick()；只处理同场景且所有模块仍属敌方的碎片，不影响友舰维修、建筑或存活驾驶舱。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SiegeWreckBudget
{
    private readonly Scene scene;
    private readonly Dictionary<ControlUnit, float> firstSeen = new Dictionary<ControlUnit, float>();
    private readonly List<ControlUnit> stale = new List<ControlUnit>();
    private readonly List<Candidate> candidates = new List<Candidate>();
    private readonly List<Block> blocks = new List<Block>();
    private readonly HashSet<ControlUnit> observed = new HashSet<ControlUnit>();
    private float nextScan;
    public int PendingGroups
    {
        get; private set;
    }
    public int PendingBlocks
    {
        get; private set;
    }
    public int RetiredGroups
    {
        get; private set;
    }

    public SiegeWreckBudget(Scene scene)
    {
        this.scene = scene;
    }

    public void Tick(float time, int maximumGroups, int maximumBlocks, float lifetime, int retirePerScan)
    {
        if (time < nextScan)
        {
            return;
        }

        nextScan = time + 0.5f;
        candidates.Clear();
        observed.Clear();
        PendingGroups = 0;
        PendingBlocks = 0;
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (!Eligible(unit))
            {
                continue;
            }

            observed.Add(unit);
            if (!firstSeen.TryGetValue(unit, out float started))
            {
                started = time;
                firstSeen.Add(unit, started);
            }

            candidates.Add(new Candidate { Unit = unit, Started = started, Blocks = blocks.Count });
            PendingGroups++;
            PendingBlocks += blocks.Count;
        }

        stale.Clear();
        foreach (ControlUnit unit in firstSeen.Keys)
        {
            if (!observed.Contains(unit))
            {
                stale.Add(unit);
            }
        }

        foreach (ControlUnit unit in stale)
        {
            firstSeen.Remove(unit);
        }

        candidates.Sort((a, b) => a.Started.CompareTo(b.Started));
        int retired = 0;
        foreach (Candidate candidate in candidates)
        {
            if (retired >= Mathf.Max(1, retirePerScan))
            {
                break;
            }

            bool expired = time - candidate.Started >= Mathf.Max(0.5f, lifetime);
            if (!expired && PendingGroups <= Mathf.Max(0, maximumGroups) && PendingBlocks <= Mathf.Max(0, maximumBlocks))
            {
                continue;
            }

            // 复用转换的幂等标记，避免与既有延迟清理重复掉落。先退场再延迟Destroy，立即释放活动物理预算。
            WreckSalvage.ConvertGroup(candidate.Unit);
            candidate.Unit.gameObject.SetActive(false);
            Object.Destroy(candidate.Unit.gameObject);
            firstSeen.Remove(candidate.Unit);
            PendingGroups--;
            PendingBlocks -= candidate.Blocks;
            RetiredGroups++;
            retired++;
        }
    }

    private bool Eligible(ControlUnit unit)
    {
        if (unit == null || !unit.gameObject.activeInHierarchy || unit.gameObject.scene != scene || unit.Building != null || unit.faction != UnitFaction.Enemy || unit.HasAnyCockpit)
        {
            return false;
        }

        blocks.Clear();
        unit.GetComponentsInChildren(false, blocks);
        if (blocks.Count == 0)
        {
            return false;
        }

        foreach (Block block in blocks)
        {
            RuntimeUnitMember member = block.GetComponent<RuntimeUnitMember>();
            if (member == null || member.ownerFaction != UnitFaction.Enemy)
            {
                return false;
            }
        }

        return true;
    }

    private struct Candidate
    {
        public ControlUnit Unit;
        public float Started;
        public int Blocks;
    }
}
