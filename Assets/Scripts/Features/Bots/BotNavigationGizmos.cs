// 职责：绘制最近一次避障采样、导航方向、速度和状态；仅显示，不修改机器人状态。
// 入口：Bot.OnDrawGizmos()；UnityEditor Handles 只在 Editor 编译分支使用。
using UnityEngine;

public static class BotNavigationGizmos
{
    public static void Draw(
        Transform transform,
        Transform navigateTarget,
        Rigidbody rb,
        Bot.NavigationState currentState,
        BotFlightMotion motion,
        bool showAvoidanceRays,
        bool showDirectionVectors)
    {
        // 3. 避障信息可视化
        if (showAvoidanceRays && motion.DebugHits.Count > 0)
        {
            foreach (var info in motion.DebugHits)
            {
                // 根据避障等级设置颜色
                switch (info.Level)
                {
                    case BotObstacleAvoidance.HazardLevel.Emergency:
                        Gizmos.color = Color.red;
                        break;
                    case BotObstacleAvoidance.HazardLevel.Primary:
                        Gizmos.color = Color.yellow;
                        break;
                    case BotObstacleAvoidance.HazardLevel.Predictive:
                        Gizmos.color = Color.green;
                        break;
                }

                // 绘制检测射线
                Gizmos.DrawLine(transform.position, info.HitPoint);
                // 绘制避障向量
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(info.HitPoint, info.AvoidanceVector * 0.9f);
                // 绘制命中点
                Gizmos.DrawSphere(info.HitPoint, 0.3f);
            }
        }

        // 4. 方向向量可视化
        if (showDirectionVectors && navigateTarget != null)
        {
            Vector3 targetDir = (navigateTarget.position - transform.position).normalized;
            // 目标方向（青色）
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, targetDir * 3f);
            // 避障方向（红色）
            if (motion.AvoidanceDirection.magnitude > 0.1f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, motion.AvoidanceDirection * 2.1f);
            }

            // 平滑后的实际方向（白色，粗线）
            Gizmos.color = Color.white;
            Gizmos.DrawRay(transform.position, motion.Direction * 2.4f);
            Gizmos.DrawSphere(transform.position + motion.Direction * 2.4f, 0.4f);
            // 当前速度方向（黄色）
            if (rb != null && rb.linearVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.yellow;
                float velocityLength = Mathf.Min(rb.linearVelocity.magnitude, 3f);
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * velocityLength);
            }
        }

#if UNITY_EDITOR
        Color labelColor = Color.white;
        switch (currentState)
        {
            case Bot.NavigationState.NavigatingToTarget:
                labelColor = Color.cyan;
                break;
            case Bot.NavigationState.ReturningHome:
                labelColor = Color.green;
                break;
            case Bot.NavigationState.Docking:
                labelColor = Color.yellow;
                break;
        }

        UnityEditor.Handles.Label(
transform.position + Vector3.up * 3f,
$"{currentState}",
new GUIStyle { fontSize = 14, normal = new GUIStyleState { textColor = labelColor } });
#endif
    }
}
