// 职责：按未缩放时间淡出能量线；池实例到期回收，非池实例仍销毁。
// 入口：Initialize()；复用时清零寿命，Update()完成淡出与释放。
using UnityEngine;

public sealed class BeamFade : MonoBehaviour
{
    private StylizedBeamEffect beam;
    private Color color;
    private float duration;
    private float elapsed;
    private TransientEffectPool pool;
    internal StylizedBeamEffect Beam => beam;

    internal void SetPool(TransientEffectPool owner, StylizedBeamEffect effect)
    {
        pool = owner;
        beam = effect;
    }

    public void Initialize(StylizedBeamEffect effect, Color lineColor, float lifetime)
    {
        beam = effect;
        color = lineColor;
        duration = lifetime;
        elapsed = 0f;
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
            if (pool != null)
            {
                pool.Return(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
