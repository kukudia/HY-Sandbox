// 职责：按移动输入转动通用推进模型，其余输入推进生命周期复用 Thruster。
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
        RotateThruster(input, active);
        return base.CalculateInputThrust(input, active);
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

    public void ApplyThrust()
    {
        ApplyDirectionalThrust();
    }

    public override bool ShouldActivate()
    {
        return ShouldActivateInputThrust(inputDir);
    }
}
