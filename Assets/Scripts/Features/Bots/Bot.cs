// 职责：保存无人机任务/插槽配置并组织任务飞行、返航、离家和精确停靠状态。
// 入口：派生机器人的 FixedUpdate → TickNavigation() → NavigateToTarget()/NavigateHomeSmoothly()。
// 依赖：BotFlightMotion 处理飞行/制动；BotObstacleAvoidance 负责三维净空与脱困。
// 不变量：homeOffset 保持 Home 局部坐标；停靠切为运动学刚体，完成后恢复准确局部姿态。
using UnityEngine;

public abstract class Bot : MonoBehaviour
{
    [Header("Authored model sockets and effects")]
    [SerializeField]
    private VfxEffect _flightEffect;
    [SerializeField]
    private VfxEffect _flightTrail;
    [SerializeField]
    private VfxEffect _flightCoreTrail;
    public Transform home;
    public Transform outside;
    public Transform navigateTarget;
    public Vector3 homeOffset = Vector3.zero;
    [Header("Bot navigation and search")]
    [Min(0)]
    public int targetRange = 5;
    public float findTargetInterval = 1f;
    [Header("可视化设置")]
    public bool showTrail = true;
    public bool showAvoidanceRays = true;
    public bool showDirectionVectors = true;
    // 保留旧 Inspector 身份；旧区域 Gizmos 原本已停用。
    public bool showAvoidanceZones = true;
    // 公共状态
    public NavigationState currentState;
    public float lastFindTime;
    public bool isCoolDown => coolDownTimer >= 0f && coolDownTimer < 3f;

    [Min(0f)]
    public float coolDownTimer = 0f;
    protected Rigidbody rb;
    protected Rigidbody homeRigidbody;
    protected VfxEffect trailRenderer;
    protected VfxEffect trailCoreRenderer;
    private BotFlightMotion _flightMotion;
    private bool _navigationStateInitialized;
    private Power _homePower;
    public enum NavigationState
    {
        Idle,
        NavigatingToTarget,
        ReturningHome,
        Docking
    }

