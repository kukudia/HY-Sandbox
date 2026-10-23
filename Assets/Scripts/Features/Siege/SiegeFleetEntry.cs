// 职责：持有一艘部署舰的蓝图身份与驾驶舱引用；父物理组变化不改变舰艇身份。
// 入口：Unit / IsAlive；仅由SiegeFleet建立，维修按同一部署ID寻找原模块。
using UnityEngine;

public sealed class SiegeFleetEntry
{
    public string Id
    {
        get;
    }
    public string DisplayName
    {
        get;
    }
    public BlockDataList Blueprint
    {
        get;
    }
    public Cockpit Cockpit
    {
        get; private set;
    }
    public int PurchasePrice
    {
        get;
    }
    public Vector3 DockPosition
    {
        get;
    }
    public Quaternion DockRotation
    {
        get;
    }
    public ControlUnit LastBoundUnit
    {
        get; set;
    }
    public bool IsAlive => Cockpit != null && Cockpit.isActiveAndEnabled;
    public ControlUnit Unit => IsAlive ? Cockpit.GetComponentInParent<ControlUnit>() : null;

    public SiegeFleetEntry(
        string id,
        string displayName,
        BlockDataList blueprint,
        Cockpit cockpit,
        int purchasePrice,
        Vector3 dockPosition,
        Quaternion dockRotation)
    {
        Id = id;
        DisplayName = displayName;
        Blueprint = blueprint;
        Cockpit = cockpit;
        PurchasePrice = purchasePrice;
        DockPosition = dockPosition;
        DockRotation = dockRotation;
    }

    public void MarkLost()
    {
        Cockpit = null;
    }
}
