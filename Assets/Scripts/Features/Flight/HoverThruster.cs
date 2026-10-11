// 职责：计算悬浮推进输出并应用高度/重力/姿态相关反馈。
// 入口：ApplyThrust() / ShouldActivate()。
using UnityEngine;

public class HoverThruster : Thruster
{
    public float rotationSpeed = 5f;
    public float heightP;
    public bool isHovered;
    private void FixedUpdate()
    {
        if (!IsPlayModeActive())
        {
            SetPowerRequested(false);
            thrust = 0f;
            lastThrustValue = 0f;
            return;
        }

        RefreshRuntimeReferences();
        if (!HasValidRuntimeOwner())
        {
            SetPowerRequested(false);
            bool hadThrust = thrust > 0f || lastThrustValue > 0f;
            thrust = 0f;
            lastThrustValue = 0f;
            VisualizeThrust(hadThrust);
            return;
        }

        SetPowerRequested(ShouldActivate());
        if (controlUnit.hoverFlightController != null)
        {
            ApplyThrust();
        }
        else
        {
            thrust = ShouldActivate() ? maxThrust * 0.75f : 0;
            ApplyThrustChangeRateLimit();
            ApplyThrust();
        }

        VisualizeThrust();
    }

    public virtual void ApplyThrust()
    {
        if (thrust <= 0f || !CanApplyThrust() || !TryEnsureRigidbody())
        {
            return;
        }

        thrustDirection = Vector3.up;
        rb.AddForceAtPosition(transform.up * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        return isHovered && IsPlayModeActive() && HasValidRuntimeOwner() && (controlUnit.hoverFlightController == null || controlUnit.hoverFlightController.isActiveAndEnabled);
    }
}
