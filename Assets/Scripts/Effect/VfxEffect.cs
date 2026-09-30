using System.Collections.Generic;
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
    private const int MaxActiveBursts = 64;
    private static readonly List<VfxEffect> ActiveBursts = new List<VfxEffect>(MaxActiveBursts);
    private bool _priorityBurst;
    public static bool CanSpawnBurst => ActiveBursts.Count < MaxActiveBursts;
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
    public static bool MakeRoomForBurst(bool priority)
    {
        for (int i = ActiveBursts.Count - 1; i >= 0; i--)
            if (ActiveBursts[i] == null) ActiveBursts.RemoveAt(i);
        if (CanSpawnBurst) return true;
        if (!priority) return false;

        // Evict only lower-priority bursts; an explosion's light may still be active.
        for (int i = 0; i < ActiveBursts.Count; i++)
        {
            VfxEffect oldest = ActiveBursts[i];
            if (oldest._priorityBurst) continue;
            ActiveBursts.RemoveAt(i);
            oldest.gameObject.SetActive(false);
            Destroy(oldest.gameObject);
            return true;
        }
        return false;
    }

    public void ReleaseAfterPlayback(bool priority = false)
    {
        if (_oneShot) return;
        _oneShot = true;
        _priorityBurst = priority;
        ActiveBursts.Add(this);
        Destroy(gameObject, _releaseAfter);
    }
    public void StopAndRelease() { SetIntensity(0f); transform.SetParent(null, true); ReleaseAfterPlayback(); }
    private void OnDisable() => Clear();
    private void OnDestroy() { if (_oneShot) ActiveBursts.Remove(this); }
}
