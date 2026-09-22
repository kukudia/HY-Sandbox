using System.Collections.Generic;
using UnityEngine;

public class RepairBot : Bot
{
    private const int InitialRepairTargetColliderCapacity = 128;
    private const int MaxRepairTargetColliderCapacity = 1024;
    private const float MinimumTargetScanInterval = 0.25f;
    [SerializeField] private Transform _repairOrigin;
    [SerializeField] private AssetParticleEffect _repairImpact;
    private Vector3 RepairOrigin => _repairOrigin != null ? _repairOrigin.position : transform.position;
    public float repairAmount = 10f;
    public float repairCooldown = 1f;
    public Gradient repairBeamGradient;
    public float beamWidth = 0.2f;
    public Durability currentTarget;
    public float lastRepairTime;
    public bool isRepairing;
    private ControlUnit ownerUnit;
    private readonly List<Durability> targetsInRange = new List<Durability>();
    private readonly HashSet<Durability> uniqueTargetsInRange = new HashSet<Durability>();
    private Collider[] repairTargetColliders = new Collider[InitialRepairTargetColliderCapacity];
    private StylizedBeamEffect repairBeamEffect;
    private int blockLayerMask;
    protected override void Start()
    {
        base.Start();
        blockLayerMask = LayerMask.GetMask("Block");
        InitializeComponents();
        InitializeTargetsInRange();
    }
    private void InitializeComponents()
    {
        // 初始化修复光束
        LineRenderer legacyRepairBeam = GetComponent<LineRenderer>();
        if (legacyRepairBeam != null)
        {
            legacyRepairBeam.enabled = false;
        }

        repairBeamEffect = GetComponent<StylizedBeamEffect>();
        if (repairBeamEffect == null)
        {
            repairBeamEffect = gameObject.AddComponent<StylizedBeamEffect>();
        }
        repairBeamEffect.Configure(beamWidth * 0.24f, 6.8f, 18, beamWidth * 0.18f, 4.4f, 13f);
        repairBeamEffect.SetVisible(false);

        EnsureRepairBeamGradient();
        EnsureRepairImpactVfx();
    }
    private void FixedUpdate()
    {
        if (!TickNavigation()) return;

        if (currentTarget == null)
        {
            FindDamagedBlock();
            UpdateRepairBeam(false);

            if (currentTarget == null && home != null)
            {
                NavigateHomeSmoothly();
            }
        }
        else
        {
            if (!IsValidRepairTarget(currentTarget))
            {
                ClearTarget();
                return;
            }

            NavigateToTarget(currentTarget.transform);
            if (transform.parent != home) CheckAndRepair();
        }
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        if (_repairImpact != null) _repairImpact.SetIntensity(0f);
        if (repairBeamEffect != null) repairBeamEffect.SetVisible(false);
    }

    private void InitializeTargetsInRange()
    {
        targetsInRange.Clear();
        uniqueTargetsInRange.Clear();
        ownerUnit = home != null ? home.GetComponentInParent<ControlUnit>() : null;
        homeRigidbody = home != null ? home.GetComponentInParent<Rigidbody>() : null;
        if (home == null || ownerUnit == null || targetRange <= 0 || blockLayerMask == 0)
        {
            return;
        }

        int colliderCount;
        while (true)
        {
            colliderCount = Physics.OverlapSphereNonAlloc(
                home.position,
                targetRange,
                repairTargetColliders,
                blockLayerMask,
                QueryTriggerInteraction.Ignore);

            if (colliderCount < repairTargetColliders.Length
                || repairTargetColliders.Length >= MaxRepairTargetColliderCapacity)
            {
                break;
            }

            int newCapacity = Mathf.Min(repairTargetColliders.Length * 2, MaxRepairTargetColliderCapacity);
            repairTargetColliders = new Collider[newCapacity];
        }

        float rangeSqr = targetRange * targetRange;
        for (int i = 0; i < colliderCount; i++)
        {
            Collider candidateCollider = repairTargetColliders[i];
            repairTargetColliders[i] = null;
            if (candidateCollider == null)
            {
                continue;
            }

            Durability durability = candidateCollider.GetComponentInParent<Durability>();
            if (durability == null
                || durability.GetComponentInParent<ControlUnit>() != ownerUnit
                || (durability.transform.position - home.position).sqrMagnitude > rangeSqr
                || !uniqueTargetsInRange.Add(durability))
            {
                continue;
            }

            targetsInRange.Add(durability);
        }
    }

