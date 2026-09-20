using UnityEngine;

[DisallowMultipleComponent]
public class ThrusterVisualEffect : MonoBehaviour
{
    [Header("Plume Palette")]
    public Color coolPlumeColor = new Color(0.22f, 0.75f, 1f, 0.9f);
    public Color hotPlumeColor = new Color(1f, 0.5f, 0.12f, 0.95f);
    public Color sparkColor = new Color(1f, 0.82f, 0.34f, 1f);

    [Header("Plume Shape")]
    public float maxEmissionRate = 130f;
    public float maxCoreEmissionRate = 90f;
    public float maxSparkEmissionRate = 18f;
    public float maxLightIntensity = 2.8f;
    public float plumeRadius = 0.13f;
    public float plumeLength = 0.62f;
    public float responseSpeed = 8f;

    private Thruster thruster;
    private Transform plumeRoot;
    private ParticleSystem plumeParticles;
    private ParticleSystem coreParticles;
    private ParticleSystem sparkParticles;
    private Light plumeLight;
    private float smoothedThrust;
    private float flickerSeed;

    public void Initialize(Thruster owner)
    {
        thruster = owner;
        EnsureVfx();
    }

    public void SetThrust(float thrustRatio, Vector3 localThrustDirection)
    {
        EnsureVfx();

        float target = Mathf.Clamp01(thrustRatio);
        smoothedThrust = Mathf.MoveTowards(smoothedThrust, target, responseSpeed * Time.deltaTime);

        Vector3 direction = localThrustDirection.sqrMagnitude > 0.001f
            ? localThrustDirection.normalized
            : Vector3.forward;
        plumeRoot.localRotation = Quaternion.LookRotation(-direction, GetStableUp(direction));

        UpdateParticleModules(smoothedThrust);
        UpdateGlow(smoothedThrust);
        SetEmissionState(smoothedThrust > 0.015f);
    }

    private void EnsureVfx()
    {
        if (plumeParticles != null) return;

        if (thruster == null)
        {
            thruster = GetComponent<Thruster>();
        }

        flickerSeed = Random.Range(0f, 100f);
        plumeRoot = new GameObject("Thruster Plume VFX").transform;
        plumeRoot.SetParent(transform, false);

        plumeParticles = CreateParticleLayer("Outer Plume");
        ConfigureOuterPlume(plumeParticles);

        coreParticles = CreateParticleLayer("Hot Core");
        ConfigureHotCore(coreParticles);

        sparkParticles = CreateParticleLayer("Plume Sparks");
        ConfigureSparks(sparkParticles);

        GameObject lightObject = new GameObject("Thruster Glow VFX");
        lightObject.transform.SetParent(plumeRoot, false);
        lightObject.transform.localPosition = Vector3.forward * 0.08f;
        plumeLight = lightObject.AddComponent<Light>();
        plumeLight.type = LightType.Point;
        plumeLight.color = coolPlumeColor;
        plumeLight.range = 2.2f;
        plumeLight.intensity = 0f;
        plumeLight.shadows = LightShadows.None;
    }

