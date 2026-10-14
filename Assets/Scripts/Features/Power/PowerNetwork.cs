// 职责：缓存无线供电拓扑，以空间索引收集负载并逐帧分配所需功率。
// 入口：Rebuild()；由 PowerTransmissionDevice 每帧装配参与者，精确位置/身份变化才重建拓扑。
// 边界：不调度 Unity 帧或绘制调试图；复用实例缓冲，在一次分配结束后提交负载状态。
using System.Collections.Generic;
using UnityEngine;

public sealed class PowerNetwork
{
    private readonly List<PowerTransmissionDevice> _devices = new List<PowerTransmissionDevice>();
    private readonly List<PowerGeneratingUnit> _generators = new List<PowerGeneratingUnit>();
    private readonly List<Power> _loads = new List<Power>();
    private readonly HashSet<PowerTransmissionDevice> _visitedDevices = new HashSet<PowerTransmissionDevice>();
    private readonly List<PowerTransmissionDevice> _componentDevices = new List<PowerTransmissionDevice>();
    private readonly HashSet<PowerGeneratingUnit> _componentGenerators = new HashSet<PowerGeneratingUnit>();
    private readonly HashSet<Power> _componentLoads = new HashSet<Power>();
    private readonly Queue<PowerTransmissionDevice> _deviceQueue = new Queue<PowerTransmissionDevice>();
    private readonly List<DeviceSnapshot> _deviceSnapshots = new List<DeviceSnapshot>();
    private readonly List<GeneratorSnapshot> _generatorSnapshots = new List<GeneratorSnapshot>();
    private readonly List<Vector3> _loadPositions = new List<Vector3>();
    private readonly PowerPositionIndex _deviceIndex = new PowerPositionIndex();
    private readonly PowerPositionIndex _generatorIndex = new PowerPositionIndex();
    private readonly PowerPositionIndex _loadIndex = new PowerPositionIndex();
    private readonly List<int> _candidates = new List<int>();
    private readonly List<ConnectedComponent> _components = new List<ConnectedComponent>();
    private int _componentCount;
    private bool _hasTopology;
    private struct DeviceSnapshot
    {
        public PowerTransmissionDevice device;
        public Vector3 position;
        public int connectionRange;
    }

    private struct GeneratorSnapshot
    {
        public PowerGeneratingUnit generator;
        public Vector3 position;
    }

    private sealed class ConnectedComponent
    {
        public readonly List<PowerTransmissionDevice> devices = new List<PowerTransmissionDevice>();
        public readonly List<PowerGeneratingUnit> generators = new List<PowerGeneratingUnit>();
    }

    /// <summary>最近一次重建使用的中继快照，供外部调试视图读取。</summary>
    public IList<PowerTransmissionDevice> Devices => _devices;

    /// <summary>从注册表重建电网：清除旧供电，累计独立网络的输出，再统一提交负载状态。</summary>
    public void Rebuild(
        IEnumerable<PowerTransmissionDevice> devices,
        IEnumerable<PowerGeneratingUnit> generators,
        IEnumerable<Power> loads)
    {
        CollectActiveParticipants(devices, generators, loads);
        bool topologyChanged = CaptureTopology();
        if (!_hasTopology || topologyChanged)
        {
            ResetConnections();
            BuildConnections();
            CacheConnectedComponents();
            _hasTopology = true;
        }

        PrepareLoads();
        DistributeNetworkPower();
        CommitPowerStatuses();
    }

    /// <summary>最后一个中继退出时立即清除残留供电，并释放网络持有的对象快照。</summary>
    public void Clear(IEnumerable<Power> loads)
    {
        foreach (Power load in loads)
        {
            if (load != null)
            {
                load.ResetPower();
                load.CommitPowerStatus();
            }
        }

        _devices.Clear();
        _generators.Clear();
        _loads.Clear();
        _visitedDevices.Clear();
        _componentDevices.Clear();
        _componentGenerators.Clear();
        _componentLoads.Clear();
        _deviceQueue.Clear();
        _deviceSnapshots.Clear();
        _generatorSnapshots.Clear();
        _loadPositions.Clear();
        _deviceIndex.Clear();
        _generatorIndex.Clear();
        _loadIndex.Clear();
        _components.Clear();
        _componentCount = 0;
        _hasTopology = false;
    }

    private void CollectActiveParticipants(
        IEnumerable<PowerTransmissionDevice> devices,
        IEnumerable<PowerGeneratingUnit> generators,
        IEnumerable<Power> loads)
    {
        _devices.Clear();
        _generators.Clear();
        _loads.Clear();
        foreach (PowerTransmissionDevice device in devices)
        {
            if (device != null && device.isActiveAndEnabled)
            {
                _devices.Add(device);
            }
        }

        foreach (PowerGeneratingUnit generator in generators)
        {
            if (generator != null && generator.isActiveAndEnabled)
            {
                _generators.Add(generator);
            }
        }

        foreach (Power load in loads)
        {
            if (load != null && load.isActiveAndEnabled)
            {
                _loads.Add(load);
            }
        }
    }

