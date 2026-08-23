using System.Collections;
using UnityEngine;

public class VisualEffectsManager : MonoBehaviour
{
    private const int RingSegments = 64;
    private const int MaxMeteorGlowLights = 24;

    private static Material sharedParticleMaterial;
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

    private Block selectedBlock;
    private GameObject selectionRing;
    private StylizedRingEffect selectionRingEffect;

    private Transform ghostTarget;
    private bool ghostBlocked;
    private GameObject ghostRing;
    private StylizedRingEffect ghostRingEffect;

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

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        sharedParticleMaterial = new Material(shader)
        {
            name = "Shared Soft Particle VFX Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        Texture2D texture = GetSoftParticleTexture();
        SetMaterialTexture(sharedParticleMaterial, texture);
        SetMaterialColor(sharedParticleMaterial, Color.white);
        return sharedParticleMaterial;
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
        if (ghost == null || instance.ghostTarget == ghost.transform)
        {
            instance.ClearGhostPreview();
        }
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

    private void Update()
    {
        UpdateSelectionRing();
        UpdateGhostRing();
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
        Vector3 basePosition = new Vector3(center.x, bounds.min.y + 0.04f, center.z);

        CreateParticleBurst("Block Place Sparks", center, buildColor, Color.white, 42, scale * 0.2f, 1.3f, 4.8f, 0.2f, 0.7f, 0.035f, 0.12f, -0.15f);
        CreateTransientRing("Block Place Ring", basePosition, Vector3.up, buildColor, Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.45f, 0.42f, 0.065f);
        CreateLightFlash(center, buildColor, 1.6f, scale * 3.2f, 0.2f);
        ShakeCamera(cameraShakeStrength * 0.45f, 0.12f);
    }

    private void PlayBlockRemoved(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 0.35f, 0.55f, 3.3f);

        CreateParticleBurst("Block Break Sparks", bounds.center, removeColor, Color.yellow, 58, scale * 0.24f, 2.2f, 7.5f, 0.22f, 0.95f, 0.045f, 0.16f, 0.2f);
        CreateParticleBurst("Block Dust Glow", bounds.center, new Color(0.55f, 0.72f, 1f, 0.45f), new Color(0.08f, 0.12f, 0.17f, 0.25f), 22, scale * 0.42f, 0.3f, 1.4f, 0.8f, 1.6f, 0.16f, 0.38f, -0.05f);
        CreateTransientRing("Block Break Ring", bounds.center, Vector3.up, removeColor, scale * 1.35f, 0.55f, 0.09f);
        CreateLightFlash(bounds.center, removeColor, 2.4f, scale * 4.2f, 0.28f);
        ShakeCamera(cameraShakeStrength, 0.18f);
    }

    private void PlayBlockExplosion(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 0.42f, 0.7f, 4f);
        Vector3 center = bounds.center;
        Color emberColor = new Color(1f, 0.62f, 0.12f, 1f);
        Color smokeColor = new Color(0.18f, 0.22f, 0.28f, 0.75f);

