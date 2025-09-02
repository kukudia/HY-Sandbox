using UnityEngine;

public abstract class Thruster : MonoBehaviour
{
    public float thrustPower = 1000f;     // 推力大小
    public Vector3 thrustDirection = Vector3.forward; // 推力方向（本地坐标）

    protected Rigidbody rb;

    protected virtual void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    // 子类必须实现：如何启用推进器（输入控制/自动触发）
    public abstract bool ShouldActivate();

    protected virtual void FixedUpdate()
    {
        if (ShouldActivate())
        {
            ApplyThrust();
        }
    }

    protected virtual void ApplyThrust()
    {
        rb.AddForceAtPosition(transform.TransformDirection(thrustDirection) * thrustPower, transform.position);
    }
}