    private ParticleSystem CreateParticleLayer(string layerName)
    {
        GameObject layer = new GameObject(layerName);
        layer.transform.SetParent(plumeRoot, false);
        ParticleSystem particles = layer.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private void ConfigureOuterPlume(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(coolPlumeColor, hotPlumeColor);
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 16f;
        shape.radius = plumeRadius;
        shape.length = plumeLength;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreatePlumeGradient());

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.18f, 1f),
                new Keyframe(0.72f, 0.62f),
                new Keyframe(1f, 0f)));

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 1.8f;
        noise.scrollSpeed = 0.8f;

        ConfigureRenderer(particles, ParticleSystemRenderMode.Stretch, 1.9f, 0.14f, 2f);
    }

    private void ConfigureHotCore(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.055f, 0.14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 8.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, coolPlumeColor);
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = plumeRadius * 0.38f;
        shape.length = plumeLength * 0.22f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.Lerp(coolPlumeColor, Color.white, 0.5f), 0.38f),
                new GradientColorKey(hotPlumeColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.95f, 0.42f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.55f, 0.72f),
                new Keyframe(1f, 0f)));

        ConfigureRenderer(particles, ParticleSystemRenderMode.Stretch, 2.8f, 0.08f, 3f);
    }

    private void ConfigureSparks(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 6.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.045f);
        main.startColor = new ParticleSystem.MinMaxGradient(sparkColor, Color.white);
        main.gravityModifier = 0.04f;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 24f;
        shape.radius = plumeRadius * 0.7f;
        shape.length = plumeLength * 0.2f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(sparkColor, 0.25f),
                new GradientColorKey(hotPlumeColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.82f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ConfigureRenderer(particles, ParticleSystemRenderMode.Stretch, 3.5f, 0.18f, 4f);
    }

    private static void ConfigureRenderer(
        ParticleSystem particles,
        ParticleSystemRenderMode renderMode,
        float lengthScale,
        float velocityScale,
        float sortingFudge)
    {
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = VisualEffectsManager.GetSharedParticleMaterial();
        renderer.renderMode = renderMode;
        renderer.lengthScale = lengthScale;
        renderer.velocityScale = velocityScale;
        renderer.sortingFudge = sortingFudge;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private Gradient CreatePlumeGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(coolPlumeColor, 0.22f),
                new GradientColorKey(hotPlumeColor, 0.72f),
                new GradientColorKey(new Color(0.2f, 0.12f, 0.1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.78f, 0.35f),
                new GradientAlphaKey(0.34f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private void UpdateParticleModules(float ratio)
    {
        float flicker = Mathf.Lerp(0.9f, 1.08f, Mathf.PerlinNoise(flickerSeed, Time.unscaledTime * 18f));
        float drivenRatio = Mathf.Clamp01(ratio * flicker);

        ParticleSystem.MainModule plumeMain = plumeParticles.main;
        plumeMain.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.08f, 0.18f, drivenRatio),
            Mathf.Lerp(0.16f, 0.46f, drivenRatio));
        plumeMain.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(1f, 3.2f, drivenRatio),
            Mathf.Lerp(2.8f, 8.8f, drivenRatio));
        plumeMain.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.025f, 0.07f, drivenRatio),
            Mathf.Lerp(0.08f, 0.23f, drivenRatio));
        plumeMain.startColor = new ParticleSystem.MinMaxGradient(
            Color.Lerp(coolPlumeColor, Color.white, drivenRatio * 0.25f),
            Color.Lerp(coolPlumeColor, hotPlumeColor, drivenRatio));

        ParticleSystem.EmissionModule plumeEmission = plumeParticles.emission;
        plumeEmission.rateOverTime = Mathf.Lerp(0f, maxEmissionRate, drivenRatio);

        ParticleSystem.ShapeModule plumeShape = plumeParticles.shape;
        plumeShape.radius = Mathf.Lerp(plumeRadius * 0.35f, plumeRadius, drivenRatio);
        plumeShape.angle = Mathf.Lerp(7f, 19f, drivenRatio);
        plumeShape.length = Mathf.Lerp(plumeLength * 0.35f, plumeLength, drivenRatio);

        ParticleSystem.MainModule coreMain = coreParticles.main;
        coreMain.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.045f, 0.09f, drivenRatio),
            Mathf.Lerp(0.09f, 0.2f, drivenRatio));
        coreMain.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(2.5f, 5.2f, drivenRatio),
            Mathf.Lerp(4.5f, 11.5f, drivenRatio));
        coreMain.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.018f, 0.038f, drivenRatio),
            Mathf.Lerp(0.045f, 0.1f, drivenRatio));

        ParticleSystem.EmissionModule coreEmission = coreParticles.emission;
        coreEmission.rateOverTime = Mathf.Lerp(0f, maxCoreEmissionRate, drivenRatio);

        ParticleSystem.ShapeModule coreShape = coreParticles.shape;
        coreShape.radius = Mathf.Lerp(plumeRadius * 0.16f, plumeRadius * 0.42f, drivenRatio);
        coreShape.length = Mathf.Lerp(plumeLength * 0.08f, plumeLength * 0.28f, drivenRatio);

        ParticleSystem.EmissionModule sparkEmission = sparkParticles.emission;
        sparkEmission.rateOverTime = drivenRatio < 0.3f
            ? 0f
            : Mathf.Lerp(0f, maxSparkEmissionRate, Mathf.InverseLerp(0.3f, 1f, drivenRatio));

        plumeRoot.localScale = Vector3.one * Mathf.Lerp(0.72f, 1.38f, drivenRatio);
    }

    private void UpdateGlow(float ratio)
    {
        if (plumeLight == null) return;

        float flicker = Mathf.Lerp(0.88f, 1.12f, Mathf.PerlinNoise(flickerSeed + 11f, Time.unscaledTime * 20f));
        plumeLight.color = Color.Lerp(coolPlumeColor, hotPlumeColor, Mathf.Clamp01(ratio * 0.58f));
        plumeLight.intensity = Mathf.Lerp(0f, maxLightIntensity, ratio) * flicker;
        plumeLight.range = Mathf.Lerp(0.6f, 3.6f, ratio);
    }

    private void SetEmissionState(bool active)
    {
        SetParticleState(plumeParticles, active);
        SetParticleState(coreParticles, active);
        SetParticleState(sparkParticles, active);
    }

    private static void SetParticleState(ParticleSystem particles, bool active)
    {
        if (particles == null) return;

        if (active && !particles.isPlaying)
        {
            particles.Play();
        }
        else if (!active && particles.isPlaying)
        {
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private static Vector3 GetStableUp(Vector3 direction)
    {
        return Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.92f
            ? Vector3.forward
            : Vector3.up;
    }
}
