// 职责：每轮从每个炮管分别发射一个真实刚体炮弹；炮弹负责飞行和命中。
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
    public Transform[] Muzzles => _muzzles;
    public TurretProjectile ProjectilePrefab => _projectilePrefab;

    protected override void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)
    {
        if (_projectilePrefab == null)
        {
            return;
        }

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
        }
    }
}
