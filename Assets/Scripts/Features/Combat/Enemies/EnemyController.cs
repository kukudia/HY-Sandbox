// 职责：为敌机与自主友舰选择敌对目标并生成驾驶意图；阵营和玩家驾驶权分别判断。
// 入口：FixedUpdate()；按固定周期更新目标和避障意图，逐物理步平滑输入。
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ControlUnit))]
public class EnemyController : MonoBehaviour
{
    // Produces smoothed movement intent; ControlUnit and thrusters remain the only physics writers.
    private const int MaxObstacleHits = 16;
    [SerializeField, Range(0.1f, 1f), Tooltip("新目标必须比当前目标更近的距离比例，抑制交叉飞行时反复换目标。")]
    private float targetSwitchDistanceRatio = 0.85f;
    public float detectionRange = 150f;
    public float desiredDistance = 18f;
    public float updateInterval = 0.25f;
    [Min(0.05f)]
    public float movementUpdateInterval = 0.5f;
    [Min(0.1f)]
    public float movementResponseRate = 2.5f;
    public float retreatDistance = 10f;
    public float obstacleAvoidanceDistance = 12f;
    public float obstacleProbeRadius = 1.5f;
    public float obstacleAvoidanceWeight = 1.4f;
    public float sideProbeAngle = 35f;
    public float targetHoverHeightOffset = 3f;
    public LayerMask obstacleLayers = ~0;
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
    private void OnEnable()
    {
        // 同批生成单位错开自主扫描；明确任务指派仍会立即清零调度。
        float phase = (GetEntityId().GetHashCode() & 1023) / 1024f;
        nextUpdateTime = Time.time + phase * Mathf.Max(0.01f, updateInterval);
        nextMovementUpdateTime = 0f;
    }

    private void Awake()
    {
        unit = GetComponent<ControlUnit>();
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
                nextMovementUpdateTime = 0f;
            }

