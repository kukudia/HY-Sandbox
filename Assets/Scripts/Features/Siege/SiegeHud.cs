// 职责：按阶段显示精简的指挥栏、整备操作与结算；布局由场景制作工具保存。
// 入口：Refresh()；部署交互交给SiegeDeploymentView，战斗反馈交给SiegeBattleFeedback。
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SiegeHud : MonoBehaviour
{
    [SerializeField]
    private SiegeSession session;
    [SerializeField]
    private Text heading;
    [SerializeField]
    private Text economy;
    [SerializeField]
    private Text status;
    [SerializeField]
    private Text coreHealth;
    [SerializeField]
    private GameObject preparation;
    [SerializeField]
    private GameObject defeat;
    [SerializeField]
    private GameObject help;
    [SerializeField]
    private Button wave;
    [SerializeField]
    private Button repairCore;
    [SerializeField]
    private Button restart;
    private float nextRefresh;
    private long baseQuote;
    private void OnEnable()
    {
        wave.onClick.AddListener(StartWave);
        repairCore.onClick.AddListener(RepairCore);
        restart.onClick.AddListener(Restart);
        session.RefitChanged += RefreshQuote;
    }

    private void OnDisable()
    {
        wave.onClick.RemoveListener(StartWave);
        repairCore.onClick.RemoveListener(RepairCore);
        restart.onClick.RemoveListener(Restart);
        session.RefitChanged -= RefreshQuote;
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current?.f2Key.wasPressedThisFrame == true)
        {
            help.SetActive(!help.activeSelf);
        }

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.25f;
            Refresh();
        }
    }

    public void Refresh()
    {
        SiegeMatch match = session.Match;
        if (match == null)
        {
            return;
        }

        bool refit = match.CanRefit && !session.IsEditingBlueprint;
        if (preparation.activeSelf != refit)
        {
            preparation.SetActive(refit);
            if (refit)
            {
                RefreshQuote();
            }
        }

        defeat.SetActive(match.Phase == SiegePhase.Defeat);
        heading.text = match.Phase == SiegePhase.Defeat ? "BASTION LOST" : "BASTION  /  WAVE " + match.Wave;
        economy.text = match.CanRefit ? "CREDITS " + match.Coins.ToString("N0") + "    NEXT " + match.NextWaveSize + " RAIDERS" : "RAIDERS " + (match.ActiveAttackers + match.PendingAttackers);
        status.text = session.Status;
        Durability core = session.Fleet?.Base?.Core != null ? session.Fleet.Base.Core.GetComponent<Durability>() : null;
        string health = core != null ? Mathf.CeilToInt(core.currentDurability) + " / " + Mathf.CeilToInt(core.maxDurability) : "LOST";
        SiegeFleetEntry driven = session.Fleet?.Driven;
        coreHealth.text = "CORE " + health;
        if (driven?.Unit != null && match.Phase == SiegePhase.Battle)
        {
            driven.Unit.TryGetTotalDurability(out float current, out float maximum);
            coreHealth.text += "    " + driven.DisplayName + " " + Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(maximum);
        }

        wave.interactable = refit && session.AttackerOptions.Length > 0;
        repairCore.interactable = refit && baseQuote > 0 && match.CanAfford(baseQuote);
        repairCore.GetComponentInChildren<Text>().text = baseQuote > 0 ? "REPAIR BASE  " + baseQuote.ToString("N0") : "BASE INTACT";
    }

    private void RefreshQuote()
    {
        baseQuote = session.Match != null && session.Match.CanRefit && session.Fleet?.Base?.IsAlive == true ? session.Fleet.Base.Quote() : 0;
    }

    private void StartWave()
    {
        session.BeginNextWave();
    }

    private void RepairCore()
    {
        session.TryRepair(true);
    }

    private void Restart()
    {
        SceneManager.LoadScene(gameObject.scene.path);
    }
}
