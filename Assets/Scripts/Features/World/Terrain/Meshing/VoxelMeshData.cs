// 职责：保存不可变邻域快照和纯CPU网格结果，将工作线程计算与Unity对象写入分开。
// 入口：Capture()在主线程读取体素；Upload()在主线程创建Mesh，快照不引用可变世界。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class VoxelMeshData
{
    internal readonly List<Vector3> Vertices = new List<Vector3>();
    internal readonly List<Vector3> Normals = new List<Vector3>();
    internal readonly List<Vector2> UVs = new List<Vector2>();
    internal readonly List<int>[] Triangles =
    {
        new List<int>(),
        new List<int>(),
        new List<int>()
    };
    public Mesh Upload(Vector3Int start)
    {
        var mesh = new Mesh
        {
            name = "VoxelChunk_" + start,
            indexFormat = Vertices.Count <= 65535 ? IndexFormat.UInt16 : IndexFormat.UInt32
        };
        mesh.SetVertices(Vertices);
        mesh.SetNormals(Normals);
        mesh.SetUVs(0, UVs);
        mesh.subMeshCount = 3;
        for (int i = 0; i < 3; i++)
        {
            mesh.SetTriangles(Triangles[i], i, false);
        }

        mesh.RecalculateBounds();
        return mesh;
    }
}

public sealed class VoxelChunkSnapshot
{
    private readonly byte[] cells;
    private readonly int stride;
    public int Size
    {
        get;
    }

    private VoxelChunkSnapshot(int size)
    {
        Size = size;
        stride = size + 2;
        cells = new byte[stride * stride * stride];
    }

    /// <summary>一格halo包含邻块边界；只在主线程调用，每个坐标仅采样一次。</summary>
    public static VoxelChunkSnapshot Capture(VoxelWorldData data, Vector3Int start, int size)
    {
        var snapshot = new VoxelChunkSnapshot(size);
        for (int z = -1; z <= size; z++)
        {
            for (int y = -1; y <= size; y++)
            {
                for (int x = -1; x <= size; x++)
                {
                    snapshot.cells[((z + 1) * snapshot.stride + y + 1) * snapshot.stride + x + 1] = data.Get(start + new Vector3Int(x, y, z));
                }
            }
        }

        return snapshot;
    }

    internal byte Get(Vector3Int cell)
    {
        return cells[Index(cell)];
    }

    internal int Index(Vector3Int cell)
    {
        return ((cell.z + 1) * stride + cell.y + 1) * stride + cell.x + 1;
    }

    internal int Step(Vector3Int direction)
    {
        return (direction.z * stride + direction.y) * stride + direction.x;
    }

    internal byte Get(int index)
    {
        return cells[index];
    }
}
