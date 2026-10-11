// 职责：维护掉落物品身份、携带/认领、吸附入库，以及散落悬浮和文字遮挡表现。
// 入口：Spawn() / Initialize() / Claim()。
using System.Collections.Generic;
using UnityEngine;

public class LootDrop : MonoBehaviour
{
    private static readonly HashSet<LootDrop> Drops = new HashSet<LootDrop>();
    [SerializeField]
    private CargoItem _item = new CargoItem();
    [SerializeField]
    private Transform _displayRoot;
    [SerializeField]
    private Renderer _beacon;
    [SerializeField]
    private VfxEffect _burst;
    [SerializeField]
    private TextMesh _label;
    [SerializeField, Min(0.1f)]
    private float _attractionSpeed = 12f;
    [SerializeField, Min(1f)]
    private float _attractionRange = 100f;
    [SerializeField, Min(0.1f)]
    private float _displaySize = 0.8f;
    [SerializeField]
    private bool _showDropState = true;
    [SerializeField, Min(0f), Tooltip("Initial scatter speed in world units per second.")]
    private float _scatterSpeed = 2.5f;
    [SerializeField, Min(0.01f)]
    private float _scatterDuration = 0.6f;
    [SerializeField, Min(0f)]
    private float _hoverAmplitude = 0.12f;
    [SerializeField, Min(0f)]
    private float _hoverFrequency = 0.8f;
    [SerializeField]
    private float _spinSpeed = 65f;
    private readonly RaycastHit[] _occlusionHits = new RaycastHit[64];
    private Vector3 _scatterVelocity;
    private float _scatterRemaining;
    private float _visualPhase;
    private Vector3 _displayPosition;
    private Quaternion _displayRotation;
    private Vector3 _beaconPosition;
    private Quaternion _beaconRotation;
    private float _nextSearch;
    private bool _consumed;
    private bool _initialized;
    public CargoItem Item => _item;
    public CollectionBot ClaimedBy
    {
        get; private set;
    }
    public CargoHold Destination
    {
        get; private set;
    }
    public bool IsCarried
    {
        get; private set;
    }
    public bool Available => !_consumed && !IsCarried && ClaimedBy == null;
    public static IEnumerable<LootDrop> Active => Drops;

    private void OnEnable()
    {
        Drops.Add(this);
    }

    private void OnDisable()
    {
        Drops.Remove(this);
        Release();
    }

    private void Start()
    {
        if (!_initialized)
        {
            Initialize(_item);
        }
    }

