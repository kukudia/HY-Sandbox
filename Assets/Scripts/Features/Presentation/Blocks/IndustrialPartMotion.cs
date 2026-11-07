// 职责：为工业零件提供配置驱动的机械运动表现。
// 入口：Configure()。
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class IndustrialPartMotion : MonoBehaviour
{
    private static readonly List<IndustrialPartMotion> Active = new List<IndustrialPartMotion>();
    [SerializeField]
    private Transform[] spinTargets = Array.Empty<Transform>();
    [SerializeField]
    private Vector3 spinAxis = Vector3.up;
    [SerializeField]
    private float degreesPerSecond = 24f;
    [SerializeField]
    private Transform bobTarget;
    [SerializeField]
    private float bobAmplitude = 0.025f;
    [SerializeField]
    private float bobFrequency = 1.2f;
    [SerializeField]
    private Renderer[] glowRenderers = Array.Empty<Renderer>();
    [SerializeField]
    private Color baseEmission = new Color(0.05f, 1.4f, 2.2f, 1f);
    [SerializeField]
    private float emissionPulse = 0.18f;
    private IndustrialEmissionBinding[] _emissionBindings = Array.Empty<IndustrialEmissionBinding>();
    private int _activeIndex = -1;
    private Vector3 _bobOrigin;
    internal bool ControlsVisual(Transform part, Renderer renderer)
    {
        return part == bobTarget || Array.IndexOf(spinTargets, part) >= 0 || Array.IndexOf(glowRenderers, renderer) >= 0;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        CacheRuntimeState();
        if (_activeIndex < 0)
        {
            _activeIndex = Active.Count;
            Active.Add(this);
        }
    }

    private void OnDisable()
    {
        ReleaseEmission();
        if (_activeIndex < 0)
        {
            return;
        }

        int last = Active.Count - 1;
        IndustrialPartMotion moved = Active[last];
        Active[_activeIndex] = moved;
        moved._activeIndex = _activeIndex;
        Active.RemoveAt(last);
        _activeIndex = -1;
    }

    internal static void AnimateAll()
    {
        float deltaTime = Time.deltaTime;
        float unscaledTime = Time.unscaledTime;
        IndustrialEmissionBinding.Tick(unscaledTime);
        for (int i = 0; i < Active.Count; i++)
        {
            Active[i].Animate(deltaTime, unscaledTime);
        }
    }

    internal static void AnimateScene(UnityEngine.SceneManagement.Scene scene)
    {
        float deltaTime = Time.deltaTime;
        float unscaledTime = Time.unscaledTime;
        IndustrialEmissionBinding.Tick(unscaledTime);
        for (int i = 0; i < Active.Count; i++)
        {
            if (Active[i] != null && Active[i].gameObject.scene == scene)
            {
                Active[i].Animate(deltaTime, unscaledTime);
            }
        }
    }

    private void Animate(float deltaTime, float unscaledTime)
    {
        float deltaDegrees = degreesPerSecond * deltaTime;
        for (int i = 0; i < spinTargets.Length; i++)
        {
            Transform target = spinTargets[i];
            if (target != null && deltaDegrees != 0f)
            {
                target.Rotate(spinAxis, deltaDegrees, Space.Self);
            }
        }

        float wave = Mathf.Sin(unscaledTime * bobFrequency * Mathf.PI * 2f);
        if (bobTarget != null && bobAmplitude != 0f)
        {
            bobTarget.localPosition = _bobOrigin + Vector3.up * (wave * bobAmplitude);
        }

        for (int i = 0; i < _emissionBindings.Length; i++)
        {
            _emissionBindings[i].Apply(baseEmission * Mathf.Max(0f, 1f + wave * emissionPulse));
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
        ReleaseEmission();
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
        ReleaseEmission();
        if (Application.isPlaying && isActiveAndEnabled && emissionPulse > 0f)
        {
            _emissionBindings = new IndustrialEmissionBinding[glowRenderers.Length];
            for (int i = 0; i < glowRenderers.Length; i++)
            {
                _emissionBindings[i] = new IndustrialEmissionBinding(glowRenderers[i], baseEmission, bobFrequency, emissionPulse);
            }
        }

        if (bobTarget != null)
        {
            _bobOrigin = bobTarget.localPosition;
        }
    }

    private void ReleaseEmission()
    {
        for (int i = 0; i < _emissionBindings.Length; i++)
        {
            _emissionBindings[i].Dispose();
        }

        _emissionBindings = Array.Empty<IndustrialEmissionBinding>();
    }
}
