using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class StatusIcon : MonoBehaviour
{
    private const float IconSize = 42f;
    private const float IconSpacing = 4f;
    private static readonly HashSet<StatusIcon> ActiveInfos = new HashSet<StatusIcon>();

    [Header("Static")]
    public string blockName;
    public string description;

    [Header("Dynamic")]
    public ConnectionStatus connectionStatus = ConnectionStatus.Normal;
    public DurabilityStatus durabilityStatus = DurabilityStatus.Normal;
    public PowerStatus powerStatus = PowerStatus.Normal;

    [Header("Display")]
    [Min(0f)] public float iconHeightOffset = 0.25f;

    private Block _block;
    private Durability _durability;
    private Power _power;
    private RectTransform _iconRoot;
    private CanvasGroup _iconGroup;
    private Transform _center;
    private Image _connectionIcon;
    private Image _durabilityIcon;
    private Image _powerIcon;
    private DebugManager _lastDebugManager;
    private bool _isOutsideViewport;

    private void Awake()
    {
        _block = GetComponent<Block>();
        _durability = GetComponent<Durability>();
        _power = GetComponent<Power>();
        _center = transform.Find("Center");
    }

    private void OnEnable()
    {
        ActiveInfos.Add(this);
        connectionStatus = ConnectionStatus.Normal;
        durabilityStatus = DurabilityStatus.Normal;
        powerStatus = PowerStatus.Normal;
        _lastDebugManager = null;
    }

    private void LateUpdate()
    {
        DebugManager manager = DebugManager.instance;
        if (manager == null)
        {
            HideIcons();
            return;
        }

        if (_lastDebugManager != manager)
        {
            _lastDebugManager = manager;
            RefreshIcons(manager);
        }

        UpdateIconScreenPosition(manager);
    }

    private void OnDisable()
    {
        ActiveInfos.Remove(this);
        HideIcons();
    }

    private void OnDestroy()
    {
        if (_iconGroup != null && IconManager.instance != null)
        {
            IconManager.instance.Unregister(_iconGroup);
        }
        if (_iconRoot != null)
        {
            Destroy(_iconRoot.gameObject);
        }
    }

    public void CheckConnectionStatus()
    {
        ConnectionStatus nextStatus = ConnectionStatus.Normal;
        if (_block == null || _block.connectors == null || _block.connectors.Count == 0)
        {
            SetConnectionStatus(nextStatus);
            return;
        }

        nextStatus = ConnectionStatus.NoConnection;
        for (int i = 0; i < _block.connectors.Count; i++)
        {
            Connector connector = _block.connectors[i];
            if (connector != null && connector.isConnected)
            {
                nextStatus = ConnectionStatus.Normal;
                break;
            }
        }

        SetConnectionStatus(nextStatus);
    }

    public void CheckDurabilityStatus()
    {
        DurabilityStatus nextStatus;
        if (_durability == null || _durability.maxDurability <= 0f)
        {
            nextStatus = DurabilityStatus.Normal;
        }
        else if (_durability.currentDurability <= 0f)
        {
            nextStatus = DurabilityStatus.Broken;
        }
        else if (_durability.currentDurability < _durability.maxDurability)
        {
            nextStatus = DurabilityStatus.Damaged;
        }
        else
        {
            nextStatus = DurabilityStatus.Normal;
        }

        if (durabilityStatus != nextStatus)
        {
            durabilityStatus = nextStatus;
            RefreshStatusIcons();
        }
    }

    public void CheckPowerStatus()
    {
        PowerStatus nextStatus;
        if (_power == null || _power.standardWorkingPower <= 0f)
        {
            nextStatus = PowerStatus.Normal;
        }
        else if (!_power.isWorking)
        {
            nextStatus = PowerStatus.NoPower;
        }
        else if (_power.efficiency < 1f)
        {
            nextStatus = PowerStatus.UnderPower;
        }
        else
        {
            nextStatus = PowerStatus.Normal;
        }

        if (powerStatus != nextStatus)
        {
            powerStatus = nextStatus;
            RefreshStatusIcons();
        }
    }

    private void SetConnectionStatus(ConnectionStatus nextStatus)
    {
        if (connectionStatus == nextStatus) return;

        connectionStatus = nextStatus;
        RefreshStatusIcons();
    }

    public void RefreshStatusIcons()
    {
        DebugManager manager = DebugManager.instance;
        if (manager != null)
        {
            _lastDebugManager = manager;
            RefreshIcons(manager);
        }
    }

    public static void RefreshAllStatusIcons()
    {
        foreach (StatusIcon info in ActiveInfos)
        {
            if (info != null)
            {
                info.RefreshStatusIcons();
            }
        }
    }

    private void RefreshIcons(DebugManager manager)
    {
        bool showConnection = manager.showConnectionStatus && connectionStatus != ConnectionStatus.Normal;
        bool showDurability = manager.showDurabilityStatus && durabilityStatus != DurabilityStatus.Normal;
        bool showPower = manager.showPowerStatus && powerStatus != PowerStatus.Normal;

        if (!showConnection && !showDurability && !showPower)
        {
            HideIcons();
            return;
        }

        EnsureIconVisuals(manager);
        if (_iconRoot == null) return;

        int visibleCount = 0;
        visibleCount += SetIcon(_connectionIcon, showConnection ? connectionStatus.ToString() : null) ? 1 : 0;
        visibleCount += SetIcon(_durabilityIcon, showDurability ? durabilityStatus.ToString() : null) ? 1 : 0;
        visibleCount += SetIcon(_powerIcon, showPower ? powerStatus.ToString() : null) ? 1 : 0;

        _iconRoot.gameObject.SetActive(visibleCount > 0);
        PositionVisibleIcons(visibleCount);
    }

    private void EnsureIconVisuals(DebugManager manager)
    {
        if (_iconRoot != null) return;

        RectTransform overlayRoot = manager.StatusIconRoot;
        if (overlayRoot == null) return;

        GameObject rootObject = new GameObject($"{name} Status Icons", typeof(RectTransform), typeof(CanvasGroup));
        _iconRoot = rootObject.GetComponent<RectTransform>();
        _iconRoot.SetParent(overlayRoot, false);
        _iconGroup = rootObject.GetComponent<CanvasGroup>();
        IconManager.instance?.Register(_iconGroup);
        _iconRoot.localScale = Vector3.one;
        _iconRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _iconRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _iconRoot.pivot = new Vector2(0.5f, 0.5f);

        _connectionIcon = CreateIcon("Connection");
        _durabilityIcon = CreateIcon("Durability");
        _powerIcon = CreateIcon("Power");
    }

    private Image CreateIcon(string iconName)
    {
        GameObject iconObject = new GameObject(iconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconTransform = iconObject.GetComponent<RectTransform>();
        iconTransform.SetParent(_iconRoot, false);
        iconTransform.sizeDelta = new Vector2(IconSize, IconSize);

        Image image = iconObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private bool SetIcon(Image image, string statusName)
    {
        Sprite sprite = string.IsNullOrEmpty(statusName) ? null : FindStatusIcon(statusName);
        image.sprite = sprite;
        image.enabled = sprite != null;
        return image.enabled;
    }

    private Sprite FindStatusIcon(string statusName)
    {
        if (IconManager.instance == null) return null;

        for (int i = 0; i < IconManager.instance.statuses.Count; i++)
        {
            Status status = IconManager.instance.statuses[i];
            if (status != null && status.name == statusName)
            {
                return status.icon;
            }
        }

        return null;
    }

    private void PositionVisibleIcons(int visibleCount)
    {
        float totalWidth = visibleCount * IconSize + Mathf.Max(0, visibleCount - 1) * IconSpacing;
        float x = -totalWidth * 0.5f + IconSize * 0.5f;
        PositionIcon(_connectionIcon, ref x);
        PositionIcon(_durabilityIcon, ref x);
        PositionIcon(_powerIcon, ref x);
        _iconRoot.sizeDelta = new Vector2(totalWidth, IconSize);
    }

    private static void PositionIcon(Image image, ref float x)
    {
        if (!image.enabled) return;

        image.rectTransform.anchoredPosition = new Vector2(x, 0f);
        x += IconSize + IconSpacing;
    }

    private void UpdateIconScreenPosition(DebugManager manager)
    {
        if (_iconRoot == null || (!_iconRoot.gameObject.activeSelf && !_isOutsideViewport)) return;

        Camera camera = PlayManager.instance != null && PlayManager.instance.mainCamera != null
            ? PlayManager.instance.mainCamera
            : Camera.main;
        if (camera == null)
        {
            _iconRoot.gameObject.SetActive(false);
            return;
        }

        Vector3 centerPosition = _center != null ? _center.position : transform.position;
        Vector3 screenPosition = camera.WorldToScreenPoint(centerPosition);
        bool isVisible = screenPosition.z > 0f
            && screenPosition.x >= 0f && screenPosition.x <= Screen.width
            && screenPosition.y >= 0f && screenPosition.y <= Screen.height;
        if (!isVisible)
        {
            _isOutsideViewport = true;
            _iconRoot.gameObject.SetActive(false);
            return;
        }


        if (_isOutsideViewport)
        {
            _isOutsideViewport = false;
            RefreshIcons(manager);
            if (!_iconRoot.gameObject.activeSelf) return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.StatusIconRoot, screenPosition, null, out Vector2 localPoint))
        {
            _iconRoot.anchoredPosition = localPoint;
        }
    }

    private void HideIcons()
    {
        if (_iconRoot != null)
        {
            _iconRoot.gameObject.SetActive(false);
        }
    }
}

public enum ConnectionStatus
{
    Normal,
    NoConnection,
}

public enum DurabilityStatus
{
    Normal,
    Damaged,
    Broken,
}

public enum PowerStatus
{
    Normal,
    UnderPower,
    NoPower,
}
