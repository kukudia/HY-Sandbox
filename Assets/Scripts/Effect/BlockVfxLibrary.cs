using UnityEngine;

[CreateAssetMenu(menuName = "HY Sandbox/Block VFX Library")]
public sealed class BlockVfxLibrary : ScriptableObject
{
    public enum Effect { Build, Break, Explosion, Smoke, Repair, Muzzle, Impact }
    [SerializeField] private VfxEffect[] _bursts = new VfxEffect[7];
    [SerializeField] private VfxEffect _detachedSmoke;
    [SerializeField] private Material _lineMaterial;
    [SerializeField] private VfxEffect _beam;
    public VfxEffect Beam => _beam;
    private static BlockVfxLibrary _instance;

    public static BlockVfxLibrary Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<BlockVfxLibrary>("VFX/BlockVfxLibrary");
            return _instance;
        }
    }

    public Material LineMaterial => _lineMaterial;
    public VfxEffect DetachedSmoke => _detachedSmoke;

    public static void Play(Effect kind, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        BlockVfxLibrary library = Instance;
        if (library == null || !VfxEffect.CanSpawnBurst) return;
        int index = (int)kind;
        if (index >= library._bursts.Length || library._bursts[index] == null) return;
        VfxEffect effect = Instantiate(library._bursts[index], position, rotation);
        effect.transform.localScale *= Mathf.Clamp(scale, 0.05f, 4f);
        effect.PlayOnce();
        effect.ReleaseAfterPlayback();
    }
}
