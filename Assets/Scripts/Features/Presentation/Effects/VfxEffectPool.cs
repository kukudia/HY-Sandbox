// 职责：按场景和模板复用 GPU 特效；活动实例按需扩容，仅限制闲置缓存。
// 入口：Rent() / Return()；场景卸载销毁池及其全部实例。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class VfxEffectPool : MonoBehaviour
{
    private static readonly Dictionary<Scene, VfxEffectPool> Pools = new Dictionary<Scene, VfxEffectPool>();
    private readonly Dictionary<VfxEffect, Stack<VfxEffect>> _idle = new Dictionary<VfxEffect, Stack<VfxEffect>>();
    private readonly HashSet<VfxEffect> _active = new HashSet<VfxEffect>();
    private Transform _inactiveRoot;
    private bool _disposing;
    public int ActiveCount => _active.Count;
    public int IdleCount
    {
        get; private set;
    }
    public int CreatedCount
    {
        get; private set;
    }

    public static VfxEffect Rent(VfxEffect prefab, Vector3 position, Quaternion rotation, Scene scene)
    {
        if (prefab == null)
        {
            return null;
        }

        if (!Pools.TryGetValue(scene, out VfxEffectPool pool) || pool == null)
        {
            var root = new GameObject("Scene GPU effect pool");
            SceneManager.MoveGameObjectToScene(root, scene);
            pool = root.AddComponent<VfxEffectPool>();
            var inactive = new GameObject("Inactive effects");
            inactive.transform.SetParent(root.transform, false);
            inactive.SetActive(false);
            pool._inactiveRoot = inactive.transform;
            Pools[scene] = pool;
        }

        if (!pool._idle.TryGetValue(prefab, out Stack<VfxEffect> idle))
        {
            idle = new Stack<VfxEffect>();
            pool._idle.Add(prefab, idle);
        }

        VfxEffect effect = null;
        while (idle.Count > 0 && effect == null)
        {
            effect = idle.Pop();
            pool.IdleCount--;
        }

        if (effect == null)
        {
            // 在非活动节点下实例化，避免模板 OnEnable 早于参数复位。
            effect = Instantiate(prefab, pool._inactiveRoot);
            effect.gameObject.SetActive(false);
            effect.ConfigurePool(pool, prefab);
            pool.CreatedCount++;
        }

        effect.transform.SetParent(pool.transform, false);
        effect.ResetForRent(position, rotation);
        pool._active.Add(effect);
        effect.gameObject.SetActive(true);
        return effect;
    }

    internal void Return(VfxEffect effect, VfxEffect prefab)
    {
        if (_disposing || !_active.Remove(effect))
        {
            return;
        }

        effect.gameObject.SetActive(false);
        effect.transform.SetParent(_inactiveRoot, false);
        BlockVfxLibrary library = BlockVfxLibrary.Instance;
        int perTemplate = library != null ? library.MaxIdlePerTemplate : 32;
        int total = library != null ? library.MaxIdleEffects : 128;
        if (prefab != null && _idle.TryGetValue(prefab, out Stack<VfxEffect> idle) && idle.Count < perTemplate && IdleCount < total)
        {
            idle.Push(effect);
            IdleCount++;
        }
        else
        {
            Destroy(effect.gameObject);
        }
    }

    internal void Forget(VfxEffect effect) => _active.Remove(effect);
    private void OnDestroy()
    {
        _disposing = true;
        if (Pools.TryGetValue(gameObject.scene, out VfxEffectPool pool) && pool == this)
        {
            Pools.Remove(gameObject.scene);
        }

        _active.Clear();
        _idle.Clear();
        IdleCount = 0;
    }
}
