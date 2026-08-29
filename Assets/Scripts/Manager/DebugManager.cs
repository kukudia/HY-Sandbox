using UnityEngine;

public class DebugManager : MonoBehaviour
{
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

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
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
    }

    public void TogglePowerConnections()
    {
        showPowerConnections = !showPowerConnections;
    }
}
