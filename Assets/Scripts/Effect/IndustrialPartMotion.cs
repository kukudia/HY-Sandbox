using System;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class IndustrialPartMotion : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private Transform[] spinTargets = Array.Empty<Transform>();
    [SerializeField] private Vector3 spinAxis = Vector3.up;
    [SerializeField] private float degreesPerSecond = 24f;
    [SerializeField] private Transform bobTarget;
    [SerializeField] private float bobAmplitude = 0.025f;
    [SerializeField] private float bobFrequency = 1.2f;
    [SerializeField] private Renderer[] glowRenderers = Array.Empty<Renderer>();
    [SerializeField] private Color baseEmission = new Color(0.05f, 1.4f, 2.2f, 1f);
    [SerializeField] private float emissionPulse = 0.18f;

    private MaterialPropertyBlock _propertyBlock;
    private Vector3 _bobOrigin;

    private void Awake()
    {
        CacheRuntimeState();
    }

    private void OnEnable()
    {
        CacheRuntimeState();
    }

    private void Update()
    {
        float deltaDegrees = degreesPerSecond * Time.deltaTime;
        for (int i = 0; i < spinTargets.Length; i++)
        {
            Transform target = spinTargets[i];
            if (target != null)
            {
                target.Rotate(spinAxis, deltaDegrees, Space.Self);
            }
        }

        float wave = Mathf.Sin(Time.unscaledTime * bobFrequency * Mathf.PI * 2f);
        if (bobTarget != null)
        {
            bobTarget.localPosition = _bobOrigin + Vector3.up * (wave * bobAmplitude);
        }

        if (glowRenderers.Length == 0 || emissionPulse <= 0f)
        {
            return;
        }

        float intensity = 1f + wave * emissionPulse;
        Color emission = baseEmission * Mathf.Max(0f, intensity);
        for (int i = 0; i < glowRenderers.Length; i++)
        {
            Renderer target = glowRenderers[i];
            if (target == null)
            {
                continue;
            }

            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorId, emission);
            target.SetPropertyBlock(_propertyBlock);
        }
    }

    public void Configure(
        Transform[] newSpinTargets,
        Vector3 newSpinAxis,
        float newDegreesPerSecond,
        Transform newBobTarget,
        float newBobAmplitude,
        float newBobFrequency,
        Renderer[] newGlowRenderers,
        Color newBaseEmission,
        float newEmissionPulse)
    {
        spinTargets = newSpinTargets ?? Array.Empty<Transform>();
        spinAxis = newSpinAxis.sqrMagnitude > 0.0001f ? newSpinAxis.normalized : Vector3.up;
        degreesPerSecond = newDegreesPerSecond;
        bobTarget = newBobTarget;
        bobAmplitude = Mathf.Max(0f, newBobAmplitude);
        bobFrequency = Mathf.Max(0f, newBobFrequency);
        glowRenderers = newGlowRenderers ?? Array.Empty<Renderer>();
        baseEmission = newBaseEmission;
        emissionPulse = Mathf.Max(0f, newEmissionPulse);
        CacheRuntimeState();
    }

    private void CacheRuntimeState()
    {
        _propertyBlock ??= new MaterialPropertyBlock();
        if (bobTarget != null)
        {
            _bobOrigin = bobTarget.localPosition;
        }
    }
}
