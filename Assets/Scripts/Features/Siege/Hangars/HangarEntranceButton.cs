// 职责：把已制作的机库方向按钮接入蓝图编辑命令，显示当前前门或顶门。
// 入口：OnEnable() / OnDisable() 对称绑定；Toggle() 供按钮调用。
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class HangarEntranceButton : MonoBehaviour
{
    [SerializeField]
    private HangarModule hangar;
    [SerializeField]
    private Text label;
    private Button button;
    private void OnEnable()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Toggle);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Toggle);
        }
    }

    private void LateUpdate()
    {
        if (label != null && hangar != null)
        {
            label.text = hangar.Entrance == HangarEntrance.Roof ? "DOOR: ROOF" : "DOOR: FRONT";
        }
    }

    public void Toggle()
    {
        if (hangar != null && !hangar.TrySetEntrance(
            hangar.Entrance == HangarEntrance.Front ? HangarEntrance.Roof : HangarEntrance.Front,
            out string reason))
        {
            Debug.Log(reason, hangar);
        }
    }
}
