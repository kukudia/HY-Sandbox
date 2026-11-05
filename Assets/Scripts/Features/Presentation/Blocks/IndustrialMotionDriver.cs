// 职责：只用一个Unity Update驱动工业零件表现，生命周期随Play会话结束。
// 入口：EnsureExists()由活动零件注册调用，Update()调度IndustrialPartMotion.AnimateAll()。
using UnityEngine;

[AddComponentMenu("")]
internal sealed class IndustrialMotionDriver : MonoBehaviour
{
    private static IndustrialMotionDriver instance;
    internal static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        var owner = new GameObject("Industrial motion driver")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        DontDestroyOnLoad(owner);
        instance = owner.AddComponent<IndustrialMotionDriver>();
    }

    private void Update()
    {
        IndustrialPartMotion.AnimateAll();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