            unit.SetTarget(currentTarget != null ? currentTarget.transform : null);
            nextUpdateTime = Time.time + Mathf.Max(0.01f, updateInterval);
        }

        ConfigureHoverThrusters();
        if (currentTarget == null)
        {
            if (Time.time >= nextMovementUpdateTime)
            {
                Vector3 toGuard = guardAnchor != null ? guardAnchor.position - transform.position : Vector3.zero;
                toGuard.y = 0f;
                desiredMovementInput = toGuard.sqrMagnitude > 100f ? ApplyObstacleAvoidance(toGuard.normalized, toGuard) : Vector3.zero;
                nextMovementUpdateTime = Time.time + Mathf.Max(0.05f, movementUpdateInterval);
            }

            unit.SetMovementInput(Vector3.MoveTowards(
                unit.MovementInput,
                desiredMovementInput,
                Mathf.Max(0.1f, movementResponseRate) * Time.fixedDeltaTime));
            return;
        }

        Vector3 toTarget = currentTarget.transform.position - transform.position;
        Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        if (Time.time >= nextMovementUpdateTime)
        {
            desiredMovementInput = CalculateDesiredMovement(flatDirection);
            nextMovementUpdateTime = Time.time + Mathf.Max(0.05f, movementUpdateInterval);
        }

        Vector3 smoothedInput = Vector3.MoveTowards(
            unit.MovementInput,
            desiredMovementInput,
            Mathf.Max(0.1f, movementResponseRate) * Time.fixedDeltaTime);
        unit.SetMovementInput(smoothedInput);
    }

    private Vector3 CalculateDesiredMovement(Vector3 flatDirection)
    {
        float distance = flatDirection.magnitude;
        Vector3 desiredMove = Vector3.zero;
        if (currentTarget == priorityTarget || distance <= detectionRange)
        {
            if (distance > desiredDistance)
            {
                desiredMove = flatDirection.normalized;
            }
            else if (distance < retreatDistance)
            {
                desiredMove = -flatDirection.normalized;
            }
            else
            {
                desiredMove = GetStrafeDirection(flatDirection);
            }

            desiredMove = ApplyObstacleAvoidance(desiredMove, flatDirection);
        }

        return desiredMove;
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

        currentTarget = null;
        desiredMovementInput = Vector3.zero;
        nextUpdateTime = 0f;
        _hasIdleHoverHeight = false;
        nextMovementUpdateTime = 0f;
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
            float targetHeight;
            if (currentTarget != null)
            {
                _hasIdleHoverHeight = false;
                targetHeight = currentTarget.transform.position.y + targetHoverHeightOffset;
            }
            else
            {
                // Hold a fixed altitude when the target is lost. Following the falling root
                // would move the setpoint every tick, preventing the controller from recovering.
                if (!_hasIdleHoverHeight)
                {
                    _idleHoverHeight = unit.hoverFlightController.CurrentHeight;
                    _hasIdleHoverHeight = true;
                }

                targetHeight = _idleHoverHeight;
            }

            unit.hoverFlightController.targetHoverHeight = Mathf.Max(2f, targetHeight);
        }
    }

    private Vector3 GetStrafeDirection(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude < 0.01f)
        {
            return Vector3.zero;
        }

        // EntityId 只作个体相位种子；限定幅度，避免大整数转 float 后吞掉时间增量。
        float phase = (GetEntityId().GetHashCode() & 0xFFFF) * (Mathf.PI * 2f / 65536f);
        float sign = Mathf.Sin(Time.time * 0.7f + phase) >= 0f ? 1f : -1f;
        return Vector3.Cross(Vector3.up, flatDirection.normalized) * sign * 0.45f;
    }

    private Vector3 ApplyObstacleAvoidance(Vector3 desiredMove, Vector3 targetDirection)
    {
        Vector3 probeDirection = desiredMove.sqrMagnitude > 0.01f ? desiredMove.normalized : targetDirection.normalized;
        if (probeDirection.sqrMagnitude < 0.01f)
        {
            return desiredMove;
        }

        Vector3 avoidance = Vector3.zero;
        avoidance += ProbeObstacle(probeDirection, 1f);
        avoidance += ProbeObstacle(Quaternion.AngleAxis(sideProbeAngle, Vector3.up) * probeDirection, 0.65f);
        avoidance += ProbeObstacle(Quaternion.AngleAxis(-sideProbeAngle, Vector3.up) * probeDirection, 0.65f);
        if (avoidance.sqrMagnitude > 0.01f)
        {
            desiredMove += avoidance.normalized * obstacleAvoidanceWeight;
        }

        return Vector3.ClampMagnitude(desiredMove, 1f);
    }

    private Vector3 ProbeObstacle(Vector3 direction, float weight)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        PhysicsScene physics = gameObject.scene.GetPhysicsScene();
        int hitCount = physics.SphereCast(
            origin,
            obstacleProbeRadius,
            direction.normalized,
            obstacleHits,
            obstacleAvoidanceDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);
        // 饱和结果不保证包含最近障碍；只在首次遇到更密集布局时扩容。
        while (hitCount == obstacleHits.Length && obstacleHits.Length < 256)
        {
            System.Array.Resize(ref obstacleHits, obstacleHits.Length * 2);
            hitCount = physics.SphereCast(
                origin,
                obstacleProbeRadius,
                direction.normalized,
                obstacleHits,
                obstacleAvoidanceDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);
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
            if (hitUnit == unit)
            {
                continue;
            }

            Vector3 surfaceNormal = hit.normal;
            if (hit.distance <= 0f)
            {
                // 球扫起点重叠时 PhysX 返回反向射线伪法线；地板支撑不能变成水平墙。
                Vector3 separation = origin - hit.collider.ClosestPoint(origin);
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
            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.01f)
            {
                normal = -direction;
            }

            float distanceFactor = 1f - Mathf.Clamp01(hit.distance / obstacleAvoidanceDistance);
            Vector3 sideStep = Vector3.Cross(Vector3.up, direction).normalized;
            if (Vector3.Dot(sideStep, normal) < 0f)
            {
                sideStep = -sideStep;
            }

            return (normal.normalized + sideStep * 0.6f) * distanceFactor * weight;
        }

        return Vector3.zero;
    }
}