    private void FindDamagedBlock()
    {
        float scanInterval = Mathf.Max(MinimumTargetScanInterval, findTargetInterval);
        if (Time.time - lastFindTime < scanInterval) return;

        lastFindTime = Time.time;
        InitializeTargetsInRange();

        float closestDistanceSqr = Mathf.Infinity;
        Durability closestBlock = null;

        foreach (Durability block in targetsInRange)
        {
            if (block == null || !block.needToRepair) continue;

            float distanceSqr = (transform.position - block.transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestBlock = block;
            }
        }

        currentTarget = closestBlock;
    }

    private bool IsValidRepairTarget(Durability target)
    {
        if (target == null || !target.needToRepair || home == null || targetRange <= 0)
        {
            return false;
        }

        ControlUnit currentOwner = home.GetComponentInParent<ControlUnit>();
        if (currentOwner == null || target.GetComponentInParent<ControlUnit>() != currentOwner)
        {
            return false;
        }

        float rangeSqr = targetRange * targetRange;
        return (target.transform.position - home.position).sqrMagnitude <= rangeSqr;
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
                VisualEffectsManager.TryPlayRepairPulse(
                    RepairOrigin,
                    GetRepairTargetPoint(),
                    pulseColor,
                    beamWidth);

                currentTarget.UpdateDurablility(repairAmount);
                lastRepairTime = Time.time;

                if (currentTarget.currentDurability >= currentTarget.maxDurability)
                {
                    currentTarget = null;
                    isRepairing = false;
                    UpdateRepairBeam(false);

                    if (trailRenderer != null) trailRenderer.Clear();
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
        if (repairBeamEffect == null) return;

        repairBeamEffect.SetVisible(active);

        if (active && currentTarget != null)
        {
            Vector3 origin = RepairOrigin;
            Vector3 targetPoint = GetRepairTargetPoint();
            repairBeamEffect.SetEndpoints(origin, targetPoint);

            float durabilityRatio = Mathf.Clamp01(currentTarget.currentDurability / currentTarget.maxDurability);
            Color beamColor = repairBeamGradient.Evaluate(durabilityRatio);
            float primaryPulse = 0.84f + Mathf.Sin(Time.unscaledTime * 13f) * 0.12f;
            float secondaryPulse = Mathf.Sin(Time.unscaledTime * 31f) * 0.04f;

            Color brightBeamColor = Color.Lerp(beamColor, Color.white, 0.34f) * 1.85f;
            brightBeamColor.a = 1f;
            repairBeamEffect.SetColor(brightBeamColor);
            repairBeamEffect.SetIntensity((primaryPulse + secondaryPulse) * 1.15f);
            UpdateRepairImpact(targetPoint, beamColor, primaryPulse);
        }
        else
        {
            SetRepairImpactActive(false);
        }
    }

    private Vector3 GetRepairTargetPoint()
    {
        if (currentTarget == null) return transform.position;

        Collider targetCollider = currentTarget.GetComponentInChildren<Collider>();
        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(RepairOrigin);
            if ((closestPoint - transform.position).sqrMagnitude > 0.0001f)
            {
                return closestPoint;
            }
        }

        Renderer targetRenderer = currentTarget.GetComponentInChildren<Renderer>();
        return targetRenderer != null ? targetRenderer.bounds.center : currentTarget.transform.position;
    }

    private void EnsureRepairImpactVfx()
    {
        if (_repairImpact != null) _repairImpact.SetIntensity(0f);
    }

    private void UpdateRepairImpact(Vector3 targetPoint, Color color, float pulse)
    {
        if (_repairImpact == null) return;
        _repairImpact.transform.position = targetPoint;
        Vector3 normal = RepairOrigin - targetPoint;
        if (normal.sqrMagnitude > 0.0001f) _repairImpact.transform.rotation = Quaternion.LookRotation(normal);
        _repairImpact.SetIntensity(Mathf.Clamp01(pulse));
    }

    private void SetRepairImpactActive(bool active)
    {
        if (_repairImpact != null) _repairImpact.SetIntensity(active ? 1f : 0f);
    }

    private void EnsureRepairBeamGradient()
    {
        if (repairBeamGradient != null) return;

        repairBeamGradient = new Gradient();
        repairBeamGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.28f, 0.12f), 0f),
                new GradientColorKey(new Color(0.1f, 1f, 0.62f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
    }

    public void ClearTarget()
    {
        currentTarget = null;
        isRepairing = false;
        UpdateRepairBeam(false);
        if (trailRenderer != null) trailRenderer.Clear();
    }
}
