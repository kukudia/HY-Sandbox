using UnityEngine;
using UnityEngine.VFX;

/// <summary>Controls authored GPU graphs; stopping emission keeps existing smoke alive.</summary>
[DisallowMultipleComponent]
public sealed class VfxEffect : MonoBehaviour
{
    [SerializeField] private VisualEffect[] _graphs = System.Array.Empty<VisualEffect>();
    [SerializeField, Min(0.1f)] private float _releaseAfter = 12f;
    [SerializeField] private bool _playOnEnable;
    private bool _emitting;
    private bool _oneShot;
    private static int _activeBursts;
    public static bool CanSpawnBurst => _activeBursts < 64;
    public VisualEffect[] Graphs => _graphs;
    public bool IsEmitting => _emitting;
    private void OnEnable() { if (_playOnEnable) SetIntensity(1f); }
    public void SetIntensity(float value)
    {
        value = Mathf.Clamp01(value);
        bool active = value > 0f;
        for (int i = 0; i < _graphs.Length; i++)
        {
            var graph = _graphs[i];
            if (graph == null) continue;
            // Keep the last opacity on stop so the authored tail can decay.
            if (active && graph.HasFloat("Intensity")) graph.SetFloat("Intensity", value);
            if (graph.HasFloat("ScaleWSP")) graph.SetFloat("ScaleWSP", graph.transform.lossyScale.magnitude / Mathf.Sqrt(3f));
            if (active && !_emitting) graph.Play();
            else if (!active && _emitting) graph.Stop();
        }
        _emitting = active;
    }
    public void SetColor(Color color)
    {
        foreach (var graph in _graphs)
            if (graph != null && graph.HasVector3("Tint")) graph.SetVector3("Tint", new Vector3(color.r, color.g, color.b));
    }
    public void SetSpawnCount(float count)
    {
        foreach (var graph in _graphs)
            if (graph != null && graph.HasFloat("SpawnCount")) graph.SetFloat("SpawnCount", count);
    }
    public void Clear()
    {
        foreach (var graph in _graphs)
            if (graph != null) { graph.initialEventName = "ControlledStart"; graph.Reinit(); graph.Stop(); }
        _emitting = false;
    }
    public void PlayOnce() { Clear(); SetIntensity(1f); }
    public void ReleaseAfterPlayback()
    {
        if (_oneShot) return;
        _oneShot = true; _activeBursts++;
        Destroy(gameObject, _releaseAfter);
    }
    public void StopAndRelease() { SetIntensity(0f); transform.SetParent(null, true); ReleaseAfterPlayback(); }
    private void OnDisable() => Clear();
    private void OnDestroy() { if (_oneShot) _activeBursts = Mathf.Max(0, _activeBursts - 1); }
}
