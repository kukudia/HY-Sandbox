using UnityEngine;
using UnityEngine.InputSystem;

public class MainThruster : Thruster
{
    private void FixedUpdate()
    {
        if (ShouldActivate())
        {
            ApplyThrust();
        }
    }

    public void ApplyThrust()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();

        if (rb == null) return;

        // 取飞船的前进方向（世界坐标）
        Vector3 forward = rb.transform.forward;

        // 投影到水平面（XZ 平面），去掉 Y 分量
        Vector3 horizontalForward = new Vector3(forward.x, 0f, forward.z);

        if (horizontalForward.sqrMagnitude > 1e-6f)
        {
            horizontalForward.Normalize();
        }
        else
        {
            horizontalForward = Vector3.forward; // 防止 NaN
        }

        // 让推进器朝向这个方向
        transform.rotation = Quaternion.LookRotation(horizontalForward, Vector3.up);

        // 推力方向 = 推进器 forward
        thrustDirection = transform.forward;

        // 施加推力
        rb.AddForceAtPosition(thrustDirection * thrust, transform.position);
    }

    public override bool ShouldActivate()
    {
        if (Keyboard.current.wKey.isPressed)
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
        }
        return false;
    }
}
