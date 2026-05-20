using UnityEngine;

public class MainThruster : Thruster
{
    public bool alignVisual = true;
    public float rotationSpeed = 5f;
    public Vector3 inputDir;

    private void FixedUpdate()
    {
        if (!PlayManager.instance.playMode) return;

        if (controlUnit == null)
        {
            controlUnit = GetComponentInParent<ControlUnit>();
        }

        inputDir = controlUnit != null && controlUnit.HasValidCockpit ? controlUnit.MovementInput : Vector3.zero;
        thrustDirection = transform.forward;

        thrust = maxThrust * GetProjectionLength(inputDir, thrustDirection);
        thrust = ShouldActivate() ? thrust : 0;
        ApplyThrustChangeRateLimit();
        ApplyThrust();
        VisualizeThrust();
    }

    public void ApplyThrust()
    {
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        if (rb == null) return;

        thrustDirection = model.forward;
        rb.AddForceAtPosition(thrustDirection * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        return inputDir.sqrMagnitude > 1e-6f
            && PlayManager.instance.playMode
            && controlUnit != null
            && controlUnit.HasValidCockpit;
    }

    private float GetProjectionLength(Vector3 vector, Vector3 thrustDir)
    {
        thrustDir = thrustDir.normalized;
        return Vector3.Dot(vector, thrustDir);
    }
}
