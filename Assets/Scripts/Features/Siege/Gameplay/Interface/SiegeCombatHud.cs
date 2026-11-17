// 职责：把新版基地战绑定到共享HUD，管理友舰关注、核心/舰船摘要与抽屉状态。
// 入口：Enter() / Exit() / AttackerResolved()；场景卸载前恢复调试开关，不接管经济或驾驶权。
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    [SerializeField]
    private Button nextWave;
    [SerializeField]
    private Button repairBase;
    [SerializeField]
    private TMP_Text phaseLabel;
    [SerializeField]
    private TMP_Text countdown;
    [SerializeField]
    private TMP_Text battleStatus;
    [SerializeField]
    private CanvasGroup failure;
    [SerializeField]
    private TMP_Text failureDetail;
    [SerializeField]
    private Button dismissFailure;
    [SerializeField]
    private Button restartFailure;
    private SiegeGameplayStage stage;
    private int selected;
    private float nextText;
    private ControlUnit lastClicked;
    private float lastClickTime;
    public CombatHud Hud => hud;

    private void Awake()
    {
        nextWave.onClick.AddListener(() => stage?.BeginWave());
        repairBase.onClick.AddListener(() => stage?.Repair());
        dismissFailure.onClick.AddListener(() => stage?.ReturnToMenu());
        if (restartFailure != null)
        {
            restartFailure.onClick.AddListener(() => PlayManager.instance.Modes.Restart());
        }

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
        failure.alpha = 0f;
        failure.gameObject.SetActive(false);
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
        PollShipControl();
        if (stage == null || stage.Fleet == null || Time.unscaledTime < nextText)
        {
            return;
        }

        nextText = Time.unscaledTime + .1f;
        bool refit = !stage.Settled && stage.Match.CanRefit;
        nextWave.gameObject.SetActive(refit);
        long quote = refit && stage.Base != null ? stage.Base.Quote() : 0;
        repairBase.gameObject.SetActive(refit && quote > 0);
        repairBase.interactable = quote <= stage.Match.Coins;
        repairBase.GetComponentInChildren<TMP_Text>().text = "REPAIR BASE  /  " + quote;
        phaseLabel.text = stage.Settled ? "SIEGE LOST" : stage.Match.CanRefit ? "WAVE " + (stage.Match.Wave + 1) + "  /  READY" : stage.Clock.Label;
        float seconds = stage.Clock.PreparationRemaining > 0f ? stage.Clock.PreparationRemaining : stage.Clock.Remaining;
        int total = Mathf.CeilToInt(seconds);
        countdown.text = !stage.Settled && stage.Clock.Active ? $"{total / 60:00}:{total % 60:00}" : "--:--";
        countdown.color = seconds <= 15f && stage.Clock.PreparationRemaining <= 0f ? hud.Theme.hostile : hud.Theme.friendly;
        battleStatus.text = $"CREDITS {stage.Match.Coins:N0}  +{stage.Match.PendingCoins:N0} PENDING\nHOSTILES {stage.Match.ActiveAttackers + stage.Match.PendingAttackers}  /  KILLS {stage.Match.DefeatedAttackers}";
        if (stage.Match.Phase == SiegePhase.Returning)
        {
            battleStatus.text += $"\nMOVEMENT CHECK  {stage.Clock.StallRemaining:0}s";
        }

        failure.gameObject.SetActive(stage.ShowFailure);
        failure.alpha = stage.ShowFailure ? 1f : 0f;
        failure.blocksRaycasts = failure.interactable = stage.ShowFailure;
        failureDetail.text = stage.Status;
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

    private void PollShipControl()
    {
        if (stage == null || PauseMenu.IsPaused || !stage.IsRunning || stage.Match.Phase != SiegePhase.Battle)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame && stage.Fleet.Driven != null)
        {
            stage.Observe();
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        Camera camera = PlayManager.instance.mainCamera;
        if (!Physics.Raycast(
            camera.ScreenPointToRay(mouse.position.ReadValue()),
            out RaycastHit hit,
            2000f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            lastClicked = null;
            return;
        }

        ControlUnit unit = hit.collider.GetComponentInParent<ControlUnit>();
        if (unit == null || unit.faction != UnitFaction.Player || !unit.HasValidCockpit || unit.gameObject.scene != stage.gameObject.scene)
        {
            lastClicked = null;
            return;
        }

        if (unit == lastClicked && Time.unscaledTime - lastClickTime <= .35f)
        {
            foreach (SiegeFleetEntry ship in stage.Fleet.Ships)
            {
                if (ship != null && ship.Unit == unit)
                {
                    stage.TryTakeControl(ship.Id);
                    break;
                }
            }

            lastClicked = null;
        }
        else
        {
            lastClicked = unit;
            lastClickTime = Time.unscaledTime;
        }
    }

    private static string CoreText(Durability health)
    {
        return health != null ? $"BASE CORE  {health.currentDurability:0} / {health.maxDurability:0}" : "BASE CORE  --";
    }
}
