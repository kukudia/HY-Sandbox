// 职责：装配悬浮参数和推进器缓存，在有效游玩归属下应用 HoverControlSolver 的控制输出。
// 入口：Init()。
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Power))]
public class HoverFlightController : MonoBehaviour
{
    [Header("悬浮参数")]
    [Tooltip("飞行器需要维持的目标悬浮高度（单位：米）")]
    public float targetHoverHeight = 10f;
    [Tooltip("允许的高度误差范围，在此范围内不会进行高度调整")]
    public float heightTolerance = 0.1f;
    //[Tooltip("最大推力乘数，允许推进器超过其标称最大推力的倍数")]
    //public float maxThrustMultiplier = 1.5f;
    [Header("姿态控制参数")]
    [Tooltip("倾斜控制的比例系数 - 影响姿态恢复速度")]
    public float tiltP = 8f;
    [Tooltip("倾斜控制的微分系数 - 减少姿态振荡")]
    public float tiltD = 2f;
    [Tooltip("允许的最大倾斜角度（单位：度），超过此角度将触发姿态矫正")]
    public float maxTiltAngle = 15f;
    [Tooltip("姿态调整的平滑度（值越高调整越快）")]
    public float rotationSmoothing = 5f;
    [Header("高度控制参数")]
    [Tooltip("高度比例系数")]
    private float HeightP = 0.5f;
    [Tooltip("高度PID积分系数")]
    public float heightI = 0.1f;
    [Tooltip("高度PID微分系数")]
    public float heightD = 2f;
    [Header("重力补偿")]
    [Tooltip("重力补偿系数（1.0 = 完全抵消重力）")]
    public float gravityCompensationFactor = 1.0f;
    private Rigidbody rb; // 飞行器的刚体组件
    private ControlUnit controlUnit;
    private Power power;
    [Tooltip("飞行器上所有的悬浮推进器数组（会自动从子物体收集）")]
    public HoverThruster[] thrusters;
    // 动态高度系数相关
    private float currentHeightP;
    private HoverThruster[] cachedThrusters = new HoverThruster[0];
    private Transform[] cachedThrusterTransforms = new Transform[0];
    private float[] cachedThrustRatios = new float[0];
    private HoverThruster[] cachedSourceThrusters;
    private int cachedSourceThrusterCount = -1;
    private bool thrusterCacheDirty = true;
    private float totalMaxThrust;
    private float maxDistanceFromCOM = 1f;
    private float cachedMass = -1f;
    private float cachedFixedDeltaTime = -1f;
    private Vector3 cachedGravity;
    private float cachedGravityMagnitude;
    private float cachedMaxTiltAngle = float.MinValue;
    private float maxTiltAngleCos;
    private readonly HoverControlSolver _solver = new HoverControlSolver();
    private HoverControlSolver.TiltCorrection _tiltCorrection;
    public bool setHeight = false;
    public float TargetHeight => targetHoverHeight;
    public float CurrentHeight => transform.position.y;
    public float HeightPValue => currentHeightP;
    public bool IsUsedByControlUnit => isActiveAndEnabled && controlUnit != null && controlUnit.HasValidCockpit && controlUnit.hoverFlightController == this && thrusters != null && thrusters.Length > 0;

    public void Init()
    {
        power = GetComponent<Power>();
        Rigidbody ownerBody = GetComponentInParent<Rigidbody>();
        ControlUnit owner = GetComponentInParent<ControlUnit>();
        if (rb != ownerBody || controlUnit != owner)
        {
            _solver.Reset();
        }

        rb = ownerBody;
        controlUnit = owner;
        RefreshCachedPhysicsValues();
        RebuildThrusterCache();
        // 配置所有推进器
        //foreach (var thruster in thrusters)
        //{
        //    thruster.hoverHeight = targetHoverHeight;
        //}
    }

    private void OnEnable()
    {
        _solver.Reset();
        thrusterCacheDirty = true;
    }

    private void OnDisable()
    {
        power?.SetWorkRequested(this, false);
        StopControlOutput();
    }

    private void OnValidate()
    {
        thrusterCacheDirty = true;
        RefreshTiltLimitCache();
    }

