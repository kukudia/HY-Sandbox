using UnityEngine;
using UnityEngine.InputSystem;

public class UniversalThruster : Thruster
{
    [Header("Thrust Settings")]
    //public float thrustPower = 1000f;   // 推力大小（N）
    public bool alignVisual = true;     // 是否旋转推进器朝向
    public float rotationSpeed = 5f;    // 推进器旋转速度 (deg/sec)
    public Transform cameraTransform;   // 主摄像机（必须指定）

    private void FixedUpdate()
    {
        if (ShouldActivate())
        {
            Vector3 inputDir = GetInputDirection();
            if (inputDir.sqrMagnitude > 1e-6f)
            {
                ApplyThrust(inputDir);
            }
        }
    }

    /// <summary>
    /// 获取输入方向：WASD 结合摄像机方向
    /// </summary>
    private Vector3 GetInputDirection()
    {
        Vector3 dir = Vector3.zero;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // 摄像机 forward / right 的水平分量
        Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        if (Keyboard.current.wKey.isPressed) dir += camFwd;
        if (Keyboard.current.sKey.isPressed) dir -= camFwd;
        if (Keyboard.current.aKey.isPressed) dir -= camRight;
        if (Keyboard.current.dKey.isPressed) dir += camRight;

        if (dir.sqrMagnitude > 1e-6f)
            dir.Normalize();

        return dir;
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
