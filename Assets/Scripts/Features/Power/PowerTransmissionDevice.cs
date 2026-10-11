// 职责：保存中继参数，登记生命周期并装配每帧一次的无线电网重建。
// 入口：Update() → PowerNetwork.Rebuild()；调试连接由 PowerConnectionVisuals 独立绘制。
// 生命周期：退出时移除双向连接；最后一个中继退出立即清除全体负载供电。
using System.Collections.Generic;
using UnityEngine;

public class PowerTransmissionDevice : MonoBehaviour
{
    private static readonly HashSet<PowerTransmissionDevice> ActiveDevices = new HashSet<PowerTransmissionDevice>();
    private static readonly PowerNetwork Network = new PowerNetwork();
    private static int _lastNetworkUpdateFrame = -1;
    public List<PowerGeneratingUnit> connectedGenerators = new List<PowerGeneratingUnit>();
    public List<PowerTransmissionDevice> connectedDevices = new List<PowerTransmissionDevice>();
    [Min(0)]
    [Tooltip("供电盒的世界坐标半边长；范围内负载参与电网均分。")]
    public int powerRange = 5;
    [Min(0)]
    [Tooltip("连接盒的世界坐标半边长；中继互连采用两端较小值。")]
    public int maxConnectionDistance = 10;
    public GameObject debugCube;
    public LineRenderer lineRenderer;
    public float availableNetworkPower
    {
        get; internal set;
    }
    public bool showPowerRange => DebugManager.instance != null && DebugManager.instance.showPowerRange;
    public bool showPowerConnections => DebugManager.instance != null && DebugManager.instance.showPowerConnections;

    private PowerConnectionVisuals _connectionVisuals;
    private Renderer _debugRenderer;
    internal Material DebugRangeMaterial
    {
        get
        {
            CacheDebugReferences();
            return _debugRenderer != null ? _debugRenderer.sharedMaterial : null;
        }
    }

    private void Awake()
    {
        _connectionVisuals = new PowerConnectionVisuals(this);
    }

    private void OnValidate()
    {
        CacheDebugReferences();
    }

    private void OnEnable()
    {
        if (_connectionVisuals == null)
        {
            _connectionVisuals = new PowerConnectionVisuals(this);
        }

        ActiveDevices.Add(this);
        UpdateDebugVisuals();
    }

    private void Update()
    {
        // 无论多少个中继执行 Update，供电规则每帧只运行一次；每个中继仍刷新自己的连线。
        if (_lastNetworkUpdateFrame != Time.frameCount)
        {
            _lastNetworkUpdateFrame = Time.frameCount;
            Network.Rebuild(ActiveDevices, PowerGeneratingUnit.ActiveUnits, Power.ActiveBlocks);
            DebugManager.instance?.RefreshPowerRangeMesh(Network.Devices);
        }

        UpdateDebugVisuals();
    }

    private void OnDisable()
    {
        ActiveDevices.Remove(this);
        Disconnect();
        _connectionVisuals?.Hide();
        if (debugCube != null)
        {
            debugCube.SetActive(false);
        }

        if (ActiveDevices.Count == 0)
        {
            Network.Clear(Power.ActiveBlocks);
            _lastNetworkUpdateFrame = -1;
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
        availableNetworkPower = 0f;
    }

    private void CacheDebugReferences()
    {
        if (debugCube == null)
        {
            Transform debugTransform = transform.Find("Debug/Cube");
            debugCube = debugTransform != null ? debugTransform.gameObject : null;
        }

        if (_debugRenderer == null && debugCube != null)
        {
            _debugRenderer = debugCube.GetComponent<Renderer>();
        }
    }

    private void UpdateDebugVisuals()
    {
        CacheDebugReferences();
        if (debugCube != null)
        {
            // 范围合并网格由 DebugManager 绘制，旧 Prefab 内的单独方块保持隐藏。
            debugCube.transform.rotation = transform.rotation;
            debugCube.SetActive(false);
        }

        _connectionVisuals?.Refresh(DebugManager.instance);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = availableNetworkPower > 0f ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (powerRange * 2f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (maxConnectionDistance * 2f));
    }
}
