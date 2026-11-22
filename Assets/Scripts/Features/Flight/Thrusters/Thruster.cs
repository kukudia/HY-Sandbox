// 职责：提供推进器共用的供电门控、运行归属和力学输出生命周期。
// 入口：TickInputThrust()；BeginGuidance/SetGuidanceOutput/EndGuidance 为机库辅助导引独占输出。
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Power), typeof(ThrusterVisualEffect))]
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
    [Tooltip("悬浮推进器的最大推力变化率（单位/秒）。主推进与通用推进使用舰船输入的共享油门。")]
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
    private Transform cachedParent;
    private ThrusterVisualEffect visualEffect;
    private Object guidanceOwner;
    public bool IsGuided => guidanceOwner != null;

    public void BeginGuidance(Object owner)
    {
        if (owner == null || (IsGuided && guidanceOwner != owner))
        {
            return;
        }

        if (!IsGuided)
        {
            thrust = lastThrustValue = 0f;
            SetPowerRequested(false);
        }

        guidanceOwner = owner;
    }

    public void SetGuidanceOutput(Object owner, float appliedForce, Vector3 worldDirection)
    {
        if (owner == null || guidanceOwner != owner)
        {
            return;
        }

        // The hangar assist already applies this force to the body. Only publish its actual
        // allocated output here; the ordinary FixedUpdate path must not apply it a second time.
        thrust = lastThrustValue = Mathf.Clamp(appliedForce, 0f, Mathf.Max(0f, maxThrust) * PowerEfficiency);
        if (worldDirection.sqrMagnitude > DirectionEpsilonSqr)
        {
            thrustDirection = worldDirection.normalized;
            AlignGuidanceVisual(thrustDirection);
        }

        VisualizeThrust(true);
    }

    protected virtual void AlignGuidanceVisual(Vector3 worldDirection)
    {
    }

    public void EndGuidance(Object owner, bool retainThrust = false)
    {
        if (guidanceOwner != owner)
        {
            return;
        }

        guidanceOwner = null;
        if (retainThrust)
        {
            SetPowerRequested(true);
        }
        else
        {
            thrust = lastThrustValue = 0f;
        }

        VisualizeThrust(true);
    }

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

    private void OnTransformParentChanged()
    {
        controlUnit = null;
        rb = null;
    }

    public void SetRuntimeReferences(ControlUnit owner, Rigidbody ownerRigidbody)
    {
        controlUnit = owner;
        rb = ownerRigidbody;
        cachedParent = transform.parent;
        CacheLocalReferences();
    }

    protected bool RefreshRuntimeReferences()
    {
        CacheLocalReferences();
        if (controlUnit == null || transform.parent != cachedParent)
        {
            controlUnit = GetComponentInParent<ControlUnit>();
            rb = controlUnit != null ? controlUnit.GetComponent<Rigidbody>() : GetComponentInParent<Rigidbody>();
            cachedParent = transform.parent;
        }

        if (rb == null)
        {
            TryEnsureRigidbody();
        }

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

    // 舰船命令已包含油门；此处只处理单个喷口的方向、供电和实际施力。
    protected void TickInputThrust(ref Vector3 input)
    {
        if (IsGuided)
        {
            return;
        }

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
        thrust = lastThrustValue = GetPoweredThrust();
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

    protected virtual void ApplyDirectionalThrust()
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

    private float GetPoweredThrust()
    {
        return Mathf.Clamp(thrust * PowerEfficiency, 0f, Mathf.Max(0f, maxThrust));
    }

    public virtual void ApplyThrustChangeRateLimit()
    {
        // 供电只限制目标输出；撤销工作请求后的残余推力仍按变化率衰减。
        float targetThrust = GetPoweredThrust();
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
        guidanceOwner = null;
        SetPowerRequested(false);
        thrust = 0f;
        lastThrustValue = 0f;
    }

    protected bool CanApplyThrust()
    {
        // 供电已计入实际输出；悬浮推进器仍可能施加闭环控制的衰减尾段。
        return !IsGuided && IsPlayModeActive() && HasValidRuntimeOwner();
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
