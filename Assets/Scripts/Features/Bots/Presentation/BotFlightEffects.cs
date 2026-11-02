// 职责：按实际飞行速度更新已创作的飞行 VFX，并在停靠状态清理尾迹。
// 入口：Bot.LateUpdate() 调用 Update()；导航状态切换调用 SetDocked()。
// 不变量：只改变 VFX 的显示强度/启用状态，不修改刚体、导航或 Inspector 参数。
using UnityEngine;

public static class BotFlightEffects
{
    public static void Update(
        Rigidbody rb,
        bool flying,
        bool showTrail,
        float movementSpeed,
        VfxEffect trailRenderer,
        VfxEffect trailCoreRenderer,
        VfxEffect flightEffect)
    {
        if (rb == null)
        {
            return;
        }

        float speed = rb.isKinematic ? 0f : rb.linearVelocity.magnitude;
        float ratio = showTrail && flying ? Mathf.InverseLerp(0.15f, Mathf.Max(0.3f, movementSpeed), speed) : 0f;
        if (trailRenderer != null)
        {
            trailRenderer.SetIntensity(ratio);
        }

        if (trailCoreRenderer != null)
        {
            trailCoreRenderer.SetIntensity(ratio);
        }

        if (flightEffect != null)
        {
            flightEffect.SetIntensity(ratio);
        }
    }

    public static void SetDocked(bool docked, VfxEffect trailRenderer, VfxEffect trailCoreRenderer, VfxEffect flightEffect)
    {
        if (trailRenderer != null)
        {
            trailRenderer.enabled = !docked;
            trailRenderer.SetIntensity(0f);
            if (docked)
            {
                trailRenderer.Clear();
            }
        }

        if (trailCoreRenderer != null)
        {
            trailCoreRenderer.enabled = !docked;
            trailCoreRenderer.SetIntensity(0f);
            if (docked)
            {
                trailCoreRenderer.Clear();
            }
        }

        if (docked && flightEffect != null)
        {
            flightEffect.Clear();
        }
    }
}
