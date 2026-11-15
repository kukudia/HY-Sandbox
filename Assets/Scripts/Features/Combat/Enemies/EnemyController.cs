// 职责：为敌机与自主友舰选择敌对目标并生成驾驶意图；阵营和玩家驾驶权分别判断。
// 入口：FixedUpdate()；战术意图交给 VesselNavigation 选路，ControlUnit/推进器负责物理执行。
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ControlUnit))]
public class EnemyController : MonoBehaviour
{
    // Produces smoothed movement intent; ControlUnit and thrusters remain the only physics writers.
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
    [HideInInspector]
    public float obstacleProbeRadius = 1.5f; // 旧资产兼容；当前使用整舰 Bounds。
    [HideInInspector]
    public float obstacleAvoidanceWeight = 1.4f; // 旧资产兼容；共享导航选择路线，不叠加斥力。
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
    private VesselNavigation navigation;
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
    private float strafeSide;
    private bool urgentAvoidance;
    private bool navigationOwnsDirection;
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
    private int _entityHashCode;
    internal void ConfigureDebugDisplay(bool enabled, bool ranges)
    {
        drawDebug = enabled;
        showDebugRanges = ranges;
    }

    public EnemyNavigationSnapshot CaptureNavigation(int previousRouteRevision = -1)
    {
        Vector3[] route = null;
        if (previousRouteRevision != navigation.Path.Revision)
        {
            route = new Vector3[navigation.Path.Points.Count];
            for (int i = 0; i < route.Length; i++)
            {
                route[i] = navigation.Path.Points[i];
            }
        }

        return new EnemyNavigationSnapshot
        {
            targetName = currentTarget != null ? currentTarget.name : string.Empty,
            hasTarget = currentTarget != null,
            targetPosition = currentTarget != null ? currentTarget.transform.position : Vector3.zero,
            navigationDestination = navigationDestination,
            navigationRequested = navigationRequested,
            controllerActive = isActiveAndEnabled,
            localDetouring = navigation.Local.Detouring,
            localQueryCount = navigation.Local.QueryCount,
            localSaturated = navigation.Local.Saturated,
            sensorColliders = navigation.Prediction.RawColliderCount,
            sensorCapacity = navigation.Prediction.SensorCapacity,
            engagementDistance = desiredDistance,
            acquisitionDistance = detectionRange,
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
            followingPath = navigation.Path.Active,
            turns = SteeringChanges,
            routeRevision = navigation.Path.Revision,
            expandedNodes = navigation.Path.Expanded,
            waypoint = navigation.Path.Waypoint,
            pathStatus = navigation.Path.Status,
            routeChanged = route != null,
            replanReason = navigation.Path.ReplanReason,
            planMilliseconds = navigation.Path.PlanMilliseconds,
            predictionDecision = navigation.Prediction.Decision,
            predictionRevision = navigation.Prediction.Revision,
            predictedThreat = navigation.Prediction.ThreatName,
            predictedThreatVelocity = navigation.Prediction.ThreatVelocity,
            sensorRadius = navigation.Prediction.SensorRadius,
            lookAheadDistance = navigation.Prediction.LookAheadDistance,
            farObstacleDistance = navigation.Prediction.FarObstacleDistance,
            farArrivalTime = navigation.Prediction.FarArrivalTime,
            farThreat = navigation.Prediction.FarThreat,
            farDecision = navigation.Prediction.FarDecision,
            observedObstacles = navigation.Prediction.Observed,
            movingObstacles = navigation.Prediction.Moving,
            predictionCandidates = navigation.Prediction.Candidates,
            predictionSegmentChecks = navigation.Prediction.SegmentChecks,
            predictedCollisionTime = navigation.Prediction.CollisionTime,
            selectedCollisionTime = navigation.Prediction.SelectedCollisionTime,
            predictionMilliseconds = navigation.Prediction.Milliseconds,
            predictionSaturated = navigation.Prediction.Saturated,
            predictiveBraking = predictiveBraking,
            predictedVelocity = navigation.Prediction.SelectedVelocity,
            route = route
        };
    }

    private void OnEnable()
    {
        // 同批生成单位错开自主扫描；明确任务指派仍会立即清零调度。
        float phase = (_entityHashCode & 1023) / 1024f;
        nextUpdateTime = Time.time + phase * Mathf.Max(0.01f, updateInterval);
        nextMovementUpdateTime = 0f;
        navigation.Reset();
        navigationOwnsDirection = false;
        navigationRequested = false;
        steeringDirection = Vector3.zero;
        courseVelocity = movementBody != null ? Vector3.ProjectOnPlane(movementBody.linearVelocity, Vector3.up) : Vector3.zero;
        SteeringChanges = 0;
        nextDirectionChange = 0f;
        nextStrafeChange = 0f;
        strafeSide = 0f;
        _smoothedHoverHeight = 0f;
        nextDescentCheck = 0f;
    }

