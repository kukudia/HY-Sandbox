// 职责：每场景一个可见调度器，集中驱动该场景工业零件表现。
// 入口：Update()；组件由场景 Managers 持有。
using UnityEngine;

public sealed class IndustrialMotionDriver : MonoBehaviour
{
    private void Update()
    {
        IndustrialPartMotion.AnimateScene(gameObject.scene);
    }
}
