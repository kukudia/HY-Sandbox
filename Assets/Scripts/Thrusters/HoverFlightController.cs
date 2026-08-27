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

    [Tooltip("飞行器上所有的悬浮推进器数组（会自动从子物体收集）")]
    public HoverThruster[] thrusters;

    private float heightErrorIntegral; // 高度误差的积分项（用于PID控制）
    private float lastHeightError;     // 上一帧的高度误差

    // 姿态控制相关
    private Vector3 lastUpVector;       // 上一帧的上方向向量
    private Vector3 targetUpVector = Vector3.up; // 目标上方向（始终垂直向上）
    private float tiltCorrectionForce;  // 当前倾斜矫正力的大小

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
    private float inverseFixedDeltaTime;
    private Vector3 cachedGravity;
    private float cachedGravityMagnitude;
    private float cachedMaxTiltAngle = float.MinValue;
    private float maxTiltAngleCos;
    private bool hasTiltCorrection;
    private Vector3 tiltCorrectionDirection;

    public bool showUI = false;
    public bool setHeight = false;

    private GUIStyle headerStyle; // GUI标题样式
    private GUIStyle labelStyle;  // GUI标签样式
    private string targetHeightText;
    private string currentHeightText;
    private string heightPText;
    private string verticalVelocityText;
    private string horizontalVelocityText;
    private readonly string[] thrusterTexts = new string[64];
    private float nextUiRefreshTime;

    public void Init()
    {
        rb = GetComponentInParent<Rigidbody>();
        controlUnit = GetComponentInParent<ControlUnit>();
        lastUpVector = transform.up;

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
        thrusterCacheDirty = true;
    }

    private void OnValidate()
    {
        thrusterCacheDirty = true;
        RefreshTiltLimitCache();
    }

    private void FixedUpdate()
    {
        if (!EnsureControllerReady()) return;

        bool acceptsPlayerInput = controlUnit == null || controlUnit.faction == UnitFaction.Player;
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
        float heightAdjustment = CalculateHeightAdjustment(heightError);

        // 重力补偿计算
        float gravityCompensation = CalculateGravityCompensation(absHeightError);

        // 姿态稳定控制
        CalculateTiltAdjustment(currentUp);

        // 分配推力到各个推进器
        DistributeThrust(heightAdjustment + gravityCompensation, currentUp);

        // 应用旋转修正
        ApplyRotationCorrection(currentUp);

        // 更新状态
        lastHeightError = heightError;
        lastUpVector = currentUp;
    }

    private bool EnsureControllerReady()
    {
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        if (rb == null)
        {
            return false;
        }

        if (controlUnit == null)
        {
            controlUnit = GetComponentInParent<ControlUnit>();
        }

        RefreshCachedPhysicsValues();

        if (thrusterCacheDirty ||
            !ReferenceEquals(cachedSourceThrusters, thrusters) ||
            cachedSourceThrusterCount != (thrusters != null ? thrusters.Length : 0))
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
            inverseFixedDeltaTime = fixedDeltaTime > 1e-5f ? 1f / fixedDeltaTime : 0f;
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
            if (thruster == null || thruster.maxThrust <= 0f) continue;

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
            if (thruster == null || thruster.maxThrust <= 0f) continue;

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

    private float CalculateHeightAdjustment(float heightError)
    {
        // 计算动态高度比例系数
        //UpdateDynamicHeightP(heightError);

        // 积分项
        heightErrorIntegral += heightError * cachedFixedDeltaTime;

        // 微分项
        float heightErrorDerivative = (heightError - lastHeightError) * inverseFixedDeltaTime;

        // PID计算 (使用动态heightP)
        return heightError * currentHeightP +
               heightErrorIntegral * heightI +
               heightErrorDerivative * heightD;
    }

    private void UpdateDynamicHeightP(float heightError)
    {
        // 1. 基于高度误差的调整
        float absError = Mathf.Abs(heightError);

        // 5. 当接近目标高度时降低响应（防止振荡）
        if (absError < heightTolerance * 2)
        {
            currentHeightP *= Mathf.Clamp01(absError / heightTolerance);
        }
    }

    private float CalculateGravityCompensation(float absHeightError)
    {
        // 计算克服重力所需的最小推力
        float gravityForce = cachedMass * cachedGravityMagnitude * gravityCompensationFactor;

        // 根据高度误差调整补偿力度
        float safeHeightTolerance = Mathf.Max(heightTolerance, 1e-5f);
        float compensationFactor = Mathf.Clamp01(absHeightError / safeHeightTolerance);

        return gravityForce * compensationFactor;
    }

    private void CalculateTiltAdjustment(Vector3 currentUp)
    {
        hasTiltCorrection = false;
        tiltCorrectionForce = 0f;

        // 计算当前倾斜角度
        float upDot = Mathf.Clamp(Vector3.Dot(currentUp, targetUpVector), -1f, 1f);

        if (upDot < maxTiltAngleCos)
        {
            // 计算倾斜方向
            Vector3 tiltDirection = Vector3.Cross(currentUp, targetUpVector);
            float tiltDirectionSqr = tiltDirection.sqrMagnitude;
            if (tiltDirectionSqr <= 1e-6f)
            {
                return;
            }

            tiltDirection /= Mathf.Sqrt(tiltDirectionSqr);

            // 计算角速度
            float angularVelocityInTiltDir = Mathf.Abs(Vector3.Dot(rb.angularVelocity, tiltDirection));

            // PID计算
            float tiltError = Mathf.Acos(upDot);
            float tiltErrorDerivative = angularVelocityInTiltDir;

            tiltCorrectionForce = tiltError * tiltP + tiltErrorDerivative * tiltD;
            tiltCorrectionDirection = tiltDirection;
            hasTiltCorrection = tiltCorrectionForce > 1e-5f;

            return;
        }
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
            if (hasTiltCorrection)
            {
                Vector3 positionFromCOM = thrusterTransform.position - centerOfMass;
                Vector3 torqueDirection = Vector3.Cross(positionFromCOM, currentUp);
                float torqueDirectionSqr = torqueDirection.sqrMagnitude;

                if (torqueDirectionSqr > 1e-6f)
                {
                    torqueDirection /= Mathf.Sqrt(torqueDirectionSqr);
                    float torqueEffectiveness = Vector3.Dot(torqueDirection, tiltCorrectionDirection);
                    float distanceWeight = Mathf.Sqrt(positionFromCOM.sqrMagnitude) * inverseMaxDistance;
                    tiltThrust = tiltCorrectionForce * torqueEffectiveness * distanceWeight;
                }
            }

            // 合并推力并限制范围
            float finalThrust = Mathf.Clamp(
                baseThrust + tiltThrust,
                0,
                thruster.maxThrust
            );

            thruster.thrust = thruster.ShouldActivate() ? finalThrust : 0;
            thruster.ApplyThrustChangeRateLimit();
        }
    }

    private void ApplyRotationCorrection(Vector3 currentUp)
    {
        if (rotationSmoothing <= 0f || Vector3.Dot(currentUp, targetUpVector) > 0.99995f)
        {
            return;
        }

        // 计算目标旋转（垂直向上）
        Quaternion targetRotation = Quaternion.FromToRotation(currentUp, targetUpVector) * rb.rotation;

        // 平滑旋转
        Quaternion newRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSmoothing * cachedFixedDeltaTime
        );

        // 应用旋转（通过角速度实现平滑物理效果）
        Quaternion rotationDelta = newRotation * Quaternion.Inverse(rb.rotation);
        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;
        if (Mathf.Abs(angle) > 0.01f)
        {
            Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad * inverseFixedDeltaTime;
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

        if (rb == null) return;

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
        if (thrusters == null) return;

        foreach (var thruster in thrusters)
        {
            if (thruster == null) continue;

            Gizmos.DrawLine(comPosition, thruster.transform.position);
            Gizmos.DrawSphere(thruster.transform.position, 0.1f);
        }
    }

    private void OnGUI()
    {
        if (!showUI || thrusters == null || !PlayManager.instance.playMode) return;

        EnsureGuiStyles();
        RefreshUiText();

        GUILayout.BeginArea(new Rect(20, 20, 320, 600), GUI.skin.window);

        GUILayout.Label("Hover Flight Controll System", headerStyle);

        GUILayout.Space(8);
        GUILayout.Label(targetHeightText, labelStyle);
        GUILayout.Label(currentHeightText, labelStyle);
        GUILayout.Label(heightPText, labelStyle);
        GUILayout.Label(verticalVelocityText, labelStyle);
        GUILayout.Label(horizontalVelocityText, labelStyle);

        GUILayout.Space(10);
        GUILayout.Label("Hover Thrusters:", headerStyle);

        for (int i = 0; i < thrusters.Length; i++)
        {
            if (thrusters[i] == null) continue;

            float norm = thrusters[i].maxThrust > 1e-5f ? thrusters[i].thrust / thrusters[i].maxThrust : 0f;
            Color barColor = Color.Lerp(Color.red, Color.green, norm);

            GUILayout.BeginHorizontal();
            GUILayout.Label(i < thrusterTexts.Length ? thrusterTexts[i] : string.Empty, labelStyle);

            if (thrusters[i].thrust > 0)
            {
                // 画进度条背景
                Rect r = GUILayoutUtility.GetRect(100, 18);
                GUI.color = Color.gray;
                GUI.Box(r, GUIContent.none);

                // 画推力值条
                Rect filled = new Rect(r.x, r.y, r.width * norm, r.height);
                GUI.color = barColor;
                GUI.Box(filled, GUIContent.none);
            }

            // 恢复颜色
            GUI.color = Color.white;

            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
    }

    private void EnsureGuiStyles()
    {
        if (headerStyle != null && labelStyle != null)
        {
            return;
        }

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        headerStyle.normal.textColor = Color.cyan;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13
        };
        labelStyle.normal.textColor = Color.white;
    }

    private void RefreshUiText()
    {
        if (Time.unscaledTime < nextUiRefreshTime)
        {
            return;
        }

        nextUiRefreshTime = Time.unscaledTime + 0.2f;
        targetHeightText = $"Target Height: {targetHoverHeight:F2}";
        currentHeightText = $"Current Height: {transform.position.y:F2}";
        heightPText = $"Height P: {currentHeightP:F2}";
        verticalVelocityText = $"Vertical Velocity: {PlayManager.instance.verticalVelocity:F2} m/s";
        horizontalVelocityText = $"Horizontal Velocity: {PlayManager.instance.horizontalVelocity:F2} m/s";

        int count = Mathf.Min(thrusters.Length, thrusterTexts.Length);
        for (int i = 0; i < count; i++)
        {
            if (thrusters[i] == null)
            {
                thrusterTexts[i] = string.Empty;
                continue;
            }

            thrusterTexts[i] = $"#{i} {thrusters[i].thrust:F1}/{thrusters[i].maxThrust}";
        }
    }
}
