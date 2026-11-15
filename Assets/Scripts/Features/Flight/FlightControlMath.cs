// 职责：提供悬浮姿态与自主航迹共用的帧率无关响应计算，不读取场景或写刚体。
// 入口：ResponseBlend()/StepCourse()；调用者持有各自状态并决定紧急动作策略。
using UnityEngine;

internal static class FlightControlMath
{
    internal static float ResponseBlend(float response, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, response) * Mathf.Max(0f, deltaTime));
    }

    internal static Vector3 StepCourse(Vector3 current, Vector3 desired, float turnRate, float acceleration, float deltaTime)
    {
        current.y = desired.y = 0f;
        float speed = Mathf.MoveTowards(current.magnitude, desired.magnitude, Mathf.Max(0f, acceleration) * deltaTime);
        if (desired.sqrMagnitude < 0.0001f)
        {
            return current.normalized * speed;
        }

        if (current.sqrMagnitude < 0.0001f)
        {
            return desired.normalized * speed;
        }

        // 水平面最短角转弯，单独限制航速变化，折返不因向量插值穿过零速。
        float angle = Vector3.SignedAngle(current, desired, Vector3.up);
        float step = Mathf.Clamp(angle, -turnRate * deltaTime, turnRate * deltaTime);
        return Quaternion.AngleAxis(step, Vector3.up) * current.normalized * speed;
    }
}
