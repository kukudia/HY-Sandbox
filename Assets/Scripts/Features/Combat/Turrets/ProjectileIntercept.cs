// 职责：求解无重力、恒速炮弹与匀速目标的最早正交会时间。
// 入口：TryGetTime()；速度采用炮口继承速度的相对参考系，无解或超寿命时由调用者回退。
using System;
using UnityEngine;

internal static class ProjectileIntercept
{
    internal static bool TryGetTime(Vector3 offset, Vector3 relativeVelocity, float speed, float lifetime, out float time)
    {
        time = 0f;
        if (!(speed > 0f) || !(lifetime > 0f))
        {
            return false;
        }

        double a = Vector3.Dot(relativeVelocity, relativeVelocity) - (double)speed * speed;
        double b = 2.0 * Vector3.Dot(offset, relativeVelocity);
        double c = Vector3.Dot(offset, offset);
        double result;
        if (c < 1e-8)
        {
            return false;
        }

        if (Math.Abs(a) < 1e-6)
        {
            if (Math.Abs(b) < 1e-8)
            {
                return false;
            }

            result = -c / b;
        }
        else
        {
            double discriminant = b * b - 4.0 * a * c;
            if (discriminant < 0.0)
            {
                return false;
            }

            // 稳定求根，避免相近大数相减导致近距离交会解丢失精度。
            double root = Math.Sqrt(discriminant);
            double q = -0.5 * (b + (b >= 0.0 ? root : -root));
            double first = q / a;
            double second = Math.Abs(q) > 1e-12 ? c / q : double.PositiveInfinity;
            result = Math.Min(first > 0.0 ? first : double.PositiveInfinity, second > 0.0 ? second : double.PositiveInfinity);
        }

        if (!(result > 0.0) || result > lifetime || double.IsInfinity(result) || double.IsNaN(result))
        {
            return false;
        }

        time = (float)result;
        return true;
    }
}
