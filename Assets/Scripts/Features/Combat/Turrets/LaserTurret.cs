// 职责：瞬时激光命中与短时光束表现；不对目标施加冲量。
// 入口：Turret 固定步调用 Fire()；公用配置通过基类保持原序列化字段。
using UnityEngine;

[RequireComponent(typeof(StylizedBeamEffect))]
public sealed class LaserTurret : Turret
{
    [SerializeField]
    private VfxEffect _muzzleFlash;
    [SerializeField, Min(0.005f), Tooltip("激光中心束宽度，米；外层辉光由库模板控制。")]
    private float beamWidth = 0.16f;
    [SerializeField, Min(0.02f), Tooltip("单次激光可见时间，秒；不改变开火频率。")]
    private float beamDuration = 0.2f;
    [SerializeField, ColorUsage(false, true)]
    private Color beamColor = new Color(6f, 1.1f, 0.15f);
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
        _fireBeam.Configure(beamWidth, "LaserBeam");
        _fireBeam.SetColor(beamColor);
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
        _hideLineTime = Time.time + beamDuration;
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
            float remaining = Mathf.Clamp01((_hideLineTime - Time.time) / Mathf.Max(0.02f, beamDuration));
            _fireBeam.SetIntensity(Mathf.SmoothStep(0f, 1f, remaining * 1.4f));
        }
        else
        {
            _fireBeam?.SetVisible(false);
        }
    }
}
