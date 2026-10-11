// 职责：从 Home 的实时停靠点向外探测净空，选取兼顾净空和机器人方位的返航方向。
// 入口：Evaluate()；返回世界方向与速度建议，不执行飞行或停靠。
// 不变量：忽略机器人与 Home 层级碰撞体；射线缓冲属于当前机器人。
using UnityEngine;

public sealed class BotHomeGuidance
{
    public struct Settings
    {
        public LayerMask ObstacleMask;
        public int RayCount;
        public float Range;
        public float AlignmentWeight;
    }

    private const int MaxHits = 16;
    private readonly Transform _transform;
    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
    private Transform _home;
    private Vector3 _homeOffset;
    private Settings _settings;
    public BotHomeGuidance(Transform transform)
    {
        _transform = transform;
    }

    public BotAvoidanceResult Evaluate(Transform home, Vector3 homeOffset, Vector3 targetDirection, float rangeScale, Settings settings)
    {
        _home = home;
        _homeOffset = homeOffset;
        _settings = settings;
        if (_home == null || rangeScale <= 0.01f)
        {
            return new BotAvoidanceResult
            {
                SpeedMultiplier = 1f
            };
        }

        Vector3 homeOrigin = _home.TransformPoint(_homeOffset);
        Vector3 homeToBot = _transform.position - homeOrigin;
        Vector3 up = _home.up.sqrMagnitude > 0.001f ? _home.up.normalized : Vector3.up;
        Vector3 preferredApproach = Vector3.ProjectOnPlane(homeToBot, up);
        if (preferredApproach.sqrMagnitude < 0.001f)
        {
            preferredApproach = homeToBot.sqrMagnitude > 0.001f ? homeToBot.normalized : targetDirection.sqrMagnitude > 0.001f ? -targetDirection : _home.forward;
        }

        preferredApproach.Normalize();
        float rayRange = Mathf.Max(0.5f, _settings.Range * rangeScale);
        int rayCount = Mathf.Clamp(_settings.RayCount, 4, MaxHits);
        float bestScore = float.NegativeInfinity;
        Vector3 bestApproach = preferredApproach;
        float bestClearFraction = 0f;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = 360f * i / rayCount;
            Vector3 candidateApproach = Quaternion.AngleAxis(angle, up) * preferredApproach;
            float clearFraction = GetHomeGuidanceClearFraction(homeOrigin, candidateApproach, rayRange);
            float alignment = Mathf.Clamp01((Vector3.Dot(candidateApproach, preferredApproach) + 1f) * 0.5f);
            float score = clearFraction + alignment * _settings.AlignmentWeight;
            if (score > bestScore)
            {
                bestScore = score;
                bestApproach = candidateApproach;
                bestClearFraction = clearFraction;
            }
        }

        // bestApproach 指向从 Home 接近 RepairBot 的安全方向，RepairBot 应沿反方向回家。
        Vector3 guidedReturnDirection = -bestApproach;
        float guidanceStrength = Mathf.Clamp01((1f - Vector3.Dot(targetDirection, guidedReturnDirection)) * 0.5f);
        return new BotAvoidanceResult
        {
            Direction = guidedReturnDirection,
            Strength = guidanceStrength,
            SpeedMultiplier = Mathf.Lerp(0.45f, 1f, bestClearFraction),
            ObstacleCount = bestClearFraction < 0.99f ? 1 : 0
        };
    }

    private float GetHomeGuidanceClearFraction(Vector3 origin, Vector3 direction, float range)
    {
        int hitCount = Physics.RaycastNonAlloc(
            origin + direction * 0.05f,
            direction,
            _hits,
            range,
            _settings.ObstacleMask,
            QueryTriggerInteraction.Ignore);
        float nearestDistance = range;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hits[i];
            if (IsIgnoredHomeGuidanceCollider(hit.collider))
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, hit.distance);
        }

        return Mathf.Clamp01(nearestDistance / Mathf.Max(range, 0.01f));
    }

    private bool IsIgnoredHomeGuidanceCollider(Collider collider)
    {
        if (collider == null || collider.transform == _transform || collider.transform.IsChildOf(_transform))
        {
            return true;
        }

        if (_home == null)
        {
            return false;
        }

        Transform colliderTransform = collider.transform;
        return colliderTransform == _home || colliderTransform.IsChildOf(_home) || _home.IsChildOf(colliderTransform);
    }
}
