// 职责：按列缓存无碰撞体素过渡环，再衔接低密度远景与距离雾。
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
    private readonly Dictionary<Vector2Int, MeshFilter> voxelRing = new Dictionary<Vector2Int, MeshFilter>();
    private readonly HashSet<Vector2Int> visibleRing = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> expiredRing = new List<Vector2Int>();
    private int ringVisibilityVersion = -1;
    private int builtVersion = -1;
    private Vector2Int builtCenter;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private int baseVertexCount;
    internal bool AllowRefresh
    {
        get; set;
    }

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
    public bool IsReady => ownedMesh != null && builtVersion == world.ColumnVersion && builtCenter == CenterAtFocus();

    private Vector2Int CenterAtFocus()
    {
        Vector2Int column = world.ColumnAt(Focus);
        return new Vector2Int(Mathf.FloorToInt(column.x / 4f) * 4, Mathf.FloorToInt(column.y / 4f) * 4);
    }

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
        renderer.receiveShadows = true;
    }

    internal IEnumerator Refresh()
    {
        while (true)
        {
            UpdateRingVisibility();
            if (AllowRefresh && !IsReady)
            {
                IEnumerator build = Rebuild(CenterAtFocus());
                try
                {
                    while (build.MoveNext())
                    {
                        UpdateRingVisibility();
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
        float span = settings.ChunkSize * settings.CellSize;
        // One extra band remains beyond full fog, including the camera's follow offset.
        int radius = Mathf.CeilToInt(settings.ViewDistance / span) + 6;
        int subdivisions = 4;
        int width = (radius * 2 + 1) * subdivisions;
        float step = span / subdivisions;
        Vector3 origin = new Vector3((center.x - radius) * span, 0f, (center.y - radius) * span);
        // 地形采样只在关注点跨过四列时更新；近景变化只更新空洞索引，复用顶点与Mesh。
        bool resample = ownedMesh == null || builtCenter != center;
        if (resample)
        {
            vertices.Clear();
            float sliceStarted = Time.realtimeSinceStartup;
            for (int z = 0; z <= width; z++)
            {
                for (int x = 0; x <= width; x++)
                {
                    Vector3 point = origin + new Vector3(x * step, 0f, z * step);
                    point.y = world.SurfaceHeight(Mathf.FloorToInt(point.x / world.CellSize), Mathf.FloorToInt(point.z / world.CellSize));
                    vertices.Add(point);
                }

                if ((Time.realtimeSinceStartup - sliceStarted) * 1000f >= settings.LoadingFrameBudgetMilliseconds)
                {
                    yield return null;
                    sliceStarted = Time.realtimeSinceStartup;
                }
            }

            baseVertexCount = vertices.Count;
        }
        else if (vertices.Count > baseVertexCount)
        {
            vertices.RemoveRange(baseVertexCount, vertices.Count - baseVertexCount);
        }

        int ringRadius = Mathf.Max(settings.UnloadRadius + 4, Mathf.CeilToInt(settings.ViewDistance * 0.45f / span) + 3);
        // Cached visual columns have no collision or gameplay state. Build at most one column
        // past the time budget; only the final mask/visibility swap is atomic.
        float ringStarted = Time.realtimeSinceStartup;
        for (int z = -ringRadius; z <= ringRadius; z++)
        {
            for (int x = -ringRadius; x <= ringRadius; x++)
            {
                Vector2Int coordinate = center + new Vector2Int(x, z);
                if (!voxelRing.ContainsKey(coordinate))
                {
                    voxelRing.Add(coordinate, BuildRingColumn(coordinate));
                }

                if ((Time.realtimeSinceStartup - ringStarted) * 1000f >= settings.LoadingFrameBudgetMilliseconds)
                {
                    yield return null;
                    ringStarted = Time.realtimeSinceStartup;
                }
            }
        }

        // Capture topology only after sampling. No yield between this snapshot and upload:
        // a loading/unloading column cannot leave a stale hole or cover an excavated near column.
        int version = world.ColumnVersion;
        var detailed = new HashSet<Vector2Int>(world.LoadedColumns);
        visibleRing.Clear();
        foreach (Vector2Int coordinate in voxelRing.Keys)
        {
            if (Mathf.Max(Mathf.Abs(coordinate.x - center.x), Mathf.Abs(coordinate.y - center.y)) <= ringRadius)
            {
                visibleRing.Add(coordinate);
                detailed.Add(coordinate);
            }
        }

        triangles.Clear();
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
        }

        if (ownedMesh == null)
        {
            ownedMesh = new Mesh
            {
                name = "World distant terrain",
                indexFormat = IndexFormat.UInt32
            };
            ownedMesh.MarkDynamic();
            filter.sharedMesh = ownedMesh;
        }

        ownedMesh.Clear();
        ownedMesh.SetVertices(vertices);
        ownedMesh.SetTriangles(triangles, 0);
        ownedMesh.RecalculateNormals();
        ownedMesh.RecalculateBounds();
        builtVersion = version;
        builtCenter = center;
        ringVisibilityVersion = -1;
        UpdateRingVisibility();
        ReleaseExpiredRing(center, ringRadius + 2);
    }

    private void UpdateRingVisibility()
    {
        if (ringVisibilityVersion == world.ColumnVersion)
        {
            return;
        }

        ringVisibilityVersion = world.ColumnVersion;
        foreach (KeyValuePair<Vector2Int, MeshFilter> column in voxelRing)
        {
            bool active = visibleRing.Contains(column.Key) && !world.HasColumn(column.Key);
            if (column.Value.gameObject.activeSelf != active)
            {
                column.Value.gameObject.SetActive(active);
            }
        }
    }

    private void ReleaseExpiredRing(Vector2Int center, int radius)
    {
        expiredRing.Clear();
        foreach (Vector2Int coordinate in voxelRing.Keys)
        {
            if (Mathf.Max(Mathf.Abs(coordinate.x - center.x), Mathf.Abs(coordinate.y - center.y)) > radius)
            {
                expiredRing.Add(coordinate);
            }
        }

        foreach (Vector2Int coordinate in expiredRing)
        {
            MeshFilter column = voxelRing[coordinate];
            Object.Destroy(column.sharedMesh);
            Object.Destroy(column.gameObject);
            voxelRing.Remove(coordinate);
        }
    }

    private MeshFilter BuildRingColumn(Vector2Int coordinate)
    {
        int size = settings.ChunkSize;
        float cell = world.CellSize;
        Mesh mesh = WorldTerrainTransitionMesh.Build(world, settings, coordinate);
        var column = new GameObject(mesh.name);
        column.SetActive(false);
        column.transform.SetParent(root.transform, false);
        column.transform.localPosition = new Vector3(coordinate.x * size * cell, 0f, coordinate.y * size * cell);
        MeshFilter result = column.AddComponent<MeshFilter>();
        result.sharedMesh = mesh;
        MeshRenderer renderer = column.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new[]
        {
            world.Materials[0],
            world.Materials[1]
        };
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return result;
    }

    private void AddSkirt(List<Vector3> vertices, List<int> triangles, int a, int b)
    {
        Vector3 first = vertices[a];
        Vector3 last = vertices[b];
        // Follow the detailed boundary at cell resolution. A fixed-depth skirt can end
        // above a steep neighbouring cell and expose the background through the LOD seam.
        Vector3 inward = Vector3.Cross(last - first, Vector3.up).normalized;
        int segments = Mathf.Max(
            1,
            Mathf.RoundToInt(Mathf.Max(Mathf.Abs(last.x - first.x), Mathf.Abs(last.z - first.z)) / world.CellSize));
        for (int i = 0; i < segments; i++)
        {
            Vector3 from = Vector3.Lerp(first, last, i / (float)segments);
            Vector3 to = Vector3.Lerp(first, last, (i + 1) / (float)segments);
            Vector3 sample = (from + to) * 0.5f + inward * world.CellSize * 0.5f;
            float detailedHeight = world.SurfaceHeight(Mathf.FloorToInt(sample.x / world.CellSize), Mathf.FloorToInt(sample.z / world.CellSize));
            float bottom = Mathf.Min(Mathf.Min(from.y, to.y), detailedHeight) - world.CellSize * 2f;
            int start = vertices.Count;
            vertices.Add(from);
            vertices.Add(new Vector3(from.x, bottom, from.z));
            vertices.Add(to);
            vertices.Add(new Vector3(to.x, bottom, to.z));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);
        }
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

        foreach (MeshFilter column in voxelRing.Values)
        {
            Object.Destroy(column.sharedMesh);
        }

        voxelRing.Clear();
        visibleRing.Clear();
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
