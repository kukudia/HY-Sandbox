using UnityEngine;

[RequireComponent(typeof(ControlUnit))]
public class EnemyController : MonoBehaviour
{
    public float detectionRange = 80f;
    public float desiredDistance = 18f;
    public float updateInterval = 0.25f;
    public float faceTargetTorque = 8f;
    public float retreatDistance = 10f;
    public float obstacleAvoidanceDistance = 12f;
    public float obstacleProbeRadius = 1.5f;
    public float obstacleAvoidanceWeight = 1.4f;
    public float sideProbeAngle = 35f;
    public float targetHoverHeightOffset = 3f;
    public LayerMask obstacleLayers = ~0;

    private ControlUnit unit;
    private Rigidbody rb;
    private ControlUnit currentTarget;
    private float nextUpdateTime;

    private void Awake()
    {
        unit = GetComponent<ControlUnit>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode) return;
        if (unit == null || !unit.HasValidCockpit || unit.faction != UnitFaction.Enemy) return;

        if (Time.time >= nextUpdateTime)
        {
            currentTarget = FindNearestPlayer();
            unit.SetTarget(currentTarget != null ? currentTarget.transform : null);
            nextUpdateTime = Time.time + updateInterval;
        }

        ConfigureHoverThrusters();

        if (currentTarget == null)
        {
            unit.SetMovementInput(Vector3.zero);
            return;
        }

        Vector3 toTarget = currentTarget.transform.position - transform.position;
        Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        float distance = flatDirection.magnitude;

        Vector3 desiredMove = Vector3.zero;
        if (distance > detectionRange)
        {
            desiredMove = Vector3.zero;
        }
        else if (distance > desiredDistance)
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
        unit.SetMovementInput(desiredMove);
        FaceTarget(flatDirection);
    }

    private ControlUnit FindNearestPlayer()
    {
        ControlUnit[] units = Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None);
        ControlUnit nearest = null;
        float nearestSqrDistance = detectionRange * detectionRange;

        foreach (ControlUnit candidate in units)
        {
            if (candidate == null || candidate == unit) continue;
            if (!candidate.HasValidCockpit || candidate.faction != UnitFaction.Player) continue;

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }

    private void FaceTarget(Vector3 flatDirection)
    {
        if (rb == null || flatDirection.sqrMagnitude < 0.01f) return;

        Vector3 desiredForward = flatDirection.normalized;
        Vector3 currentForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (currentForward.sqrMagnitude < 0.01f) return;

        float turn = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);
        rb.AddTorque(Vector3.up * turn * faceTargetTorque, ForceMode.Acceleration);
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
            float targetHeight = currentTarget != null
                ? currentTarget.transform.position.y + targetHoverHeightOffset
                : transform.position.y;
            unit.hoverFlightController.targetHoverHeight = Mathf.Max(2f, targetHeight);
        }
    }

    private Vector3 GetStrafeDirection(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude < 0.01f) return Vector3.zero;

        float sign = Mathf.Sin(Time.time * 0.7f + GetInstanceID()) >= 0f ? 1f : -1f;
        return Vector3.Cross(Vector3.up, flatDirection.normalized) * sign * 0.45f;
    }

    private Vector3 ApplyObstacleAvoidance(Vector3 desiredMove, Vector3 targetDirection)
    {
        Vector3 probeDirection = desiredMove.sqrMagnitude > 0.01f ? desiredMove.normalized : targetDirection.normalized;
        if (probeDirection.sqrMagnitude < 0.01f) return desiredMove;

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
        RaycastHit[] hits = Physics.SphereCastAll(origin, obstacleProbeRadius, direction.normalized, obstacleAvoidanceDistance, obstacleLayers);
        if (hits == null || hits.Length == 0) return Vector3.zero;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            ControlUnit hitUnit = hit.collider.GetComponentInParent<ControlUnit>();
            if (hitUnit == unit) continue;

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
