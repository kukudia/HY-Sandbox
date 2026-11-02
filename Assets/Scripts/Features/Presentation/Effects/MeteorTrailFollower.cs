// 职责：让独立尾迹跟随陨石，陨石销毁后停止并释放。
using UnityEngine;

public sealed class MeteorTrailFollower : MonoBehaviour
{
    private Transform _meteor;
    private Rigidbody _body;
    private VfxEffect _effect;
    public void Initialize(Transform meteor, VfxEffect effect)
    {
        _meteor = meteor;
        _body = meteor.GetComponent<Rigidbody>();
        _effect = effect;
        Follow();
    }

    private void LateUpdate()
    {
        if (_effect == null || !_effect.IsEmitting)
        {
            enabled = false;
            return;
        }

        if (_meteor == null)
        {
            _effect.StopAndRelease();
            enabled = false;
            return;
        }

        Follow();
    }

    private void Follow()
    {
        transform.position = _meteor.position;
        transform.rotation = _body != null && _body.linearVelocity.sqrMagnitude > 0.01f ? Quaternion.FromToRotation(Vector3.up, -_body.linearVelocity.normalized) : Quaternion.identity;
    }
}
