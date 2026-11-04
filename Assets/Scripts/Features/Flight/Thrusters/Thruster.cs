// 职责：提供推进器共用的供电门控、运行归属和力学输出生命周期。
// 入口：TickInputThrust() / ShouldActivate() / SetRuntimeReferences() / ApplyThrustChangeRateLimit()。
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Power))]
public abstract class Thruster : MonoBehaviour
{
    // Shared runtime contract for thrust application, reference caching, and visual feedback.
    protected const float InputEpsilonSqr = 1e-6f;
    private const float DirectionEpsilonSqr = 1e-6f;
    public ControlUnit controlUnit;
    public Transform model;
    public float thrust; // 推力大小
    public float lastThrustValue;
    public float maxThrust = 100f;
    [Tooltip("最大推力变化率（单位/秒），防止推力瞬间变化")]
    public float maxThrustChangeRate = 50f;
    public Vector3 thrustDirection = Vector3.forward; // 推力方向（本地坐标）
    public Transform cameraTransform; // 主摄像机
    public Rigidbody rb;
    private Power power;
    [Header("推力可视化")]
    [Tooltip("推力可视化刷新间隔（秒），降低粒子系统在物理帧中的更新频率")]
    public float visualizationInterval = 0.05f;
    private float nextVisualizationTime;
    private bool modelLookupComplete;
    private ThrusterVisualEffect visualEffect;
    // 子类必须实现：如何启用推进器（输入控制/自动触发）
    public abstract bool ShouldActivate();
    protected virtual void Awake()
    {
        power = GetComponent<Power>();
        CacheLocalReferences();
        DisableLegacyLineRenderer();
    }

    protected virtual void Start()
    {
        CacheLocalReferences();
        EnsureVisualEffect();
        VisualizeThrust(true);
    }

    private void OnTransformChildrenChanged()
    {
        modelLookupComplete = false;
    }

    public void SetRuntimeReferences(ControlUnit owner, Rigidbody ownerRigidbody)
    {
        controlUnit = owner;
        rb = ownerRigidbody;
        CacheLocalReferences();
    }

    protected bool RefreshRuntimeReferences()
    {
        CacheLocalReferences();
        if (controlUnit == null)
        {
            controlUnit = GetComponentInParent<ControlUnit>();
        }

        TryEnsureRigidbody();
        return controlUnit != null && rb != null;
    }

    protected bool TryEnsureRigidbody()
    {
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        return rb != null;
    }

    protected bool IsPlayModeActive()
    {
        return PlayManager.instance != null && PlayManager.instance.playMode;
    }

    protected bool HasValidRuntimeOwner()
    {
        return controlUnit != null && controlUnit.HasValidCockpit;
    }

    // 输入推进器共享物理步；悬浮推进器仍由高度控制器提交独立目标。
    protected void TickInputThrust(ref Vector3 input)
    {
        if (!IsPlayModeActive())
        {
            SetPowerRequested(false);
            thrust = 0f;
            lastThrustValue = 0f;
            return;
        }

        RefreshRuntimeReferences();
        bool hasValidOwner = HasValidRuntimeOwner();
        input = hasValidOwner ? controlUnit.MovementInput : Vector3.zero;
        thrust = CalculateInputThrust(input, hasValidOwner && input.sqrMagnitude > InputEpsilonSqr);
        SetPowerRequested(thrust > 0f);
        ApplyThrustChangeRateLimit();
        ApplyDirectionalThrust();
        VisualizeThrust();
    }

    protected virtual float CalculateInputThrust(Vector3 input, bool active)
    {
        return active ? maxThrust : 0f;
    }

    protected bool ShouldActivateInputThrust(Vector3 input)
    {
        return input.sqrMagnitude > InputEpsilonSqr && IsPlayModeActive() && HasValidRuntimeOwner();
    }

