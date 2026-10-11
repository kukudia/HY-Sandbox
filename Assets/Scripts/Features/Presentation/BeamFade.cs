// 职责：按寿命衰减能量线并销毁临时对象。
using UnityEngine;

public sealed class BeamFade : MonoBehaviour
{
    private StylizedBeamEffect beam;
    private Color color;
    private float duration;
    private float elapsed;
    public void Initialize(StylizedBeamEffect effect, Color lineColor, float lifetime)
    {
        beam = effect;
        color = lineColor;
        duration = lifetime;
    }

    private void Update()
    {
        if (beam == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        beam.SetIntensity(1f - t);
        if (t >= 1f)
        {
            beam.SetVisible(false);
            Destroy(gameObject);
        }
    }
}
