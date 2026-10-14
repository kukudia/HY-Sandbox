// 职责：按炮弹 Prefab 复用运行实例，限制闲置数量并在模拟结束时清理。
// 入口：Rent() / Return()；炮弹仍独立执行原有物理与命中流程。
using System.Collections.Generic;
using UnityEngine;

public sealed class TurretProjectilePool : MonoBehaviour
{
    private const int MaxIdlePerPrefab = 128;
    private static TurretProjectilePool _instance;
    private readonly Dictionary<TurretProjectile, Stack<TurretProjectile>> _idle = new Dictionary<TurretProjectile, Stack<TurretProjectile>>();
    public static TurretProjectile Rent(TurretProjectile prefab, Vector3 position, Quaternion rotation)
    {
        if (_instance == null)
        {
            _instance = new GameObject("Runtime projectile pool").AddComponent<TurretProjectilePool>();
        }

        if (!_instance._idle.TryGetValue(prefab, out Stack<TurretProjectile> idle))
        {
            idle = new Stack<TurretProjectile>();
            _instance._idle.Add(prefab, idle);
        }

        TurretProjectile shot = null;
        while (idle.Count > 0 && shot == null)
        {
            shot = idle.Pop();
        }

        if (shot == null)
        {
            shot = Instantiate(prefab, position, rotation, _instance.transform);
            shot.SetPool(_instance, prefab);
        }
        else
        {
            shot.transform.SetPositionAndRotation(position, rotation);
            shot.gameObject.SetActive(true);
        }

        return shot;
    }

    internal void Return(TurretProjectile shot, TurretProjectile prefab)
    {
        shot.gameObject.SetActive(false);
        if (prefab != null && _idle.TryGetValue(prefab, out Stack<TurretProjectile> idle) && idle.Count < MaxIdlePerPrefab)
        {
            idle.Push(shot);
        }
        else
        {
            Destroy(shot.gameObject);
        }
    }

    private void Update()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        _idle.Clear();
    }
}
