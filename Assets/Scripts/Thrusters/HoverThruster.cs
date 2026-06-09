using UnityEngine;

public class HoverThruster : Thruster
{
    public float rotationSpeed = 5f;
    public float heightP;
    public bool isHovered;

    private void FixedUpdate()
    {
        if (!IsPlayModeActive()) return;

        RefreshRuntimeReferences();

        if (!HasValidRuntimeOwner())
        {
            bool hadThrust = thrust > 0f || lastThrustValue > 0f;
            thrust = 0f;
            lastThrustValue = 0f;
            VisualizeThrust(hadThrust);
            return;
        }

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
        if (thrust <= 0f || !TryEnsureRigidbody()) return;

        thrustDirection = Vector3.up;
        rb.AddForceAtPosition(transform.up * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        return isHovered
            && IsPlayModeActive()
            && HasValidRuntimeOwner();
    }
}
