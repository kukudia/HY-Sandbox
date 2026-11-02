// 职责：配置可复用能量线与灯光闪烁，统一径向碎光、颜色及可见网格包围盒规则。
// 入口：CreateLineStreak() / CreateRadialStreakBurst() / CreateLightFlash()；无模式或场景单例依赖。
using UnityEngine;

internal static class TransientEffects
{
    public static void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)
    {
        BeamFade fade = TransientEffectPool.RentBeam();
        StylizedBeamEffect beam = fade.Beam;
        beam.Configure(width);
        beam.SetEndpoints(from, to);
        beam.SetColor(color);
        beam.SetIntensity(1f);
        beam.SetVisible(true);
        fade.Initialize(beam, color, Mathf.Max(0.05f, duration));
    }

    public static void CreateRadialStreakBurst(
        Vector3 center,
        Vector3 normal,
        Color color,
        int count,
        float length,
        float duration,
        float width)
    {
        Vector3 hemisphereNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Vector3 direction = Random.onUnitSphere;
            if (hemisphereNormal != Vector3.zero && Vector3.Dot(direction, hemisphereNormal) < 0f)
            {
                direction = -direction;
            }

            Vector3 start = center + direction * Random.Range(0.02f, length * 0.12f);
            Vector3 end = start + direction * Random.Range(length * 0.45f, length);
            TransientEffects.CreateLineStreak(
                start,
                end,
                Color.Lerp(color, Brighten(Color.white, 2.4f), Random.Range(0.05f, 0.35f)),
                Random.Range(duration * 0.7f, duration * 1.15f),
                Random.Range(width * 0.65f, width * 1.25f));
        }
    }

    public static void CreateLightFlash(Vector3 position, Color color, float intensity, float range, float duration)
    {
        LightFade fade = TransientEffectPool.RentLight(position);
        Light light = fade.Light;
        light.color = color;
        light.intensity = intensity;
        light.range = Mathf.Max(0.5f, range);
        fade.Initialize(light, Mathf.Max(0.05f, duration), intensity);
    }

    public static Bounds GetBounds(GameObject target, Vector3 fallbackCenter, Vector3 fallbackSize)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds bounds = new Bounds(fallbackCenter, fallbackSize);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    public static Vector3 GetBlockSize(Block block)
    {
        return new Vector3(Mathf.Max(1, block.x), Mathf.Max(1, block.y), Mathf.Max(1, block.z));
    }

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    public static Color Brighten(Color color, float intensity, float whiteBlend = 0f)
    {
        Color bright = Color.Lerp(color, Color.white, Mathf.Clamp01(whiteBlend));
        bright.r *= intensity;
        bright.g *= intensity;
        bright.b *= intensity;
        bright.a = color.a;
        return bright;
    }
}
