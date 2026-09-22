using UnityEngine;
using UnityEngine.Rendering;

/// <summary>One shared flat ring mesh; no connector GameObjects, colliders or per-frame materials.</summary>
public sealed class ConnectorPlacementHints : MonoBehaviour
{
    [SerializeField] private Mesh _outlineMesh;
    [SerializeField] private Material _outlineMaterial;
    [SerializeField, Min(0.001f)] private float _surfaceOffset = 0.015f;
    [SerializeField, Min(0.1f)] private float _size = 0.9f;
    private Block _target;
    public int VisibleCount { get; private set; }

    public void Show(Block target) { _target = target; }
    public void Hide() { _target = null; VisibleCount = 0; }
    private void OnDisable() { Hide(); }

    private void LateUpdate()
    {
        VisibleCount = 0;
        if (_target == null || !_target.gameObject.activeInHierarchy || _outlineMesh == null || _outlineMaterial == null) return;
        foreach (Connector connector in _target.connectors)
        {
            if (!connector.canConnect || connector.isConnected) continue;
            Vector3 normal = _target.GetConnectorWorldNormal(connector);
            if (normal.sqrMagnitude < 0.5f) continue;
            Vector3 up = Vector3.ProjectOnPlane(_target.transform.up, normal);
            if (up.sqrMagnitude < 0.01f) up = Vector3.ProjectOnPlane(_target.transform.forward, normal);
            Vector3 position = _target.GetConnectorWorldPosition(connector) + normal * _surfaceOffset;
            Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.LookRotation(normal, up), Vector3.one * _size);
            Graphics.DrawMesh(_outlineMesh, matrix, _outlineMaterial, 2, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
            VisibleCount++;
        }
    }
}
