// 职责：显示自动对战轮次、十槽位阵容和击杀列表，复用探索模式击杀提示表现。
// 入口：SetState() / ShowKill() / ResetRound()；UI引用由场景制作保存。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkirmishHud : MonoBehaviour
{
    [SerializeField]
    private CombatHud combatHud;
    [SerializeField]
    private Text status;
    [SerializeField]
    private Text roster;
    [SerializeField]
    private Text feed;
    private readonly Queue<string> messages = new Queue<string>();
    public int KillMessages
    {
        get; private set;
    }

    public void ResetRound()
    {
        messages.Clear();
        KillMessages = 0;
        feed.text = string.Empty;
        combatHud.ResetHud();
    }

    public void SetState(string title, string lineup)
    {
        status.text = title;
        roster.text = lineup;
    }

    public void ShowKill(string victim, string attacker, int blueAlive, int redAlive)
    {
        KillMessages++;
        messages.Enqueue(string.IsNullOrEmpty(attacker) ? victim + " — DESTROYED" : attacker + " → " + victim);
        while (messages.Count > 6)
        {
            messages.Dequeue();
        }

        feed.text = string.Join("\n", messages);
        combatHud.ShowCombatKill(victim, attacker, $"BLUE {blueAlive}  :  {redAlive} RED");
    }
}
