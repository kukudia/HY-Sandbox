// 职责：将移动输入投影到主推进方向，其余输入推进生命周期复用 Thruster。
// 入口：FixedUpdate() → TickInputThrust()；保留 ApplyThrust() / ShouldActivate()。
using UnityEngine;

public class MainThruster : Thruster
{
    public bool alignVisual = true;
    public float rotationSpeed = 5f;
    public Vector3 inputDir;
    private void FixedUpdate()
    {
        TickInputThrust(ref inputDir);
    }

    protected override float CalculateInputThrust(Vector3 input, bool active)
    {
        thrustDirection = transform.forward;
        return active ? maxThrust * Mathf.Max(0f, Vector3.Dot(input, thrustDirection)) : 0f;
    }

    public void ApplyThrust()
    {
        ApplyDirectionalThrust();
    }

    public override bool ShouldActivate()
    {
        return ShouldActivateInputThrust(inputDir);
    }
}
