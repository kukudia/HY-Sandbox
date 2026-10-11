// 职责：寻找受损友方设备并维修，复用 Bot 的导航和返航能力。
// 入口：FixedUpdate() → 搜索受损目标 / 导航并维修；ClearTarget() 取消当前维修。
// 依赖：RepairTargetSearch 扫描归属与范围；RepairBeamView 显示光束与命中效果。
using UnityEngine;

public class RepairBot : Bot
{
    private const float MinimumTargetScanInterval = 0.25f;
    [SerializeField]
    private Transform _repairOrigin;
    [SerializeField]
    private VfxEffect _repairImpact;
    private Vector3 RepairOrigin => _repairOrigin != null ? _repairOrigin.position : transform.position;

    public float repairAmount = 10f;
    public float repairCooldown = 1f;
    public Gradient repairBeamGradient;
    public float beamWidth = 0.2f;
    public Durability currentTarget;
    public float lastRepairTime;
    public bool isRepairing;
    private RepairTargetSearch _targetSearch;
    private RepairBeamView _beamView;
    protected override void Start()
    {
        base.Start();
        _targetSearch = new RepairTargetSearch();
        _beamView = new RepairBeamView(transform);
        EnsureRepairBeamGradient();
        _beamView.Initialize(gameObject, _repairImpact, beamWidth);
        _targetSearch.Scan(home, targetRange);
    }

    private void FixedUpdate()
    {
        if (!TickNavigation())
        {
            SetHomePowerRequested(false);
            return;
        }

        ControlUnit owner = home.GetComponentInParent<ControlUnit>();
        if (owner == null || !owner.HasValidCockpit)
        {
            ClearTarget();
            SetHomePowerRequested(false);
            return;
        }

        if (currentTarget != null && !RepairTargetSearch.IsValidTarget(currentTarget, home, targetRange))
        {
            ClearTarget();
        }

        if (currentTarget == null)
        {
            FindDamagedBlock();
        }

        bool requested = currentTarget != null || transform.parent != home;
        Power power = SetHomePowerRequested(requested);
        if (requested && (power == null || !power.isWorking))
        {
            isRepairing = false;
            UpdateRepairBeam(false);
            return;
        }

        if (currentTarget == null)
        {
            UpdateRepairBeam(false);
            if (currentTarget == null && home != null)
            {
                NavigateHomeSmoothly();
            }
        }
        else
        {
            NavigateToTarget(currentTarget.transform);
            if (transform.parent != home)
            {
                CheckAndRepair();
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _beamView?.Hide();
    }

    private void FindDamagedBlock()
    {
        float scanInterval = Mathf.Max(MinimumTargetScanInterval, findTargetInterval);
        if (Time.time - lastFindTime < scanInterval)
        {
            return;
        }

        lastFindTime = Time.time;
        _targetSearch.Scan(home, targetRange);
        currentTarget = _targetSearch.FindClosestDamaged(transform.position);
    }

    private void CheckAndRepair()
    {
        if (currentTarget == null)
        {
            UpdateRepairBeam(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance < 10f)
        {
            isRepairing = true;
            rb.linearVelocity *= 0.8f;
            rb.angularVelocity = Vector3.zero;
            UpdateRepairBeam(true);
            if (Time.time - lastRepairTime >= repairCooldown)
            {
                float durabilityRatio = Mathf.Clamp01(currentTarget.currentDurability / currentTarget.maxDurability);
                Color pulseColor = repairBeamGradient.Evaluate(durabilityRatio);
                VisualEffectsManager.TryPlayRepairPulse(RepairOrigin, _beamView.GetRepairTargetPoint(currentTarget, RepairOrigin), pulseColor, beamWidth);
                currentTarget.UpdateDurablility(repairAmount);
                lastRepairTime = Time.time;
                if (currentTarget.currentDurability >= currentTarget.maxDurability)
                {
                    currentTarget = null;
                    isRepairing = false;
                    UpdateRepairBeam(false);
                    if (trailRenderer != null)
                    {
                        trailRenderer.Clear();
                    }
                }
            }
        }
        else if (distance >= 15f)
        {
            isRepairing = false;
            UpdateRepairBeam(false);
        }
    }

    private void UpdateRepairBeam(bool active)
    {
        _beamView?.Update(active, currentTarget, RepairOrigin, repairBeamGradient);
    }

    private void EnsureRepairBeamGradient()
    {
        if (repairBeamGradient != null)
        {
            return;
        }

        repairBeamGradient = new Gradient();
        repairBeamGradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.28f, 0.12f), 0f), new GradientColorKey(new Color(0.1f, 1f, 0.62f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
    }

    public void ClearTarget()
    {
        currentTarget = null;
        isRepairing = false;
        SetHomePowerRequested(PlayManager.instance != null && PlayManager.instance.playMode && home != null && transform.parent != home);
        UpdateRepairBeam(false);
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }
}
