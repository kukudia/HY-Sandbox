// 职责：共享炮塔启动、独立索敌、功率请求和双轴瞄准；武器种类只实现发射与表现。
// 入口：SetStarted() / FixedUpdate()；禁用和退出运行时撤销功率需求。
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Power))]
public abstract class Turret : MonoBehaviour
{
    public UnitFaction targetFaction = UnitFaction.Enemy;
    public Transform horizontalAxis;
    public Transform verticalAxis;
    public Transform aimPivot;
    public Transform muzzle;
    public float range = 45f;
    public float damage = 12f;
    public float fireInterval = 0.45f;
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
    [SerializeField, Tooltip("启动后自动索敌；没有有效目标时不请求功率。")]
    private bool _isStarted = true;
    private ControlUnit _target;
    private Durability _targetDurability;
    private TurretTargetSelector _targetSelector;
    private float _nextSearchTime;
    private float _nextFireTime;
    protected Power Supply
    {
        get; private set;
    }
    protected ControlUnit Owner
    {
        get; private set;
    }
    public bool IsStarted => _isStarted;

    protected virtual void Awake()
    {
        Supply = GetComponent<Power>();
        _targetSelector = new TurretTargetSelector(transform);
        ResolveAimingRig();
    }

    protected virtual void OnValidate()
    {
        horizontalTurnSpeed = Mathf.Max(0f, horizontalTurnSpeed);
        verticalTurnSpeed = Mathf.Max(0f, verticalTurnSpeed);
        minElevation = Mathf.Clamp(minElevation, -89f, 0f);
        maxElevation = Mathf.Clamp(maxElevation, 0f, 89f);
        fireInterval = Mathf.Max(0.01f, fireInterval);
        range = Mathf.Max(0f, range);
    }

    /// <summary>关闭立即撤销功率请求；开启后由下一物理步独立索敌决定实际需求。</summary>
    public void SetStarted(bool started)
    {
        _isStarted = started;
        if (!started)
        {
            StopWorking();
        }
    }

    protected virtual void FixedUpdate()
    {
        Owner = GetComponentInParent<ControlUnit>();
        if (!_isStarted || Owner == null || !Owner.IsSimulationActive || !Owner.IsOperational)
        {
            StopWorking();
            return;
        }

        RefreshTarget();
        bool hasTarget = _target != null && _targetDurability != null;
        Supply.SetWorkRequested(this, hasTarget);
        if (!hasTarget || !Supply.isWorking)
        {
            HideShotFeedback();
            return;
        }

        AimAndFire(_targetDurability);
        UpdateShotFeedback();
    }

    protected virtual void OnDisable()
    {
        StopWorking();
    }

    private void StopWorking()
    {
        Supply?.SetWorkRequested(this, false);
        _target = null;
        _targetDurability = null;
        _nextSearchTime = 0f;
        HideShotFeedback();
    }

    private void RefreshTarget()
    {
        if (_target != null && (!_target.IsOperational || !_target.IsSimulationActive || _targetDurability == null || !_targetDurability.isActiveAndEnabled || _targetDurability.currentDurability <= 0f || (_targetDurability.transform.position - transform.position).sqrMagnitude > range * range))
        {
            _target = null;
            _targetDurability = null;
            _nextSearchTime = 0f;
        }

        if (Time.time < _nextSearchTime)
        {
            return;
        }

        UnitFaction faction = GetEffectiveTargetFaction();
        _target = _targetSelector.FindNearestTarget(
            ControlUnit.RuntimeUnits,
            Owner,
            faction,
            range,
            Owner.faction == UnitFaction.Player && faction == UnitFaction.Enemy,
            out _targetDurability);
        _nextSearchTime = Time.time + Mathf.Max(0.01f, targetRefreshInterval);
    }

    private void AimAndFire(Durability aimTarget)
    {
        Vector3 direction = aimTarget.transform.position - GetMuzzlePosition();
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        AimAt(direction.normalized);
        Vector3 origin = GetMuzzlePosition();
        Vector3 aimDirection = (aimTarget.transform.position - origin).normalized;
        Vector3 fireDirection = GetAimForward();
        if (Vector3.Angle(fireDirection, aimDirection) <= maxFireAngle && Time.time >= _nextFireTime)
        {
            Fire(origin, fireDirection, GetEffectiveTargetFaction());
            _nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval) / Mathf.Max(Supply.efficiency, 0.0001f);
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
        float elevation = Mathf.Clamp(Mathf.Atan2(localDirection.y, planarMagnitude) * Mathf.Rad2Deg, minElevation, maxElevation);
        float currentPitch = Mathf.DeltaAngle(0f, verticalAxis.localEulerAngles.x);
        float nextPitch = Mathf.MoveTowardsAngle(currentPitch, -elevation, verticalTurnSpeed * Time.fixedDeltaTime);
        verticalAxis.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);
    }

    private void ResolveAimingRig()
    {
        Transform model = transform.Find("Model");
        Transform generated = model != null ? model.Find("IndustrialVisual/LOD0") : null;
        if (aimPivot == null)
        {
            aimPivot = generated != null ? generated : model != null ? model : transform;
        }

        if (horizontalAxis == null)
        {
            horizontalAxis = generated != null ? generated.Find("Horizontal") : aimPivot;
        }

        if (horizontalAxis == null)
        {
            horizontalAxis = aimPivot;
        }

        if (verticalAxis == null)
        {
            verticalAxis = horizontalAxis.Find("Vertical");
            if (verticalAxis == null)
            {
                verticalAxis = horizontalAxis;
            }
        }

        if (muzzle == null && verticalAxis != horizontalAxis)
        {
            muzzle = verticalAxis.Find("Muzzle");
        }
    }

    protected Vector3 GetMuzzlePosition()
    {
        if (muzzle != null)
        {
            return muzzle.position;
        }

        Transform origin = verticalAxis != null ? verticalAxis : horizontalAxis != null ? horizontalAxis : aimPivot;
        return origin.position + GetAimForward() * 0.7f;
    }

    protected Vector3 GetAimForward()
    {
        Transform axis = muzzle != null ? muzzle : verticalAxis != null ? verticalAxis : horizontalAxis != null ? horizontalAxis : aimPivot;
        return axis.forward;
    }

    protected UnitFaction GetEffectiveTargetFaction()
    {
        return targetFaction != Owner.faction ? targetFaction : Owner.faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
    }

    protected abstract void Fire(Vector3 origin, Vector3 direction, UnitFaction faction);
    protected virtual void HideShotFeedback()
    {
    }

    protected virtual void UpdateShotFeedback()
    {
    }
}