    private void Awake()
    {
        unit = GetComponent<ControlUnit>();
        navigation = new VesselNavigation(transform);
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
                navigation.Reset();
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
            navigationRequested = climbingObstacle || navigation.Path.Active || distance > desiredDistance;
            if (climbingObstacle || navigation.Path.Active || distance > desiredDistance)
            {
                // 翻越期间保持前进任务，不因进入接敌距离而切换横移。
                if (climbingObstacle || navigation.Path.Active)
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
            navigation.Prediction.Clear();
            return proposed;
        }

        Vector3 selected = navigation.Prediction.Choose(
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
            navigation.Path.Active || climbingObstacle,
            navigationRequested ? navigationDestination : transform.position + proposed * farLookAheadDistance,
            navigation.Path.Active ? navigation.Path.Points : null,
            navigation.Path.Waypoint);
        if (navigation.Prediction.CollisionTime >= 0f || navigation.Prediction.Saturated)
        {
            // 安全预判必须能覆盖普通航向保持；只改变意图，真实施力继续由推进器负责。
            urgentAvoidance = true;
            // 让行需要真实推进器反推来消减惯性；只收油无法执行预测中的等待动作。
            predictiveBraking = selected.magnitude + 0.05f < proposed.magnitude || navigation.Prediction.Saturated;
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

        Vector3 safeVelocity = navigation.ConstrainVelocity(unit, courseVelocity, velocity, predictionBrakingAcceleration);
        if ((safeVelocity - courseVelocity).sqrMagnitude > 0.0001f)
        {
            urgentAvoidance = true;
            avoidingObstacle = true;
            predictiveBraking = true;
            obstacleSpeedRatio = 0f;
            courseVelocity = safeVelocity;
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
        else if (Vector3.Angle(steeringDirection, direction) >= directionDeadZone && (urgentAvoidance || navigationOwnsDirection || Time.time >= nextDirectionChange))
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

        navigation.Reset();
        navigationOwnsDirection = false;
        predictiveBraking = false;
        navigationRequested = false;
        currentTarget = null;
        desiredMovementInput = Vector3.zero;
        courseVelocity = Vector3.zero;
        avoidingObstacle = false;
        nextUpdateTime = 0f;
        _hasIdleHoverHeight = false;
        climbingObstacle = false;
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
            descentClear = navigation.Climb.CanLowerTo(unit, requested, direction, Mathf.Max(probeDistance, climbLookAhead), obstacleLayers);
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
        var settings = new VesselNavigationSettings
        {
            ObstacleAvoidanceDistance = obstacleAvoidanceDistance,
            AvoidancePredictionTime = avoidancePredictionTime,
            PredictionBrakingAcceleration = predictionBrakingAcceleration,
            PathTurnPenalty = pathTurnPenalty,
            PathPredictionTime = pathPredictionTime,
            UsePredictiveDecisions = usePredictiveDecisions,
            FarLookAheadDistance = farLookAheadDistance,
            FarLookAheadBodyLengths = farLookAheadBodyLengths,
            FarLookAheadSeconds = farLookAheadSeconds,
            CruiseSpeed = cruiseSpeed,
            DecisionHorizon = decisionHorizon,
            PathCellSize = pathCellSize,
            PathSearchRadius = pathSearchRadius,
            PathNodeBudget = pathNodeBudget,
            PathReplanInterval = pathReplanInterval,
            MaxObstacleDescent = maxObstacleDescent,
            MaxObstacleClimb = maxObstacleClimb,
            PreferObstacleClimb = preferObstacleClimb,
            ClimbLookAhead = climbLookAhead,
            ObstacleClimbClearance = obstacleClimbClearance,
            ObstacleLayers = obstacleLayers,
            AvoidanceSideHoldTime = avoidanceSideHoldTime,
            MovementInterval = GetMovementInterval(),
            BaseHoverHeight = GetBaseHoverHeight()
        };
        Vector3 movement = navigation.Evaluate(
            unit,
            movementBody,
            desiredMove,
            navigationDestination,
            navigationRequested,
            currentTarget,
            settings);
        avoidanceBounds = navigation.avoidanceBounds;
        probeDistance = navigation.probeDistance;
        avoidingObstacle = navigation.avoidingObstacle;
        urgentAvoidance = navigation.urgentAvoidance;
        obstacleSpeedRatio = navigation.obstacleSpeedRatio;
        climbingObstacle = navigation.climbingObstacle;
        if (climbingObstacle)
        {
            obstacleHoverHeight = ConstrainDescent(navigation.obstacleHoverHeight, movement);
        }

        // 路线与局部绕行已有自己的方向承诺；再等待战斗航向冷却会在拐点停滞。
        navigationOwnsDirection = navigation.Path.Active || navigation.Local.Detouring || climbingObstacle;
        return StabilizeDirection(movement);
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
        if (usePredictiveDecisions && navigation.Prediction.LookAheadDistance > 0f)
        {
            // 青色圆表示远场预读范围；原BoxCast盒线只表示近处即时安全检测。
            DrawDebugCircle(origin, navigation.Prediction.LookAheadDistance, new Color(0f, 0.75f, 1f, 0.7f));
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
        foreach (NavigationAvoidance.DebugHit hit in navigation.Local.DebugHits)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, hit.HitPoint);
            Gizmos.DrawWireCube(hit.HitPoint, avoidanceBounds.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(hit.HitPoint, hit.AvoidanceVector * 2f);
        }

        Gizmos.color = new Color(1f, 0.6f, 0f);
        Vector3 previous = origin;
        foreach (Vector3 point in navigation.Path.Points)
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