    private void ResetConnections()
    {
        foreach (PowerTransmissionDevice device in _devices)
        {
            device.connectedGenerators.Clear();
            device.connectedDevices.Clear();
            device.availableNetworkPower = 0f;
        }

        foreach (PowerGeneratingUnit generator in _generators)
        {
            generator.connectedDevices.Clear();
        }
    }

    private bool CaptureTopology()
    {
        bool changed = _deviceSnapshots.Count != _devices.Count || _generatorSnapshots.Count != _generators.Count;
        for (int i = 0; i < _devices.Count; i++)
        {
            PowerTransmissionDevice device = _devices[i];
            DeviceSnapshot next = new DeviceSnapshot
            {
                device = device,
                position = device.transform.position,
                connectionRange = device.maxConnectionDistance
            };
            if (i < _deviceSnapshots.Count)
            {
                DeviceSnapshot old = _deviceSnapshots[i];
                changed |= old.device != device || !old.position.Equals(next.position) || old.connectionRange != next.connectionRange;
                _deviceSnapshots[i] = next;
            }
            else
            {
                _deviceSnapshots.Add(next);
            }
        }

        if (_deviceSnapshots.Count > _devices.Count)
        {
            _deviceSnapshots.RemoveRange(_devices.Count, _deviceSnapshots.Count - _devices.Count);
        }

        for (int i = 0; i < _generators.Count; i++)
        {
            PowerGeneratingUnit generator = _generators[i];
            GeneratorSnapshot next = new GeneratorSnapshot
            {
                generator = generator,
                position = generator.transform.position
            };
            if (i < _generatorSnapshots.Count)
            {
                GeneratorSnapshot old = _generatorSnapshots[i];
                changed |= old.generator != generator || !old.position.Equals(next.position);
                _generatorSnapshots[i] = next;
            }
            else
            {
                _generatorSnapshots.Add(next);
            }
        }

        if (_generatorSnapshots.Count > _generators.Count)
        {
            _generatorSnapshots.RemoveRange(_generators.Count, _generatorSnapshots.Count - _generators.Count);
        }

        return changed;
    }

    private void BuildConnections()
    {
        _deviceIndex.Clear();
        _generatorIndex.Clear();
        for (int i = 0; i < _devices.Count; i++)
        {
            _deviceIndex.Add(_deviceSnapshots[i].position, i);
        }

        for (int i = 0; i < _generators.Count; i++)
        {
            _generatorIndex.Add(_generatorSnapshots[i].position, i);
        }

        for (int i = 0; i < _devices.Count; i++)
        {
            PowerTransmissionDevice device = _devices[i];
            Vector3 position = _deviceSnapshots[i].position;
            ConnectGenerators(device, position);
            _deviceIndex.Query(position, device.maxConnectionDistance, _candidates);
            _candidates.Sort();
            // 中继的双向连接必须同时满足两端范围，且每一对只检查一次。
            foreach (int j in _candidates)
            {
                if (j <= i)
                {
                    continue;
                }

                PowerTransmissionDevice other = _devices[j];
                float connectionDistance = Mathf.Min(device.maxConnectionDistance, other.maxConnectionDistance);
                if (IsWithinBoxRange(_deviceSnapshots[j].position, position, connectionDistance))
                {
                    device.connectedDevices.Add(other);
                    other.connectedDevices.Add(device);
                }
            }
        }
    }

    private void ConnectGenerators(PowerTransmissionDevice device, Vector3 position)
    {
        _generatorIndex.Query(position, device.maxConnectionDistance, _candidates);
        _candidates.Sort();
        foreach (int index in _candidates)
        {
            PowerGeneratingUnit generator = _generators[index];
            if (IsWithinBoxRange(_generatorSnapshots[index].position, position, device.maxConnectionDistance))
            {
                device.connectedGenerators.Add(generator);
                generator.connectedDevices.Add(device);
            }
        }
    }

