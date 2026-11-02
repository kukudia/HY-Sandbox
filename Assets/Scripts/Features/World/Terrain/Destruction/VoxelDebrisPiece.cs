// 职责：拥有有限寿命的金属碎片刚体及单元BoxCollider，清理自身网格。
// 入口：Initialize()；碎片不是可建造Block，不进入玩家分组/供电/存档。
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(Rigidbody))]
public sealed class VoxelDebrisPiece : MonoBehaviour
{
    private Mesh ownedMesh;
    private float expiresAt;
    public void Initialize(List<Vector3Int> cells, float cellSize, Material[] materials, float lifetime)
    {
        Vector3Int minimum = cells[0];
        Vector3Int maximum = cells[0];
        foreach (Vector3Int cell in cells)
        {
            minimum = Vector3Int.Min(minimum, cell);
            maximum = Vector3Int.Max(maximum, cell);
        }

        // Allocate only the connected fragment bounds, not a copy of the entire world.
        var fragment = new VoxelWorldData();
        fragment.InitializeEmpty(maximum - minimum + Vector3Int.one);
        foreach (Vector3Int cell in cells)
        {
            fragment.Set(cell - minimum, 3);
        }

        ownedMesh = VoxelSurfaceMesher.Build(fragment, Vector3Int.zero, 128, cellSize);
        ownedMesh.name = "VoxelDebris";
        GetComponent<MeshFilter>().sharedMesh = ownedMesh;
        GetComponent<MeshRenderer>().sharedMaterials = materials;
        foreach (Vector3Int cell in cells)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.center = ((Vector3)(cell - minimum) + Vector3.one * 0.5f) * cellSize;
            collider.size = Vector3.one * cellSize * 0.98f;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        body.mass = cells.Count * cellSize * cellSize * cellSize * 40f;
        body.linearDamping = 0.2f;
        body.angularDamping = 0.5f;
        body.maxDepenetrationVelocity = 3f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        expiresAt = Time.time + Mathf.Max(1f, lifetime);
    }

    private void Update()
    {
        if (Time.time >= expiresAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (ownedMesh != null)
        {
            Destroy(ownedMesh);
        }
    }
}
