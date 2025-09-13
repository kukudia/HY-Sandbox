using UnityEngine;
using UnityEngine.InputSystem;

public class HoverThruster : Thruster
{
    public float rotationSpeed = 5f;
    public float heightP;
    public bool isHovered;

    private void FixedUpdate()
    {
        if (!PlayManager.instance.playMode) return;

        if (controlUnit == null)
        {
            controlUnit = GetComponentInParent<ControlUnit>();
        }

        if (controlUnit.hoverFlightController != null)
        {
            //ApplyThrustChangeRateLimit();
            ApplyThrust();
        }
        else
        {
            thrust = ShouldActivate() ? maxThrust * 0.75f : 0;
            ApplyThrustChangeRateLimit();
            ApplyThrust();
        }

        VisualizeThrust();
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
            thrustDirection = new Vector3(0, 1, 0);
            rb.AddForceAtPosition(transform.TransformDirection(thrustDirection) * thrust, transform.position);
        }
    }

    public override bool ShouldActivate()
    {
        if (isHovered)
        {
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