    private void FixedUpdate()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            power?.SetWorkRequested(this, false);
            StopControlOutput();
            return;
        }

        if (power == null)
        {
            power = GetComponent<Power>();
        }

        bool requested = EnsureControllerReady() && HasActiveHoverThruster();
        power?.SetWorkRequested(this, requested);
        if (!requested || power == null || !power.isWorking)
        {
            StopControlOutput();
            return;
        }

        bool acceptsPlayerInput = controlUnit.AcceptsPlayerInput;
        Keyboard keyboard = Keyboard.current;
        if (acceptsPlayerInput && keyboard != null && keyboard.qKey.isPressed && !keyboard.eKey.isPressed)
        {
            targetHoverHeight += 0.2f;
        }

        if (acceptsPlayerInput && keyboard != null && keyboard.eKey.isPressed && !keyboard.qKey.isPressed)
        {
            targetHoverHeight -= 0.2f;
        }

        Vector3 position = transform.position;
        Vector3 currentUp = transform.up;
        if (acceptsPlayerInput && keyboard != null && (keyboard.eKey.wasReleasedThisFrame || keyboard.qKey.wasReleasedThisFrame))
        {
            targetHoverHeight = position.y;
        }

        float heightError = targetHoverHeight - position.y;
        float absHeightError = Mathf.Abs(heightError);
        // 高度PID控制
        float heightAdjustment = _solver.StepHeight(heightError, currentHeightP, heightI, heightD, cachedFixedDeltaTime);
        // 重力补偿计算
        float gravityCompensation = HoverControlSolver.CalculateGravityCompensation(cachedMass, cachedGravityMagnitude, gravityCompensationFactor, absHeightError, heightTolerance);
        // 姿态稳定控制
        _tiltCorrection = HoverControlSolver.CalculateTilt(currentUp, rb.angularVelocity, maxTiltAngleCos, tiltP, tiltD);
        DistributeThrust(heightAdjustment + gravityCompensation, currentUp);
        // 应用旋转修正
        ApplyRotationCorrection(currentUp, 1f);
    }

    private bool HasActiveHoverThruster()
    {
        foreach (HoverThruster thruster in cachedThrusters)
        {
            if (thruster != null && thruster.isActiveAndEnabled && thruster.isHovered)
            {
                return true;
            }
        }

        return false;
    }

    private void StopControlOutput()
    {
        _solver.Reset();
        _tiltCorrection = default;
        ClearHoverThrust();
    }

    private void ClearHoverThrust()
    {
        if (thrusters == null)
        {
            return;
        }

        foreach (HoverThruster thruster in thrusters)
        {
            if (thruster == null)
            {
                continue;
            }

            // Regrouping can move a cached thruster to another controller. Do not clear its new output.
            ControlUnit owner = thruster.controlUnit;
            if (owner != null && owner.hoverFlightController != null && owner.hoverFlightController != this)
            {
                continue;
            }

            thruster.SetControlledThrust(0f);
        }
    }

    private bool EnsureControllerReady()
    {
        ControlUnit actualOwner = controlUnit;
        if (actualOwner == null || !transform.IsChildOf(actualOwner.transform))
        {
            actualOwner = GetComponentInParent<ControlUnit>();
        }

        if (controlUnit != actualOwner)
        {
            controlUnit = actualOwner;
            rb = GetComponentInParent<Rigidbody>();
            thrusterCacheDirty = true;
            _solver.Reset();
        }

        // Reject stale ownership before rebuilding a cache that would otherwise rebind moved thrusters.
        if (controlUnit == null || !controlUnit.HasValidCockpit || controlUnit.hoverFlightController != this)
        {
            return false;
        }

        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        if (rb == null)
        {
            return false;
        }

        RefreshCachedPhysicsValues();
        if (thrusterCacheDirty || !ReferenceEquals(cachedSourceThrusters, thrusters) || cachedSourceThrusterCount != (thrusters != null ? thrusters.Length : 0))
        {
            RebuildThrusterCache();
        }

        return cachedThrusters.Length > 0 && totalMaxThrust > 1e-5f;
    }

    private void RefreshCachedPhysicsValues()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;
        if (!Mathf.Approximately(cachedFixedDeltaTime, fixedDeltaTime))
        {
            cachedFixedDeltaTime = fixedDeltaTime;
        }

        if (rb != null && !Mathf.Approximately(cachedMass, rb.mass))
        {
            cachedMass = rb.mass;
            currentHeightP = cachedMass * HeightP;
        }

        Vector3 gravity = Physics.gravity;
        if (cachedGravity != gravity)
        {
            cachedGravity = gravity;
            cachedGravityMagnitude = gravity.magnitude;
        }

        if (!Mathf.Approximately(cachedMaxTiltAngle, maxTiltAngle))
        {
            RefreshTiltLimitCache();
        }
    }

    private void RefreshTiltLimitCache()
    {
        cachedMaxTiltAngle = maxTiltAngle;
        maxTiltAngleCos = Mathf.Cos(maxTiltAngle * Mathf.Deg2Rad);
    }

    private void RebuildThrusterCache()
    {
        cachedSourceThrusters = thrusters;
        cachedSourceThrusterCount = thrusters != null ? thrusters.Length : 0;
        thrusterCacheDirty = false;
        totalMaxThrust = 0f;
        if (thrusters == null || thrusters.Length == 0)
        {
            cachedThrusters = new HoverThruster[0];
            cachedThrusterTransforms = new Transform[0];
            cachedThrustRatios = new float[0];
            maxDistanceFromCOM = 1f;
            return;
        }

        int validCount = 0;
        for (int i = 0; i < thrusters.Length; i++)
        {
            HoverThruster thruster = thrusters[i];
            if (thruster == null || thruster.maxThrust <= 0f)
            {
                continue;
            }

            totalMaxThrust += thruster.maxThrust;
            validCount++;
        }

        if (validCount == 0 || totalMaxThrust <= 1e-5f)
        {
            cachedThrusters = new HoverThruster[0];
            cachedThrusterTransforms = new Transform[0];
            cachedThrustRatios = new float[0];
            maxDistanceFromCOM = 1f;
            return;
        }

        if (cachedThrusters.Length != validCount)
        {
            cachedThrusters = new HoverThruster[validCount];
            cachedThrusterTransforms = new Transform[validCount];
            cachedThrustRatios = new float[validCount];
        }

        Vector3 centerOfMass = rb != null ? rb.worldCenterOfMass : transform.position;
        float maxDistanceSqr = 0f;
        int cachedIndex = 0;
        for (int i = 0; i < thrusters.Length; i++)
        {
            HoverThruster thruster = thrusters[i];
            if (thruster == null || thruster.maxThrust <= 0f)
            {
                continue;
            }

            Transform thrusterTransform = thruster.transform;
            thruster.SetRuntimeReferences(controlUnit, rb);
            cachedThrusters[cachedIndex] = thruster;
            cachedThrusterTransforms[cachedIndex] = thrusterTransform;
            cachedThrustRatios[cachedIndex] = thruster.maxThrust / totalMaxThrust;
            float distanceSqr = (thrusterTransform.position - centerOfMass).sqrMagnitude;
            if (distanceSqr > maxDistanceSqr)
            {
                maxDistanceSqr = distanceSqr;
            }

            cachedIndex++;
        }

        maxDistanceFromCOM = maxDistanceSqr > 1e-5f ? Mathf.Sqrt(maxDistanceSqr) : 1f;
    }

    private void DistributeThrust(float heightAdjustment, Vector3 currentUp)
    {
        Vector3 centerOfMass = rb.worldCenterOfMass;
        float inverseMaxDistance = maxDistanceFromCOM > 1e-5f ? 1f / maxDistanceFromCOM : 1f;
        // 计算总需求推力（限制在最大能力范围内）
        float totalThrustRequired = Mathf.Clamp(heightAdjustment, 0, totalMaxThrust);
        // 按推进器最大推力比例分配基础推力
        for (int i = 0; i < cachedThrusters.Length; i++)
        {
            HoverThruster thruster = cachedThrusters[i];
            Transform thrusterTransform = cachedThrusterTransforms[i];
            if (thruster == null || thrusterTransform == null)
            {
                thrusterCacheDirty = true;
                continue;
            }

            // 核心修改：按推力占比分配基础推力
            float baseThrust = totalThrustRequired * cachedThrustRatios[i];
            // 姿态调整推力（保持原逻辑）
            float tiltThrust = 0f;
            if (_tiltCorrection.IsActive)
            {
                Vector3 positionFromCOM = thrusterTransform.position - centerOfMass;
                Vector3 torqueDirection = Vector3.Cross(positionFromCOM, currentUp);
                float torqueDirectionSqr = torqueDirection.sqrMagnitude;
                if (torqueDirectionSqr > 1e-6f)
                {
                    torqueDirection /= Mathf.Sqrt(torqueDirectionSqr);
                    float torqueEffectiveness = Vector3.Dot(torqueDirection, _tiltCorrection.Direction);
                    float distanceWeight = Mathf.Sqrt(positionFromCOM.sqrMagnitude) * inverseMaxDistance;
                    tiltThrust = _tiltCorrection.Force * torqueEffectiveness * distanceWeight;
                }
            }

            // 合并推力并限制范围
            float finalThrust = Mathf.Clamp(baseThrust + tiltThrust, 0, thruster.maxThrust);
            thruster.SetControlledThrust(thruster.ShouldActivate() ? finalThrust : 0f);
        }
    }

    private void ApplyRotationCorrection(Vector3 currentUp, float outputEfficiency)
    {
        if (HoverControlSolver.TryCalculateAngularVelocity(
            rb.rotation,
            currentUp,
            rotationSmoothing,
            cachedFixedDeltaTime,
            outputEfficiency,
            out Vector3 angularVelocity))
        {
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, angularVelocity, 0.1f);
        }
    }

    // 在编辑器中可视化
    private void OnDrawGizmosSelected()
    {
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        if (rb == null)
        {
            return;
        }

        // 绘制目标高度平面
        Gizmos.color = Color.green;
        Vector3 planeCenter = new Vector3(transform.position.x, targetHoverHeight, transform.position.z);
        Gizmos.DrawWireCube(planeCenter, new Vector3(5, 0.01f, 5));
        // 绘制重心位置
        Gizmos.color = Color.red;
        Vector3 comPosition = transform.position + rb.centerOfMass;
        Gizmos.DrawSphere(comPosition, 0.2f);
        // 绘制推进器位置
        Gizmos.color = Color.blue;
        if (thrusters == null)
        {
            return;
        }

        foreach (var thruster in thrusters)
        {
            if (thruster == null)
            {
                continue;
            }

            Gizmos.DrawLine(comPosition, thruster.transform.position);
            Gizmos.DrawSphere(thruster.transform.position, 0.1f);
        }
    }
}
