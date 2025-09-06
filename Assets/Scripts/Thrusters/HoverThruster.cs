using UnityEngine;
using UnityEngine.InputSystem;

public class HoverThruster : Thruster
{
    public float maxThrust = 250f;   // 最大推力
    public float rotationSpeed = 5f;
    public float hoverHeight = 5f;    // 悬浮目标高度（可选）
    public float heightP;
    public bool isHovered;

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isHovered = !isHovered;
        }
    }

    private void FixedUpdate()
    {
        if (ShouldActivate())
        {
            ApplyThrust();
        }
    }

    public virtual void ApplyThrust()
    {
        //float eulerZ = transform.rotation.eulerAngles.z;

        //if (eulerZ > -30 && eulerZ < 30)
        //{
        //    Quaternion targetRot = Quaternion.LookRotation(transform.forward, Vector3.up);
        //    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime * 60f);
        //}

        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }
        else
        {
            thrustDirection = transform.up;
            rb.AddForceAtPosition(transform.TransformDirection(thrustDirection) * thrust, transform.position);
        }
    }

    public override bool ShouldActivate()
    {
        if (isHovered)
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
