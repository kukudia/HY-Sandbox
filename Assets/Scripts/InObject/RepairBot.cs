using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairBot : MonoBehaviour
{
    private const int MaxNearbyColliders = 32;
    private const int InitialRepairTargetColliderCapacity = 128;
    private const int MaxRepairTargetColliderCapacity = 1024;
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
    [Min(0.1f)] public float dockingApproachHeight = 1f;
    [Min(0.01f)] public float dockingApproachTolerance = 0.2f;
    [Min(0.1f)] public float dockingPositionSpeed = 2f;

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
    private Vector3 currentAvoidanceDirection;
    private Vector3 smoothedDirection;
    private float currentSpeedMultiplier = 1f;
    private TrailRenderer trailRenderer;
    private ControlUnit ownerUnit;
    private readonly List<Durability> targetsInRange = new List<Durability>();
    private readonly HashSet<Durability> uniqueTargetsInRange = new HashSet<Durability>();
    private Collider[] repairTargetColliders = new Collider[InitialRepairTargetColliderCapacity];
    private readonly Collider[] nearbyColliders = new Collider[MaxNearbyColliders];
    private StylizedBeamEffect repairBeamEffect;
    private int blockLayerMask;
    private Transform cachedNavigationTarget;
    private AdvancedAvoidanceResult cachedAvoidanceResult;
    private float nextDirectionUpdateTime;
    private bool isPrecisionDocking;

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
        SetDockedState(transform.parent == home);
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
        if (isPrecisionDocking)
        {
            isPrecisionDocking = false;
            SetDockedState(false);
        }

        if (transform.parent == home)
        {
            LeaveHome();
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

        Vector3 approachPosition = home.TransformPoint(homeOffset + Vector3.up * dockingApproachHeight);
        if (isPrecisionDocking)
        {
            ReturnHome();
            return;
        }

        if ((approachPosition - transform.position).sqrMagnitude
            <= dockingApproachTolerance * dockingApproachTolerance)
        {
            ReturnHome();
            return;
        }

        NavigateToPosition(approachPosition, home);
    }

    private void NavigateToPosition(Vector3 targetPosition, Transform targetReference)
    {
        navigateTarget = targetReference;

        // 计算目标方向
        Vector3 targetDirection = (targetPosition - transform.position).normalized;

        // 避障查询按固定间隔采样，物理帧之间只渐进跟随缓存结果，避免方向高频抖动和重复 Raycast。
        if (targetReference != cachedNavigationTarget || Time.time >= nextDirectionUpdateTime)
        {
            cachedNavigationTarget = targetReference;
            nextDirectionUpdateTime = Time.time + Mathf.Max(0.05f, directionUpdateInterval);
            cachedAvoidanceResult = CalculateAdvancedAvoidance(targetDirection);
        }

        // 目标方向使用当前世界坐标实时计算；避障查询可以降频，但移动中的 home 不能使用旧位置。
        Vector3 finalDirection = BlendDirections(
            targetDirection,
            cachedAvoidanceResult.avoidanceDirection,
            cachedAvoidanceResult.avoidanceStrength);
        if (smoothedDirection.sqrMagnitude < 0.001f)
            smoothedDirection = transform.forward;
        smoothedDirection = Vector3.RotateTowards(
            smoothedDirection,
            finalDirection,
            Mathf.Max(0.1f, directionResponseRate) * Time.fixedDeltaTime,
            0f);

        // 根据障碍物密度调整速度
        currentSpeedMultiplier = Mathf.Lerp(
            currentSpeedMultiplier,
            cachedAvoidanceResult.speedMultiplier,
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
        rb.AddForce(transform.forward * effectiveSpeed, ForceMode.Acceleration);

        // 限制最大速度
        if (rb.linearVelocity.magnitude > effectiveSpeed * 2f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * effectiveSpeed * 2f;
        }

        // 保存当前避障方向用于可视化
        currentAvoidanceDirection = cachedAvoidanceResult.avoidanceDirection;
    }

    // ===== 优化的避障算法核心 =====
    private struct AdvancedAvoidanceResult
    {
        public Vector3 avoidanceDirection;
        public float avoidanceStrength;
        public float speedMultiplier;
        public int obstacleCount;
    }

    private AdvancedAvoidanceResult CalculateAdvancedAvoidance(Vector3 targetDirection)
    {
        debugAvoidanceInfo.Clear();

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

            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, predictiveAvoidanceRange, obstacleMask))
            {
                // 过滤地面
                if (Vector3.Dot(hit.normal, Vector3.up) >= groundNormalThreshold)
                    continue;

                if (hit.point.y - transform.position.y < groundHeightThreshold)
                    continue;

                ProcessRaycastHit(hit, ref emergencyAvoidance, ref primaryAvoidance, ref predictiveAvoidance,
                    ref emergencyCount, ref primaryCount, ref predictiveCount);
            }
        }

        // 2. 球形检测法（补充检测盲区）
        int nearbyColliderCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            primaryAvoidanceRange,
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
                ref emergencyCount, ref primaryCount);
        }

        // 3. 前方预测检测（基于当前速度）
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            Vector3 predictedPosition = transform.position + rb.linearVelocity.normalized * predictiveAvoidanceRange;
            Vector3 predictionDirection = (predictedPosition - transform.position).normalized;

            if (Physics.Raycast(transform.position, predictionDirection, out RaycastHit predictHit,
                predictiveAvoidanceRange, obstacleMask))
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
        ref int emergencyCount, ref int primaryCount, ref int predictiveCount)
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

        if (distance < emergencyAvoidanceRange)
        {
            float force = 1f - (distance / emergencyAvoidanceRange);
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
        else if (distance < primaryAvoidanceRange)
        {
            float force = 1f - (distance / primaryAvoidanceRange);
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
            float force = 1f - (distance / predictiveAvoidanceRange);
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
        ref int emergencyCount, ref int primaryCount)
    {
        Vector3 avoidDir = (transform.position - obstaclePos).normalized;

        if (distance < emergencyAvoidanceRange)
        {
            float force = 1f - (distance / emergencyAvoidanceRange);
            emergency += avoidDir * force;
            emergencyCount++;
        }
        else if (distance < primaryAvoidanceRange)
        {
            float force = 1f - (distance / primaryAvoidanceRange);
            primary += avoidDir * force;
            primaryCount++;
        }
    }

    private Vector3 BlendDirections(Vector3 targetDir, Vector3 avoidanceDir, float avoidanceStrength)
    {
        if (avoidanceStrength < 0.01f)
            return targetDir;

        // 动态调整权重
        float targetWeight = targetDirectionWeight * (1f - avoidanceStrength * 0.5f);
        float avoidanceWeight = Mathf.Lerp(1f, 10f, avoidanceStrength);

        Vector3 blended = (targetDir * targetWeight + avoidanceDir * avoidanceWeight).normalized;

        // 确保不会完全偏离目标
        float maxDeviation = Mathf.Lerp(90f, 120f, avoidanceStrength);
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
            SetDockedState(true);
            return;
        }

        if (!isPrecisionDocking)
        {
            isPrecisionDocking = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            SetDockedState(true);
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
        SetDockedState(true);
        isPrecisionDocking = false;
    }

    private void LeaveHome()
    {
        isPrecisionDocking = false;
        transform.parent = outside;
        SetDockedState(false);
        smoothedDirection = transform.forward;
    }

    private void SetDockedState(bool docked)
    {
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
            Gizmos.DrawSphere(transform.position, emergencyAvoidanceRange);

            // 主要区域（黄色）
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, primaryAvoidanceRange);

            // 预测区域（绿色）
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawSphere(transform.position, predictiveAvoidanceRange);
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
            $"Speed: {currentSpeedMultiplier:F2}x\nObstacles: {debugAvoidanceInfo.Count}"
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
