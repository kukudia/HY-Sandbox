// 职责：仅为暴露的体素面生成三材质分块网格，边界查询邻块以避免内部重复面。
// 入口：Build()；调用者拥有返回Mesh并负责销毁，不修改源数据。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new[]
        {
            new List<int>(),
            new List<int>(),
            new List<int>()
        };
        Vector3Int end = Vector3Int.Min(start + Vector3Int.one * chunkSize, data.Size);
        for (int z = start.z; z < end.z; z++)
        {
            for (int y = start.y; y < end.y; y++)
            {
                for (int x = start.x; x < end.x; x++)
                {
                    var cell = new Vector3Int(x, y, z);
                    byte material = data.Get(cell);
                    if (material == 0)
                    {
                        continue;
                    }

                    for (int face = 0; face < 6; face++)
                    {
                        if (data.Get(cell + Directions[face]) != 0)
                        {
                            continue;
                        }

                        int index = vertices.Count;
                        float shade = 0.9f + ((x * 17 + y * 7 + z * 13) % 11) * 0.01f;
                        for (int corner = 0; corner < 4; corner++)
                        {
                            vertices.Add(((Vector3)(cell - start) + Corners[face][corner]) * cellSize);
                            normals.Add(Directions[face]);
                            uvs.Add(new Vector2(corner == 1 || corner == 2 ? 1 : 0, corner >= 2 ? 1 : 0));
                            colors.Add(new Color(shade, shade, shade, 1));
                        }

                        List<int> indices = triangles[material - 1];
                        indices.Add(index);
                        indices.Add(index + 1);
                        indices.Add(index + 2);
                        indices.Add(index);
                        indices.Add(index + 2);
                        indices.Add(index + 3);
                    }
                }
            }
        }

        var mesh = new Mesh
        {
            name = "VoxelChunk_" + start,
            indexFormat = IndexFormat.UInt32
        };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.subMeshCount = 3;
        for (int i = 0; i < 3; i++)
        {
            mesh.SetTriangles(triangles[i], i);
        }

        mesh.RecalculateBounds();
        return mesh;
    }
}
