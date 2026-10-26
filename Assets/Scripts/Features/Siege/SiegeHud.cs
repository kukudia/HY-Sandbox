// 职责：绑定可编辑UGUI阵地战面板，低频显示金币、波次、核心耐久和舰队驾驶权。
// 入口：OnEnable()注册按钮与缓存标签，OnDisable()解绑；规则与修缮由SiegeSession执行。
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

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
    [SerializeField, Tooltip("顶部指挥栏的核心耐久填充条；按比例改变锚点宽度。")]
    private Image coreFill;
    [SerializeField, Tooltip("默认隐藏的按键说明面板，F2切换，避免长期遮挡战场。")]
    private GameObject helpPanel;
    [SerializeField, Tooltip("整备阶段的部署控件组；战斗中隐藏，只保留泊位切换。")]
    private GameObject deployGroup;
    [SerializeField, Tooltip("战斗阶段的底部波次进度面板。")]
    private GameObject combatGroup;
    [SerializeField]
    private Text combatLabel;
    [SerializeField]
    private Image combatFill;
    [SerializeField, Tooltip("波次入场横幅，进入战斗时短暂显示后淡出。")]
    private GameObject bannerPanel;
    [SerializeField]
    private Text bannerTitle;
    [SerializeField]
    private Text bannerSubtitle;
    [SerializeField]
    private CanvasGroup bannerGroup;
    [SerializeField, Tooltip("击毁敌舰的短时提示；只显示公开战果，不参与结算。")]
    private Text killFeed;
    [SerializeField]
    private CanvasGroup killGroup;
    [SerializeField, Tooltip("Main的坐舰耐久面板：只在战斗阶段显示，避免整备时占用视野。")]
    private GameObject shipStatus;
    [SerializeField, Tooltip("核心受击的全屏告警层；低耐久时保持低频脉动。")]
    private CanvasGroup damageGroup;
    [SerializeField]
    private GameObject dockPanel;
    [SerializeField]
    private GameObject baysGroup;
    [SerializeField]
    private GameObject refitGroup;
    [SerializeField]
    private GameObject dockTitle;
    [SerializeField]
    private Text shipTitle;
    [SerializeField]
    private Image shipIntegrityFill;
    [SerializeField]
    private Text shipIntegrityValue;
    [SerializeField]
    private Image shipCockpitFill;
    [SerializeField]
    private Text shipCockpitValue;
    [SerializeField]
    private Button wave;
    [SerializeField]
    private Button repairCore;
    [SerializeField]
    private Button repairShip;
    [SerializeField]
    private Button restart;
    [SerializeField]
    private Button[] deploy = new Button[0];
    [SerializeField]
    private Button[] takeControl = new Button[0];
    [SerializeField]
    private Dropdown defenderBlueprint;
    [SerializeField]
    private Dropdown attackerBlueprint;
    [SerializeField]
    private Button refreshBlueprints;
    private static readonly Color Accent = new Color(0.31f, 0.88f, 0.95f, 1f);
    private static readonly Color Idle = new Color(0.83f, 0.94f, 0.98f, 1f);
    private static readonly Color Muted = new Color(0.5f, 0.62f, 0.68f, 1f);
    private static readonly Color Warning = new Color(1f, 0.78f, 0.32f, 1f);
    private static readonly Color Critical = new Color(1f, 0.36f, 0.33f, 1f);
    private SiegeShipOption[] displayedOptions;
    private UnityAction[] deploymentActions;
    private UnityAction[] controlActions;
    private Text[] deploymentLabels = new Text[0];
    private Text[] controlLabels = new Text[0];
    private GameObject workshopButton;
    private float nextRefresh;
    private SiegePhase phaseTransition = SiegePhase.Defeat;
    private int lastDefeated;
    private int waveTotal;
    private float bannerUntil;
    private float killUntil;
    private float coreHitUntil;
    private float coreRatio = 1f;
    private void OnEnable()
    {
        workshopButton = transform.Find("Open Blueprint Workshop")?.gameObject;
        deploymentActions = new UnityAction[deploy.Length];
        controlActions = new UnityAction[takeControl.Length];
        deploymentLabels = new Text[deploy.Length];
        controlLabels = new Text[takeControl.Length];
        for (int index = 0; index < deploy.Length; index++)
        {
            int option = index;
            deploymentActions[index] = () => session.TryDeploy(defenderBlueprint != null ? defenderBlueprint.value : option, out _);
            deploy[index].onClick.AddListener(deploymentActions[index]);
            deploymentLabels[index] = deploy[index].GetComponentInChildren<Text>();
        }

        for (int index = 0; index < takeControl.Length; index++)
        {
            int slot = index;
            controlActions[index] = () => session.TryTakeControl(slot);
            takeControl[index].onClick.AddListener(controlActions[index]);
            controlLabels[index] = takeControl[index].GetComponentInChildren<Text>();
        }

        wave.onClick.AddListener(StartWave);
        repairCore.onClick.AddListener(RepairCore);
        repairShip.onClick.AddListener(RepairShip);
        restart.onClick.AddListener(Restart);
        refreshBlueprints?.onClick.AddListener(RefreshBlueprints);
        defenderBlueprint?.onValueChanged.AddListener(SelectDefender);
        attackerBlueprint?.onValueChanged.AddListener(SelectAttacker);
    }

    private void OnDisable()
    {
        for (int index = 0; index < deploy.Length; index++)
        {
            deploy[index].onClick.RemoveListener(deploymentActions[index]);
        }

        for (int index = 0; index < takeControl.Length; index++)
        {
            takeControl[index].onClick.RemoveListener(controlActions[index]);
        }

        wave.onClick.RemoveListener(StartWave);
        repairCore.onClick.RemoveListener(RepairCore);
        repairShip.onClick.RemoveListener(RepairShip);
        restart.onClick.RemoveListener(Restart);
        refreshBlueprints?.onClick.RemoveListener(RefreshBlueprints);
        defenderBlueprint?.onValueChanged.RemoveListener(SelectDefender);
        attackerBlueprint?.onValueChanged.RemoveListener(SelectAttacker);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        PollCamera();
        ToggleHelp();
        UpdateTransient();
        if (session == null || session.Match == null || Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + 0.25f;
        if (displayedOptions != session.DefenderOptions)
        {
            RefreshOptions();
        }

        SiegeMatch match = session.Match;
        UpdatePhase(match);
        heading.text = match.Phase == SiegePhase.Battle && match.IsEliteWave ? "BASTION   ·   ELITE WAVE" : match.Phase == SiegePhase.Defeat ? "BASTION   ·   LOST" : "BASTION   ·   " + match.Phase.ToString().ToUpperInvariant();
        string preview = match.CanRefit ? "     NEXT  " + session.NextWaveSize + " × " + session.AttackerPreview : string.Empty;
        int remaining = match.ActiveAttackers + match.PendingAttackers;
        economy.text = "CREDITS  " + match.Coins.ToString("N0") + "      WAVE  " + match.Wave + "      RAIDERS  " + remaining + preview;
        status.text = session.Status;
        Durability core = session.Fleet.Base.Core != null ? session.Fleet.Base.Core.GetComponent<Durability>() : null;
        float ratio = core != null && core.maxDurability > 0f ? Mathf.Clamp01(core.currentDurability / core.maxDurability) : 0f;
        if (ratio < coreRatio - 0.0005f)
        {
            coreHitUntil = Time.unscaledTime + 0.5f;
            VisualEffectsManager.instance?.PlayCameraShake(0.35f + 0.5f * (coreRatio - ratio), 0.22f);
        }

        coreRatio = ratio;
        coreHealth.text = core != null ? "CORE  " + Mathf.CeilToInt(core.currentDurability) + " / " + Mathf.CeilToInt(core.maxDurability) : "CORE DESTROYED";
        coreHealth.color = core == null ? Critical : ratio < 0.25f ? Critical : ratio < 0.55f ? Warning : Idle;
        if (coreFill != null)
        {
            RectTransform fill = coreFill.rectTransform;
            fill.anchorMax = new Vector2(ratio, 1f);
            fill.offsetMax = Vector2.zero;
            coreFill.color = ratio < 0.25f ? Critical : ratio < 0.55f ? Warning : Accent;
        }

        wave.interactable = match.CanRefit && session.AttackerOptions.Length > 0;
        if (defenderBlueprint != null)
        {
            defenderBlueprint.interactable = match.CanRefit;
            attackerBlueprint.interactable = match.CanRefit;
            refreshBlueprints.interactable = match.CanRefit;
        }

        repairCore.interactable = match.CanRefit && session.Fleet.Base.IsAlive;
        repairShip.interactable = match.CanRefit && session.Fleet.Driven != null && session.Fleet.Driven.IsAlive;
        for (int index = 0; index < deploy.Length; index++)
        {
            int optionIndex = defenderBlueprint != null ? defenderBlueprint.value : index;
            Text label = deploymentLabels[index];
            if (optionIndex >= session.DefenderOptions.Length)
            {
                deploy[index].interactable = false;
                label.text = "NO VALID SAVES BLUEPRINT";
                label.color = Muted;
                continue;
            }

            SiegeShipOption option = session.DefenderOptions[optionIndex];
            deploy[index].interactable = match.CanAfford(option.Price);
            label.text = "DEPLOY  " + option.DisplayName.ToUpperInvariant() + "   ·   " + option.Price.ToString("N0");
            label.color = Idle;
        }

        for (int index = 0; index < takeControl.Length; index++)
        {
            SiegeFleetEntry ship = session.Fleet.Ships[index];
            bool alive = ship != null && ship.IsAlive;
            takeControl[index].interactable = alive && match.Phase != SiegePhase.Defeat;
            Text label = controlLabels[index];
            if (!alive)
            {
                label.text = "BAY " + (index + 1) + "   ·   EMPTY";
                label.color = Muted;
                continue;
            }

            bool pilot = ship == session.Fleet.Driven;
            label.text = "BAY " + (index + 1) + "   ·   " + (pilot ? "PILOT   " : "AI   ") + ship.DisplayName.ToUpperInvariant();
            label.color = pilot ? Accent : Idle;
        }
    }

    /// <summary>每帧推进横幅与击杀提示的淡出；只改CanvasGroup透明度，不做布局或文字重建。</summary>
    private void UpdateTransient()
    {
        if (bannerGroup != null && bannerPanel != null && bannerPanel.activeSelf)
        {
            float remaining = bannerUntil - Time.unscaledTime;
            if (remaining <= 0f)
            {
                bannerPanel.SetActive(false);
            }
            else
            {
                bannerGroup.alpha = Mathf.Clamp01(remaining / 0.6f);
            }
        }

        if (killGroup != null)
        {
            float remaining = killUntil - Time.unscaledTime;
            killGroup.alpha = remaining > 0f ? Mathf.Clamp01(remaining / 0.5f) : 0f;
        }

        if (damageGroup != null)
        {
            float flash = coreHitUntil > Time.unscaledTime ? Mathf.Clamp01((coreHitUntil - Time.unscaledTime) / 0.5f) : 0f;
            // 低耐久常驻低频脉动，只在比例变化时读取，不参与布局或物理。
            float alarm = coreRatio > 0f && coreRatio < 0.3f ? 0.24f + 0.10f * Mathf.Sin(Time.unscaledTime * 2.6f) : 0f;
            damageGroup.alpha = Mathf.Clamp01(Mathf.Max(flash * 0.85f, alarm));
        }
    }

    /// <summary>按阶段切换整备/战斗控件，并给出波次总览、入场横幅与击毁提示。</summary>
    private void UpdatePhase(SiegeMatch match)
    {
        bool battle = match.Phase == SiegePhase.Battle;
        if (deployGroup != null)
        {
            deployGroup.SetActive(match.CanRefit);
        }

        if (wave != null && wave.gameObject.activeSelf != match.CanRefit)
        {
            wave.gameObject.SetActive(match.CanRefit);
        }

        if (workshopButton != null && workshopButton.activeSelf != match.CanRefit)
        {
            workshopButton.SetActive(match.CanRefit);
        }

        if (combatGroup != null)
        {
            combatGroup.SetActive(battle);
        }

        if (shipStatus != null && shipStatus.activeSelf != battle)
        {
            shipStatus.SetActive(battle);
        }

        if (phaseTransition != match.Phase)
        {
            phaseTransition = match.Phase;
            ApplyDockLayout(battle);
            if (battle)
            {
                waveTotal = Mathf.Max(1, match.ActiveAttackers + match.PendingAttackers);
                if (match.IsEliteWave)
                {
                    ShowBanner("ELITE WAVE " + match.Wave, waveTotal + " HEAVY RAIDERS   ·   DOUBLE REWARD");
                }
                else
                {
                    ShowBanner("WAVE " + match.Wave, waveTotal == 1 ? "1 RAIDER INBOUND" : waveTotal + " RAIDERS INBOUND");
                }
            }
            else if (match.Phase == SiegePhase.Intermission)
            {
                ShowBanner("WAVE " + match.Wave + " CLEARED", "REPAIR AND REINFORCE");
            }
            else if (match.Phase == SiegePhase.Defeat)
            {
                ShowBanner("BASTION LOST", "THE CORE IS GONE");
            }
        }

        if (battle)
        {
            RefreshShipStatus();
        }

        if (match.DefeatedAttackers > lastDefeated)
        {
            lastDefeated = match.DefeatedAttackers;
            if (killFeed != null)
            {
                killFeed.text = "RAIDER DESTROYED   ·   +" + match.KillReward.ToString("N0") + " CREDITS";
                if (session.Combo >= 2 && session.LastComboBonus > 0)
                {
                    killFeed.text += "      COMBO x" + session.Combo + "   +" + session.LastComboBonus.ToString("N0");
                }

                killUntil = Time.unscaledTime + 2.6f;
            }
        }

        if (battle && combatLabel != null)
        {
            int remaining = match.ActiveAttackers + match.PendingAttackers;
            combatLabel.text = "WAVE " + match.Wave + "   ·   RAIDERS " + remaining + " / " + waveTotal;
            if (combatFill != null)
            {
                combatFill.rectTransform.anchorMax = new Vector2(waveTotal > 0 ? Mathf.Clamp01((float)remaining / waveTotal) : 0f, 1f);
                combatFill.rectTransform.offsetMax = Vector2.zero;
            }
        }
    }

    /// <summary>战斗阶段收起部署区并压缩舰队坞，把视野让给战场；整备阶段恢复完整高度。</summary>
    private void ApplyDockLayout(bool battle)
    {
        if (refitGroup != null && refitGroup.activeSelf == battle)
        {
            refitGroup.SetActive(!battle);
        }

        if (dockTitle != null && dockTitle.activeSelf == battle)
        {
            dockTitle.SetActive(!battle);
        }

        if (baysGroup != null)
        {
            RectTransform bays = (RectTransform)baysGroup.transform;
            Vector2 target = battle ? new Vector2(0f, -16f) : new Vector2(0f, -266f);
            if (bays.anchoredPosition != target)
            {
                bays.anchoredPosition = target;
            }
        }

        if (dockPanel != null)
        {
            RectTransform dock = (RectTransform)dockPanel.transform;
            float height = battle ? 344f : 726f;
            if (!Mathf.Approximately(dock.sizeDelta.y, height))
            {
                dock.sizeDelta = new Vector2(dock.sizeDelta.x, height);
            }
        }
    }

    /// <summary>座舰耐久只在战斗界面刷新；整备阶段的舰队状态由泊位按钮表达。</summary>
    private void RefreshShipStatus()
    {
        SiegeFleetEntry driven = session.Fleet != null ? session.Fleet.Driven : null;
        ControlUnit unit = driven != null ? driven.Unit : null;
        if (unit == null || !driven.IsAlive)
        {
            if (shipTitle != null)
            {
                shipTitle.text = "PILOT   ·   NO SHIP";
            }

            SetBar(shipIntegrityFill, shipIntegrityValue, 0f, 0f);
            SetBar(shipCockpitFill, shipCockpitValue, 0f, 0f);
            return;
        }

        if (shipTitle != null)
        {
            shipTitle.text = "PILOT   ·   " + driven.DisplayName.ToUpperInvariant();
        }

        if (unit.TryGetTotalDurability(out float integrity, out float maximum))
        {
            SetBar(shipIntegrityFill, shipIntegrityValue, integrity, maximum);
        }

        Durability cockpit = unit.cockpit != null ? unit.cockpit.GetComponent<Durability>() : null;
        if (cockpit != null)
        {
            SetBar(shipCockpitFill, shipCockpitValue, cockpit.currentDurability, cockpit.maxDurability);
        }
    }

    private static void SetBar(Image fill, Text value, float current, float maximum)
    {
        float ratio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        if (fill != null)
        {
            fill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            fill.rectTransform.offsetMax = Vector2.zero;
            fill.color = ratio < 0.25f ? Critical : ratio < 0.55f ? Warning : Accent;
        }

        if (value != null)
        {
            value.text = Mathf.CeilToInt(Mathf.Max(0f, current)) + " / " + Mathf.CeilToInt(Mathf.Max(0f, maximum));
        }
    }

    private void ShowBanner(string title, string subtitle)
    {
        if (bannerPanel == null)
        {
            return;
        }

        if (bannerTitle != null)
        {
            bannerTitle.text = title;
        }

        if (bannerSubtitle != null)
        {
            bannerSubtitle.text = subtitle;
        }

        bannerPanel.SetActive(true);
        if (bannerGroup != null)
        {
            bannerGroup.alpha = 1f;
        }

        bannerUntil = Time.unscaledTime + 2.6f;
    }

    private void ToggleHelp()
    {
        if (helpPanel == null || Keyboard.current == null || !Keyboard.current.f2Key.wasPressedThisFrame)
        {
            return;
        }

        helpPanel.SetActive(!helpPanel.activeSelf);
    }

    private void PollCamera()
    {
        if (InputManager.instance != null && InputManager.instance.isActiveAndEnabled)
        {
            return;
        }

        if (CameraController.instance == null || Mouse.current == null)
        {
            return;
        }

        bool looking = Mouse.current.rightButton.isPressed;
        CameraController.instance.currentMode = looking ? CameraMode.ThirdPerson : CameraMode.ThirdPersonLock;
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !looking;
    }

    private void RefreshOptions()
    {
        displayedOptions = session.DefenderOptions;
        if (defenderBlueprint == null)
        {
            return;
        }

        var defenders = new List<string>();
        foreach (SiegeShipOption option in session.DefenderOptions)
        {
            defenders.Add(option.DisplayName + "  /  " + option.Price.ToString("N0"));
        }

        defenderBlueprint.ClearOptions();
        defenderBlueprint.AddOptions(defenders);
        var attackers = new List<string>
        {
            "RANDOM ENEMY"
        };
        foreach (SiegeShipOption option in session.AttackerOptions)
        {
            attackers.Add(option.DisplayName);
        }

        attackerBlueprint.ClearOptions();
        attackerBlueprint.AddOptions(attackers);
        defenderBlueprint.SetValueWithoutNotify(session.SelectedDefender >= 0 ? session.SelectedDefender : 0);
        attackerBlueprint.SetValueWithoutNotify(session.SelectedAttacker + 1);
    }

    private void RefreshBlueprints()
    {
        session.RefreshBlueprints();
        RefreshOptions();
    }

    private void SelectAttacker(int index)
    {
        session.SelectAttacker(index - 1);
    }

    private void SelectDefender(int index)
    {
        session.SelectDefender(index);
    }

    private void StartWave()
    {
        session.BeginNextWave();
    }

    private void RepairCore()
    {
        session.TryRepair(true);
    }

    private void RepairShip()
    {
        session.TryRepair(false);
    }

    private void Restart()
    {
        SceneManager.LoadScene(gameObject.scene.name);
    }
}
