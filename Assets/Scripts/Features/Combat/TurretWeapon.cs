// 职责：执行炮塔索敌、转向、俯仰约束及射击反馈。
// 入口：FixedUpdate() 的 AimAndFire()；索敌和命中结算由独立战斗规则执行。
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Power))]
public class TurretWeapon : MonoBehaviour
{
    public UnitFaction targetFaction = UnitFaction.Enemy;
    public Transform horizontalAxis;
    public Transform verticalAxis;
    public Transform aimPivot;
    public Transform muzzle;
    [SerializeField]
    private VfxEffect _muzzleFlash;
    private Vector3 _beamEnd;
    public float range = 45f;
    public float damage = 12f;
    public float fireInterval = 0.45f;
    [SerializeField, Min(0f), Tooltip("Momentum delivered at the hit point in N s. Heavy constructs respond less; off-centre hits generate torque.")]
    private float _impactImpulse = 45f;
    [FormerlySerializedAs("turnSpeed")]
    public float horizontalTurnSpeed = 240f;
    public float verticalTurnSpeed = 180f;
    [Range(-89f, 0f)]
    public float minElevation = -15f;
    [Range(0f, 89f)]
    public float maxElevation = 65f;
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
    private readonly TurretHitscan _hitscan = new TurretHitscan();
    private TurretTargetSelector _targetSelector;
    private void Awake()
    {
        power = GetComponent<Power>();
        _targetSelector = new TurretTargetSelector(transform);
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

        fireBeam.Configure(0.035f);
        fireBeam.SetColor(new Color(1f, 0.46f, 0.08f, 1f));
        fireBeam.SetVisible(false);
    }

    private void FixedUpdate()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            if (fireBeam != null)
            {
                fireBeam.SetVisible(false);
            }

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
            if (fireBeam != null)
            {
                fireBeam.SetVisible(false);
            }

            return;
        }

        if (owner == null)
        {
            owner = GetComponentInParent<ControlUnit>();
        }

        if (owner == null || !owner.HasValidCockpit)
        {
            if (fireBeam != null)
            {
                fireBeam.SetVisible(false);
            }

            return;
        }

        if (Time.time >= nextSearchTime)
        {
            UnitFaction faction = GetEffectiveTargetFaction();
            target = _targetSelector.FindNearestTarget(
                PlayManager.instance.GetControlUnits(),
                owner,
                faction,
                range,
                owner.faction == UnitFaction.Player && faction == UnitFaction.Enemy,
                out targetDurability);
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
        if (fireBeam != null)
        {
            fireBeam.SetVisible(false);
        }

        if (_muzzleFlash != null)
        {
            _muzzleFlash.SetIntensity(0f);
        }
    }

    private UnitFaction GetEffectiveTargetFaction()
    {
        return targetFaction == owner.faction ? Opposite(owner.faction) : targetFaction;
    }

    private void AimAndFire(Durability aimTarget)
    {
        Vector3 aimPoint = aimTarget.transform.position;
        Vector3 direction = aimPoint - GetMuzzlePosition();
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

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
        if (horizontalAxis == null)
        {
            return;
        }

        if (verticalAxis == null || verticalAxis == horizontalAxis)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            horizontalAxis.rotation = Quaternion.RotateTowards(horizontalAxis.rotation, targetRotation, horizontalTurnSpeed * Time.fixedDeltaTime);
            return;
        }

        Vector3 horizontalUp = horizontalAxis.parent != null ? horizontalAxis.parent.up : Vector3.up;
        Vector3 flatDirection = Vector3.ProjectOnPlane(worldDirection, horizontalUp);
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion yawRotation = Quaternion.LookRotation(flatDirection.normalized, horizontalUp);
            horizontalAxis.rotation = Quaternion.RotateTowards(horizontalAxis.rotation, yawRotation, horizontalTurnSpeed * Time.fixedDeltaTime);
        }

        Vector3 localDirection = horizontalAxis.InverseTransformDirection(worldDirection);
        float planarMagnitude = new Vector2(localDirection.x, localDirection.z).magnitude;
        float targetElevation = Mathf.Atan2(localDirection.y, planarMagnitude) * Mathf.Rad2Deg;
        targetElevation = Mathf.Clamp(targetElevation, minElevation, maxElevation);
        // Unity's positive local X rotation points the forward axis downward, so elevation is negated.
        float currentPitch = Mathf.DeltaAngle(0f, verticalAxis.localEulerAngles.x);
        float targetPitch = -targetElevation;
        float nextPitch = Mathf.MoveTowardsAngle(currentPitch, targetPitch, verticalTurnSpeed * Time.fixedDeltaTime);
        verticalAxis.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);
    }

    private void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)
    {
        Vector3 end = _hitscan.Fire(
            origin,
            direction,
            owner,
            faction,
            range,
            hitLayers,
            damage * power.efficiency,
            _impactImpulse * power.efficiency);
        _beamEnd = end;
        if (_muzzleFlash != null)
        {
            _muzzleFlash.PlayOnce();
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
}
