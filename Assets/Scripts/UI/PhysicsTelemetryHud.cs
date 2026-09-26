using UnityEngine;
using UnityEngine.UI;

public sealed class PhysicsTelemetryHud : MonoBehaviour
{
    [SerializeField] private Text _motion;
    [SerializeField] private Text _impact;
    [SerializeField] private Image _impactPulse;
    private float _energy;
    private float _impulse;
    private float _peakEnergy;
    private float _impactAt = -100f;
    private float _nextRefresh;

    private void OnEnable()
    {
        _energy = _impulse = _peakEnergy = 0f;
        _impactAt = -100f;
        ImpactPhysics.PlayerImpact += OnImpact;
    }

    private void OnDisable() { ImpactPhysics.PlayerImpact -= OnImpact; }

    private void OnImpact(float energy, float impulse)
    {
        _energy = energy;
        _impulse = impulse;
        _peakEnergy = Mathf.Max(_peakEnergy, energy);
        _impactAt = Time.unscaledTime;
    }

    private void Update()
    {
        if (_impactPulse != null)
        {
            Color color = _impactPulse.color;
            color.a = Mathf.Clamp01(1f - (Time.unscaledTime - _impactAt) / 0.5f);
            _impactPulse.color = color;
        }
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.1f;
        Rigidbody body = PlayManager.instance != null && PlayManager.instance.blocksParent != null
            ? PlayManager.instance.blocksParent.GetComponent<Rigidbody>() : null;
        if (_motion != null) _motion.text = body != null
            ? $"SPEED      {body.linearVelocity.magnitude:0.0} m/s\nMASS       {body.mass:0.0} kg\nKINETIC    {0.0005f * body.mass * body.linearVelocity.sqrMagnitude:0.0} kJ\nROTATION   {body.angularVelocity.magnitude * Mathf.Rad2Deg:0.0} deg/s"
            : "NO ACTIVE CONSTRUCT";
        if (_impact != null) _impact.text = $"LAST IMPACT   {_energy / 1000f:0.0} kJ\nIMPULSE       {_impulse:0.0} N s\nPEAK IMPACT   {_peakEnergy / 1000f:0.0} kJ";
    }
}
