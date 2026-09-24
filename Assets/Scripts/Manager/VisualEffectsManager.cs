using System.Collections;
using UnityEngine;

public class VisualEffectsManager : MonoBehaviour
{
    private const int MaxMeteorGlowLights = 24;

    private static int activeMeteorGlowLights;

    public static VisualEffectsManager instance;

    [Header("Runtime VFX")]
    public bool enableRuntimeVfx = true;
    public bool enableSceneLook = true;
    [SerializeField, Tooltip("Log each destruction VFX request and its spawn result.")]
    private bool _debugDestructionVfx = true;
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

    public static Material GetSharedLineMaterial()
    {
        return BlockVfxLibrary.Instance != null ? BlockVfxLibrary.Instance.LineMaterial : null;
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

        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Build, center, Quaternion.identity, scale);
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
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Break, bounds.center, Quaternion.identity, scale);
        CreateRadialStreakBurst(bounds.center, Vector3.zero, brightRemove, 10, scale * 1.1f, 0.28f, 0.045f);
        CreateLightFlash(bounds.center, removeColor, 4.2f, scale * 4.8f, 0.34f);
        ShakeCamera(cameraShakeStrength, 0.18f);
    }

    private void PlayBlockExplosion(Block block)
    {
        if (!enableRuntimeVfx)
        {
            if (_debugDestructionVfx) Debug.Log($"[Destruction VFX] PlayBlockExplosion skipped: runtime VFX disabled; target={block.name}", block);
            return;
        }

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 20f, 20f, 80f);
        Vector3 center = bounds.center;
        Color emberColor = new Color(2.6f, 1.15f, 0.22f, 1f);

        // The library's Explosion prefab is the native UNI Aerial Explosion graph.
        BlockVfxLibrary.SpawnResult result = BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Explosion, center, Quaternion.identity, scale);
        if (_debugDestructionVfx)
            Debug.Log($"[Destruction VFX] PlayBlockExplosion target={block.name}, center={center}, bounds={bounds.size}, scale={scale:F2}, graph={result}", block);
        CreateLightFlash(center, emberColor, 7.5f, scale * 7f, 0.62f);
        ShakeCamera(cameraShakeStrength * 1.35f, 0.42f);
    }

    private void PlayObjectDestroyed(GameObject target)
    {
        if (!enableRuntimeVfx)
        {
            if (_debugDestructionVfx) Debug.Log($"[Destruction VFX] PlayObjectDestroyed skipped: runtime VFX disabled; target={target.name}", target);
            return;
        }

        Bounds bounds = GetBounds(target, target.transform.position, Vector3.one);
        float scale = Mathf.Clamp(bounds.size.magnitude * 10f, 10f, 40f);
        BlockVfxLibrary.SpawnResult result = BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Explosion, bounds.center, Quaternion.identity, scale);
        if (_debugDestructionVfx)
            Debug.Log($"[Destruction VFX] PlayObjectDestroyed target={target.name}, center={bounds.center}, bounds={bounds.size}, scale={scale:F2}, graph={result}", target);
        CreateLightFlash(bounds.center, removeColor, 5f, scale * 5f, 0.45f);
    }

    private void PlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)
    {
        if (!enableRuntimeVfx) return;

        float scale = Mathf.Clamp(width * 2.4f, 0.22f, 0.7f);
        Color brightRepair = Brighten(color, 2.4f, 0.2f);

        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Repair, target, Quaternion.identity, 1f);
        CreateRadialStreakBurst(target, target - origin, brightRepair, 5, scale * 0.75f, 0.16f, Mathf.Max(0.018f, width * 0.12f));
        CreateLineStreak(origin, target, WithAlpha(brightRepair, 0.95f), 0.16f, Mathf.Max(0.02f, width * 0.15f));
        CreateLightFlash(target, color, 1.8f, Mathf.Max(1f, scale * 2.8f), 0.16f);
    }

    private void PlayBlockMoved(Block block, Vector3 from, Vector3 to)
    {
        if (!enableRuntimeVfx || (to - from).sqrMagnitude < 0.0025f) return;

        Bounds bounds = GetBounds(block.gameObject, to, GetBlockSize(block));
        Vector3 centerOffset = bounds.center - block.transform.position;
        CreateLineStreak(from + centerOffset, to + centerOffset, buildColor, 0.24f, 0.06f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Build, to + centerOffset, Quaternion.identity, 0.35f);
    }

    private void PlayBlockRotated(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.extents.magnitude, 0.45f, 2.5f);
        Color brightSelection = Brighten(selectionColor, 2.1f, 0.2f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Build, bounds.center, Quaternion.identity, scale * 0.4f);
        CreateRadialStreakBurst(bounds.center, block.transform.up, brightSelection, 6, scale * 0.8f, 0.18f, 0.025f);
    }

    private void ShowBlockSelection(Block block)
    {
        if (!enableRuntimeVfx) return;

        Bounds bounds = GetBounds(block.gameObject, block.transform.position, GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.extents.magnitude, 0.4f, 2.5f);
        Color brightSelection = Brighten(selectionColor, 2.25f, 0.2f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Build, bounds.center, Quaternion.identity, scale * 0.3f);
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
        var library = BlockVfxLibrary.Instance;
        if (meteor.trailEffect == null && library != null && library.DetachedSmoke != null)
        {
            meteor.trailEffect = Instantiate(library.DetachedSmoke, meteor.transform.position, Quaternion.identity);
            meteor.trailEffect.transform.localScale = Vector3.Scale(
                meteor.trailEffect.transform.localScale, meteor.transform.lossyScale);
        }
        if (meteor.trailEffect != null)
        {
            meteor.trailEffect.transform.SetParent(null, true);
            meteor.trailEffect.SetIntensity(1f);
            var follower = meteor.trailEffect.GetComponent<MeteorTrailFollower>() ??
                meteor.trailEffect.gameObject.AddComponent<MeteorTrailFollower>();
            follower.Initialize(meteor.transform, meteor.trailEffect);
        }

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
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Explosion, position + impactNormal * 0.08f, Quaternion.identity, impactScale);
        CreateRadialStreakBurst(position + impactNormal * 0.05f, impactNormal, meteorColor, 14, impactScale * 1.8f, 0.3f, 0.05f);
        CreateLightFlash(position, meteorColor, impactScale * 3.8f, impactScale * 5.5f, 0.32f);
        ShakeCamera(cameraShakeStrength * Mathf.Clamp(impactScale, 1f, 3f), 0.2f);
    }

    private void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)
    {
        GameObject streak = new GameObject("Energy Streak VFX");
        StylizedBeamEffect beam = streak.AddComponent<StylizedBeamEffect>();
        beam.Configure(width);
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
            if (renderer == null || !renderer.enabled || (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))) continue;

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

    private sealed class MeteorTrailFollower : MonoBehaviour
    {
        private Transform _meteor;
        private Rigidbody _body;
        private VfxEffect _effect;

        public void Initialize(Transform meteor, VfxEffect effect)
        {
            _meteor = meteor;
            _body = meteor.GetComponent<Rigidbody>();
            _effect = effect;
            Follow();
        }

        private void LateUpdate()
        {
            if (_effect == null || !_effect.IsEmitting) { enabled = false; return; }
            if (_meteor == null)
            {
                _effect.StopAndRelease();
                enabled = false;
                return;
            }
            Follow();
        }

        private void Follow()
        {
            transform.position = _meteor.position;
            transform.rotation = _body != null && _body.linearVelocity.sqrMagnitude > 0.01f
                ? Quaternion.FromToRotation(Vector3.up, -_body.linearVelocity.normalized)
                : Quaternion.identity;
        }
    }
}
