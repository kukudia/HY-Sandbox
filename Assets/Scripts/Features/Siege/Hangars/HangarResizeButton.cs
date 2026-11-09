// 职责：将已制作的世界空间按钮转成机库尺寸命令，不动态创建 UI。
// 入口：OnEnable() 绑定按钮；OnDisable() 对称解除。
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class HangarResizeButton : MonoBehaviour
{
    [SerializeField]
    private HangarModule hangar;
    [SerializeField, Range(0, 2)]
    private int axis;
    [SerializeField]
    private int delta = 1;
    private Button button;
    private void OnEnable()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Resize);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Resize);
        }
    }

    public void Resize()
    {
        if (hangar != null && !hangar.TryResize(axis, delta, out string reason))
        {
            Debug.Log(reason, hangar);
        }
    }
}
