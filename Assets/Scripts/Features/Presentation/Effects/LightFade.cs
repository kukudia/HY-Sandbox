// 职责：按未缩放时间衰减闪光；池实例到期回收，非池实例仍销毁。
// 入口：Initialize()；复用时清零寿命，Update()完成淡出与释放。
using UnityEngine;

public sealed class LightFade : MonoBehaviour
{
    private Light targetLight;
    private float startIntensity;
    private float duration;
    private float elapsed;
    private TransientEffectPool pool;
    internal Light Light => targetLight;

    internal void SetPool(TransientEffectPool owner, Light light)
    {
        pool = owner;
        targetLight = light;
    }

    public void Initialize(Light light, float lifetime, float intensity)
    {
        targetLight = light;
        duration = lifetime;
        startIntensity = intensity;
        elapsed = 0f;
    }

    private void Update()
    {
        if (targetLight == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        targetLight.intensity = Mathf.Lerp(startIntensity, 0f, t * t);
        if (t >= 1f)
        {
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
