// 职责：标记并显示建造旋转轴，供 BuildManager 拖拽操作使用。
// 入口：Awake()。
using UnityEngine;

public class RotateAxisHandle : MonoBehaviour
{
    public Vector3 axis; // (1,0,0) / (0,1,0) / (0,0,1)
    public Vector3 WorldAxis => BuildHandleAxis.World(transform);

    private void Awake()
    {
        if (axis.sqrMagnitude < 0.001f)
        {
            axis = BuildHandleAxis.Local(transform);
        }
    }
}
