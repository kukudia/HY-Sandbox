// 职责：重建无线供电图，按连通分量将发电量均分给范围内的唯一负载。
// 入口：Rebuild()；由 PowerTransmissionDevice 每帧装配已启用的中继、发电机和负载。
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
    /// <summary>最近一次重建使用的中继快照，供外部调试视图读取。</summary>
    public IList<PowerTransmissionDevice> Devices => _devices;

    /// <summary>从注册表重建电网：清除旧供电，累计独立网络的输出，再统一提交负载状态。</summary>
    public void Rebuild(
        IEnumerable<PowerTransmissionDevice> devices,
        IEnumerable<PowerGeneratingUnit> generators,
        IEnumerable<Power> loads)
    {
        CollectActiveParticipants(devices, generators, loads);
        ResetConnectionsAndPower();
        BuildConnections();
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

    private void ResetConnectionsAndPower()
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

        foreach (Power load in _loads)
        {
            load.ResetPower();
        }
    }

    private void BuildConnections()
    {
        for (int i = 0; i < _devices.Count; i++)
        {
            PowerTransmissionDevice device = _devices[i];
            ConnectGenerators(device);
            // 中继的双向连接必须同时满足两端范围，且每一对只检查一次。
            for (int j = i + 1; j < _devices.Count; j++)
            {
                PowerTransmissionDevice other = _devices[j];
                float connectionDistance = Mathf.Min(device.maxConnectionDistance, other.maxConnectionDistance);
                if (IsWithinBoxRange(other.transform.position, device.transform.position, connectionDistance))
                {
                    device.connectedDevices.Add(other);
                    other.connectedDevices.Add(device);
                }
            }
        }
    }

    private void ConnectGenerators(PowerTransmissionDevice device)
    {
        foreach (PowerGeneratingUnit generator in _generators)
        {
            if (IsWithinBoxRange(generator.transform.position, device.transform.position, device.maxConnectionDistance))
            {
                device.connectedGenerators.Add(generator);
                generator.connectedDevices.Add(device);
            }
        }
    }

    private void DistributeNetworkPower()
    {
        _visitedDevices.Clear();
        foreach (PowerTransmissionDevice rootDevice in _devices)
        {
            if (_visitedDevices.Add(rootDevice))
            {
                CollectConnectedComponent(rootDevice);
                SupplyConnectedComponent();
            }
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
            CollectDeviceLoads(device);
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
        foreach (Power load in _loads)
        {
            if (IsWithinBoxRange(load.transform.position, device.transform.position, device.powerRange))
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

        // 组件内部重叠覆盖只供一次；多个独立组件仍能向同一个负载叠加供电。
        float powerPerLoad = totalOutputPower / _componentLoads.Count;
        foreach (Power load in _componentLoads)
        {
            load.ReceivePower(powerPerLoad);
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
