// 职责：瞬时激光命中与短时光束表现；不对目标施加冲量。
// 入口：Turret 固定步调用 Fire()，LateUpdate() 只更新画面。
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
    [SerializeField, Min(0.01f), Tooltip("激光命中光斑直径，米。")]
    private float contactDiameter = 0.26f;
    private const float LostSurfaceFade = 0.045f;
    private const float SurfaceBias = 0.018f;
    private readonly TurretHitscan _hitscan = new TurretHitscan();
    private StylizedBeamEffect _fireBeam;
    private VfxEffect _contact;
    private LaserShotEndpoint _endpoint;
    private Vector3 _beamEnd;
    private Vector3 _contactNormal;
    private float _shotTime;
    private float _hideLineTime;
    private float _surfaceLostAt = -1f;
    private bool _shotVisible;
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

    protected override bool Fire(Vector3 origin, Vector3 direction, UnitFaction faction)
    {
        _endpoint = _hitscan.Fire(
            origin,
            direction,
            Owner,
            faction,
            range,
            hitLayers,
            damage * Supply.efficiency);
        _beamEnd = _endpoint.Point;
        _contactNormal = _endpoint.Normal;
        _shotTime = Time.time;
        _hideLineTime = _shotTime + beamDuration;
        _surfaceLostAt = -1f;
        _shotVisible = true;
        _muzzleFlash?.PlayOnce();
        _fireBeam.SetEndpoints(origin, _beamEnd);
        _fireBeam.SetTipFade(!_endpoint.IsHit);
        _fireBeam.SetIntensity(1f);
        _fireBeam.SetVisible(true);
        if (_endpoint.IsHit)
        {
            PlayContact();
        }
        else
        {
            _contact?.Clear();
        }

        return true;
    }

    private void PlayContact()
    {
        if (_contact == null)
        {
            VfxEffect template = BlockVfxLibrary.Instance?.Resolve("LaserContact");
            if (template == null)
            {
                return;
            }

            // Keep world scale independent of both the victim and the potentially scaled turret.
            _contact = Instantiate(template);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_contact.gameObject, gameObject.scene);
        }

        _contact.transform.localScale = Vector3.one * contactDiameter;
        UpdateContactPose();
        _contact.PlayOnce();
    }

    private void UpdateContactPose()
    {
        if (_contact != null)
        {
            _contact.transform.SetPositionAndRotation(_beamEnd + _contactNormal * SurfaceBias, Quaternion.LookRotation(_contactNormal));
        }
    }

    protected override void HideShotFeedback()
    {
        _shotVisible = false;
        _endpoint = default;
        _fireBeam?.SetVisible(false);
        if (_contact != null && _contact.IsEmitting)
        {
            _contact.Clear();
        }

        _muzzleFlash?.SetIntensity(0f);
    }

    private void LateUpdate()
    {
        if (!_shotVisible)
        {
            return;
        }

        float now = Time.time;
        if (now >= _hideLineTime)
        {
            HideShotFeedback();
            return;
        }

        if (_endpoint.IsHit && _surfaceLostAt < 0f)
        {
            if (_endpoint.TryGetSurfacePose(out Vector3 point, out Vector3 normal))
            {
                _beamEnd = point;
                _contactNormal = normal;
                UpdateContactPose();
            }
            else
            {
                // A destroyed surface keeps its last valid pose only for the remainder of the fade.
                _surfaceLostAt = now;
                _endpoint = default;
            }
        }

        float lossFade = _surfaceLostAt < 0f ? 1f : Mathf.Clamp01(1f - (now - _surfaceLostAt) / LostSurfaceFade);
        if (lossFade <= 0f)
        {
            HideShotFeedback();
            return;
        }

        float remaining = Mathf.InverseLerp(_hideLineTime, _shotTime, now);
        _fireBeam.SetEndpoints(GetMuzzlePosition(), _beamEnd);
        _fireBeam.SetIntensity(Mathf.SmoothStep(0f, 1f, remaining * 1.4f) * lossFade);
        if (_surfaceLostAt >= 0f)
        {
            _contact?.SetIntensity(lossFade);
        }
    }

    private void OnDestroy()
    {
        if (_contact != null)
        {
            Destroy(_contact.gameObject);
        }
    }
}
