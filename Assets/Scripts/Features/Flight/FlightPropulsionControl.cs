// 职责：把参考速度转成独立的喷口朝向、比例油门与显式制动，不把细小速度误差当成直航方向。
// 入口：Evaluate()；已确认的供电能力与刚体阻尼提供前馈，真实施力仍由推进器执行。
// 生命周期：每舰独立持有油门与制动迟滞，停用/重组Reset()；不直接写刚体。
using UnityEngine;

internal sealed class FlightPropulsionControl
{
    private static readonly Unity.Profiling.ProfilerMarker CapacityMarker = new("HY.Flight.AvailableAcceleration");
    internal readonly struct Command
    {
        internal readonly Vector3 Direction;
        internal readonly float Throttle;
        internal Command(Vector3 direction, float throttle)
        {
            Direction = direction;
            Throttle = throttle;
        }
    }

    private float throttle;
    private bool turning;
    private Vector3 cachedCapacityDirection;
    private float cachedCapacity;
    private float capacityCacheTime = float.NegativeInfinity;
    private const float CapacityCacheDuration = 0.08f;
    internal bool Braking
    {
        get; private set;
    }

    internal void Reset()
    {
        throttle = 0f;
        turning = Braking = false;
        capacityCacheTime = float.NegativeInfinity;
    }

    internal Command Evaluate(
        ControlUnit owner,
        Rigidbody body,
        Vector3 reference,
        bool requestBrake,
        float brakingAcceleration,
        float response,
        float deltaTime)
    {
        if (body == null)
        {
            return new Command(Vector3.zero, 0f);
        }

        Vector3 velocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        reference.y = 0f;
        float speed = velocity.magnitude;
        float targetSpeed = reference.magnitude;
        Vector3 heading = targetSpeed > 0.05f ? reference / targetSpeed : owner.PropulsionDirection;
        float alongSpeed = Vector3.Dot(velocity, heading);
        bool stopping = targetSpeed < 0.05f;
        // 正常巡航有明确的超速迟滞，不能在零误差附近反复翻转喷口来维持速度。
        Braking = speed > 0.15f && (stopping || requestBrake && speed > targetSpeed + 0.1f || alongSpeed > targetSpeed + (Braking ? 0.2f : 0.75f));
        Vector3 acceleration;
        if (Braking)
        {
            heading = -velocity / speed;
            acceleration = heading * Mathf.Max(0.1f, brakingAcceleration);
            turning = false;
        }
        else if (stopping)
        {
            throttle = 0f;
            turning = false;
            return new Command(heading, 0f);
        }
        else
        {
            float angle = speed > 0.5f ? Vector3.Angle(velocity, heading) : 0f;
            turning = angle > (turning ? 8f : 25f);
            // PhysX每步先加力再阻尼，前馈补偿稳态阻力，避免比例油门造成持续欠速。
            float damping = Mathf.Max(0f, body.linearDamping);
            float drag = damping / Mathf.Max(0.1f, 1f - damping * deltaTime);
            if (turning)
            {
                acceleration = reference * drag + reference - velocity;
                heading = acceleration.normalized;
            }
            else
            {
                acceleration = heading * Mathf.Max(0f, targetSpeed * drag + targetSpeed - alongSpeed);
            }
        }

        float capacity = GetAvailableAcceleration(owner, heading, body.mass);
        // 空闲供电状态默认可尝试启动；确认全部缺电时也保持请求，避免恢复后永远无法重启。
        float requested = capacity > 0.001f ? Mathf.Clamp01(acceleration.magnitude / capacity) : acceleration.sqrMagnitude > 0.0001f ? 1f : 0f;
        throttle = Braking ? requested : response > 0f ? Mathf.MoveTowards(throttle, requested, response * deltaTime) : 0f;
        return new Command(heading, throttle);
    }

    private float GetAvailableAcceleration(ControlUnit owner, Vector3 direction, float mass)
    {
        float now = Time.fixedTime;
        bool directionChanged = cachedCapacityDirection.sqrMagnitude < 0.001f || Vector3.Dot(cachedCapacityDirection, direction) < 0.995f;
        if (directionChanged || now - capacityCacheTime >= CapacityCacheDuration)
        {
            using var sample = CapacityMarker.Auto();
            cachedCapacityDirection = direction;
            cachedCapacity = AvailableAcceleration(owner, direction, mass);
            capacityCacheTime = now;
        }

        return cachedCapacity;
    }

    private static float AvailableAcceleration(ControlUnit owner, Vector3 direction, float mass)
    {
        float force = 0f;
        foreach (Thruster thruster in owner.Thrusters)
        {
            if (thruster == null || !thruster.isActiveAndEnabled || thruster.controlUnit != owner)
            {
                continue;
            }

            // 使用已确认的供电比例，不把已断电的喷口计入可用推进能力。
            float available = Mathf.Max(0f, thruster.maxThrust) * Mathf.Clamp01(thruster.PowerSupplyEfficiency);
            if (thruster is UniversalThruster universal && universal.alignVisual)
            {
                force += available;
            }
            else if (thruster is MainThruster || thruster is UniversalThruster)
            {
                Vector3 inputAxis = thruster is MainThruster ? thruster.transform.forward : direction;
                Vector3 forceAxis = thruster.model != null ? thruster.model.forward : thruster.transform.forward;
                float activation = Mathf.Max(0f, Vector3.Dot(inputAxis, direction));
                float projection = Mathf.Max(0f, Vector3.Dot(forceAxis, direction));
                force += available * activation * projection;
            }
        }

        return force / Mathf.Max(0.01f, mass);
    }
}
