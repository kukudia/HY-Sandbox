// 职责：采样一艘舰船的生产导航、实际航程/停滞和接触，驱动预制轨迹与规划线。
// 入口：Bind()/Sample()/SetVisible()；场景协调器负责选人和采样频率。
// 生命周期：换轮清空历史，同驾驶舱重组可续接；解绑注销碰撞事件，禁用释放速度纹理。
using UnityEngine;

public sealed class NavigationTrack : MonoBehaviour
{
    [SerializeField]
    private LineRenderer traceLine;
    [SerializeField]
    private LineRenderer routeLine;
    [SerializeField, Min(32)]
    private int maximumPoints = 4096;
    [SerializeField, Min(0.01f)]
    private float minimumSpacing = 0.4f;
    [SerializeField, Min(0.1f), Tooltip("与试验场相同的固定速度颜色标尺，米/秒。")]
    private float traceFastSpeed = 8f;
    private NavigationFlightTrace trace;
    private EnemyController controller;
    private float started;
    private float lastMotion;
    private float lastSampleTime;
    private Vector3 previous;
    private Vector3 motionAnchor;
    private int routeRevision = -1;
    private Vector3[] route;
    public ControlUnit Owner
    {
        get; private set;
    }
    public string Label
    {
        get; private set;
    }
    public EnemyNavigationSnapshot Snapshot
    {
        get; private set;
    }
    public float Travel
    {
        get; private set;
    }
    public int Contacts
    {
        get; private set;
    }
    public int TracePoints => trace != null ? trace.Count : 0;
    public float Elapsed => Mathf.Max(0f, (Alive ? Time.time : lastSampleTime) - started);
    public float StoppedSeconds => Mathf.Max(0f, (Alive ? Time.time : lastSampleTime) - lastMotion);
    public float TraceFastSpeed => traceFastSpeed;
    public bool Alive => Owner != null && Owner.HasValidCockpit && Owner.gameObject.activeInHierarchy;

    public void Bind(ControlUnit owner, string label, bool clearHistory)
    {
        Unsubscribe();
        Owner = owner;
        Label = label;
        controller = owner != null ? owner.GetComponent<EnemyController>() : null;
        routeRevision = -1;
        route = null;
        trace ??= new NavigationFlightTrace(traceLine, maximumPoints);
        if (clearHistory)
        {
            ClearHistory();
        }

        if (owner != null)
        {
            previous = motionAnchor = owner.transform.position;
            lastMotion = Time.time;
            owner.CollisionEntered += OnContact;
        }
    }

    public void ClearHistory()
    {
        trace?.Clear();
        Travel = 0f;
        Contacts = 0;
        started = lastMotion = lastSampleTime = Time.time;
        routeRevision = -1;
        if (Owner != null)
        {
            previous = motionAnchor = Owner.transform.position;
        }

        if (routeLine != null)
        {
            routeLine.positionCount = 0;
        }
    }

    public void Sample()
    {
        if (!Alive || controller == null)
        {
            return;
        }

        EnemyNavigationSnapshot snapshot = controller.CaptureNavigation(routeRevision);
        Snapshot = snapshot;
        lastSampleTime = Time.time;
        if (snapshot.routeChanged)
        {
            routeRevision = snapshot.routeRevision;
            route = snapshot.route;
        }

        Travel += Vector3.Distance(previous, snapshot.position);
        previous = snapshot.position;
        if (Vector3.ProjectOnPlane(snapshot.position - motionAnchor, Vector3.up).sqrMagnitude >= 1f)
        {
            motionAnchor = snapshot.position;
            lastMotion = Time.time;
        }

        trace.Sample(snapshot.position, snapshot.velocity.magnitude, minimumSpacing, traceFastSpeed);
        if (routeLine != null)
        {
            // 仅绘制尚未走过的航点，并以当前机体位置接起，避免把旧航段误作未来路径。
            int remaining = route != null && snapshot.followingPath ? Mathf.Max(0, route.Length - snapshot.waypoint) : 0;
            routeLine.positionCount = remaining > 0 ? remaining + 1 : 0;
            if (remaining > 0)
            {
                routeLine.SetPosition(0, snapshot.position);
                for (int i = 0; i < remaining; i++)
                {
                    routeLine.SetPosition(i + 1, route[snapshot.waypoint + i]);
                }
            }
        }
    }

    public void SetVisible(bool showTrace, bool showRoute)
    {
        if (traceLine != null)
        {
            traceLine.enabled = showTrace;
        }

        if (routeLine != null)
        {
            routeLine.enabled = showRoute && Alive;
        }
    }

    private void OnContact(Collision collision)
    {
        if (Alive)
        {
            Contacts++;
        }
    }

    private void Unsubscribe()
    {
        if (Owner != null)
        {
            Owner.CollisionEntered -= OnContact;
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
        trace?.Dispose();
        trace = null;
        Owner = null;
        controller = null;
    }
}
