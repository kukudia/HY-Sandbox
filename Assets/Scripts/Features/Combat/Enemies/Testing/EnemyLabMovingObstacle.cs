// 职责：按可重复的周期驱动试验场运动学障碍，保留真实碰撞及暂停语义。
// 入口：OnEnable() / ResetMotion()重置相位；FixedUpdate()提交物理运动。
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class EnemyLabMovingObstacle : MonoBehaviour
{
    [SerializeField, Tooltip("相对初始世界位置的单侧往返位移，单位米。")]
    private Vector3 travel = new Vector3(0f, 0f, 20f);
    [SerializeField, Min(1f), Tooltip("完成一次往返所需的模拟秒数；暂停时停止。")]
    private float period = 16f;
    [SerializeField, Tooltip("相对初始姿态的局部旋转轴。")]
    private Vector3 rotationAxis = Vector3.up;
    [SerializeField, Range(0f, 180f), Tooltip("单侧摆动角度；0表示只平移。")]
    private float swingAngle;
    private Rigidbody body;
    private Vector3 origin;
    private Quaternion orientation;
    private float elapsed;
    private void Awake()
    {
        CaptureOrigin();
    }

    private void CaptureOrigin()
    {
        if (body != null)
        {
            return;
        }

        body = GetComponent<Rigidbody>();
        origin = transform.position;
        orientation = transform.rotation;
    }

    private void OnEnable()
    {
        ResetMotion();
    }

    public void ResetMotion()
    {
        CaptureOrigin();
        elapsed = 0f;
        // 重试必须瞬移回同一相位，避免上轮障碍位置影响本轮对比。
        body.position = origin;
        body.rotation = orientation;
        transform.SetPositionAndRotation(origin, orientation);
    }

    private void FixedUpdate()
    {
        elapsed += Time.fixedDeltaTime;
        float wave = Mathf.Sin(elapsed * Mathf.PI * 2f / Mathf.Max(1f, period));
        body.MovePosition(origin + travel * wave);
        Vector3 axis = rotationAxis.sqrMagnitude > 0.001f ? rotationAxis.normalized : Vector3.up;
        body.MoveRotation(orientation * Quaternion.AngleAxis(swingAngle * wave, axis));
    }

    private void OnDisable()
    {
        if (body != null)
        {
            ResetMotion();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying && body != null ? origin : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center - travel, center + travel);
        Gizmos.DrawWireSphere(center - travel, 2f);
        Gizmos.DrawWireSphere(center + travel, 2f);
    }
}
