// 职责：管理可编辑 VFX 资源和效果生成/回收预算，供玩法调用。
// 入口：Play()。
using UnityEngine;

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

        int index = (int)kind;
        if (index < 0 || index >= library._bursts.Length || library._bursts[index] == null)
        {
            return SpawnResult.MissingPrefab;
        }

        bool priority = kind == Effect.Explosion;
        if (!VfxEffect.MakeRoomForBurst(priority))
        {
            return SpawnResult.BurstLimit;
        }

        VfxEffect effect = Instantiate(library._bursts[index], position, rotation);
        effect.transform.localScale *= Mathf.Clamp(scale, 0.05f, 4f);
        effect.PlayOnce();
        effect.ReleaseAfterPlayback(priority);
        return SpawnResult.Spawned;
    }
}
