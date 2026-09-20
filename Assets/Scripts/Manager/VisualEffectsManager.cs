using System.Collections;
using UnityEngine;

public class VisualEffectsManager : MonoBehaviour
{
    private const int MaxMeteorGlowLights = 24;

    private static Material sharedParticleMaterial;
    private static Material sharedGlowParticleMaterial;
    private static Material sharedLineMaterial;
    private static Texture2D softParticleTexture;
    private static int activeMeteorGlowLights;

    public static VisualEffectsManager instance;

    [Header("Runtime VFX")]
    public bool enableRuntimeVfx = true;
    public bool enableSceneLook = true;
    public float cameraShakeStrength = 0.055f;
    public Color buildColor = new Color(0.2f, 0.95f, 1f, 1f);
    public Color removeColor = new Color(1f, 0.34f, 0.08f, 1f);
    public Color selectionColor = new Color(0.38f, 0.95f, 1f, 1f);
    public Color blockedGhostColor = new Color(1f, 0.12f, 0.08f, 1f);
    public Color validGhostColor = new Color(0.24f, 1f, 0.58f, 1f);

    private Coroutine cameraShakeRoutine;
    private Transform shakenCamera;
    private Vector3 activeCameraOffset;

    public static VisualEffectsManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<VisualEffectsManager>();
        if (instance != null)
        {
            return instance;
        }

