// 职责：为敌机与自主友舰选择敌对目标并生成驾驶意图；阵营和玩家驾驶权分别判断。
// 入口：FixedUpdate()；按固定周期更新目标和避障意图，逐物理步平滑输入。
using UnityEngine;

[RequireComponent(typeof(ControlUnit))]
public class EnemyController : MonoBehaviour
{
    // Produces smoothed movement intent; ControlUnit and thrusters remain the only physics writers.
    private const int MaxObstacleHits = 16;
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
    private readonly RaycastHit[] obstacleHits = new RaycastHit[MaxObstacleHits];
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

        if (Time.time >= nextUpdateTime)
        {
            currentTarget = FindNearestOpponent();
            unit.SetTarget(currentTarget != null ? currentTarget.transform : null);
            nextUpdateTime = Time.time + updateInterval;
        }

        ConfigureHoverThrusters();
        if (currentTarget == null)
        {
            Vector3 toGuard = guardAnchor != null ? guardAnchor.position - transform.position : Vector3.zero;
            toGuard.y = 0f;
            desiredMovementInput = toGuard.sqrMagnitude > 100f ? ApplyObstacleAvoidance(toGuard.normalized, toGuard) : Vector3.zero;
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
        if (distance <= detectionRange)
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
    }

    public void SetPriorityTarget(ControlUnit targetUnit)
    {
        priorityTarget = targetUnit;
        nextUpdateTime = 0f;
    }

    private ControlUnit FindNearestOpponent()
    {
        if (IsValidOpponent(priorityTarget) && (priorityTarget.transform.position - transform.position).sqrMagnitude < detectionRange * detectionRange)
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

        float sign = Mathf.Sin(Time.time * 0.7f + GetInstanceID()) >= 0f ? 1f : -1f;
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
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            obstacleProbeRadius,
            direction.normalized,
            obstacleHits,
            obstacleAvoidanceDistance,
            obstacleLayers);
        if (hitCount == 0)
        {
            return Vector3.zero;
        }

        System.Array.Sort(obstacleHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = obstacleHits[i];
            ControlUnit hitUnit = hit.collider.GetComponentInParent<ControlUnit>();
            if (hitUnit == unit)
            {
                continue;
            }

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

    private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();
        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }
}
