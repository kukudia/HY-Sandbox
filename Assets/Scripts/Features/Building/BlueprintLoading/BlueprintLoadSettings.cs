// 职责：保存一次蓝图加载所使用的延时与镜头参数快照。
// 来源：BuildManager 的现有 Inspector 字段；不改变场景或 Prefab 的字段身份。
using System;
using UnityEngine;

internal sealed class BlueprintLoadSettings
{
    public float InitialInterval;
    public float MinimumInterval;
    public float IntervalHalfLife;
    public float GridSize;
    public bool MoveCamera;
    public int MinimumOrbitBlocks;
    public float CameraReferenceSecondsPerBlock;
    public float CameraMoveDuration;
    public float CameraPitch;
    public float CameraRadiusVariation;
    public float CameraRadiusWaveDegrees;
    public float GetInterval(double elapsedSeconds)
    {
        float initial = Mathf.Max(0f, InitialInterval);
        float minimum = Mathf.Clamp(MinimumInterval, 0f, initial);
        float halfLife = Mathf.Max(0.01f, IntervalHalfLife);
        float decay = Mathf.Pow(0.5f, (float)Math.Max(0d, elapsedSeconds) / halfLife);
        return Mathf.Max(minimum, initial * decay);
    }
}
