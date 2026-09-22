using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Block))]
public class CargoHold : MonoBehaviour
{
    private static readonly HashSet<CargoHold> ActiveHolds = new HashSet<CargoHold>();
    [SerializeField] private CargoKind _kind;
    [SerializeField, Min(1)] private int _capacity = 8;
    [SerializeField] private bool _requiresPower = true;
    [SerializeField] private bool _dropPlayerContents = true;
    [SerializeField] private bool _dropEnemyContents = true;
    [Tooltip("Authored starting contents, also restored from blueprint data.")]
    [SerializeField] private List<CargoItem> _contents = new List<CargoItem>();
    private Power _power;
    private bool _released;
    private readonly HashSet<LootDrop> _reservations = new HashSet<LootDrop>();

    public static IEnumerable<CargoHold> Active => ActiveHolds;
    public CargoKind Kind => _kind;
    public int Capacity => Mathf.Max(1, _capacity);
    public int Used { get { int total = 0; foreach (CargoItem item in _contents) if (item != null) total += item.amount; return total; } }
    public int Free => Mathf.Max(0, Capacity - Used);
    public bool IsOperational => isActiveAndEnabled && !_released && (!_requiresPower || (_power != null && _power.isWorking));
    public IReadOnlyList<CargoItem> Contents => _contents;
    public int Revision { get; private set; }
    public ControlUnit Owner => GetComponentInParent<ControlUnit>();

    private void Awake() { _power = GetComponent<Power>(); RestoreContents(CaptureContents()); }
    private void OnValidate() { _capacity = Mathf.Max(1, _capacity); Revision++; }
    private void OnEnable() { ActiveHolds.Add(this); }
    private void OnDisable() { ActiveHolds.Remove(this); _reservations.Clear(); }

    public bool CanReceive(CargoItem item, LootDrop reservation = null)
    {
        _reservations.RemoveWhere(drop => drop == null || drop.Destination != this);
        int reserved = 0;
        foreach (LootDrop drop in _reservations) if (drop != reservation) reserved += 1;
        return item != null && item.IsValid && item.kind == _kind && IsOperational && Free > reserved;
    }

    public bool Reserve(LootDrop drop)
    {
        if (drop == null || !CanReceive(drop.Item, drop)) return false;
        _reservations.Add(drop);
        return true;
    }

    public void ReleaseReservation(LootDrop drop) { _reservations.Remove(drop); }

    public int Store(CargoItem item, LootDrop reservation = null)
    {
        if (!CanReceive(item, reservation)) return 0;
        int count = Mathf.Min(Free, item.amount);
        if (_kind == CargoKind.SpecialPart)
        {
            for (int i = 0; i < count; i++) _contents.Add(item.Copy(1));
        }
        else if (_contents.Count > 0) _contents[0].amount += count;
        else _contents.Add(item.Copy(count));
        Revision++;
        return count;
    }

    public List<CargoItem> CaptureContents()
    {
        var result = new List<CargoItem>();
        foreach (CargoItem item in _contents) if (item != null && item.IsValid) result.Add(item.Copy());
        return result;
    }

    public void RestoreContents(List<CargoItem> items)
    {
        _contents.Clear();
        _released = false;
        if (items != null) foreach (CargoItem item in items)
        {
            if (item == null || !item.IsValid || item.kind != _kind || Free == 0) continue;
            int count = Mathf.Min(Free, item.amount);
            if (_kind == CargoKind.SpecialPart) for (int i = 0; i < count; i++) _contents.Add(item.Copy(1));
            else if (_contents.Count > 0) _contents[0].amount += count;
            else _contents.Add(item.Copy(count));
        }
        Revision++;
    }

    // Explicit destruction only: scene unload and blueprint reload must never spawn loot.
    public void ReleaseContents(UnitFaction faction)
    {
        if (_released) return;
        _released = true;
        bool drop = faction == UnitFaction.Player ? _dropPlayerContents : _dropEnemyContents;
        if (drop) foreach (CargoItem item in _contents)
            LootDrop.Spawn(item.Copy(), transform.position + Random.insideUnitSphere * 0.65f);
        _contents.Clear();
        _reservations.Clear();
        Revision++;
    }

    public static CargoHold FindReceiver(CargoItem item, Vector3 position, ControlUnit owner = null, LootDrop reservation = null)
    {
        CargoHold closest = null;
        float best = float.PositiveInfinity;
        foreach (CargoHold hold in ActiveHolds)
        {
            ControlUnit unit = hold.Owner;
            if (unit == null || !unit.HasValidCockpit || (owner != null ? unit != owner : !unit.IsPlayer)
                || !hold.CanReceive(item, reservation)) continue;
            float distance = (hold.transform.position - position).sqrMagnitude;
            if (distance < best) { closest = hold; best = distance; }
        }
        return closest;
    }
}
