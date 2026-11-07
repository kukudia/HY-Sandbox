// 职责：管理可编辑 VFX 资源和效果生成/回收预算，供玩法调用。
// 入口：Play()。
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "HY Sandbox/Block VFX Library")]
public sealed class BlockVfxLibrary : ScriptableObject
{
    [System.Serializable]
    public sealed class AttachedEffect
    {
        public string id;
        public VfxEffect prefab;
    }

    public enum Effect
    {
        Build,
        Break,
        Explosion,
        Smoke,
        Repair,
        Muzzle,
        Impact
    }

    public enum SpawnResult
    {
        Spawned,
        MissingLibrary,
        BurstLimit,
        MissingPrefab
    }

    [SerializeField]
    private VfxEffect[] _bursts = new VfxEffect[7];
    [SerializeField]
    private VfxEffect _detachedSmoke;
    [SerializeField]
    private Material _lineMaterial;
    [SerializeField]
    private VfxEffect _beam;
    [SerializeField]
    private AttachedEffect[] _attached = System.Array.Empty<AttachedEffect>();
    [SerializeField, Min(0), Tooltip("每个模板保留的闲置实例数；不限制正在播放的特效。")]
    private int _maxIdlePerTemplate = 32;
    [SerializeField, Min(0), Tooltip("每个场景闲置 GPU 特效的总缓存上限。")]
    private int _maxIdleEffects = 128;
    public int MaxIdlePerTemplate => Mathf.Max(0, _maxIdlePerTemplate);
    public int MaxIdleEffects => Mathf.Max(0, _maxIdleEffects);
    public VfxEffect Beam => _beam;

    public VfxEffect Resolve(string id)
    {
        switch (id)
        {
            case "BuildBurst":
                return Burst(Effect.Build);
            case "BreakBurst":
                return Burst(Effect.Break);
            case "Explosion":
                return Burst(Effect.Explosion);
            case "SmokeBurst":
                return Burst(Effect.Smoke);
            case "RepairPulse":
                return Burst(Effect.Repair);
            case "MuzzleFlash":
                return Burst(Effect.Muzzle);
            case "ImpactBurst":
                return Burst(Effect.Impact);
            case "SteamLeak":
            case "DetachedSmoke":
                return _detachedSmoke;
            case "EnergyBeam":
                return _beam;
        }

        foreach (AttachedEffect entry in _attached)
        {
            if (entry != null && entry.id == id)
            {
                return entry.prefab;
            }
        }

        return null;
    }

    private VfxEffect Burst(Effect kind)
    {
        int index = (int)kind;
        return index >= 0 && index < _bursts.Length ? _bursts[index] : null;
    }

    private static BlockVfxLibrary _instance;
    public static BlockVfxLibrary Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<BlockVfxLibrary>("VFX/BlockVfxLibrary");
            }

            return _instance;
        }
    }

    public Material LineMaterial => _lineMaterial;
    public VfxEffect DetachedSmoke => _detachedSmoke;

    public static SpawnResult Play(Effect kind, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        BlockVfxLibrary library = Instance;
        if (library == null)
        {
            return SpawnResult.MissingLibrary;
        }

        return PlayTemplate(library.Burst(kind), position, rotation, Mathf.Clamp(scale, 0.05f, 4f));
    }

    /// <summary>播放集中库的具名一次性模板；缩放以模板单位为准，不沿用方块爆炸的4倍限制。</summary>
    public static SpawnResult Play(string id, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        BlockVfxLibrary library = Instance;
        if (library == null)
        {
            return SpawnResult.MissingLibrary;
        }

        return PlayTemplate(library.Resolve(id), position, rotation, Mathf.Max(0.05f, scale));
    }

    private static SpawnResult PlayTemplate(VfxEffect template, Vector3 position, Quaternion rotation, float scale)
    {
        if (template == null)
        {
            return SpawnResult.MissingPrefab;
        }

        VfxEffect effect = VfxEffectPool.Rent(template, position, rotation, SceneManager.GetActiveScene());
        effect.transform.localScale *= scale;
        effect.PlayOnce();
        effect.ReleaseAfterPlayback();
        return SpawnResult.Spawned;
    }
}
