// 职责：展示模式选择、场景加载和失败重试；按钮仅调用Main的模式协调器。
// 入口：Bind() / Show()；布局与引用通过Editor制作并保存在Main场景。
using UnityEngine;
using UnityEngine.UI;

public sealed class GameModePanel : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup group;
    [SerializeField]
    private Text status;
    [SerializeField]
    private Button exploration;
    [SerializeField]
    private Button siege;
    [SerializeField]
    private Button retry;
    [SerializeField]
    private Button cancel;
    private GameModeController owner;
    public bool IsVisible => group != null && group.alpha > 0f;

    public void Bind(GameModeController controller)
    {
        owner = controller;
        exploration.onClick.AddListener(owner.SelectExploration);
        siege.onClick.AddListener(owner.SelectSiege);
        retry.onClick.AddListener(owner.Retry);
        cancel.onClick.AddListener(owner.ReturnToBuild);
    }

    public void Show(bool visible)
    {
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        if (visible)
        {
            transform.SetAsLastSibling();
            Refresh();
        }
    }

    private void Update()
    {
        if (IsVisible && owner != null)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        status.text = owner.Status;
        bool choosing = owner.State == GameModeState.Choosing;
        exploration.gameObject.SetActive(choosing);
        siege.gameObject.SetActive(choosing);
        retry.gameObject.SetActive(owner.State == GameModeState.Failed);
        cancel.interactable = owner.State != GameModeState.Returning;
    }

    private void OnDestroy()
    {
        if (owner == null)
        {
            return;
        }

        exploration.onClick.RemoveListener(owner.SelectExploration);
        siege.onClick.RemoveListener(owner.SelectSiege);
        retry.onClick.RemoveListener(owner.Retry);
        cancel.onClick.RemoveListener(owner.ReturnToBuild);
    }
}
