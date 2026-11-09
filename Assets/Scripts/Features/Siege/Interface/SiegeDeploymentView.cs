// 职责：点击场景泊位预览并购买飞船，点击友舰打开驾驶/修缮卡片；预览没有物理和战斗组件。
// 入口：SelectDock() / SelectShip() / Confirm() / Cancel()；确认时重新验证净空和余额。
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SiegeDeploymentView : MonoBehaviour
{
    [SerializeField]
    private SiegeSession session;
    [SerializeField]
    private SiegeGameplayStage stage;
    private ISiegeDeploymentHost Host => stage != null ? stage : session;

    [SerializeField]
    private GameObject card;
    [SerializeField]
    private Text title;
    [SerializeField]
    private Text details;
    [SerializeField]
    private Dropdown defenderBlueprint;
    [SerializeField]
    private Dropdown attackerBlueprint;
    [SerializeField]
    private Button confirm;
    [SerializeField]
    private Button pilot;
    [SerializeField]
    private Button repair;
    [SerializeField]
    private Button replace;
    [SerializeField]
    private Button undeploy;
    [SerializeField]
    private Button close;
    [SerializeField]
    private Button refresh;
    [SerializeField]
    private GameObject debugPanel;
    [SerializeField]
    private Button[] markers;
    private SiegeShipOption[] options;
    private int optionsDock = int.MinValue;
    private readonly System.Collections.Generic.List<int> filtered = new System.Collections.Generic.List<int>();
    private GameObject preview;
    private int dock = -1;
    private string shipId;
    private SiegePhase phase = SiegePhase.Defeat;
    private UnityEngine.Events.UnityAction[] markerActions;
    public GameObject Preview => preview;
    public int SelectedDock => dock;
    public string SelectedShipId => shipId;

    private void OnEnable()
    {
        markerActions = new UnityEngine.Events.UnityAction[markers.Length];
        for (int i = 0; i < markers.Length; i++)
        {
            int position = i;
            markerActions[i] = () => SelectDock(position);
            markers[i].onClick.AddListener(markerActions[i]);
        }

        defenderBlueprint.onValueChanged.AddListener(SelectOption);
        attackerBlueprint.onValueChanged.AddListener(SelectAttacker);
        confirm.onClick.AddListener(Confirm);
        pilot.onClick.AddListener(Pilot);
        repair.onClick.AddListener(Repair);
        replace.onClick.AddListener(BeginReplacement);
        undeploy.onClick.AddListener(Undeploy);
        close.onClick.AddListener(Cancel);
        refresh.onClick.AddListener(RefreshBlueprints);
        if (Host != null)
        {
            Host.RefitChanged += RefreshSelection;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < markers.Length; i++)
        {
            markers[i].onClick.RemoveListener(markerActions[i]);
        }

        defenderBlueprint.onValueChanged.RemoveListener(SelectOption);
        attackerBlueprint.onValueChanged.RemoveListener(SelectAttacker);
        confirm.onClick.RemoveListener(Confirm);
        pilot.onClick.RemoveListener(Pilot);
        repair.onClick.RemoveListener(Repair);
        replace.onClick.RemoveListener(BeginReplacement);
        undeploy.onClick.RemoveListener(Undeploy);
        close.onClick.RemoveListener(Cancel);
        refresh.onClick.RemoveListener(RefreshBlueprints);
        if (Host != null)
        {
            Host.RefitChanged -= RefreshSelection;
        }

        ClearPreview();
    }

    private void Update()
    {
        if (Host == null || Host.Match == null || Host.Fleet == null)
        {
            return;
        }

        RefreshOptions();
        bool editing = Host.IsEditingBlueprint;
        if (editing || phase != Host.Match.Phase)
        {
            Cancel();
            debugPanel.SetActive(false);
            phase = Host.Match.Phase;
        }

        bool refit = Host.Match.CanRefit && !editing;
        Camera camera = Camera.main;
        RectTransform canvas = (RectTransform)transform;
        for (int i = 0; i < markers.Length; i++)
        {
            Transform deploymentPoint = Host.Fleet.GetDeploymentPoint(i);
            if (deploymentPoint == null || i >= Host.Fleet.DeploymentCount)
            {
                markers[i].gameObject.SetActive(false);
                continue;
            }

            Vector3 point = camera.WorldToScreenPoint(deploymentPoint.position);
            bool empty = Host.Fleet.Ships[i] == null || !Host.Fleet.Ships[i].IsAlive;
            bool operational = Host.Fleet.GetHangar(i) == null || Host.Fleet.GetHangar(i).IsOperational;
            bool visible = refit && operational && point.z > 0f && point.x > 0f && point.x < Screen.width && point.y > 0f && point.y < Screen.height;
            markers[i].gameObject.SetActive(visible);
            Text markerLabel = markers[i].GetComponentInChildren<Text>();
            if (markerLabel != null)
            {
                markerLabel.text = empty ? "DEPLOY" : "MANAGE";
            }

            if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, point, null, out Vector2 local))
            {
                ((RectTransform)markers[i].transform).anchoredPosition = local;
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard?.escapeKey.wasPressedThisFrame == true)
        {
            Cancel();
        }

        if (keyboard?.f3Key.wasPressedThisFrame == true && refit)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
        }

        if (keyboard?.tabKey.wasPressedThisFrame == true && !editing)
        {
            CycleShip();
        }

        if (shipId != null && Host.Fleet.Find(shipId) == null)
        {
            Cancel();
        }

        if (editing || Host.Match.Phase == SiegePhase.Defeat || Mouse.current == null || Cursor.lockState == CursorLockMode.Locked || !Mouse.current.leftButton.wasPressedThisFrame || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            camera.farClipPlane,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            SiegeDeploymentPad pad = hit.collider.GetComponentInParent<SiegeDeploymentPad>();
            RuntimeUnitMember member = hit.collider.GetComponentInParent<RuntimeUnitMember>();
            if (member != null && Host.Fleet.Find(member.ownerUnitId) != null)
            {
                SelectShip(member.ownerUnitId);
            }
            else if (pad != null && refit)
            {
                SelectDock(pad.Index);
            }
        }
    }

    public void SelectDock(int index)
    {
        if (Host.IsEditingBlueprint || Host.Match?.CanRefit != true || Host.Fleet.GetDeploymentPoint(index) == null)
        {
            return;
        }

        SiegeFleetEntry entry = Host.Fleet.Ships[index];
        if (entry?.IsAlive == true)
        {
            SelectShip(entry.Id);
            return;
        }

        dock = index;
        shipId = null;
        RefreshOptions();
        RefreshSelection();
    }

    public void SelectShip(string id)
    {
        if (Host.IsEditingBlueprint || !Host.IsRunning || Host.Fleet.Find(id) == null)
        {
            return;
        }

        dock = -1;
        shipId = id;
        ClearPreview();
        RefreshSelection();
    }

    private void SelectOption(int index)
    {
        if (index >= 0 && index < filtered.Count)
        {
            Host.SelectDefender(filtered[index]);
        }

        RefreshSelection();
    }

    private void SelectAttacker(int index)
    {
        Host.SelectAttacker(index - 1);
    }

    public void RefreshOptions()
    {
        if (options == Host.DefenderOptions && optionsDock == dock)
        {
            return;
        }

        options = Host.DefenderOptions;
        optionsDock = dock;
        filtered.Clear();
        var labels = new System.Collections.Generic.List<string>();
        for (int i = 0; i < options.Length; i++)
        {
            BlockDataList data = JsonUtility.FromJson<BlockDataList>(options[i].BlueprintJson);
            if (dock < 0 || Host.Fleet.FitsHangar(dock, data))
            {
                filtered.Add(i);
                labels.Add(options[i].DisplayName + " / " + options[i].Price.ToString("N0"));
            }
        }

        if (!filtered.Contains(Host.SelectedDefender) && filtered.Count > 0)
        {
            Host.SelectDefender(filtered[0]);
        }

        defenderBlueprint.ClearOptions();
        defenderBlueprint.AddOptions(labels);
        defenderBlueprint.interactable = filtered.Count > 0;
        attackerBlueprint.ClearOptions();
        var attackers = new System.Collections.Generic.List<string>
        {
            "RANDOM ENEMY"
        };
        foreach (SiegeShipOption option in Host.AttackerOptions)
        {
            attackers.Add(option.DisplayName);
        }

        attackerBlueprint.AddOptions(attackers);
        defenderBlueprint.SetValueWithoutNotify(Mathf.Max(0, filtered.IndexOf(Host.SelectedDefender)));
        attackerBlueprint.SetValueWithoutNotify(Host.SelectedAttacker + 1);
    }

    private void RefreshBlueprints()
    {
        Host.RefreshBlueprints();
        RefreshOptions();
        RefreshSelection();
    }

    public void RefreshSelection()
    {
        if (Host == null || Host.IsEditingBlueprint || Host.Match == null || !Host.IsRunning)
        {
            Cancel();
            return;
        }

        bool placing = dock >= 0 && Host.Match.CanRefit;
        SiegeFleetEntry ship = Host.Fleet.Find(shipId);
        bool replacing = placing && ship != null;
        card.SetActive(placing || ship != null);
        defenderBlueprint.gameObject.SetActive(placing);
        confirm.gameObject.SetActive(placing);
        pilot.gameObject.SetActive(!placing && ship != null && stage == null);
        repair.gameObject.SetActive(!placing && ship != null && Host.Match.CanRefit);
        replace.gameObject.SetActive(!placing && ship != null && Host.Match.CanRefit);
        undeploy.gameObject.SetActive(!placing && ship != null && Host.Match.CanRefit);
        confirm.GetComponentInChildren<Text>(true).text = replacing ? "CONFIRM REPLACEMENT" : "DEPLOY SHIP";
        ClearPreview();
        if (placing)
        {
            title.text = (replacing ? "REPLACE / HANGAR " : "HANGAR ") + (dock + 1);
            int index = Host.SelectedDefender;
            if (filtered.Count == 0 || !filtered.Contains(index))
            {
                details.text = "No ship blueprint fits this hangar. Resize the hangar in Base Editor or refresh blueprints in F3.";
                confirm.interactable = false;
                return;
            }

            SiegeShipOption option = Host.DefenderOptions[index];
            BlockDataList data = SiegeFleet.PrepareBlueprint(option.BlueprintJson, out string id);
            Pose point = Host.Fleet.GetDeploymentPose(dock, data);
            preview = BlueprintModelPreview.Create(data.blocks, null, "Siege deployment preview");
            preview.transform.SetPositionAndRotation(point.position, point.rotation);
            bool clear = SiegeRefit.IsClear(data, point.position, point.rotation, replacing ? ship.Id : id, out string reason);
            bool available = replacing || Host.Fleet.Ships[dock]?.IsAlive != true;
            long credit = replacing ? Host.Fleet.CancellationCredit(dock) : 0;
            bool affordable = Host.Match.CanExchange(option.Price, credit);
            confirm.interactable = available && clear && affordable;
            details.text = !available ? "This deployment position is occupied." : !clear ? reason : !affordable ? "Insufficient credits after trade-in: " + credit.ToString("N0") : option.BlockCount + " modules / " + option.Price.ToString("N0") + " credits.";
            if (replacing)
            {
                details.text += "\nTrade-in: " + credit.ToString("N0") + " / Net cost: " + (option.Price - credit).ToString("N0") + ". Old ship retained if replacement fails.";
            }
        }
        else if (ship != null)
        {
            title.text = ship.DisplayName;
            pilot.interactable = Host.Match.Phase != SiegePhase.Returning && ship != Host.Fleet.Driven;
            long quote = Host.Match.CanRefit ? SiegeRefit.Quote(ship, Host.Settings) : 0;
            repair.interactable = quote > 0 && Host.Match.CanAfford(quote);
            details.text = ship == Host.Fleet.Driven ? "PILOT" : "AUTONOMOUS SUPPORT";
            if (Host.Match.CanRefit)
            {
                details.text += quote > 0 ? " / REPAIR " + quote.ToString("N0") : " / INTACT";
                int slot = Array.IndexOf(Host.Fleet.Ships, ship);
                details.text += "\nUndeploy / trade-in refund: " + Host.Fleet.CancellationCredit(slot).ToString("N0") + " credits. Damage reduces the refund by repair cost.";
            }
        }
    }

    public void Confirm()
    {
        bool success = dock >= 0 && (shipId != null ? Host.TryReplaceAt(Host.SelectedDefender, dock, out _) : Host.TryDeployAt(Host.SelectedDefender, dock, out _));
        if (success)
        {
            Cancel();
        }
        else
        {
            RefreshSelection();
        }
    }

    public void BeginReplacement()
    {
        SiegeFleetEntry ship = Host.Fleet.Find(shipId);
        if (ship == null || !Host.Match.CanRefit)
        {
            return;
        }

        dock = Array.IndexOf(Host.Fleet.Ships, ship);
        RefreshOptions();
        RefreshSelection();
    }

    public void Undeploy()
    {
        SiegeFleetEntry ship = Host.Fleet.Find(shipId);
        if (ship != null && Host.TryCancelDeployment(Array.IndexOf(Host.Fleet.Ships, ship), out _))
        {
            Cancel();
        }
    }

    private void Pilot()
    {
        Host.TryTakeControl(shipId);
        Cancel();
    }

    private void Repair()
    {
        Host.TryRepairShip(shipId);
    }

    public void CycleShip()
    {
        SiegeFleetEntry[] ships = Host.Fleet.Ships;
        int start = Array.IndexOf(ships, Host.Fleet.Driven);
        for (int step = 1; step <= ships.Length; step++)
        {
            SiegeFleetEntry next = ships[(start + step + ships.Length) % ships.Length];
            if (next?.IsAlive == true)
            {
                Host.TryTakeControl(next.Id);
                Cancel();
                return;
            }
        }
    }

    public void Cancel()
    {
        dock = -1;
        shipId = null;
        card.SetActive(false);
        ClearPreview();
    }

    private void ClearPreview()
    {
        if (preview != null)
        {
            preview.SetActive(false);
            Destroy(preview);
            preview = null;
        }
    }
}
