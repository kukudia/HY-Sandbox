// 职责：在返航/整备期展示场地扫描并分批回收敌方残骸，保留最短物理展示期与既有掉落规则。
// 入口：Begin()/Tick()/Stop()；由宿主显式调度，无额外金币、自动修复或场景外清理。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeCleanupSweep : MonoBehaviour
{
    private static readonly HashSet<SiegeCleanupSweep> activeSweeps = new HashSet<SiegeCleanupSweep>();
    public static bool OwnsRetirement(ControlUnit unit)
    {
        foreach (SiegeCleanupSweep sweep in activeSweeps)
        {
            if (sweep != null && sweep.IsActive && EnemyWreckRetirement.Eligible(unit, sweep.gameObject.scene, sweep.retirementChecks))
            {
                return true;
            }
        }

        return false;
    }

    [SerializeField]
    private LineRenderer scanLine;
    [SerializeField]
    private LineRenderer perimeter;
    [SerializeField]
    private Text status;
    [SerializeField]
    private MaterialBreakEffect salvageEffect;
    [SerializeField, Min(.5f)]
    private float sweepSeconds = 4f;
    [SerializeField, Min(.05f)]
    private float scanInterval = .25f;
    [SerializeField, Min(1)]
    private int groupsPerScan = 6;
    private readonly Dictionary<ControlUnit, float> firstSeen = new Dictionary<ControlUnit, float>();
    private readonly List<ControlUnit> candidates = new List<ControlUnit>();
    private readonly List<Block> blocks = new List<Block>();
    private readonly List<Block> retirementChecks = new List<Block>();
    private Bounds bounds;
    private float started;
    private float nextScan;
    private int wave;
    public bool IsActive
    {
        get; private set;
    }
    public int RetiredGroups
    {
        get; private set;
    }
    public int PendingGroups
    {
        get; private set;
    }
    public int RetiredBlocks
    {
        get; private set;
    }

    public void Begin(int completedWave, Bounds area, float time)
    {
        if (completedWave <= wave)
        {
            return;
        }

        wave = completedWave;
        bounds = area;
        started = time;
        nextScan = time;
        firstSeen.Clear();
        RetiredGroups = RetiredBlocks = PendingGroups = 0;
        IsActive = true;
        activeSweeps.Add(this);
        if (perimeter != null)
        {
            perimeter.SetPositions(new[] { new Vector3(bounds.min.x, bounds.center.y + .4f, bounds.min.z), new Vector3(bounds.max.x, bounds.center.y + .4f, bounds.min.z), new Vector3(bounds.max.x, bounds.center.y + .4f, bounds.max.z), new Vector3(bounds.min.x, bounds.center.y + .4f, bounds.max.z) });
        }

        Show(true);
    }

    public void Tick(float time)
    {
        if (!IsActive)
        {
            return;
        }

        float duration = Mathf.Max(.5f, sweepSeconds);
        float progress = Mathf.Clamp01((time - started) / duration);
        float z = Mathf.Lerp(bounds.min.z, bounds.max.z, Mathf.Repeat((time - started) / duration, 1f));
        if (scanLine != null)
        {
            scanLine.SetPosition(0, new Vector3(bounds.min.x, bounds.center.y + .55f, z));
            scanLine.SetPosition(1, new Vector3(bounds.max.x, bounds.center.y + .55f, z));
        }

        if (time >= nextScan)
        {
            nextScan = time + Mathf.Max(.05f, scanInterval);
            Scan(time, progress);
        }

        if (status != null)
        {
            status.text = $"SALVAGE SCAN  ·  WAVE {wave}  ·  {Mathf.RoundToInt(progress * 100)}%\n{RetiredGroups} WRECKS / {RetiredBlocks} MODULES RECYCLED  ·  {PendingGroups} RETAINED\nFRIENDLY WRECKS PRESERVED  /  REPAIRS REMAIN PAID";
        }

        if (progress >= 1f && PendingGroups == 0)
        {
            IsActive = false;
            activeSweeps.Remove(this);
            if (scanLine != null)
            {
                scanLine.enabled = false;
            }

            if (perimeter != null)
            {
                perimeter.enabled = false;
            }

            if (status != null)
            {
                status.text = $"SALVAGE COMPLETE  ·  {RetiredGroups} WRECKS / {RetiredBlocks} MODULES\nDROPS REMAIN ON FIELD  /  FRIENDLY WRECKS PRESERVED";
            }
        }
    }

    private void Scan(float time, float progress)
    {
        candidates.Clear();
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (EnemyWreckRetirement.Eligible(unit, gameObject.scene, blocks))
            {
                candidates.Add(unit);
            }
        }

        PendingGroups = candidates.Count;
        int retired = 0;
        foreach (ControlUnit unit in candidates)
        {
            if (!firstSeen.TryGetValue(unit, out float seen))
            {
                firstSeen[unit] = seen = time;
            }

            if (retired >= Mathf.Max(1, groupsPerScan) || !FragmentLifetime.CanRetire(time - seen) || FragmentLifetime.Remaining(unit.gameObject) > 0f || progress < 1f && unit.transform.position.z > Mathf.Lerp(bounds.min.z, bounds.max.z, progress))
            {
                continue;
            }

            if (!EnemyWreckRetirement.Eligible(unit, gameObject.scene, blocks))
            {
                continue;
            }

            int count = blocks.Count;
            // Cosmetic effect uses the existing bounded particle budget, never creates damage colliders.
            if (blocks.Count > 0)
            {
                MaterialBreakEffect.Play(salvageEffect, blocks[0]);
            }

            if (EnemyWreckRetirement.Retire(unit, gameObject.scene, blocks, true))
            {
                firstSeen.Remove(unit);
                RetiredGroups++;
                RetiredBlocks += count;
                PendingGroups--;
                retired++;
            }
        }
    }

    public void Stop()
    {
        IsActive = false;
        activeSweeps.Remove(this);
        firstSeen.Clear();
        candidates.Clear();
        Show(false);
    }

    private void Show(bool visible)
    {
        if (scanLine != null)
        {
            scanLine.enabled = visible;
        }

        if (perimeter != null)
        {
            perimeter.enabled = visible;
        }

        if (status != null)
        {
            status.gameObject.SetActive(visible);
        }
    }

    private void Awake()
    {
        Show(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        activeSweeps.Clear();
    }

    private void OnDisable()
    {
        Stop();
    }
}
