using UnityEngine;

[RequireComponent(typeof(CargoHold))]
public class CargoHoldView : MonoBehaviour
{
    [SerializeField] private Transform _contentsRoot;
    [SerializeField] private Transform _liquid;
    [SerializeField] private Renderer _statusLamp;
    [SerializeField, Range(0.1f, 0.95f)] private float _itemScale = 0.8f;
    [SerializeField] private Vector3 _displaySize = Vector3.one * 1.7f;
    [SerializeField] private Color _empty = new Color(0.1f, 0.7f, 1f);
    [SerializeField] private Color _loaded = new Color(0.1f, 1f, 0.35f);
    [SerializeField] private Color _full = new Color(1f, 0.35f, 0.05f);
    private CargoHold _hold;
    private int _revision = -1;
    private MaterialPropertyBlock _properties;

    private void Awake() { _hold = GetComponent<CargoHold>(); _properties = new MaterialPropertyBlock(); }
    private void LateUpdate()
    {
        if (_statusLamp != null)
        {
            Color color = _hold.Free == 0 ? _full : _hold.Used == 0 ? _empty : _loaded;
            if (!_hold.IsOperational) color *= 0.18f;
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_EmissionColor", color * 2f);
            _statusLamp.SetPropertyBlock(_properties);
        }
        if (_revision == _hold.Revision) return;
        _revision = _hold.Revision;
        if (_liquid != null)
        {
            float fill = Mathf.Clamp01((float)_hold.Used / _hold.Capacity);
            _liquid.gameObject.SetActive(fill > 0f);
            _liquid.localScale = new Vector3(_displaySize.x, _displaySize.y * fill, _displaySize.z);
            _liquid.localPosition = Vector3.up * (_displaySize.y * (fill - 1f) * 0.5f);
        }
        if (_contentsRoot == null || _hold.Kind != CargoKind.SpecialPart) return;
        foreach (Transform child in _contentsRoot) Destroy(child.gameObject);
        int side = Mathf.CeilToInt(Mathf.Pow(_hold.Capacity, 1f / 3f));
        float cell = Mathf.Min(_displaySize.x, _displaySize.y, _displaySize.z) / side;
        for (int i = 0; i < _hold.Contents.Count; i++)
        {
            GameObject display = CargoVisual.Create(_hold.Contents[i].resourcePath, _contentsRoot, cell * _itemScale);
            if (display == null) continue;
            display.transform.localPosition = new Vector3(i % side, i / (side * side), i / side % side) * cell
                - Vector3.one * ((side - 1) * cell * 0.5f);
        }
    }
}
