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
    private Button close;
    [SerializeField]
    private Button refresh;
    [SerializeField]
    private GameObject debugPanel;
    [SerializeField]
    private Button[] markers;
    private SiegeShipOption[] options;
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
        close.onClick.AddListener(Cancel);
        refresh.onClick.AddListener(RefreshBlueprints);
        session.RefitChanged += RefreshSelection;
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
        close.onClick.RemoveListener(Cancel);
        refresh.onClick.RemoveListener(RefreshBlueprints);
        session.RefitChanged -= RefreshSelection;
        ClearPreview();
    }

    private void Update()
    {
        if (session.Match == null || session.Fleet == null)
        {
            return;
        }

        RefreshOptions();
        bool editing = session.IsEditingBlueprint;
        if (editing || phase != session.Match.Phase)
        {
            Cancel();
            debugPanel.SetActive(false);
            phase = session.Match.Phase;
        }

        bool refit = session.Match.CanRefit && !editing;
        Camera camera = Camera.main;
        RectTransform canvas = (RectTransform)transform;
        for (int i = 0; i < markers.Length; i++)
        {
            Vector3 point = camera.WorldToScreenPoint(session.Fleet.GetDeploymentPoint(i).position);
            bool empty = session.Fleet.Ships[i] == null || !session.Fleet.Ships[i].IsAlive;
            bool visible = refit && empty && point.z > 0f && point.x > 0f && point.x < Screen.width && point.y > 0f && point.y < Screen.height;
            markers[i].gameObject.SetActive(visible);
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

        if (shipId != null && session.Fleet.Find(shipId) == null)
        {
            Cancel();
        }

        if (editing || session.Match.Phase == SiegePhase.Defeat || Mouse.current == null || Cursor.lockState == CursorLockMode.Locked || !Mouse.current.leftButton.wasPressedThisFrame || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
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
            if (member != null && session.Fleet.Find(member.ownerUnitId) != null)
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
        if (session.IsEditingBlueprint || session.Match?.CanRefit != true || session.Fleet.GetDeploymentPoint(index) == null)
        {
            return;
        }

        SiegeFleetEntry entry = session.Fleet.Ships[index];
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
        if (session.IsEditingBlueprint || !session.IsRunning || session.Fleet.Find(id) == null)
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
        session.SelectDefender(index);
        RefreshSelection();
    }

    private void SelectAttacker(int index)
    {
        session.SelectAttacker(index - 1);
    }

    public void RefreshOptions()
    {
        if (options == session.DefenderOptions)
        {
            return;
        }

        options = session.DefenderOptions;
        defenderBlueprint.ClearOptions();
        defenderBlueprint.AddOptions(new System.Collections.Generic.List<string>(Array.ConvertAll(options, option => option.DisplayName + " / " + option.Price.ToString("N0"))));
        attackerBlueprint.ClearOptions();
        var attackers = new System.Collections.Generic.List<string>
        {
            "RANDOM ENEMY"
        };
        foreach (SiegeShipOption option in session.AttackerOptions)
        {
            attackers.Add(option.DisplayName);
        }

        attackerBlueprint.AddOptions(attackers);
        defenderBlueprint.SetValueWithoutNotify(Mathf.Max(0, session.SelectedDefender));
        attackerBlueprint.SetValueWithoutNotify(session.SelectedAttacker + 1);
    }

    private void RefreshBlueprints()
    {
        session.RefreshBlueprints();
        RefreshOptions();
        RefreshSelection();
    }

    public void RefreshSelection()
    {
        if (session.IsEditingBlueprint || session.Match == null || !session.IsRunning)
        {
            Cancel();
            return;
        }

        bool placing = dock >= 0 && session.Match.CanRefit;
        SiegeFleetEntry ship = session.Fleet.Find(shipId);
        card.SetActive(placing || ship != null);
        defenderBlueprint.gameObject.SetActive(placing);
        confirm.gameObject.SetActive(placing);
        pilot.gameObject.SetActive(ship != null);
        repair.gameObject.SetActive(ship != null && session.Match.CanRefit);
        ClearPreview();
        if (placing)
        {
            title.text = "DEPLOY AT PAD " + (dock + 1);
            int index = session.SelectedDefender;
            if (index < 0 || index >= session.DefenderOptions.Length)
            {
                details.text = "No valid Saves blueprint. Open Blueprints or refresh in F3.";
                confirm.interactable = false;
                return;
            }

            SiegeShipOption option = session.DefenderOptions[index];
            BlockDataList data = SiegeFleet.PrepareBlueprint(option.BlueprintJson, out string id);
            Transform point = session.Fleet.GetDeploymentPoint(dock);
            preview = BlueprintModelPreview.Create(data.blocks, null, "Siege deployment preview");
            preview.transform.SetPositionAndRotation(point.position, point.rotation);
            bool clear = SiegeRefit.IsClear(data, point.position, point.rotation, id, out string reason);
            bool empty = session.Fleet.Ships[dock]?.IsAlive != true;
            confirm.interactable = empty && clear && session.Match.CanAfford(option.Price);
            details.text = !empty ? "This deployment position is occupied." : !clear ? reason : !session.Match.CanAfford(option.Price) ? "Insufficient credits: " + option.Price : option.BlockCount + " modules / " + option.Price.ToString("N0") + " credits. Confirm to deploy.";
        }
        else if (ship != null)
        {
            title.text = ship.DisplayName;
            pilot.interactable = ship != session.Fleet.Driven;
            long quote = session.Match.CanRefit ? SiegeRefit.Quote(ship, session.Settings) : 0;
            repair.interactable = quote > 0 && session.Match.CanAfford(quote);
            details.text = ship == session.Fleet.Driven ? "PILOT" : "AUTONOMOUS SUPPORT";
            if (session.Match.CanRefit)
            {
                details.text += quote > 0 ? " / REPAIR " + quote.ToString("N0") : " / INTACT";
            }
        }
    }

    public void Confirm()
    {
        if (dock >= 0 && session.TryDeployAt(session.SelectedDefender, dock, out _))
        {
            Cancel();
        }
        else
        {
            RefreshSelection();
        }
    }

    private void Pilot()
    {
        session.TryTakeControl(shipId);
        Cancel();
    }

    private void Repair()
    {
        session.TryRepairShip(shipId);
    }

    public void CycleShip()
    {
        SiegeFleetEntry[] ships = session.Fleet.Ships;
        int start = Array.IndexOf(ships, session.Fleet.Driven);
        for (int step = 1; step <= ships.Length; step++)
        {
            SiegeFleetEntry next = ships[(start + step + ships.Length) % ships.Length];
            if (next?.IsAlive == true)
            {
                session.TryTakeControl(next.Id);
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
