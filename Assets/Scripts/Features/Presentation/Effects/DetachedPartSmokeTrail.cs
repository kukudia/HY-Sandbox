// 职责：管理断裂零件烟迹和停止后的尾烟生命周期。
// 入口：Attach()。
using UnityEngine;

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
    private Rigidbody targetBody;
    private Transform emissionRoot;
    private Vector3 localAnchor;
    private VfxEffect _smokeEffect;
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
            if (existing.enabled && existing.targetBody != null && !existing.stopping)
            {
                existing.Refresh(worldAnchor, effectIntensity);
            }
            else
            {
                existing.Initialize(body, worldAnchor, effectIntensity);
            }

            return;
        }

        throw new MissingComponentException("Rigidbody prefab requires an authored DetachedPartSmokeTrail.");
    }

    private void Initialize(Rigidbody body, Vector3 worldAnchor, float effectIntensity)
    {
        enabled = true;
        targetBody = body;
        intensity = Mathf.Clamp(effectIntensity, 0.25f, 1.5f);
        elapsed = 0f;
        stopping = false;
        BlockVfxLibrary library = BlockVfxLibrary.Instance;
        if (library == null || library.DetachedSmoke == null)
        {
            enabled = false;
            return;
        }

        // 烟迹拥有独立生命周期：残骸被禁用/回收时，已发射的世界空间烟粒仍可自然消散。
        _smokeEffect = VfxEffectPool.Rent(library.DetachedSmoke, worldAnchor, Quaternion.identity, body.gameObject.scene);
        if (_smokeEffect == null)
        {
            enabled = false;
            return;
        }

        emissionRoot = _smokeEffect.transform;
        localAnchor = transform.InverseTransformPoint(worldAnchor);
        _smokeEffect.DisableFlameEnvelope();
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

        emissionRoot = null;
        _smokeEffect = null;
        enabled = false;
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
