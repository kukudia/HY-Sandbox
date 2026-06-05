using UnityEngine;

public class TurretWeapon : MonoBehaviour
{
    public UnitFaction targetFaction = UnitFaction.Enemy;
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
    private LineRenderer fireLine;
    private float nextSearchTime;
    private float nextFireTime;
    private float hideLineTime;

    private void Awake()
    {
        if (aimPivot == null)
        {
            Transform model = transform.Find("Model");
            aimPivot = model != null ? model : transform;
        }
    }

    private void Start()
    {
        owner = GetComponentInParent<ControlUnit>();
        fireLine = gameObject.AddComponent<LineRenderer>();
        fireLine.positionCount = 2;
        fireLine.enabled = false;
        fireLine.startWidth = 0.04f;
        fireLine.endWidth = 0.01f;
        fireLine.material = new Material(Shader.Find("Unlit/Color"));
        fireLine.startColor = Color.yellow;
        fireLine.endColor = Color.red;
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

        if (fireLine.enabled && Time.time >= hideLineTime)
        {
            fireLine.enabled = false;
        }
    }

    private UnitFaction GetEffectiveTargetFaction()
    {
        return targetFaction == owner.faction ? Opposite(owner.faction) : targetFaction;
    }

    private ControlUnit FindNearestTarget(UnitFaction faction, out Durability nearestDurability)
    {
        ControlUnit[] units = Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None);
        ControlUnit nearest = null;
        nearestDurability = null;
        float nearestSqrDistance = range * range;

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
        Durability[] durabilities = unit.GetComponentsInChildren<Durability>();
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

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        aimPivot.rotation = Quaternion.RotateTowards(aimPivot.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

        float angle = Vector3.Angle(aimPivot.forward, direction.normalized);
        if (angle <= maxFireAngle && Time.time >= nextFireTime)
        {
            Fire(origin, aimPivot.forward, GetEffectiveTargetFaction());
            nextFireTime = Time.time + fireInterval;
        }
    }

    private void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)
    {
        Vector3 end = origin + direction * range;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, hitLayers);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
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

        fireLine.enabled = true;
        fireLine.SetPosition(0, origin);
        fireLine.SetPosition(1, end);
        hideLineTime = Time.time + 0.06f;
    }

    private Vector3 GetMuzzlePosition()
    {
        if (muzzle != null)
        {
            return muzzle.position;
        }

        return aimPivot.position + aimPivot.forward * 0.7f;
    }

    private static UnitFaction Opposite(UnitFaction faction)
    {
        return faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
    }
}
