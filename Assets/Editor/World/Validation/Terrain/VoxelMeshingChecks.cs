// 职责：以逐体素暴露面为独立基准，验证贪心网格覆盖、材质、法线、接缝与快照隔离。
// 入口：Run()；只创建临时数据/Mesh，报告保存到Temp，不修改场景和玩家数据。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VoxelMeshingChecks
{
    private static int checks;
    public static string Run()
    {
        checks = 0;
        var data = new VoxelWorldData();
        data.InitializeEmpty(new Vector3Int(32, 16, 16));
        for (int z = 0; z < 16; z++)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    data.Set(new Vector3Int(x, y, z), (byte)(x < 16 ? 1 : 2));
                }
            }
        }

        Mesh slab = VoxelSurfaceMesher.Build(data, Vector3Int.zero, 16, 1f);
        Check(slab.vertexCount == 20, "solid half slab merges to five quads; adjacent chunk hides sixth");
        Object.DestroyImmediate(slab);
        Verify(data, Vector3Int.zero, 16);
        Verify(data, new Vector3Int(16, 0, 0), 16);
        var random = new System.Random(8817);
        for (int i = 0; i < 2500; i++)
        {
            data.Set(new Vector3Int(random.Next(32), random.Next(16), random.Next(16)), (byte)random.Next(4));
        }

        Verify(data, Vector3Int.zero, 16);
        Verify(data, new Vector3Int(16, 0, 0), 16);
        var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>("Assets/World/WorldGeneration.asset");
        data.InitializeStreaming(new StreamingWorldTerrain(settings, 14721));
        data.Set(new Vector3Int(-1, 6, -1), 3);
        data.Set(new Vector3Int(-2, 6, -1), 0);
        Verify(data, new Vector3Int(-16, 0, -16), 16);
        VoxelChunkSnapshot snapshot = VoxelChunkSnapshot.Capture(data, Vector3Int.zero, 16);
        Mesh before = VoxelSurfaceMesher.Generate(snapshot, 1f).Upload(Vector3Int.zero);
        data.Set(new Vector3Int(1, 6, 1), data.Get(new Vector3Int(1, 6, 1)) == 0 ? (byte)1 : (byte)0);
        Mesh after = System.Threading.Tasks.Task.Run(() => VoxelSurfaceMesher.Generate(snapshot, 1f)).Result.Upload(Vector3Int.zero);
        Check(
            before.vertexCount == after.vertexCount && before.triangles.Length == after.triangles.Length,
            "worker owns isolated snapshot after live data edit");
        Object.DestroyImmediate(before);
        Object.DestroyImmediate(after);
        string report = $"Passed {checks} assertions: slab merging, exact visible face coverage, three materials, normals/winding, UV tiling, negative coordinates, chunk halo and worker snapshot isolation.";
        Directory.CreateDirectory("Temp/WorldPlayer");
        File.WriteAllText("Temp/WorldPlayer/meshing-checks.txt", report);
        return report;
    }

    private static void Verify(VoxelWorldData data, Vector3Int start, int size)
    {
        var expected = new HashSet<string>();
        Vector3Int[] directions =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.forward,
            Vector3Int.back
        };
        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var cell = start + new Vector3Int(x, y, z);
                    byte material = data.Get(cell);
                    if (material == 0)
                    {
                        continue;
                    }

                    for (int face = 0; face < 6; face++)
                    {
                        if (data.Get(cell + directions[face]) == 0)
                        {
                            expected.Add(Key(cell, directions[face], material));
                        }
                    }
                }
            }
        }

        Mesh mesh = VoxelSurfaceMesher.Build(data, start, size, 1f);
        try
        {
            var actual = new HashSet<string>();
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uv = mesh.uv;
            for (int m = 0; m < 3; m++)
            {
                int[] triangles = mesh.GetTriangles(m);
                for (int i = 0; i < triangles.Length; i += 6)
                {
                    int a = triangles[i];
                    Vector3 origin = vertices[a];
                    Vector3 u = vertices[a + 1] - origin;
                    Vector3 v = vertices[a + 3] - origin;
                    Vector3 normal = normals[a];
                    Check(Vector3.Dot(Vector3.Cross(u, v).normalized, normal) > .999f, "outward winding");
                    Check(uv[a + 2] == new Vector2(u.magnitude, v.magnitude), "tiled UV dimensions");
                    for (int y = 0; y < Mathf.RoundToInt(v.magnitude); y++)
                    {
                        for (int x = 0; x < Mathf.RoundToInt(u.magnitude); x++)
                        {
                            Vector3 point = origin + u.normalized * (x + .5f) + v.normalized * (y + .5f) - normal * .5f;
                            Vector3Int cell = start + Vector3Int.FloorToInt(point);
                            Check(actual.Add(Key(cell, Vector3Int.RoundToInt(normal), m + 1)), "no duplicated face coverage");
                        }
                    }
                }
            }

            Check(actual.SetEquals(expected), "exact material and face coverage against independent voxel reference");
        }
        finally
        {
            Object.DestroyImmediate(mesh);
        }
    }

    private static string Key(Vector3Int cell, Vector3Int normal, int material)
    {
        return cell + ":" + normal + ":" + material;
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
        {
            throw new InvalidOperationException(message);
        }

        checks++;
    }
}
