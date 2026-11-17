// 职责：用有界战报池和有界提示队列展示战斗事实，处理补位、入场和退场动画。
// 入口：Push() / Clear()；不结算奖励，禁用时释放待播消息。
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CombatNotifications : MonoBehaviour
{
    [SerializeField]
    private CombatHudTheme theme;
    [SerializeField]
    private CombatFeedItem[] feed;
    [SerializeField]
    private CanvasGroup notice;
    [SerializeField]
    private TMP_Text noticeName;
    [SerializeField]
    private TMP_Text noticeDetail;
    [SerializeField]
    private RectTransform feedRoot;
    private readonly Queue<Message> pending = new Queue<Message>();
    private float noticeAt = -100f;
    private int combined;
    private Vector2 noticeOrigin;
    public int TotalMessages
    {
        get; private set;
    }
    public int PendingCount => pending.Count;

    public void SetToolsOpen(bool open)
    {
        Vector2 target = open ? new Vector2(-420f, -155f) : new Vector2(-20f, -65f);
        feedRoot.anchoredPosition = Vector2.Lerp(feedRoot.anchoredPosition, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
    }

    private struct Message
    {
        public string title;
        public string detail;
    }

    private void Awake()
    {
        noticeOrigin = ((RectTransform)notice.transform).anchoredPosition;
        Clear();
    }

    public void Push(string victim, string attacker, string detail, bool celebrate)
    {
        TotalMessages++;
        string title = string.IsNullOrEmpty(attacker) ? victim + "  /  DESTROYED" : attacker + "  >  " + victim;
        for (int i = feed.Length - 1; i > 0; i--)
        {
            feed[i].CopyFrom(feed[i - 1]);
        }

        if (feed.Length > 0)
        {
            feed[0].Set(title, detail, Time.unscaledTime);
        }

        if (!celebrate)
        {
            return;
        }

        if (pending.Count < 4)
        {
            pending.Enqueue(new Message { title = victim, detail = detail });
        }
        else
        {
            // Keep the feed exact; bounded celebration summaries prevent a burst from delaying feedback indefinitely.
            combined++;
        }
    }

    public void Clear()
    {
        pending.Clear();
        combined = 0;
        TotalMessages = 0;
        noticeAt = -100f;
        if (notice != null)
        {
            notice.alpha = 0f;
        }

        foreach (CombatFeedItem item in feed)
        {
            item.Clear();
        }
    }

    private void OnDisable()
    {
        Clear();
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        for (int i = 0; i < feed.Length; i++)
        {
            feed[i].Present(now, theme, i);
        }

        float duration = pending.Count > 0 || combined > 0 ? Mathf.Min(1.15f, theme.noticeDuration) : theme.noticeDuration;
        if (now - noticeAt >= duration && (pending.Count > 0 || combined > 0))
        {
            Message message = pending.Count > 0 ? pending.Dequeue() : new Message
            {
                title = combined + " ADDITIONAL TARGETS",
                detail = "DESTROYED"
            };
            if (pending.Count == 0 && message.title.EndsWith("ADDITIONAL TARGETS"))
            {
                combined = 0;
            }

            noticeName.text = message.title;
            noticeDetail.text = message.detail;
            noticeAt = now;
        }

        float age = now - noticeAt;
        float enter = Mathf.Clamp01(age / theme.enterDuration);
        float exit = Mathf.Clamp01((duration - age) / theme.exitDuration);
        notice.alpha = Mathf.Min(enter, exit);
        RectTransform rect = (RectTransform)notice.transform;
        rect.anchoredPosition = noticeOrigin + Vector2.up * ((1f - exit) * 18f);
        rect.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, 1f - Mathf.Pow(1f - enter, 3f));
    }
}
