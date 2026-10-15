// 职责：持有一个分块的渲染/碰撞网格，替换时只清理本组件创建的临时Mesh。
// 入口：Rebuild()；烘焙资产引用不销毁，碰撞更新与渲染共用同一表面。
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public sealed class VoxelWorldChunk : MonoBehaviour
{
    [SerializeField]
    private Vector3Int start;
    private Mesh ownedMesh;
    public Vector3Int Start => start;

    public void Configure(Vector3Int cellStart)
    {
        start = cellStart;
    }

    public void Rebuild(VoxelWorldData data, int chunkSize, float cellSize, Material[] materials)
    {
        Mesh mesh = VoxelSurfaceMesher.Build(data, start, chunkSize, cellSize);
        MeshCollider collider = GetComponent<MeshCollider>();
        collider.sharedMesh = null;
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterials = materials;
        if (mesh.vertexCount > 0)
        {
            collider.sharedMesh = mesh;
        }

        ReleaseMesh();
        ownedMesh = mesh;
    }

    public void AdoptBakedMesh(Mesh asset)
    {
        GetComponent<MeshFilter>().sharedMesh = asset;
        GetComponent<MeshCollider>().sharedMesh = asset.vertexCount > 0 ? asset : null;
        ReleaseMesh();
    }

    private void OnDestroy()
    {
        ReleaseMesh();
    }

    private void ReleaseMesh()
    {
        if (ownedMesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(ownedMesh);
        }
        else
        {
            DestroyImmediate(ownedMesh);
        }

        ownedMesh = null;
    }
}
