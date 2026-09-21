using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent (typeof(Power))]
public class TurretWeapon : MonoBehaviour
{
    private const int MaxRaycastHits = 16;

    public UnitFaction targetFaction = UnitFaction.Enemy;
    public Transform horizontalAxis;
    public Transform verticalAxis;
    public Transform aimPivot;
    public Transform muzzle;
    [SerializeField] private AssetParticleEffect _muzzleFlash;
    private Vector3 _beamEnd;
    public float range = 45f;
    public float damage = 12f;
    public float fireInterval = 0.45f;
    [FormerlySerializedAs("turnSpeed")]
    public float horizontalTurnSpeed = 240f;
    public float verticalTurnSpeed = 180f;
    [Range(-89f, 0f)] public float minElevation = -15f;
    [Range(0f, 89f)] public float maxElevation = 65f;
    public float maxFireAngle = 8f;
    public float targetRefreshInterval = 0.2f;
    public LayerMask hitLayers = ~0;

    private ControlUnit owner;
    private Power power;
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
        power = GetComponent<Power>();
        ResolveAimingRig();
    }

    private void OnValidate()
    {
        horizontalTurnSpeed = Mathf.Max(0f, horizontalTurnSpeed);
        verticalTurnSpeed = Mathf.Max(0f, verticalTurnSpeed);
        minElevation = Mathf.Clamp(minElevation, -89f, 0f);
        maxElevation = Mathf.Clamp(maxElevation, 0f, 89f);
    }

    private void ResolveAimingRig()
    {
        Transform model = transform.Find("Model");
        Transform generatedModel = model != null ? model.Find("IndustrialVisual/LOD0") : null;
        if (aimPivot == null)
        {
            aimPivot = generatedModel != null ? generatedModel : model != null ? model : transform;
        }

        if (horizontalAxis == null)
        {
            horizontalAxis = generatedModel != null ? generatedModel.Find("Horizontal") : null;
            if (horizontalAxis == null)
            {
                horizontalAxis = aimPivot != null ? aimPivot : model != null ? model : transform;
            }
        }

        if (verticalAxis == null)
        {
            verticalAxis = horizontalAxis != null ? horizontalAxis.Find("Vertical") : null;
            if (verticalAxis == null)
            {
                verticalAxis = horizontalAxis;
            }
        }

        if (muzzle == null && verticalAxis != null && verticalAxis != horizontalAxis)
        {
            muzzle = verticalAxis.Find("Muzzle");
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
        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            if (fireBeam != null) fireBeam.SetVisible(false);
            return;
        }

        if (power == null)
        {
            power = GetComponent<Power>();
        }

        if (power == null || !power.isWorking)
        {
            target = null;
            targetDurability = null;
            if (fireBeam != null) fireBeam.SetVisible(false);
            return;
        }

        if (owner == null)
        {
            owner = GetComponentInParent<ControlUnit>();
        }

        if (owner == null || !owner.HasValidCockpit)
        {
            if (fireBeam != null) fireBeam.SetVisible(false);
            return;
        }

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
            fireBeam.SetEndpoints(GetMuzzlePosition(), _beamEnd);
            fireBeam.SetIntensity(Mathf.Clamp01((hideLineTime - Time.time) / 0.085f));
        }
        else if (fireBeam != null)
        {
            fireBeam.SetVisible(false);
        }
    }

    private void OnDisable()
    {
        if (fireBeam != null) fireBeam.SetVisible(false);
        if (_muzzleFlash != null) _muzzleFlash.SetIntensity(0f);
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
        Vector3 aimPoint = aimTarget.transform.position;
        Vector3 direction = aimPoint - GetMuzzlePosition();
        if (direction.sqrMagnitude < 0.01f) return;

        AimAt(direction.normalized);

        Vector3 origin = GetMuzzlePosition();
        Vector3 aimDirection = (aimPoint - origin).normalized;
        Vector3 fireDirection = GetAimForward();
        float angle = Vector3.Angle(fireDirection, aimDirection);
        if (angle <= maxFireAngle && Time.time >= nextFireTime)
        {
            Fire(origin, fireDirection, GetEffectiveTargetFaction());
            nextFireTime = Time.time + fireInterval / Mathf.Max(power.efficiency, 0.0001f);
        }
    }

    private void AimAt(Vector3 worldDirection)
    {
        if (horizontalAxis == null) return;

        if (verticalAxis == null || verticalAxis == horizontalAxis)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            horizontalAxis.rotation = Quaternion.RotateTowards(
                horizontalAxis.rotation,
                targetRotation,
                horizontalTurnSpeed * Time.fixedDeltaTime);
            return;
        }

        Vector3 horizontalUp = horizontalAxis.parent != null ? horizontalAxis.parent.up : Vector3.up;
        Vector3 flatDirection = Vector3.ProjectOnPlane(worldDirection, horizontalUp);
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion yawRotation = Quaternion.LookRotation(flatDirection.normalized, horizontalUp);
            horizontalAxis.rotation = Quaternion.RotateTowards(
                horizontalAxis.rotation,
                yawRotation,
                horizontalTurnSpeed * Time.fixedDeltaTime);
        }

        Vector3 localDirection = horizontalAxis.InverseTransformDirection(worldDirection);
        float planarMagnitude = new Vector2(localDirection.x, localDirection.z).magnitude;
        float targetElevation = Mathf.Atan2(localDirection.y, planarMagnitude) * Mathf.Rad2Deg;
        targetElevation = Mathf.Clamp(targetElevation, minElevation, maxElevation);

        // Unity's positive local X rotation points the forward axis downward, so elevation is negated.
        float currentPitch = Mathf.DeltaAngle(0f, verticalAxis.localEulerAngles.x);
        float targetPitch = -targetElevation;
        float nextPitch = Mathf.MoveTowardsAngle(
            currentPitch,
            targetPitch,
            verticalTurnSpeed * Time.fixedDeltaTime);
        verticalAxis.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);
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
            BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Impact, hit.point, Quaternion.LookRotation(hit.normal), 0.6f);
            Durability durability = hit.collider.GetComponentInParent<Durability>();

            if (hitUnit != null && hitUnit.HasValidCockpit && hitUnit.faction == faction && durability != null)
            {
                durability.UpdateDurablility(-damage * power.efficiency);
            }

            break;
        }

        _beamEnd = end;
        if (_muzzleFlash != null) _muzzleFlash.PlayOnce();
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
