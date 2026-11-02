// 职责：采集单个刚体的异常速度/冲量线索，供调试记录器分析。
// 入口：Awake() / OnCollisionEnter()。
#if UNITY_EDITOR
using UnityEngine;

/// <summary>Editor-only contact observer attached to discovered Rigidbody objects in Play Mode.</summary>
public sealed class AbnormalImpulseBodyProbe : MonoBehaviour
{
    private Rigidbody _body;
    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        AbnormalImpulseRecorder.RecordCollision(_body, collision);
    }
}
#endif
