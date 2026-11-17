// 职责：集中保存共享战斗界面的配色、采样预算与动效时间，所有消费者只读。
// 入口：由 CombatHud / CombatNameplate / CombatNotifications 的 Inspector 引用。
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/Combat HUD Theme")]
public sealed class CombatHudTheme : ScriptableObject
{
    public Color background = new Color(.025f, .045f, .065f, .94f);
    public Color friendly = new Color(.25f, .85f, 1f);
    public Color hostile = new Color(1f, .38f, .24f);
    public Color warning = new Color(1f, .76f, .25f);
    [Min(.05f)]
    public float sampleInterval = .1f;
    [Min(.05f)]
    public float occlusionInterval = .15f;
    [Min(1)]
    public int maximumNameplates = 24;
    [Min(1)]
    public int maximumMarkers = 12;
    [Min(1f)]
    public float nameplateDistance = 220f;
    [Min(.01f)]
    public float enterDuration = .18f;
    [Min(.01f)]
    public float exitDuration = .3f;
    [Min(.1f)]
    public float noticeDuration = 2.8f;
    [Min(.1f)]
    public float feedDuration = 5f;
    [Min(0f)]
    public float damageDelay = .15f;
    [Min(.01f)]
    public float damageDuration = .35f;
}
