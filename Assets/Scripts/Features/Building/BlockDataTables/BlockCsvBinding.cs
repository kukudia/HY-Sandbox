// 职责：在其他组件 Awake 前应用 CSV 配置；OnValidate 仅请求 Editor 延迟校验。
// 入口：Awake() / OnValidate()；相同 CSV 共用已校验快照，错误保留 Prefab 原参数。
using System;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class BlockCsvBinding : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string _prefabGuid;
    private static BlockCsvDocument _document;
    private static bool _loaded;
    private static bool _applying;
    public string PrefabGuid => _prefabGuid;

    public static Action<BlockCsvBinding> ValidationRequested;
    public void SetPrefabGuid(string guid)
    {
        _prefabGuid = guid;
    }

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        RequestValidation();
    }

    public void RequestValidation()
    {
        if (!_applying)
        {
            ValidationRequested?.Invoke(this);
        }
    }

    public bool Apply()
    {
        if (_applying)
        {
            return false;
        }

        _applying = true;
        try
        {
            if (!_loaded)
            {
                _loaded = true;
                TextAsset csv = Resources.Load<TextAsset>(BlockCsvDocument.ResourcePath);
                if (csv != null)
                {
                    _document = BlockCsvDocument.Parse(csv.text);
                }
            }

            return _document != null && _document.Apply(gameObject, _prefabGuid);
        }
        catch (Exception exception)
        {
            Debug.LogError("方块 CSV 应用失败：" + exception.Message, this);
            return false;
        }
        finally
        {
            _applying = false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCache()
    {
        _document = null;
        _loaded = false;
        _applying = false;
    }
}
