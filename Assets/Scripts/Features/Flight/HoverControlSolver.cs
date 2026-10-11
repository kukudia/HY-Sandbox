// 职责：保存高度 PID 历史，计算高度需求、重力补偿、倾斜及旋转修正建议。
// 入口：StepHeight() / CalculateTilt() / TryCalculateAngularVelocity()；不读取组件或写刚体。
// 生命周期：停止输出及切换运行归属时 Reset()，不将旧积分带入新物理会话。
using UnityEngine;

internal sealed class HoverControlSolver
{
    internal readonly struct TiltCorrection
    {
        internal readonly float Force;
        internal readonly Vector3 Direction;
        internal bool IsActive => Force > 1e-5f;

        internal TiltCorrection(float force, Vector3 direction)
        {
            Force = force;
            Direction = direction;
        }
    }

    private float _heightIntegral;
    private float _lastHeightError;
    internal void Reset()
    {
        _heightIntegral = 0f;
        _lastHeightError = 0f;
    }

    internal float StepHeight(
        float heightError,
        float proportionalGain,
        float integralGain,
        float derivativeGain,
        float deltaTime)
    {
        _heightIntegral += heightError * deltaTime;
        float inverseDeltaTime = deltaTime > 1e-5f ? 1f / deltaTime : 0f;
        float derivative = (heightError - _lastHeightError) * inverseDeltaTime;
        _lastHeightError = heightError;
        return heightError * proportionalGain + _heightIntegral * integralGain + derivative * derivativeGain;
    }

    internal static float CalculateGravityCompensation(
        float mass,
        float gravityMagnitude,
        float compensationFactor,
        float absoluteHeightError,
        float heightTolerance)
    {
        float gravityForce = mass * gravityMagnitude * compensationFactor;
        float safeHeightTolerance = Mathf.Max(heightTolerance, 1e-5f);
        return gravityForce * Mathf.Clamp01(absoluteHeightError / safeHeightTolerance);
    }

    internal static TiltCorrection CalculateTilt(
        Vector3 currentUp,
        Vector3 angularVelocity,
        float maximumTiltCos,
        float proportionalGain,
        float derivativeGain)
    {
        float upDot = Mathf.Clamp(Vector3.Dot(currentUp, Vector3.up), -1f, 1f);
        if (upDot >= maximumTiltCos)
        {
            return default;
        }

        Vector3 direction = Vector3.Cross(currentUp, Vector3.up);
        float directionSqr = direction.sqrMagnitude;
        if (directionSqr <= 1e-6f)
        {
            return default;
        }

        direction /= Mathf.Sqrt(directionSqr);
        float angularSpeed = Mathf.Abs(Vector3.Dot(angularVelocity, direction));
        float force = Mathf.Acos(upDot) * proportionalGain + angularSpeed * derivativeGain;
        return new TiltCorrection(force, direction);
    }

    internal static bool TryCalculateAngularVelocity(
        Quaternion rotation,
        Vector3 currentUp,
        float rotationSmoothing,
        float deltaTime,
        float outputEfficiency,
        out Vector3 velocity)
    {
        velocity = Vector3.zero;
        if (rotationSmoothing <= 0f || Vector3.Dot(currentUp, Vector3.up) > 0.99995f)
        {
            return false;
        }

        Quaternion targetRotation = Quaternion.FromToRotation(currentUp, Vector3.up) * rotation;
        Quaternion newRotation = Quaternion.Slerp(rotation, targetRotation, rotationSmoothing * deltaTime);
        Quaternion rotationDelta = newRotation * Quaternion.Inverse(rotation);
        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
        {
            angle -= 360f;
        }

        if (Mathf.Abs(angle) <= 0.01f)
        {
            return false;
        }

        float inverseDeltaTime = deltaTime > 1e-5f ? 1f / deltaTime : 0f;
        velocity = axis * angle * Mathf.Deg2Rad * inverseDeltaTime * outputEfficiency;
        return true;
    }
}
