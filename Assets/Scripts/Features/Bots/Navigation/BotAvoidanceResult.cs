// 职责：表达一次导航采样的方向修正、强度、速度比例和障碍数量。
// Direction 在世界坐标中；没有障碍时 SpeedMultiplier 为 1。
using UnityEngine;

public struct BotAvoidanceResult
{
    public Vector3 Direction;
    public bool OverrideDirection;
    public float Strength;
    public float SpeedMultiplier;
    public int ObstacleCount;
}
