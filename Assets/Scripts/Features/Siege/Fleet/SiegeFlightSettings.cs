// 职责：保存机库出入航线、速度与失能报废阈值，供预研和正式玩法共享。
// 入口：SiegeReturnFlight 读取；宿主通过 Inspector 编辑，不持有运行状态。
using System;
using UnityEngine;

[Serializable]
public sealed class SiegeFlightSettings
{
    [Min(1f), Tooltip("单舰出入库最长时间，超时按无法返航报废（秒）。")]
    public float timeout = 120f;
    [Min(0.1f), Tooltip("持续缺电/缺推进能力的宽限时间，避免电网刷新瞬间误报废。")]
    public float disabledGrace = 5f;
    [Min(0.1f), Tooltip("机库自动驾驶最大航速（米/秒）。")]
    public float speed = 8f;
    [Min(0.1f), Tooltip("自动驾驶加速度上限；还会受现存发动机实际供电推力限制。")]
    public float acceleration = 6f;
    [Min(1f), Tooltip("对准机库航向的最大角速度（度/秒）。")]
    public float angularSpeed = 60f;
    [Min(0.05f), Tooltip("最终停泊位置误差（米），不会远距离传送或吸附。")]
    public float dockingTolerance = 0.15f;
    [Min(0.1f), Tooltip("外部航线高于库顶的安全余量（米）。")]
    public float approachClearance = 4f;
}
