// 职责：由集中配置库生成挂载特效，封装播放、停止、参数与尾烟处理。
// 入口：SetIntensity() / SetColor() / SetSpawnCount()。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>Controls authored GPU graphs; stopping emission keeps existing smoke alive.</summary>
[DisallowMultipleComponent]
public sealed class VfxEffect : MonoBehaviour
{
    [SerializeField]
    private VisualEffect[] _graphs = System.Array.Empty<VisualEffect>();
    [SerializeField, Min(0.1f)]
    private float _releaseAfter = 12f;
    [SerializeField]
    private bool _playOnEnable;
    [SerializeField, Tooltip("挂载点只保存效果编号；具体 Prefab 由 BlockVfxLibrary 统一配置。模板自身留空。")]
    private string _effectId;
    [SerializeField, Tooltip("组合效果的子挂载点，不直接引用其内部 Graph。")]
    private VfxEffect[] _members = System.Array.Empty<VfxEffect>();
    private bool _resolved;
    public string EffectId => _effectId;
    public bool IsTemplate => string.IsNullOrEmpty(_effectId) && _members.Length == 0;
    public bool IsGroup => _members.Length > 0;

    private bool _emitting;
    private bool _oneShot;
    private const int MaxActiveBursts = 64;
    private static readonly List<VfxEffect> ActiveBursts = new List<VfxEffect>(MaxActiveBursts);
    private bool _priorityBurst;
    public static bool CanSpawnBurst => ActiveBursts.Count < MaxActiveBursts;

    public VisualEffect[] Graphs
    {
        get
        {
            EnsureContent();
            return _graphs;
        }
    }

    public bool IsEmitting => _emitting;

    private void EnsureContent()
    {
        if (_resolved || !Application.isPlaying)
        {
            return;
        }

        _resolved = true;
        if (_members.Length > 0)
        {
            var graphs = new List<VisualEffect>();
            foreach (VfxEffect member in _members)
            {
                if (member != null && member != this)
                {
                    graphs.AddRange(member.Graphs);
                }
            }

            _graphs = graphs.ToArray();
            return;
        }

        if (string.IsNullOrEmpty(_effectId))
        {
            return;
        }

        VfxEffect template = BlockVfxLibrary.Instance != null ? BlockVfxLibrary.Instance.Resolve(_effectId) : null;
        if (template == null || !template.IsTemplate || template._graphs.Length == 0)
        {
            Debug.LogError("Missing or recursive VFX catalog entry: " + _effectId, this);
            return;
        }

        // 在非活动父节点下创建，避免模板自动播放先于挂载点的控制逻辑。
        var content = new GameObject("Catalog content - " + _effectId);
        content.SetActive(false);
        content.transform.SetParent(transform, false);
        VfxEffect instance = Instantiate(template, content.transform, false);
        instance._playOnEnable = false;
        instance.enabled = false;
        _graphs = instance._graphs;
        _releaseAfter = instance._releaseAfter;
        instance._graphs = System.Array.Empty<VisualEffect>();
        Destroy(instance);
        content.SetActive(true);
        Clear();
    }

    private void OnEnable()
    {
        EnsureContent();
        if (_playOnEnable)
        {
            SetIntensity(1f);
        }
    }

    public void SetIntensity(float value)
    {
        EnsureContent();
        value = Mathf.Clamp01(value);
        bool active = value > 0f;
        for (int i = 0; i < _graphs.Length; i++)
        {
            var graph = _graphs[i];
            if (graph == null)
            {
                continue;
            }

            // Keep the last opacity on stop so the authored tail can decay.
            if (active && graph.HasFloat("Intensity"))
            {
                graph.SetFloat("Intensity", value);
            }

            if (graph.HasFloat("ScaleWSP"))
            {
                graph.SetFloat("ScaleWSP", graph.transform.lossyScale.magnitude / Mathf.Sqrt(3f));
            }

            if (active && !_emitting)
            {
                graph.Play();
            }
            else if (!active && _emitting)
            {
                graph.Stop();
            }
        }

        _emitting = active;
    }

    public void SetColor(Color color)
    {
        EnsureContent();
        foreach (var graph in _graphs)
        {
            if (graph != null && graph.HasVector3("Tint"))
            {
                graph.SetVector3("Tint", new Vector3(color.r, color.g, color.b));
            }
        }
    }

    public void SetSpawnCount(float count)
    {
        EnsureContent();
        foreach (var graph in _graphs)
        {
            if (graph != null && graph.HasFloat("SpawnCount"))
            {
                graph.SetFloat("SpawnCount", count);
            }
        }
    }

    public void Clear()
    {
        foreach (var graph in _graphs)
        {
            if (graph != null)
            {
                graph.initialEventName = "ControlledStart";
                graph.Reinit();
                graph.Stop();
            }
        }

        _emitting = false;
    }

    public void PlayOnce()
    {
        Clear();
        SetIntensity(1f);
    }

    public static bool MakeRoomForBurst(bool priority)
    {
        for (int i = ActiveBursts.Count - 1; i >= 0; i--)
        {
            if (ActiveBursts[i] == null)
            {
                ActiveBursts.RemoveAt(i);
            }
        }

        if (CanSpawnBurst)
        {
            return true;
        }

        if (!priority)
        {
            return false;
        }

        // Evict only lower-priority bursts; an explosion's light may still be active.
        for (int i = 0; i < ActiveBursts.Count; i++)
        {
            VfxEffect oldest = ActiveBursts[i];
            if (oldest._priorityBurst)
            {
                continue;
            }

            ActiveBursts.RemoveAt(i);
            oldest.gameObject.SetActive(false);
            Destroy(oldest.gameObject);
            return true;
        }

        return false;
    }

    public void ReleaseAfterPlayback(bool priority = false)
    {
        if (_oneShot)
        {
            return;
        }

        _oneShot = true;
        _priorityBurst = priority;
        ActiveBursts.Add(this);
        Destroy(gameObject, _releaseAfter);
    }

    public void StopAndRelease()
    {
        SetIntensity(0f);
        transform.SetParent(null, true);
        ReleaseAfterPlayback();
    }

    private void OnDisable() => Clear();
    private void OnDestroy()
    {
        if (_oneShot)
        {
            ActiveBursts.Remove(this);
        }
    }
}