    protected void ApplyDirectionalThrust()
    {
        if (thrust <= 0f || !CanApplyThrust() || !TryEnsureRigidbody())
        {
            return;
        }

        thrustDirection = model != null ? model.forward : transform.forward;
        rb.AddForceAtPosition(thrustDirection * thrust, transform.position);
    }

    public bool IsPlayerControllable => isActiveAndEnabled && HasValidRuntimeOwner() && controlUnit.AcceptsPlayerInput;

    public float PowerSupplyEfficiency
    {
        get
        {
            if (power == null)
            {
                power = GetComponent<Power>();
            }

            return power != null ? power.SupplyEfficiency : 0f;
        }
    }

    public float PowerEfficiency
    {
        get
        {
            if (power == null)
            {
                power = GetComponent<Power>();
            }

            return power != null && power.isWorking ? power.efficiency : 0f;
        }
    }

    protected void CacheLocalReferences()
    {
        if (model == null && !modelLookupComplete)
        {
            model = transform.Find("Model");
            modelLookupComplete = true;
        }
    }

    public virtual void ApplyThrustChangeRateLimit()
    {
        float powerEfficiency = PowerEfficiency;
        // 供电只限制目标输出；撤销工作请求后的残余推力仍按变化率衰减。
        float targetThrust = Mathf.Clamp(thrust * powerEfficiency, 0f, Mathf.Max(0f, maxThrust));
        float maxChange = Mathf.Max(0f, maxThrustChangeRate) * Time.fixedDeltaTime;
        thrust = Mathf.MoveTowards(lastThrustValue, targetThrust, maxChange);
        lastThrustValue = thrust;
    }

    protected void SetPowerRequested(bool requested)
    {
        if (power == null)
        {
            power = GetComponent<Power>();
        }

        power?.SetWorkRequested(this, requested);
    }

    protected virtual void OnDisable()
    {
        SetPowerRequested(false);
        thrust = 0f;
        lastThrustValue = 0f;
    }

    protected bool CanApplyThrust()
    {
        // 实际输出已在限速步骤受供电影响，关机后的衰减尾段仍需要施力。
        return IsPlayModeActive() && HasValidRuntimeOwner();
    }

    public virtual void VisualizeThrust()
    {
        VisualizeThrust(false);
    }

    public virtual void VisualizeThrust(bool forceUpdate)
    {
        // Throttle visual updates independently from physics to avoid rebuilding particle state every tick.
        float currentTime = Time.unscaledTime;
        if (!forceUpdate && visualizationInterval > 0f && currentTime < nextVisualizationTime)
        {
            return;
        }

        nextVisualizationTime = currentTime + Mathf.Max(0f, visualizationInterval);
        Vector3 lineDirection = GetNormalizedThrustDirection();
        float thrustRatio = maxThrust > 1e-5f ? Mathf.Clamp01(thrust / maxThrust) : 0f;
        EnsureVisualEffect();
        visualEffect.SetThrust(thrustRatio, lineDirection);
    }

    private void DisableLegacyLineRenderer()
    {
        LineRenderer legacyLine = GetComponent<LineRenderer>();
        if (legacyLine != null)
        {
            legacyLine.enabled = false;
        }
    }

    private void EnsureVisualEffect()
    {
        if (visualEffect == null)
        {
            visualEffect = GetComponent<ThrusterVisualEffect>();
        }

        if (visualEffect == null)
        {
            visualEffect = gameObject.AddComponent<ThrusterVisualEffect>();
        }

        visualEffect.Initialize(this);
    }

    private Vector3 GetNormalizedThrustDirection()
    {
        Vector3 direction = thrustDirection;
        float sqrMagnitude = direction.sqrMagnitude;
        if (sqrMagnitude <= DirectionEpsilonSqr)
        {
            return Vector3.forward;
        }

        if (Mathf.Abs(sqrMagnitude - 1f) <= 0.001f)
        {
            return direction;
        }

        return direction / Mathf.Sqrt(sqrMagnitude);
    }
}
