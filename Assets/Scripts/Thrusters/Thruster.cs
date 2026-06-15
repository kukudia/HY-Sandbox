using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public abstract class Thruster : MonoBehaviour
{
    protected const float InputEpsilonSqr = 1e-6f;
    private const float DirectionEpsilonSqr = 1e-6f;

    private static Material sharedLineMaterial;

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

    [Tooltip("推力可视化刷新间隔（秒），降低 LineRenderer 在物理帧中的更新频率")]
    public float visualizationInterval = 0.05f;

    private float nextVisualizationTime;
    private bool modelLookupComplete;
    private readonly Vector3[] linePositions = new Vector3[2];
    private ThrusterVisualEffect visualEffect;

    // 子类必须实现：如何启用推进器（输入控制/自动触发）
    public abstract bool ShouldActivate();

    protected virtual void Awake()
    {
        CacheLocalReferences();
    }

    protected virtual void Start()
    {
        CacheLocalReferences();
        EnsureLineRenderer();
        EnsureThrustGradient();
        EnsureVisualEffect();
        VisualizeThrust(true);
    }

    private void OnTransformChildrenChanged()
    {
        modelLookupComplete = false;
    }

    public void SetRuntimeReferences(ControlUnit owner, Rigidbody ownerRigidbody)
    {
        controlUnit = owner;
        rb = ownerRigidbody;
        CacheLocalReferences();
    }

    protected bool RefreshRuntimeReferences()
    {
        CacheLocalReferences();

        if (controlUnit == null)
        {
            controlUnit = GetComponentInParent<ControlUnit>();
        }

        TryEnsureRigidbody();
        return controlUnit != null && rb != null;
    }

    protected bool TryEnsureRigidbody()
    {
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        return rb != null;
    }

    protected bool IsPlayModeActive()
    {
        return PlayManager.instance != null && PlayManager.instance.playMode;
    }

    protected bool HasValidRuntimeOwner()
    {
        return controlUnit != null && controlUnit.HasValidCockpit;
    }

    protected void CacheLocalReferences()
    {
        if (model == null && !modelLookupComplete)
        {
            model = transform.Find("Model");
            modelLookupComplete = true;
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
        VisualizeThrust(false);
    }

    public virtual void VisualizeThrust(bool forceUpdate)
    {
        EnsureLineRenderer();
        EnsureThrustGradient();

        if (thrustLine == null) return;

        Vector3 lineDirection = GetNormalizedThrustDirection();
        float currentTime = Time.unscaledTime;

        if (!forceUpdate && visualizationInterval > 0f && currentTime < nextVisualizationTime)
        {
            return;
        }

        nextVisualizationTime = currentTime + Mathf.Max(0f, visualizationInterval);

        // 计算推力向量（本地空间方向）
        float thrustRatio = maxThrust > 1e-5f ? Mathf.Clamp01(thrust / maxThrust) : 0f;
        Vector3 thrustVec = -lineDirection * thrustRatio * maxLineLength;

        // 设置线段位置（从推进器中心开始）
        linePositions[0] = Vector3.zero;
        linePositions[1] = thrustVec;
        thrustLine.SetPositions(linePositions);

        // 根据推力强度设置颜色
        Color thrustColor = thrustColorGradient.Evaluate(thrustRatio);
        thrustLine.startColor = thrustColor;
        thrustLine.endColor = thrustColor;

        EnsureVisualEffect();
        visualEffect.SetThrust(thrustRatio, lineDirection);
    }

    public virtual Vector3 GetInputDirection()
    {
        Vector3 dir = Vector3.zero;

        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return dir;

            cameraTransform = mainCamera.transform;
        }

        // 摄像机 forward / right 的水平分量
        Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return dir;

        if (keyboard.wKey.isPressed) dir += camFwd;
        if (keyboard.sKey.isPressed) dir -= camFwd;
        if (keyboard.aKey.isPressed) dir -= camRight;
        if (keyboard.dKey.isPressed) dir += camRight;

        if (dir.sqrMagnitude > InputEpsilonSqr)
            dir.Normalize();

        return dir;
    }

    private void EnsureLineRenderer()
    {
        if (thrustLine == null)
        {
            thrustLine = GetComponent<LineRenderer>();
        }

        if (thrustLine == null)
        {
            return;
        }

        if (thrustLine.positionCount != 2)
        {
            thrustLine.positionCount = 2;
        }

        thrustLine.useWorldSpace = false;

        if (thrustLine.widthCurve == null || thrustLine.widthCurve.length == 0)
        {
            thrustLine.widthCurve = AnimationCurve.Linear(0, 0.1f, 1, 0.05f);
        }

        if (thrustLine.sharedMaterial == null)
        {
            thrustLine.sharedMaterial = GetSharedLineMaterial();
        }
    }

    private static Material GetSharedLineMaterial()
    {
        if (sharedLineMaterial != null)
        {
            return sharedLineMaterial;
        }

        Shader shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            sharedLineMaterial = new Material(shader)
            {
                name = "Shared Thruster Line Material"
            };
        }

        return sharedLineMaterial;
    }

    private void EnsureThrustGradient()
    {
        if (thrustColorGradient != null)
        {
            return;
        }

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

    private void EnsureVisualEffect()
    {
        if (visualEffect == null)
        {
            visualEffect = GetComponent<ThrusterVisualEffect>();
        }

        if (visualEffect == null)
        {
            visualEffect = gameObject.AddComponent<ThrusterVisualEffect>();
        }

        visualEffect.Initialize(this);
    }

    private Vector3 GetNormalizedThrustDirection()
    {
        Vector3 direction = thrustDirection;
        float sqrMagnitude = direction.sqrMagnitude;

        if (sqrMagnitude <= DirectionEpsilonSqr)
        {
            return Vector3.forward;
        }

        if (Mathf.Abs(sqrMagnitude - 1f) <= 0.001f)
        {
            return direction;
        }

        return direction / Mathf.Sqrt(sqrMagnitude);
    }
}
