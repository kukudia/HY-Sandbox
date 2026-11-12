// 职责：周向采集远场路况，沿路线预读距离/时间，按相对运动扫掠近场候选航迹并提前调速或让行。
// 入口：Choose()；由EnemyController降频调用，不写刚体、不分配候选数组，不使用试验场运动脚本的未来参数。
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class EnemyNavigationPrediction
{
    private const int Capacity = 256;
    private const int Steps = 12;
    private readonly Collider[] overlaps = new Collider[Capacity];
    private readonly Observation[] observations = new Observation[Capacity];
    private Vector3 committed;
    private float holdUntil;
    internal int Revision
    {
        get; private set;
    }
    internal string ThreatName
    {
        get; private set;
    }
    internal Vector3 ThreatVelocity
    {
        get; private set;
    }
    internal float SensorRadius
    {
        get; private set;
    }
    internal float LookAheadDistance
    {
        get; private set;
    }
    internal float FarObstacleDistance { get; private set; } = -1f;
    internal float FarArrivalTime { get; private set; } = -1f;
    internal string FarThreat
    {
        get; private set;
    }
    internal string FarDecision { get; private set; } = "Clear";
    internal float MinimumDistance { get; set; } = 80f;
    internal float BodyLengths { get; set; } = 8f;
    internal float LookAheadSeconds { get; set; } = 10f;
    internal ControlUnit TaskTarget
    {
        get; set;
    }
    internal string Decision { get; private set; } = "Idle";
    internal int Observed
    {
        get; private set;
    }
    internal int Moving
    {
        get; private set;
    }
    internal int Candidates
    {
        get; private set;
    }
    internal int SegmentChecks
    {
        get; private set;
    }
    internal float CollisionTime { get; private set; } = -1f;
    internal float SelectedCollisionTime { get; private set; } = -1f;
    internal float Milliseconds
    {
        get; private set;
    }
    internal bool Saturated
    {
        get; private set;
    }
    internal Vector3 SelectedVelocity
    {
        get; private set;
    }

    private struct Observation
    {
        internal Collider Source;
        internal Bounds Bounds;
        internal Vector3 Velocity;
        internal float AngularSpeed;
        internal float Radius;
    }

    private struct Outcome
    {
        internal Vector3 Input;
        internal float HitTime;
        internal float Progress;
        internal float Clearance;
        internal int HitIndex;
    }

    internal void Clear()
    {
        committed = Vector3.zero;
        holdUntil = 0f;
        Observed = 0;
        Moving = 0;
        Candidates = 0;
        SegmentChecks = 0;
        CollisionTime = -1f;
        SelectedCollisionTime = -1f;
        SelectedVelocity = Vector3.zero;
        Milliseconds = 0f;
        Saturated = false;
        Decision = "Idle";
        ThreatName = null;
        ThreatVelocity = Vector3.zero;
        SensorRadius = 0f;
        Revision++;
        System.Array.Clear(overlaps, 0, overlaps.Length);
        System.Array.Clear(observations, 0, observations.Length);
        FarObstacleDistance = -1f;
        FarArrivalTime = -1f;
        FarThreat = null;
        FarDecision = "Clear";
    }

    internal Vector3 Choose(
        ControlUnit owner,
        Bounds hull,
        Vector3 velocity,
        Vector3 desired,
        float targetHeight,
        LayerMask layers,
        float cruiseSpeed,
        float horizon,
        float acceleration,
        float turnRate,
        bool preserveCourse,
        Vector3 destination,
        System.Collections.Generic.IReadOnlyList<Vector3> route,
        int waypoint)
    {
        double began = Time.realtimeSinceStartupAsDouble;
        horizon = Mathf.Clamp(horizon, 1f, 4f);
        acceleration = Mathf.Clamp(acceleration, 0.25f, 12f);
        turnRate = Mathf.Clamp(turnRate, 10f, 180f);
        Candidates = 0;
        SegmentChecks = 0;
        Revision++;
        Capture(
            owner,
            hull,
            velocity,
            layers,
            cruiseSpeed,
            horizon);
        Outcome original = Evaluate(
            hull,
            velocity,
            desired,
            targetHeight - owner.transform.position.y,
            cruiseSpeed,
            horizon,
            acceleration,
            turnRate);
        CollisionTime = original.HitTime;
        ThreatName = original.HitIndex >= 0 ? observations[original.HitIndex].Source.name : null;
        ThreatVelocity = original.HitIndex >= 0 ? observations[original.HitIndex].Velocity : Vector3.zero;
        Outcome best = original;
        Decision = "Continue";
        if (Saturated)
        {
            best.Input = Vector3.zero;
            Decision = "SensorSaturated";
        }

        // 远场沿已规划路线采样，路线外才看任务方向；远期运动只作为风险信息，不假装恒速预测可信到十几秒。
        Preview(
            hull,
            destination + hull.center - owner.transform.position,
            route,
            waypoint,
            owner.transform.position,
            targetHeight,
            cruiseSpeed);
        if (!Saturated && CollisionTime < 0f && FarObstacleDistance > 0f && FarDecision == "CrossingWatch")
        {
            float ratio = Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(FarArrivalTime / Mathf.Max(1f, LookAheadSeconds)));
            Outcome cautious = Evaluate(
                hull,
                velocity,
                best.Input * ratio,
                targetHeight - owner.transform.position.y,
                cruiseSpeed,
                horizon,
                acceleration,
                turnRate);
            if (cautious.HitTime < 0f)
            {
                best = cautious;
                Decision = "EarlySlow";
            }
        }
        else if (!Saturated && original.HitTime >= 0f)
        {
            // 先比较降速/让行，再比较左右小角和大角绕行；保持原方向优先，避免见障就换边。
            Consider(
                ref best,
                Evaluate(
                    hull,
                    velocity,
                    desired * 0.4f,
                    targetHeight - owner.transform.position.y,
                    cruiseSpeed,
                    horizon,
                    acceleration,
                    turnRate),
                desired);
            Consider(
                ref best,
                Evaluate(
                    hull,
                    velocity,
                    Vector3.zero,
                    targetHeight - owner.transform.position.y,
                    cruiseSpeed,
                    horizon,
                    acceleration,
                    turnRate),
                desired);
            Vector3 heading = desired.sqrMagnitude > 0.001f ? desired.normalized : Vector3.ProjectOnPlane(velocity, Vector3.up).normalized;
            bool movingThreat = original.HitIndex >= 0 && (observations[original.HitIndex].Velocity.sqrMagnitude > 0.01f || observations[original.HitIndex].AngularSpeed > 0.01f);
            // 静态路线由A*/升降控制负责，局部预测以降速为主，避免第二个绕行器持续抢夺航向。
            for (int i = 0; i < ((!preserveCourse || movingThreat) ? 4 : 0); i++)
            {
                float angle = (i < 2 ? 35f : 70f) * (i % 2 == 0 ? 1f : -1f);
                Vector3 input = Quaternion.AngleAxis(angle, Vector3.up) * heading * Mathf.Max(0.5f, desired.magnitude);
                Consider(
                    ref best,
                    Evaluate(
                        hull,
                        velocity,
                        input,
                        targetHeight - owner.transform.position.y,
                        cruiseSpeed,
                        horizon,
                        acceleration,
                        turnRate),
                    desired);
            }

            Decision = best.Input.sqrMagnitude < 0.001f ? "Yield" : Vector3.Angle(best.Input, desired) > 10f ? "Bypass" : "Slow";
            if (best.HitTime >= 0f)
            {
                Decision = "NoSafeCandidate";
            }
        }

        if (best.Input.sqrMagnitude > 0.001f && Vector3.Angle(best.Input, desired) > 10f)
        {
            committed = best.Input.normalized;
            holdUntil = Time.time + 1f;
        }

        SelectedCollisionTime = best.HitTime;
        SelectedVelocity = best.Input * cruiseSpeed;
        Milliseconds = (float)((Time.realtimeSinceStartupAsDouble - began) * 1000.0);
        return best.Input;
    }

    private void Capture(
        ControlUnit owner,
        Bounds hull,
        Vector3 velocity,
        LayerMask layers,
        float cruiseSpeed,
        float horizon)
    {
        Observed = 0;
        Moving = 0;
        System.Array.Clear(observations, 0, observations.Length);
        // 周向采集覆盖候选动作和横向驶入者，而非只向当前速度发射射线。额外24米为外部运动余量。
        LookAheadDistance = Mathf.Clamp(
            Mathf.Max(
                MinimumDistance,
                Mathf.Max(hull.size.x, hull.size.z) * BodyLengths,
                Mathf.Max(velocity.magnitude, cruiseSpeed) * LookAheadSeconds),
            20f,
            160f);
        float reach = Mathf.Max(LookAheadDistance, Mathf.Max(velocity.magnitude, cruiseSpeed) * horizon) + 24f;
        SensorRadius = reach;
        int count = owner.gameObject.scene.GetPhysicsScene().OverlapBox(
            hull.center,
            hull.extents + Vector3.one * reach,
            overlaps,
            Quaternion.identity,
            layers,
            QueryTriggerInteraction.Ignore);
        Saturated = count == overlaps.Length;
        for (int i = 0; i < count; i++)
        {
            Collider collider = overlaps[i];
            if (collider.transform.IsChildOf(owner.transform) || (TaskTarget != null && collider.transform.IsChildOf(TaskTarget.transform)))
            {
                continue;
            }

            Rigidbody body = collider.attachedRigidbody;
            Bounds bounds = collider.bounds;
            Vector3 motion = body != null ? body.GetPointVelocity(bounds.center) : Vector3.zero;
            float angular = body != null ? body.angularVelocity.magnitude : 0f;
            // 贴地悬浮的初始小误差不能把地板变成水平死路；升降轨迹仍在Evaluate逐段检查。
            observations[Observed++] = new Observation
            {
                Source = collider,
                Bounds = bounds,
                Velocity = motion,
                AngularSpeed = angular,
                Radius = bounds.extents.magnitude
            };
            if (motion.sqrMagnitude > 0.01f || angular > 0.01f)
            {
                Moving++;
            }
        }

        System.Array.Clear(overlaps, 0, count);
    }

    private void Preview(
        Bounds hull,
        Vector3 destination,
        System.Collections.Generic.IReadOnlyList<Vector3> route,
        int waypoint,
        Vector3 root,
        float height,
        float speed)
    {
        FarObstacleDistance = -1f;
        FarArrivalTime = -1f;
        FarThreat = null;
        FarDecision = "Clear";
        Vector3 from = hull.center;
        Vector3 offset = hull.center - root;
        float travelled = 0f;
        // 远场最多四段，净空检查不再额外访问物理场景。
        for (int segment = 0; segment < 4 && travelled < LookAheadDistance; segment++)
        {
            bool onRoute = route != null && waypoint + segment < route.Count;
            Vector3 to = onRoute ? route[waypoint + segment] + offset : destination;
            if (!onRoute)
            {
                to.y = height + offset.y;
            }

            Vector3 delta = Vector3.ClampMagnitude(to - from, LookAheadDistance - travelled);
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                break;
            }

            for (int i = 0; i < Observed; i++)
            {
                Observation obstacle = observations[i];
                Bounds bounds = obstacle.Bounds;
                bool moving = obstacle.Velocity.sqrMagnitude > 0.01f || obstacle.AngularSpeed > 0.01f;
                if (moving && obstacle.AngularSpeed < 0.01f && Vector3.Dot(obstacle.Velocity, delta / length) >= speed && Vector3.Dot(bounds.center - from, delta) > 0f)
                {
                    // 前方同向更快的物体正在拉开距离，不能因远期占道包络而错误让行。
                    continue;
                }

                if (bounds.max.y <= hull.min.y + 0.15f && !moving && delta.y >= -0.05f)
                {
                    continue;
                }

                if (moving)
                {
                    Bounds future = bounds;
                    future.center += Vector3.ClampMagnitude(obstacle.Velocity * 3f, 24f);
                    bounds.Encapsulate(future);
                    bounds.Expand(Mathf.Min(8f, obstacle.AngularSpeed * 3f * obstacle.Radius) * 2f);
                }

                bounds.Expand((hull.extents + Vector3.one * 0.5f) * 2f);
                SegmentChecks++;
                if (bounds.Contains(from))
                {
                    continue;
                }

                if (bounds.IntersectRay(new Ray(from, delta / length), out float hit) && hit <= length && (FarObstacleDistance < 0f || travelled + hit < FarObstacleDistance))
                {
                    FarObstacleDistance = travelled + hit;
                    FarArrivalTime = FarObstacleDistance / Mathf.Max(1f, speed);
                    FarThreat = obstacle.Source.name;
                    FarDecision = moving ? "CrossingWatch" : "RouteBlocked";
                }
            }

            travelled += length;
            from += delta;
            if (!onRoute)
            {
                break;
            }
        }
    }

    private Outcome Evaluate(
        Bounds hull,
        Vector3 velocity,
        Vector3 input,
        float deltaHeight,
        float speed,
        float horizon,
        float acceleration,
        float turnRate)
    {
        Candidates++;
        var outcome = new Outcome
        {
            Input = input,
            HitTime = -1f,
            Clearance = 10f,
            HitIndex = -1
        };
        Vector3 position = hull.center;
        Vector3 horizontal = Vector3.ProjectOnPlane(velocity, Vector3.up);
        Vector3 target = input * speed;
        float vertical = velocity.y;
        float step = horizon / Steps;
        Vector3 padding = hull.extents + new Vector3(0.35f, 0.1f, 0.35f);
        for (int sample = 0; sample < Steps; sample++)
        {
            float time = sample * step;
            // 模拟有限转向和速度响应，不能把高速舰假定为立即停下或瞬间侧移。
            Vector3 nextVelocity = Vector3.RotateTowards(horizontal, target, turnRate * Mathf.Deg2Rad * step, acceleration * step);
            float desiredVertical = Mathf.Clamp((hull.center.y + deltaHeight - position.y) * 0.5f, -3f, 3f);
            vertical = Mathf.MoveTowards(vertical, desiredVertical, 2f * step);
            Vector3 next = position + (horizontal + nextVelocity) * (step * 0.5f) + Vector3.up * vertical * step;
            for (int i = 0; i < Observed; i++)
            {
                Observation obstacle = observations[i];
                // 仅跳过贴地且没有继续下降的地面接触，向上/侧面障碍不能用此规则忽略。
                if (obstacle.Bounds.max.y <= hull.min.y + 0.15f && obstacle.Velocity.sqrMagnitude < 0.001f && next.y >= hull.center.y - 0.05f)
                {
                    continue;
                }

                Bounds expanded = obstacle.Bounds;
                float rotation = Mathf.Min(8f, obstacle.AngularSpeed * (time + step) * obstacle.Radius);
                expanded.Expand((padding + Vector3.one * rotation) * 2f);
                Vector3 from = position - obstacle.Velocity * time;
                Vector3 to = next - obstacle.Velocity * (time + step);
                SegmentChecks++;
                float before = Clearance(expanded, from);
                float after = Clearance(expanded, to);
                outcome.Clearance = Mathf.Min(outcome.Clearance, Mathf.Max(0f, after));
                // 初始重叠允许单调脱离，禁止再深入；否则任何小包络误差都会永久锁死。
                if (before < 0f && after > before + 0.001f)
                {
                    continue;
                }

                Vector3 delta = to - from;
                float length = delta.magnitude;
                bool intersects = before <= 0f || after <= 0f;
                float fraction = 0f;
                if (!intersects && length > 0.0001f && expanded.IntersectRay(new Ray(from, delta / length), out float distance) && distance <= length)
                {
                    intersects = true;
                    fraction = distance / length;
                }

                if (intersects)
                {
                    outcome.HitTime = time + fraction * step;
                    outcome.HitIndex = i;
                    return outcome;
                }
            }

            horizontal = nextVelocity;
            position = next;
        }

        outcome.Progress = Vector3.Dot(position - hull.center, input.sqrMagnitude > 0.001f ? input.normalized : Vector3.zero);
        return outcome;
    }

    private void Consider(ref Outcome best, Outcome candidate, Vector3 desired)
    {
        if (candidate.HitTime >= 0f)
        {
            if (best.HitTime >= 0f && candidate.HitTime > best.HitTime + 0.1f)
            {
                best = candidate;
            }

            return;
        }

        if (best.HitTime >= 0f || Score(candidate, desired) > Score(best, desired) + 0.5f)
        {
            best = candidate;
        }
    }

    private float Score(Outcome candidate, Vector3 desired)
    {
        float alignment = Vector3.Dot(candidate.Input.normalized, desired.normalized);
        float continuity = Time.time < holdUntil ? Vector3.Dot(candidate.Input.normalized, committed) * 2f : 0f;
        return candidate.Progress * Mathf.Max(0f, alignment) + candidate.Clearance * 0.5f + continuity - (1f - alignment) * 3f;
    }

    private static float Clearance(Bounds bounds, Vector3 point)
    {
        Vector3 distance = new Vector3(
            Mathf.Abs(point.x - bounds.center.x),
            Mathf.Abs(point.y - bounds.center.y),
            Mathf.Abs(point.z - bounds.center.z)) - bounds.extents;
        if (distance.x <= 0f && distance.y <= 0f && distance.z <= 0f)
        {
            return Mathf.Max(distance.x, Mathf.Max(distance.y, distance.z));
        }

        return new Vector3(Mathf.Max(0f, distance.x), Mathf.Max(0f, distance.y), Mathf.Max(0f, distance.z)).magnitude;
    }
}
