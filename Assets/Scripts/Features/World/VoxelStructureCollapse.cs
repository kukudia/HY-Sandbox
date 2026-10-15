// 职责：破坏后按预算判定金属支撑，将少量孤立结构转为有界物理碎片。
// 入口：RequestCheck()由世界成功破坏调用，LateUpdate避开未刷新的静态碰撞。
// 约束：仅土/岩邻接支撑，小片段研究模型，不等于全地形结构力学求解。
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProceduralVoxelWorld))]
public sealed class VoxelStructureCollapse : MonoBehaviour
{
    [SerializeField, Range(1, 64)]
    private int maximumCellsPerPiece = 32;
    [SerializeField, Range(1, 24)]
    private int maximumPieces = 12;
    [SerializeField, Min(1f)]
    private float debrisLifetime = 12f;
    [SerializeField, Min(0.05f)]
    private float checkInterval = 0.25f;
    private ProceduralVoxelWorld world;
    private readonly List<VoxelDebrisPiece> pieces = new List<VoxelDebrisPiece>();
    private bool requested;
    private float nextCheck;
    public int DetachedCells
    {
        get; private set;
    }
    public int ActivePieces => pieces.Count;

    private void Awake()
    {
        world = GetComponent<ProceduralVoxelWorld>();
    }

    public void RequestCheck()
    {
        requested = true;
    }

    private void LateUpdate()
    {
        pieces.RemoveAll(piece => piece == null);
        if (!requested || Time.time < nextCheck || world.PendingChunks > 0)
        {
            return;
        }

        requested = false;
        nextCheck = Time.time + checkInterval;
        ResolveDetached();
    }

    public void ResolveDetached()
    {
        pieces.RemoveAll(piece => piece == null);
        int room = maximumPieces - pieces.Count;
        if (room <= 0)
        {
            return;
        }

        foreach (List<Vector3Int> cells in VoxelStructureSupport.FindDetached(world.Data, maximumCellsPerPiece, room))
        {
            Vector3Int minimum = cells[0];
            foreach (Vector3Int cell in cells)
            {
                minimum = Vector3Int.Min(minimum, cell);
            }

            var obj = new GameObject("WorldLab metal debris");
            obj.transform.SetPositionAndRotation(world.transform.TransformPoint((Vector3)minimum * world.CellSize), world.transform.rotation);
            obj.transform.localScale = world.transform.lossyScale;
            VoxelDebrisPiece piece = obj.AddComponent<VoxelDebrisPiece>();
            piece.Initialize(cells, world.CellSize, world.Materials, debrisLifetime);
            pieces.Add(piece);
            foreach (Vector3Int cell in cells)
            {
                world.SetCell(cell, 0);
                DetachedCells++;
            }
        }

        // Remove source collider cells before the next physical step; default piece cap bounds this burst.
        world.FlushDirty();
    }

    public void ClearDebris()
    {
        requested = false;
        foreach (VoxelDebrisPiece piece in pieces)
        {
            if (piece != null)
            {
                piece.gameObject.SetActive(false);
                Destroy(piece.gameObject);
            }
        }

        pieces.Clear();
        DetachedCells = 0;
    }

    private void OnDisable()
    {
        ClearDebris();
    }
}
