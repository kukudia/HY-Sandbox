using UnityEngine;
using UnityEngine.InputSystem;

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
    [Tooltip("基础高度比例系数")]
    public float baseHeightP = 5f;

    [Tooltip("高度比例系数的最小值")]
    public float minHeightP = 2f;

    [Tooltip("高度比例系数的最大值")]
    public float maxHeightP = 15f;

    [Tooltip("高度误差的响应曲线（X轴：高度误差，Y轴：比例系数乘数）")]
    public AnimationCurve heightPResponseCurve = AnimationCurve.Linear(0, 1, 10, 3);

    [Tooltip("基于速度的比例系数调整曲线（X轴：垂直速度，Y轴：比例系数乘数）")]
    public AnimationCurve velocityHeightPAdjustment = AnimationCurve.Linear(-5, 1.5f, 5, 1.5f);

    [Tooltip("高度PID积分系数")]
    public float heightI = 0.1f;

    [Tooltip("高度PID微分系数")]
    public float heightD = 2f;

    [Header("重力补偿")]
    [Tooltip("重力补偿系数（1.0 = 完全抵消重力）")]
    public float gravityCompensationFactor = 1.0f;

    private Rigidbody rb; // 飞行器的刚体组件

    [Tooltip("飞行器上所有的悬浮推进器数组（会自动从子物体收集）")]
    public HoverThruster[] thrusters;

    private float heightErrorIntegral; // 高度误差的积分项（用于PID控制）
    private float lastHeightError;     // 上一帧的高度误差
    private float lastHeight;          // 上一帧的飞行器高度

    // 姿态控制相关
    private Vector3 lastUpVector;       // 上一帧的上方向向量
    private Vector3 targetUpVector = Vector3.up; // 目标上方向（始终垂直向上）
    private float tiltCorrectionForce;  // 当前倾斜矫正力的大小

    // 动态高度系数相关
    private float currentHeightP;
    private float verticalVelocity;

    [Tooltip("是否在游戏运行时显示调试UI")]
    public bool showUI = true;

    private GUIStyle headerStyle; // GUI标题样式
    private GUIStyle labelStyle;  // GUI标签样式

    public void Init()
    {
        rb = GetComponent<Rigidbody>();
        lastHeight = transform.position.y;
        lastUpVector = transform.up;

        // 初始化动态高度系数
        currentHeightP = baseHeightP;

        // 设置默认响应曲线（如果未在编辑器中设置）
        if (heightPResponseCurve.length == 0)
        {
            heightPResponseCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(5f, 2f),
                new Keyframe(10f, 3f)
            );
        }

        if (velocityHeightPAdjustment.length == 0)
        {
            velocityHeightPAdjustment = new AnimationCurve(
                new Keyframe(-10f, 1.8f),
                new Keyframe(0f, 1f),
                new Keyframe(10f, 1.8f)
            );
        }

        // 配置所有推进器
        foreach (var thruster in thrusters)
        {
            thruster.hoverHeight = targetHoverHeight;
        }
    }

    private void FixedUpdate()
    {
        if (thrusters.Length == 0) return;

        if (Keyboard.current.qKey.isPressed && !Keyboard.current.eKey.isPressed)
        {
            targetHoverHeight += 0.2f;
        }

        if (Keyboard.current.eKey.isPressed && !Keyboard.current.qKey.isPressed)
        {
            targetHoverHeight -= 0.2f;
        }

        float currentHeight = transform.position.y;
        float heightError = targetHoverHeight - currentHeight;

        // 高度PID控制
        float heightAdjustment = CalculateHeightAdjustment(heightError);

        // 计算垂直速度
        verticalVelocity = (transform.position.y - lastHeight) / Time.fixedDeltaTime;

        // 重力补偿计算
        float gravityCompensation = CalculateGravityCompensation();

        // 姿态稳定控制
        Vector3 tiltAdjustment = CalculateTiltAdjustment();

        // 分配推力到各个推进器
        DistributeThrust(heightAdjustment + gravityCompensation, tiltAdjustment);

        // 应用旋转修正
        ApplyRotationCorrection();

        // 更新状态
        lastHeightError = heightError;
        lastHeight = currentHeight;
        lastUpVector = transform.up;
    }

    private float CalculateHeightAdjustment(float heightError)
    {
        // 计算动态高度比例系数
        UpdateDynamicHeightP(heightError);

        // 积分项
        heightErrorIntegral += heightError * Time.fixedDeltaTime;

        // 微分项
        float heightErrorDerivative = (heightError - lastHeightError) / Time.fixedDeltaTime;

        // PID计算 (使用动态heightP)
        return heightError * currentHeightP +
               heightErrorIntegral * heightI +
               heightErrorDerivative * heightD;
    }

    private void UpdateDynamicHeightP(float heightError)
    {
        // 1. 基于高度误差的调整
        float absError = Mathf.Abs(heightError);
        float errorAdjustment = heightPResponseCurve.Evaluate(absError);

        // 2. 基于垂直速度的调整
        float velocityAdjustment = velocityHeightPAdjustment.Evaluate(verticalVelocity);

        // 3. 组合调整因子
        float combinedAdjustment = errorAdjustment * velocityAdjustment;

        // 4. 计算最终高度P值（应用限制）
        currentHeightP = Mathf.Clamp(baseHeightP * combinedAdjustment, minHeightP, maxHeightP);

        // 5. 当接近目标高度时降低响应（防止振荡）
        if (absError < heightTolerance * 2)
        {
            currentHeightP *= Mathf.Clamp01(absError / heightTolerance);
        }
    }

    private float CalculateGravityCompensation()
    {
        // 计算克服重力所需的最小推力
        float gravityForce = rb.mass * Physics.gravity.magnitude * gravityCompensationFactor;

        // 根据高度误差调整补偿力度
        float heightError = targetHoverHeight - transform.position.y;
        float compensationFactor = Mathf.Clamp01(Mathf.Abs(heightError) / heightTolerance);

        return gravityForce * compensationFactor;
    }

    private Vector3 CalculateTiltAdjustment()
    {
        // 计算当前倾斜角度
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        if (tiltAngle > maxTiltAngle)
        {
            // 计算倾斜方向
            Vector3 tiltDirection = Vector3.Cross(transform.up, Vector3.up).normalized;

            // 计算角速度
            Vector3 angularVelocity = rb.angularVelocity;
            Vector3 angularVelocityInTiltDir = Vector3.Project(angularVelocity, tiltDirection);

            // PID计算
            float tiltError = tiltAngle * Mathf.Deg2Rad;
            float tiltErrorDerivative = angularVelocityInTiltDir.magnitude;

            tiltCorrectionForce = tiltError * tiltP + tiltErrorDerivative * tiltD;

            return tiltDirection * tiltCorrectionForce;
        }

        return Vector3.zero;
    }

    private void DistributeThrust(float heightAdjustment, Vector3 tiltAdjustment)
    {
        // 计算重心位置
        Vector3 centerOfMass = rb.centerOfMass + transform.position;

        // 计算总推力需求
        float totalThrustRequired = Mathf.Clamp(
            heightAdjustment,
            0,
            GetTotalMaxThrust()
        );

        foreach (var thruster in thrusters)
        {
            // 1. 基础高度推力分配
            float baseThrust = totalThrustRequired / thrusters.Length;

            // 2. 姿态调整推力分配
            Vector3 positionFromCOM = thruster.transform.position - centerOfMass;
            float tiltThrust = 0f;

            if (tiltAdjustment != Vector3.zero)
            {
                // 计算推进器位置对扭矩的贡献
                Vector3 torqueDirection = Vector3.Cross(positionFromCOM, transform.up).normalized;
                float torqueEffectiveness = Vector3.Dot(torqueDirection, tiltAdjustment.normalized);

                // 根据距离重心距离加权
                float distanceWeight = positionFromCOM.magnitude / GetMaxDistanceFromCOM();
                tiltThrust = tiltCorrectionForce * torqueEffectiveness * distanceWeight;
            }

            // 3. 组合推力并限制范围
            float finalThrust = Mathf.Clamp(
                baseThrust + tiltThrust,
                0,
                thruster.maxThrust
            );

            // 应用推力
            thruster.thrust = thruster.ShouldActivate() ? finalThrust : 0;
        }
    }

    private void ApplyRotationCorrection()
    {
        // 计算目标旋转（垂直向上）
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * rb.rotation;

        // 平滑旋转
        Quaternion newRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSmoothing * Time.fixedDeltaTime
        );

        // 应用旋转（通过角速度实现平滑物理效果）
        Quaternion rotationDelta = newRotation * Quaternion.Inverse(rb.rotation);
        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;
        if (Mathf.Abs(angle) > 0.01f)
        {
            Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad / Time.fixedDeltaTime;
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, angularVelocity, 0.1f);
        }
    }

    private float GetTotalMaxThrust()
    {
        float total = 0f;
        foreach (var thruster in thrusters)
        {
            total += thruster.maxThrust;
        }
        return total;
    }

    private float GetMaxDistanceFromCOM()
    {
        Vector3 centerOfMass = rb.centerOfMass + transform.position;
        float maxDistance = 0f;

        foreach (var thruster in thrusters)
        {
            float distance = Vector3.Distance(thruster.transform.position, centerOfMass);
            if (distance > maxDistance) maxDistance = distance;
        }

        return maxDistance > 0 ? maxDistance : 1f;
    }

    public void SetTargetHeight(float newHeight)
    {
        targetHoverHeight = newHeight;
        foreach (var thruster in thrusters)
        {
            thruster.hoverHeight = newHeight;
        }
    }

    // 在编辑器中可视化
    private void OnDrawGizmosSelected()
    {
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
        foreach (var thruster in thrusters)
        {
            Gizmos.DrawLine(comPosition, thruster.transform.position);
            Gizmos.DrawSphere(thruster.transform.position, 0.1f);
        }
    }

    void OnGUI()
    {
        if (!showUI || thrusters == null) return;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.cyan;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 13;
        labelStyle.normal.textColor = Color.white;

        GUILayout.BeginArea(new Rect(20, 20, 320, 600), GUI.skin.window);

        GUILayout.Label("Hover Flight Controll System", headerStyle);

        GUILayout.Space(8);
        GUILayout.Label($"Target Height: {targetHoverHeight:F2}", labelStyle);
        GUILayout.Label($"Current Height: {transform.position.y:F2}", labelStyle);
        GUILayout.Label($"Dynamic Height P: {currentHeightP:F2}", labelStyle);
        GUILayout.Label($"Vertical Velocity: {verticalVelocity:F2} m/s", labelStyle);

        GUILayout.Space(10);
        GUILayout.Label("Hover Thrusters:", headerStyle);

        for (int i = 0; i < thrusters.Length; i++)
        {
            if (thrusters[i] == null) continue;

            float norm = thrusters[i].maxThrust > 1e-5f ? thrusters[i].thrust / thrusters[i].maxThrust : 0f;
            Color barColor = Color.Lerp(Color.red, Color.green, norm);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"#{i} {thrusters[i].thrust:F1}/{thrusters[i].maxThrust}", labelStyle);

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
}
