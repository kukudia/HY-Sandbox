using UnityEngine;

public class HoverThruster : Thruster
{
    public float rotationSpeed = 5f;
    public float heightP;
    public bool isHovered;

    private void FixedUpdate()
    {
        if (!PlayManager.instance.playMode) return;

        if (controlUnit == null)
        {
            controlUnit = GetComponentInParent<ControlUnit>();
        }

        if (controlUnit == null || !controlUnit.HasValidCockpit)
        {
            thrust = 0;
            VisualizeThrust();
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
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        if (rb == null) return;

        thrustDirection = new Vector3(0, 1, 0);
        rb.AddForceAtPosition(transform.TransformDirection(thrustDirection) * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        return isHovered
            && PlayManager.instance.playMode
            && controlUnit != null
            && controlUnit.HasValidCockpit;
    }
}
