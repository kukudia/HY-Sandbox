// 职责：为建筑与载具碎片提供同一最短物理保留期，避免预算和旧清理计时器吞掉新断片。
// 入口：Mark() 在脱落时调用；Remaining() / CanRetire() 在真正清理前检查；不阻止伤害和场景卸载。
using UnityEngine;

public sealed class FragmentLifetime : MonoBehaviour
{
    private float createdAt;
    internal static float MinimumLifetime => Mathf.Max(0f, WreckSalvage.Settings != null ? WreckSalvage.Settings.minimumFragmentLifetime : 10f);

    private void Awake()
    {
        createdAt = Time.time;
    }

    internal static void Mark(GameObject root)
    {
        if (root.GetComponent<FragmentLifetime>() == null)
        {
            root.AddComponent<FragmentLifetime>();
        }
    }

    internal static float Remaining(GameObject root)
    {
        FragmentLifetime lifetime = root != null ? root.GetComponent<FragmentLifetime>() : null;
        return lifetime != null ? Mathf.Max(0f, MinimumLifetime - (Time.time - lifetime.createdAt)) : 0f;
    }

    internal static bool CanRetire(float age)
    {
        return age >= MinimumLifetime;
    }
}
