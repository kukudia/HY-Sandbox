using UnityEngine;

[DisallowMultipleComponent]
public class StylizedBeamEffect : MonoBehaviour
{
    private const int MinimumSegments = 2;

    private Mesh glowMesh;
    private Mesh coreMesh;
    private MeshRenderer glowRenderer;
    private MeshRenderer coreRenderer;
    private MaterialPropertyBlock glowProperties;
    private MaterialPropertyBlock coreProperties;

    private Vector3[] centerPoints;
    private Vector3[] glowVertices;
    private Vector3[] coreVertices;
    private Vector2[] uvs;
    private int[] triangles;

    private Vector3 startPoint;
    private Vector3 endPoint;
    private Color beamColor = Color.cyan;
    private float coreWidth = 0.04f;
    private float glowWidthMultiplier = 4f;
    private float noiseAmplitude;
    private float noiseFrequency = 3f;
    private float flowSpeed = 8f;
    private float intensity = 1f;
    private int segmentCount = MinimumSegments;
    private bool visible;
    private bool geometryDirty = true;

    public void Configure(float width, float glowMultiplier, int segments, float noise, float frequency, float speed)
    {
        coreWidth = Mathf.Max(0.001f, width);
        glowWidthMultiplier = Mathf.Max(1f, glowMultiplier);
        noiseAmplitude = Mathf.Max(0f, noise);
        noiseFrequency = Mathf.Max(0.1f, frequency);
        flowSpeed = speed;

        int requestedSegments = Mathf.Max(MinimumSegments, segments);
        if (segmentCount != requestedSegments)
        {
            segmentCount = requestedSegments;
            AllocateGeometry();
        }

        EnsureInitialized();
        geometryDirty = true;
    }

    public void SetEndpoints(Vector3 start, Vector3 end)
    {
        startPoint = start;
        endPoint = end;
        geometryDirty = true;
    }

    public void SetColor(Color color)
    {
        beamColor = color;
        ApplyColors();
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
        ApplyColors();
    }

    public void SetVisible(bool value)
    {
        EnsureInitialized();
        visible = value;
        glowRenderer.enabled = value;
        coreRenderer.enabled = value;
        geometryDirty |= value;
    }

    private void LateUpdate()
    {
        if (!visible) return;

        if (noiseAmplitude > 0f || geometryDirty)
        {
            UpdateGeometry();
        }
    }

    private void EnsureInitialized()
    {
        if (glowRenderer != null && coreRenderer != null) return;

        CreateLayer("Beam Glow", 0, out glowMesh, out glowRenderer);
        CreateLayer("Beam Core", 1, out coreMesh, out coreRenderer);
        glowProperties = new MaterialPropertyBlock();
        coreProperties = new MaterialPropertyBlock();

        if (centerPoints == null || centerPoints.Length != segmentCount)
        {
            AllocateGeometry();
        }

        ApplyColors();
        SetVisible(false);
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
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;
    }

    private void AllocateGeometry()
    {
        centerPoints = new Vector3[segmentCount];
        glowVertices = new Vector3[segmentCount * 2];
        coreVertices = new Vector3[segmentCount * 2];
        uvs = new Vector2[segmentCount * 2];
        triangles = new int[(segmentCount - 1) * 6];

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            int vertexIndex = i * 2;
            uvs[vertexIndex] = new Vector2(t, 0f);
            uvs[vertexIndex + 1] = new Vector2(t, 1f);

            if (i >= segmentCount - 1) continue;

            int triangleIndex = i * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 2;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
        }

        geometryDirty = true;
    }

    private void UpdateGeometry()
    {
        Vector3 beamVector = endPoint - startPoint;
        float beamLength = beamVector.magnitude;
        if (beamLength <= 0.001f)
        {
            glowRenderer.enabled = false;
            coreRenderer.enabled = false;
            return;
        }

        glowRenderer.enabled = visible;
        coreRenderer.enabled = visible;

        Vector3 direction = beamVector / beamLength;
        Camera camera = Camera.main;
        Vector3 viewDirection = camera != null
            ? (camera.transform.position - (startPoint + endPoint) * 0.5f).normalized
            : Vector3.forward;
        Vector3 widthDirection = Vector3.Cross(direction, viewDirection);
        if (widthDirection.sqrMagnitude < 0.001f)
        {
            widthDirection = Vector3.Cross(direction, Vector3.up);
        }
        if (widthDirection.sqrMagnitude < 0.001f)
        {
            widthDirection = Vector3.right;
        }
        widthDirection.Normalize();

        Vector3 noiseDirection = Vector3.Cross(direction, widthDirection).normalized;
        float phase = Time.unscaledTime * flowSpeed;
        float glowHalfWidth = coreWidth * glowWidthMultiplier * 0.5f;
        float coreHalfWidth = coreWidth * 0.5f;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float wave = Mathf.Sin(t * noiseFrequency * Mathf.PI * 2f + phase) * noiseAmplitude * envelope;
            Vector3 worldCenter = Vector3.Lerp(startPoint, endPoint, t) + noiseDirection * wave;
            centerPoints[i] = transform.InverseTransformPoint(worldCenter);

            Vector3 localWidth = transform.InverseTransformVector(widthDirection).normalized;
            int vertexIndex = i * 2;
            glowVertices[vertexIndex] = centerPoints[i] - localWidth * glowHalfWidth;
            glowVertices[vertexIndex + 1] = centerPoints[i] + localWidth * glowHalfWidth;
            coreVertices[vertexIndex] = centerPoints[i] - localWidth * coreHalfWidth;
            coreVertices[vertexIndex + 1] = centerPoints[i] + localWidth * coreHalfWidth;
        }

        UpdateMesh(glowMesh, glowVertices);
        UpdateMesh(coreMesh, coreVertices);
        geometryDirty = false;
    }

    private void UpdateMesh(Mesh mesh, Vector3[] vertices)
    {
        mesh.Clear(false);
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void ApplyColors()
    {
        if (glowRenderer == null || coreRenderer == null) return;

        Color glowColor = beamColor;
        glowColor.a *= 0.24f * intensity;
        Color coreColor = Color.Lerp(beamColor, Color.white, 0.72f);
        coreColor.a = beamColor.a * intensity;

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
