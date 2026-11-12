// 职责：为敌机与自主友舰选择敌对目标并生成驾驶意图；阵营和玩家驾驶权分别判断。
// 入口：FixedUpdate()；优先上下避障，受阻时局部三维A*规划，逐物理步连续跟随。
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ControlUnit))]
public class EnemyController : MonoBehaviour
{
    // Produces smoothed movement intent; ControlUnit and thrusters remain the only physics writers.
    private const int MaxObstacleHits = 32;
    [SerializeField, Range(0.1f, 1f), Tooltip("新目标必须比当前目标更近的距离比例，抑制交叉飞行时反复换目标。")]
    private float targetSwitchDistanceRatio = 0.85f;
    public float detectionRange = 150f;
    public float desiredDistance = 18f;
    public float updateInterval = 0.25f;
    [Min(0.5f)]
    public float movementUpdateInterval = 0.5f;
    [Min(0.1f)]
    public float movementResponseRate = 2.5f;
    public float retreatDistance = 10f;
    public float obstacleAvoidanceDistance = 12f;
    public float obstacleProbeRadius = 1.5f;
    public float obstacleAvoidanceWeight = 1.4f;
    [HideInInspector]
    public float sideProbeAngle = 35f; // 保留旧序列化数据；当前不再使用扇形探测。
    public float targetHoverHeightOffset = 3f;
    public LayerMask obstacleLayers = ~0;
    [Header("Stable Steering")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("navigationCruiseSpeed"), Min(1f)]
    private float cruiseSpeed = 6f;
    [SerializeField, Min(0.1f), Tooltip("非紧急情况下两次方向切换之间的最短时间（秒）。")]
    private float directionHoldTime = 1.5f;
    [SerializeField, Range(1f, 45f), Tooltip("小于此角度的目标偏移不触发转向。")]
    private float directionDeadZone = 12f;
    [SerializeField, Min(0.1f), Tooltip("脱离障碍后继续保持绕行侧向的时间（秒）。")]
    private float avoidanceSideHoldTime = 3f;
    [SerializeField, Min(1f)]
    private float strafeSwitchInterval = 6f;
    [SerializeField, Min(0.1f), Tooltip("按实际速度预测移动路径的时间（秒）；扫描至少覆盖下一次决策。")]
    private float avoidancePredictionTime = 1.5f;
    [SerializeField, Min(0.1f), Tooltip("用于估算高速减速所需提前量的保守减速度（米/秒²）。")]
    private float predictionBrakingAcceleration = 6f;
    [SerializeField, Min(10f), Tooltip("普通战斗航向衔接的最大角速度（度/秒），避免新航向直接要求反向急刹。")]
    private float courseTurnRate = 90f;
    [SerializeField, Min(0.1f), Tooltip("普通参考航速的变化率（米/秒²）；停止和紧急避障仍立即制动。")]
    private float courseAcceleration = 6f;
    [SerializeField, Min(0.1f), Tooltip("接近接敌半径时混入横移的距离（米）。")]
    private float engagementBlendDistance = 8f;
    [Header("Local 3D A Star")]
    [SerializeField, Min(2f)]
    private float pathCellSize = 6f;
    [SerializeField, Min(20f)]
    private float pathSearchRadius = 96f;
    [SerializeField, Range(64, 4096)]
    private int pathNodeBudget = 1024;
    [SerializeField, Min(0.5f)]
    private float pathReplanInterval = 2f;
    [SerializeField, Range(0f, 6f), Tooltip("栅格方向变化的附加米数代价；最终路线仍执行整舰净空简化。")]
    private float pathTurnPenalty = 2f;
    [SerializeField, Range(0f, 3f), Tooltip("运动学障碍短期包络预判秒数；平移和旋转余量各限12米。")]
    private float pathPredictionTime = 1.5f;
    private readonly EnemyLocalPath localPath = new EnemyLocalPath();
    [Header("Predictive Local Decisions")]
    [SerializeField, Tooltip("在爬升、A*与直达分支之后统一比较周围障碍和短期候选航迹。")]
    private bool usePredictiveDecisions = true;
    [SerializeField, Range(1f, 4f), Tooltip("短期航迹预判秒数；每次移动决策只采集一次周围物体，最多7个动作、每动作12段。")]
    private float decisionHorizon = 3f;
    [SerializeField, Range(0.25f, 6f), Tooltip("候选动作模拟的保守加减速度估计（米/秒²）；不是直接写入刚体的加速度。")]
    private float decisionAcceleration = 2f;
    [SerializeField, Range(20f, 160f), Tooltip("远场最小预读距离（米），与身位倍数、航速×预读秒数取最大值。")]
    private float farLookAheadDistance = 80f;
    [SerializeField, Range(3f, 12f), Tooltip("远场最少预读多少个舰体长度；总距离最多160米。")]
    private float farLookAheadBodyLengths = 8f;
    [SerializeField, Range(4f, 16f), Tooltip("按航速换算远场距离的提前秒数，远期运动风险与近处确定碰撞分别记录。")]
    private float farLookAheadSeconds = 10f;
    private readonly EnemyNavigationPrediction prediction = new EnemyNavigationPrediction();
    private bool predictiveBraking;
    private Vector3 navigationDestination;
    private bool navigationRequested;
    private Vector3 courseVelocity;
    private Bounds avoidanceBounds;
    private float probeDistance;
    private Rigidbody movementBody;
    private Vector3 steeringDirection;
    private float nextDirectionChange;
    private float nextStrafeChange;
    private float avoidanceSideUntil;
    private float avoidanceSide;
    private float strafeSide;
    private bool urgentAvoidance;
    private bool avoidingObstacle;
    private float obstacleSpeedRatio = 1f;
    public Vector3 SteeringDirection => steeringDirection;
    public int SteeringChanges
    {
        get; private set;
    }

