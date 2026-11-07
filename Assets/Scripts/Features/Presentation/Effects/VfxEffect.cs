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
    private VfxEffectPool _pool;
    private VfxEffect _poolPrefab;
    private float _releaseAt;
    private Vector3 _prefabScale;
    private Vector3[] _graphPositions;
    private Quaternion[] _graphRotations;
    private Vector3[] _graphScales;
    private readonly List<VFXExposedProperty> _properties = new List<VFXExposedProperty>();
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

    internal void ConfigurePool(VfxEffectPool pool, VfxEffect prefab)
    {
        _pool = pool;
        _poolPrefab = prefab;
        _prefabScale = transform.localScale;
        _graphPositions = new Vector3[_graphs.Length];
        _graphRotations = new Quaternion[_graphs.Length];
        _graphScales = new Vector3[_graphs.Length];
        for (int i = 0; i < _graphs.Length; i++)
        {
            if (_graphs[i] == null)
            {
                continue;
            }

            _graphPositions[i] = _graphs[i].transform.localPosition;
            _graphRotations[i] = _graphs[i].transform.localRotation;
            _graphScales[i] = _graphs[i].transform.localScale;
        }
    }

    internal void ResetForRent(Vector3 position, Quaternion rotation)
    {
        CancelInvoke(nameof(FinishPlayback));
        _oneShot = false;
        _releaseAt = 0f;
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = _prefabScale;
        MeteorTrailFollower follower = GetComponent<MeteorTrailFollower>();
        if (follower != null)
        {
            follower.enabled = false;
        }

        for (int i = 0; i < _graphs.Length; i++)
        {
            VisualEffect graph = _graphs[i];
            if (graph == null)
            {
                continue;
            }

            graph.transform.SetLocalPositionAndRotation(_graphPositions[i], _graphRotations[i]);
            graph.transform.localScale = _graphScales[i];
            // 归还模板的真实覆盖值，避免前一残骸的尺寸/颜色污染后一次使用。
            VisualEffect source = _poolPrefab._graphs[i];
            _properties.Clear();
            graph.visualEffectAsset.GetExposedProperties(_properties);
            foreach (VFXExposedProperty property in _properties)
            {
                if (property.type == typeof(float))
                {
                    graph.SetFloat(property.name, source.GetFloat(property.name));
                }
                else if (property.type == typeof(Vector3))
                {
                    graph.SetVector3(property.name, source.GetVector3(property.name));
                }
            }
        }

        Clear();
    }

    public void ReleaseAfterPlayback()
    {
        if (_oneShot)
        {
            return;
        }

        _oneShot = true;
        _releaseAt = Time.time + _releaseAfter;
        Invoke(nameof(FinishPlayback), _releaseAfter);
    }

    public void StopAndRelease()
    {
        SetIntensity(0f);
        if (_pool == null)
        {
            transform.SetParent(null, true);
        }

        ReleaseAfterPlayback();
    }

    private void FinishPlayback()
    {
        if (!_oneShot)
        {
            return;
        }

        if (Time.time < _releaseAt)
        {
            // Invoke可能在目标帧的Time.time略早于截止值时触发；必须补约，否则效果永久留在活动池。
            CancelInvoke(nameof(FinishPlayback));
            Invoke(nameof(FinishPlayback), Mathf.Max(0.01f, _releaseAt - Time.time));
            return;
        }

        _oneShot = false;
        CancelInvoke(nameof(FinishPlayback));
        if (_pool != null)
        {
            _pool.Return(this, _poolPrefab);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable() => Clear();
    private void OnDestroy()
    {
        if (_pool != null)
        {
            _pool.Forget(this);
        }
    }
}
