// 职责：试验舰的碰撞计数，不对运动、伤害或生产AI施加修正。
// 入口：OnCollisionEnter()；仅对试验布局的实体障碍记数，过滤地面。
using UnityEngine;

public sealed class EnemyAvoidanceContact : MonoBehaviour
{
    [SerializeField]
    private EnemyAvoidanceLab lab;
    private void OnCollisionEnter(Collision collision)
    {
        if (lab != null && collision.collider.transform.IsChildOf(lab.transform))
        {
            lab.RecordContact();
        }
    }
}
