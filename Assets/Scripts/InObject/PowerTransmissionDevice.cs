using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PowerTransmissionDevice : MonoBehaviour
{
    private static readonly HashSet<PowerTransmissionDevice> ActiveDevices = new HashSet<PowerTransmissionDevice>();
    private static readonly List<PowerTransmissionDevice> DeviceBuffer = new List<PowerTransmissionDevice>();
    private static readonly List<PowerGeneratingUnit> GeneratorBuffer = new List<PowerGeneratingUnit>();
    private static readonly List<Power> PowerBuffer = new List<Power>();
    private static readonly HashSet<PowerTransmissionDevice> VisitedDevices = new HashSet<PowerTransmissionDevice>();
    private static readonly HashSet<PowerGeneratingUnit> ComponentGenerators = new HashSet<PowerGeneratingUnit>();
    private static readonly HashSet<Power> ComponentLoads = new HashSet<Power>();
    private static readonly List<PowerTransmissionDevice> ComponentDevices = new List<PowerTransmissionDevice>();
    private static readonly Queue<PowerTransmissionDevice> DeviceQueue = new Queue<PowerTransmissionDevice>();
    private static int lastNetworkUpdateFrame = -1;
    private static Material dashedLineMaterial;

    public List<PowerGeneratingUnit> connectedGenerators = new List<PowerGeneratingUnit>();
    public List<PowerTransmissionDevice> connectedDevices = new List<PowerTransmissionDevice>();

    [Min(0)]
    public int powerRange = 5;

    [Min(0)]
    public int maxConnectionDistance = 10;

    public GameObject debugCube;

    public LineRenderer lineRenderer;

    public float availableNetworkPower { get; private set; }
    public bool showPowerRange => DebugManager.instance != null && DebugManager.instance.showPowerRange;
    public bool showPowerConnections => DebugManager.instance != null && DebugManager.instance.showPowerConnections;

    internal Material DebugRangeMaterial
    {
        get
        {
            CacheDebugReferences();
            return debugRenderer != null ? debugRenderer.sharedMaterial : null;
        }
    }

    private readonly List<LineRenderer> connectionLines = new List<LineRenderer>();
    private MaterialPropertyBlock debugProperties;
    private Renderer debugRenderer;

    private void Awake()
    {
        EnsureDebugProperties();
    }

    private void OnValidate()
    {
        CacheDebugReferences();
    }

    private void OnEnable()
    {
        ActiveDevices.Add(this);
        CacheDebugReferences();
        UpdateDebugVisuals();
    }

    private void Update()
    {
        if (lastNetworkUpdateFrame != Time.frameCount)
        {
            lastNetworkUpdateFrame = Time.frameCount;
            RefreshPowerNetwork();
        }

        UpdateDebugVisuals();
    }

    private void OnDisable()
    {
        ActiveDevices.Remove(this);
        Disconnect();
        SetConnectionLinesVisible(false);

        if (debugCube != null)
        {
            debugCube.SetActive(false);
        }

        if (ActiveDevices.Count == 0)
        {
            ResetAllPowerBlocks();
            DebugManager.instance?.ClearPowerRangeMesh();
        }
    }

    private void Disconnect()
    {
        foreach (PowerGeneratingUnit generator in connectedGenerators)
        {
            if (generator != null)
            {
                generator.connectedDevices.Remove(this);
            }
        }

        foreach (PowerTransmissionDevice device in connectedDevices)
        {
            if (device != null)
            {
                device.connectedDevices.Remove(this);
            }
        }

        connectedGenerators.Clear();
        connectedDevices.Clear();
    }

    private static void RefreshPowerNetwork()
    {
        RebuildBuffers();
        ResetConnectionsAndPower();
        BuildConnections();
        DistributeNetworkPower();
        DebugManager.instance?.RefreshPowerRangeMesh(DeviceBuffer);
    }

    private static void RebuildBuffers()
    {
        DeviceBuffer.Clear();
        GeneratorBuffer.Clear();
        PowerBuffer.Clear();

        foreach (PowerTransmissionDevice device in ActiveDevices)
        {
            if (device != null && device.isActiveAndEnabled)
            {
                DeviceBuffer.Add(device);
            }
        }

        foreach (PowerGeneratingUnit generator in PowerGeneratingUnit.ActiveUnits)
        {
            if (generator != null && generator.isActiveAndEnabled)
            {
                GeneratorBuffer.Add(generator);
            }
        }

        foreach (Power powerBlock in Power.ActiveBlocks)
        {
            if (powerBlock != null && powerBlock.isActiveAndEnabled)
            {
                PowerBuffer.Add(powerBlock);
            }
        }
    }

    private static void ResetConnectionsAndPower()
    {
        foreach (PowerTransmissionDevice device in DeviceBuffer)
        {
            device.connectedGenerators.Clear();
            device.connectedDevices.Clear();
            device.availableNetworkPower = 0f;
        }

        foreach (PowerGeneratingUnit generator in GeneratorBuffer)
        {
            generator.connectedDevices.Clear();
        }

        foreach (Power powerBlock in PowerBuffer)
        {
            powerBlock.ResetPower();
        }
    }

    private static void BuildConnections()
    {
        for (int i = 0; i < DeviceBuffer.Count; i++)
        {
            PowerTransmissionDevice device = DeviceBuffer[i];

            foreach (PowerGeneratingUnit generator in GeneratorBuffer)
            {
                if (!IsWithinBoxRange(
                        generator.transform.position,
                        device.transform.position,
                        device.maxConnectionDistance))
                {
                    continue;
                }

                device.connectedGenerators.Add(generator);
                generator.connectedDevices.Add(device);
            }

            for (int j = i + 1; j < DeviceBuffer.Count; j++)
            {
                PowerTransmissionDevice other = DeviceBuffer[j];
                float connectionDistance = Mathf.Min(device.maxConnectionDistance, other.maxConnectionDistance);
                if (!IsWithinBoxRange(other.transform.position, device.transform.position, connectionDistance))
                {
                    continue;
                }

                device.connectedDevices.Add(other);
                other.connectedDevices.Add(device);
            }
        }
    }

    private static void DistributeNetworkPower()
    {
        VisitedDevices.Clear();

        foreach (PowerTransmissionDevice rootDevice in DeviceBuffer)
        {
            if (!VisitedDevices.Add(rootDevice)) continue;

            ComponentGenerators.Clear();
            ComponentLoads.Clear();
            ComponentDevices.Clear();
            DeviceQueue.Clear();
            DeviceQueue.Enqueue(rootDevice);

            while (DeviceQueue.Count > 0)
            {
                PowerTransmissionDevice device = DeviceQueue.Dequeue();
                ComponentDevices.Add(device);
                CollectDeviceLoads(device);

                foreach (PowerGeneratingUnit generator in device.connectedGenerators)
                {
                    if (generator == null || !ComponentGenerators.Add(generator)) continue;

                    foreach (PowerTransmissionDevice generatorDevice in generator.connectedDevices)
                    {
                        if (generatorDevice != null && VisitedDevices.Add(generatorDevice))
                        {
                            DeviceQueue.Enqueue(generatorDevice);
                        }
                    }
                }

                foreach (PowerTransmissionDevice connectedDevice in device.connectedDevices)
                {
                    if (connectedDevice != null && VisitedDevices.Add(connectedDevice))
                    {
                        DeviceQueue.Enqueue(connectedDevice);
                    }
                }
            }

            float totalOutputPower = 0f;
            foreach (PowerGeneratingUnit generator in ComponentGenerators)
            {
                totalOutputPower += Mathf.Max(0f, generator.outputPower);
            }

            foreach (PowerTransmissionDevice device in ComponentDevices)
            {
                device.availableNetworkPower = totalOutputPower;
            }

            if (totalOutputPower <= 0f || ComponentLoads.Count == 0) continue;

            float powerPerBlock = totalOutputPower / ComponentLoads.Count;
            foreach (Power powerBlock in ComponentLoads)
            {
                powerBlock.ReceivePower(powerPerBlock);
            }
        }
    }

    private static void CollectDeviceLoads(PowerTransmissionDevice device)
    {
        foreach (Power powerBlock in PowerBuffer)
        {
            if (IsWithinBoxRange(powerBlock.transform.position, device.transform.position, device.powerRange))
            {
                ComponentLoads.Add(powerBlock);
            }
        }
    }

    private static bool IsWithinBoxRange(Vector3 firstPosition, Vector3 secondPosition, float range)
    {
        Vector3 offset = firstPosition - secondPosition;
        float clampedRange = Mathf.Max(0f, range);
        return Mathf.Abs(offset.x) <= clampedRange
            && Mathf.Abs(offset.y) <= clampedRange
            && Mathf.Abs(offset.z) <= clampedRange;
    }

    private static void ResetAllPowerBlocks()
    {
        foreach (Power powerBlock in Power.ActiveBlocks)
        {
            if (powerBlock != null)
            {
                powerBlock.ResetPower();
            }
        }
    }

    private void CacheDebugReferences()
    {
        if (debugCube == null)
        {
            Transform debugTransform = transform.Find("Debug/Cube");
            debugCube = debugTransform != null ? debugTransform.gameObject : null;
        }

        if (debugRenderer == null && debugCube != null)
        {
            debugRenderer = debugCube.GetComponent<Renderer>();
        }
    }

    private void UpdateDebugVisuals()
    {
        CacheDebugReferences();
        UpdatePowerRangeVisual();
        UpdateConnectionLines();
    }

    private void UpdatePowerRangeVisual()
    {
        if (debugCube == null) return;
        debugCube.SetActive(false);
    }

    private void UpdateConnectionLines()
    {
        if (!showPowerConnections)
        {
            SetConnectionLinesVisible(false);
            return;
        }

        int lineIndex = 0;
        DebugManager manager = DebugManager.instance;
        Color generatorColor = manager != null ? manager.generatorConnectionColor : Color.green;
        Color deviceColor = manager != null ? manager.deviceConnectionColor : Color.cyan;

        foreach (PowerGeneratingUnit generator in connectedGenerators)
        {
            if (generator == null) continue;

            DrawDashedConnection(lineIndex++, transform.position, generator.transform.position, generatorColor);
        }

        foreach (PowerTransmissionDevice device in connectedDevices)
        {
            if (device == null || GetInstanceID() >= device.GetInstanceID()) continue;

            DrawDashedConnection(lineIndex++, transform.position, device.transform.position, deviceColor);
        }

        for (int i = lineIndex; i < connectionLines.Count; i++)
        {
            if (connectionLines[i] != null)
            {
                connectionLines[i].enabled = false;
            }
        }
    }

    private void DrawDashedConnection(int index, Vector3 start, Vector3 end, Color color)
    {
        LineRenderer connectionLine = GetOrCreateConnectionLine(index);
        if (connectionLine == null) return;

        EnsureDebugProperties();

        DebugManager manager = DebugManager.instance;
        float width = manager != null ? manager.connectionLineWidth : 0.055f;
        float dashLength = manager != null ? manager.connectionDashLength : 0.35f;
        float scrollSpeed = manager != null ? manager.connectionDashScrollSpeed : 0.45f;
        float distance = Vector3.Distance(start, end);

        connectionLine.enabled = true;
        connectionLine.startWidth = width;
        connectionLine.endWidth = width;
        connectionLine.startColor = color;
        connectionLine.endColor = color;
        connectionLine.SetPosition(0, start);
        connectionLine.SetPosition(1, end);

        connectionLine.GetPropertyBlock(debugProperties);
        debugProperties.SetVector("_MainTex_ST", new Vector4(
            Mathf.Max(1f, distance / Mathf.Max(0.02f, dashLength * 2f)),
            1f,
            -Time.unscaledTime * scrollSpeed,
            0f));
        connectionLine.SetPropertyBlock(debugProperties);
    }

    private void EnsureDebugProperties()
    {
        if (debugProperties == null)
        {
            debugProperties = new MaterialPropertyBlock();
        }
    }

    private LineRenderer GetOrCreateConnectionLine(int index)
    {
        while (connectionLines.Count <= index)
        {
            LineRenderer createdLine;
            if (connectionLines.Count == 0 && lineRenderer != null)
            {
                createdLine = lineRenderer;
            }
            else
            {
                GameObject lineObject = new GameObject($"Power Connection {connectionLines.Count + 1}");
                lineObject.transform.SetParent(transform, false);
                createdLine = lineObject.AddComponent<LineRenderer>();
            }

            ConfigureConnectionLine(createdLine);
            connectionLines.Add(createdLine);
        }

        return connectionLines[index];
    }

    private static void ConfigureConnectionLine(LineRenderer connectionLine)
    {
        if (connectionLine == null) return;

        connectionLine.useWorldSpace = true;
        connectionLine.positionCount = 2;
        connectionLine.textureMode = LineTextureMode.Stretch;
        connectionLine.alignment = LineAlignment.View;
        connectionLine.numCapVertices = 2;
        connectionLine.shadowCastingMode = ShadowCastingMode.Off;
        connectionLine.receiveShadows = false;
        connectionLine.sharedMaterial = GetDashedLineMaterial();
    }

    private static Material GetDashedLineMaterial()
    {
        if (dashedLineMaterial != null) return dashedLineMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        dashedLineMaterial = new Material(shader)
        {
            name = "Runtime Power Connection Dashes",
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = CreateDashTexture()
        };
        return dashedLineMaterial;
    }

    private static Texture2D CreateDashTexture()
    {
        Texture2D texture = new Texture2D(8, 1, TextureFormat.RGBA32, false)
        {
            name = "Runtime Power Dash Pattern",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int x = 0; x < texture.width; x++)
        {
            texture.SetPixel(x, 0, x < texture.width / 2 ? Color.white : Color.clear);
        }

        texture.Apply(false, true);
        return texture;
    }

    private void SetConnectionLinesVisible(bool visible)
    {
        foreach (LineRenderer connectionLine in connectionLines)
        {
            if (connectionLine != null)
            {
                connectionLine.enabled = visible;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = availableNetworkPower > 0f ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (powerRange * 2f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (maxConnectionDistance * 2f));
    }
}
