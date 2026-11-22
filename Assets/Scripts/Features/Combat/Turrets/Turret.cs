// 职责：共享炮塔启动、独立索敌、功率请求和双轴瞄准；武器种类只实现发射与表现。
// 入口：SetStarted() / FixedUpdate()；禁用和退出运行时撤销功率需求。
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Power))]
public abstract class Turret : MonoBehaviour
{
    private static readonly Unity.Profiling.ProfilerMarker TargetMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.RefreshTarget");
    private static readonly Unity.Profiling.ProfilerMarker AimMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.AimAndFire");
    private static readonly Unity.Profiling.ProfilerMarker FireMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.Fire");
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

    internal void DelayTargetSearch(float seconds)
    {
        _nextSearchTime = Time.time + Mathf.Max(0f, seconds);
    }

    protected virtual void Awake()
    {
        Supply = GetComponent<Power>();
        Supply.TrackAvailability(_isStarted);
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
        Supply?.TrackAvailability(started);
        if (!started)
        {
            StopWorking();
        }
    }

    protected virtual void FixedUpdate()
    {
        Supply.TrackAvailability(_isStarted);
        Owner = GetComponentInParent<ControlUnit>();
        if (!_isStarted || Owner == null || !Owner.IsSimulationActive || !Owner.IsOperational)
        {
            StopWorking();
            return;
        }

        RefreshTarget();
        bool hasTarget = _target != null;
        Supply.SetWorkRequested(this, hasTarget);
        if (!hasTarget || !Supply.isWorking)
        {
            HideShotFeedback();
            return;
        }

        AimAndFire(_target);
        UpdateShotFeedback();
    }

    protected virtual void OnDisable()
    {
        Supply?.TrackAvailability(false);
        StopWorking();
    }

    private void StopWorking()
    {
        _targetSelector?.CancelScheduledSearch();
        Supply?.SetWorkRequested(this, false);
        _target = null;
        _nextSearchTime = 0f;
        HideShotFeedback();
    }

    private void RefreshTarget()
    {
        using var sample = TargetMarker.Auto();
        Vector3 origin = GetMuzzlePosition();
        _targetSelector.ConfigureVisibility(origin, hitLayers);
        if (Time.time >= _nextSearchTime && _targetSelector.TryScheduleSearch())
        {
            SearchTarget();
            return;
        }

        // 追踪阶段只更新已知命中点。开火前从转动后的炮口重新确认遮挡。
        if (_target != null && (!_target.isActiveAndEnabled || _target.faction != GetEffectiveTargetFaction() || _target.gameObject.scene != gameObject.scene || !_target.IsOperational || !_target.IsSimulationActive || !_targetSelector.UpdateTrackedPoint(_target, range)))
        {
            _target = null;
            if (Time.time < _nextSearchTime)
            {
                _nextSearchTime = 0f;
                if (_targetSelector.TryScheduleSearch())
                {
                    SearchTarget();
                }
            }
        }
    }

    private void SearchTarget()
    {
        UnitFaction faction = GetEffectiveTargetFaction();
        _target = _targetSelector.FindNearestTarget(
            ControlUnit.CombatUnits,
            Owner,
            faction,
            range,
            Owner.faction == UnitFaction.Player && faction == UnitFaction.Enemy);
        _nextSearchTime = Time.time + Mathf.Max(0.01f, targetRefreshInterval);
    }

    private void AimAndFire(ControlUnit aimTarget)
    {
        using var sample = AimMarker.Auto();
        Vector3 origin = GetMuzzlePosition();
        Vector3 direction = GetAimPosition(aimTarget, origin) - origin;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 aimDirection = direction.normalized;
        AimAt(aimDirection);
        if (Time.time < _nextFireTime)
        {
            return;
        }

        origin = GetMuzzlePosition();
        Vector3 fireDirection = GetAimForward();
        direction = GetAimPosition(aimTarget, origin) - origin;
        if (!IsAligned(fireDirection, direction))
        {
            return;
        }

        using var fireSample = FireMarker.Auto();
        _targetSelector.ConfigureVisibility(origin, hitLayers);
        if (!_targetSelector.HasLineOfSight(Owner, aimTarget, range))
        {
            StopWorking();
            return;
        }

        direction = GetAimPosition(aimTarget, origin) - origin;
        if (IsAligned(fireDirection, direction) && Fire(origin, fireDirection, GetEffectiveTargetFaction()))
        {
            ScheduleNextFire();
        }
    }

    private bool IsAligned(Vector3 forward, Vector3 direction)
    {
        return direction.sqrMagnitude >= 0.01f && Vector3.Angle(forward, direction) <= maxFireAngle;
    }

    private void ScheduleNextFire()
    {
        float efficiency = Mathf.Max(Supply.efficiency, 0.0001f);
        _nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval) / efficiency;
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

    // 激光即时命中；实体炮塔覆盖为基于实际炮速的提前量。
    protected Vector3 TargetSurfacePoint => _targetSelector.SurfacePoint;

    protected virtual Vector3 GetAimPosition(ControlUnit target, Vector3 origin)
    {
        return TargetSurfacePoint;
    }

    protected abstract bool Fire(Vector3 origin, Vector3 direction, UnitFaction faction);
    protected virtual void HideShotFeedback()
    {
    }

    protected virtual void UpdateShotFeedback()
    {
    }
}
