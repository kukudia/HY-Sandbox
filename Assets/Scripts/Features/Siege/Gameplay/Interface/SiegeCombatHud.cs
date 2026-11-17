// 职责：把新版基地战绑定到共享HUD，管理友舰关注、核心/舰船摘要与抽屉状态。
// 入口：Enter() / Exit() / AttackerResolved()；场景卸载前恢复调试开关，不接管经济或驾驶权。
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeCombatHud : MonoBehaviour
{
    [SerializeField]
    private CombatHud hud;
    [SerializeField]
    private CombatHudPanels panels;
    [SerializeField]
    private ThrusterInfoPanel thrusters;
    [SerializeField]
    private TMP_Text summary;
    [SerializeField]
    private Button nextShip;
    private SiegeGameplayStage stage;
    private int selected;
    private float nextText;
    public CombatHud Hud => hud;

    private void Awake()
    {
        nextShip.onClick.AddListener(() =>
        {
            selected++;
            nextText = 0f;
        });
    }

    public void Enter(SiegeGameplayStage owner)
    {
        stage = owner;
        hud.Bind(owner.gameObject.scene, PlayManager.instance.mainCamera, true);
        panels.Bind();
        selected = 0;
        nextText = 0f;
    }

    public void Exit()
    {
        panels.Restore();
        hud.Unbind();
        stage = null;
    }

    private void OnDisable()
    {
        Exit();
    }

    public void AttackerResolved(string victim, string attacker, int reward, bool playerCredit)
    {
        hud.ShowDestruction(victim, attacker, "WAVE " + stage.Match.Wave + "  /  +" + reward + " PENDING", playerCredit);
    }

    private void Update()
    {
        if (stage == null || stage.Fleet == null || Time.unscaledTime < nextText)
        {
            return;
        }

        nextText = Time.unscaledTime + .1f;
        hud.EliteWave = stage.Match.IsEliteWave;
        SiegeFleetEntry[] ships = stage.Fleet.Ships;
        SiegeFleetEntry focus = stage.Fleet.Driven;
        if (focus == null && ships.Length > 0)
        {
            selected %= ships.Length;
            for (int i = 0; i < ships.Length; i++)
            {
                SiegeFleetEntry candidate = ships[(selected + i) % ships.Length];
                if (candidate != null && candidate.IsAlive)
                {
                    selected = (selected + i) % ships.Length;
                    focus = candidate;
                    break;
                }
            }
        }

        ControlUnit unit = focus != null ? focus.Unit : null;
        hud.SetFocus(unit);
        string control = focus != null && stage.FlightGuidance?.IsGuiding(focus) == true ? "GUIDANCE" : focus == stage.Fleet.Driven && focus != null ? "PILOT" : "AI";
        thrusters.BindOwner(unit, control);
        string core = stage.Base != null && stage.Base.Core != null ? CoreText(stage.Base.Core.GetComponent<Durability>()) : "CORE LOST";
        if (unit != null)
        {
            unit.TryGetTotalDurability(out float current, out _);
            summary.text = core + "\n" + focus.DisplayName + "  /  " + control + $"\nHULL  {current:0} / {focus.MaximumDurability:0}";
        }
        else
        {
            summary.text = core + "\nOBSERVING  /  NO ALLIED SHIP";
        }

        int alive = 0;
        foreach (SiegeFleetEntry ship in ships)
        {
            if (ship != null && ship.IsAlive)
            {
                alive++;
            }
        }

        nextShip.interactable = alive > 1 && stage.Fleet.Driven == null;
        if (stage.Settled)
        {
            panels.CloseAll();
        }
    }

    private static string CoreText(Durability health)
    {
        return health != null ? $"BASE CORE  {health.currentDurability:0} / {health.maxDurability:0}" : "BASE CORE  --";
    }
}
