using UnityEngine;

public abstract class Thruster : MonoBehaviour
{
    public Transform model;
    public float thrust = 100f;     // 推力大小
    public Vector3 thrustDirection = Vector3.forward; // 推力方向（本地坐标）

    protected Rigidbody rb;

    // 子类必须实现：如何启用推进器（输入控制/自动触发）
    public abstract bool ShouldActivate();

    protected virtual void Start()
    {
        if (model == null)
        {
            model = transform.Find("Model");
        }
    }
}
