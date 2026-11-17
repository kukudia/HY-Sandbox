// 职责：显示单个威胁的箭头、定位框、距离和聚合数量；标签保持水平。
// 入口：BeginFrame() / Place() / EndFrame()；实例由Editor保存。
using TMPro;
using UnityEngine;

public sealed class CombatThreatView : MonoBehaviour
{
    [SerializeField]
    private RectTransform arrow;
    [SerializeField]
    private GameObject bracket;
    [SerializeField]
    private TMP_Text label;
    [SerializeField]
    private CanvasGroup group;
    private float appearedAt;
    private float nextText;
    private float distance;
    private bool behind;
    private bool elite;
    private int cluster;
    private int lastCluster;
    public Cockpit Owner
    {
        get; private set;
    }
    public bool Used
    {
        get; private set;
    }
    public bool Offscreen
    {
        get; private set;
    }
    public Vector2 Position => ((RectTransform)transform).anchoredPosition;

    public void Clear()
    {
        Owner = null;
        Used = false;
        group.alpha = 0f;
    }

    public void BeginFrame()
    {
        Used = false;
        cluster = 1;
    }

    public void Place(
        Cockpit owner,
        Vector2 position,
        bool offscreen,
        bool rear,
        float meters,
        bool eliteWave,
        CombatHudTheme theme)
    {
        bool changed = Owner != owner;
        if (changed)
        {
            appearedAt = Time.unscaledTime;
            nextText = 0f;
        }

        Owner = owner;
        Used = true;
        Offscreen = offscreen;
        behind = rear;
        elite = eliteWave;
        distance = meters;
        ((RectTransform)transform).anchoredPosition = position;
        arrow.gameObject.SetActive(offscreen);
        bracket.SetActive(!offscreen);
        arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg - 90f);
        group.alpha = Mathf.Clamp01((Time.unscaledTime - appearedAt) / theme.enterDuration);
        if (elite)
        {
            group.alpha *= .9f + .1f * Mathf.Sin(Time.unscaledTime * 4f);
        }
    }

    public void AddCluster()
    {
        cluster++;
    }

    public void EndFrame()
    {
        if (!Used)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime * 8f);
            return;
        }

        if (Time.unscaledTime >= nextText || cluster != lastCluster)
        {
            label.text = (elite ? "ELITE " : behind ? "REAR " : "") + Mathf.RoundToInt(distance) + "m" + (cluster > 1 ? "  x" + cluster : "");
            nextText = Time.unscaledTime + .15f;
            lastCluster = cluster;
        }
    }
}
