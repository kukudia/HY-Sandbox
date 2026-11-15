// 职责：用有界环形历史绘制实际飞行轨迹，沿累计航程编码固定速度标尺。
// 入口：Sample()/Clear()/DrawGizmos()；由试验场与实战追踪共同调用。
// 生命周期：Dispose()恢复作者材质并释放本实例纹理和材质；不写驾驶或刚体。
using System;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class NavigationFlightTrace : IDisposable
{
    private readonly LineRenderer line;
    private readonly Vector3[] points;
    private readonly Vector3[] ordered;
    private readonly float[] speeds;
    private readonly float[] distances;
    private readonly Color32[] pixels = new Color32[2048];
    private readonly Material originalMaterial;
    private Material material;
    private Texture2D texture;
    private int head;
    private float totalDistance;
    public int Count
    {
        get; private set;
    }

    public NavigationFlightTrace(LineRenderer renderer, int capacity)
    {
        line = renderer;
        capacity = Mathf.Max(2, capacity);
        points = new Vector3[capacity];
        ordered = new Vector3[capacity];
        speeds = new float[capacity];
        distances = new float[capacity];
        originalMaterial = line != null ? line.sharedMaterial : null;
    }

    private int Index(int offset) => (head + offset) % points.Length;
    public void Clear()
    {
        Count = head = 0;
        totalDistance = 0f;
        if (line != null)
        {
            line.positionCount = 0;
        }
    }

    public void Sample(Vector3 position, float speed, float minimumSpacing, float fastSpeed)
    {
        float distance = Count > 0 ? Vector3.Distance(points[Index(Count - 1)], position) : 0f;
        if (Count == 0 || distance >= minimumSpacing)
        {
            totalDistance += distance;
            bool shifted = Count == points.Length;
            if (shifted)
            {
                head = (head + 1) % points.Length;
            }
            else
            {
                Count++;
            }

            int last = Index(Count - 1);
            points[last] = position;
            speeds[last] = speed;
            distances[last] = totalDistance;
            if (line != null)
            {
                line.positionCount = Count;
                if (shifted)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        ordered[i] = points[Index(i)];
                    }

                    line.SetPositions(ordered);
                }
                else
                {
                    line.SetPosition(Count - 1, position);
                }
            }
        }
        else
        {
            speeds[Index(Count - 1)] = speed;
        }

        UpdateColors(fastSpeed);
    }

    private void UpdateColors(float fastSpeed)
    {
        if (line == null || originalMaterial == null || Count == 0)
        {
            return;
        }

        if (texture == null)
        {
            texture = new Texture2D(pixels.Length, 1, TextureFormat.RGBA32, false)
            {
                name = "Navigation speed history",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            material = new Material(originalMaterial);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", texture);
            line.sharedMaterial = material;
            line.textureMode = LineTextureMode.Stretch;
            line.startColor = line.endColor = Color.white;
        }

        float start = distances[Index(0)];
        float length = totalDistance - start;
        int segment = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            float distance = start + length * i / (pixels.Length - 1);
            while (segment + 2 < Count && distance > distances[Index(segment + 1)])
            {
                segment++;
            }

            int a = Index(segment);
            int b = Index(Mathf.Min(segment + 1, Count - 1));
            float speed = Mathf.Lerp(speeds[a], speeds[b], Mathf.InverseLerp(distances[a], distances[b], distance));
            pixels[i] = SpeedColor(speed, fastSpeed);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
    }

    private static Color SpeedColor(float speed, float fastSpeed)
    {
        return Color.Lerp(
            new Color(0.02f, 0.08f, 0.2f),
            new Color(0.65f, 1f, 1f),
            Mathf.Clamp01(speed / Mathf.Max(0.1f, fastSpeed)));
    }

    public void DrawGizmos(float fastSpeed)
    {
        for (int i = 1; i < Count; i++)
        {
            Gizmos.color = SpeedColor(speeds[Index(i)], fastSpeed);
            Gizmos.DrawLine(points[Index(i - 1)], points[Index(i)]);
        }
    }

    public void Dispose()
    {
        if (line != null)
        {
            line.sharedMaterial = originalMaterial;
        }

        Release(material);
        Release(texture);
        material = null;
        texture = null;
    }

    private static void Release(Object value)
    {
        if (value == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(value);
        }
        else
        {
            Object.DestroyImmediate(value);
        }
    }
}
