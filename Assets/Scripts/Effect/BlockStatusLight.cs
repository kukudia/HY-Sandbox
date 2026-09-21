using UnityEngine;

[DisallowMultipleComponent]
public sealed class BlockStatusLight : MonoBehaviour
{
    [SerializeField] private Light[] _lights = System.Array.Empty<Light>();
    [SerializeField] private PowerGeneratingUnit _generator;
    [SerializeField] private RepairBot _bot;
    [SerializeField] private Color _readyColor = new Color(0.12f, 0.8f, 1f);
    [SerializeField] private Color _activeColor = new Color(0.18f, 1f, 0.52f);
    [SerializeField, Min(0f)] private float _intensity = 1.4f;

    private void Update()
    {
        bool available = _generator == null || (_generator.isActiveAndEnabled && _generator.outputPower > 0f);
        bool working = _bot != null && _bot.currentState != RepairBot.NavigationState.Idle;
        foreach (Light light in _lights)
        {
            if (light == null) continue;
            light.color = working ? _activeColor : _readyColor;
            light.intensity = available ? _intensity * (working ? 0.85f + 0.15f * Mathf.Sin(Time.time * 5f) : 1f) : 0f;
        }
    }
}
