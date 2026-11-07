// 职责：保存数量由玩法决定的运行对象模板；组件组成在 Prefab 中制作，运行时只实例化。
// 入口：Instance；场景常驻服务直接放在 Managers，不通过此库自动创建。
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "HY Sandbox/Runtime Object Templates")]
public sealed class RuntimeObjectTemplates : ScriptableObject
{
    [SerializeField]
    private BuildingRuntime building;
    [SerializeField]
    private Rigidbody buildingDebris;
    [SerializeField]
    private VoxelWorldChunk terrainChunk;
    [SerializeField]
    private VoxelDebrisPiece voxelDebris;
    [SerializeField]
    private MeshFilter visualMesh;
    [SerializeField]
    private TextMesh worldLabel;
    [SerializeField]
    private CanvasGroup statusGroup;
    [SerializeField]
    private Image statusIcon;
    [SerializeField]
    private BeamFade transientBeam;
    [SerializeField]
    private LightFade transientLight;
    [SerializeField]
    private Light meteorLight;
    [SerializeField]
    private Light workshopLight;
    [SerializeField]
    private LineRenderer powerConnection;
    private static RuntimeObjectTemplates instance;
    public static RuntimeObjectTemplates Instance => instance != null ? instance : (instance = Resources.Load<RuntimeObjectTemplates>("Runtime/Objects") ?? throw new MissingReferenceException("Missing Runtime/Objects. Run Tools/HY Sandbox/Architecture/Bake Editable Components."));
    public BuildingRuntime Building => building;
    public Rigidbody BuildingDebris => buildingDebris;
    public VoxelWorldChunk TerrainChunk => terrainChunk;
    public VoxelDebrisPiece VoxelDebris => voxelDebris;
    public MeshFilter VisualMesh => visualMesh;
    public TextMesh WorldLabel => worldLabel;
    public CanvasGroup StatusGroup => statusGroup;
    public Image StatusIcon => statusIcon;
    public BeamFade TransientBeam => transientBeam;
    public LightFade TransientLight => transientLight;
    public Light MeteorLight => meteorLight;
    public Light WorkshopLight => workshopLight;
    public LineRenderer PowerConnection => powerConnection;
}
