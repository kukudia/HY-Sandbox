// 职责：按寿命衰减闪光并销毁临时对象。
using UnityEngine;

public sealed class LightFade : MonoBehaviour
{
    private Light targetLight;
    private float startIntensity;
    private float duration;
    private float elapsed;
    public void Initialize(Light light, float lifetime, float intensity)
    {
        targetLight = light;
        duration = lifetime;
        startIntensity = intensity;
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
            Destroy(gameObject);
        }
    }
}
