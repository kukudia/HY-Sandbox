using UnityEngine;

[DisallowMultipleComponent]
public class ThrusterVisualEffect : MonoBehaviour
{
    [Tooltip("Each effect is baked onto a physical nozzle, with local +Z pointing out of it.")]
    [SerializeField] private AssetParticleEffect[] _nozzles = System.Array.Empty<AssetParticleEffect>();
    [SerializeField, Min(0f)] private float _responseSpeed = 8f;
    private float _target;
    private float _value;
    private Thruster _owner;
    private Power _power;

    public void Initialize(Thruster owner)
    {
        _owner = owner;
        if (_power == null && owner != null) _power = owner.GetComponent<Power>();
    }

    public void SetThrust(float thrustRatio, Vector3 thrustDirection)
    {
        // The authored socket follows the gimbal. Direction guesses break rotated blocks.
        _target = Mathf.Clamp01(thrustRatio);
    }

    private void LateUpdate()
    {
        if (_owner == null || !_owner.isActiveAndEnabled || _power == null || !_power.isWorking
            || PlayManager.instance == null || !PlayManager.instance.playMode) _target = 0f;
        _value = Mathf.MoveTowards(_value, _target, _responseSpeed * Time.deltaTime);
        foreach (AssetParticleEffect nozzle in _nozzles)
            if (nozzle != null) nozzle.SetIntensity(_value);
    }

    private void OnDisable()
    {
        _target = _value = 0f;
        foreach (AssetParticleEffect nozzle in _nozzles)
            if (nozzle != null) nozzle.SetIntensity(0f);
    }
}
