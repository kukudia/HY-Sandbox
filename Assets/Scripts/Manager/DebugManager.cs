using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DebugManager : MonoBehaviour
{
    private const int IsolatedState = 0;
    private const int ConnectedState = 1;
    private const int PoweredState = 2;

    public static DebugManager instance;

    [Header("Power Debug Visibility")]
    public bool showPowerRange = false;
    public bool showPowerConnections = false;

    [Header("Power Range Colors")]
    public Color poweredRangeColor = new Color(0.15f, 1f, 0.45f, 0.16f);
    public Color connectedRangeColor = new Color(1f, 0.78f, 0.12f, 0.14f);
    public Color isolatedRangeColor = new Color(1f, 0.2f, 0.12f, 0.12f);

    [Header("Power Connection Lines")]
    public Color generatorConnectionColor = new Color(0.2f, 1f, 0.45f, 0.95f);
    public Color deviceConnectionColor = new Color(0.15f, 0.75f, 1f, 0.9f);
    [Min(0.005f)] public float connectionLineWidth = 0.055f;
    [Min(0.02f)] public float connectionDashLength = 0.35f;
    [Min(0f)] public float connectionDashScrollSpeed = 0.45f;

    private readonly List<float> xCoordinates = new List<float>();
    private readonly List<float> yCoordinates = new List<float>();
    private readonly List<float> zCoordinates = new List<float>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int>[] triangles =
    {
        new List<int>(),
        new List<int>(),
        new List<int>()
    };
    private readonly Material[] powerRangeMaterials = new Material[3];

    private GameObject powerRangeObject;
    private MeshRenderer powerRangeRenderer;
    private Mesh powerRangeMesh;
    private int lastPowerRangeSignature = int.MinValue;

    private void Awake()
    {
        instance = this;
        EnsurePowerRangeObject();
        SetPowerRangeMeshVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (powerRangeMesh != null)
        {
            Destroy(powerRangeMesh);
        }

        for (int i = 0; i < powerRangeMaterials.Length; i++)
        {
            if (powerRangeMaterials[i] != null)
            {
                Destroy(powerRangeMaterials[i]);
            }
        }
    }

    public Color GetPowerRangeColor(PowerTransmissionDevice device)
    {
        if (device != null && device.availableNetworkPower > 0f)
        {
            return poweredRangeColor;
        }

        if (device != null && (device.connectedGenerators.Count > 0 || device.connectedDevices.Count > 0))
        {
            return connectedRangeColor;
        }

        return isolatedRangeColor;
    }

    public void TogglePowerRange()
    {
        showPowerRange = !showPowerRange;
        if (!showPowerRange)
        {
            SetPowerRangeMeshVisible(false);
        }
        else
        {
            lastPowerRangeSignature = int.MinValue;
        }
    }

    public void TogglePowerConnections()
    {
        showPowerConnections = !showPowerConnections;
    }

    internal void RefreshPowerRangeMesh(IList<PowerTransmissionDevice> devices)
    {
        EnsurePowerRangeObject();
        if (!showPowerRange)
        {
            SetPowerRangeMeshVisible(false);
            return;
        }

        EnsurePowerRangeMaterials(devices);
        int signature = CalculatePowerRangeSignature(devices);
        if (signature != lastPowerRangeSignature)
        {
            BuildPowerRangeMesh(devices);
            lastPowerRangeSignature = signature;
        }

        SetPowerRangeMeshVisible(true);
    }

    internal void ClearPowerRangeMesh()
    {
        EnsurePowerRangeObject();
        powerRangeMesh.Clear();
        lastPowerRangeSignature = int.MinValue;
        SetPowerRangeMeshVisible(false);
    }

    private void EnsurePowerRangeObject()
    {
        if (powerRangeObject != null) return;

        powerRangeObject = new GameObject("Power Range Union");
        powerRangeObject.transform.SetParent(transform, false);
        MeshFilter powerRangeFilter = powerRangeObject.AddComponent<MeshFilter>();
        powerRangeRenderer = powerRangeObject.AddComponent<MeshRenderer>();
        powerRangeRenderer.shadowCastingMode = ShadowCastingMode.Off;
        powerRangeRenderer.receiveShadows = false;
        powerRangeRenderer.lightProbeUsage = LightProbeUsage.Off;
        powerRangeRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        powerRangeMesh = new Mesh
        {
            name = "Power Range Union Mesh",
            indexFormat = IndexFormat.UInt32
        };
        powerRangeMesh.MarkDynamic();
        powerRangeFilter.sharedMesh = powerRangeMesh;
    }

    private void BuildPowerRangeMesh(IList<PowerTransmissionDevice> devices)
    {
        // Coordinate compression turns overlapping axis-aligned boxes into occupied cells;
        // only cells adjacent to empty space contribute faces, so internal overlaps vanish.
        CollectCoordinates(devices);
        int xCellCount = Mathf.Max(0, xCoordinates.Count - 1);
        int yCellCount = Mathf.Max(0, yCoordinates.Count - 1);
        int zCellCount = Mathf.Max(0, zCoordinates.Count - 1);
        byte[,,] cellStates = new byte[xCellCount, yCellCount, zCellCount];

        for (int i = 0; i < devices.Count; i++)
        {
            PowerTransmissionDevice device = devices[i];
            if (device == null || !device.isActiveAndEnabled || device.powerRange <= 0) continue;

            Vector3 position = device.transform.position;
            float halfSize = device.powerRange;
            int xMin = FindCoordinateIndex(xCoordinates, position.x - halfSize);
            int xMax = FindCoordinateIndex(xCoordinates, position.x + halfSize);
            int yMin = FindCoordinateIndex(yCoordinates, position.y - halfSize);
            int yMax = FindCoordinateIndex(yCoordinates, position.y + halfSize);
            int zMin = FindCoordinateIndex(zCoordinates, position.z - halfSize);
            int zMax = FindCoordinateIndex(zCoordinates, position.z + halfSize);
            byte state = (byte)(GetPowerState(device) + 1);

            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    for (int z = zMin; z < zMax; z++)
                    {
                        cellStates[x, y, z] = (byte)Mathf.Max(cellStates[x, y, z], state);
                    }
                }
            }
        }

        vertices.Clear();
        for (int i = 0; i < triangles.Length; i++) triangles[i].Clear();

        for (int x = 0; x < xCellCount; x++)
        {
            for (int y = 0; y < yCellCount; y++)
            {
                for (int z = 0; z < zCellCount; z++)
                {
                    byte state = cellStates[x, y, z];
                    if (state == 0) continue;

                    int materialIndex = state - 1;
                    if (x == 0 || cellStates[x - 1, y, z] == 0) AddXFace(x, y, z, false, materialIndex);
                    if (x == xCellCount - 1 || cellStates[x + 1, y, z] == 0) AddXFace(x, y, z, true, materialIndex);
                    if (y == 0 || cellStates[x, y - 1, z] == 0) AddYFace(x, y, z, false, materialIndex);
                    if (y == yCellCount - 1 || cellStates[x, y + 1, z] == 0) AddYFace(x, y, z, true, materialIndex);
                    if (z == 0 || cellStates[x, y, z - 1] == 0) AddZFace(x, y, z, false, materialIndex);
                    if (z == zCellCount - 1 || cellStates[x, y, z + 1] == 0) AddZFace(x, y, z, true, materialIndex);
                }
            }
        }

        powerRangeMesh.Clear();
        powerRangeMesh.SetVertices(vertices);
        powerRangeMesh.subMeshCount = triangles.Length;
        for (int i = 0; i < triangles.Length; i++)
        {
            powerRangeMesh.SetTriangles(triangles[i], i, false);
        }

        powerRangeMesh.RecalculateNormals();
        powerRangeMesh.RecalculateBounds();
    }

    private void CollectCoordinates(IList<PowerTransmissionDevice> devices)
    {
        xCoordinates.Clear();
        yCoordinates.Clear();
        zCoordinates.Clear();

        for (int i = 0; i < devices.Count; i++)
        {
            PowerTransmissionDevice device = devices[i];
            if (device == null || !device.isActiveAndEnabled || device.powerRange <= 0) continue;

            Vector3 position = device.transform.position;
            float halfSize = device.powerRange;
            xCoordinates.Add(position.x - halfSize);
            xCoordinates.Add(position.x + halfSize);
            yCoordinates.Add(position.y - halfSize);
            yCoordinates.Add(position.y + halfSize);
            zCoordinates.Add(position.z - halfSize);
            zCoordinates.Add(position.z + halfSize);
        }

        SortAndUnique(xCoordinates);
        SortAndUnique(yCoordinates);
        SortAndUnique(zCoordinates);
    }

    private void AddXFace(int x, int y, int z, bool positive, int materialIndex)
    {
        float faceX = positive ? xCoordinates[x + 1] : xCoordinates[x];
        AddQuad(
            new Vector3(faceX, yCoordinates[y], zCoordinates[z]),
            new Vector3(faceX, yCoordinates[y + 1], zCoordinates[z]),
            new Vector3(faceX, yCoordinates[y + 1], zCoordinates[z + 1]),
            new Vector3(faceX, yCoordinates[y], zCoordinates[z + 1]),
            materialIndex,
            !positive);
    }

    private void AddYFace(int x, int y, int z, bool positive, int materialIndex)
    {
        float faceY = positive ? yCoordinates[y + 1] : yCoordinates[y];
        AddQuad(
            new Vector3(xCoordinates[x], faceY, zCoordinates[z]),
            new Vector3(xCoordinates[x], faceY, zCoordinates[z + 1]),
            new Vector3(xCoordinates[x + 1], faceY, zCoordinates[z + 1]),
            new Vector3(xCoordinates[x + 1], faceY, zCoordinates[z]),
            materialIndex,
            !positive);
    }

    private void AddZFace(int x, int y, int z, bool positive, int materialIndex)
    {
        float faceZ = positive ? zCoordinates[z + 1] : zCoordinates[z];
        AddQuad(
            new Vector3(xCoordinates[x], yCoordinates[y], faceZ),
            new Vector3(xCoordinates[x + 1], yCoordinates[y], faceZ),
            new Vector3(xCoordinates[x + 1], yCoordinates[y + 1], faceZ),
            new Vector3(xCoordinates[x], yCoordinates[y + 1], faceZ),
            materialIndex,
            !positive);
    }

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int materialIndex, bool reverse)
    {
        int vertexStart = vertices.Count;
        Transform meshTransform = powerRangeObject.transform;
        vertices.Add(meshTransform.InverseTransformPoint(a));
        vertices.Add(meshTransform.InverseTransformPoint(b));
        vertices.Add(meshTransform.InverseTransformPoint(c));
        vertices.Add(meshTransform.InverseTransformPoint(d));

        List<int> targetTriangles = triangles[materialIndex];
        if (reverse)
        {
            targetTriangles.Add(vertexStart);
            targetTriangles.Add(vertexStart + 2);
            targetTriangles.Add(vertexStart + 1);
            targetTriangles.Add(vertexStart);
            targetTriangles.Add(vertexStart + 3);
            targetTriangles.Add(vertexStart + 2);
        }
        else
        {
            targetTriangles.Add(vertexStart);
            targetTriangles.Add(vertexStart + 1);
            targetTriangles.Add(vertexStart + 2);
            targetTriangles.Add(vertexStart);
            targetTriangles.Add(vertexStart + 2);
            targetTriangles.Add(vertexStart + 3);
        }
    }

    private void EnsurePowerRangeMaterials(IList<PowerTransmissionDevice> devices)
    {
        Material source = null;
        for (int i = 0; i < devices.Count && source == null; i++)
        {
            if (devices[i] != null)
            {
                source = devices[i].DebugRangeMaterial;
            }
        }

        if (source == null) return;

        Color[] colors = { isolatedRangeColor, connectedRangeColor, poweredRangeColor };
        for (int i = 0; i < powerRangeMaterials.Length; i++)
        {
            if (powerRangeMaterials[i] == null || powerRangeMaterials[i].shader != source.shader)
            {
                if (powerRangeMaterials[i] != null) Destroy(powerRangeMaterials[i]);
                powerRangeMaterials[i] = new Material(source)
                {
                    name = $"Power Range Union {i}",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (powerRangeMaterials[i].HasProperty("_BaseColor"))
            {
                powerRangeMaterials[i].SetColor("_BaseColor", colors[i]);
            }

            if (powerRangeMaterials[i].HasProperty("_Color"))
            {
                powerRangeMaterials[i].SetColor("_Color", colors[i]);
            }
        }

        powerRangeRenderer.sharedMaterials = powerRangeMaterials;
    }

    private static int CalculatePowerRangeSignature(IList<PowerTransmissionDevice> devices)
    {
        unchecked
        {
            int signature = devices.Count;
            for (int i = 0; i < devices.Count; i++)
            {
                PowerTransmissionDevice device = devices[i];
                if (device == null) continue;

                int deviceSignature = device.GetInstanceID();
                deviceSignature = (deviceSignature * 397) ^ device.transform.position.GetHashCode();
                deviceSignature = (deviceSignature * 397) ^ device.powerRange;
                deviceSignature = (deviceSignature * 397) ^ GetPowerState(device);
                signature ^= deviceSignature;
            }

            return signature;
        }
    }

    private static int GetPowerState(PowerTransmissionDevice device)
    {
        if (device.availableNetworkPower > 0f) return PoweredState;
        return device.connectedGenerators.Count > 0 || device.connectedDevices.Count > 0
            ? ConnectedState
            : IsolatedState;
    }

    private static int FindCoordinateIndex(List<float> values, float target)
    {
        int index = values.BinarySearch(target);
        if (index >= 0) return index;

        index = ~index;
        if (index > 0 && Mathf.Abs(values[index - 1] - target) <= 0.0001f) return index - 1;
        return Mathf.Min(index, values.Count - 1);
    }

    private static void SortAndUnique(List<float> values)
    {
        values.Sort();
        for (int i = values.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(values[i] - values[i - 1]) <= 0.0001f)
            {
                values.RemoveAt(i);
            }
        }
    }

    private void SetPowerRangeMeshVisible(bool visible)
    {
        if (powerRangeObject != null)
        {
            powerRangeObject.SetActive(visible && powerRangeMesh != null && powerRangeMesh.vertexCount > 0);
        }
    }
}
