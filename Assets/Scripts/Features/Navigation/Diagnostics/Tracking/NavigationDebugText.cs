// 职责：把真实导航快照与轨迹指标格式化为可读的追踪报告；不额外查询物理或影响驾驶。
// 入口：Build()生成降频缓存文本；Mode()也用于场景标签。
using System.Text;
using UnityEngine;

public static class NavigationDebugText
{
    public static string Mode(EnemyNavigationSnapshot value)
    {
        if (!value.controllerActive)
        {
            return "INACTIVE";
        }

        return value.followingPath ? "A* FOLLOW" : value.changingHeight ? "ALTITUDE BYPASS" : value.localDetouring ? "LOCAL DETOUR" : value.hasTarget ? "ENGAGE / APPROACH" : "NO TARGET";
    }

    public static string Build(NavigationTrack track)
    {
        EnemyNavigationSnapshot s = track.Snapshot;
        var text = new StringBuilder(1600);
        text.AppendLine(track.Label ?? "Waiting for round");
        text.AppendLine(track.Alive ? Mode(s) : "DESTROYED / last recorded sample");
        text.Append("Target: ").AppendLine(s.hasTarget ? s.targetName : "none");
        text.Append("Range: ").Append(s.hasTarget ? Vector3.Distance(s.position, s.targetPosition).ToString("F1") + "m" : "--");
        text.Append("  Speed: ").Append(s.velocity.magnitude.ToString("F1")).AppendLine("m/s");
        text.Append("Engage/acquire: ").Append(s.engagementDistance.ToString("F0")).Append(" / ").Append(s.acquisitionDistance.ToString("F0")).AppendLine("m");
        text.Append("Elapsed: ").Append(track.Elapsed.ToString("F1")).Append("s  Travel: ").Append(track.Travel.ToString("F1")).AppendLine("m");
        text.Append("Stopped (<1m horizontal gain): ").Append(track.StoppedSeconds.ToString("F1")).AppendLine("s");
        text.Append("Contacts: ").Append(track.Contacts).Append("  Turns: ").Append(s.turns).Append("  Trace: ").Append(track.TracePoints).AppendLine(" pts");
        text.Append("Position: ").Append(s.position.ToString("F1")).AppendLine();
        text.Append("Height: ").Append(s.position.y.ToString("F1")).Append(" -> ").Append(s.targetHeight.ToString("F1")).AppendLine("m");
        text.Append("Desired: ").Append(s.desiredInput.ToString("F2")).AppendLine();
        text.Append("Applied: ").Append(s.appliedInput.ToString("F2")).AppendLine();
        text.Append("Reference: ").Append(s.referenceVelocity.magnitude.ToString("F1")).Append("m/s  Throttle: ").Append(s.throttleRatio.ToString("F2")).AppendLine();
        text.Append("Brake: ").Append(s.predictiveBraking).Append("  Urgent: ").Append(s.urgent).Append("  Probe: ").Append(s.probeDistance.ToString("F1")).AppendLine("m");
        text.Append("A*: ").Append(s.pathStatus).Append("  ").AppendLine(s.replanReason);
        text.Append("Waypoint: ").Append(s.waypoint).Append("  Nodes: ").Append(s.expandedNodes).Append("  Plan: ").Append(s.planMilliseconds.ToString("F2")).AppendLine("ms");
        text.Append("Predict: ").Append(s.predictionDecision).Append("  ").Append(s.predictionMilliseconds.ToString("F2")).AppendLine("ms");
        text.Append("Threat: ").AppendLine(string.IsNullOrEmpty(s.predictedThreat) ? "none" : s.predictedThreat);
        text.Append("TTC original/selected: ").Append(TimeText(s.predictedCollisionTime)).Append(" / ").AppendLine(TimeText(s.selectedCollisionTime));
        text.Append("Obstacles: ").Append(s.observedObstacles).Append("  Moving: ").Append(s.movingObstacles).Append("  Candidates: ").Append(s.predictionCandidates).AppendLine();
        text.Append("Far: ").Append(s.lookAheadDistance.ToString("F0")).Append("m  ").AppendLine(s.farDecision);
        text.Append("Far threat: ").AppendLine(string.IsNullOrEmpty(s.farThreat) ? "none" : s.farThreat);
        text.Append("Sensor: ").Append(s.sensorColliders).Append(" / ").Append(s.sensorCapacity).Append(" colliders; full: ").Append(s.predictionSaturated).AppendLine();
        text.Append("Local queries: ").Append(s.localQueryCount).Append("  Full: ").Append(s.localSaturated).AppendLine();
        text.Append("Speed trace: dark blue 0 -> cyan ").Append(track.TraceFastSpeed.ToString("F1")).Append("m/s; orange = future route");
        return text.ToString();
    }

    private static string TimeText(float seconds) => seconds < 0f ? "clear" : seconds.ToString("F1") + "s";
}
