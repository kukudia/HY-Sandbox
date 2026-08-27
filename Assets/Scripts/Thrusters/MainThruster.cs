using UnityEngine;

public class MainThruster : Thruster
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
        thrustDirection = transform.forward;

        bool active = hasValidOwner && inputDir.sqrMagnitude > InputEpsilonSqr;
        thrust = active ? maxThrust * Mathf.Max(0f, Vector3.Dot(inputDir, thrustDirection)) : 0f;
        ApplyThrustChangeRateLimit();
        ApplyThrust();
        VisualizeThrust();
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
