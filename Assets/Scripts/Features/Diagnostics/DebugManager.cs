// 职责：保存调试显示配置和状态图标 Canvas；供电范围网格由 PowerRangeUnionView 拥有。
// 入口：GetPowerRangeColor() / TogglePowerRange() / TogglePowerConnections()。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugManager : MonoBehaviour
{
    public static DebugManager instance;
    [Header("Power Debug Visibility")]
    public bool showPowerRange = false;
    public bool showPowerConnections = false;
    [Header("Block Status Visibility")]
    public bool showConnectionStatus = false;
    public bool showDurabilityStatus = false;
    public bool showPowerStatus = false;
    [Header("Power Range Colors")]
    public Color poweredRangeColor = new Color(0.15f, 1f, 0.45f, 0.16f);
    public Color connectedRangeColor = new Color(1f, 0.78f, 0.12f, 0.14f);
    public Color isolatedRangeColor = new Color(1f, 0.2f, 0.12f, 0.12f);
    [Header("Power Connection Lines")]
    public Color generatorConnectionColor = new Color(0.2f, 1f, 0.45f, 0.95f);
    public Color deviceConnectionColor = new Color(0.15f, 0.75f, 1f, 0.9f);
    [Min(0.005f)]
    public float connectionLineWidth = 0.055f;
    [Min(0.02f)]
    public float connectionDashLength = 0.35f;
    [Min(0f)]
    public float connectionDashScrollSpeed = 0.45f;
    [Header("AI Debug")]
    [SerializeField, Tooltip("绘制所有自主友舰的目标、移动意图与避障采样。")]
    private bool showFriendlyAI;
    [SerializeField, Tooltip("绘制所有敌机的目标、移动意图与避障采样。")]
    private bool showEnemyAI;
    public bool ShowsAI(UnitFaction faction) => faction == UnitFaction.Player ? showFriendlyAI : faction == UnitFaction.Enemy && showEnemyAI;
    private PowerRangeUnionView _powerRanges;
    private PowerRangeUnionView PowerRanges => _powerRanges ?? (_powerRanges = new PowerRangeUnionView(transform));

    [SerializeField]
    private RectTransform statusIconRoot;
    public RectTransform StatusIconRoot
    {
        get
        {
            EnsureStatusIconCanvas();
            return statusIconRoot;
        }
    }

    private void Awake()
    {
        instance = this;
        EnsureStatusIconCanvas();
        PowerRanges.Hide();
    }

    private void OnDestroy()
    {
        _powerRanges?.Dispose();
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
        if (!showPowerRange)
        {
            _powerRanges?.Hide();
        }
        else
        {
            _powerRanges?.Invalidate();
        }
    }

    public void TogglePowerConnections()
    {
        showPowerConnections = !showPowerConnections;
    }

    public void ToggleConnectionStatus()
    {
        showConnectionStatus = !showConnectionStatus;
        StatusIcon.RefreshAllStatusIcons();
    }

    public void ToggleDurabilityStatus()
    {
        showDurabilityStatus = !showDurabilityStatus;
        StatusIcon.RefreshAllStatusIcons();
    }

    public void TogglePowerStatus()
    {
        showPowerStatus = !showPowerStatus;
        StatusIcon.RefreshAllStatusIcons();
    }

    private void EnsureStatusIconCanvas()
    {
        if (statusIconRoot != null)
        {
            return;
        }

        throw new MissingReferenceException("DebugManager requires an authored Block Status Overlay reference.");
    }

    internal void RefreshPowerRangeMesh(IList<PowerTransmissionDevice> devices)
    {
        PowerRanges.Refresh(devices, showPowerRange, isolatedRangeColor, connectedRangeColor, poweredRangeColor);
    }

    internal void ClearPowerRangeMesh()
    {
        PowerRanges.Clear();
    }
}
