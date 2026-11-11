// 职责：保存试验布局的推荐起点、目标点和观察时长，保留可编辑场景参数。
// 入口：EnemyAvoidanceLab.SelectLayout()读取；不生成运行时障碍。
using UnityEngine;

public sealed class EnemyAvoidanceCourse : MonoBehaviour
{
    [SerializeField]
    private Vector3 start = new Vector3(-45f, 6f, 0f);
    [SerializeField]
    private Vector3 goal = new Vector3(45f, 3f, 0f);
    [SerializeField, Min(5f)]
    private float duration = 150f;
    [SerializeField, TextArea]
    private string description;
    public Vector3 Start => start;
    public Vector3 Goal => goal;
    public float Duration => duration;
    public string Description => description;
}
