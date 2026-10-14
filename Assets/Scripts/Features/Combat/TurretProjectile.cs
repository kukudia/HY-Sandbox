// 职责：真实刚体炮弹飞行、每步扫掠防穿透、敌对伤害和寿命清理。
// 入口：Launch()；触发器避免物理引擎额外施加冲量，自身构造始终过滤。
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class TurretProjectile : MonoBehaviour
{
    private RaycastHit[] _hits = new RaycastHit[32];
    private Collider[] _overlaps = new Collider[32];
    private readonly List<Collider> _ignoredColliders = new List<Collider>();
    private TurretProjectilePool _pool;
    private TurretProjectile _prefab;
    private Rigidbody _body;
    private SphereCollider _collider;
    private ControlUnit _owner;
    private UnitFaction _faction;
    private float _damage;
    private float _impulse;
    private float _expiresAt;
    private float _remainingDistance;
    private LayerMask _hitLayers;
    private bool _launched;
    private bool _resolved;
    internal void SetPool(TurretProjectilePool pool, TurretProjectile prefab)
    {
        _pool = pool;
        _prefab = prefab;
    }

    private void OnDisable()
    {
        ClearIgnoredCollisions();
        _launched = false;
        _owner = null;
    }

    private void ClearIgnoredCollisions()
    {
        foreach (Collider ownCollider in _ignoredColliders)
        {
            if (ownCollider != null && _collider != null)
            {
                Physics.IgnoreCollision(_collider, ownCollider, false);
            }
        }

        _ignoredColliders.Clear();
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>();
        _body.useGravity = false;
        _body.isKinematic = false;
        _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
        _collider.isTrigger = true;
    }

    /// <summary>初始化一次发射；继承载具点速度，伤害与冲量由炮塔按供电效率计算。</summary>
    public void Launch(
        ControlUnit owner,
        UnitFaction faction,
        float damage,
        float impulse,
        Vector3 velocity,
        float lifetime,
        float maxDistance,
        LayerMask hitLayers)
    {
        ClearIgnoredCollisions();
        _collider.enabled = true;
        _body.angularVelocity = Vector3.zero;
        _owner = owner;
        _faction = faction;
        _damage = Mathf.Max(0f, damage);
        _impulse = Mathf.Max(0f, impulse);
        _expiresAt = Time.time + Mathf.Max(0.05f, lifetime);
        _remainingDistance = Mathf.Max(0.01f, maxDistance);
        _hitLayers = hitLayers;
        _body.linearVelocity = velocity;
        _launched = true;
        _resolved = false;
        if (owner != null)
        {
            owner.GetComponentsInChildren<Collider>(_ignoredColliders);
            foreach (Collider ownCollider in _ignoredColliders)
            {
                Physics.IgnoreCollision(_collider, ownCollider, true);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_launched || _resolved)
        {
            return;
        }

        if (Time.time >= _expiresAt || _remainingDistance <= 0f || PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            Retire();
            return;
        }

        Vector3 origin = _body.position + transform.TransformVector(_collider.center);
        Vector3 step = _body.linearVelocity * Time.fixedDeltaTime;
        float distance = Mathf.Min(step.magnitude, _remainingDistance);
        float radius = _collider.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Max(Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z)));
        if (ResolveOverlaps(origin, radius) || distance <= 0.0001f)
        {
            return;
        }

        Vector3 direction = step.normalized;
        int count = Physics.SphereCastNonAlloc(
            origin,
            radius,
            direction,
            _hits,
            distance,
            _hitLayers,
            QueryTriggerInteraction.Ignore);
        // NonAlloc 满载时结果无序且可能漏掉最近障碍；扩容重查并保留容量供后续帧复用。
        while (count == _hits.Length)
        {
            System.Array.Resize(ref _hits, _hits.Length * 2);
            count = Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                _hits,
                distance,
                _hitLayers,
                QueryTriggerInteraction.Ignore);
        }

        RaycastHit[] hits = _hits;
        int nearest = -1;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!CanHit(hits[i].collider) || hits[i].distance >= nearestDistance)
            {
                continue;
            }

            nearest = i;
            nearestDistance = hits[i].distance;
        }

        if (nearest >= 0)
        {
            ResolveHit(hits[nearest].collider, hits[nearest].point, hits[nearest].normal);
        }

        _remainingDistance -= step.magnitude;
    }

    private bool ResolveOverlaps(Vector3 origin, float radius)
    {
        int count = Physics.OverlapSphereNonAlloc(origin, radius, _overlaps, _hitLayers, QueryTriggerInteraction.Ignore);
        while (count == _overlaps.Length)
        {
            System.Array.Resize(ref _overlaps, _overlaps.Length * 2);
            count = Physics.OverlapSphereNonAlloc(origin, radius, _overlaps, _hitLayers, QueryTriggerInteraction.Ignore);
        }

        Collider[] overlaps = _overlaps;
        for (int i = 0; i < count; i++)
        {
            if (CanHit(overlaps[i]))
            {
                ResolveHit(overlaps[i], overlaps[i].ClosestPoint(origin), -transform.forward);
                return true;
            }
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_launched && !_resolved && CanHit(other))
        {
            Vector3 point = other.ClosestPoint(_body.position);
            ResolveHit(other, point, -transform.forward);
        }
    }

    private bool CanHit(Collider collider)
    {
        return collider != null && !collider.isTrigger && collider != _collider && ((_hitLayers.value & (1 << collider.gameObject.layer)) != 0) && (_owner == null || collider.GetComponentInParent<ControlUnit>() != _owner);
    }

    private void ResolveHit(Collider collider, Vector3 point, Vector3 normal)
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        Vector3 direction = _body.linearVelocity.sqrMagnitude > 0.001f ? _body.linearVelocity.normalized : transform.forward;
        TurretHitResolver.Apply(
            collider,
            point,
            normal,
            direction,
            _owner,
            _faction,
            _damage,
            _impulse);
        Retire();
    }

    private void Retire()
    {
        _resolved = true;
        _collider.enabled = false;
        _body.linearVelocity = Vector3.zero;
        if (_pool != null)
        {
            _pool.Return(this, _prefab);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
