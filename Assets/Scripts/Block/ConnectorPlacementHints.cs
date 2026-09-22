using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Discovers nearby blocks on a short interval; shared geometry fades independently per connector.</summary>
public sealed class ConnectorPlacementHints : MonoBehaviour
{
    [SerializeField] private Mesh _outlineMesh;
    [SerializeField] private Material _outlineMaterial;
    [SerializeField, Min(0.001f)] private float _surfaceOffset = 0.015f;
    [SerializeField, Min(0.1f)] private float _size = 0.9f;
    [SerializeField, Min(0.01f)] private float _fadeInSeconds = 0.18f;
    [SerializeField, Min(0.01f)] private float _fadeOutSeconds = 0.25f;
    [SerializeField, Min(0.02f)] private float _scanInterval = 0.1f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly Dictionary<Connector, Hint> _hints = new Dictionary<Connector, Hint>();
    private readonly HashSet<Block> _blocks = new HashSet<Block>();
    private readonly List<Connector> _expired = new List<Connector>();
    private MaterialPropertyBlock _properties;
    private Collider[] _overlaps = new Collider[128];
    private Camera _camera;
    private GameObject _excludedGhost;
    private int _layerMask;
    private float _radius;
    private float _nextScan;
    private bool _show;
    public int VisibleCount { get; private set; }
    public int AvailableCount { get; private set; }

    private sealed class Hint
    {
        public Block block;
        public float alpha;
        public Matrix4x4 matrix;
    }

    public void Show(Camera camera, float radius, LayerMask layerMask, GameObject excludedGhost = null)
    {
        if (camera == null) { Hide(); return; }
        if (!_show || _camera != camera || _layerMask != layerMask.value) _nextScan = 0f;
        _show = true; _camera = camera; _radius = Mathf.Max(0f, radius);
        _layerMask = layerMask.value; _excludedGhost = excludedGhost;
    }

    public void Hide() { _show = false; AvailableCount = 0; }
    private void OnDisable() { _hints.Clear(); _blocks.Clear(); VisibleCount = 0; Hide(); }

    private void ScanNearbyBlocks()
    {
        int count;
        // Grow only when crowded; never silently omit connectors when the reusable query buffer fills up.
        while ((count = Physics.OverlapSphereNonAlloc(_camera.transform.position, _radius + 1f, _overlaps, _layerMask, QueryTriggerInteraction.Ignore)) == _overlaps.Length)
            System.Array.Resize(ref _overlaps, _overlaps.Length * 2);
        _blocks.Clear();
        for (int i = 0; i < count; i++)
        {
            Block block = _overlaps[i].GetComponentInParent<Block>();
            _overlaps[i] = null;
            if (block == null || block.gameObject == _excludedGhost || !_blocks.Add(block)) continue;
            foreach (Connector connector in block.connectors)
                if (CanDisplay(block, connector) && !_hints.ContainsKey(connector)) _hints.Add(connector, new Hint { block = block });
        }
    }

    private bool CanDisplay(Block block, Connector connector)
    {
        return _show && _camera != null && block != null && block.gameObject.activeInHierarchy
            && block.gameObject != _excludedGhost && block.IsConnectorAvailableForPlacement(connector)
            && (_camera.transform.position - block.GetConnectorWorldPosition(connector)).sqrMagnitude <= _radius * _radius;
    }

    private void LateUpdate()
    {
        VisibleCount = 0; AvailableCount = 0;
        if (_outlineMesh == null || _outlineMaterial == null) return;
        if (_properties == null) _properties = new MaterialPropertyBlock();
        if (_show && _camera != null && Time.unscaledTime >= _nextScan)
        {
            ScanNearbyBlocks();
            _nextScan = Time.unscaledTime + Mathf.Max(0.02f, _scanInterval);
        }
        _expired.Clear();
        foreach (KeyValuePair<Connector, Hint> pair in _hints)
        {
            Connector connector = pair.Key;
            Hint hint = pair.Value;
            bool available = CanDisplay(hint.block, connector);
            if (available) AvailableCount++;
            hint.alpha = Mathf.MoveTowards(hint.alpha, available ? 1f : 0f,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, available ? _fadeInSeconds : _fadeOutSeconds));
            if (hint.alpha <= 0f) { _expired.Add(connector); continue; }
            // A destroyed block retains its final transform until fade-out finishes.
            if (hint.block != null)
            {
                Vector3 normal = hint.block.GetConnectorWorldNormal(connector);
                if (normal.sqrMagnitude < 0.5f) continue;
                Vector3 up = Vector3.ProjectOnPlane(hint.block.transform.up, normal);
                if (up.sqrMagnitude < 0.01f) up = Vector3.ProjectOnPlane(hint.block.transform.forward, normal);
                Vector3 position = hint.block.GetConnectorWorldPosition(connector) + normal * _surfaceOffset;
                hint.matrix = Matrix4x4.TRS(position, Quaternion.LookRotation(normal, up), Vector3.one * _size);
            }
            _properties.SetColor(BaseColorId, new Color(1f, 1f, 1f, hint.alpha));
            Graphics.DrawMesh(_outlineMesh, hint.matrix, _outlineMaterial, 2, _camera, 0, _properties, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
            VisibleCount++;
        }
        foreach (Connector connector in _expired) _hints.Remove(connector);
    }
}
