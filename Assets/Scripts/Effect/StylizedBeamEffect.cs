using UnityEngine;

/// <summary>Endpoint adapter for the editable GPU beam graph.</summary>
[DisallowMultipleComponent]
public class StylizedBeamEffect : MonoBehaviour
{
    private VfxEffect _beam;
    private Vector3 _start;
    private Vector3 _end;
    private float _width = 0.04f;
    private float _intensity = 1f;
    private Color _color = Color.cyan;
    private bool _visible;
    public void Configure(float width)
    { _width = Mathf.Max(0.001f, width); }
    public void SetEndpoints(Vector3 start, Vector3 end) { _start = start; _end = end; UpdateBeam(); }
    public void SetColor(Color color) { _color = color; if (_beam != null) _beam.SetColor(color); }
    public void SetIntensity(float value) { _intensity = Mathf.Clamp01(value); if (_beam != null) _beam.SetIntensity(_visible ? _intensity : 0); }
    public void SetVisible(bool value)
    {
        _visible = value;
        if (_beam == null && value && BlockVfxLibrary.Instance != null && BlockVfxLibrary.Instance.Beam != null)
        {
            _beam = Instantiate(BlockVfxLibrary.Instance.Beam, transform);
            _beam.SetColor(_color);
        }
        if (_beam == null) return;
        if (value) _beam.SetIntensity(_intensity); else _beam.Clear();
        UpdateBeam();
    }
    private void UpdateBeam()
    {
        if (_beam == null) return;
        Vector3 delta = _end - _start;
        _beam.transform.position = _start;
        if (delta.sqrMagnitude > 0.000001f) _beam.transform.rotation = Quaternion.LookRotation(delta);
        // Parent may be a scaled part; use the reciprocal world scale for endpoint accuracy.
        Vector3 parentScale = transform.lossyScale;
        _beam.transform.localScale = new Vector3(_width / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            _width / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), delta.magnitude / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
    }
    private void OnDisable() { if (_beam != null) _beam.Clear(); }
}
