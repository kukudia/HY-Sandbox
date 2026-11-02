// 职责：显示短时波次、击毁和核心受击反馈；不参与阶段、经济或输入决策。
// 入口：Update()；关闭时不改变战场状态。
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeBattleFeedback : MonoBehaviour
{
    [SerializeField]
    private SiegeSession session;
    [SerializeField]
    private Text banner;
    [SerializeField]
    private Text notice;
    [SerializeField]
    private CanvasGroup damage;
    private SiegePhase phase = SiegePhase.Defeat;
    private int kills;
    private float health = -1f;
    private float bannerUntil;
    private float noticeUntil;
    private float damageUntil;
    private void Update()
    {
        SiegeMatch match = session.Match;
        if (match == null || session.IsEditingBlueprint)
        {
            return;
        }

        if (phase != match.Phase)
        {
            phase = match.Phase;
            banner.text = phase == SiegePhase.Battle ? (match.IsEliteWave ? "ELITE " : match.IsSwarmWave ? "SWARM " : "") + "WAVE " + match.Wave : phase == SiegePhase.Intermission ? "WAVE CLEARED  /  " + session.LastWaveKills + " KILLS" : phase == SiegePhase.Defeat ? "BEST WAVE " + Mathf.Max(session.BestWave, match.Wave) : "CLICK A DEPLOYMENT PAD";
            bannerUntil = Time.unscaledTime + 3f;
        }

        if (match.DefeatedAttackers > kills)
        {
            kills = match.DefeatedAttackers;
            notice.text = "RAIDER DESTROYED  +" + match.KillReward + (session.Combo >= 2 ? "   COMBO x" + session.Combo : "");
            noticeUntil = Time.unscaledTime + 2f;
        }

        Durability core = session.Fleet?.Base?.Core != null ? session.Fleet.Base.Core.GetComponent<Durability>() : null;
        if (core != null)
        {
            if (health >= 0f && core.currentDurability < health)
            {
                damageUntil = Time.unscaledTime + 0.5f;
                VisualEffectsManager.instance?.PlayCameraShake(0.4f, 0.22f);
            }

            health = core.currentDurability;
            float low = core.maxDurability > 0f && health / core.maxDurability < 0.3f ? 0.12f + Mathf.Sin(Time.unscaledTime * 3f) * 0.05f : 0f;
            damage.alpha = Mathf.Max(low, Mathf.Clamp01((damageUntil - Time.unscaledTime) * 2f) * 0.4f);
        }

        banner.enabled = Time.unscaledTime < bannerUntil;
        notice.enabled = Time.unscaledTime < noticeUntil;
    }
}
