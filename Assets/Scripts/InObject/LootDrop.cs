using System.Collections.Generic;
using UnityEngine;

public class LootDrop : MonoBehaviour
{
    private static readonly HashSet<LootDrop> Drops = new HashSet<LootDrop>();
    [SerializeField] private CargoItem _item = new CargoItem();
    [SerializeField] private Transform _displayRoot;
    [SerializeField] private Renderer _beacon;
    [SerializeField] private VfxEffect _burst;
    [SerializeField] private TextMesh _label;
    [SerializeField, Min(0.1f)] private float _attractionSpeed = 12f;
    [SerializeField, Min(1f)] private float _attractionRange = 100f;
    [SerializeField, Min(0.1f)] private float _displaySize = 0.8f;
    [SerializeField] private bool _showDropState = true;
    private float _nextSearch;
    private bool _consumed;
    private bool _initialized;
    public CargoItem Item => _item;
    public CollectionBot ClaimedBy { get; private set; }
    public CargoHold Destination { get; private set; }
    public bool IsCarried { get; private set; }
    public bool Available => !_consumed && !IsCarried && ClaimedBy == null;
    public static IEnumerable<LootDrop> Active => Drops;

    private void OnEnable() { Drops.Add(this); }
    private void OnDisable() { Drops.Remove(this); Release(); }
    private void Start() { if (!_initialized) Initialize(_item); }

    public static LootDrop Spawn(CargoItem item, Vector3 position)
    {
        if (item == null || !item.IsValid) return null;
        if (item.kind != CargoKind.SpecialPart)
        {
            int currencyCount = 0;
            LootDrop merge = null;
            float nearest = float.PositiveInfinity;
            foreach (LootDrop candidate in Drops)
            {
                if (candidate == null || candidate._consumed || candidate.Item.kind == CargoKind.SpecialPart) continue;
                currencyCount++;
                float distance = (candidate.transform.position - position).sqrMagnitude;
                if (candidate.Item.kind == item.kind && distance < nearest && candidate.Item.amount <= int.MaxValue - item.amount)
                { merge = candidate; nearest = distance; }
            }
            int limit = WreckSalvage.Settings != null ? WreckSalvage.Settings.maxCurrencyPackets : 64;
            if (merge != null && currencyCount >= limit)
            {
                merge._item.amount += item.amount;
                merge.RefreshLabel();
                return merge;
            }
        }
        LootDrop prefab = Resources.Load<LootDrop>("Salvage/LootDrop");
        if (prefab == null) { Debug.LogError("Missing Resources/Salvage/LootDrop prefab. Run Tools/Salvage/Bake assets."); return null; }
        LootDrop drop = Instantiate(prefab, position, Quaternion.identity);
        drop.Initialize(item);
        return drop;
    }

    public void Initialize(CargoItem item)
    {
        _item = item.Copy();
        _initialized = true;
        Color color = item.kind == CargoKind.Coins ? new Color(1f, 0.65f, 0.05f)
            : item.kind == CargoKind.Technology ? new Color(0.1f, 0.5f, 1f) : new Color(0.15f, 1f, 0.7f);
        if (_beacon != null)
        {
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_EmissionColor", color * 2f);
            _beacon.SetPropertyBlock(properties);
            _beacon.gameObject.SetActive(_showDropState);
        }
        if (_displayRoot != null && item.kind == CargoKind.SpecialPart)
            CargoVisual.Create(item.resourcePath, _displayRoot, _displaySize);
        if (_burst != null)
        {
            _burst.SetColor(color);
            _burst.SetSpawnCount(Mathf.Clamp(item.amount, 4, 16));
            _burst.PlayOnce();
        }
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (_label == null) return;
        _label.gameObject.SetActive(_showDropState);
        _label.text = _item.kind == CargoKind.SpecialPart
            ? System.IO.Path.GetFileNameWithoutExtension(_item.resourcePath) : _item.kind + " +" + _item.amount;
    }

    public bool Claim(CollectionBot bot, CargoHold destination)
    {
        if (!Available || bot == null || destination == null || !destination.Reserve(this)) return false;
        ClaimedBy = bot;
        Destination = destination;
        return true;
    }

    public void Carry(Transform socket)
    {
        if (ClaimedBy == null || socket == null || _consumed) return;
        IsCarried = true;
        transform.SetParent(socket, true);
        transform.position = socket.position + socket.rotation * (Vector3.down * 0.65f);
    }

    public void Release()
    {
        if (Destination != null) Destination.ReleaseReservation(this);
        ClaimedBy = null;
        Destination = null;
        if (IsCarried) transform.SetParent(null, true);
        IsCarried = false;
    }

    public bool Deliver(CargoHold hold)
    {
        if (_consumed || hold == null) return false;
        int stored = hold.Store(_item, this);
        if (stored == 0) return false;
        _item.amount -= stored;
        if (_item.amount <= 0)
        {
            _consumed = true;
            Release();
            Drops.Remove(this);
            Destroy(gameObject);
        }
        else { Release(); RefreshLabel(); }
        return true;
    }

    private void Update()
    {
        if (_consumed || PlayManager.instance == null || !PlayManager.instance.playMode) return;
        if (_label != null && Camera.main != null) _label.transform.rotation = Camera.main.transform.rotation;
        if (IsCarried || ClaimedBy != null || _item.kind == CargoKind.SpecialPart) return;
        if (Destination != null && (!Destination.CanReceive(_item, this)
            || (Destination.transform.position - transform.position).sqrMagnitude > _attractionRange * _attractionRange)) Release();
        if (Destination == null && Time.time >= _nextSearch)
        {
            _nextSearch = Time.time + 0.5f;
            CargoHold receiver = CargoHold.FindReceiver(_item, transform.position);
            if (receiver != null && (receiver.transform.position - transform.position).sqrMagnitude <= _attractionRange * _attractionRange)
                Destination = receiver;
        }
        if (Destination == null) return;
        transform.position = Vector3.MoveTowards(transform.position, Destination.transform.position, _attractionSpeed * Time.deltaTime);
        if ((transform.position - Destination.transform.position).sqrMagnitude < 0.09f) Deliver(Destination);
    }

    public static void ClearSession()
    {
        foreach (LootDrop drop in new List<LootDrop>(Drops))
            if (drop != null) { drop.Release(); Destroy(drop.gameObject); }
        Drops.Clear();
    }
}
