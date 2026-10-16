// 职责：生成无碰撞体素过渡列，保留格级轮廓并合并同高顶面。
// 入口：WorldDistanceView 分帧调用 Build()；返回的 Mesh 由调用者释放。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

internal static class WorldTerrainTransitionMesh
{
    internal static Mesh Build(ProceduralVoxelWorld world, WorldGenerationSettings settings, Vector2Int coordinate)
    {
        int size = settings.ChunkSize;
        float cell = world.CellSize;
        var heights = new float[size + 2, size + 2];
        for (int z = -1; z <= size; z++)
        {
            for (int x = -1; x <= size; x++)
            {
                heights[x + 1, z + 1] = world.SurfaceHeight(coordinate.x * size + x, coordinate.y * size + z);
            }
        }

        var points = new List<Vector3>();
        var faces = new List<int>();
        var soilFaces = new List<int>();
        for (int z = 0; z < size; z++)
        {
            // Merge flat runs without smoothing the voxel silhouette at the near seam.
            for (int x = 0; x < size;)
            {
                int end = x + 1;
                float height = heights[x + 1, z + 1];
                while (end < size && heights[end + 1, z + 1] == height)
                {
                    end++;
                }

                AddRingFace(
                    points,
                    faces,
                    new Vector3(x * cell, height, z * cell),
                    new Vector3(x * cell, height, (z + 1) * cell),
                    new Vector3(end * cell, height, (z + 1) * cell),
                    new Vector3(end * cell, height, z * cell));
                x = end;
            }

            for (int x = 0; x < size; x++)
            {
                float height = heights[x + 1, z + 1];
                float x0 = x * cell;
                float x1 = (x + 1) * cell;
                float z0 = z * cell;
                float z1 = (z + 1) * cell;
                AddRingWall(
                    points,
                    faces,
                    soilFaces,
                    cell,
                    new Vector3(x0, height, z0),
                    new Vector3(x0, height, z1),
                    heights[x, z + 1]);
                AddRingWall(
                    points,
                    faces,
                    soilFaces,
                    cell,
                    new Vector3(x1, height, z1),
                    new Vector3(x1, height, z0),
                    heights[x + 2, z + 1]);
                AddRingWall(
                    points,
                    faces,
                    soilFaces,
                    cell,
                    new Vector3(x1, height, z0),
                    new Vector3(x0, height, z0),
                    heights[x + 1, z]);
                AddRingWall(
                    points,
                    faces,
                    soilFaces,
                    cell,
                    new Vector3(x0, height, z1),
                    new Vector3(x1, height, z1),
                    heights[x + 1, z + 2]);
            }
        }

        var mesh = new Mesh
        {
            name = "Visual voxel ring " + coordinate,
            indexFormat = IndexFormat.UInt32
        };
        mesh.SetVertices(points);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(faces, 0);
        mesh.SetTriangles(soilFaces, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        return mesh;
    }

    private static void AddRingWall(
        List<Vector3> points,
        List<int> faces,
        List<int> soilFaces,
        float cell,
        Vector3 first,
        Vector3 last,
        float bottom)
    {
        if (bottom >= first.y)
        {
            return;
        }

        float grassBottom = Mathf.Max(bottom, first.y - cell * 2f);
        AddRingFace(
            points,
            faces,
            new Vector3(first.x, grassBottom, first.z),
            new Vector3(last.x, grassBottom, last.z),
            last,
            first);
        if (bottom < grassBottom)
        {
            AddRingFace(
                points,
                soilFaces,
                new Vector3(first.x, bottom, first.z),
                new Vector3(last.x, bottom, last.z),
                new Vector3(last.x, grassBottom, last.z),
                new Vector3(first.x, grassBottom, first.z));
        }
    }

    private static void AddRingFace(
        List<Vector3> points,
        List<int> faces,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int start = points.Count;
        points.Add(a);
        points.Add(b);
        points.Add(c);
        points.Add(d);
        faces.Add(start);
        faces.Add(start + 1);
        faces.Add(start + 2);
        faces.Add(start);
        faces.Add(start + 2);
        faces.Add(start + 3);
    }
}
