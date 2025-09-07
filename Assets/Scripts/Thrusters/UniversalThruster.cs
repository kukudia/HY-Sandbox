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
        if (inputDir.sqrMagnitude > 1e-6f)
        {
            thrust = ShouldActivate() ? thrust : 0;
            ApplyThrustChangeRateLimit();
            ApplyThrust(inputDir);
        }
    }

    public void ApplyThrust(Vector3 worldDir)
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        // 旋转推进器方向
        if (alignVisual && worldDir.sqrMagnitude > 1e-6f)
        {
            Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime * 60f // rotationSpeed = deg/sec
            );
        }

        // 推力施加
        rb.AddForceAtPosition(transform.forward * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        if (PlayManager.instance.playMode && PlayManager.instance.hasCockpit())
        {
            return true;
        }
        else if (!PlayManager.instance.playMode)
        {
            Debug.LogWarning("Not in play mode.");
        }
        else if (!PlayManager.instance.hasCockpit())
        {
            Debug.LogWarning("Lack of cockpit.");
        }
        return false;
    }
}
