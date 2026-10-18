// 职责：缓存加载过的方块预制体，避免 Resources.Load() 重复加载相同资源。
// 入口：GetPrefab()；支持清空缓存。
using System.Collections.Generic;
using UnityEngine;

internal static class BlockPrefabCache
{
    private static readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();
    private const int MaxCacheSize = 256;
    /// <summary>
    /// 获取或加载方块预制体。首次加载时缓存，后续调用直接返回同一预制体引用。
    /// </summary>
    public static GameObject GetPrefab(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (_cache.TryGetValue(resourcePath, out GameObject cached) && cached != null)
        {
            return cached;
        }

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab != null && _cache.Count < MaxCacheSize)
        {
            _cache[resourcePath] = prefab;
        }

        return prefab;
    }

    /// <summary>
    /// 清空所有缓存。通常在场景切换或内存紧张时调用。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// 获取当前缓存的预制体数量。
    /// </summary>
    public static int CacheSize => _cache.Count;
}
