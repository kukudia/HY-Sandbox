using UnityEngine;

[RequireComponent (typeof(Power))]
public class TurretWeapon : MonoBehaviour
{
    private const int MaxRaycastHits = 16;

    public UnitFaction targetFaction = UnitFaction.Enemy;
    public Transform horizontalAxis;
    public Transform verticalAxis;
    public Transform aimPivot;
    public Transform muzzle;
    public float range = 45f;
    public float damage = 12f;
    public float fireInterval = 0.45f;
    public float turnSpeed = 240f;
    public float maxFireAngle = 8f;
    public float targetRefreshInterval = 0.2f;
    public LayerMask hitLayers = ~0;

    private ControlUnit owner;
    private ControlUnit target;
    private Durability targetDurability;
    private StylizedBeamEffect fireBeam;
    private float nextSearchTime;
    private float nextFireTime;
    private float hideLineTime;
    private readonly RaycastHit[] raycastHits = new RaycastHit[MaxRaycastHits];
    private readonly System.Collections.Generic.Dictionary<ControlUnit, Durability[]> durabilityCache = new System.Collections.Generic.Dictionary<ControlUnit, Durability[]>();

    private void Awake()
    {
        Transform model = transform.Find("Model");
        if (aimPivot == null)
        {
            aimPivot = model != null ? model : transform;
        }

        if (horizontalAxis == null)
        {
            horizontalAxis = aimPivot != null ? aimPivot : model != null ? model : transform;
        }

        if (verticalAxis == null)
        {
            verticalAxis = horizontalAxis;
        }
    }

    private void Start()
    {
        owner = GetComponentInParent<ControlUnit>();
        LineRenderer legacyFireLine = GetComponent<LineRenderer>();
        if (legacyFireLine != null)
        {
            legacyFireLine.enabled = false;
        }

        fireBeam = GetComponent<StylizedBeamEffect>();
        if (fireBeam == null)
        {
            fireBeam = gameObject.AddComponent<StylizedBeamEffect>();
        }
        fireBeam.Configure(0.035f, 6f, 7, 0.008f, 2.2f, 18f);
        fireBeam.SetColor(new Color(1f, 0.46f, 0.08f, 1f));
        fireBeam.SetVisible(false);
    }

