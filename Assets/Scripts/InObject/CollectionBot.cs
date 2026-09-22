using UnityEngine;

public class CollectionBot : Bot
{
    [SerializeField] private bool _requiresPower = true;
    [SerializeField, Min(0.1f)] private float _pickupDistance = 1.2f;
    [SerializeField, Min(1f)] private float _missionTimeout = 45f;
    [SerializeField] private Transform _carrySocket;
    private LootDrop _target;
    private CargoHold _destination;
    private float _missionStarted;
    private float _nextScan;
    public bool HasCargo => _target != null && _target.IsCarried;

    protected override void Start() { base.Start(); if (_carrySocket == null) _carrySocket = transform; }

    private void FixedUpdate()
    {
        if (!TickNavigation()) return;
        ControlUnit owner = home.GetComponentInParent<ControlUnit>();
        Power power = home.GetComponentInParent<Power>();
        bool canWork = owner != null && owner.HasValidCockpit && (!_requiresPower || power != null && power.isWorking);
        if (_target != null && (_target.ClaimedBy != this || Time.time - _missionStarted > _missionTimeout
            || _destination == null || _destination.Owner != owner || !_destination.CanReceive(_target.Item, _target) || !canWork))
            Abandon();
        if (_target == null && canWork && Time.time >= _nextScan)
        {
            _nextScan = Time.time + Mathf.Max(0.25f, findTargetInterval);
            FindTarget(owner);
        }
        if (_target == null) { NavigateHomeSmoothly(); return; }
        if (!_target.IsCarried)
        {
            NavigateToTarget(_target.transform);
            if (transform.parent != home && (transform.position - _target.transform.position).sqrMagnitude <= _pickupDistance * _pickupDistance)
                _target.Carry(_carrySocket);
        }
        else
        {
            // Return to the authored bay; deliver only after docking, never at pickup time.
            NavigateHomeSmoothly();
            if (transform.parent == home)
            {
                _target.Deliver(_destination);
                Abandon();
                _nextScan = Time.time + Mathf.Max(0.25f, findTargetInterval);
            }
        }
    }

    private void FindTarget(ControlUnit owner)
    {
        LootDrop candidate = null;
        CargoHold receiver = null;
        float best = targetRange * targetRange;
        foreach (LootDrop drop in LootDrop.Active)
        {
            if (drop == null || !drop.Available || drop.Item.kind != CargoKind.SpecialPart) continue;
            float distance = (drop.transform.position - home.position).sqrMagnitude;
            if (distance >= best) continue;
            CargoHold hold = CargoHold.FindReceiver(drop.Item, home.position, owner);
            if (hold == null) continue;
            candidate = drop; receiver = hold; best = distance;
        }
        if (candidate != null && candidate.Claim(this, receiver))
        {
            _target = candidate; _destination = receiver; _missionStarted = Time.time;
        }
    }

    private void Abandon()
    {
        if (_target != null && _target.ClaimedBy == this) _target.Release();
        _target = null; _destination = null;
    }

    protected override void OnDisable() { Abandon(); base.OnDisable(); }
    protected override void OnDestroy() { Abandon(); base.OnDestroy(); }
    public override void PrepareForHomeDestruction() { Abandon(); }
}