    [Header("Obstacle Climb Priority")]
    [SerializeField, Tooltip("前方静态障碍有上方净空时，优先抬升越障，再考虑水平绕行。")]
    private bool preferObstacleClimb = true;
    [SerializeField, Min(1f)]
    private float climbLookAhead = 50f;
    [SerializeField, Min(1f), Tooltip("相对正常悬浮目标高度允许的最大额外爬升（米）。")]
    private float maxObstacleClimb = 32f;
    [SerializeField, Min(0f), Tooltip("无法向上越障时允许向下避让的最大高度差（米），仍验证地面及完整通道净空。")]
    private float maxObstacleDescent = 12f;
    [SerializeField, Min(0.5f)]
    private float obstacleClimbClearance = 3f;
    // 仅保留旧资产字段；当前速度比例由完整升降走廊与滑行余量决定。
    [SerializeField, HideInInspector]
    private float climbForwardSpeedRatio = 0.15f;
    private readonly EnemyObstacleClimb obstacleClimb = new EnemyObstacleClimb();
    private bool climbingObstacle;
    private float obstacleHoverHeight;
    private float _smoothedHoverHeight;
    private float nextDescentCheck;
    private float descentCheckHeight;
    private bool descentClear;
    [Header("AI Debug")]
    [SerializeField, Tooltip("在 Scene / Game 的 Gizmos 中显示自主友舰和敌机的决策；玩家驾驶时隐藏。")]
    private bool drawDebug;
    [SerializeField, Tooltip("显示索敌球、接近/撤退水平距离和守卫范围。")]
    private bool showDebugRanges = true;
    [SerializeField, Min(0.1f), Tooltip("移动输入向量的显示长度（米）。")]
    private float debugVectorLength = 8f;
    // 保存实际整舰扫掠快照，绘制不触发新的物理查询。
    private readonly Vector3[] debugProbeOrigins = new Vector3[1];
    private readonly Vector3[] debugProbeEnds = new Vector3[1];
    private readonly Vector3[] debugProbeNormals = new Vector3[1];
    private int debugProbeCount;
    private ControlUnit unit;
    private ControlUnit currentTarget;
    private float nextUpdateTime;
    private float nextMovementUpdateTime;
    private Vector3 desiredMovementInput;
    private bool _hasIdleHoverHeight;
    private float _idleHoverHeight;
    private Transform guardAnchor;
    private float guardRadius;
    private ControlUnit priorityTarget;
    private RaycastHit[] obstacleHits = new RaycastHit[MaxObstacleHits];
    private int _entityHashCode;
    internal void ConfigureDebugDisplay(bool enabled, bool ranges)
    {
        drawDebug = enabled;
        showDebugRanges = ranges;
    }

    public EnemyNavigationSnapshot CaptureNavigation(int previousRouteRevision = -1)
    {
        Vector3[] route = null;
        if (previousRouteRevision != localPath.Revision)
        {
            route = new Vector3[localPath.Points.Count];
            for (int i = 0; i < route.Length; i++)
            {
                route[i] = localPath.Points[i];
            }
        }

        return new EnemyNavigationSnapshot
        {
            position = transform.position,
            velocity = movementBody != null ? movementBody.linearVelocity : Vector3.zero,
            rotation = transform.rotation,
            angularVelocity = movementBody != null ? movementBody.angularVelocity : Vector3.zero,
            desiredInput = desiredMovementInput,
            referenceVelocity = courseVelocity,
            appliedInput = unit != null ? unit.MovementInput : Vector3.zero,
            steering = steeringDirection,
            targetHeight = unit != null && unit.hoverFlightController != null ? unit.hoverFlightController.targetHoverHeight : 0f,
            probeDistance = probeDistance,
            throttleRatio = obstacleSpeedRatio,
            hull = avoidanceBounds,
            avoiding = avoidingObstacle,
            changingHeight = climbingObstacle,
            urgent = urgentAvoidance,
            followingPath = localPath.Active,
            turns = SteeringChanges,
            routeRevision = localPath.Revision,
            expandedNodes = localPath.Expanded,
            waypoint = localPath.Waypoint,
            pathStatus = localPath.Status,
            routeChanged = route != null,
            replanReason = localPath.ReplanReason,
            planMilliseconds = localPath.PlanMilliseconds,
            predictionDecision = prediction.Decision,
            predictionRevision = prediction.Revision,
            predictedThreat = prediction.ThreatName,
            predictedThreatVelocity = prediction.ThreatVelocity,
            sensorRadius = prediction.SensorRadius,
            lookAheadDistance = prediction.LookAheadDistance,
            farObstacleDistance = prediction.FarObstacleDistance,
            farArrivalTime = prediction.FarArrivalTime,
            farThreat = prediction.FarThreat,
            farDecision = prediction.FarDecision,
            observedObstacles = prediction.Observed,
            movingObstacles = prediction.Moving,
            predictionCandidates = prediction.Candidates,
            predictionSegmentChecks = prediction.SegmentChecks,
            predictedCollisionTime = prediction.CollisionTime,
            selectedCollisionTime = prediction.SelectedCollisionTime,
            predictionMilliseconds = prediction.Milliseconds,
            predictionSaturated = prediction.Saturated,
            predictiveBraking = predictiveBraking,
            predictedVelocity = prediction.SelectedVelocity,
            route = route
        };
    }