    public static LootDrop Spawn(CargoItem item, Vector3 position)
    {
        if (item == null || !item.IsValid)
        {
            return null;
        }

        if (item.kind != CargoKind.SpecialPart)
        {
            int currencyCount = 0;
            LootDrop merge = null;
            float nearest = float.PositiveInfinity;
            foreach (LootDrop candidate in Drops)
            {
                if (candidate == null || candidate._consumed || candidate.Item.kind == CargoKind.SpecialPart)
                {
                    continue;
                }

                currencyCount++;
                float distance = (candidate.transform.position - position).sqrMagnitude;
                if (candidate.Item.kind == item.kind && distance < nearest && candidate.Item.amount <= int.MaxValue - item.amount)
                {
                    merge = candidate;
                    nearest = distance;
                }
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
        if (prefab == null)
        {
            Debug.LogError("Missing Resources/Salvage/LootDrop prefab. Run Tools/Salvage/Bake assets.");
            return null;
        }

        LootDrop drop = Instantiate(prefab, position, Quaternion.identity);
        drop.Initialize(item);
        return drop;
    }

    public void Initialize(CargoItem item)
    {
        _item = item.Copy();
        _initialized = true;
        _visualPhase = Random.value * Mathf.PI * 2f;
        Vector2 scatter = Random.insideUnitCircle;
        _scatterVelocity = new Vector3(scatter.x, Random.Range(0.2f, 0.5f), scatter.y) * _scatterSpeed;
        _scatterRemaining = Mathf.Max(0.01f, _scatterDuration);
        if (_displayRoot != null)
        {
            _displayPosition = _displayRoot.localPosition;
            _displayRotation = _displayRoot.localRotation;
        }

        if (_beacon != null)
        {
            _beaconPosition = _beacon.transform.localPosition;
            _beaconRotation = _beacon.transform.localRotation;
        }

        Color color = item.kind == CargoKind.Coins ? new Color(1f, 0.65f, 0.05f) : item.kind == CargoKind.Technology ? new Color(0.1f, 0.5f, 1f) : new Color(0.15f, 1f, 0.7f);
        if (_beacon != null)
        {
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_EmissionColor", color * 2f);
            _beacon.SetPropertyBlock(properties);
            _beacon.gameObject.SetActive(_showDropState);
        }

        if (_displayRoot != null && item.kind == CargoKind.SpecialPart)
        {
            CargoVisual.Create(item.resourcePath, _displayRoot, _displaySize);
        }

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
        if (_label == null)
        {
            return;
        }

        _label.gameObject.SetActive(_showDropState);
        _label.text = _item.kind == CargoKind.SpecialPart ? System.IO.Path.GetFileNameWithoutExtension(_item.resourcePath) : _item.kind + " +" + _item.amount;
    }

    public bool Claim(CollectionBot bot, CargoHold destination)
    {
        if (!Available || bot == null || destination == null || !destination.Reserve(this))
        {
            return false;
        }

        ClaimedBy = bot;
        Destination = destination;
        return true;
    }

    public void Carry(Transform socket)
    {
        if (ClaimedBy == null || socket == null || _consumed)
        {
            return;
        }

        ResetDisplayPose();
        _scatterRemaining = 0f;
        IsCarried = true;
        transform.SetParent(socket, true);
        transform.position = socket.position + socket.rotation * (Vector3.down * 0.65f);
    }

    public void Release()
    {
        if (Destination != null)
        {
            Destination.ReleaseReservation(this);
        }

        ClaimedBy = null;
        Destination = null;
        if (IsCarried)
        {
            transform.SetParent(null, true);
        }

        IsCarried = false;
    }

    public bool Deliver(CargoHold hold)
    {
        if (_consumed || hold == null)
        {
            return false;
        }

        int stored = hold.Store(_item, this);
        if (stored == 0)
        {
            return false;
        }

        _item.amount -= stored;
        if (_item.amount <= 0)
        {
            _consumed = true;
            Release();
            Drops.Remove(this);
            Destroy(gameObject);
        }
        else
        {
            Release();
            RefreshLabel();
        }

        return true;
    }

    private void Update()
    {
        if (_consumed || PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            return;
        }

        if (!IsCarried && ClaimedBy == null && Destination == null && _scatterRemaining > 0f)
        {
            float step = Mathf.Min(Time.deltaTime, _scatterRemaining);
            Vector3 movement = _scatterVelocity * (_scatterRemaining / Mathf.Max(0.01f, _scatterDuration)) * step;
            // 散落只持续短时间；路径遇到实体障碍就停止，避免穿进墙内。
            if (movement.sqrMagnitude > 0f && !Physics.SphereCast(
                transform.position,
                0.15f,
                movement.normalized,
                out _,
                movement.magnitude,
                ~((1 << 2) | (1 << 5)),
                QueryTriggerInteraction.Ignore))
            {
                transform.position += movement;
            }

            _scatterRemaining -= step;
        }

        if (IsCarried || ClaimedBy != null || _item.kind == CargoKind.SpecialPart)
        {
            return;
        }

        if (Destination != null && (!Destination.CanReceive(_item, this) || (Destination.transform.position - transform.position).sqrMagnitude > _attractionRange * _attractionRange))
        {
            Release();
        }

        if (Destination == null && Time.time >= _nextSearch)
        {
            _nextSearch = Time.time + 0.5f;
            CargoHold receiver = CargoHold.FindReceiver(_item, transform.position);
            if (receiver != null && (receiver.transform.position - transform.position).sqrMagnitude <= _attractionRange * _attractionRange)
            {
                Destination = receiver;
            }
        }

        if (Destination == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, Destination.transform.position, _attractionSpeed * Time.deltaTime);
        if ((transform.position - Destination.transform.position).sqrMagnitude < 0.09f)
        {
            Deliver(Destination);
        }
    }

    private void LateUpdate()
    {
        if (!_initialized || _consumed)
        {
            return;
        }

        bool playing = PlayManager.instance != null && PlayManager.instance.playMode;
        Camera camera = PlayManager.instance != null && PlayManager.instance.mainCamera != null ? PlayManager.instance.mainCamera : Camera.main;
        if (_label != null)
        {
            bool visible = false;
            if (playing && _showDropState && camera != null)
            {
                _label.transform.rotation = camera.transform.rotation;
                Vector3 screen = camera.WorldToScreenPoint(transform.position);
                visible = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height && !CombatHud.IsWorldPointOccluded(camera, transform.position, transform.root, _occlusionHits);
            }

            _label.gameObject.SetActive(visible);
        }

        if (!playing || IsCarried)
        {
            ResetDisplayPose();
            return;
        }

        // 只动画显示子节点，拾取/认领使用的根位置不会随悬浮来回跳动。
        Vector3 hover = Vector3.up * (Mathf.Sin(Time.time * _hoverFrequency * Mathf.PI * 2f + _visualPhase) * _hoverAmplitude);
        Quaternion spin = Quaternion.Euler(0f, Time.time * _spinSpeed, 0f);
        if (_displayRoot != null)
        {
            _displayRoot.localPosition = _displayPosition + hover;
            _displayRoot.localRotation = _displayRotation * spin;
        }

        if (_beacon != null)
        {
            _beacon.transform.localPosition = _beaconPosition + hover;
            _beacon.transform.localRotation = _beaconRotation * spin;
        }
    }

    private void ResetDisplayPose()
    {
        if (_displayRoot != null)
        {
            _displayRoot.localPosition = _displayPosition;
            _displayRoot.localRotation = _displayRotation;
        }

        if (_beacon != null)
        {
            _beacon.transform.localPosition = _beaconPosition;
            _beacon.transform.localRotation = _beaconRotation;
        }
    }

    public static void ClearSession()
    {
        foreach (LootDrop drop in new List<LootDrop>(Drops))
        {
            if (drop != null)
            {
                drop.Release();
                Destroy(drop.gameObject);
            }
        }

        Drops.Clear();
    }
}
