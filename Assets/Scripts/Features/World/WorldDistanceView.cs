// 职责：用无碰撞的低密度地形延伸视野，并在游玩期间用距离雾隐藏最远边界。
// 入口：Refresh()分帧重建远景，Enter()/Dispose()成对应用和恢复相机/环境设置。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

internal sealed class WorldDistanceView : IDisposable
{
    private readonly ProceduralVoxelWorld world;
    private readonly WorldGenerationSettings settings;
    private readonly GameObject root;
    private readonly MeshFilter filter;
    private Mesh ownedMesh;
    private int builtVersion = -1;
    private Vector2Int builtCenter;
    private Camera camera;
    private bool entered;
    private bool previousFog;
    private FogMode previousMode;
    private float previousStart;
    private float previousEnd;
    private Color previousFogColor;
    private float previousFar;
    private Color previousBackground;
    private CameraClearFlags previousClear;
    public Vector3 Focus
    {
        get; set;
    }
    public bool IsReady => ownedMesh != null && builtVersion == world.ColumnVersion && builtCenter == world.ColumnAt(Focus);

    internal WorldDistanceView(ProceduralVoxelWorld terrain, WorldGenerationSettings configuration)
    {
        world = terrain;
        settings = configuration;
        Focus = world.SpawnPosition;
        root = new GameObject("Distant terrain - visual only");
        root.transform.SetParent(world.transform, false);
        filter = root.AddComponent<MeshFilter>();
        MeshRenderer renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = world.Materials[0];
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    internal IEnumerator Refresh()
    {
        while (true)
        {
            if (!IsReady)
            {
                IEnumerator build = Rebuild(world.ColumnAt(Focus));
                try
                {
                    while (build.MoveNext())
                    {
                        yield return build.Current;
                    }
                }
                finally
                {
                    (build as IDisposable)?.Dispose();
                }
            }

            yield return null;
        }
    }

    private IEnumerator Rebuild(Vector2Int center)
    {
        int version = world.ColumnVersion;
        var detailed = new HashSet<Vector2Int>(world.LoadedColumns);
        float span = settings.ChunkSize * settings.CellSize;
        // One extra band remains beyond full fog, including the camera's follow offset.
        int radius = Mathf.CeilToInt(settings.ViewDistance / span) + 3;
        int subdivisions = Mathf.Clamp(256 / (radius * 2 + 1), 1, 4);
        int width = (radius * 2 + 1) * subdivisions;
        float step = span / subdivisions;
        Vector3 origin = new Vector3((center.x - radius) * span, 0f, (center.y - radius) * span);
        var vertices = new List<Vector3>((width + 1) * (width + 1));
        var triangles = new List<int>(width * width * 6);
        for (int z = 0; z <= width; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                Vector3 point = origin + new Vector3(x * step, 0f, z * step);
                point.y = world.SurfaceHeight(Mathf.FloorToInt(point.x / world.CellSize), Mathf.FloorToInt(point.z / world.CellSize));
                vertices.Add(point);
            }

            if (z % 12 == 0)
            {
                yield return null;
            }
        }

        for (int z = 0; z < width; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int column = center + new Vector2Int(x / subdivisions - radius, z / subdivisions - radius);
                if (detailed.Contains(column))
                {
                    continue;
                }

                int a = z * (width + 1) + x;
                int b = a + width + 1;
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(a + 1);
                triangles.Add(a + 1);
                triangles.Add(b);
                triangles.Add(b + 1);
                // Skirts overlap the side of detailed voxels at the LOD seam; the far mesh never fills mined holes.
                if (x % subdivisions == 0 && detailed.Contains(column + Vector2Int.left))
                {
                    AddSkirt(vertices, triangles, a, b);
                }

                if (x % subdivisions == subdivisions - 1 && detailed.Contains(column + Vector2Int.right))
                {
                    AddSkirt(vertices, triangles, b + 1, a + 1);
                }

                if (z % subdivisions == 0 && detailed.Contains(column + Vector2Int.down))
                {
                    AddSkirt(vertices, triangles, a + 1, a);
                }

                if (z % subdivisions == subdivisions - 1 && detailed.Contains(column + Vector2Int.up))
                {
                    AddSkirt(vertices, triangles, b, b + 1);
                }
            }

            if (z % 16 == 0)
            {
                yield return null;
            }
        }

        var mesh = new Mesh
        {
            name = "World distant terrain",
            indexFormat = IndexFormat.UInt32
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
        if (ownedMesh != null)
        {
            Object.Destroy(ownedMesh);
        }

        ownedMesh = mesh;
        builtVersion = version;
        builtCenter = center;
    }

    private void AddSkirt(List<Vector3> vertices, List<int> triangles, int a, int b)
    {
        int start = vertices.Count;
        vertices.Add(vertices[a] - Vector3.up * settings.CellSize * 4f);
        vertices.Add(vertices[b] - Vector3.up * settings.CellSize * 4f);
        triangles.Add(a);
        triangles.Add(start);
        triangles.Add(b);
        triangles.Add(b);
        triangles.Add(start);
        triangles.Add(start + 1);
    }

    internal void Enter(Camera view)
    {
        if (entered || view == null)
        {
            return;
        }

        entered = true;
        camera = view;
        previousFog = RenderSettings.fog;
        previousMode = RenderSettings.fogMode;
        previousStart = RenderSettings.fogStartDistance;
        previousEnd = RenderSettings.fogEndDistance;
        previousFogColor = RenderSettings.fogColor;
        previousFar = camera.farClipPlane;
        previousBackground = camera.backgroundColor;
        previousClear = camera.clearFlags;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = settings.DistanceFogColor;
        RenderSettings.fogStartDistance = settings.ViewDistance * 0.45f;
        RenderSettings.fogEndDistance = settings.ViewDistance;
        camera.farClipPlane = Mathf.Max(previousFar, settings.ViewDistance + 150f);
        camera.backgroundColor = RenderSettings.fogColor;
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    public void Dispose()
    {
        if (entered)
        {
            RenderSettings.fog = previousFog;
            RenderSettings.fogMode = previousMode;
            RenderSettings.fogStartDistance = previousStart;
            RenderSettings.fogEndDistance = previousEnd;
            RenderSettings.fogColor = previousFogColor;
            if (camera != null)
            {
                camera.farClipPlane = previousFar;
                camera.backgroundColor = previousBackground;
                camera.clearFlags = previousClear;
            }

            entered = false;
        }

        if (root != null)
        {
            root.SetActive(false);
            Object.Destroy(root);
        }

        if (ownedMesh != null)
        {
            Object.Destroy(ownedMesh);
        }
    }
}
