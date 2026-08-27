using System.Collections.Generic;
using UnityEngine;

public class PowerTransmissionDevice : MonoBehaviour
{
    private static readonly HashSet<PowerTransmissionDevice> ActiveDevices = new HashSet<PowerTransmissionDevice>();
    private static readonly List<PowerTransmissionDevice> DeviceBuffer = new List<PowerTransmissionDevice>();
    private static readonly List<PowerGeneratingUnit> GeneratorBuffer = new List<PowerGeneratingUnit>();
    private static readonly List<Power> PowerBuffer = new List<Power>();
    private static readonly HashSet<PowerTransmissionDevice> VisitedDevices = new HashSet<PowerTransmissionDevice>();
    private static readonly HashSet<PowerGeneratingUnit> ComponentGenerators = new HashSet<PowerGeneratingUnit>();
    private static readonly HashSet<Power> ComponentLoads = new HashSet<Power>();
    private static readonly Queue<PowerTransmissionDevice> DeviceQueue = new Queue<PowerTransmissionDevice>();
    private static int lastNetworkUpdateFrame = -1;

    public List<PowerGeneratingUnit> connectedGenerators = new List<PowerGeneratingUnit>();
    public List<PowerTransmissionDevice> connectedDevices = new List<PowerTransmissionDevice>();

    [Min(0)]
    public int powerRange = 5;

    [Min(0)]
    public int maxConnectionDistance = 10;

    public LineRenderer lineRenderer;

    private void OnEnable()
    {
        ActiveDevices.Add(this);
    }

    private void Update()
    {
        if (lastNetworkUpdateFrame == Time.frameCount) return;

        lastNetworkUpdateFrame = Time.frameCount;
        RefreshPowerNetwork();
    }

    private void OnDisable()
    {
        ActiveDevices.Remove(this);
        Disconnect();

        if (ActiveDevices.Count == 0)
        {
            ResetAllPowerBlocks();
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
            float generatorConnectionDistanceSqr = device.maxConnectionDistance * device.maxConnectionDistance;

            foreach (PowerGeneratingUnit generator in GeneratorBuffer)
            {
                if ((generator.transform.position - device.transform.position).sqrMagnitude > generatorConnectionDistanceSqr)
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
                if ((other.transform.position - device.transform.position).sqrMagnitude > connectionDistance * connectionDistance)
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
            DeviceQueue.Clear();
            DeviceQueue.Enqueue(rootDevice);

            while (DeviceQueue.Count > 0)
            {
                PowerTransmissionDevice device = DeviceQueue.Dequeue();
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

            if (ComponentGenerators.Count == 0 || ComponentLoads.Count == 0) continue;

            float totalOutputPower = 0f;
            foreach (PowerGeneratingUnit generator in ComponentGenerators)
            {
                totalOutputPower += Mathf.Max(0f, generator.outputPower);
            }

            float powerPerBlock = totalOutputPower / ComponentLoads.Count;
            foreach (Power powerBlock in ComponentLoads)
            {
                powerBlock.ReceivePower(powerPerBlock);
            }
        }
    }

    private static void CollectDeviceLoads(PowerTransmissionDevice device)
    {
        float powerRangeSqr = device.powerRange * device.powerRange;

        foreach (Power powerBlock in PowerBuffer)
        {
            if ((powerBlock.transform.position - device.transform.position).sqrMagnitude <= powerRangeSqr)
            {
                ComponentLoads.Add(powerBlock);
            }
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, powerRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxConnectionDistance);
    }
}