    protected virtual void Start()
    {
        if (home == null)
        {
            home = transform.parent;
        }

        if (home != null)
        {
            homeOffset = home.InverseTransformPoint(transform.position);
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        BotNavigationPolicy.ConfigureBody(rb);
        rb.angularDamping = 3f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _flightMotion = new BotFlightMotion(transform, rb);
        InitializeTrail();
        SetNavigationState(transform.parent == home ? NavigationState.Idle : NavigationState.ReturningHome);
    }

    /// <summary>推进物理帧冷却并核对 Home 与模拟模式；Home 消失时销毁当前机器人。</summary>
    protected bool TickNavigation()
    {
        coolDownTimer += Time.fixedDeltaTime;
        if (home == null)
        {
            Destroy(gameObject);
            return false;
        }

        homeRigidbody = home.GetComponentInParent<Rigidbody>();
        ControlUnit owner = home.GetComponentInParent<ControlUnit>();
        return rb != null && owner != null && owner.IsSimulationActive;
    }

    protected virtual void OnDisable()
    {
        SetHomePowerRequested(false);
        if (_flightEffect != null)
        {
            _flightEffect.SetIntensity(0f);
        }

        if (trailRenderer != null)
        {
            trailRenderer.SetIntensity(0f);
        }

        if (trailCoreRenderer != null)
        {
            trailCoreRenderer.SetIntensity(0f);
        }
    }

    protected virtual void OnDestroy()
    {
        SetHomePowerRequested(false);
    }

    /// <summary>任务意图独立于供电结果；脱离 Home 后仍向原机器人仓请求功率。</summary>
    protected Power SetHomePowerRequested(bool requested)
    {
        Power actualPower = home != null ? home.GetComponentInParent<Power>() : null;
        if (_homePower != actualPower)
        {
            _homePower?.SetWorkRequested(this, false);
            _homePower = actualPower;
        }

        _homePower?.SetWorkRequested(this, requested);
        return _homePower;
    }

    public virtual void PrepareForHomeDestruction()
    {
    }

    private void InitializeTrail()
    {
        trailRenderer = _flightTrail;
        trailCoreRenderer = _flightCoreTrail;
        if (trailRenderer != null)
        {
            trailRenderer.SetIntensity(0f);
        }

        if (trailCoreRenderer != null)
        {
            trailCoreRenderer.SetIntensity(0f);
        }

        if (_flightEffect != null)
        {
            _flightEffect.SetIntensity(0f);
        }
    }

    private void LateUpdate()
    {
        bool flying = currentState == NavigationState.NavigatingToTarget || currentState == NavigationState.ReturningHome;
        BotFlightEffects.Update(
            rb,
            flying,
            showTrail,
            BotNavigationPolicy.EffectFullSpeed,
            trailRenderer,
            trailCoreRenderer,
            _flightEffect);
    }

    protected void NavigateToTarget(Transform target)
    {
        if (transform.parent == home)
        {
            LeaveHome();
            if (transform.parent == home)
            {
                return;
            }
        }
        else
        {
            SetNavigationState(NavigationState.NavigatingToTarget);
        }

        navigateTarget = target;
        _flightMotion.NavigateToTarget(target);
    }

    /// <summary>追踪移动 Home 的实时接近点，达到捕获距离后执行精确局部坐标停靠。</summary>
    protected void NavigateHomeSmoothly()
    {
        navigateTarget = home;
        if (transform.parent == home)
        {
            ReturnHome();
            return;
        }

        if (currentState != NavigationState.Docking)
        {
            SetNavigationState(NavigationState.ReturningHome);
        }

        Vector3 approachPosition = home.TransformPoint(homeOffset + Vector3.up * BotNavigationPolicy.DockingApproachHeight);
        if (currentState == NavigationState.Docking)
        {
            ReturnHome();
            return;
        }

        float dockingCaptureDistance = Mathf.Max(
            BotNavigationPolicy.DockingApproachTolerance,
            GetRelativeHomeVelocity(approachPosition).magnitude * Time.fixedDeltaTime * 1.5f);
        if ((approachPosition - transform.position).sqrMagnitude <= dockingCaptureDistance * dockingCaptureDistance)
        {
            ReturnHome();
            return;
        }

        float avoidanceRangeScale = Mathf.InverseLerp(
            dockingCaptureDistance,
            Mathf.Max(dockingCaptureDistance + 0.01f, BotNavigationPolicy.PredictiveRange),
            Vector3.Distance(transform.position, approachPosition));
        _flightMotion.NavigateHome(home, approachPosition, homeRigidbody, avoidanceRangeScale);
    }

    private Vector3 GetRelativeHomeVelocity(Vector3 worldPosition)
    {
        Vector3 homeVelocity = homeRigidbody != null ? homeRigidbody.GetPointVelocity(worldPosition) : Vector3.zero;
        return rb.linearVelocity - homeVelocity;
    }

    private void ReturnHome()
    {
        if (home == null || rb == null)
        {
            return;
        }

        if (transform.parent == home)
        {
            SetNavigationState(NavigationState.Idle);
            return;
        }

        if (currentState != NavigationState.Docking)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            SetNavigationState(NavigationState.Docking);
        }

        Vector3 dockPosition = home.TransformPoint(homeOffset);
        Quaternion dockRotation = home.rotation;
        transform.position = Vector3.MoveTowards(
            transform.position,
            dockPosition,
            Mathf.Max(0.1f, BotNavigationPolicy.DockingPositionSpeed) * Time.fixedDeltaTime);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            dockRotation,
            Mathf.Max(1f, BotNavigationPolicy.RotationSpeed * 90f) * Time.fixedDeltaTime);
        if ((dockPosition - transform.position).sqrMagnitude > BotNavigationPolicy.DockingPositionTolerance * BotNavigationPolicy.DockingPositionTolerance || Quaternion.Angle(transform.rotation, dockRotation) > BotNavigationPolicy.DockingAlignmentAngle)
        {
            return;
        }

        transform.SetParent(home, false);
        transform.localPosition = homeOffset;
        transform.localRotation = Quaternion.identity;
        _flightMotion.ClearTarget();
        SetNavigationState(NavigationState.Idle);
    }

    private void LeaveHome()
    {
        if (isCoolDown)
        {
            return;
        }

        transform.parent = outside;
        SetNavigationState(NavigationState.NavigatingToTarget);
        _flightMotion.ResetDirection();
    }

    private void SetNavigationState(NavigationState nextState)
    {
        if (_navigationStateInitialized && currentState == nextState)
        {
            return;
        }

        bool docked = nextState == NavigationState.Idle || nextState == NavigationState.Docking;
        coolDownTimer = 0f;
        currentState = nextState;
        _navigationStateInitialized = true;
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = docked;
            rb.detectCollisions = !docked;
            if (!docked)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        BotFlightEffects.SetDocked(docked, trailRenderer, trailCoreRenderer, _flightEffect);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || currentState == NavigationState.Idle || _flightMotion == null)
        {
            return;
        }

        BotNavigationGizmos.Draw(
            transform,
            navigateTarget,
            rb,
            currentState,
            _flightMotion,
            showAvoidanceRays,
            showDirectionVectors);
    }
}
