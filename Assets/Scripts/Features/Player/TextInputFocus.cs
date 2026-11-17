// 职责：统一识别EventSystem当前文本编辑选择，阻止场景快捷键窃取文字输入。
// 入口：IsEditingText；支持uGUI和TMP，忽略已隐藏或禁用的旧选择。
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class TextInputFocus
{
    public static bool IsEditingText
    {
        get
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null || !selected.activeInHierarchy)
            {
                return false;
            }

            InputField legacy = selected.GetComponentInParent<InputField>();
            TMPro.TMP_InputField tmp = selected.GetComponentInParent<TMPro.TMP_InputField>();
            return (legacy != null && legacy.isActiveAndEnabled) || (tmp != null && tmp.isActiveAndEnabled);
        }
    }
}
