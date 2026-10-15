// 职责：保存无人机 Inspector 配置并组织任务飞行、返航、离家和精确停靠状态。
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
    public float movementSpeed = 5f;
    public float rotationSpeed = 2f;
    [Header("优化避障设置")]
    public LayerMask obstacleMask;
    [Tooltip("紧急避障距离（立即反应）")]
    public float emergencyAvoidanceRange = 3f;
    [Tooltip("主要避障距离")]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float primaryAvoidanceRange = 8f;
    [Tooltip("预测避障距离（提前规划）")]
    public float predictiveAvoidanceRange = 15f;
    [Tooltip("紧急避障力度")]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float emergencyAvoidanceForce = 10f;
    [Tooltip("主要避障力度")]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float primaryAvoidanceForce = 5f;
    [Tooltip("预测避障力度")]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float predictiveAvoidanceForce = 2f;
    [Tooltip("目标方向基础权重")]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float targetDirectionWeight = 3f;
    [Tooltip("射线检测数量")]
    public int avoidanceRays = 12;
    [Tooltip("地面法线点积阈值")]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float groundNormalThreshold = 0.9f;
    [Tooltip("忽略此高度以下的障碍物")]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float groundHeightThreshold = -2f;
    [Tooltip("速度衰减系数（障碍物密集时减速）")]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float speedDampingFactor = 0.5f;
    [Tooltip("路径平滑系数（0-1，越大越平滑）")]
    // 保留旧 Inspector 身份；当前方向响应由 directionResponseRate 控制。
    public float pathSmoothness = 0.7f;
    [Header("寻路平滑设置")]
    [Min(0.05f)]
    public float directionUpdateInterval = 0.25f;
    [Min(0.1f)]
    public float directionResponseRate = 3f;
    [Min(0.1f)]
    public float returnAlignmentAngle = 8f;
    [Min(0.1f)]
    public float returnBrakeDistance = 2f;
    [Min(0.01f)]
    public float returnPositionTolerance = 0.08f;
    [Min(0.01f)]
    public float returnStopSpeed = 0.15f;
    [Min(0.1f)]
    public float returnBrakingAcceleration = 12f;
    [Min(0.1f)]
    public float dockingApproachHeight = 1f;
    [Min(0.01f)]
    public float dockingApproachTolerance = 0.2f;
    [Min(0.1f)]
    public float dockingPositionSpeed = 2f;
    [Range(0f, 90f)]
    // 旧版兼容字段：保留序列化/外部身份，新导航不使用。
    public float returnMaxAvoidanceAngle = 65f;
    // 保留旧场景/Prefab 的 public 序列化身份；三维导航不再使用 Home 原点射线或偏角限制。
    [Header("旧版返航引导（兼容字段，新导航不使用）")]
    [Range(4, 16)]
    public int homeGuidanceRays = 8;
    [Min(0.5f)]
    public float homeGuidanceRange = 8f;
    [Range(0f, 2f)]
    public float homeGuidanceAlignmentWeight = 0.75f;
    [Header("三维绕障与脱困")]
    [SerializeField, Min(0.05f), Tooltip("球形导航净空半径，按机器人机身大小调整（米）")]
    private float _navigationClearanceRadius = 0.4f;
    [SerializeField, Min(0.5f), Tooltip("持续未接近目标多久后重新选择脱困方向（秒）")]
    private float _navigationStuckTimeout = 1.5f;
    [SerializeField, Min(0.05f), Tooltip("视为有效接近目标的最小距离（米）")]
    private float _navigationProgressDistance = 0.25f;
    [SerializeField, Min(0.2f), Tooltip("绕障方向最短保持时间，避免左右反复切换（秒）")]
    private float _navigationDetourHoldTime = 1f;
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

        rb.useGravity = false;
        rb.linearDamping = 2f;
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
            movementSpeed,
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
        _flightMotion.NavigateToTarget(target, CreateFlightSettings());
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

        Vector3 approachPosition = home.TransformPoint(homeOffset + Vector3.up * dockingApproachHeight);
        if (currentState == NavigationState.Docking)
        {
            ReturnHome();
            return;
        }

        float dockingCaptureDistance = Mathf.Max(
            dockingApproachTolerance,
            GetRelativeHomeVelocity(approachPosition).magnitude * Time.fixedDeltaTime * 1.5f);
        if ((approachPosition - transform.position).sqrMagnitude <= dockingCaptureDistance * dockingCaptureDistance)
        {
            ReturnHome();
            return;
        }

        float avoidanceRangeScale = Mathf.InverseLerp(
            dockingCaptureDistance,
            Mathf.Max(dockingCaptureDistance + 0.01f, predictiveAvoidanceRange),
            Vector3.Distance(transform.position, approachPosition));
        _flightMotion.NavigateHome(home, approachPosition, homeRigidbody, avoidanceRangeScale, CreateFlightSettings());
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
        transform.position = Vector3.MoveTowards(transform.position, dockPosition, Mathf.Max(0.1f, dockingPositionSpeed) * Time.fixedDeltaTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, dockRotation, Mathf.Max(1f, rotationSpeed * 90f) * Time.fixedDeltaTime);
        if ((dockPosition - transform.position).sqrMagnitude > returnPositionTolerance * returnPositionTolerance || Quaternion.Angle(transform.rotation, dockRotation) > returnAlignmentAngle)
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
            rb.isKinematic = docked;
            rb.detectCollisions = !docked;
        }

        BotFlightEffects.SetDocked(docked, trailRenderer, trailCoreRenderer, _flightEffect);
    }

    private BotFlightMotion.Settings CreateFlightSettings()
    {
        return new BotFlightMotion.Settings
        {
            SampleInterval = directionUpdateInterval,
            DirectionResponseRate = directionResponseRate,
            RotationSpeed = rotationSpeed,
            MovementSpeed = movementSpeed,
            DockingApproachTolerance = dockingApproachTolerance,
            ReturnBrakingAcceleration = returnBrakingAcceleration,
            ReturnStopSpeed = returnStopSpeed,
            ReturnBrakeDistance = returnBrakeDistance,
            Avoidance = new BotAvoidanceSettings
            {
                ObstacleMask = obstacleMask,
                ClearanceRadius = _navigationClearanceRadius,
                StuckTimeout = _navigationStuckTimeout,
                ProgressDistance = _navigationProgressDistance,
                DetourHoldTime = _navigationDetourHoldTime,
                EmergencyRange = emergencyAvoidanceRange,
                PredictiveRange = predictiveAvoidanceRange,
                BrakingAcceleration = returnBrakingAcceleration,
                RayCount = avoidanceRays,
            },
        };
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
