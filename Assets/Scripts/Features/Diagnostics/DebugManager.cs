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
    private PowerRangeUnionView _powerRanges;
    private PowerRangeUnionView PowerRanges => _powerRanges ?? (_powerRanges = new PowerRangeUnionView(transform));

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

        GameObject canvasObject = new GameObject("Block Status Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        int uiLayer = LayerMask.NameToLayer("UI");
        canvasObject.layer = uiLayer >= 0 ? uiLayer : gameObject.layer;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -1;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        statusIconRoot = canvasObject.GetComponent<RectTransform>();
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
