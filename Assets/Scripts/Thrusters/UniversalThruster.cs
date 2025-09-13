using UnityEngine;
using UnityEngine.InputSystem;

public class UniversalThruster : Thruster
{
    [Header("Thrust Settings")]
    public bool alignVisual = true;     // 是否旋转推进器朝向
    public float rotationSpeed = 5f;    // 推进器旋转速度 (deg/sec)
    public Vector3 inputDir;

    private void FixedUpdate()
    {
        if (!PlayManager.instance.playMode) return;

        if (controlUnit == null)
        {
            controlUnit = GetComponentInParent<ControlUnit>();
        }

        if (controlUnit.cockpit != null)
        {
            inputDir = GetInputDirection();
        }

        RotateThruster(inputDir);
        thrust = ShouldActivate() ? maxThrust : 0;
        ApplyThrustChangeRateLimit();
        ApplyThrust();
        VisualizeThrust();
    }

    void RotateThruster(Vector3 worldDir)
    {
        // 旋转推进器方向
        if (alignVisual && ShouldActivate())
        {
            Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
            model.rotation = Quaternion.RotateTowards(
                model.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime * 60f // rotationSpeed = deg/sec
            );
        }
    }

    public void ApplyThrust()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        // 推力施加
        thrustDirection = model.forward;
        rb.AddForceAtPosition(thrustDirection * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        if (inputDir.sqrMagnitude > 1e-6f)
        {
            if (controlUnit == null)
                controlUnit = GetComponentInParent<ControlUnit>();

            if (PlayManager.instance.playMode)
            {
                return true;
            }
            //else if (!PlayManager.instance.playMode)
            //{
            //    Debug.LogWarning("Not in play mode.");
            //}
            //else if (!controlUnit.HasCockpit())
            //{
            //    Debug.LogWarning("Lack of cockpit.");
            //}
        }
        return false;
    }
}
