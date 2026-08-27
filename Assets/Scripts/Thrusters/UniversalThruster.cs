using UnityEngine;

public class UniversalThruster : Thruster
{
    public bool alignVisual = true;
    public float rotationSpeed = 5f;
    public Vector3 inputDir;

    private void FixedUpdate()
    {
        if (!IsPlayModeActive()) return;

        RefreshRuntimeReferences();

        bool hasValidOwner = HasValidRuntimeOwner();
        inputDir = hasValidOwner ? controlUnit.MovementInput : Vector3.zero;
        bool active = hasValidOwner && inputDir.sqrMagnitude > InputEpsilonSqr;

        RotateThruster(inputDir, active);
        thrust = active ? maxThrust : 0f;
        ApplyThrustChangeRateLimit();
        ApplyThrust();
        VisualizeThrust();
    }

    private void RotateThruster(Vector3 worldDir, bool active)
    {
        if (!alignVisual || !active || model == null) return;

        Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
        model.rotation = Quaternion.RotateTowards(
            model.rotation,
            targetRot,
            rotationSpeed * Time.fixedDeltaTime * 60f
        );
    }

    public void ApplyThrust()
    {
        if (thrust <= 0f || !CanApplyThrust() || !TryEnsureRigidbody()) return;

        thrustDirection = model != null ? model.forward : transform.forward;
        rb.AddForceAtPosition(thrustDirection * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        return inputDir.sqrMagnitude > InputEpsilonSqr
            && IsPlayModeActive()
            && CanApplyThrust()
            && HasValidRuntimeOwner();
    }
}
