// 职责：计算悬浮推进输出并应用高度/重力/姿态相关反馈。
// 入口：ApplyThrust() / ShouldActivate()。
using UnityEngine;

public class HoverThruster : Thruster
{
    public float rotationSpeed = 5f;
    public float heightP;
    public bool isHovered;
    private float _controlledThrust;
    public void SetControlledThrust(float desiredThrust)
    {
        _controlledThrust = Mathf.Max(0f, desiredThrust);
    }

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

        bool active = ShouldActivate();
        SetPowerRequested(active);
        thrust = active ? controlUnit.hoverFlightController != null ? _controlledThrust : maxThrust * 0.75f : 0f;
        // 控制器只提交目标；由推进器每个物理帧限速一次，避免关机被重复衰减或覆盖。
        ApplyThrustChangeRateLimit();
        ApplyThrust();
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