        GameObject managerObject = new GameObject("Visual Effects Manager");
        instance = managerObject.AddComponent<VisualEffectsManager>();
        return instance;
    }

    public static Material GetSharedParticleMaterial()
    {
        if (sharedParticleMaterial != null)
        {
            return sharedParticleMaterial;
        }

        sharedParticleMaterial = CreateParticleMaterial("Shared Soft Particle VFX Material", false);
        return sharedParticleMaterial;
    }

    public static Material GetSharedGlowParticleMaterial()
    {
        if (sharedGlowParticleMaterial != null)
        {
            return sharedGlowParticleMaterial;
        }

        sharedGlowParticleMaterial = CreateParticleMaterial("Shared Additive Particle VFX Material", true);
        return sharedGlowParticleMaterial;
    }

    public static Material GetSharedLineMaterial()
    {
        if (sharedLineMaterial != null)
        {
            return sharedLineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        sharedLineMaterial = new Material(shader)
        {
            name = "Shared Line VFX Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        SetMaterialColor(sharedLineMaterial, Color.white);
        return sharedLineMaterial;
    }

    public static void TryPlayBlockPlaced(Block block)
    {
        if (block == null) return;
        EnsureInstance().PlayBlockPlaced(block);
    }

    public static void TryPlayBlockRemoved(Block block)
    {
        if (block == null) return;
        EnsureInstance().PlayBlockRemoved(block);
    }

    public static void TryPlayBlockExplosion(Block block)
    {
        if (block == null) return;
        EnsureInstance().PlayBlockExplosion(block);
    }

    public static void TryPlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)
    {
        EnsureInstance().PlayRepairPulse(origin, target, color, width);
    }

    public static void TryAttachDetachedPartSmoke(Rigidbody body, Vector3 worldAnchor, float intensity)
    {
        if (body == null) return;
        VisualEffectsManager manager = EnsureInstance();
        if (!manager.enableRuntimeVfx) return;

        DetachedPartSmokeTrail.Attach(body, worldAnchor, intensity);
    }

    public static void TryPlayObjectDestroyed(GameObject target)
    {
        if (target == null) return;
        EnsureInstance().PlayObjectDestroyed(target);
    }

    public static void TryPlayBlockMoved(Block block, Vector3 from, Vector3 to)
    {
        if (block == null) return;
        EnsureInstance().PlayBlockMoved(block, from, to);
    }

    public static void TryPlayBlockRotated(Block block)
    {
        if (block == null) return;
        EnsureInstance().PlayBlockRotated(block);
    }

    public static void TryShowBlockSelection(Block block)
    {
        if (block == null) return;
        EnsureInstance().ShowBlockSelection(block);
    }

    public static void TryClearBlockSelection(Block block)
    {
        if (instance == null) return;
        instance.ClearBlockSelection(block);
    }

    public static void TryUpdateGhostPreview(GameObject ghost, bool isBlocked)
    {
        if (ghost == null) return;
        EnsureInstance().UpdateGhostPreview(ghost.transform, isBlocked);
    }

    public static void TryClearGhostPreview(GameObject ghost)
    {
        if (instance == null) return;
        instance.ClearGhostPreview();
    }

    public static void TryDecorateMeteor(Meteor meteor)
    {
        if (meteor == null) return;
        EnsureInstance().DecorateMeteor(meteor);
    }

    public static void TryPlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)
    {
        EnsureInstance().PlayMeteorImpact(position, normal, scale, speed);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ApplySceneLook();
    }

    private void OnEnable()
    {
        ApplySceneLook();
    }

    private void ApplySceneLook()
    {
        if (!enableSceneLook) return;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.035f, 0.047f, 0.065f, 1f);
        RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity, 0.0045f);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, new Color(0.17f, 0.2f, 0.25f), 0.45f);
    }

    private void PlayBlockPlaced(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 0.35f, 0.55f, 2.8f);
        Vector3 center = bounds.center;
        Color brightBuild = Brighten(buildColor, 2.2f, 0.18f);

        CreateParticleBurst("Block Place Sparks", center, brightBuild, Brighten(Color.white, 2.8f), 56, scale * 0.24f, 1.8f, 6.2f, 0.2f, 0.78f, 0.04f, 0.15f, -0.12f);
        CreateParticleBurst("Block Place Core Flash", center, Brighten(Color.white, 3.4f), brightBuild, 18, scale * 0.12f, 0.1f, 1.1f, 0.05f, 0.2f, scale * 0.12f, scale * 0.34f, -0.02f);
        CreateRadialStreakBurst(center, Vector3.up, brightBuild, 8, scale * 0.9f, 0.2f, 0.035f);
        CreateLightFlash(center, buildColor, 3.4f, scale * 4.2f, 0.26f);
        ShakeCamera(cameraShakeStrength * 0.45f, 0.12f);
    }

    private void PlayBlockRemoved(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 0.35f, 0.55f, 3.3f);

        Color brightRemove = Brighten(removeColor, 2.35f, 0.15f);
        CreateParticleBurst("Block Break Sparks", bounds.center, brightRemove, Brighten(Color.yellow, 2.4f), 72, scale * 0.28f, 2.6f, 9.5f, 0.22f, 1.05f, 0.05f, 0.19f, 0.2f);
        CreateParticleBurst("Block Dust Glow", bounds.center, new Color(0.55f, 0.72f, 1f, 0.45f), new Color(0.08f, 0.12f, 0.17f, 0.25f), 22, scale * 0.42f, 0.3f, 1.4f, 0.8f, 1.6f, 0.16f, 0.38f, -0.05f, false);
        CreateRadialStreakBurst(bounds.center, Vector3.zero, brightRemove, 10, scale * 1.1f, 0.28f, 0.045f);
        CreateLightFlash(bounds.center, removeColor, 4.2f, scale * 4.8f, 0.34f);
        ShakeCamera(cameraShakeStrength, 0.18f);
    }

    private void PlayBlockExplosion(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 0.42f, 0.7f, 4f);
        Vector3 center = bounds.center;
        Color emberColor = new Color(2.6f, 1.15f, 0.22f, 1f);
        Color smokeColor = new Color(0.18f, 0.22f, 0.28f, 0.75f);

        CreateParticleBurst("Block Explosion Flash", center, Brighten(Color.white, 4.5f), emberColor, 26, scale * 0.15f, 0.18f, 1.8f, 0.06f, 0.26f, scale * 0.2f, scale * 0.52f, -0.04f);
        CreateParticleBurst("Block Explosion Fireball", center, Brighten(Color.white, 3.2f), Brighten(removeColor, 2.5f, 0.12f), 68, scale * 0.4f, 0.9f, 5.6f, 0.14f, 0.82f, scale * 0.1f, scale * 0.4f, -0.06f);
        CreateParticleBurst("Block Explosion Embers", center, Brighten(removeColor, 2.6f, 0.08f), emberColor, 118, scale * 0.32f, 3.2f, 13f, 0.32f, 2.35f, 0.05f, 0.22f, 0.12f);
        CreateParticleBurst("Block Explosion Smoke", center, smokeColor, new Color(0.03f, 0.04f, 0.06f, 0f), 34, scale * 0.35f, 0.35f, 2.1f, 1.0f, 3.4f, 0.14f, 0.42f, -0.08f, false);
        CreateExplosionShrapnel(center, scale, emberColor);
        CreateRadialStreakBurst(center, Vector3.zero, emberColor, 14, scale * 2.1f, 0.34f, 0.055f);
        CreateLightFlash(center, emberColor, 7.5f, scale * 7f, 0.62f);
        ShakeCamera(cameraShakeStrength * 1.35f, 0.42f);
        StartCoroutine(PlayExplosionAftershock(center, scale, emberColor, smokeColor));
    }

    private IEnumerator PlayExplosionAftershock(Vector3 center, float scale, Color emberColor, Color smokeColor)
    {
        yield return new WaitForSecondsRealtime(0.22f);
        if (!enableRuntimeVfx) yield break;

        CreateParticleBurst("Block Explosion Aftershock", center, emberColor, WithAlpha(smokeColor, 0f), 58, scale * 0.56f, 1.1f, 5.2f, 0.4f, 1.45f, 0.04f, 0.15f, 0.02f);
        CreateParticleBurst("Block Explosion Rolling Smoke", center + Vector3.up * scale * 0.2f, smokeColor, WithAlpha(smokeColor, 0f), 24, scale * 0.38f, 0.2f, 1.15f, 1.4f, 3.8f, scale * 0.1f, scale * 0.28f, -0.16f, false);
        CreateRadialStreakBurst(center, Vector3.zero, WithAlpha(emberColor, 0.8f), 8, scale * 1.45f, 0.28f, 0.035f);
        CreateLightFlash(center, emberColor, 2.8f, scale * 4.2f, 0.3f);
    }

    private void PlayObjectDestroyed(GameObject target)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(target, target.transform.position, Vector3.one);
        Color brightRemove = Brighten(removeColor, 2.2f, 0.12f);
        CreateParticleBurst("Object Destroyed Sparks", bounds.center, brightRemove, Brighten(Color.white, 2.4f), 48, 0.45f, 1.8f, 5.6f, 0.5f, 2.0f, 0.045f, 0.16f, 0.15f);
        CreateParticleBurst("Object Destroyed Smoke", bounds.center, new Color(0.24f, 0.28f, 0.34f, 0.6f), new Color(0.05f, 0.06f, 0.08f, 0f), 14, 0.32f, 0.15f, 0.9f, 0.65f, 1.8f, 0.1f, 0.3f, -0.08f, false);
        CreateRadialStreakBurst(bounds.center, Vector3.zero, brightRemove, 8, Mathf.Max(bounds.extents.magnitude, 0.6f), 0.24f, 0.035f);
        CreateLightFlash(bounds.center, removeColor, 2.8f, Mathf.Max(bounds.size.magnitude * 1.35f, 2f), 0.4f);
    }

    private void PlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)
    {
        if (!enableRuntimeVfx) return;

        float scale = Mathf.Clamp(width * 2.4f, 0.22f, 0.7f);
        Color brightRepair = Brighten(color, 2.4f, 0.2f);

        CreateParticleBurst("Repair Impact Sparks", target, Brighten(Color.white, 3f), brightRepair, 28, scale * 0.28f, 0.4f, 2.8f, 0.16f, 0.56f, 0.022f, 0.085f, -0.05f);
        CreateRadialStreakBurst(target, target - origin, brightRepair, 5, scale * 0.75f, 0.16f, Mathf.Max(0.018f, width * 0.12f));
        CreateLineStreak(origin, target, WithAlpha(brightRepair, 0.95f), 0.16f, Mathf.Max(0.02f, width * 0.15f));
        CreateLightFlash(target, color, 1.8f, Mathf.Max(1f, scale * 2.8f), 0.16f);
    }

    private void CreateExplosionShrapnel(Vector3 center, float scale, Color color)
    {
        int streakCount = Mathf.Clamp(Mathf.RoundToInt(8f + scale * 2f), 8, 16);
        for (int i = 0; i < streakCount; i++)
        {
            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y) * 0.65f + 0.1f;
            direction.Normalize();

            Vector3 start = center + direction * Random.Range(scale * 0.04f, scale * 0.18f);
            Vector3 end = start + direction * Random.Range(scale * 0.55f, scale * 1.9f);
            CreateLineStreak(start, end, Color.Lerp(color, Color.white, Random.Range(0.15f, 0.65f)), Random.Range(0.16f, 0.34f), Random.Range(0.018f, 0.052f));
        }
    }

    private void PlayBlockMoved(Block block, Vector3 from, Vector3 to)
    {
        if (!enableRuntimeVfx || (to - from).sqrMagnitude < 0.0025f) return;

        Bounds bounds = GetBounds(block.gameObject, to, GetBlockSize(block));
        Vector3 centerOffset = bounds.center - block.transform.position;
        CreateLineStreak(from + centerOffset, to + centerOffset, buildColor, 0.24f, 0.06f);
        CreateParticleBurst("Block Move Motes", to + centerOffset, buildColor, Color.white, 16, 0.12f, 0.4f, 1.6f, 0.18f, 0.42f, 0.025f, 0.08f, -0.1f);
    }

    private void PlayBlockRotated(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.extents.magnitude, 0.45f, 2.5f);
        Color brightSelection = Brighten(selectionColor, 2.1f, 0.2f);
        CreateParticleBurst("Block Rotate Motes", bounds.center, brightSelection, Brighten(Color.white, 2.2f), 24, scale * 0.32f, 0.35f, 2.2f, 0.12f, 0.42f, 0.025f, 0.085f, -0.04f);
        CreateRadialStreakBurst(bounds.center, block.transform.up, brightSelection, 6, scale * 0.8f, 0.18f, 0.025f);
    }

    private void ShowBlockSelection(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.extents.magnitude, 0.4f, 2.5f);
        Color brightSelection = Brighten(selectionColor, 2.25f, 0.2f);
        CreateParticleBurst("Block Selection Spark", bounds.center, brightSelection, Brighten(Color.white, 2.5f), 18, scale * 0.42f, 0.2f, 1.8f, 0.1f, 0.35f, 0.025f, 0.075f, -0.08f);
        CreateLightFlash(bounds.center, selectionColor, 1.4f, Mathf.Max(1.2f, scale * 2.2f), 0.16f);
    }

    private void ClearBlockSelection(Block block)
    {
        // Selection readability is owned by the block material highlight; no planar helper remains.
    }

    private void UpdateGhostPreview(Transform ghost, bool isBlocked)
    {
        // The ghost's valid/blocked material already provides continuous feedback without floor geometry.
    }

    private void ClearGhostPreview()
    {
    }

    private void DecorateMeteor(Meteor meteor)
    {
        if (!enableRuntimeVfx || meteor == null) return;

        float scale = Mathf.Max(0.35f, meteor.transform.lossyScale.magnitude / 1.732f);
        if (meteor.trailRenderer == null)
        {
            GameObject trailObject = new GameObject("Meteor Trail VFX");
            trailObject.transform.SetParent(meteor.transform, false);
            meteor.trailRenderer = trailObject.AddComponent<TrailRenderer>();
        }

        meteor.trailRenderer.sharedMaterial = GetSharedLineMaterial();
        meteor.trailRenderer.time = Mathf.Clamp(scale * 0.5f, 0.35f, 2.4f);
        meteor.trailRenderer.startWidth = Mathf.Clamp(scale * 0.18f, 0.08f, 0.85f);
        meteor.trailRenderer.endWidth = 0f;
        meteor.trailRenderer.minVertexDistance = 0.12f;
        meteor.trailRenderer.numCornerVertices = 3;
        meteor.trailRenderer.colorGradient = MakeGradient(new Color(1f, 0.88f, 0.46f, 0.95f), new Color(0.2f, 0.55f, 1f, 0f));
        meteor.trailRenderer.emitting = true;
        meteor.trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (meteor.glowLight == null && activeMeteorGlowLights < MaxMeteorGlowLights)
        {
            GameObject lightObject = new GameObject("Meteor Glow VFX");
            lightObject.transform.SetParent(meteor.transform, false);
            meteor.glowLight = lightObject.AddComponent<Light>();
            lightObject.AddComponent<MeteorLightToken>();
            activeMeteorGlowLights++;
        }

        if (meteor.glowLight != null)
        {
            meteor.glowLight.type = LightType.Point;
            meteor.glowLight.color = new Color(1f, 0.62f, 0.28f, 1f);
            meteor.glowLight.intensity = Mathf.Clamp(scale * 2.1f, meteor.minGlowIntensity, meteor.maxGlowIntensity * 1.45f);
            meteor.glowLight.range = Mathf.Clamp(scale * 5f, 2.5f, 22f);
            meteor.glowLight.shadows = LightShadows.None;
        }
    }

    private void PlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)
    {
        if (!enableRuntimeVfx) return;

        float impactScale = Mathf.Clamp(scale * 0.65f + speed * 0.025f, 0.6f, 5f);
        Vector3 impactNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;

        Color meteorColor = new Color(2.8f, 1.1f, 0.2f, 1f);
        CreateParticleBurst("Meteor Impact Flash", position + impactNormal * 0.08f, Brighten(Color.white, 4f), meteorColor, 22, impactScale * 0.12f, 0.15f, 1.6f, 0.05f, 0.24f, impactScale * 0.12f, impactScale * 0.34f, -0.02f);
        CreateParticleBurst("Meteor Impact Sparks", position + impactNormal * 0.08f, meteorColor, Brighten(Color.yellow, 2.8f), 92, impactScale * 0.18f, 3.5f, 14f, 0.2f, 0.85f, 0.05f, 0.22f, 0.1f);
        CreateParticleBurst("Meteor Impact Smoke", position + impactNormal * 0.18f, new Color(0.5f, 0.58f, 0.68f, 0.45f), new Color(0.05f, 0.065f, 0.08f, 0.15f), 32, impactScale * 0.32f, 0.4f, 1.8f, 0.8f, 1.8f, 0.28f, 0.75f, -0.08f, false);
        CreateRadialStreakBurst(position + impactNormal * 0.05f, impactNormal, meteorColor, 14, impactScale * 1.8f, 0.3f, 0.05f);
        CreateLightFlash(position, meteorColor, impactScale * 3.8f, impactScale * 5.5f, 0.32f);
        ShakeCamera(cameraShakeStrength * Mathf.Clamp(impactScale, 1f, 3f), 0.2f);
    }

    private void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)
    {
        GameObject streak = new GameObject("Energy Streak VFX");
        StylizedBeamEffect beam = streak.AddComponent<StylizedBeamEffect>();
        beam.Configure(width, 5.2f, 10, 0.018f, 3.2f, 18f);
        beam.SetEndpoints(from, to);
        beam.SetColor(color);
        beam.SetVisible(true);
        streak.AddComponent<BeamFade>().Initialize(beam, color, Mathf.Max(0.05f, duration));
    }

    private void CreateRadialStreakBurst(
        Vector3 center,
        Vector3 normal,
        Color color,
        int count,
        float length,
        float duration,
        float width)
    {
        Vector3 hemisphereNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Vector3 direction = Random.onUnitSphere;
            if (hemisphereNormal != Vector3.zero && Vector3.Dot(direction, hemisphereNormal) < 0f)
            {
                direction = -direction;
            }

            Vector3 start = center + direction * Random.Range(0.02f, length * 0.12f);
            Vector3 end = start + direction * Random.Range(length * 0.45f, length);
            CreateLineStreak(
                start,
                end,
                Color.Lerp(color, Brighten(Color.white, 2.4f), Random.Range(0.05f, 0.35f)),
                Random.Range(duration * 0.7f, duration * 1.15f),
                Random.Range(width * 0.65f, width * 1.25f));
        }
    }

    private void CreateParticleBurst(
        string name,
        Vector3 position,
        Color startColor,
        Color endColor,
        int count,
        float radius,
        float minSpeed,
        float maxSpeed,
        float minLifetime,
        float maxLifetime,
        float minSize,
        float maxSize,
        float gravity,
        bool additive = true)
    {
        GameObject burstObject = new GameObject(name);
        burstObject.transform.position = position;

        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = Mathf.Max(0.05f, maxLifetime);
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(startColor, endColor);
        main.gravityModifier = gravity;
        main.maxParticles = Mathf.Max(count + 8, 32);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.01f, radius);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(MakeGradient(startColor, WithAlpha(endColor, 0f)));

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = additive ? GetSharedGlowParticleMaterial() : GetSharedParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 4f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        particles.Emit(Mathf.Max(1, count));
        Destroy(burstObject, maxLifetime + 0.6f);
    }

    private void CreateLightFlash(Vector3 position, Color color, float intensity, float range, float duration)
    {
        GameObject lightObject = new GameObject("Light Flash VFX");
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = Mathf.Max(0.5f, range);
        light.shadows = LightShadows.None;
        lightObject.AddComponent<LightFade>().Initialize(light, Mathf.Max(0.05f, duration), intensity);
    }

    private void ShakeCamera(float amplitude, float duration)
    {
        if (amplitude <= 0f || duration <= 0f) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        if (cameraShakeRoutine != null)
        {
            StopCoroutine(cameraShakeRoutine);
            ClearCameraOffset();
        }

        shakenCamera = camera.transform;
        cameraShakeRoutine = StartCoroutine(CameraShakeRoutine(amplitude, duration));
    }

    private IEnumerator CameraShakeRoutine(float amplitude, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && shakenCamera != null)
        {
            ClearCameraOffset();

            float falloff = 1f - elapsed / duration;
            activeCameraOffset = Random.insideUnitSphere * (amplitude * falloff);
            activeCameraOffset.z *= 0.35f;
            shakenCamera.localPosition += activeCameraOffset;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ClearCameraOffset();
        cameraShakeRoutine = null;
        shakenCamera = null;
    }

    private void ClearCameraOffset()
    {
        if (shakenCamera != null)
        {
            shakenCamera.localPosition -= activeCameraOffset;
        }

        activeCameraOffset = Vector3.zero;
    }

    private static Bounds GetBounds(GameObject target, Vector3 fallbackCenter, Vector3 fallbackSize)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds bounds = new Bounds(fallbackCenter, fallbackSize);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private static Vector3 GetBlockSize(Block block)
    {
        return new Vector3(Mathf.Max(1, block.x), Mathf.Max(1, block.y), Mathf.Max(1, block.z));
    }

    private static Gradient MakeGradient(Color start, Color end)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(Color.Lerp(start, end, 0.65f), 0.55f),
                new GradientColorKey(end, 1f)
            },
            new[]
            {
                new GradientAlphaKey(start.a, 0f),
                new GradientAlphaKey(Mathf.Lerp(start.a, end.a, 0.45f), 0.65f),
                new GradientAlphaKey(end.a, 1f)
            }
        );

        return gradient;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    private static Color Brighten(Color color, float intensity, float whiteBlend = 0f)
    {
        Color bright = Color.Lerp(color, Color.white, Mathf.Clamp01(whiteBlend));
        bright.r *= intensity;
        bright.g *= intensity;
        bright.b *= intensity;
        bright.a = color.a;
        return bright;
    }

    private static Material CreateParticleMaterial(string materialName, bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = materialName,
            hideFlags = HideFlags.HideAndDontSave
        };

        SetMaterialTexture(material, GetSoftParticleTexture());
        SetMaterialColor(material, Color.white);
        if (additive)
        {
            ConfigureAdditiveMaterial(material);
        }
        else
        {
            ConfigureTransparentMaterial(material);
        }

        return material;
    }

    private static Texture2D GetSoftParticleTexture()
    {
        if (softParticleTexture != null)
        {
            return softParticleTexture;
        }

        const int size = 64;
        softParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Soft Particle Texture",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(u * u + v * v);
                float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(distance));
                softParticleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        softParticleTexture.Apply(false, true);
        return softParticleTexture;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null) return;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void SetMaterialTexture(Material material, Texture texture)
    {
        if (material == null || texture == null) return;

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material == null) return;

        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void ConfigureAdditiveMaterial(Material material)
    {
        if (material == null) return;

        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 2f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private sealed class BeamFade : MonoBehaviour
    {
        private StylizedBeamEffect beam;
        private Color color;
        private float duration;
        private float elapsed;

        public void Initialize(StylizedBeamEffect effect, Color lineColor, float lifetime)
        {
            beam = effect;
            color = lineColor;
            duration = lifetime;
        }

        private void Update()
        {
            if (beam == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            beam.SetIntensity(1f - t);

            if (t >= 1f)
            {
                beam.SetVisible(false);
                Destroy(gameObject);
            }
        }
    }

    private sealed class LightFade : MonoBehaviour
    {
        private Light targetLight;
        private float startIntensity;
        private float duration;
        private float elapsed;

        public void Initialize(Light light, float lifetime, float intensity)
        {
            targetLight = light;
            duration = lifetime;
            startIntensity = intensity;
        }

        private void Update()
        {
            if (targetLight == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            targetLight.intensity = Mathf.Lerp(startIntensity, 0f, t * t);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    private sealed class MeteorLightToken : MonoBehaviour
    {
        private void OnDestroy()
        {
            activeMeteorGlowLights = Mathf.Max(0, activeMeteorGlowLights - 1);
        }
    }
}
