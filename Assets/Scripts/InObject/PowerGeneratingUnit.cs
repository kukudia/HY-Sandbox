using System.Collections.Generic;
using UnityEngine;

public class PowerGeneratingUnit : MonoBehaviour
{
    private static readonly HashSet<PowerGeneratingUnit> ActiveGenerators = new HashSet<PowerGeneratingUnit>();

    [Min(0f)]
    public float outputPower = 1000f;

    public List<PowerTransmissionDevice> connectedDevices = new List<PowerTransmissionDevice>();

    public static IEnumerable<PowerGeneratingUnit> ActiveUnits => ActiveGenerators;

    private void OnEnable()
    {
        ActiveGenerators.Add(this);
    }

    private void OnDisable()
    {
        ActiveGenerators.Remove(this);

        foreach (PowerTransmissionDevice device in connectedDevices)
        {
            if (device != null)
            {
                device.connectedGenerators.Remove(this);
            }
        }

        connectedDevices.Clear();
    }
}
