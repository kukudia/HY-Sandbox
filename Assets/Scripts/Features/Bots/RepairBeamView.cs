// 职责：初始化与显示维修光束、命中 VFX，并计算可见目标表面的光束落点。
// 入口：Initialize()/Update()/Hide()；只修改表现，维修计时和耐久规则由 RepairBot 持有。
using UnityEngine;

public sealed class RepairBeamView
{
    private readonly Transform _transform;
    private StylizedBeamEffect _beam;
    private VfxEffect _impact;
    public RepairBeamView(Transform transform)
    {
        _transform = transform;
    }

    public void Initialize(GameObject gameObject, VfxEffect impact, float beamWidth)
    {
        // 初始化修复光束
        LineRenderer legacyRepairBeam = gameObject.GetComponent<LineRenderer>();
        if (legacyRepairBeam != null)
        {
            legacyRepairBeam.enabled = false;
        }

        _beam = gameObject.GetComponent<StylizedBeamEffect>();
        if (_beam == null)
        {
            _beam = gameObject.AddComponent<StylizedBeamEffect>();
        }

        _beam.Configure(beamWidth * 0.24f);
        _beam.SetVisible(false);
        _impact = impact;
        Hide();
    }

    public void Hide()
    {
        if (_impact != null)
        {
            _impact.SetIntensity(0f);
        }

        if (_beam != null)
        {
            _beam.SetVisible(false);
        }
    }

    public void Update(bool active, Durability target, Vector3 origin, Gradient gradient)
    {
        if (_beam == null)
        {
            return;
        }

        _beam.SetVisible(active);
        if (active && target != null)
        {
            Vector3 targetPoint = GetRepairTargetPoint(target, origin);
            _beam.SetEndpoints(origin, targetPoint);
            float durabilityRatio = Mathf.Clamp01(target.currentDurability / target.maxDurability);
            Color beamColor = gradient.Evaluate(durabilityRatio);
            float primaryPulse = 0.84f + Mathf.Sin(Time.unscaledTime * 13f) * 0.12f;
            float secondaryPulse = Mathf.Sin(Time.unscaledTime * 31f) * 0.04f;
            Color brightBeamColor = Color.Lerp(beamColor, Color.white, 0.34f) * 1.85f;
            brightBeamColor.a = 1f;
            _beam.SetColor(brightBeamColor);
            _beam.SetIntensity((primaryPulse + secondaryPulse) * 1.15f);
            UpdateRepairImpact(targetPoint, origin, primaryPulse);
        }
        else
        {
            SetRepairImpactActive(false);
        }
    }

    public Vector3 GetRepairTargetPoint(Durability target, Vector3 origin)
    {
        if (target == null)
        {
            return _transform.position;
        }

        Collider targetCollider = target.GetComponentInChildren<Collider>();
        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(origin);
            if ((closestPoint - _transform.position).sqrMagnitude > 0.0001f)
            {
                return closestPoint;
            }
        }

        Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
        return targetRenderer != null ? targetRenderer.bounds.center : target.transform.position;
    }

    private void UpdateRepairImpact(Vector3 targetPoint, Vector3 origin, float pulse)
    {
        if (_impact == null)
        {
            return;
        }

        _impact.transform.position = targetPoint;
        Vector3 normal = origin - targetPoint;
        if (normal.sqrMagnitude > 0.0001f)
        {
            _impact.transform.rotation = Quaternion.LookRotation(normal);
        }

        _impact.SetIntensity(Mathf.Clamp01(pulse));
    }

    private void SetRepairImpactActive(bool active)
    {
        if (_impact != null)
        {
            _impact.SetIntensity(active ? 1f : 0f);
        }
    }
}