    private void CacheConnectedComponents()
    {
        _visitedDevices.Clear();
        _componentCount = 0;
        foreach (PowerTransmissionDevice rootDevice in _devices)
        {
            if (_visitedDevices.Add(rootDevice))
            {
                CollectConnectedComponent(rootDevice);
                if (_componentCount == _components.Count)
                {
                    _components.Add(new ConnectedComponent());
                }

                ConnectedComponent component = _components[_componentCount++];
                component.devices.Clear();
                component.generators.Clear();
                component.devices.AddRange(_componentDevices);
                component.generators.AddRange(_componentGenerators);
            }
        }

        // 较小网络不再持有旧组件的已销毁对象；保留容器容量便于复用。
        for (int i = _componentCount; i < _components.Count; i++)
        {
            _components[i].devices.Clear();
            _components[i].generators.Clear();
        }
    }

    private void PrepareLoads()
    {
        _loadIndex.Clear();
        _loadPositions.Clear();
        for (int i = 0; i < _loads.Count; i++)
        {
            Power load = _loads[i];
            load.ResetPower();
            Vector3 position = load.transform.position;
            _loadPositions.Add(position);
            if (load.IsWorkRequested)
            {
                _loadIndex.Add(position, i);
            }
        }
    }

    private void DistributeNetworkPower()
    {
        for (int i = 0; i < _componentCount; i++)
        {
            ConnectedComponent component = _components[i];
            _componentDevices.Clear();
            _componentGenerators.Clear();
            _componentLoads.Clear();
            _componentDevices.AddRange(component.devices);
            foreach (PowerGeneratingUnit generator in component.generators)
            {
                _componentGenerators.Add(generator);
            }

            foreach (PowerTransmissionDevice device in component.devices)
            {
                CollectDeviceLoads(device);
            }

            SupplyConnectedComponent();
        }
    }

    private void CollectConnectedComponent(PowerTransmissionDevice rootDevice)
    {
        _componentDevices.Clear();
        _componentGenerators.Clear();
        _componentLoads.Clear();
        _deviceQueue.Clear();
        _deviceQueue.Enqueue(rootDevice);
        while (_deviceQueue.Count > 0)
        {
            PowerTransmissionDevice device = _deviceQueue.Dequeue();
            _componentDevices.Add(device);
            // 同一个发电机覆盖的中继属于同一电网，不能重复分配该发电机的输出。
            foreach (PowerGeneratingUnit generator in device.connectedGenerators)
            {
                if (generator != null && _componentGenerators.Add(generator))
                {
                    VisitDevices(generator.connectedDevices);
                }
            }

            VisitDevices(device.connectedDevices);
        }
    }

    private void VisitDevices(List<PowerTransmissionDevice> devices)
    {
        foreach (PowerTransmissionDevice device in devices)
        {
            if (device != null && _visitedDevices.Add(device))
            {
                _deviceQueue.Enqueue(device);
            }
        }
    }

    private void CollectDeviceLoads(PowerTransmissionDevice device)
    {
        Vector3 position = device.transform.position;
        _loadIndex.Query(position, device.powerRange, _candidates);
        // 原负载登记顺序影响独立电网叠加的小数舍入，候选按登记索引排序。
        _candidates.Sort();
        foreach (int index in _candidates)
        {
            Power load = _loads[index];
            if (IsWithinBoxRange(_loadPositions[index], position, device.powerRange))
            {
                _componentLoads.Add(load);
            }
        }
    }

    private void SupplyConnectedComponent()
    {
        float totalOutputPower = 0f;
        foreach (PowerGeneratingUnit generator in _componentGenerators)
        {
            totalOutputPower += Mathf.Max(0f, generator.outputPower);
        }

        foreach (PowerTransmissionDevice device in _componentDevices)
        {
            device.availableNetworkPower = totalOutputPower;
        }

        if (totalOutputPower <= 0f || _componentLoads.Count == 0)
        {
            return;
        }

        // 重叠中继只收集一次；独立网络补足剩余需求，已经满足的负载不继续占用输出。
        float remainingDemand = 0f;
        foreach (Power load in _componentLoads)
        {
            remainingDemand += Mathf.Max(0f, load.RequestedPower - load.currentPower);
        }

        if (remainingDemand <= 0f)
        {
            return;
        }

        float supplyRatio = Mathf.Min(1f, totalOutputPower / remainingDemand);
        foreach (Power load in _componentLoads)
        {
            load.ReceivePower(Mathf.Max(0f, load.RequestedPower - load.currentPower) * supplyRatio);
        }
    }

    private void CommitPowerStatuses()
    {
        foreach (Power load in _loads)
        {
            load.CommitPowerStatus();
        }
    }

    private static bool IsWithinBoxRange(Vector3 firstPosition, Vector3 secondPosition, float range)
    {
        Vector3 offset = firstPosition - secondPosition;
        float clampedRange = Mathf.Max(0f, range);
        return Mathf.Abs(offset.x) <= clampedRange && Mathf.Abs(offset.y) <= clampedRange && Mathf.Abs(offset.z) <= clampedRange;
    }
}
