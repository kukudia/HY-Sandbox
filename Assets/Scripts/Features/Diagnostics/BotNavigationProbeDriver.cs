// 职责：在探针物理帧调用生产导航并记录到达、碰撞和爬升，不影响真实机器人任务。
// 入口：BotNavigationPlayProbe.Initialize() 创建一次性驱动；只在 Editor Play 验证使用。
#if UNITY_EDITOR
using UnityEngine;

public sealed class BotNavigationProbeDriver : MonoBehaviour
{
    private Transform _target;
    private Rigidbody _body;
    private Rigidbody _home;
    private BotFlightMotion _motion;
    private BotFlightMotion.Settings _settings;
    private float _started;
    public string CaseName
    {
        get; private set;
    }
    public bool Finished
    {
        get; private set;
    }
    public bool Reached
    {
        get; private set;
    }
    public int ObstacleContacts
    {
        get; private set;
    }
    public float MaximumHeight
    {
        get; private set;
    }
    public float RemainingDistance => _target == null ? float.PositiveInfinity : Vector3.Distance(transform.position, _target.position);

    public void Initialize(string caseName, Transform target, Rigidbody body, Rigidbody home, Bot authored)
    {
        CaseName = caseName;
        _target = target;
        _body = body;
        _home = home;
        _motion = new BotFlightMotion(transform, body);
        _motion.ResetDirection();
        _started = Time.time;
        _settings = new BotFlightMotion.Settings
        {
            MovementSpeed = authored.movementSpeed,
            RotationSpeed = authored.rotationSpeed,
            DirectionResponseRate = authored.directionResponseRate,
            SampleInterval = authored.directionUpdateInterval,
            DockingApproachTolerance = authored.dockingApproachTolerance,
            ReturnBrakingAcceleration = authored.returnBrakingAcceleration,
            ReturnBrakeDistance = authored.returnBrakeDistance,
            ReturnStopSpeed = authored.returnStopSpeed,
            Avoidance = new BotAvoidanceSettings
            {
                ObstacleMask = authored.obstacleMask,
                ClearanceRadius = 0.4f,
                PredictiveRange = authored.predictiveAvoidanceRange,
                EmergencyRange = authored.emergencyAvoidanceRange,
                BrakingAcceleration = authored.returnBrakingAcceleration,
                RayCount = authored.avoidanceRays,
                StuckTimeout = 1.5f,
                ProgressDistance = 0.25f,
                DetourHoldTime = 1f
            }
        };
    }

    private void FixedUpdate()
    {
        if (Finished || _motion == null)
        {
            return;
        }

        MaximumHeight = Mathf.Max(MaximumHeight, transform.position.y);
        if (_home != null)
        {
            _home.MovePosition(_home.position + Vector3.right * (0.8f * Time.fixedDeltaTime));
            float rangeScale = Mathf.InverseLerp(0.2f, 15f, RemainingDistance);
            _motion.NavigateHome(_target, _target.position, _home, rangeScale, _settings);
        }
        else
        {
            _motion.NavigateToTarget(_target, _settings);
        }

        Reached = RemainingDistance < 0.8f;
        Finished = Reached || Time.time - _started > 24f;
        if (Finished)
        {
            _body.linearVelocity = Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "NavigationProbeObstacle" && (!CaseName.Contains("initial overlap") || Time.time - _started > 0.5f))
        {
            ObstacleContacts++;
        }
    }
}
#endif
