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
    private ParticleSystem smokeParticles;
    private TrailRenderer emberTrail;
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

        emissionRoot = new GameObject("Detached Part Smoke VFX").transform;
        emissionRoot.SetParent(transform, false);
        emissionRoot.position = worldAnchor;

        smokeParticles = emissionRoot.gameObject.AddComponent<ParticleSystem>();
        smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ConfigureSmoke(smokeParticles);

        GameObject emberObject = new GameObject("Detached Part Ember Trail");
        emberObject.transform.SetParent(emissionRoot, false);
        emberTrail = emberObject.AddComponent<TrailRenderer>();
        ConfigureEmberTrail(emberTrail);
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

    private void ConfigureSmoke(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 2.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.32f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.42f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.24f, 0.27f, 0.3f, 0.72f),
            new Color(0.07f, 0.08f, 0.1f, 0.5f));
        main.maxParticles = 96;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.22f, 0.7f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.28f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.3f, 0.32f, 0.34f), 0f),
                new GradientColorKey(new Color(0.13f, 0.15f, 0.18f), 0.55f),
                new GradientColorKey(new Color(0.04f, 0.05f, 0.065f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.62f, 0.12f),
                new GradientAlphaKey(0.35f, 0.68f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 1.55f)));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = VisualEffectsManager.GetSharedParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 1.5f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void ConfigureEmberTrail(TrailRenderer trail)
    {
        trail.sharedMaterial = VisualEffectsManager.GetSharedLineMaterial();
        trail.time = 0.48f;
        trail.minVertexDistance = 0.08f;
        trail.widthMultiplier = 0.055f;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.35f, 0.42f),
            new Keyframe(1f, 0f));
        trail.colorGradient = CreateEmberGradient();
        trail.numCornerVertices = 2;
        trail.numCapVertices = 2;
        trail.textureMode = LineTextureMode.Stretch;
        trail.alignment = LineAlignment.View;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;
    }

    private void Update()
    {
        if (stopping || targetBody == null)
        {
            StopAndRelease();
            return;
        }

        elapsed += Time.deltaTime;
        float speed = targetBody.linearVelocity.magnitude + targetBody.angularVelocity.magnitude * 0.08f;
        float speedRatio = Mathf.InverseLerp(minimumSpeed, Mathf.Max(minimumSpeed + 0.01f, fullEmissionSpeed), speed);
        float lifetimeFade = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.1f, effectLifetime));
        float emissionRatio = speedRatio * lifetimeFade * intensity;

        ParticleSystem.EmissionModule emission = smokeParticles.emission;
        emission.rateOverTime = maximumEmissionRate * emissionRatio;

        bool shouldEmit = emissionRatio > 0.025f && !targetBody.isKinematic;
        if (shouldEmit && !smokeParticles.isPlaying)
        {
            smokeParticles.Play();
        }
        else if (!shouldEmit && smokeParticles.isPlaying)
        {
            smokeParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        emberTrail.emitting = shouldEmit && speedRatio > 0.35f;
        emberTrail.widthMultiplier = Mathf.Lerp(0.025f, 0.075f, Mathf.Clamp01(emissionRatio));

        if (elapsed >= effectLifetime)
        {
            StopAndRelease();
        }
    }

    private void StopAndRelease()
    {
        if (stopping) return;

        stopping = true;
        if (smokeParticles != null)
        {
            smokeParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        if (emberTrail != null)
        {
            emberTrail.emitting = false;
        }
        if (emissionRoot != null)
        {
            emissionRoot.SetParent(null, true);
            Destroy(emissionRoot.gameObject, 2.4f);
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        if (!registered) return;

        registered = false;
        activeTrailCount = Mathf.Max(0, activeTrailCount - 1);
    }

    private static Gradient CreateEmberGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.58f, 0.12f), 0.28f),
                new GradientColorKey(new Color(0.35f, 0.08f, 0.025f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.68f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }
}
