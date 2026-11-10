// 职责：在战斗区域异步生成共享 NavMesh；只采集静态碰撞，移动舰船交给局部避障。
// 入口：Update() / Rebuild()；组件显式保存在玩法场景，禁用时注销并释放导航数据。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public sealed class EnemyNavigationWorld : MonoBehaviour
{
    [SerializeField, Tooltip("Navigation 窗口中配置的 Agent Type ID。半径应覆盖典型舰体半宽。")]
    private int agentTypeId;
    [SerializeField, Min(32f), Tooltip("围绕参战舰队中心的水平导航半径（米），限制流式世界烘焙成本。")]
    private float navigationRadius = 240f;
    [SerializeField, Min(16f)]
    private float navigationHeight = 160f;
    [SerializeField, Min(0.25f), Tooltip("运行时地形破坏、建筑加载/损毁后的重新采集间隔（秒）。")]
    private float rebuildInterval = 3f;
    [SerializeField, Min(0.1f)]
    private float voxelSize = 0.5f;
    [SerializeField, Min(0.1f), Tooltip("导航走廊与墙面的净空；大型舰船可提高此半径。")]
    private float clearanceRadius = 2f;
    [SerializeField, Min(0.1f)]
    private float clearanceHeight = 3f;
    [SerializeField, Tooltip("按本物理场景中自主舰船的真实水平碰撞范围提高净空，避免宽舰体贴墙。")]
    private bool fitActiveShips = true;
    [SerializeField, Min(0f)]
    private float shipClearancePadding = 2.5f;
    [SerializeField]
    private LayerMask geometryLayers = ~0;
    private static readonly List<EnemyNavigationWorld> Worlds = new List<EnemyNavigationWorld>();
    private readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
    private readonly List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
    private readonly List<NavMeshBuildSource> localSources = new List<NavMeshBuildSource>();
    private NavMeshData data;
    private NavMeshDataInstance instance;
    private AsyncOperation build;
    private float nextBuildTime;
    public float BuiltClearanceRadius
    {
        get; private set;
    }
    public int AgentTypeId => agentTypeId;
    public bool IsReady => instance.valid;

    private void OnEnable()
    {
        Worlds.Add(this);
        nextBuildTime = 0f;
    }

    public static EnemyNavigationWorld Find(Scene scene)
    {
        foreach (EnemyNavigationWorld world in Worlds)
        {
            if (world != null && world.isActiveAndEnabled && world.gameObject.scene.GetPhysicsScene() == scene.GetPhysicsScene())
            {
                return world;
            }
        }

        return null;
    }

    private void Update()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            Release();
            return;
        }

        if (build != null)
        {
            if (!build.isDone)
            {
                return;
            }

            build = null;
            if (!instance.valid)
            {
                instance = NavMesh.AddNavMeshData(data);
            }
        }

        if (Time.time >= nextBuildTime)
        {
            Rebuild();
        }
    }

    public void Rebuild()
    {
        if (build != null)
        {
            return;
        }

        nextBuildTime = Time.time + Mathf.Max(0.25f, rebuildInterval);
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(agentTypeId);
        if (settings.agentTypeID < 0)
        {
            return;
        }

        Vector3 center = transform.position;
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (unit != null && unit.isActiveAndEnabled && unit.HasValidCockpit && unit.IsSimulationActive && unit.gameObject.scene.GetPhysicsScene() == gameObject.scene.GetPhysicsScene())
            {
                sum += unit.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            center = sum / count;
        }

        // 固定网格对齐，舰队细小运动不会使整片瓦片失去增量烘焙缓存。
        center.x = Mathf.Round(center.x / 32f) * 32f;
        center.z = Mathf.Round(center.z / 32f) * 32f;
        center.y = Mathf.Round(center.y / 32f) * 32f;
        Bounds bounds = new Bounds(center, new Vector3(navigationRadius * 2f, navigationHeight, navigationRadius * 2f));
        sources.Clear();
        CollectSources(bounds);
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            Component component = sources[i].component;
            ControlUnit unit = component != null ? component.GetComponentInParent<ControlUnit>() : null;
            Collider collider = component as Collider;
            // 建筑是可破坏的静态导航几何；载具、碎片、触发器不能烘焙成地面或墙。
            if (component == null || component.gameObject.scene.GetPhysicsScene() != gameObject.scene.GetPhysicsScene() || (unit != null && unit.Building == null) || (collider != null && (!bounds.Intersects(collider.bounds) || collider.isTrigger || (collider.attachedRigidbody != null && !collider.attachedRigidbody.isKinematic))))
            {
                sources.RemoveAt(i);
            }
        }

        settings.overrideVoxelSize = true;
        settings.voxelSize = Mathf.Max(0.1f, voxelSize);
        settings.agentRadius = CalculateClearanceRadius();
        BuiltClearanceRadius = settings.agentRadius;
        settings.agentHeight = Mathf.Max(0.1f, clearanceHeight);
        if (data == null)
        {
            data = new NavMeshData(agentTypeId);
            data.name = "Runtime enemy navigation";
        }

        build = NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, bounds);
    }

    private float CalculateClearanceRadius()
    {
        float radius = Mathf.Max(0.1f, clearanceRadius);
        if (!fitActiveShips)
        {
            return radius;
        }

        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            EnemyController controller = unit != null ? unit.GetComponent<EnemyController>() : null;
            if (controller == null || !controller.isActiveAndEnabled || !unit.HasValidCockpit || unit.gameObject.scene.GetPhysicsScene() != gameObject.scene.GetPhysicsScene())
            {
                continue;
            }

            foreach (Collider collider in unit.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != unit.GetComponent<Rigidbody>())
                {
                    continue;
                }

                Bounds bounds = collider.bounds;
                Vector3 offset = bounds.center - unit.transform.position;
                float x = Mathf.Abs(offset.x) + bounds.extents.x;
                float z = Mathf.Abs(offset.z) + bounds.extents.z;
                radius = Mathf.Max(radius, Mathf.Sqrt(x * x + z * z) + shipClearancePadding);
            }
        }

        return radius;
    }

    private void CollectSources(Bounds bounds)
    {
        if (gameObject.scene.GetPhysicsScene() == Physics.defaultPhysicsScene)
        {
            NavMeshBuilder.CollectSources(
                bounds,
                geometryLayers,
                NavMeshCollectGeometry.PhysicsColliders,
                0,
                markups,
                sources);
            return;
        }

        // Unity 的 Bounds 重载只采集默认物理场景；隔离物理场景必须按根对象采集。
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            localSources.Clear();
            NavMeshBuilder.CollectSources(
                root.transform,
                geometryLayers,
                NavMeshCollectGeometry.PhysicsColliders,
                0,
                markups,
                localSources);
            sources.AddRange(localSources);
        }
    }

    private void OnDisable()
    {
        Worlds.Remove(this);
        Release();
    }

    private void Release()
    {
        if (data == null)
        {
            return;
        }

        NavMeshBuilder.Cancel(data);
        build = null;
        instance.Remove();
        Destroy(data);
        data = null;
        nextBuildTime = 0f;
    }
}
