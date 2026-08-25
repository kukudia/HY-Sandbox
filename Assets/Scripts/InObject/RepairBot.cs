using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairBot : MonoBehaviour
{
    private const int MaxNearbyColliders = 32;
    private const int InitialRepairTargetColliderCapacity = 128;
    private const int MaxRepairTargetColliderCapacity = 1024;
    private const int MaxHomeGuidanceHits = 16;
    private const float MinimumTargetScanInterval = 0.25f;

    public Transform home;
    public Transform outside;
    public Transform navigateTarget;
    public Vector3 homeOffset = Vector3.zero;

    [Header("修复设置")]
    [Min(0)] public int targetRange = 5;
    public float repairAmount = 10f;
    public float repairCooldown = 1f;
    public float findTargetInterval = 1f;
    public float movementSpeed = 5f;
    public float rotationSpeed = 2f;

    [Header("优化避障设置")]
    public LayerMask obstacleMask;

    [Tooltip("紧急避障距离（立即反应）")]
    public float emergencyAvoidanceRange = 3f;
    [Tooltip("主要避障距离")]
    public float primaryAvoidanceRange = 8f;
    [Tooltip("预测避障距离（提前规划）")]
    public float predictiveAvoidanceRange = 15f;

    [Tooltip("紧急避障力度")]
    public float emergencyAvoidanceForce = 10f;
    [Tooltip("主要避障力度")]
    public float primaryAvoidanceForce = 5f;
    [Tooltip("预测避障力度")]
    public float predictiveAvoidanceForce = 2f;

    [Tooltip("目标方向基础权重")]
    public float targetDirectionWeight = 3f;

    [Tooltip("射线检测数量")]
    public int avoidanceRays = 12;

    [Tooltip("地面法线点积阈值")]
    public float groundNormalThreshold = 0.9f;
    [Tooltip("忽略此高度以下的障碍物")]
    public float groundHeightThreshold = -2f;

    [Tooltip("速度衰减系数（障碍物密集时减速）")]
    public float speedDampingFactor = 0.5f;
    [Tooltip("路径平滑系数（0-1，越大越平滑）")]
    public float pathSmoothness = 0.7f;

    [Header("寻路平滑设置")]
    [Min(0.05f)] public float directionUpdateInterval = 0.25f;
    [Min(0.1f)] public float directionResponseRate = 3f;
    [Min(0.1f)] public float returnAlignmentAngle = 8f;
    [Min(0.1f)] public float returnBrakeDistance = 2f;
    [Min(0.01f)] public float returnPositionTolerance = 0.08f;
    [Min(0.01f)] public float returnStopSpeed = 0.15f;
    [Min(0.1f)] public float returnBrakingAcceleration = 12f;
    [Min(0.1f)] public float dockingApproachHeight = 1f;
    [Min(0.01f)] public float dockingApproachTolerance = 0.2f;
    [Min(0.1f)] public float dockingPositionSpeed = 2f;
    [Range(0f, 90f)] public float returnMaxAvoidanceAngle = 65f;

    [Header("返航 Home 引导")]
    [Range(4, 16)] public int homeGuidanceRays = 8;
    [Min(0.5f)] public float homeGuidanceRange = 8f;
    [Range(0f, 2f)] public float homeGuidanceAlignmentWeight = 0.75f;

    [Header("可视化设置")]
    public bool showTrail = true;
    public float trailDuration = 5f;
    public bool showAvoidanceRays = true;
    public bool showDirectionVectors = true;
    public bool showAvoidanceZones = true;

    [Header("修复效果")]
    public Gradient repairBeamGradient;
    public float beamWidth = 0.2f;

    // 公共状态
    public Durability currentTarget;
    public float lastRepairTime;
    public float lastFindTime;
    public bool isRepairing;

    // 私有变量
    private Rigidbody rb;
    private Rigidbody homeRigidbody;
    private Vector3 currentAvoidanceDirection;
    private Vector3 smoothedDirection;
    private float currentSpeedMultiplier = 1f;
    private TrailRenderer trailRenderer;
    private ControlUnit ownerUnit;
    private readonly List<Durability> targetsInRange = new List<Durability>();
    private readonly HashSet<Durability> uniqueTargetsInRange = new HashSet<Durability>();
    private Collider[] repairTargetColliders = new Collider[InitialRepairTargetColliderCapacity];
    private readonly Collider[] nearbyColliders = new Collider[MaxNearbyColliders];
    private readonly RaycastHit[] homeGuidanceHits = new RaycastHit[MaxHomeGuidanceHits];
    private StylizedBeamEffect repairBeamEffect;
    private int blockLayerMask;
    private Transform cachedNavigationTarget;
    private AdvancedAvoidanceResult cachedAvoidanceResult;
    private float nextDirectionUpdateTime;
    private float cachedAvoidanceRangeScale = 1f;
    private enum NavigationState
    {
        Idle,
        NavigatingToTarget,
        ReturningHome,
        Docking
    }
    private NavigationState currentState;
    private bool navigationStateInitialized;

    // 调试信息
    private List<AvoidanceDebugInfo> debugAvoidanceInfo = new List<AvoidanceDebugInfo>();

    // 避障调试信息结构
    private struct AvoidanceDebugInfo
    {
        public Vector3 hitPoint;
        public Vector3 avoidanceVector;
        public float distance;
        public AvoidanceLevel level;
    }

    private enum AvoidanceLevel
    {
        Emergency,
        Primary,
        Predictive
    }

    private void Start()
    {
        home = transform.parent;
        homeOffset = transform.localPosition;
        blockLayerMask = LayerMask.GetMask("Block");
        InitializeComponents();
        InitializeTargetsInRange();
        InitializeTrail();
        SetNavigationState(transform.parent == home
            ? NavigationState.Idle
            : NavigationState.ReturningHome);
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.linearDamping = 2f;
        rb.angularDamping = 3f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

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
        repairBeamEffect.Configure(beamWidth * 0.28f, 5.5f, 14, beamWidth * 0.16f, 3.2f, 9f);
        repairBeamEffect.SetVisible(false);

        EnsureRepairBeamGradient();
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

    private void InitializeTrail()
    {
        if (showTrail)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
                trailRenderer.startColor = new Color(0.2f, 0.8f, 1f, 0.8f);
                trailRenderer.endColor = new Color(0.2f, 0.8f, 1f, 0.1f);
                trailRenderer.startWidth = 0.15f;
                trailRenderer.endWidth = 0.05f;
                trailRenderer.time = trailDuration;
                trailRenderer.enabled = true;
            }
        }
    }

    private void FixedUpdate()
    {
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
            CheckAndRepair();
        }
    }

    private void NavigateToTarget(Transform target)
    {
        if (transform.parent == home)
        {
            LeaveHome();
        }
        else
        {
            SetNavigationState(NavigationState.NavigatingToTarget);
        }

        NavigateToPosition(target.position, target);
    }

    private void NavigateHomeSmoothly()
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
        if ((approachPosition - transform.position).sqrMagnitude
            <= dockingCaptureDistance * dockingCaptureDistance)
        {
            ReturnHome();
            return;
        }

        float avoidanceRangeScale = Mathf.InverseLerp(
            dockingCaptureDistance,
            Mathf.Max(dockingCaptureDistance + 0.01f, predictiveAvoidanceRange),
            Vector3.Distance(transform.position, approachPosition));
        NavigateToPosition(
            approachPosition,
            home,
            avoidanceRangeScale,
            returnMaxAvoidanceAngle);
    }

    private void NavigateToPosition(
        Vector3 targetPosition,
        Transform targetReference,
        float avoidanceRangeScale = 1f,
        float maxAvoidanceAngle = 120f)
    {
        navigateTarget = targetReference;
        avoidanceRangeScale = Mathf.Clamp01(avoidanceRangeScale);

        // 计算目标方向
        Vector3 targetDirection = (targetPosition - transform.position).normalized;

        // 避障查询按固定间隔采样，物理帧之间只渐进跟随缓存结果，避免方向高频抖动和重复 Raycast。
        if (targetReference != cachedNavigationTarget
            || Time.time >= nextDirectionUpdateTime
            || Mathf.Abs(avoidanceRangeScale - cachedAvoidanceRangeScale) >= 0.15f)
        {
            cachedNavigationTarget = targetReference;
            cachedAvoidanceRangeScale = avoidanceRangeScale;
            nextDirectionUpdateTime = Time.time + Mathf.Max(0.05f, directionUpdateInterval);
            cachedAvoidanceResult = currentState == NavigationState.ReturningHome
                ? CalculateHomeNavigationGuidance(targetDirection, avoidanceRangeScale)
                : CalculateAdvancedAvoidance(targetDirection, avoidanceRangeScale);
        }

        // 目标方向使用当前世界坐标实时计算；避障查询可以降频，但移动中的 home 不能使用旧位置。
        Vector3 finalDirection = BlendDirections(
            targetDirection,
            cachedAvoidanceResult.avoidanceDirection,
            cachedAvoidanceResult.avoidanceStrength * avoidanceRangeScale,
            maxAvoidanceAngle);
        if (smoothedDirection.sqrMagnitude < 0.001f)
            smoothedDirection = transform.forward;
        smoothedDirection = Vector3.RotateTowards(
            smoothedDirection,
            finalDirection,
            Mathf.Max(0.1f, directionResponseRate) * Time.fixedDeltaTime,
            0f);

        // 根据障碍物密度调整速度
        float targetSpeedMultiplier = Mathf.Lerp(
            1f,
            cachedAvoidanceResult.speedMultiplier,
            avoidanceRangeScale);
        currentSpeedMultiplier = Mathf.Lerp(
            currentSpeedMultiplier,
            targetSpeedMultiplier,
            Time.fixedDeltaTime * 3f
        );

        // 旋转朝向移动方向
        if (smoothedDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(smoothedDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }

        // 应用移动力
        float effectiveSpeed = movementSpeed * currentSpeedMultiplier;
        if (currentState == NavigationState.ReturningHome)
        {
            ApplyReturnHomeMovement(targetPosition, targetDirection, effectiveSpeed);
        }
        else
        {
            rb.AddForce(transform.forward * effectiveSpeed, ForceMode.Acceleration);

            // 限制最大速度
            if (rb.linearVelocity.magnitude > effectiveSpeed * 2f)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * effectiveSpeed * 2f;
            }
        }

        // 保存当前避障方向用于可视化
        currentAvoidanceDirection = Vector3.Lerp(
            Vector3.zero,
            cachedAvoidanceResult.avoidanceDirection,
            avoidanceRangeScale);
    }

    private void ApplyReturnHomeMovement(
        Vector3 targetPosition,
        Vector3 targetDirection,
        float effectiveSpeed)
    {
        float remainingDistance = Mathf.Max(
            0f,
            Vector3.Distance(transform.position, targetPosition) - dockingApproachTolerance);
        float brakingAcceleration = Mathf.Max(0.1f, returnBrakingAcceleration);
        float maxReturnSpeed = Mathf.Max(returnStopSpeed, effectiveSpeed * 2f);
        float stoppingLimitedSpeed = Mathf.Sqrt(2f * brakingAcceleration * remainingDistance);
        float distanceLimitedSpeed = Mathf.Lerp(
            returnStopSpeed,
            maxReturnSpeed,
            Mathf.Clamp01(remainingDistance / Mathf.Max(0.1f, returnBrakeDistance)));
        float facingAlignment = Mathf.Clamp01(Vector3.Dot(transform.forward, targetDirection));
        float desiredRelativeSpeed = Mathf.Min(stoppingLimitedSpeed, distanceLimitedSpeed, maxReturnSpeed);
        desiredRelativeSpeed = Mathf.Max(
            returnStopSpeed,
            desiredRelativeSpeed * Mathf.Lerp(0.35f, 1f, facingAlignment));

        Vector3 homeVelocity = GetHomePointVelocity(targetPosition);
        Vector3 desiredWorldVelocity = homeVelocity + smoothedDirection.normalized * desiredRelativeSpeed;
        Vector3 velocityDelta = desiredWorldVelocity - rb.linearVelocity;
        Vector3 acceleration = Vector3.ClampMagnitude(
            velocityDelta / Mathf.Max(Time.fixedDeltaTime, 0.001f),
            brakingAcceleration);
        rb.AddForce(acceleration, ForceMode.Acceleration);
    }

    private Vector3 GetRelativeHomeVelocity(Vector3 worldPosition)
    {
        return rb.linearVelocity - GetHomePointVelocity(worldPosition);
    }

    private Vector3 GetHomePointVelocity(Vector3 worldPosition)
    {
        return homeRigidbody != null
            ? homeRigidbody.GetPointVelocity(worldPosition)
            : Vector3.zero;
    }

    // ===== 优化的避障算法核心 =====
    private struct AdvancedAvoidanceResult
    {
        public Vector3 avoidanceDirection;
        public float avoidanceStrength;
        public float speedMultiplier;
        public int obstacleCount;
    }

    private AdvancedAvoidanceResult CalculateHomeNavigationGuidance(
        Vector3 targetDirection,
        float rangeScale)
    {
        debugAvoidanceInfo.Clear();
        if (home == null || rangeScale <= 0.01f)
        {
            return new AdvancedAvoidanceResult { speedMultiplier = 1f };
        }

        Vector3 homeOrigin = home.TransformPoint(homeOffset);
        Vector3 homeToBot = transform.position - homeOrigin;
        Vector3 up = home.up.sqrMagnitude > 0.001f ? home.up.normalized : Vector3.up;
        Vector3 preferredApproach = Vector3.ProjectOnPlane(homeToBot, up);
        if (preferredApproach.sqrMagnitude < 0.001f)
        {
            preferredApproach = homeToBot.sqrMagnitude > 0.001f
                ? homeToBot.normalized
                : targetDirection.sqrMagnitude > 0.001f ? -targetDirection : home.forward;
        }

        preferredApproach.Normalize();
        float rayRange = Mathf.Max(0.5f, homeGuidanceRange * rangeScale);
        int rayCount = Mathf.Clamp(homeGuidanceRays, 4, MaxHomeGuidanceHits);
        float bestScore = float.NegativeInfinity;
        Vector3 bestApproach = preferredApproach;
        float bestClearFraction = 0f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = 360f * i / rayCount;
            Vector3 candidateApproach = Quaternion.AngleAxis(angle, up) * preferredApproach;
            float clearFraction = GetHomeGuidanceClearFraction(
                homeOrigin,
                candidateApproach,
                rayRange);
            float alignment = Mathf.Clamp01((Vector3.Dot(candidateApproach, preferredApproach) + 1f) * 0.5f);
            float score = clearFraction + alignment * homeGuidanceAlignmentWeight;

            if (score > bestScore)
            {
                bestScore = score;
                bestApproach = candidateApproach;
                bestClearFraction = clearFraction;
            }
        }

        // bestApproach 指向从 Home 接近 RepairBot 的安全方向，RepairBot 应沿反方向回家。
        Vector3 guidedReturnDirection = -bestApproach;
        float guidanceStrength = Mathf.Clamp01(
            (1f - Vector3.Dot(targetDirection, guidedReturnDirection)) * 0.5f);

        return new AdvancedAvoidanceResult
        {
            avoidanceDirection = guidedReturnDirection,
            avoidanceStrength = guidanceStrength,
            speedMultiplier = Mathf.Lerp(0.45f, 1f, bestClearFraction),
            obstacleCount = bestClearFraction < 0.99f ? 1 : 0
        };
    }

    private float GetHomeGuidanceClearFraction(
        Vector3 origin,
        Vector3 direction,
        float range)
    {
        int hitCount = Physics.RaycastNonAlloc(
            origin + direction * 0.05f,
            direction,
            homeGuidanceHits,
            range,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
        float nearestDistance = range;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = homeGuidanceHits[i];
            if (IsIgnoredHomeGuidanceCollider(hit.collider))
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, hit.distance);
        }

        return Mathf.Clamp01(nearestDistance / Mathf.Max(range, 0.01f));
    }

    private bool IsIgnoredHomeGuidanceCollider(Collider collider)
    {
        if (collider == null || collider.transform == transform || collider.transform.IsChildOf(transform))
        {
            return true;
        }

        if (home == null)
        {
            return false;
        }

        Transform colliderTransform = collider.transform;
        return colliderTransform == home
            || colliderTransform.IsChildOf(home)
            || home.IsChildOf(colliderTransform);
    }

    private AdvancedAvoidanceResult CalculateAdvancedAvoidance(
        Vector3 targetDirection,
        float rangeScale)
    {
        debugAvoidanceInfo.Clear();

        rangeScale = Mathf.Clamp01(rangeScale);
        if (rangeScale <= 0.01f)
        {
            return new AdvancedAvoidanceResult { speedMultiplier = 1f };
        }

        float emergencyRange = emergencyAvoidanceRange * rangeScale;
        float primaryRange = primaryAvoidanceRange * rangeScale;
        float predictiveRange = predictiveAvoidanceRange * rangeScale;

        Vector3 emergencyAvoidance = Vector3.zero;
        Vector3 primaryAvoidance = Vector3.zero;
        Vector3 predictiveAvoidance = Vector3.zero;

        int emergencyCount = 0;
        int primaryCount = 0;
        int predictiveCount = 0;

        // 1. 射线检测法（精确检测）
        float angleStep = 360f / avoidanceRays;
        for (int i = 0; i < avoidanceRays; i++)
        {
            Quaternion rotation = Quaternion.AngleAxis(angleStep * i, Vector3.up);
            Vector3 direction = rotation * Vector3.forward;

            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, predictiveRange, obstacleMask))
            {
                // 过滤地面
                if (Vector3.Dot(hit.normal, Vector3.up) >= groundNormalThreshold)
                    continue;

                if (hit.point.y - transform.position.y < groundHeightThreshold)
                    continue;

                ProcessRaycastHit(hit, ref emergencyAvoidance, ref primaryAvoidance, ref predictiveAvoidance,
                    ref emergencyCount, ref primaryCount, ref predictiveCount,
                    emergencyRange, primaryRange, predictiveRange);
            }
        }

        // 2. 球形检测法（补充检测盲区）
        int nearbyColliderCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            primaryRange,
            nearbyColliders,
            obstacleMask
        );

        for (int i = 0; i < nearbyColliderCount; i++)
        {
            Collider collider = nearbyColliders[i];
            if (collider.transform == transform ||
                (currentTarget != null && collider.transform == currentTarget.transform))
                continue;

            Vector3 obstaclePos = collider.ClosestPoint(transform.position);
            float distance = Vector3.Distance(transform.position, obstaclePos);

            // 过滤地面
            if (obstaclePos.y - transform.position.y < groundHeightThreshold)
                continue;

            ProcessSphereCollider(obstaclePos, distance, ref emergencyAvoidance, ref primaryAvoidance,
                ref emergencyCount, ref primaryCount, emergencyRange, primaryRange);
        }

        // 3. 前方预测检测（基于当前速度）
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            Vector3 predictedPosition = transform.position + rb.linearVelocity.normalized * predictiveRange;
            Vector3 predictionDirection = (predictedPosition - transform.position).normalized;

            if (Physics.Raycast(transform.position, predictionDirection, out RaycastHit predictHit,
                predictiveRange, obstacleMask))
            {
                if (Vector3.Dot(predictHit.normal, Vector3.up) < groundNormalThreshold &&
                    predictHit.point.y - transform.position.y >= groundHeightThreshold)
                {
                    Vector3 avoidDir = Vector3.Cross(predictHit.normal, Vector3.up).normalized;
                    if (Vector3.Dot(avoidDir, targetDirection) < 0)
                        avoidDir = -avoidDir;

                    predictiveAvoidance += avoidDir;
                    predictiveCount++;
                }
            }
        }

        // 4. 综合计算避障向量
        Vector3 totalAvoidance = Vector3.zero;
        float totalWeight = 0f;

        if (emergencyCount > 0)
        {
            totalAvoidance += (emergencyAvoidance / emergencyCount) * emergencyAvoidanceForce;
            totalWeight += emergencyAvoidanceForce;
        }

        if (primaryCount > 0)
        {
            totalAvoidance += (primaryAvoidance / primaryCount) * primaryAvoidanceForce;
            totalWeight += primaryAvoidanceForce;
        }

        if (predictiveCount > 0)
        {
            totalAvoidance += (predictiveAvoidance / predictiveCount) * predictiveAvoidanceForce;
            totalWeight += predictiveAvoidanceForce;
        }

        // 5. 计算速度衰减
        int totalObstacles = emergencyCount + primaryCount + predictiveCount;
        float speedMultiplier = 1f;

        if (emergencyCount > 0)
            speedMultiplier = speedDampingFactor * 0.3f; // 紧急情况大幅减速
        else if (primaryCount > 0)
            speedMultiplier = Mathf.Lerp(1f, speedDampingFactor, primaryCount / 5f);
        else if (predictiveCount > 0)
            speedMultiplier = Mathf.Lerp(1f, speedDampingFactor * 1.5f, predictiveCount / 8f);

        return new AdvancedAvoidanceResult
        {
            avoidanceDirection = totalAvoidance.normalized,
            avoidanceStrength = Mathf.Clamp01(totalWeight / 10f),
            speedMultiplier = speedMultiplier,
            obstacleCount = totalObstacles
        };
    }

    private void ProcessRaycastHit(RaycastHit hit,
        ref Vector3 emergency, ref Vector3 primary, ref Vector3 predictive,
        ref int emergencyCount, ref int primaryCount, ref int predictiveCount,
        float emergencyRange, float primaryRange, float predictiveRange)
    {
        float distance = hit.distance;
        Vector3 avoidDir = (transform.position - hit.point).normalized;

        // 倾向于绕向目标方向的一侧
        if (navigateTarget != null)
        {
            Vector3 toTarget = (navigateTarget.position - transform.position).normalized;
            Vector3 sideDir = Vector3.Cross(avoidDir, Vector3.up);

            if (Vector3.Dot(sideDir, toTarget) < 0)
                avoidDir = Quaternion.Euler(0, 30, 0) * avoidDir;
            else
                avoidDir = Quaternion.Euler(0, -30, 0) * avoidDir;
        }

        if (distance < emergencyRange)
        {
            float force = 1f - (distance / emergencyRange);
            emergency += avoidDir * force;
            emergencyCount++;

            debugAvoidanceInfo.Add(new AvoidanceDebugInfo
            {
                hitPoint = hit.point,
                avoidanceVector = avoidDir * force,
                distance = distance,
                level = AvoidanceLevel.Emergency
            });
        }
        else if (distance < primaryRange)
        {
            float force = 1f - (distance / primaryRange);
            primary += avoidDir * force;
            primaryCount++;

            debugAvoidanceInfo.Add(new AvoidanceDebugInfo
            {
                hitPoint = hit.point,
                avoidanceVector = avoidDir * force,
                distance = distance,
                level = AvoidanceLevel.Primary
            });
        }
        else
        {
            float force = 1f - (distance / predictiveRange);
            predictive += avoidDir * force;
            predictiveCount++;

            debugAvoidanceInfo.Add(new AvoidanceDebugInfo
            {
                hitPoint = hit.point,
                avoidanceVector = avoidDir * force,
                distance = distance,
                level = AvoidanceLevel.Predictive
            });
        }
    }

    private void ProcessSphereCollider(Vector3 obstaclePos, float distance,
        ref Vector3 emergency, ref Vector3 primary,
        ref int emergencyCount, ref int primaryCount,
        float emergencyRange, float primaryRange)
    {
        Vector3 avoidDir = (transform.position - obstaclePos).normalized;

        if (distance < emergencyRange)
        {
            float force = 1f - (distance / emergencyRange);
            emergency += avoidDir * force;
            emergencyCount++;
        }
        else if (distance < primaryRange)
        {
            float force = 1f - (distance / primaryRange);
            primary += avoidDir * force;
            primaryCount++;
        }
    }

    private Vector3 BlendDirections(
        Vector3 targetDir,
        Vector3 avoidanceDir,
        float avoidanceStrength,
        float maxAvoidanceAngle)
    {
        if (avoidanceStrength < 0.01f)
            return targetDir;

        // 动态调整权重
        float targetWeight = targetDirectionWeight * (1f - avoidanceStrength * 0.5f);
        float avoidanceWeight = Mathf.Lerp(1f, 10f, avoidanceStrength);

        Vector3 blended = (targetDir * targetWeight + avoidanceDir * avoidanceWeight).normalized;

        // 确保不会完全偏离目标
        float maxDeviation = Mathf.Min(
            Mathf.Lerp(90f, 120f, avoidanceStrength),
            Mathf.Clamp(maxAvoidanceAngle, 0f, 180f));
        float angle = Vector3.Angle(targetDir, blended);

        if (angle > maxDeviation)
        {
            blended = Vector3.RotateTowards(targetDir, blended, maxDeviation * Mathf.Deg2Rad, 0f);
        }

        return blended.normalized;
    }

    // ===== 其他方法（保持原有逻辑）=====
    private void ReturnHome()
    {
        if (home == null || rb == null)
            return;

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
            Mathf.Max(0.1f, dockingPositionSpeed) * Time.fixedDeltaTime);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            dockRotation,
            Mathf.Max(1f, rotationSpeed * 90f) * Time.fixedDeltaTime);

        if ((dockPosition - transform.position).sqrMagnitude > returnPositionTolerance * returnPositionTolerance
            || Quaternion.Angle(transform.rotation, dockRotation) > returnAlignmentAngle)
        {
            return;
        }

        transform.SetParent(home, false);
        transform.localPosition = homeOffset;
        transform.localRotation = Quaternion.identity;
        cachedNavigationTarget = null;
        SetNavigationState(NavigationState.Idle);
    }

    private void LeaveHome()
    {
        transform.parent = outside;
        SetNavigationState(NavigationState.NavigatingToTarget);
        smoothedDirection = transform.forward;
    }

    private void SetNavigationState(NavigationState nextState)
    {
        if (navigationStateInitialized && currentState == nextState)
            return;

        currentState = nextState;
        navigationStateInitialized = true;
        bool docked = nextState == NavigationState.Idle || nextState == NavigationState.Docking;
        if (rb != null)
        {
            rb.isKinematic = docked;
            rb.detectCollisions = !docked;
        }

        if (trailRenderer != null)
        {
            trailRenderer.enabled = !docked;
            if (docked)
            {
                trailRenderer.Clear();
            }
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
            repairBeamEffect.SetEndpoints(transform.position, currentTarget.transform.position);

            float durabilityRatio = Mathf.Clamp01(currentTarget.currentDurability / currentTarget.maxDurability);
            Color beamColor = repairBeamGradient.Evaluate(durabilityRatio);

            repairBeamEffect.SetColor(Color.Lerp(beamColor, Color.white, 0.18f));
            repairBeamEffect.SetIntensity(0.82f + Mathf.Sin(Time.unscaledTime * 11f) * 0.18f);
        }
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

    // ===== 增强版可视化 =====
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 1. 避障区域可视化
        if (showAvoidanceZones)
        {
            // 紧急区域（红色）
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, emergencyAvoidanceRange);

            // 主要区域（黄色）
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, primaryAvoidanceRange);

            // 预测区域（绿色）
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, predictiveAvoidanceRange);
        }

        // 2. 修复目标范围
        if (home != null)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireSphere(home.position, targetRange);
        }

        // 3. 避障信息可视化
        if (showAvoidanceRays && debugAvoidanceInfo.Count > 0)
        {
            foreach (var info in debugAvoidanceInfo)
            {
                // 根据避障等级设置颜色
                switch (info.level)
                {
                    case AvoidanceLevel.Emergency:
                        Gizmos.color = Color.red;
                        break;
                    case AvoidanceLevel.Primary:
                        Gizmos.color = Color.yellow;
                        break;
                    case AvoidanceLevel.Predictive:
                        Gizmos.color = Color.green;
                        break;
                }

                // 绘制检测射线
                Gizmos.DrawLine(transform.position, info.hitPoint);

                // 绘制避障向量
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(info.hitPoint, info.avoidanceVector * 3f);

                // 绘制命中点
                Gizmos.DrawSphere(info.hitPoint, 0.3f);
            }
        }

        // 4. 方向向量可视化
        if (showDirectionVectors && navigateTarget != null)
        {
            Vector3 targetDir = (navigateTarget.position - transform.position).normalized;

            // 目标方向（青色）
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, targetDir * 10f);

            // 避障方向（红色）
            if (currentAvoidanceDirection.magnitude > 0.1f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, currentAvoidanceDirection * 7f);
            }

            // 平滑后的实际方向（白色，粗线）
            Gizmos.color = Color.white;
            Gizmos.DrawRay(transform.position, smoothedDirection * 8f);
            Gizmos.DrawSphere(transform.position + smoothedDirection * 8f, 0.4f);

            // 当前速度方向（黄色）
            if (rb != null && rb.linearVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.yellow;
                float velocityLength = Mathf.Min(rb.linearVelocity.magnitude, 10f);
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * velocityLength);
            }
        }

        // 5. 目标连线
        if (currentTarget != null)
        {
            Gizmos.color = isRepairing ? Color.green : new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            Gizmos.DrawWireSphere(currentTarget.transform.position, 1f);
        }
        else if (home != null && navigateTarget == home)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.7f);
            Gizmos.DrawLine(transform.position, home.position);
        }

        // 6. 基地标记
        if (home != null)
        {
            Gizmos.color = new Color(0.3f, 0.3f, 1f, 0.5f);
            Gizmos.DrawSphere(home.position, 1.5f);
        }

        // 7. 速度信息文本（需要Handles）
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 3f,
            $"State: {currentState}\nSpeed: {currentSpeedMultiplier:F2}x\nObstacles: {debugAvoidanceInfo.Count}"
        );
#endif
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        // 编辑器下的静态预览
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, emergencyAvoidanceRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, primaryAvoidanceRange);

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, predictiveAvoidanceRange);

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, targetRange);

        if (navigateTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, navigateTarget.position);
        }
    }
}
