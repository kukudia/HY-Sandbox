using UnityEngine;

/// <summary>Plays authored particle assets without rebuilding their modules at runtime.</summary>
[DisallowMultipleComponent]
public sealed class AssetParticleEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particles = System.Array.Empty<ParticleSystem>();
    [SerializeField] private Light[] _lights = System.Array.Empty<Light>();
    [SerializeField, Min(0.1f)] private float _releaseAfter = 6f;
    private float[] _rates;
    private float[] _intensities;
    private bool _oneShot;
    private static int _activeBursts;

    private void Awake() => Cache();

    private void Cache()
    {
        if (_rates != null) return;
        _rates = new float[_particles.Length];
        for (int i = 0; i < _particles.Length; i++)
            if (_particles[i] != null) _rates[i] = _particles[i].emission.rateOverTimeMultiplier;
        _intensities = new float[_lights.Length];
        for (int i = 0; i < _lights.Length; i++)
            if (_lights[i] != null) _intensities[i] = _lights[i].intensity;
    }

    public void SetIntensity(float value)
    {
        Cache();
        value = Mathf.Clamp01(value);
        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem particles = _particles[i];
            if (particles == null) continue;
            var emission = particles.emission;
            emission.rateOverTimeMultiplier = _rates[i] * value;
            if (value > 0.01f && !particles.isPlaying) particles.Play(false);
            else if (value <= 0.01f && particles.isPlaying)
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
        foreach (ParticleSystem particles in _particles)
            if (particles != null) particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        if (_oneShot) _activeBursts = Mathf.Max(0, _activeBursts - 1);
    }
}
