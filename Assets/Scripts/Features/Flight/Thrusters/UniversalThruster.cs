// 职责：按明确的推进朝向对齐喷口，按油门比例输出；转向未对准时不施加侧向残余推力。
// 入口：FixedUpdate() → TickInputThrust()；保留 ApplyThrust() / ShouldActivate()。
using UnityEngine;

public class UniversalThruster : Thruster
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
        Vector3 direction = controlUnit != null ? controlUnit.PropulsionDirection : input;
        RotateThruster(direction, HasValidRuntimeOwner() && direction.sqrMagnitude > InputEpsilonSqr);
        return active && IsAligned(direction) ? maxThrust * Mathf.Clamp01(input.magnitude) : 0f;
    }

    private bool IsAligned(Vector3 direction)
    {
        return !alignVisual || model == null || direction.sqrMagnitude < InputEpsilonSqr || Vector3.Dot(model.forward, direction.normalized) >= 0.995f;
    }

    protected override void ApplyDirectionalThrust()
    {
        // 外部调用 ApplyThrust() 时也不能在喷口未对准的情况下施力。
        if (IsAligned(controlUnit != null ? controlUnit.PropulsionDirection : inputDir))
        {
            base.ApplyDirectionalThrust();
        }
    }

    private void RotateThruster(Vector3 worldDir, bool active)
    {
        if (!alignVisual || !active || model == null)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
        model.rotation = Quaternion.RotateTowards(model.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime * 60f);
    }

    protected override void AlignGuidanceVisual(Vector3 worldDirection)
    {
        CacheLocalReferences();
        RotateThruster(worldDirection, true);
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
