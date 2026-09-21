using UnityEngine;

/// <summary>Plays authored particle assets without rebuilding their modules at runtime.</summary>
[DisallowMultipleComponent]
public sealed class AssetParticleEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particles = System.Array.Empty<ParticleSystem>();
    [SerializeField] private Light[] _lights = System.Array.Empty<Light>();
    [SerializeField, Min(0.1f)] private float _releaseAfter = 6f;
    [Tooltip("Keep a continuous plume/contact at low intensity; fade its material instead of thinning emission.")]
    [SerializeField] private bool _continuousEmission;
    private float[] _rates;
    private float[] _intensities;
    private ParticleSystemRenderer[] _renderers;
    private MaterialPropertyBlock[] _properties;
    private Color[] _baseColors;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private bool _oneShot;
    private static int _activeBursts;

    private void Awake() => Cache();

    private void Cache()
    {
        if (_rates != null) return;
        _rates = new float[_particles.Length];
        _renderers = new ParticleSystemRenderer[_particles.Length];
        _properties = new MaterialPropertyBlock[_particles.Length];
        _baseColors = new Color[_particles.Length];
        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i] == null) continue;
            _rates[i] = _particles[i].emission.rateOverTimeMultiplier;
            if (!_continuousEmission) continue;
            var renderer = _particles[i].GetComponent<ParticleSystemRenderer>();
            if (renderer == null || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty(BaseColor)) continue;
            _renderers[i] = renderer;
            _baseColors[i] = renderer.sharedMaterial.GetColor(BaseColor);
            _properties[i] = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_properties[i]);
        }
        _intensities = new float[_lights.Length];
        for (int i = 0; i < _lights.Length; i++)
            if (_lights[i] != null) _intensities[i] = _lights[i].intensity;
    }

    public void SetIntensity(float value)
    {
        Cache();
        value = Mathf.Clamp01(value);
        bool emitting = value > 0f;
        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem particles = _particles[i];
            if (particles == null) continue;
            var emission = particles.emission;
            // Birth-rate modulation leaves short-lived cores empty at low throttle. Fade all
            // live particles together through a property block, without cloning shared materials.
            emission.rateOverTimeMultiplier = _rates[i] * (_continuousEmission && emitting ? 1f : value);
            if (_continuousEmission && _renderers[i] != null)
            {
                Color color = _baseColors[i];
                color.a *= value;
                _properties[i].SetColor(BaseColor, color);
                _renderers[i].SetPropertyBlock(_properties[i]);
            }
            if (emitting && !particles.isEmitting) particles.Play(false);
            else if (!emitting && particles.isEmitting)
                particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        for (int i = 0; i < _lights.Length; i++)
            if (_lights[i] != null) _lights[i].intensity = _intensities[i] * value;
    }

    public void PlayOnce()
    {
        Cache();
        foreach (ParticleSystem particles in _particles)
            if (particles != null)
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Play(false);
            }
    }

    public static bool CanSpawnBurst => _activeBursts < 64;

    public void ReleaseAfterPlayback()
    {
        if (_oneShot) return;
        _oneShot = true;
        _activeBursts++;
        Destroy(gameObject, _releaseAfter);
    }

    private void OnDisable()
    {
        SetIntensity(0f);
        foreach (ParticleSystem particles in _particles)
            if (particles != null) particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        if (_oneShot) _activeBursts = Mathf.Max(0, _activeBursts - 1);
    }
}
