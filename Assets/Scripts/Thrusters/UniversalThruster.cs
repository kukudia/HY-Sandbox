using UnityEngine;

public class UniversalThruster : Thruster
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

        RotateThruster(inputDir);
        thrust = ShouldActivate() ? maxThrust : 0;
        ApplyThrustChangeRateLimit();
        ApplyThrust();
        VisualizeThrust();
    }

    private void RotateThruster(Vector3 worldDir)
    {
        if (!alignVisual || !ShouldActivate()) return;

        Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
        model.rotation = Quaternion.RotateTowards(
            model.rotation,
            targetRot,
            rotationSpeed * Time.fixedDeltaTime * 60f
        );
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
}
