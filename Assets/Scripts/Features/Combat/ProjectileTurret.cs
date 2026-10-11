// 职责：每炮管发射真实炮弹并播放后退/复位动画；炮弹负责飞行和命中。
// 入口：Turret.Fire()；炮管、速度、寿命和冲量可在 Prefab Inspector 配置。
using UnityEngine;

public sealed class ProjectileTurret : Turret
{
    [SerializeField]
    private Transform[] _muzzles = System.Array.Empty<Transform>();
    [SerializeField]
    private TurretProjectile _projectilePrefab;
    [SerializeField, Min(1f), Tooltip("炮弹相对炮口速度，米/秒。")]
    private float _projectileSpeed = 85f;
    [SerializeField, Min(0.05f)]
    private float _projectileLifetime = 4f;
    [SerializeField, Min(0f), Tooltip("单发命中冲量，牛顿秒。")]
    private float _impactImpulse = 45f;
    [Header("Barrel Recoil")]
    [SerializeField, Tooltip("只移动炮管模型，炮口随炮管移动；不移动炮架或方块碰撞体。")]
    private Transform[] _recoilBarrels = System.Array.Empty<Transform>();
    [SerializeField, Min(0f), Tooltip("炮管沿发射方向后退的世界距离，米。")]
    private float _recoilDistance = 0.12f;
    [SerializeField, Min(0.001f)]
    private float _recoilKickDuration = 0.04f;
    [SerializeField, Min(0.001f)]
    private float _recoilReturnDuration = 0.22f;
    private Vector3[] _barrelRestPositions;
    private float _recoilElapsed;
    private float _recoilStartOffset;
    private float _recoilOffset;
    private bool _recoiling;
    public Transform[] Muzzles => _muzzles;
    public TurretProjectile ProjectilePrefab => _projectilePrefab;

    protected override void Awake()
    {
        base.Awake();
        // 旧运行副本未带新字段时，现有炮口的父节点就是炮管模型。
        if (_recoilBarrels.Length == 0)
        {
            _recoilBarrels = new Transform[_muzzles.Length];
            for (int i = 0; i < _muzzles.Length; i++)
            {
                _recoilBarrels[i] = _muzzles[i] != null ? _muzzles[i].parent : null;
            }
        }

        _barrelRestPositions = new Vector3[_recoilBarrels.Length];
        for (int i = 0; i < _recoilBarrels.Length; i++)
        {
            if (_recoilBarrels[i] != null)
            {
                _barrelRestPositions[i] = _recoilBarrels[i].localPosition;
            }
        }
    }

    private void LateUpdate()
    {
        // 表现独立于供电/索敌门控，最后一发后失去目标仍完整复位。
        if (_recoiling)
        {
            UpdateRecoil(Time.deltaTime);
        }
    }

    private void UpdateRecoil(float deltaTime)
    {
        _recoilElapsed += Mathf.Max(0f, deltaTime);
        float kick = Mathf.Max(0.001f, _recoilKickDuration);
        float recovery = Mathf.Max(0.001f, _recoilReturnDuration);
        if (_recoilElapsed < kick)
        {
            _recoilOffset = Mathf.Lerp(_recoilStartOffset, _recoilDistance, Mathf.SmoothStep(0f, 1f, _recoilElapsed / kick));
        }
        else
        {
            _recoilOffset = Mathf.Lerp(_recoilDistance, 0f, Mathf.SmoothStep(0f, 1f, (_recoilElapsed - kick) / recovery));
            _recoiling = _recoilElapsed < kick + recovery;
        }

        ApplyRecoilPose();
    }

    private void ApplyRecoilPose()
    {
        if (_barrelRestPositions == null)
        {
            return;
        }

        for (int i = 0; i < _recoilBarrels.Length; i++)
        {
            Transform barrel = _recoilBarrels[i];
            if (barrel == null)
            {
                continue;
            }

            Vector3 worldOffset = -barrel.forward * _recoilOffset;
            Vector3 localOffset = barrel.parent != null ? barrel.parent.InverseTransformVector(worldOffset) : worldOffset;
            barrel.localPosition = _barrelRestPositions[i] + localOffset;
        }
    }

    protected override void OnDisable()
    {
        _recoiling = false;
        _recoilOffset = 0f;
        ApplyRecoilPose();
        base.OnDisable();
    }

    protected override void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)
    {
        if (_projectilePrefab == null)
        {
            return;
        }

        bool fired = false;
        foreach (Transform barrel in _muzzles)
        {
            if (barrel == null)
            {
                continue;
            }

            TurretProjectile shot = Instantiate(_projectilePrefab, barrel.position, barrel.rotation);
            Rigidbody ownerBody = Owner.GetComponent<Rigidbody>();
            Vector3 inheritedVelocity = ownerBody != null ? ownerBody.GetPointVelocity(barrel.position) : Vector3.zero;
            shot.Launch(
                Owner,
                faction,
                damage * Supply.efficiency,
                _impactImpulse * Supply.efficiency,
                barrel.forward * _projectileSpeed + inheritedVelocity,
                _projectileLifetime,
                range,
                hitLayers);
            BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Muzzle, barrel.position, barrel.rotation, 0.35f);
            fired = true;
        }

        if (fired)
        {
            // 先按当前炮口位置生成整轮炮弹，再触发模型位移；连续射击不累加漂移。
            _recoilStartOffset = _recoilOffset;
            _recoilElapsed = 0f;
            _recoiling = true;
        }
    }
}
