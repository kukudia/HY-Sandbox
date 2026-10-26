// 职责：为建造、维修、损伤、爆炸和能量反馈提供统一效果入口。
// 入口：EnsureInstance() / GetSharedLineMaterial() / TryPlayBlockPlaced()。
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class VisualEffectsManager : MonoBehaviour
{
    public static VisualEffectsManager instance;
    [Header("Runtime VFX")]
    public bool enableRuntimeVfx = true;
    public bool enableSceneLook = true;
    [SerializeField, Tooltip("Log each destruction VFX request and its spawn result.")]
    private bool _debugDestructionVfx;
    public float cameraShakeStrength = 0.055f;
    public Color buildColor = new Color(0.2f, 0.95f, 1f, 1f);
    public Color removeColor = new Color(1f, 0.34f, 0.08f, 1f);
    public Color selectionColor = new Color(0.38f, 0.95f, 1f, 1f);
    public Color blockedGhostColor = new Color(1f, 0.12f, 0.08f, 1f);
    public Color validGhostColor = new Color(0.24f, 1f, 0.58f, 1f);
    [Header("Scene Look")]
    [SerializeField, Tooltip("场景雾色；默认值保持既有Main外观，Siege场景单独覆盖为夜色。")]
    private Color sceneFogColor = new Color(0.035f, 0.047f, 0.065f, 1f);
    [SerializeField, Min(0f), Tooltip("场景雾密度的下限，不会把已有更高密度调低。")]
    private float sceneFogDensity = 0.0045f;
    [SerializeField, Tooltip("环境光模式；场景已有设置会被这里覆盖。")]
    private AmbientMode sceneAmbientMode = AmbientMode.Flat;
    [SerializeField, Tooltip("环境光目标色，按混合比例靠近当前环境光。")]
    private Color sceneAmbientColor = new Color(0.17f, 0.2f, 0.25f, 1f);
    [SerializeField, Range(0f, 1f), Tooltip("向环境光目标色混合的比例；0保持场景原值。")]
    private float sceneAmbientBlend = 0.45f;
    private CameraShake _cameraShake;
    private CameraShake Shake => _cameraShake ?? (_cameraShake = new CameraShake(this));

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
        if (block == null)
        {
            return;
        }

        EnsureInstance().PlayBlockPlaced(block);
    }

    public static void TryPlayBlockRemoved(Block block)
    {
        if (block == null)
        {
            return;
        }

        EnsureInstance().PlayBlockRemoved(block);
    }

    public static void TryPlayBlockExplosion(Block block)
    {
        if (block == null)
        {
            return;
        }

        EnsureInstance().PlayBlockExplosion(block);
    }

    public static void TryPlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)
    {
        EnsureInstance().PlayRepairPulse(origin, target, color, width);
    }

    public static void TryAttachDetachedPartSmoke(Rigidbody body, Vector3 worldAnchor, float intensity)
    {
        if (body == null)
        {
            return;
        }

        VisualEffectsManager manager = EnsureInstance();
        if (!manager.enableRuntimeVfx)
        {
            return;
        }

        DetachedPartSmokeTrail.Attach(body, worldAnchor, intensity);
    }

    public static void TryPlayObjectDestroyed(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        EnsureInstance().PlayObjectDestroyed(target);
    }

    public static void TryPlayBlockMoved(Block block, Vector3 from, Vector3 to)
    {
        if (block == null)
        {
            return;
        }

        EnsureInstance().PlayBlockMoved(block, from, to);
    }

    public static void TryPlayBlockRotated(Block block)
    {
        if (block == null)
        {
            return;
        }

        EnsureInstance().PlayBlockRotated(block);
    }

    public static void TryShowBlockSelection(Block block)
    {
        if (block == null)
        {
            return;
        }

        EnsureInstance().ShowBlockSelection(block);
    }

    public static void TryClearBlockSelection(Block block)
    {
        if (instance == null)
        {
            return;
        }

        instance.ClearBlockSelection(block);
    }

    public static void TryUpdateGhostPreview(GameObject ghost, bool isBlocked)
    {
        if (ghost == null)
        {
            return;
        }

        EnsureInstance().UpdateGhostPreview(ghost.transform, isBlocked);
    }

    public static void TryClearGhostPreview(GameObject ghost)
    {
        if (instance == null)
        {
            return;
        }

        instance.ClearGhostPreview();
    }

    public static void TryDecorateMeteor(Meteor meteor)
    {
        if (meteor == null)
        {
            return;
        }

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

    private void OnDisable()
    {
        _cameraShake?.Cancel();
    }

    /// <summary>供独立玩法界面请求一次镜头抖动；不创建对象、不改变效果预算。</summary>
    public void PlayCameraShake(float amplitude, float duration)
    {
        Shake.Play(Camera.main, amplitude, duration);
    }

    /// <summary>敌舰入场信标：一次闪光、垂直光柱和短促爆闪，复用既有池，标记进场方向。</summary>
    public void PlaySpawnBeacon(Vector3 position)
    {
        if (!enableRuntimeVfx)
        {
            return;
        }

        Color beacon = new Color(1f, 0.44f, 0.2f, 1f);
        TransientEffects.CreateLightFlash(position, beacon, 7f, 34f, 1.1f);
        TransientEffects.CreateLineStreak(position, position + Vector3.up * 14f, beacon, 2.2f, 0.5f);
        TransientEffects.CreateRadialStreakBurst(position, Vector3.up, beacon, 6, 7f, 0.6f, 0.07f);
    }

    private void OnDestroy()
    {
        _cameraShake?.Cancel();
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnEnable()
    {
        ApplySceneLook();
    }

    private void ApplySceneLook()
    {
        if (!enableSceneLook)
        {
            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = sceneFogColor;
        RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity, sceneFogDensity);
        RenderSettings.ambientMode = sceneAmbientMode;
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, sceneAmbientColor, sceneAmbientBlend);
    }

    private void PlayBlockPlaced(Block block)
    {
        if (!enableRuntimeVfx)
        {
            return;
        }

        Bounds bounds = TransientEffects.GetBounds(block.gameObject, block.transform.position, TransientEffects.GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 0.35f, 0.55f, 2.8f);
        Vector3 center = bounds.center;
        Color brightBuild = TransientEffects.Brighten(buildColor, 2.2f, 0.18f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Build, center, Quaternion.identity, scale);
        TransientEffects.CreateRadialStreakBurst(
            center,
            Vector3.up,
            brightBuild,
            8,
            scale * 0.9f,
            0.2f,
            0.035f);
        TransientEffects.CreateLightFlash(center, buildColor, 3.4f, scale * 4.2f, 0.26f);
        Shake.Play(Camera.main, cameraShakeStrength * 0.45f, 0.12f);
    }

    private void PlayBlockRemoved(Block block)
    {
        if (!enableRuntimeVfx)
        {
            return;
        }

        Bounds bounds = TransientEffects.GetBounds(block.gameObject, block.transform.position, TransientEffects.GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 0.35f, 0.55f, 3.3f);
        Color brightRemove = TransientEffects.Brighten(removeColor, 2.35f, 0.15f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Break, bounds.center, Quaternion.identity, scale);
        TransientEffects.CreateRadialStreakBurst(
            bounds.center,
            Vector3.zero,
            brightRemove,
            10,
            scale * 1.1f,
            0.28f,
            0.045f);
        TransientEffects.CreateLightFlash(bounds.center, removeColor, 4.2f, scale * 4.8f, 0.34f);
        Shake.Play(Camera.main, cameraShakeStrength, 0.18f);
    }

    private void PlayBlockExplosion(Block block)
    {
        if (!enableRuntimeVfx)
        {
            if (_debugDestructionVfx)
            {
                Debug.Log($"[Destruction VFX] PlayBlockExplosion skipped: runtime VFX disabled; target={block.name}", block);
            }

            return;
        }

        Bounds bounds = TransientEffects.GetBounds(block.gameObject, block.transform.position, TransientEffects.GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.size.magnitude * 20f, 20f, 80f);
        Vector3 center = bounds.center;
        Color emberColor = new Color(2.6f, 1.15f, 0.22f, 1f);
        // The library's Explosion prefab is the native UNI Aerial Explosion graph.
        BlockVfxLibrary.SpawnResult result = BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Explosion, center, Quaternion.identity, scale);
        if (_debugDestructionVfx)
        {
            Debug.Log(
                $"[Destruction VFX] PlayBlockExplosion target={block.name}, center={center}, bounds={bounds.size}, scale={scale:F2}, graph={result}",
                block);
        }

        if (result != BlockVfxLibrary.SpawnResult.Spawned)
        {
            return;
        }

        TransientEffects.CreateLightFlash(center, emberColor, 7.5f, scale * 7f, 0.62f);
        Shake.Play(Camera.main, cameraShakeStrength * 1.35f, 0.42f);
    }

    private void PlayObjectDestroyed(GameObject target)
    {
        if (!enableRuntimeVfx)
        {
            if (_debugDestructionVfx)
            {
                Debug.Log(
                    $"[Destruction VFX] PlayObjectDestroyed skipped: runtime VFX disabled; target={target.name}",
                    target);
            }

            return;
        }

        Bounds bounds = TransientEffects.GetBounds(target, target.transform.position, Vector3.one);
        float scale = Mathf.Clamp(bounds.size.magnitude * 10f, 10f, 40f);
        BlockVfxLibrary.SpawnResult result = BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Explosion, bounds.center, Quaternion.identity, scale);
        if (_debugDestructionVfx)
        {
            Debug.Log(
                $"[Destruction VFX] PlayObjectDestroyed target={target.name}, center={bounds.center}, bounds={bounds.size}, scale={scale:F2}, graph={result}",
                target);
        }

        if (result != BlockVfxLibrary.SpawnResult.Spawned)
        {
            return;
        }

        TransientEffects.CreateLightFlash(bounds.center, removeColor, 5f, scale * 5f, 0.45f);
    }

    private void PlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)
    {
        if (!enableRuntimeVfx)
        {
            return;
        }

        float scale = Mathf.Clamp(width * 2.4f, 0.22f, 0.7f);
        Color brightRepair = TransientEffects.Brighten(color, 2.4f, 0.2f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Repair, target, Quaternion.identity, 1f);
        TransientEffects.CreateRadialStreakBurst(
            target,
            target - origin,
            brightRepair,
            5,
            scale * 0.75f,
            0.16f,
            Mathf.Max(0.018f, width * 0.12f));
        TransientEffects.CreateLineStreak(
            origin,
            target,
            TransientEffects.WithAlpha(brightRepair, 0.95f),
            0.16f,
            Mathf.Max(0.02f, width * 0.15f));
        TransientEffects.CreateLightFlash(target, color, 1.8f, Mathf.Max(1f, scale * 2.8f), 0.16f);
    }

    private void PlayBlockMoved(Block block, Vector3 from, Vector3 to)
    {
        if (!enableRuntimeVfx || (to - from).sqrMagnitude < 0.0025f)
        {
            return;
        }

        Bounds bounds = TransientEffects.GetBounds(block.gameObject, to, TransientEffects.GetBlockSize(block));
        Vector3 centerOffset = bounds.center - block.transform.position;
        TransientEffects.CreateLineStreak(from + centerOffset, to + centerOffset, buildColor, 0.24f, 0.06f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Build, to + centerOffset, Quaternion.identity, 0.35f);
    }

    private void PlayBlockRotated(Block block)
    {
        if (!enableRuntimeVfx)
        {
            return;
        }

        Bounds bounds = TransientEffects.GetBounds(block.gameObject, block.transform.position, TransientEffects.GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.extents.magnitude, 0.45f, 2.5f);
        Color brightSelection = TransientEffects.Brighten(selectionColor, 2.1f, 0.2f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Build, bounds.center, Quaternion.identity, scale * 0.4f);
        TransientEffects.CreateRadialStreakBurst(
            bounds.center,
            block.transform.up,
            brightSelection,
            6,
            scale * 0.8f,
            0.18f,
            0.025f);
    }

    private void ShowBlockSelection(Block block)
    {
        if (!enableRuntimeVfx)
        {
            return;
        }

        Bounds bounds = TransientEffects.GetBounds(block.gameObject, block.transform.position, TransientEffects.GetBlockSize(block));
        float scale = Mathf.Clamp(bounds.extents.magnitude, 0.4f, 2.5f);
        Color brightSelection = TransientEffects.Brighten(selectionColor, 2.25f, 0.2f);
        BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Build, bounds.center, Quaternion.identity, scale * 0.3f);
        TransientEffects.CreateLightFlash(bounds.center, selectionColor, 1.4f, Mathf.Max(1.2f, scale * 2.2f), 0.16f);
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
        if (!enableRuntimeVfx || meteor == null)
        {
            return;
        }

        float scale = Mathf.Max(0.35f, meteor.transform.lossyScale.magnitude / 1.732f);
        var library = BlockVfxLibrary.Instance;
        if (meteor.trailEffect == null && library != null && library.DetachedSmoke != null)
        {
            meteor.trailEffect = Instantiate(library.DetachedSmoke, meteor.transform.position, Quaternion.identity);
            meteor.trailEffect.transform.localScale = Vector3.Scale(meteor.trailEffect.transform.localScale, meteor.transform.lossyScale);
        }

        if (meteor.trailEffect != null)
        {
            meteor.trailEffect.transform.SetParent(null, true);
            meteor.trailEffect.SetIntensity(1f);
            var follower = meteor.trailEffect.GetComponent<MeteorTrailFollower>() ?? meteor.trailEffect.gameObject.AddComponent<MeteorTrailFollower>();
            follower.Initialize(meteor.transform, meteor.trailEffect);
        }

        if (meteor.glowLight == null && MeteorLightToken.ActiveCount < MeteorLightToken.Maximum)
        {
            GameObject lightObject = new GameObject("Meteor Glow VFX");
            lightObject.transform.SetParent(meteor.transform, false);
            meteor.glowLight = lightObject.AddComponent<Light>();
            lightObject.AddComponent<MeteorLightToken>();
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
        if (!enableRuntimeVfx)
        {
            return;
        }

        float impactScale = Mathf.Clamp(scale * 0.65f + speed * 0.025f, 0.6f, 5f);
        Vector3 impactNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
        Color meteorColor = new Color(2.8f, 1.1f, 0.2f, 1f);
        BlockVfxLibrary.SpawnResult result = BlockVfxLibrary.Play(
            BlockVfxLibrary.Effect.Explosion,
            position + impactNormal * 0.08f,
            Quaternion.identity,
            impactScale);
        TransientEffects.CreateRadialStreakBurst(
            position + impactNormal * 0.05f,
            impactNormal,
            meteorColor,
            14,
            impactScale * 1.8f,
            0.3f,
            0.05f);
        if (result == BlockVfxLibrary.SpawnResult.Spawned)
        {
            TransientEffects.CreateLightFlash(position, meteorColor, impactScale * 3.8f, impactScale * 5.5f, 0.32f);
        }

        Shake.Play(Camera.main, cameraShakeStrength * Mathf.Clamp(impactScale, 1f, 3f), 0.2f);
    }
}
