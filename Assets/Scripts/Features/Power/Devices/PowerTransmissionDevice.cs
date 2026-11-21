// 职责：保存中继参数，登记生命周期并装配每帧一次的无线电网重建。
// 入口：Update() → PowerNetwork.Rebuild()；调试连接由 PowerConnectionVisuals 独立绘制。
// 生命周期：退出时移除双向连接；最后一个中继退出立即清除全体负载供电。
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class PowerTransmissionDevice : MonoBehaviour
{
    private static readonly HashSet<PowerTransmissionDevice> ActiveDevices = new HashSet<PowerTransmissionDevice>();
    private static readonly PowerNetwork Network = new PowerNetwork();
    private static int _lastNetworkUpdateFrame = -1;
    private static bool _networkDirty = true;
    private static float _nextFallbackRebuild;
    private const float FallbackRebuildInterval = 0.25f;
    private const float VisualRefreshInterval = 0.0667f;
    private static readonly ProfilerMarker NetworkMarker = new ProfilerMarker("HY.PowerTransmissionDevice.Network.Rebuild");
    private static readonly ProfilerMarker RangeMarker = new ProfilerMarker("HY.PowerTransmissionDevice.RefreshPowerRangeMesh");
    private static readonly ProfilerMarker VisualMarker = new ProfilerMarker("HY.PowerTransmissionDevice.UpdateDebugVisuals");
    public List<PowerGeneratingUnit> connectedGenerators = new List<PowerGeneratingUnit>();
    public List<PowerTransmissionDevice> connectedDevices = new List<PowerTransmissionDevice>();
    [Min(0)]
    [Tooltip("供电盒的世界坐标半边长；范围内已启动负载按需求参与功率分配。")]
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
    private int _lastVisualNetworkVersion = -1;
    private int _lastVisualConfigHash;
    private Vector3 _lastVisualPosition;
    private Quaternion _lastVisualRotation;
    private bool _visualsWereEnabled;
    private Vector3 _lastObservedPosition;
    private float _nextVisualRefresh;
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
        MarkNetworkDirty();
    }

    private void OnEnable()
    {
        if (_connectionVisuals == null)
        {
            _connectionVisuals = new PowerConnectionVisuals(this);
        }

        ActiveDevices.Add(this);
        _lastObservedPosition = transform.position;
        CacheDebugReferences();
        if (debugCube != null)
        {
            debugCube.SetActive(false);
        }

        MarkNetworkDirty();
        UpdateDebugVisuals();
    }

    public static void MarkNetworkDirty()
    {
        _networkDirty = true;
    }

    private void Update()
    {
        if (_lastObservedPosition != transform.position)
        {
            _lastObservedPosition = transform.position;
            MarkNetworkDirty();
        }

        // 工作意图/拓扑变化立即更新；没有事件的外部 Inspector/运行时输出变更由低频校正兜底。
        if (_lastNetworkUpdateFrame != Time.frameCount && (_networkDirty || Time.unscaledTime >= _nextFallbackRebuild))
        {
            _lastNetworkUpdateFrame = Time.frameCount;
            _networkDirty = false;
            _nextFallbackRebuild = Time.unscaledTime + FallbackRebuildInterval;
            using (NetworkMarker.Auto())
            {
                Network.Rebuild(ActiveDevices, PowerGeneratingUnit.ActiveUnits, Power.ActiveBlocks);
            }

            if (DebugManager.instance != null && DebugManager.instance.showPowerRange)
            {
                using (RangeMarker.Auto())
                {
                    DebugManager.instance.RefreshPowerRangeMesh(Network.Devices);
                }
            }
        }

        UpdateDebugVisuals();
    }

    private void OnDisable()
    {
        ActiveDevices.Remove(this);
        Disconnect();
        MarkNetworkDirty();
        _connectionVisuals?.Hide();
        if (debugCube != null)
        {
            debugCube.SetActive(false);
        }

        if (ActiveDevices.Count == 0)
        {
            Network.Clear(Power.ActiveBlocks);
            _lastNetworkUpdateFrame = -1;
            _networkDirty = true;
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
        using var marker = VisualMarker.Auto();
        DebugManager settings = DebugManager.instance;
        bool enabled = settings != null && settings.showPowerConnections && settings.Includes(this);
        if (!enabled)
        {
            if (_visualsWereEnabled)
            {
                _connectionVisuals?.Hide();
                _visualsWereEnabled = false;
            }

            return;
        }

        int configHash = settings.GetPowerConnectionVisualHash();
        bool changed = !_visualsWereEnabled || _lastVisualNetworkVersion != Network.Version || _lastVisualConfigHash != configHash || _lastVisualPosition != transform.position || _lastVisualRotation != transform.rotation;
        if (!changed && Time.unscaledTime < _nextVisualRefresh)
        {
            return;
        }

        _connectionVisuals?.Refresh(settings);
        _visualsWereEnabled = true;
        _lastVisualNetworkVersion = Network.Version;
        _lastVisualConfigHash = configHash;
        _lastVisualPosition = transform.position;
        _lastVisualRotation = transform.rotation;
        _nextVisualRefresh = Time.unscaledTime + VisualRefreshInterval;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = availableNetworkPower > 0f ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (powerRange * 2f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (maxConnectionDistance * 2f));
    }
}
