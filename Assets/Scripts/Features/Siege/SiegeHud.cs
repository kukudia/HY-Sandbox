// 职责：绑定可编辑UGUI阵地战面板，低频显示金币、波次、核心耐久和舰队驾驶权。
// 入口：OnEnable()注册按钮，OnDisable()解绑；规则与修缮由SiegeSession执行。
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
    private SiegeShipOption[] displayedOptions;
    private UnityAction[] deploymentActions;
    private UnityAction[] controlActions;
    private float nextRefresh;
    private void OnEnable()
    {
        deploymentActions = new UnityAction[deploy.Length];
        controlActions = new UnityAction[takeControl.Length];
        for (int index = 0; index < deploy.Length; index++)
        {
            int option = index;
            deploymentActions[index] = () => session.TryDeploy(defenderBlueprint != null ? defenderBlueprint.value : option, out _);
            deploy[index].onClick.AddListener(deploymentActions[index]);
        }

        for (int index = 0; index < takeControl.Length; index++)
        {
            int slot = index;
            controlActions[index] = () => session.TryTakeControl(slot);
            takeControl[index].onClick.AddListener(controlActions[index]);
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
        if (session.Match == null || Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + 0.25f;
        if (displayedOptions != session.DefenderOptions)
        {
            RefreshOptions();
        }

        SiegeMatch match = session.Match;
        heading.text = match.Phase == SiegePhase.Defeat ? "BASTION LOST" : "BASTION / " + match.Phase.ToString().ToUpperInvariant();
        string preview = match.CanRefit ? "     NEXT  " + session.NextWaveSize + "x " + session.AttackerPreview : string.Empty;
        economy.text = "CREDITS  " + match.Coins.ToString("N0") + "     WAVE  " + match.Wave + "     RAIDERS  " + match.ActiveAttackers + " / +" + match.PendingAttackers + preview;
        status.text = session.Status;
        Durability core = session.Fleet.Base.Core != null ? session.Fleet.Base.Core.GetComponent<Durability>() : null;
        coreHealth.text = core != null ? "CORE  " + Mathf.CeilToInt(core.currentDurability) + " / " + Mathf.CeilToInt(core.maxDurability) : "CORE DESTROYED";
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
            if (optionIndex >= session.DefenderOptions.Length)
            {
                deploy[index].interactable = false;
                deploy[index].GetComponentInChildren<Text>().text = "NO VALID SAVES BLUEPRINT";
                continue;
            }

            SiegeShipOption option = session.DefenderOptions[optionIndex];
            deploy[index].interactable = match.CanAfford(option.Price);
            deploy[index].GetComponentInChildren<Text>().text = option.DisplayName.ToUpperInvariant() + "  /  " + option.Price;
        }

        for (int index = 0; index < takeControl.Length; index++)
        {
            SiegeFleetEntry ship = session.Fleet.Ships[index];
            bool alive = ship != null && ship.IsAlive;
            takeControl[index].interactable = alive && match.Phase != SiegePhase.Defeat;
            takeControl[index].GetComponentInChildren<Text>().text = alive ? (ship == session.Fleet.Driven ? "PILOT  /  " : "AI  /  ") + ship.DisplayName : "BAY " + (index + 1) + "  /  EMPTY";
        }
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
            defenders.Add(option.DisplayName + " / " + option.Price);
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