    private void FixedUpdate()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode) return;

        if (owner == null)
        {
            owner = GetComponentInParent<ControlUnit>();
        }

        if (owner == null || !owner.HasValidCockpit) return;

        if (Time.time >= nextSearchTime)
        {
            target = FindNearestTarget(GetEffectiveTargetFaction(), out targetDurability);
            nextSearchTime = Time.time + targetRefreshInterval;
        }

        if (target != null && targetDurability != null)
        {
            AimAndFire(targetDurability);
        }

        if (fireBeam != null && Time.time < hideLineTime)
        {
            fireBeam.SetIntensity(Mathf.Clamp01((hideLineTime - Time.time) / 0.085f));
        }
        else if (fireBeam != null)
        {
            fireBeam.SetVisible(false);
        }
    }

    private UnitFaction GetEffectiveTargetFaction()
    {
        return targetFaction == owner.faction ? Opposite(owner.faction) : targetFaction;
    }

    private ControlUnit FindNearestTarget(UnitFaction faction, out Durability nearestDurability)
    {
        var units = PlayManager.instance != null ? PlayManager.instance.GetControlUnits() : null;
        ControlUnit nearest = null;
        nearestDurability = null;
        float nearestSqrDistance = range * range;

        if (units == null)
        {
            return null;
        }

        if (ShouldPrioritizeEnemyCockpit())
        {
            foreach (ControlUnit candidate in units)
            {
                if (candidate == null || candidate == owner) continue;
                if (!candidate.HasValidCockpit || candidate.faction != faction) continue;

                Durability cockpitDurability = FindCockpitDurability(candidate);
                if (cockpitDurability == null) continue;

                float sqrDistance = (cockpitDurability.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearest = candidate;
                    nearestDurability = cockpitDurability;
                    nearestSqrDistance = sqrDistance;
                }
            }

            if (nearestDurability != null)
            {
                return nearest;
            }
        }

        foreach (ControlUnit candidate in units)
        {
            if (candidate == null || candidate == owner) continue;
            if (!candidate.HasValidCockpit || candidate.faction != faction) continue;

            Durability candidateDurability = FindNearestDurability(candidate);
            if (candidateDurability == null) continue;

            float sqrDistance = (candidateDurability.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearest = candidate;
                nearestDurability = candidateDurability;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }

    private Durability FindNearestDurability(ControlUnit unit)
    {
        Durability[] durabilities = GetDurabilities(unit);
        Durability nearest = null;
        float nearestSqrDistance = float.MaxValue;

        foreach (Durability durability in durabilities)
        {
            if (durability == null) continue;

            float sqrDistance = (durability.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearest = durability;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }

    private Durability[] GetDurabilities(ControlUnit unit)
    {
        if (unit == null)
        {
            return System.Array.Empty<Durability>();
        }

        if (!durabilityCache.TryGetValue(unit, out Durability[] durabilities) || durabilities == null)
        {
            durabilities = unit.GetComponentsInChildren<Durability>();
            durabilityCache[unit] = durabilities;
        }

        return durabilities;
    }

    private bool ShouldPrioritizeEnemyCockpit()
    {
        return owner != null
            && owner.faction == UnitFaction.Player
            && GetEffectiveTargetFaction() == UnitFaction.Enemy;
    }

    private Durability FindCockpitDurability(ControlUnit unit)
    {
        if (unit == null || unit.cockpit == null)
        {
            return null;
        }

        return unit.cockpit.GetComponentInParent<Durability>();
    }

    private void AimAndFire(Durability aimTarget)
    {
        Vector3 origin = GetMuzzlePosition();
        Vector3 aimPoint = aimTarget.transform.position;
        Vector3 direction = aimPoint - origin;
        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 aimDirection = direction.normalized;
        AimAt(aimDirection);

        Vector3 fireDirection = GetAimForward();
        float angle = Vector3.Angle(fireDirection, aimDirection);
        if (angle <= maxFireAngle && Time.time >= nextFireTime)
        {
            Fire(origin, fireDirection, GetEffectiveTargetFaction());
            nextFireTime = Time.time + fireInterval;
        }
    }

    private void AimAt(Vector3 worldDirection)
    {
        if (horizontalAxis == null) return;

        float maxDegreesDelta = turnSpeed * Time.fixedDeltaTime;
        if (verticalAxis == null || verticalAxis == horizontalAxis)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            horizontalAxis.rotation = Quaternion.RotateTowards(horizontalAxis.rotation, targetRotation, maxDegreesDelta);
            return;
        }

        Vector3 horizontalUp = horizontalAxis.parent != null ? horizontalAxis.parent.up : Vector3.up;
        Vector3 flatDirection = Vector3.ProjectOnPlane(worldDirection, horizontalUp);
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion yawRotation = Quaternion.LookRotation(flatDirection.normalized, horizontalUp);
            horizontalAxis.rotation = Quaternion.RotateTowards(horizontalAxis.rotation, yawRotation, maxDegreesDelta);
        }

        Vector3 localDirection = horizontalAxis.InverseTransformDirection(worldDirection);
        if (localDirection.z <= 0.001f) return;

        Vector3 pitchDirection = new Vector3(0f, localDirection.y, localDirection.z).normalized;
        Quaternion pitchRotation = Quaternion.LookRotation(pitchDirection, Vector3.up);
        verticalAxis.localRotation = Quaternion.RotateTowards(verticalAxis.localRotation, pitchRotation, maxDegreesDelta);
    }

    private void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)
    {
        Vector3 end = origin + direction * range;
        int hitCount = Physics.RaycastNonAlloc(origin, direction, raycastHits, range, hitLayers);
        System.Array.Sort(raycastHits, 0, hitCount, RaycastHitDistanceComparer.Instance);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            ControlUnit hitUnit = hit.collider.GetComponentInParent<ControlUnit>();
            if (hitUnit == owner) continue;

            end = hit.point;
            Durability durability = hit.collider.GetComponentInParent<Durability>();

            if (hitUnit != null && hitUnit.HasValidCockpit && hitUnit.faction == faction && durability != null)
            {
                durability.UpdateDurablility(-damage);
            }

            break;
        }

        fireBeam.SetEndpoints(origin, end);
        fireBeam.SetIntensity(1f);
        fireBeam.SetVisible(true);
        hideLineTime = Time.time + 0.085f;
    }

    private Vector3 GetMuzzlePosition()
    {
        if (muzzle != null)
        {
            return muzzle.position;
        }

        Transform origin = verticalAxis != null ? verticalAxis : horizontalAxis != null ? horizontalAxis : aimPivot != null ? aimPivot : transform;
        return origin.position + GetAimForward() * 0.7f;
    }

    private Vector3 GetAimForward()
    {
        if (muzzle != null)
        {
            return muzzle.forward;
        }

        if (verticalAxis != null)
        {
            return verticalAxis.forward;
        }

        if (horizontalAxis != null)
        {
            return horizontalAxis.forward;
        }

        return aimPivot != null ? aimPivot.forward : transform.forward;
    }

    private static UnitFaction Opposite(UnitFaction faction)
    {
        return faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
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
