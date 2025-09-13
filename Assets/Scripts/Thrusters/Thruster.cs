using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public abstract class Thruster : MonoBehaviour
{
    public ControlUnit controlUnit;
    public Transform model;
    public float thrust;     // 推力大小
    public float lastThrustValue;
    public float maxThrust = 100f;

    [Tooltip("最大推力变化率（单位/秒），防止推力瞬间变化")]
    public float maxThrustChangeRate = 50f;

    public Vector3 thrustDirection = Vector3.forward; // 推力方向（本地坐标）

    public Transform cameraTransform;   // 主摄像机

    public Rigidbody rb;

    [Header("推力可视化")]
    public LineRenderer thrustLine;      // 线渲染器组件
    public float maxLineLength = 2f;      // 最大线长度（对应最大推力）
    public Gradient thrustColorGradient;  // 根据推力变化的颜色

    // 子类必须实现：如何启用推进器（输入控制/自动触发）
    public abstract bool ShouldActivate();

    protected virtual void Start()
    {
        if (model == null)
        {
            model = transform.Find("Model");
        }

        // 初始化线渲染器
        if (thrustLine == null)
        {
            thrustLine = GetComponent<LineRenderer>();
            thrustLine.positionCount = 2;
            thrustLine.useWorldSpace = false;
            thrustLine.widthCurve = AnimationCurve.Linear(0, 0.1f, 1, 0.05f);
            thrustLine.material = new Material(Shader.Find("Unlit/Color"));
        }

        // 应用颜色梯度
        if (thrustColorGradient == null)
        {
            thrustColorGradient = new Gradient();
            thrustColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.blue, 0.0f),
                    new GradientColorKey(Color.red, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.7f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
        }
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

    public virtual void VisualizeThrust()
    {
        if (thrustLine == null) return;

        // 计算推力向量（本地空间方向）
        Vector3 thrustVec = thrustDirection.normalized *
                          (thrust / maxThrust) *
                          maxLineLength;

        // 设置线段位置（从推进器中心开始）
        thrustLine.SetPosition(0, Vector3.zero);
        thrustLine.SetPosition(1, thrustVec);

        // 根据推力强度设置颜色
        float thrustRatio = thrust / maxThrust;
        thrustLine.startColor = thrustColorGradient.Evaluate(thrustRatio);
        thrustLine.endColor = thrustColorGradient.Evaluate(thrustRatio);
    }

    public virtual Vector3 GetInputDirection()
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
}
