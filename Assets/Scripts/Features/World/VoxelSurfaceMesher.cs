// 职责：从一格邻域快照合并同材质的暴露面，生成三材质贪心网格。
// 入口：Build()用于同步编辑；Generate()只计算数组，可在工作线程运行，Unity提交留在主线程。
using UnityEngine;

public static class VoxelSurfaceMesher
{
    private static readonly Vector3Int[] Directions =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward,
        Vector3Int.back
    };
    private static readonly Vector3[][] Corners =
    {
        new[]
        {
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1),
            new Vector3(1, 0, 1)
        },
        new[]
        {
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 1),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 0)
        },
        new[]
        {
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0)
        },
        new[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1)
        },
        new[]
        {
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1),
            new Vector3(0, 0, 1)
        },
        new[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 0)
        }
    };
    public static Mesh Build(VoxelWorldData data, Vector3Int start, int chunkSize, float cellSize)
    {
        return Generate(VoxelChunkSnapshot.Capture(data, start, chunkSize), cellSize).Upload(start);
    }

    /// <summary>无Unity对象、噪声或共享世界读取。每次调用独享mask与结果，允许后台计算。</summary>
    public static VoxelMeshData Generate(VoxelChunkSnapshot snapshot, float cellSize)
    {
        int size = snapshot.Size;
        var result = new VoxelMeshData();
        var mask = new byte[size * size];
        for (int face = 0; face < 6; face++)
        {
            Vector3Int normal = Directions[face];
            Vector3Int axis = new Vector3Int(System.Math.Abs(normal.x), System.Math.Abs(normal.y), System.Math.Abs(normal.z));
            Vector3Int u = Vector3Int.RoundToInt(Corners[face][1] - Corners[face][0]);
            Vector3Int v = Vector3Int.RoundToInt(Corners[face][3] - Corners[face][0]);
            Vector3Int offset = new Vector3Int(u.x + v.x < 0 ? size - 1 : 0, u.y + v.y < 0 ? size - 1 : 0, u.z + v.z < 0 ? size - 1 : 0);
            int stepU = snapshot.Step(u);
            int stepV = snapshot.Step(v);
            int stepNormal = snapshot.Step(normal);
            for (int slice = 0; slice < size; slice++)
            {
                Vector3Int origin = offset + axis * slice;
                int first = snapshot.Index(origin);
                for (int row = 0; row < size; row++)
                {
                    int index = first + stepV * row;
                    int maskIndex = row * size;
                    for (int column = 0; column < size; column++)
                    {
                        byte material = snapshot.Get(index);
                        mask[maskIndex + column] = material != 0 && snapshot.Get(index + stepNormal) == 0 ? material : (byte)0;
                        index += stepU;
                    }
                }

                MergeSlice(
                    result,
                    mask,
                    size,
                    face,
                    origin,
                    u,
                    v,
                    cellSize);
            }
        }

        return result;
    }

    private static void MergeSlice(
        VoxelMeshData result,
        byte[] mask,
        int size,
        int face,
        Vector3Int origin,
        Vector3Int u,
        Vector3Int v,
        float scale)
    {
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                byte material = mask[row * size + column];
                if (material == 0)
                {
                    continue;
                }

                int width = 1;
                while (column + width < size && mask[row * size + column + width] == material)
                {
                    width++;
                }

                int height = 1;
                bool matches = true;
                while (row + height < size && matches)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (mask[(row + height) * size + column + x] != material)
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        height++;
                    }
                }

                Vector3 corner = (Vector3)(origin + u * column + v * row) + Corners[face][0];
                AddQuad(
                    result,
                    material,
                    corner,
                    u * width,
                    v * height,
                    Directions[face],
                    width,
                    height,
                    scale);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        mask[(row + y) * size + column + x] = 0;
                    }
                }
            }
        }
    }

    private static void AddQuad(
        VoxelMeshData mesh,
        byte material,
        Vector3 corner,
        Vector3 u,
        Vector3 v,
        Vector3 normal,
        int width,
        int height,
        float scale)
    {
        int index = mesh.Vertices.Count;
        mesh.Vertices.Add(corner * scale);
        mesh.Vertices.Add((corner + u) * scale);
        mesh.Vertices.Add((corner + u + v) * scale);
        mesh.Vertices.Add((corner + v) * scale);
        mesh.UVs.Add(Vector2.zero);
        mesh.UVs.Add(new Vector2(width, 0));
        mesh.UVs.Add(new Vector2(width, height));
        mesh.UVs.Add(new Vector2(0, height));
        for (int i = 0; i < 4; i++)
        {
            mesh.Normals.Add(normal);
        }

        var indices = mesh.Triangles[material - 1];
        indices.Add(index);
        indices.Add(index + 1);
        indices.Add(index + 2);
        indices.Add(index);
        indices.Add(index + 2);
        indices.Add(index + 3);
    }
}
