// 职责：提供回收物品、无人机、残骸和掉落的可编辑配置。
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/Salvage settings")]
public class SalvageSettings : ScriptableObject
{
    [Min(1)]
    public int coinsPerBlock = 2;
    [Range(0f, 1f)]
    public float specialPartChance = 0.08f;
    [Range(0f, 1f), Tooltip("Chance of a coin packet when no special part survives. Cargo contents still spill exactly.")]
    public float coinDropChance = 0.3f;
    [Tooltip("Only these module resources may survive as special parts. Empty means no random special drops.")]
    public string[] specialPartResources;
    [Min(1f)]
    public float cleanupDelay = 10f;
    [Tooltip("Resource packets merge beyond this count. Special parts are never removed by this limit.")]
    [Min(8)]
    public int maxCurrencyPackets = 64;
}
