using UnityEngine;
using UnityEngine.InputSystem;

public abstract class Thruster : MonoBehaviour
{
    public ControlUnit controlUnit;
    public Transform model;
    public Transform cameraTransform;   // 主摄像机
    public float thrust;     // 推力大小
    public float lastThrustValue;
    public float maxThrust = 100f;

    [Tooltip("最大推力变化率（单位/秒），防止推力瞬间变化")]
    public float maxThrustChangeRate = 50f;

    public Vector3 thrustDirection = Vector3.forward; // 推力方向（本地坐标）

    public Rigidbody rb;

    // 子类必须实现：如何启用推进器（输入控制/自动触发）
    public abstract bool ShouldActivate();

    protected virtual void Start()
    {
        if (model == null)
        {
            model = transform.Find("Model");
        }

        // 初始化推力记录数组
        lastThrustValue = thrust;
    }

    protected virtual Vector3 GetInputDirection()
    {
        Vector3 dir = Vector3.zero;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // 摄像机 forward / right 的水平分量
        Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        if (Keyboard.current.wKey.isPressed) dir += camFwd;
        if (Keyboard.current.sKey.isPressed) dir -= camFwd;
        if (Keyboard.current.aKey.isPressed) dir -= camRight;
        if (Keyboard.current.dKey.isPressed) dir += camRight;

        if (dir.sqrMagnitude > 1e-6f)
            dir.Normalize();

        return dir;
    }

    public virtual void ApplyThrustChangeRateLimit()
    {
        float maxChange = maxThrustChangeRate * Time.fixedDeltaTime;

        // 计算允许的推力变化范围
        float minT = Mathf.Max(0, lastThrustValue - maxChange);
        float maxT = Mathf.Min(maxThrust, lastThrustValue + maxChange);

        // 应用限制
        thrust = Mathf.Clamp(thrust, minT, maxT);

        // 记录当前推力供下一帧使用
        lastThrustValue = thrust;
    }
}
