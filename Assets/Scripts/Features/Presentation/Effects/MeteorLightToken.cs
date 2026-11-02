// 职责：统计仍存活的陨石灯，释放时归还灯光预算。
using UnityEngine;

public sealed class MeteorLightToken : MonoBehaviour
{
    public const int Maximum = 24;
    public static int ActiveCount
    {
        get; private set;
    }

    private void Awake()
    {
        ActiveCount++;
    }

    private void OnDestroy()
    {
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
    }
}
