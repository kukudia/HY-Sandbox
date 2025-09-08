using UnityEngine;
using UnityEngine.InputSystem;

public class UniversalThruster : Thruster
{
    [Header("Thrust Settings")]
    public bool alignVisual = true;     // 是否旋转推进器朝向
    public float rotationSpeed = 5f;    // 推进器旋转速度 (deg/sec)

    private void FixedUpdate()
    {
        Vector3 inputDir = GetInputDirection();
        RotateThruster(inputDir);
        thrust = ShouldActivate() ? thrust : 0;
        ApplyThrustChangeRateLimit();
        ApplyThrust();
    }

    void RotateThruster(Vector3 worldDir)
    {
        // 旋转推进器方向
        if (alignVisual && worldDir.sqrMagnitude > 1e-6f && ShouldActivate())
        {
            Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
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
        rb.AddForceAtPosition(transform.forward * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        if (controlUnit == null)
            controlUnit = GetComponentInParent<ControlUnit>();

        if (PlayManager.instance.playMode && controlUnit.HasCockpit())
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
        return false;
    }
}
