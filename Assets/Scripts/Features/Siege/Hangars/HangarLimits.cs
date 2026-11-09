// 职责：保存基地机库数量上限；尺寸使用基地自身边界，不与数量限制混用。
// 入口：MaximumCount；Resources/Hangars/Limits 是共享编辑与部署配置。
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/Hangar Limits")]
public sealed class HangarLimits : ScriptableObject
{
    [SerializeField, Range(1, 6)]
    private int maximumCount = 6;
    [SerializeField, Min(1), Tooltip("机库外壳每平方米的设计预算；内部空体积不收费。")]
    private int shellCoinsPerSquareMetre = 1;
    public int ShellCoinsPerSquareMetre => Mathf.Max(1, shellCoinsPerSquareMetre);
    public int MaximumCount => Mathf.Clamp(maximumCount, 1, 6);
}