    private void OnEnable()
    {
        // 同批生成单位错开自主扫描；明确任务指派仍会立即清零调度。
        float phase = (_entityHashCode & 1023) / 1024f;
        nextUpdateTime = Time.time + phase * Mathf.Max(0.01f, updateInterval);
        nextMovementUpdateTime = 0f;
        localPath.Clear();
        obstacleClimb.Clear();
        prediction.Clear();
        navigationRequested = false;
        steeringDirection = Vector3.zero;
        courseVelocity = movementBody != null ? Vector3.ProjectOnPlane(movementBody.linearVelocity, Vector3.up) : Vector3.zero;
        SteeringChanges = 0;
        nextDirectionChange = 0f;
        nextStrafeChange = 0f;
        strafeSide = 0f;
        avoidanceSideUntil = 0f;
        _smoothedHoverHeight = 0f;
        nextDescentCheck = 0f;
    }

    private void Awake()
    {
        unit = GetComponent<ControlUnit>();
        movementBody = GetComponent<Rigidbody>();
        _entityHashCode = GetEntityId().GetHashCode();
    }

    private void FixedUpdate()
    {
        // Sample targets/obstacles at a bounded rate, while interpolating input every physics tick.
        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            return;
        }

        if (unit == null || !unit.HasValidCockpit || unit.faction == UnitFaction.Neutral || unit.AcceptsPlayerInput)
        {
            return;
        }

        if (!ReferenceEquals(currentTarget, null) && !IsValidOpponent(currentTarget))
        {
            currentTarget = null;
            desiredMovementInput = Vector3.zero;
            unit.SetTarget(null);
            nextUpdateTime = 0f;
            nextMovementUpdateTime = 0f;
        }

        if (Time.time >= nextUpdateTime)
        {
            ControlUnit previousTarget = currentTarget;
            currentTarget = FindNearestOpponent();
            if (previousTarget != currentTarget)
            {
                localPath.Clear();
                obstacleClimb.Clear();
                climbingObstacle = false;
                nextMovementUpdateTime = 0f;
            }

            unit.SetTarget(currentTarget != null ? currentTarget.transform : null);
            nextUpdateTime = Time.time + Mathf.Max(0.01f, updateInterval);
        }

        if (currentTarget == null)
        {
            if (Time.time >= nextMovementUpdateTime)
            {
                debugProbeCount = 0;
                Vector3 toGuard = guardAnchor != null ? guardAnchor.position - transform.position : Vector3.zero;
                toGuard.y = 0f;
                climbingObstacle = false;
                desiredMovementInput = toGuard.sqrMagnitude > 100f ? PredictMovement(ApplyObstacleAvoidance(GetApproachDirection(guardAnchor.position, toGuard.normalized), toGuard)) : Vector3.zero;
                nextMovementUpdateTime = Time.time + GetMovementInterval();
            }

            ConfigureHoverThrusters();
            unit.SetMovementInput(LimitObstacleInput(Vector3.MoveTowards(
                unit.MovementInput,
                GetVelocityInput(),
                Mathf.Max(0.1f, movementResponseRate) * Time.fixedDeltaTime)));
            return;
        }

        Vector3 toTarget = currentTarget.transform.position - transform.position;
        Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        if (Time.time >= nextMovementUpdateTime)
        {
            debugProbeCount = 0;
            desiredMovementInput = CalculateDesiredMovement(flatDirection);
            nextMovementUpdateTime = Time.time + GetMovementInterval();
        }

