using UnityEngine;

[DisallowMultipleComponent]
public sealed class HoverControllerStatusLight : MonoBehaviour
{
    [SerializeField] private HoverFlightController _controller;
    [SerializeField] private Renderer _lens;
    [SerializeField] private Light _glow;
    [SerializeField] private Color _idleColor = new Color(1f, 0.34f, 0.28f);
    [SerializeField] private Color _usedColor = new Color(0.23f, 1f, 0.55f);

    private MaterialPropertyBlock _properties;
    private bool _lastUsed;

    private void Awake()
    {
        _properties = new MaterialPropertyBlock();
        _lastUsed = _controller != null && !_controller.IsUsedByControlUnit;
    }

    private void Update()
    {
        if (_controller == null || _lens == null) return;
        bool used = _controller.IsUsedByControlUnit;
        if (used == _lastUsed) return;
        _lastUsed = used;
        Color color = used ? _usedColor : _idleColor;
        _lens.GetPropertyBlock(_properties);
        _properties.SetColor("_BaseColor", color);
        _properties.SetColor("_EmissionColor", color * 2f);
        _lens.SetPropertyBlock(_properties);
        if (_glow != null)
        {
            _glow.color = color;
            _glow.intensity = used ? 1.3f : 0.5f;
        }
    }
}
