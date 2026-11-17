// 职责：按场景和模板复用GPU特效；默认按需扩容，演出场可显式配置活动预算。
// 入口：Rent() / Return()；场景卸载销毁池及其全部实例。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class VfxEffectPool : MonoBehaviour
{
    private static readonly Dictionary<Scene, VfxEffectPool> Pools = new Dictionary<Scene, VfxEffectPool>();
    private readonly Dictionary<VfxEffect, Stack<VfxEffect>> _idle = new Dictionary<VfxEffect, Stack<VfxEffect>>();
    private readonly HashSet<VfxEffect> _active = new HashSet<VfxEffect>();
    private readonly Dictionary<VfxEffect, int> activeByTemplate = new Dictionary<VfxEffect, int>();
    [SerializeField]
    private Transform _inactiveRoot;
    [SerializeField, Min(0), Tooltip("0保持不限活动实例；超过预算只省略视觉，不影响伤害。")]
    private int maximumActiveEffects;
    [SerializeField, Min(0), Tooltip("0保持不限单模板；可防止密集炮口占满所有爆炸/烟雾预算。")]
    private int maximumActivePerTemplate;
    private bool _disposing;
    public int DroppedCount
    {
        get; private set;
    }
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
            pool = SceneObjectQuery.FindInScene<VfxEffectPool>(scene);
            if (pool == null)
            {
                throw new MissingReferenceException($"Scene {scene.name} requires an authored VfxEffectPool.");
            }

            Pools[scene] = pool;
        }

        pool.activeByTemplate.TryGetValue(prefab, out int activeCount);
        if ((pool.maximumActiveEffects > 0 && pool._active.Count >= pool.maximumActiveEffects) || (pool.maximumActivePerTemplate > 0 && activeCount >= pool.maximumActivePerTemplate))
        {
            pool.DroppedCount++;
            return null;
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
        pool.activeByTemplate[prefab] = activeCount + 1;
        effect.gameObject.SetActive(true);
        return effect;
    }

    internal void Return(VfxEffect effect, VfxEffect prefab)
    {
        if (_disposing || !_active.Remove(effect))
        {
            return;
        }

        if (prefab != null && activeByTemplate.TryGetValue(prefab, out int count))
        {
            activeByTemplate[prefab] = Mathf.Max(0, count - 1);
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

    internal void Forget(VfxEffect effect, VfxEffect prefab)
    {
        if (_active.Remove(effect) && prefab != null && activeByTemplate.TryGetValue(prefab, out int count))
        {
            activeByTemplate[prefab] = Mathf.Max(0, count - 1);
        }
    }

    public void ClearActive()
    {
        var effects = new List<VfxEffect>(_active);
        foreach (VfxEffect effect in effects)
        {
            if (effect != null)
            {
                effect.Clear();
                effect.gameObject.SetActive(false);
                Destroy(effect.gameObject);
            }
        }

        _active.Clear();
        activeByTemplate.Clear();
    }

    private void OnDestroy()
    {
        _disposing = true;
        if (Pools.TryGetValue(gameObject.scene, out VfxEffectPool pool) && pool == this)
        {
            Pools.Remove(gameObject.scene);
        }

        _active.Clear();
        activeByTemplate.Clear();
        _idle.Clear();
        IdleCount = 0;
    }
}
