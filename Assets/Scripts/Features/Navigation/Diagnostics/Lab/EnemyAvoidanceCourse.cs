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
    [SerializeField, Tooltip("可独立选择的安全起终点；Transform坐标即舰体根节点目标高度。")]
    private Transform[] locations = new Transform[0];
    public int LocationCount => locations.Length;

    public Transform Location(int index) => index >= 0 && index < locations.Length ? locations[index] : null;
    public Vector3 Start => start;
    public Vector3 Goal => goal;
    public float Duration => duration;
    public string Description => description;
}
