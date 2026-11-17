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
    [SerializeField, Tooltip("工具抽屉展开时战报右边距；保持顶部对齐，避免压住中央击杀提示。")]
    private Vector2 toolsFeedPosition = new Vector2(-420f, -65f);
    [SerializeField, Tooltip("工具展开时中央击杀提示水平偏移，给右侧抽屉留出净空。")]
    private float toolsNoticeOffset = -120f;
    private readonly Queue<Message> pending = new Queue<Message>();
    private float noticeAt = -100f;
    private int combined;
    private Vector2 noticeOrigin;
    private bool toolsOpen;
    private Vector2 NoticeAnchor => noticeOrigin + Vector2.right * (toolsOpen ? toolsNoticeOffset : 0f);
    public int TotalMessages
    {
        get; private set;
    }
    public int PendingCount => pending.Count;

    public void SetToolsOpen(bool open, bool immediate = false)
    {
        toolsOpen = open;
        Vector2 target = open ? toolsFeedPosition : new Vector2(-20f, -65f);
        feedRoot.anchoredPosition = immediate ? target : Vector2.Lerp(feedRoot.anchoredPosition, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        if (immediate)
        {
            ((RectTransform)notice.transform).anchoredPosition = NoticeAnchor;
        }
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
        rect.anchoredPosition = NoticeAnchor + Vector2.up * ((1f - exit) * 18f);
        rect.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, 1f - Mathf.Pow(1f - enter, 3f));
    }
}
