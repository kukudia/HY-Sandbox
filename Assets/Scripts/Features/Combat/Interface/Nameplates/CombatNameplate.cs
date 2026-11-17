// 职责：显示整舰/驾驶舱耐久与延迟损伤条；只接收数据，不扫描场景或修改耐久。
// 入口：Bind() / Sample() / Present()；对象回收后 Bind 重置全部动画状态。
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CombatNameplate : MonoBehaviour
{
    [SerializeField]
    private TMP_Text nameLabel;
    [SerializeField]
    private TMP_Text valueLabel;
    [SerializeField]
    private TMP_Text cockpitLabel;
    [SerializeField]
    private RectTransform healthFill;
    [SerializeField]
    private RectTransform damageFill;
    [SerializeField]
    private RectTransform cockpitFill;
    [SerializeField]
    private Image accent;
    [SerializeField]
    private CanvasGroup group;
    private CombatHudTheme theme;
    private Color team;
    private float healthRatio;
    private float damageRatio;
    private float cockpitRatio;
    private float damageAt;
    private bool sampled;
    public RectTransform Rect => (RectTransform)transform;
    public bool IsVisible => gameObject.activeSelf && group.alpha > .2f;

    public void Bind(CombatHudTheme style, string title, Color color)
    {
        theme = style;
        team = color;
        nameLabel.text = title;
        nameLabel.color = color;
        healthFill.GetComponent<Image>().color = color;
        group.alpha = 0f;
        sampled = false;
        gameObject.SetActive(true);
    }

    public void Sample(float health, float maximum, float cockpit, float cockpitMaximum, bool detailed)
    {
        float ratio = maximum > 0f ? Mathf.Clamp01(health / maximum) : 0f;
        if (sampled && ratio < healthRatio - .001f)
        {
            damageAt = Time.unscaledTime;
        }

        if (!sampled || ratio > damageRatio)
        {
            damageRatio = ratio;
        }

        healthRatio = ratio;
        cockpitRatio = cockpitMaximum > 0f ? Mathf.Clamp01(cockpit / cockpitMaximum) : 0f;
        sampled = true;
        valueLabel.text = detailed ? $"{health:0} / {maximum:0}" : string.Empty;
        cockpitLabel.text = detailed ? $"CORE  {cockpit:0} / {cockpitMaximum:0}" : "CORE";
    }

    public void Present(Vector2 position, bool visible, float delta)
    {
        Rect.anchoredPosition = position;
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, delta * 8f);
        if (Time.unscaledTime - damageAt > theme.damageDelay)
        {
            damageRatio = Mathf.MoveTowards(damageRatio, healthRatio, delta / theme.damageDuration);
        }

        healthFill.anchorMax = new Vector2(healthRatio, 1f);
        damageFill.anchorMax = new Vector2(damageRatio, 1f);
        cockpitFill.anchorMax = new Vector2(cockpitRatio, 1f);
        float flash = Mathf.Clamp01(1f - (Time.unscaledTime - damageAt) / .25f);
        Color low = healthRatio < .25f ? Color.Lerp(team, theme.warning, .35f + .15f * Mathf.Sin(Time.unscaledTime * 4f)) : team;
        accent.color = Color.Lerp(low, Color.white, sampled ? flash : 0f);
    }
}
