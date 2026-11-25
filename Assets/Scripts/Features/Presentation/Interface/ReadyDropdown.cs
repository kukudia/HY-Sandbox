// 职责：在下拉菜单接收输入前初始化 uGUI 的淡入淡出状态。
// 入口：OnEnable()；保留 Dropdown 的模板、选项和输入行为。
using UnityEngine;
using UnityEngine.UI;

public sealed class ReadyDropdown : Dropdown
{
    [System.NonSerialized]
    private bool initialized;
    protected override void OnEnable()
    {
        base.OnEnable();
        if (Application.isPlaying)
        {
            Initialize();
        }
    }

    protected override void Start()
    {
        if (Application.isPlaying)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        // uGUI 2.6.0 creates its tween runner in Start. A click can arrive as soon
        // as the control is enabled, and script reload can recreate its private state.
        base.Start();
        initialized = true;
    }
}