        CreateParticleBurst("Block Explosion Embers", center, removeColor, emberColor, 96, scale * 0.28f, 2.5f, 10f, 0.3f, 2.1f, 0.045f, 0.18f, 0.12f);
        CreateParticleBurst("Block Explosion Smoke", center, smokeColor, new Color(0.03f, 0.04f, 0.06f, 0f), 34, scale * 0.35f, 0.35f, 2.1f, 1.0f, 3.4f, 0.14f, 0.42f, -0.08f);
        CreateTransientRing("Block Explosion Ring", center, Vector3.up, removeColor, scale * 1.65f, 1.15f, 0.12f);
        CreateTransientRing("Block Explosion Inner Ring", center, Vector3.up, emberColor, scale * 0.9f, 0.72f, 0.075f);
        CreateLightFlash(center, emberColor, 4.2f, scale * 5.8f, 0.55f);
        ShakeCamera(cameraShakeStrength * 1.35f, 0.42f);
        StartCoroutine(PlayExplosionAftershock(center, scale, emberColor, smokeColor));
    }

    private IEnumerator PlayExplosionAftershock(Vector3 center, float scale, Color emberColor, Color smokeColor)
    {
        yield return new WaitForSecondsRealtime(0.22f);
        if (!enableRuntimeVfx) yield break;

        CreateParticleBurst("Block Explosion Aftershock", center, emberColor, WithAlpha(smokeColor, 0f), 42, scale * 0.5f, 0.8f, 3.8f, 0.4f, 1.35f, 0.035f, 0.12f, 0.02f);
        CreateTransientRing("Block Explosion Aftershock Ring", center, Vector3.up, WithAlpha(emberColor, 0.7f), scale * 1.25f, 0.85f, 0.055f);
    }

    private void PlayObjectDestroyed(GameObject target)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(target, target.transform.position, Vector3.one);
        CreateParticleBurst("Object Destroyed Sparks", bounds.center, removeColor, Color.white, 36, 0.4f, 1.4f, 4.2f, 0.5f, 2.0f, 0.04f, 0.13f, 0.15f);
        CreateLightFlash(bounds.center, removeColor, 1.3f, Mathf.Max(bounds.size.magnitude, 1.5f), 0.36f);
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
        CreateTransientRing("Block Rotate Ring", bounds.center, block.transform.up, selectionColor, Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.35f, 0.3f, 0.045f);
    }

    private void ShowBlockSelection(Block block)
    {
        if (!enableRuntimeVfx) return;

        selectedBlock = block;
        EnsureSelectionRing();
        selectionRing.SetActive(true);
        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        CreateTransientRing("Block Selection Ping", bounds.center, block.transform.up, selectionColor, Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.5f, 0.32f, 0.045f);
    }

    private void ClearBlockSelection(Block block)
    {
        if (block != null && selectedBlock != block) return;

        selectedBlock = null;
        if (selectionRing != null)
        {
            selectionRing.SetActive(false);
        }
        if (selectionRingEffect != null)
        {
            selectionRingEffect.SetVisible(false);
        }
    }

    private void UpdateGhostPreview(Transform ghost, bool isBlocked)
    {
        if (!enableRuntimeVfx) return;

        ghostTarget = ghost;
        ghostBlocked = isBlocked;
        EnsureGhostRing();
        ghostRing.SetActive(true);
    }

    private void ClearGhostPreview()
    {
        ghostTarget = null;
        if (ghostRing != null)
        {
            ghostRing.SetActive(false);
        }
        if (ghostRingEffect != null)
        {
            ghostRingEffect.SetVisible(false);
        }
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
            meteor.glowLight.intensity = Mathf.Clamp(scale * 1.3f, meteor.minGlowIntensity, meteor.maxGlowIntensity);
            meteor.glowLight.range = Mathf.Clamp(scale * 4f, 2f, 18f);
            meteor.glowLight.shadows = LightShadows.None;
        }
    }

    private void PlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)
    {
        if (!enableRuntimeVfx) return;

        float impactScale = Mathf.Clamp(scale * 0.65f + speed * 0.025f, 0.6f, 5f);
        Vector3 impactNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;

        CreateParticleBurst("Meteor Impact Sparks", position + impactNormal * 0.08f, new Color(1f, 0.5f, 0.1f, 1f), Color.yellow, 70, impactScale * 0.15f, 3f, 11f, 0.2f, 0.75f, 0.045f, 0.18f, 0.1f);
        CreateParticleBurst("Meteor Impact Smoke", position + impactNormal * 0.18f, new Color(0.5f, 0.58f, 0.68f, 0.45f), new Color(0.05f, 0.065f, 0.08f, 0.15f), 32, impactScale * 0.32f, 0.4f, 1.8f, 0.8f, 1.8f, 0.28f, 0.75f, -0.08f);
        CreateTransientRing("Meteor Shockwave", position + impactNormal * 0.04f, impactNormal, new Color(1f, 0.72f, 0.24f, 0.9f), impactScale * 1.5f, 0.65f, 0.1f);
        CreateLightFlash(position, new Color(1f, 0.5f, 0.14f, 1f), impactScale * 2.2f, impactScale * 4.5f, 0.25f);
        ShakeCamera(cameraShakeStrength * Mathf.Clamp(impactScale, 1f, 3f), 0.2f);
    }

    private void UpdateSelectionRing()
    {
        if (selectionRing == null || selectionRingEffect == null) return;
        if (selectedBlock == null)
        {
            selectionRing.SetActive(false);
            return;
        }

        Bounds bounds = GetBounds(selectedBlock.gameObject, selectedBlock.transform.position, GetBlockSize(selectedBlock));
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.35f;
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5.3f) * 0.045f;
        Color color = WithAlpha(selectionColor, 0.58f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.14f);

        selectionRing.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.045f, bounds.center.z);
        selectionRing.transform.rotation = Quaternion.identity;
        selectionRing.transform.localScale = Vector3.one * radius * pulse;
        selectionRingEffect.SetVisual(color, 0.05f);
        selectionRingEffect.SetVisible(true);
    }

    private void UpdateGhostRing()
    {
        if (ghostRing == null || ghostRingEffect == null) return;
        if (ghostTarget == null)
        {
            ghostRing.SetActive(false);
            return;
        }

        Block ghostBlock = ghostTarget.GetComponent<Block>();
        Vector3 fallbackSize = ghostBlock != null ? GetBlockSize(ghostBlock) : Vector3.one;
        Bounds bounds = GetBounds(ghostTarget.gameObject, ghostTarget.position, fallbackSize);
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.28f;
        Color color = ghostBlocked ? blockedGhostColor : validGhostColor;
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.035f;

        ghostRing.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.055f, bounds.center.z);
        ghostRing.transform.rotation = Quaternion.identity;
        ghostRing.transform.localScale = Vector3.one * radius * pulse;
        ghostRingEffect.SetVisual(color, ghostBlocked ? 0.075f : 0.052f);
        ghostRingEffect.SetVisible(true);
    }

    private void EnsureSelectionRing()
    {
        if (selectionRing != null) return;

        selectionRing = CreateRingObject("Selection Ring VFX", out selectionRingEffect);
        selectionRing.SetActive(false);
    }

    private void EnsureGhostRing()
    {
        if (ghostRing != null) return;

        ghostRing = CreateRingObject("Ghost Preview Ring VFX", out ghostRingEffect);
        ghostRing.SetActive(false);
    }

    private static GameObject CreateRingObject(string name, out StylizedRingEffect ringEffect)
    {
        GameObject ring = new GameObject(name);
        ringEffect = ring.AddComponent<StylizedRingEffect>();
        ringEffect.Configure(RingSegments, 0.05f);

        return ring;
    }

    private void CreateTransientRing(string name, Vector3 position, Vector3 normal, Color color, float radius, float duration, float width)
    {
        GameObject ring = CreateRingObject(name, out StylizedRingEffect ringEffect);
        ring.transform.position = position;
        ring.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up);
        ring.transform.localScale = Vector3.one * 0.15f;
        ringEffect.SetVisual(color, width);
        ringEffect.SetVisible(true);
        ring.AddComponent<RingFade>().Initialize(ringEffect, color, Mathf.Max(0.05f, radius), Mathf.Max(0.05f, duration), width);
    }

    private void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)
    {
        GameObject streak = new GameObject("Movement Streak VFX");
        StylizedBeamEffect beam = streak.AddComponent<StylizedBeamEffect>();
        beam.Configure(width, 3.8f, 8, 0.015f, 2.4f, 14f);
        beam.SetEndpoints(from, to);
        beam.SetColor(color);
        beam.SetVisible(true);
        streak.AddComponent<BeamFade>().Initialize(beam, color, Mathf.Max(0.05f, duration));
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
        float gravity)
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
        renderer.sharedMaterial = GetSharedParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 4f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

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

    private sealed class RingFade : MonoBehaviour
    {
        private StylizedRingEffect ringEffect;
        private Color color;
        private float targetRadius;
        private float duration;
        private float width;
        private float elapsed;

        public void Initialize(StylizedRingEffect effect, Color lineColor, float radius, float lifetime, float lineWidth)
        {
            ringEffect = effect;
            color = lineColor;
            targetRadius = radius;
            duration = lifetime;
            width = lineWidth;
        }

        private void Update()
        {
            if (ringEffect == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            transform.localScale = Vector3.one * Mathf.Lerp(0.15f, targetRadius, eased);
            ringEffect.SetIntensity((1f - t) * (1f - t));

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
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
