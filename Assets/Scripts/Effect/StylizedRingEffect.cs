using UnityEngine;

[DisallowMultipleComponent]
public class StylizedRingEffect : MonoBehaviour
{
    private const int MinimumSegments = 24;

    private Mesh glowMesh;
    private Mesh coreMesh;
    private MeshRenderer glowRenderer;
    private MeshRenderer coreRenderer;
    private MaterialPropertyBlock glowProperties;
    private MaterialPropertyBlock coreProperties;
    private int segmentCount = 64;
    private float lineWidth = 0.05f;
    private Color ringColor = Color.cyan;
    private float intensity = 1f;

    public void Configure(int segments, float width)
    {
        segmentCount = Mathf.Max(MinimumSegments, segments);
        lineWidth = Mathf.Max(0.001f, width);
        EnsureInitialized();
        RebuildMeshes();
    }

    public void SetVisual(Color color, float width, float visualIntensity = 1f)
    {
        ringColor = color;
        intensity = Mathf.Clamp01(visualIntensity);

        float requestedWidth = Mathf.Max(0.001f, width);
        if (!Mathf.Approximately(lineWidth, requestedWidth))
        {
            lineWidth = requestedWidth;
            RebuildMeshes();
        }

        ApplyColors();
    }

    public void SetIntensity(float visualIntensity)
    {
        intensity = Mathf.Clamp01(visualIntensity);
        ApplyColors();
    }

    public void SetVisible(bool value)
    {
        EnsureInitialized();
        glowRenderer.enabled = value;
        coreRenderer.enabled = value;
    }

    private void EnsureInitialized()
    {
        if (glowRenderer != null && coreRenderer != null) return;

        CreateLayer("Ring Glow", 0, out glowMesh, out glowRenderer);
        CreateLayer("Ring Core", 1, out coreMesh, out coreRenderer);
        glowProperties = new MaterialPropertyBlock();
        coreProperties = new MaterialPropertyBlock();
        RebuildMeshes();
        ApplyColors();
    }

    private void CreateLayer(string layerName, int sortingOrder, out Mesh mesh, out MeshRenderer meshRenderer)
    {
        GameObject layer = new GameObject(layerName);
        layer.transform.SetParent(transform, false);

        MeshFilter meshFilter = layer.AddComponent<MeshFilter>();
        meshRenderer = layer.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = VisualEffectsManager.GetSharedLineMaterial();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshRenderer.sortingOrder = sortingOrder;

        mesh = new Mesh
        {
            name = layerName + " Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };
        meshFilter.sharedMesh = mesh;
    }

    private void RebuildMeshes()
    {
        if (glowMesh == null || coreMesh == null) return;

        BuildRingMesh(glowMesh, lineWidth * 3.8f, -0.002f);
        BuildRingMesh(coreMesh, lineWidth, 0f);
    }

    private void BuildRingMesh(Mesh mesh, float width, float heightOffset)
    {
        int vertexCount = (segmentCount + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segmentCount * 6];
        float innerRadius = Mathf.Max(0.02f, 1f - width * 0.5f);
        float outerRadius = 1f + width * 0.5f;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            float angle = t * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            int vertexIndex = i * 2;

            vertices[vertexIndex] = new Vector3(x * innerRadius, heightOffset, z * innerRadius);
            vertices[vertexIndex + 1] = new Vector3(x * outerRadius, heightOffset, z * outerRadius);
            uvs[vertexIndex] = new Vector2(t, 0f);
            uvs[vertexIndex + 1] = new Vector2(t, 1f);

            if (i >= segmentCount) continue;

            int triangleIndex = i * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 2;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void ApplyColors()
    {
        if (glowRenderer == null || coreRenderer == null) return;

        Color glowColor = ringColor;
        glowColor.a *= 0.2f * intensity;
        Color coreColor = Color.Lerp(ringColor, Color.white, 0.48f);
        coreColor.a = ringColor.a * intensity;

        SetRendererColor(glowRenderer, glowProperties, glowColor);
        SetRendererColor(coreRenderer, coreProperties, coreColor);
    }

    private static void SetRendererColor(Renderer renderer, MaterialPropertyBlock properties, Color color)
    {
        renderer.GetPropertyBlock(properties);
        properties.SetColor("_Color", color);
        properties.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(properties);
    }

    private void OnDestroy()
    {
        if (glowMesh != null) Destroy(glowMesh);
        if (coreMesh != null) Destroy(coreMesh);
    }
}
