// 职责：绑定可编辑UGUI阵地战面板，低频显示金币、波次、核心耐久和舰队驾驶权。
// 入口：OnEnable()注册按钮，OnDisable()解绑；规则与修缮由SiegeSession执行。
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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
            deploymentActions[index] = () => session.TryDeploy(option, out _);
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
        SiegeMatch match = session.Match;
        heading.text = match.Phase == SiegePhase.Defeat ? "BASTION LOST" : "BASTION / " + match.Phase.ToString().ToUpperInvariant();
        economy.text = "CREDITS  " + match.Coins.ToString("N0") + "     WAVE  " + match.Wave + "     RAIDERS  " + match.ActiveAttackers + " / +" + match.PendingAttackers;
        status.text = session.Status;
        Durability core = session.Fleet.Base.Core != null ? session.Fleet.Base.Core.GetComponent<Durability>() : null;
        coreHealth.text = core != null ? "CORE  " + Mathf.CeilToInt(core.currentDurability) + " / " + Mathf.CeilToInt(core.maxDurability) : "CORE DESTROYED";
        wave.interactable = match.CanRefit;
        repairCore.interactable = match.CanRefit && session.Fleet.Base.IsAlive;
        repairShip.interactable = match.CanRefit && session.Fleet.Driven != null && session.Fleet.Driven.IsAlive;
        for (int index = 0; index < deploy.Length; index++)
        {
            SiegeShipOption option = session.Settings.Ships[index];
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
        if (CameraController.instance == null || Mouse.current == null)
        {
            return;
        }

        bool looking = Mouse.current.rightButton.isPressed;
        CameraController.instance.currentMode = looking ? CameraMode.ThirdPerson : CameraMode.ThirdPersonLock;
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !looking;
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
