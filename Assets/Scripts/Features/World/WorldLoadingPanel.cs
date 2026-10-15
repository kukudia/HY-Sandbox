// 职责：呈现世界准备进度及取消入口；实际准备与模式切换由WorldSessionManager持有。
// 入口：Bind() / Show()；组件留在活动Canvas，隐藏仅改变CanvasGroup。
using UnityEngine;
using UnityEngine.UI;

public sealed class WorldLoadingPanel : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup group;
    [SerializeField]
    private Text message;
    [SerializeField]
    private Image progress;
    [SerializeField]
    private Button cancelButton;
    private WorldSessionManager session;
    private bool shown;
    public void Bind(WorldSessionManager owner)
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Cancel);
            cancelButton.onClick.AddListener(Cancel);
        }

        session = owner;
        Image backdrop = GetComponent<Image>();
        if (backdrop != null)
        {
            Color color = backdrop.color;
            color.a = 1f;
            backdrop.color = color;
        }

        Show(false);
    }

    public void Show(bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        if (cancelButton != null)
        {
            cancelButton.interactable = session != null && session.State != WorldSessionState.Playing;
        }

        if (visible && !shown)
        {
            transform.SetAsLastSibling();
        }

        shown = visible;
    }

    private void Update()
    {
        if (session == null || group == null || group.alpha <= 0f)
        {
            return;
        }

        if (message != null)
        {
            message.text = session.Status;
        }

        if (progress != null)
        {
            progress.fillAmount = session.Progress;
        }
    }

    private void Cancel()
    {
        session?.CancelPlayRequest();
    }

    private void OnDestroy()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Cancel);
        }
    }
}
