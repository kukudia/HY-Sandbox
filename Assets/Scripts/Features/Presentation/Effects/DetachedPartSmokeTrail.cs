// 职责：管理断裂零件烟迹和停止后的尾烟生命周期。
// 入口：Attach()。
using UnityEngine;
using UnityEngine.VFX;

[DisallowMultipleComponent]
public class DetachedPartSmokeTrail : MonoBehaviour
{
    [Header("Detached Part Smoke")]
    [Min(0.1f)]
    public float effectLifetime = 6f;
    [Min(0f)]
    public float minimumSpeed = 0.7f;
    [Min(0f)]
    public float fullEmissionSpeed = 8f;
    [Min(0f)]
    public float maximumEmissionRate = 26f;
    [SerializeField, Min(0.1f)]
    private float _initialSmokeDuration = 1.5f;
    [SerializeField, Range(0f, 1f)]
    private float _initialSmokeIntensity = 0.4f;
    [SerializeField, Range(0f, 1f), Tooltip("运动减弱时仍保留烟团对比；最终消散由粒子自身寿命曲线控制。")]
    private float _minimumOpacity = 0.85f;
    [SerializeField, Range(1f, 2f), Tooltip("火焰发射体积相对模型包围盒的扩张，确保粒子能覆盖模型前侧。")]
    private float _flameEnvelopeScale = 1.25f;
    [SerializeField, Range(1f, 4f)]
    private float _flameCoverage = 1.8f;
    private Rigidbody targetBody;
    private Transform emissionRoot;
    private Vector3 localAnchor;
    private VfxEffect _smokeEffect;
    private VisualEffect _envelope;
    private Bounds _modelBounds;
    private float intensity = 1f;
    private float elapsed;
    private bool stopping;
    public static void Attach(Rigidbody body, Vector3 worldAnchor, float effectIntensity)
    {
        if (body == null)
        {
            return;
        }

        DetachedPartSmokeTrail existing = body.GetComponent<DetachedPartSmokeTrail>();
        if (existing != null)
        {
            existing.Refresh(worldAnchor, effectIntensity);
            return;
        }

        DetachedPartSmokeTrail trail = body.gameObject.AddComponent<DetachedPartSmokeTrail>();
        trail.Initialize(body, worldAnchor, effectIntensity);
    }

    private void Initialize(Rigidbody body, Vector3 worldAnchor, float effectIntensity)
    {
        targetBody = body;
        intensity = Mathf.Clamp(effectIntensity, 0.25f, 1.5f);
        elapsed = 0f;
        stopping = false;
        BlockVfxLibrary library = BlockVfxLibrary.Instance;
        if (library == null || library.DetachedSmoke == null)
        {
            Destroy(this);
            return;
        }

        // 烟迹拥有独立生命周期：残骸被禁用/回收时，已发射的世界空间烟粒仍可自然消散。
        _smokeEffect = VfxEffectPool.Rent(library.DetachedSmoke, worldAnchor, Quaternion.identity, body.gameObject.scene);
        emissionRoot = _smokeEffect.transform;
        localAnchor = transform.InverseTransformPoint(worldAnchor);
        ConfigureEnvelope();
        _smokeEffect.SetIntensity(0f);
    }

    private void ConfigureEnvelope()
    {
        bool found = false;
        _modelBounds = new Bounds(localAnchor, Vector3.one * 0.3f);
        foreach (Renderer renderer in targetBody.GetComponentsInChildren<Renderer>())
        {
            // 只统计本刚体的真实模型，排除特效和另一个残骸刚体。
            if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || renderer.GetComponentInParent<Rigidbody>() != targetBody)
            {
                continue;
            }

            BlockBounds.Encapsulate(
                ref _modelBounds,
                ref found,
                BlockBounds.Transform(renderer.localBounds, transform.worldToLocalMatrix * renderer.localToWorldMatrix));
        }

        foreach (VisualEffect graph in _smokeEffect.Graphs)
        {
            if (graph != null && graph.HasFloat("FlameSize"))
            {
                _envelope = graph;
                break;
            }
        }

        UpdateEnvelope();
    }

    private void UpdateEnvelope()
    {
        if (_envelope == null)
        {
            return;
        }

        Vector3 scale = transform.lossyScale;
        Vector3 size = Vector3.Scale(_modelBounds.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        Vector3 extents = size * (0.5f * _flameEnvelopeScale);
        _envelope.transform.SetPositionAndRotation(transform.TransformPoint(_modelBounds.center), transform.rotation);
        _envelope.SetVector3("FlameMin", -extents);
        _envelope.SetVector3("FlameMax", extents);
        _envelope.SetFloat("FlameSize", Mathf.Max(0.25f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * _flameCoverage));
    }

    private void Refresh(Vector3 worldAnchor, float effectIntensity)
    {
        intensity = Mathf.Max(intensity, Mathf.Clamp(effectIntensity, 0.25f, 1.5f));
        elapsed = 0f;
        stopping = false;
        localAnchor = transform.InverseTransformPoint(worldAnchor);
        if (emissionRoot != null)
        {
            emissionRoot.position = worldAnchor;
        }
    }

    private void Update()
    {
        if (stopping || targetBody == null || _smokeEffect == null)
        {
            StopAndRelease();
            return;
        }

        elapsed += Time.deltaTime;
        Vector3 anchor = transform.TransformPoint(localAnchor);
        Vector3 velocity = targetBody.GetPointVelocity(anchor);
        float speed = velocity.magnitude;
        if (emissionRoot != null)
        {
            emissionRoot.position = anchor;
        }

        UpdateEnvelope();
        float speedRatio = Mathf.InverseLerp(minimumSpeed, Mathf.Max(minimumSpeed + 0.01f, fullEmissionSpeed), speed);
        float lifetimeFade = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.1f, effectLifetime));
        // Explosion impulses can be too small to cross the speed gate; show a brief residual plume.
        float initialSmoke = _initialSmokeIntensity * (1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _initialSmokeDuration)));
        float emissionRatio = Mathf.Max(speedRatio * intensity, initialSmoke) * lifetimeFade;
        bool shouldEmit = emissionRatio > 0.025f && !targetBody.isKinematic;
        if (_smokeEffect != null)
        {
            // 速度决定是否发射，不再把低速尾烟的透明度压到几乎不可见。
            float opacity = Mathf.Lerp(_minimumOpacity, 1f, Mathf.Clamp01(emissionRatio * maximumEmissionRate / 26f));
            _smokeEffect.SetIntensity(shouldEmit ? opacity : 0f);
        }

        if (elapsed >= effectLifetime)
        {
            StopAndRelease();
        }
    }

    private void StopAndRelease()
    {
        if (stopping)
        {
            return;
        }

        stopping = true;
        if (_smokeEffect != null)
        {
            _smokeEffect.SetIntensity(0f);
        }

        if (emissionRoot != null)
        {
            _smokeEffect.ReleaseAfterPlayback();
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        StopAndRelease();
    }

    private void OnDisable()
    {
        StopAndRelease();
    }
}
