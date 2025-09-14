using UnityEngine;
using UnityEngine.InputSystem;

public class MainThruster : Thruster
{
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
        thrustDirection = transform.forward;

        thrust = maxThrust * GetProjectionLength(inputDir, thrustDirection);
        thrust = ShouldActivate() ? thrust : 0;
        ApplyThrustChangeRateLimit();
        ApplyThrust();
        VisualizeThrust();
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

    float GetProjectionLength(Vector3 vector, Vector3 thrustDir)
    {
        // 1. 确保方向向量是单位向量
        thrustDir = thrustDir.normalized;

        // 2. 计算投影长度（点积结果）
        float projectionLength = Vector3.Dot(vector, thrustDir);

        // 3. 计算分量向量 = 投影长度 × 方向单位向量
        return projectionLength;
    }
}
