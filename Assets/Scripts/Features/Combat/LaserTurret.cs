// 职责：瞬时激光命中与短时光束表现；不对目标施加冲量。
// 入口：Turret 固定步调用 Fire()；公用配置通过基类保持原序列化字段。
using UnityEngine;

public sealed class LaserTurret : Turret
{
    [SerializeField]
    private VfxEffect _muzzleFlash;
    private readonly TurretHitscan _hitscan = new TurretHitscan();
    private StylizedBeamEffect _fireBeam;
    private Vector3 _beamEnd;
    private float _hideLineTime;
    protected override void Awake()
    {
        base.Awake();
        LineRenderer legacyLine = GetComponent<LineRenderer>();
        if (legacyLine != null)
        {
            legacyLine.enabled = false;
        }

        _fireBeam = GetComponent<StylizedBeamEffect>();
        if (_fireBeam == null)
        {
            _fireBeam = gameObject.AddComponent<StylizedBeamEffect>();
        }

        _fireBeam.Configure(0.035f);
        _fireBeam.SetColor(new Color(1f, 0.46f, 0.08f, 1f));
        _fireBeam.SetVisible(false);
    }

    protected override void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)
    {
        _beamEnd = _hitscan.Fire(
            origin,
            direction,
            Owner,
            faction,
            range,
            hitLayers,
            damage * Supply.efficiency);
        _muzzleFlash?.PlayOnce();
        _fireBeam.SetEndpoints(origin, _beamEnd);
        _fireBeam.SetIntensity(1f);
        _fireBeam.SetVisible(true);
        _hideLineTime = Time.time + 0.085f;
    }

    protected override void HideShotFeedback()
    {
        _fireBeam?.SetVisible(false);
        _muzzleFlash?.SetIntensity(0f);
    }

    protected override void UpdateShotFeedback()
    {
        if (_fireBeam != null && Time.time < _hideLineTime)
        {
            _fireBeam.SetEndpoints(GetMuzzlePosition(), _beamEnd);
            _fireBeam.SetIntensity(Mathf.Clamp01((_hideLineTime - Time.time) / 0.085f));
        }
        else
        {
            _fireBeam?.SetVisible(false);
        }
    }
}
