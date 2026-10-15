// 职责：显示持续的返航保存失败原因与重试提示，不抢焦点或阻挡操作。
// 入口：ShowFailure() / Clear()；MainUIPanels在返航失败/成功时调用。
using UnityEngine;
using UnityEngine.UI;

public sealed class ReturnStatusPanel : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup _group;
    [SerializeField]
    private Text _message;
    public bool IsVisible => _group != null && _group.alpha > .99f;
    public string Message => _message != null ? _message.text : string.Empty;

    private void Awake()
    {
        Clear();
    }

    public void ShowFailure(string reason)
    {
        if (_message != null)
        {
            _message.text = "RETURN FAILED - CARGO NOT SAVED\n" + reason + " Your current session is still open.";
        }

        if (_group != null)
        {
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        transform.SetAsLastSibling();
    }

    public void Clear()
    {
        if (_message != null)
        {
            _message.text = string.Empty;
        }

        if (_group != null)
        {
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }
    }
}
