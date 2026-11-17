// 职责：维护一条可复用战报的展示数据和补位动画。
// 入口：Set() / CopyFrom() / Present()；实例由制作工具预存，不在击杀时创建。
using TMPro;
using UnityEngine;

public sealed class CombatFeedItem : MonoBehaviour
{
    [SerializeField]
    private TMP_Text title;
    [SerializeField]
    private TMP_Text detail;
    [SerializeField]
    private CanvasGroup group;
    private float createdAt = -100f;
    private string message;
    private string extra;
    private bool positioned;
    public void Set(string text, string subtitle, float time)
    {
        message = text;
        extra = subtitle;
        title.text = text;
        detail.text = subtitle;
        createdAt = time;
        positioned = false;
    }

    public void CopyFrom(CombatFeedItem source)
    {
        message = source.message;
        extra = source.extra;
        title.text = message;
        detail.text = extra;
        createdAt = source.createdAt;
        ((RectTransform)transform).anchoredPosition = ((RectTransform)source.transform).anchoredPosition;
        positioned = true;
    }

    public void Clear()
    {
        createdAt = -100f;
        group.alpha = 0f;
    }

    public void Present(float now, CombatHudTheme theme, int index)
    {
        float age = now - createdAt;
        group.alpha = Mathf.Min(
            Mathf.Clamp01(age / theme.enterDuration),
            Mathf.Clamp01((theme.feedDuration - age) / theme.exitDuration));
        Vector2 target = new Vector2(0f, -index * 62f);
        RectTransform rect = (RectTransform)transform;
        if (!positioned)
        {
            rect.anchoredPosition = target + Vector2.right * 36f;
            positioned = true;
        }

        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
    }
}
