using UnityEngine;

[DisallowMultipleComponent]
public class ThrusterVisualEffect : MonoBehaviour
{
    public Color coolPlumeColor = new Color(0.22f, 0.75f, 1f, 0.9f);
    public Color hotPlumeColor = new Color(1f, 0.5f, 0.12f, 0.95f);
    public float maxEmissionRate = 130f;
    public float maxLightIntensity = 2.8f;
    public float plumeRadius = 0.13f;
    public float plumeLength = 0.62f;
    public float responseSpeed = 8f;

    private Thruster thruster;
    private Transform plumeRoot;
    private ParticleSystem plumeParticles;
    private Light plumeLight;
    private float smoothedThrust;

    public void Initialize(Thruster owner)
    {
        thruster = owner;
        EnsureVfx();
    }

    public void SetThrust(float thrustRatio, Vector3 localThrustDirection)
    {
        EnsureVfx();

        float target = Mathf.Clamp01(thrustRatio);
        smoothedThrust = Mathf.MoveTowards(smoothedThrust, target, responseSpeed * Time.deltaTime);

        Vector3 direction = localThrustDirection.sqrMagnitude > 0.001f
            ? localThrustDirection.normalized
            : Vector3.forward;
        plumeRoot.localRotation = Quaternion.LookRotation(-direction, GetStableUp(direction));

        UpdateParticleModules(smoothedThrust);
        UpdateGlow(smoothedThrust);

        if (smoothedThrust > 0.015f)
        {
            if (!plumeParticles.isPlaying)
            {
                plumeParticles.Play();
            }
        }
        else if (plumeParticles.isPlaying)
        {
            plumeParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void EnsureVfx()
    {
        if (plumeParticles != null) return;

        if (thruster == null)
        {
            thruster = GetComponent<Thruster>();
        }

        plumeRoot = new GameObject("Thruster Plume VFX").transform;
        plumeRoot.SetParent(transform, false);

        plumeParticles = plumeRoot.gameObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = plumeParticles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(coolPlumeColor, hotPlumeColor);
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = plumeParticles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = plumeParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 16f;
        shape.radius = plumeRadius;
        shape.length = plumeLength;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = plumeParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(coolPlumeColor, 0.35f),
                new GradientColorKey(hotPlumeColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.65f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = plumeParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

        ParticleSystemRenderer renderer = plumeParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = VisualEffectsManager.GetSharedParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 2f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject lightObject = new GameObject("Thruster Glow VFX");
        lightObject.transform.SetParent(transform, false);
        plumeLight = lightObject.AddComponent<Light>();
        plumeLight.type = LightType.Point;
        plumeLight.color = coolPlumeColor;
        plumeLight.range = 2.2f;
        plumeLight.intensity = 0f;
        plumeLight.shadows = LightShadows.None;
    }

    private void UpdateParticleModules(float ratio)
    {
        ParticleSystem.MainModule main = plumeParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.09f, 0.16f, ratio),
            Mathf.Lerp(0.18f, 0.38f, ratio)
        );
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(1f, 2.7f, ratio),
            Mathf.Lerp(2.8f, 7.2f, ratio)
        );
        main.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.025f, 0.065f, ratio),
            Mathf.Lerp(0.08f, 0.2f, ratio)
        );
        main.startColor = new ParticleSystem.MinMaxGradient(
            Color.Lerp(coolPlumeColor, Color.white, ratio * 0.35f),
            Color.Lerp(coolPlumeColor, hotPlumeColor, ratio)
        );

        ParticleSystem.EmissionModule emission = plumeParticles.emission;
        emission.rateOverTime = Mathf.Lerp(0f, maxEmissionRate, ratio);

        ParticleSystem.ShapeModule shape = plumeParticles.shape;
        shape.radius = Mathf.Lerp(plumeRadius * 0.45f, plumeRadius, ratio);
        shape.angle = Mathf.Lerp(10f, 24f, ratio);
        shape.length = Mathf.Lerp(plumeLength * 0.4f, plumeLength, ratio);

        plumeRoot.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.28f, ratio);
    }

    private void UpdateGlow(float ratio)
    {
        if (plumeLight == null) return;

        plumeLight.color = Color.Lerp(coolPlumeColor, hotPlumeColor, Mathf.Clamp01(ratio * 0.65f));
        plumeLight.intensity = Mathf.Lerp(0f, maxLightIntensity, ratio);
        plumeLight.range = Mathf.Lerp(0.6f, 3.4f, ratio);
    }

    private static Vector3 GetStableUp(Vector3 direction)
    {
        return Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.92f
            ? Vector3.forward
            : Vector3.up;
    }
}