        ConfigureHoverThrusters();
        Vector3 smoothedInput = Vector3.MoveTowards(
            unit.MovementInput,
            GetVelocityInput(),
            Mathf.Max(0.1f, movementResponseRate) * Time.fixedDeltaTime);
        unit.SetMovementInput(LimitObstacleInput(smoothedInput));
    }

    private Vector3 CalculateDesiredMovement(Vector3 flatDirection)
    {
        navigationRequested = false;
        float distance = flatDirection.magnitude;
        Vector3 desiredMove = Vector3.zero;
        if (currentTarget == priorityTarget || distance <= detectionRange)
        {
            Vector3 approach = GetApproachDirection(currentTarget.transform.position, flatDirection.normalized);
            navigationRequested = climbingObstacle || localPath.Active || distance > desiredDistance;
            if (climbingObstacle || localPath.Active || distance > desiredDistance)
            {
                // 翻越期间保持前进任务，不因进入接敌距离而切换横移。
                if (climbingObstacle || localPath.Active)
                {
                    desiredMove = approach;
                }
                else
                {
                    float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(desiredDistance, desiredDistance + engagementBlendDistance, distance));
                    desiredMove = Vector3.Slerp(GetStrafeDirection(flatDirection), approach, blend);
                }
            }
            else if (distance < retreatDistance)
            {
                desiredMove = -flatDirection.normalized;
            }
            else
            {
                desiredMove = GetStrafeDirection(flatDirection);
            }

            desiredMove = PredictMovement(ApplyObstacleAvoidance(desiredMove, flatDirection));
        }

        return desiredMove;
    }

    private Vector3 GetApproachDirection(Vector3 destination, Vector3 fallback)
    {
        navigationDestination = new Vector3(destination.x, GetBaseHoverHeight(), destination.z);
        navigationRequested = true;
        return fallback;
    }

    private Vector3 PredictMovement(Vector3 proposed)
    {
        predictiveBraking = false;
        if (!usePredictiveDecisions)
        {
            prediction.Clear();
            return proposed;
        }

        prediction.MinimumDistance = farLookAheadDistance;
        prediction.BodyLengths = farLookAheadBodyLengths;
        prediction.LookAheadSeconds = farLookAheadSeconds;
        prediction.TaskTarget = currentTarget;
        Vector3 selected = prediction.Choose(
            unit,
            avoidanceBounds,
            movementBody != null ? movementBody.linearVelocity : Vector3.zero,
            proposed,
            climbingObstacle ? obstacleHoverHeight : GetBaseHoverHeight(),
            obstacleLayers,
            cruiseSpeed,
            decisionHorizon,
            decisionAcceleration,
            courseTurnRate,
            localPath.Active || climbingObstacle,
            navigationRequested ? navigationDestination : transform.position + proposed * farLookAheadDistance,
            localPath.Active ? localPath.Points : null,
            localPath.Waypoint);
        if (prediction.CollisionTime >= 0f || prediction.Saturated)
        {
            // 安全预判必须能覆盖普通航向保持；只改变意图，真实施力继续由推进器负责。
            urgentAvoidance = true;
            // 让行需要真实推进器反推来消减惯性；只收油无法执行预测中的等待动作。
            predictiveBraking = selected.magnitude + 0.05f < proposed.magnitude || prediction.Saturated;
            selected = StabilizeDirection(selected);
        }

        return selected;
    }

    private float GetMovementInterval()
    {
        return Mathf.Max(0.5f, movementUpdateInterval);
    }

    private Vector3 GetVelocityInput()
    {
        Vector3 velocity = movementBody != null ? Vector3.ProjectOnPlane(movementBody.linearVelocity, Vector3.up) : Vector3.zero;
        Vector3 targetVelocity = desiredMovementInput * cruiseSpeed;
        if (urgentAvoidance || targetVelocity.sqrMagnitude < 0.0001f)
        {
            // 危险和真正结束任务不能被平滑层延迟；无目标仍正常制动。
            courseVelocity = targetVelocity;
        }
        else
        {
            float speed = Mathf.MoveTowards(
                courseVelocity.magnitude,
                targetVelocity.magnitude,
                Mathf.Max(0.1f, courseAcceleration) * Time.fixedDeltaTime);
            if (courseVelocity.sqrMagnitude < 0.0001f)
            {
                courseVelocity = targetVelocity.normalized * speed;
            }
            else
            {
                // 在水平面转弯并单独保持航速；两方向线性插值会在折返中穿过零速。
                float angle = Vector3.SignedAngle(courseVelocity, targetVelocity, Vector3.up);
                float step = Mathf.Clamp(angle, -courseTurnRate * Time.fixedDeltaTime, courseTurnRate * Time.fixedDeltaTime);
                courseVelocity = Quaternion.AngleAxis(step, Vector3.up) * courseVelocity.normalized * speed;
            }
        }

        return LimitObstacleInput(Vector3.ClampMagnitude((courseVelocity - velocity) / Mathf.Max(1f, cruiseSpeed * 0.5f), 1f));
    }

    private Vector3 LimitObstacleInput(Vector3 input)
    {
        if (avoidingObstacle && movementBody != null)
        {
            Vector3 flatVelocity = Vector3.ProjectOnPlane(movementBody.linearVelocity, Vector3.up);
            if (flatVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 direction = flatVelocity.normalized;
                float forward = Vector3.Dot(input, direction);
                if (forward > 0f)
                {
                    // 遇障按接近比例缩减前向推力，不施加反向推力。
                    input -= direction * (forward * (1f - obstacleSpeedRatio));
                }
                else if (forward < 0f && !predictiveBraking)
                {
                    input -= direction * forward;
                }
            }
        }

        return input;
    }

    private Vector3 StabilizeDirection(Vector3 candidate)
    {
        float magnitude = candidate.magnitude;
        if (magnitude < 0.01f)
        {
            return Vector3.zero;
        }

        Vector3 direction = candidate / magnitude;
        if (steeringDirection == Vector3.zero)
        {
            steeringDirection = direction;
            nextDirectionChange = Time.time + directionHoldTime;
        }
        else if (Vector3.Angle(steeringDirection, direction) >= directionDeadZone && (urgentAvoidance || Time.time >= nextDirectionChange))
        {
            steeringDirection = direction;
            nextDirectionChange = Time.time + directionHoldTime;
            SteeringChanges++;
        }

        return steeringDirection * magnitude;
    }

    public void SetGuardAnchor(Transform anchor, float radius)
    {
        guardAnchor = anchor;
        guardRadius = Mathf.Max(1f, radius);
        nextUpdateTime = 0f;
        nextMovementUpdateTime = 0f;
    }

    public void SetPriorityTarget(ControlUnit targetUnit)
    {
        priorityTarget = targetUnit;
        nextUpdateTime = 0f;
        nextMovementUpdateTime = 0f;
    }

    private ControlUnit FindNearestOpponent()
    {
        // 任务目标由波次明确指派；索敌半径只限制自主发现，不能让远处增援失去进攻方向。
        if (IsValidOpponent(priorityTarget))
        {
            return priorityTarget;
        }

        var units = PlayManager.instance != null ? PlayManager.instance.GetControlUnits() : null;
        ControlUnit nearest = null;
        float nearestSqrDistance = detectionRange * detectionRange;
        if (units == null)
        {
            return null;
        }

        foreach (ControlUnit candidate in units)
        {
            // The registry can retain a disabled build-mode construct. Hidden units are not targets.
            if (candidate == null || candidate == unit || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            if (!IsValidOpponent(candidate))
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }
        }

        // 保留仍在发现半径内的有效目标，除非新目标明显更近；任务优先级不受影响。
        if (IsValidOpponent(currentTarget))
        {
            float currentDistance = (currentTarget.transform.position - transform.position).sqrMagnitude;
            float ratio = Mathf.Clamp(targetSwitchDistanceRatio, 0.1f, 1f);
            if (currentDistance <= detectionRange * detectionRange && (nearest == null || nearestSqrDistance >= currentDistance * ratio * ratio))
            {
                return currentTarget;
            }
        }

        return nearest;
    }

    private bool IsValidOpponent(ControlUnit candidate)
    {
        if (candidate == null || candidate == unit || !candidate.isActiveAndEnabled || !candidate.IsOperational || !candidate.IsSimulationActive || candidate.gameObject.scene != gameObject.scene)
        {
            return false;
        }

        if (candidate.faction == UnitFaction.Neutral || candidate.faction == unit.faction)
        {
            return false;
        }

        return guardAnchor == null || (candidate.transform.position - guardAnchor.position).sqrMagnitude <= guardRadius * guardRadius;
    }

    private void OnDisable()
    {
        if (unit != null && !unit.AcceptsPlayerInput)
        {
            unit.SetMovementInput(Vector3.zero);
            unit.SetTarget(null);
        }

        prediction.Clear();
        predictiveBraking = false;
        prediction.TaskTarget = null;
        localPath.Clear();
        obstacleClimb.Clear();
        navigationRequested = false;
        debugProbeCount = 0;
        currentTarget = null;
        desiredMovementInput = Vector3.zero;
        courseVelocity = Vector3.zero;
        avoidingObstacle = false;
        nextUpdateTime = 0f;
        _hasIdleHoverHeight = false;
        climbingObstacle = false;
        obstacleClimb.Clear();
        nextMovementUpdateTime = 0f;
        _smoothedHoverHeight = 0f;
    }

    private void ConfigureHoverThrusters()
    {
        if (unit.hoverThrusters != null)
        {
            foreach (HoverThruster hoverThruster in unit.hoverThrusters)
            {
                if (hoverThruster != null)
                {
                    hoverThruster.isHovered = true;
                }
            }
        }

        if (unit.hoverFlightController != null)
        {
            unit.hoverFlightController.setHeight = true;
            float targetHeight = GetBaseHoverHeight();
            if (climbingObstacle)
            {
                targetHeight = obstacleHoverHeight;
            }

            // 越障结束后高度需平滑回落，避免 PID 看到大误差产生向下猛冲。
            float desired = Mathf.Max(2f, ConstrainDescent(targetHeight, steeringDirection));
            if (_smoothedHoverHeight < 1f)
            {
                _smoothedHoverHeight = desired;
            }
            else if (!climbingObstacle && desired < _smoothedHoverHeight)
            {
                _smoothedHoverHeight = Mathf.MoveTowards(_smoothedHoverHeight, desired, 4f * Time.fixedDeltaTime);
            }
            else
            {
                _smoothedHoverHeight = desired;
            }

            // 高度求解器测量所在模块的位置，导航高度统一以舰体根节点为基准。
            unit.hoverFlightController.targetHoverHeight = _smoothedHoverHeight + unit.hoverFlightController.transform.position.y - transform.position.y;
        }
    }

    private float ConstrainDescent(float requested, Vector3 direction)
    {
        if (requested >= transform.position.y - 1f || _smoothedHoverHeight < 1f)
        {
            return requested;
        }

        if (Time.time >= nextDescentCheck || Mathf.Abs(requested - descentCheckHeight) > 0.5f)
        {
            descentCheckHeight = requested;
            nextDescentCheck = Time.time + GetMovementInterval();
            descentClear = obstacleClimb.CanLowerTo(unit, requested, direction, Mathf.Max(probeDistance, climbLookAhead), obstacleLayers);
        }

        // 路线结束或重规划失败不代表下层已安全，保留最后安全航层，避免跌入下一组建筑。
        return descentClear ? requested : Mathf.Max(_smoothedHoverHeight, transform.position.y);
    }

    private float GetBaseHoverHeight()
    {
        if (currentTarget != null)
        {
            _hasIdleHoverHeight = false;
            return currentTarget.transform.position.y + targetHoverHeightOffset;
        }

        // 丢失目标时固定高度，不能追随下落中的根节点不断降低设定值。
        if (!_hasIdleHoverHeight)
        {
            _idleHoverHeight = unit.hoverFlightController != null ? unit.hoverFlightController.CurrentHeight : transform.position.y;
            _hasIdleHoverHeight = true;
        }

        return _idleHoverHeight;
    }

    private Vector3 GetStrafeDirection(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude < 0.01f)
        {
            return Vector3.zero;
        }

        if (Time.time >= nextStrafeChange)
        {
            strafeSide = strafeSide == 0f ? ((_entityHashCode & 1) == 0 ? 1f : -1f) : -strafeSide;
            nextStrafeChange = Time.time + strafeSwitchInterval;
        }

        return Vector3.Cross(Vector3.up, flatDirection.normalized) * strafeSide * 0.45f;
    }

    private Vector3 ApplyObstacleAvoidance(Vector3 desiredMove, Vector3 targetDirection)
    {
        urgentAvoidance = false;
        avoidingObstacle = false;
        obstacleSpeedRatio = 1f;
        Vector3 velocity = movementBody != null ? movementBody.linearVelocity : Vector3.zero;
        climbingObstacle = false;
        Vector3 probeDirection = velocity.normalized;
        avoidanceBounds = obstacleClimb.GetHullBounds(unit);
        Vector3 extents = avoidanceBounds.extents;
        extents.x += 0.25f;
        extents.z += 0.25f;
        avoidanceBounds.extents = extents;
        float horizon = Mathf.Max(GetMovementInterval(), avoidancePredictionTime);
        float speed = velocity.magnitude;
        // 速度越高，基础时间预判之外再加入刹停距离，提前量单调增加。
        float brakingDistance = speed * speed / (2f * Mathf.Max(0.1f, predictionBrakingAcceleration));
        probeDistance = Mathf.Max(0f, obstacleAvoidanceDistance) + speed * horizon + brakingDistance;
        localPath.TurnPenalty = pathTurnPenalty;
        localPath.PredictionTime = pathPredictionTime;
        climbingObstacle = !localPath.Active && preferObstacleClimb && unit.hoverFlightController != null && (speed >= 0.1f || obstacleClimb.Active) && obstacleClimb.TryGetHeight(
            unit,
            probeDirection,
            GetBaseHoverHeight(),
            Mathf.Max(probeDistance, climbLookAhead),
            maxObstacleClimb,
            obstacleClimbClearance,
            obstacleLayers,
            out obstacleHoverHeight,
            maxObstacleDescent);
        if (climbingObstacle)
        {
            avoidingObstacle = true;
            float ratio = obstacleClimb.ForwardRatio(
                unit,
                desiredMove,
                obstacleHoverHeight,
                cruiseSpeed,
                obstacleClimbClearance,
                horizon,
                obstacleLayers);
            obstacleSpeedRatio = ratio;
            urgentAvoidance = ratio < 0.25f;
            return StabilizeDirection(desiredMove) * ratio;
        }

        if (navigationRequested && unit.hoverFlightController != null && localPath.TryFollow(
            unit,
            avoidanceBounds,
            navigationDestination,
            obstacleLayers,
            pathCellSize,
            pathSearchRadius,
            pathNodeBudget,
            pathReplanInterval,
            Mathf.Max(2f, GetBaseHoverHeight() - maxObstacleDescent),
            GetBaseHoverHeight() + maxObstacleClimb,
            out Vector3 waypoint))
        {
            climbingObstacle = true;
            avoidingObstacle = true;
            Vector3 alongPath = Vector3.ProjectOnPlane(waypoint - transform.position, Vector3.up);
            obstacleHoverHeight = ConstrainDescent(waypoint.y, alongPath);
            float heightRatio = obstacleClimb.ForwardRatio(
                unit,
                alongPath,
                obstacleHoverHeight,
                cruiseSpeed,
                obstacleClimbClearance,
                horizon,
                obstacleLayers);
            urgentAvoidance = heightRatio < 0.25f;
            obstacleSpeedRatio = heightRatio;
            if (speed >= 0.1f)
            {
                // 规划路线不替代惯性安全检测，仍按当前真实速度限制迎障推力。
                ProbeObstacle(probeDirection, 1f);
            }

            // 高度与水平同时跟随；现高度走廊余量决定水平速度，航点距离本身不要求停车。
            return StabilizeDirection(alongPath.normalized) * heightRatio;
        }

        if (speed < 0.1f)
        {
            debugProbeCount = 0;
            return StabilizeDirection(desiredMove);
        }

        if (Time.time >= avoidanceSideUntil)
        {
            avoidanceSide = (_entityHashCode & 1) == 0 ? 1f : -1f;
        }

        Vector3 avoidance = ProbeObstacle(probeDirection, 1f);
        if (avoidance.sqrMagnitude > 0.01f)
        {
            avoidanceSideUntil = Time.time + avoidanceSideHoldTime;
            desiredMove += avoidance.normalized * obstacleAvoidanceWeight;
        }

        Vector3 result = StabilizeDirection(Vector3.ClampMagnitude(desiredMove, 1f));
        if (avoidingObstacle)
        {
            Vector3 flatVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up).normalized;
            result -= flatVelocity * Mathf.Min(0f, Vector3.Dot(result, flatVelocity));
            result *= obstacleSpeedRatio;
        }

        return result;
    }

    private Vector3 ProbeObstacle(Vector3 direction, float weight)
    {
        // BoxCast自身包含舰体前沿；平移整个盒子会漏掉贴近舰头的障碍。
        Vector3 origin = avoidanceBounds.center;
        PhysicsScene physics = gameObject.scene.GetPhysicsScene();
        int hitCount = physics.BoxCast(
            origin,
            avoidanceBounds.extents,
            direction.normalized,
            obstacleHits,
            Quaternion.identity,
            probeDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);
        // 饱和结果不保证包含最近障碍；只在首次遇到更密集布局时扩容。
        while (hitCount == obstacleHits.Length && obstacleHits.Length < 256)
        {
            System.Array.Resize(ref obstacleHits, obstacleHits.Length * 2);
            hitCount = physics.BoxCast(
                origin,
                avoidanceBounds.extents,
                direction.normalized,
                obstacleHits,
                Quaternion.identity,
                probeDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);
        }

        int debugIndex = debugProbeCount < debugProbeOrigins.Length ? debugProbeCount++ : -1;
        if (debugIndex >= 0)
        {
            debugProbeOrigins[debugIndex] = origin;
            debugProbeEnds[debugIndex] = origin + direction.normalized * probeDistance;
            debugProbeNormals[debugIndex] = Vector3.zero;
        }

        if (hitCount == 0)
        {
            return Vector3.zero;
        }

        int closest = -1;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = obstacleHits[i];
            ControlUnit hitUnit = hit.collider.GetComponentInParent<ControlUnit>();
            // BoxCast 起点可能与地面/屋顶接触；它们不构成水平墙。
            Bounds obstacleBounds = hit.collider.bounds;
            if (obstacleBounds.max.y <= avoidanceBounds.min.y + 0.05f || obstacleBounds.min.y >= avoidanceBounds.max.y - 0.05f)
            {
                continue;
            }

            if (hitUnit == unit)
            {
                continue;
            }

            Vector3 surfaceNormal = hit.normal;
            if (hit.distance <= 0f)
            {
                // 初始重叠使用几何分离方向；远离墙面时不能被反向扫掠伪法线拉回。
                Vector3 separation = origin - hit.collider.ClosestPoint(origin);
                if (separation.sqrMagnitude <= 0.0001f)
                {
                    // 中心恰在表面/内部时 ClosestPoint 无方向，取包围盒最近面；地板返回竖直法线。
                    Vector3 offset = origin - obstacleBounds.center;
                    Vector3 faceDistance = obstacleBounds.extents - new Vector3(Mathf.Abs(offset.x), Mathf.Abs(offset.y), Mathf.Abs(offset.z));
                    if (faceDistance.y <= faceDistance.x && faceDistance.y <= faceDistance.z)
                    {
                        separation = Vector3.up * (offset.y >= 0f ? 1f : -1f);
                    }
                    else
                    {
                        separation = faceDistance.x <= faceDistance.z ? Vector3.right * Mathf.Sign(offset.x) : Vector3.forward * Mathf.Sign(offset.z);
                    }
                }

                if (separation.sqrMagnitude > 0.0001f)
                {
                    surfaceNormal = separation.normalized;
                    if (Vector3.Dot(surfaceNormal, direction) >= -0.001f)
                    {
                        continue;
                    }
                }
            }

            if (Mathf.Abs(surfaceNormal.y) > 0.7f)
            {
                continue;
            }

            hit.normal = surfaceNormal;
            obstacleHits[i] = hit;
            if (hit.distance < closestDistance)
            {
                closest = i;
                closestDistance = hit.distance;
            }
        }

        if (closest >= 0)
        {
            RaycastHit hit = obstacleHits[closest];
            if (debugIndex >= 0)
            {
                // 显示整舰包围盒终点与实际使用的表面法线。
                debugProbeEnds[debugIndex] = origin + direction.normalized * hit.distance;
                debugProbeNormals[debugIndex] = hit.normal;
            }

            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.01f)
            {
                normal = -direction;
            }

            if (Time.time >= avoidanceSideUntil)
            {
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                float lateral = Vector3.Dot(hit.collider.bounds.center - origin, right);
                // 障碍偏右则从左绕；正前方保持既有确定性选择，避免左右抖动。
                if (Mathf.Abs(lateral) > 0.25f)
                {
                    avoidanceSide = lateral > 0f ? -1f : 1f;
                }

                avoidanceSideUntil = Time.time + avoidanceSideHoldTime;
            }

            avoidingObstacle = true;
            obstacleSpeedRatio = Mathf.Min(obstacleSpeedRatio, Mathf.Clamp01(hit.distance / Mathf.Max(1f, probeDistance)));
            float distanceFactor = 1f - Mathf.Clamp01(hit.distance / Mathf.Max(0.1f, probeDistance));
            urgentAvoidance |= hit.distance < Mathf.Max(3f, obstacleProbeRadius * 2f);
            Vector3 sideStep = Vector3.Cross(Vector3.up, direction).normalized * avoidanceSide;
            // 表面法线仅用于侧向引导，不能产生与实际水平速度相反的推进意图。
            Vector3 forward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Vector3 lateralNormal = normal.normalized - forward * Vector3.Dot(normal.normalized, forward);
            return (lateralNormal + sideStep * 0.6f) * distanceFactor * weight;
        }

        return Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        DrawDebug();
    }

    private void DrawDebug()
    {
        bool globallyVisible = DebugManager.instance != null && DebugManager.instance.gameObject.scene == gameObject.scene && DebugManager.instance.ShowsAI(unit != null ? unit.faction : UnitFaction.Neutral);
        if ((!drawDebug && !globallyVisible) || !Application.isPlaying || !isActiveAndEnabled || unit == null || !unit.HasValidCockpit || unit.AcceptsPlayerInput || unit.faction == UnitFaction.Neutral || PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            return;
        }

        Color previousColor = Gizmos.color;
        Vector3 origin = transform.position;
        if (usePredictiveDecisions && prediction.LookAheadDistance > 0f)
        {
            // 青色圆表示远场预读范围；原BoxCast盒线只表示近处即时安全检测。
            DrawDebugCircle(origin, prediction.LookAheadDistance, new Color(0f, 0.75f, 1f, 0.7f));
        }

        Gizmos.color = unit.faction == UnitFaction.Player ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, 1f);
        if (showDebugRanges)
        {
            Gizmos.DrawWireSphere(origin, Mathf.Max(0f, detectionRange));
            DrawDebugCircle(origin, desiredDistance, Color.yellow);
            DrawDebugCircle(origin, retreatDistance, new Color(1f, 0.4f, 0f));
            if (guardAnchor != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, guardAnchor.position);
                Gizmos.DrawWireSphere(guardAnchor.position, guardRadius);
            }
        }

        if (currentTarget != null && IsValidOpponent(currentTarget))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, currentTarget.transform.position);
            Gizmos.DrawWireSphere(currentTarget.transform.position, 1.5f);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(origin, desiredMovementInput * debugVectorLength);
        Gizmos.color = Color.white;
        Gizmos.DrawRay(origin, unit.MovementInput * debugVectorLength);
        for (int i = 0; i < debugProbeCount; i++)
        {
            Gizmos.color = debugProbeNormals[i] == Vector3.zero ? Color.green : Color.red;
            Gizmos.DrawLine(debugProbeOrigins[i], debugProbeEnds[i]);
            Gizmos.DrawWireCube(debugProbeEnds[i], avoidanceBounds.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(debugProbeEnds[i], debugProbeNormals[i] * 2f);
        }

        Gizmos.color = new Color(1f, 0.6f, 0f);
        Vector3 previous = origin;
        foreach (Vector3 point in localPath.Points)
        {
            Gizmos.DrawLine(previous, point);
            Gizmos.DrawWireSphere(point, 0.6f);
            previous = point;
        }

        Gizmos.color = previousColor;
    }

    private static void DrawDebugCircle(Vector3 origin, float radius, Color color)
    {
        Gizmos.color = color;
        const int segments = 48;
        Vector3 previous = origin + Vector3.right * Mathf.Max(0f, radius);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * (Mathf.PI * 2f / segments);
            Vector3 next = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Mathf.Max(0f, radius);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
