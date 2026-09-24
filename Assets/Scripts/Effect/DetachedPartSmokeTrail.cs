using UnityEngine;

[DisallowMultipleComponent]
public class DetachedPartSmokeTrail : MonoBehaviour
{
    private const int MaxActiveTrails = 24;

    [Header("Detached Part Smoke")]
    [Min(0.1f)] public float effectLifetime = 6f;
    [Min(0f)] public float minimumSpeed = 0.7f;
    [Min(0f)] public float fullEmissionSpeed = 8f;
    [Min(0f)] public float maximumEmissionRate = 26f;

    private static int activeTrailCount;

    private Rigidbody targetBody;
    private Transform emissionRoot;
    private VfxEffect _smokeEffect;
    private float intensity = 1f;
    private float elapsed;
    private bool registered;
    private bool stopping;

    public static void Attach(Rigidbody body, Vector3 worldAnchor, float effectIntensity)
    {
        if (body == null) return;

        DetachedPartSmokeTrail existing = body.GetComponent<DetachedPartSmokeTrail>();
        if (existing != null)
        {
            existing.Refresh(worldAnchor, effectIntensity);
            return;
        }

        if (activeTrailCount >= MaxActiveTrails)
        {
            return;
        }

        DetachedPartSmokeTrail trail = body.gameObject.AddComponent<DetachedPartSmokeTrail>();
        trail.registered = true;
        activeTrailCount++;
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
        _smokeEffect = Instantiate(library.DetachedSmoke, transform);
        emissionRoot = _smokeEffect.transform;
        emissionRoot.position = worldAnchor;
        _smokeEffect.SetIntensity(0f);


    }

    private void Refresh(Vector3 worldAnchor, float effectIntensity)
    {
        intensity = Mathf.Max(intensity, Mathf.Clamp(effectIntensity, 0.25f, 1.5f));
        elapsed = Mathf.Min(elapsed, effectLifetime * 0.35f);
        stopping = false;

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
        float speed = targetBody.linearVelocity.magnitude + targetBody.angularVelocity.magnitude * 0.08f;
        float speedRatio = Mathf.InverseLerp(minimumSpeed, Mathf.Max(minimumSpeed + 0.01f, fullEmissionSpeed), speed);
        float lifetimeFade = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.1f, effectLifetime));
        float emissionRatio = speedRatio * lifetimeFade * intensity;

        bool shouldEmit = emissionRatio > 0.025f && !targetBody.isKinematic;
        if (_smokeEffect != null) _smokeEffect.SetIntensity(shouldEmit ? emissionRatio * maximumEmissionRate / 26f : 0f);



        if (elapsed >= effectLifetime)
        {
            StopAndRelease();
        }
    }

    private void StopAndRelease()
    {
        if (stopping) return;

        stopping = true;
        if (_smokeEffect != null) _smokeEffect.SetIntensity(0f);

        if (emissionRoot != null)
        {
            emissionRoot.SetParent(null, true);
            _smokeEffect.ReleaseAfterPlayback();
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        if (!registered) return;

        registered = false;
        activeTrailCount = Mathf.Max(0, activeTrailCount - 1);
    }

}
