// 职责：协调可编辑导航试验、手动蓝图部署、目标设置与操作/导航记录。
// 入口：Start() / DeploySelected() / SelectLayout()；真实施力仍由生产AI与推进器完成。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EnemyAvoidanceLab : MonoBehaviour
{
    [SerializeField]
    private ControlUnit ship;
    [SerializeField]
    private ControlUnit target;
    [SerializeField]
    private GameObject[] layouts;
    [SerializeField, Min(0)]
    private int layoutIndex;
    [SerializeField]
    private Vector3 startPosition = new Vector3(-45f, 6f, 0f);
    [SerializeField]
    private Vector3 targetPosition = new Vector3(45f, 3f, 0f);
    [SerializeField, Min(5f)]
    private float timeout = 150f;
    [SerializeField, Min(1f)]
    private float arrivalRadius = 22f;
    [SerializeField]
    private bool runAll;
    [SerializeField]
    private LineRenderer traceLine;
    [SerializeField]
    private LineRenderer routeLine;
    [SerializeField]
    private bool recordTrials = true;
    [SerializeField, Min(0.02f)]
    private float recordInterval = 0.1f;
    private Rigidbody body;
    private EnemyController controller;
    private float started;
    private float settledAt;
    private float travel;
    private float nextSample;
    private Vector3 previous;
    private bool running;
    private int contacts;
    private int obstacleContacts;
    private int targetContacts;
    private int groundContacts;
    private int selectedBlueprint;
    private string blueprintName = "Scene Finch";
    private string blueprintJson;
    private readonly EnemyLabBlueprints blueprints = new EnemyLabBlueprints();
    private readonly EnemyLabRecorder recorder = new EnemyLabRecorder();
    private readonly List<Vector3> trace = new List<Vector3>();
    private EnemyNavigationSnapshot lastNavigation;
    private Vector3[] displayedRoute = Array.Empty<Vector3>();
    private bool paused;
    private bool oldRunInBackground;
    private bool initialized;
    private float previousTimeScale;
    private string notice;
    private EnemyLabPanel panel;
    public string Status { get; private set; } = "Ready";
    public ControlUnit Ship => ship;
    public int BlueprintCount => blueprints.Options.Count;
    public int LayoutCount => layouts.Length;
    public bool Running => running;
    public string RecordingDirectory => recorder.DirectoryPath;
    internal EnemyNavigationSnapshot Navigation => lastNavigation;
    internal EnemyLabBlueprints Blueprints => blueprints;
    internal int SelectedBlueprint => selectedBlueprint;
    internal int LayoutIndex => layoutIndex;
    internal EnemyAvoidanceCourse Course => layouts[layoutIndex].GetComponent<EnemyAvoidanceCourse>();
    internal string BlueprintName => blueprintName;
    internal string Notice => notice ?? blueprints.Message;
    internal string RecordingStatus => recorder.Error ?? (recorder.Active ? "正在记录" : "记录已保存");
    internal bool Paused => paused;
    internal Vector3 StartPosition => startPosition;
    internal Vector3 GoalPosition => targetPosition;

    internal string LayoutName(int index) => layouts[index].name;
    public void RecordOperation(string action, string detail) => recorder.Event(action, detail);
    internal void SetRoofVisibility(bool visible)
    {
        foreach (GameObject layout in layouts)
        {
            foreach (MeshRenderer renderer in layout.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name.IndexOf("roof", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    renderer.enabled = visible;
                }
            }
        }

        recorder.Event("roof_visibility", visible.ToString());
    }

    public void BeginPointPick(bool start)
    {
        panel.BeginPick(start);
    }

    public void SetStart(Vector3 position)
    {
        startPosition = position;
        startPosition.y = Mathf.Max(2f, startPosition.y);
        recorder.Event("set_start", startPosition.ToString("F3"));
        ResetTrial();
    }

    private void Start()
    {
        initialized = true;
        oldRunInBackground = Application.runInBackground;
        previousTimeScale = Time.timeScale;
        Application.runInBackground = true;
        PlayManager.instance.playMode = true;
        target.RefreshChildren();
        target.GetComponent<EnemyController>().enabled = false;
        target.GetComponent<Rigidbody>().isKinematic = true;
        blueprintJson = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch").text;
        recorder.Enabled = recordTrials;
        RefreshBlueprints();
        BindShip(ship);
        panel = new EnemyLabPanel(this);
        SetRoofVisibility(false);
        ResetTrial();
    }

    public void RefreshBlueprints()
    {
        blueprints.Refresh();
        selectedBlueprint = Mathf.Clamp(selectedBlueprint, 0, Mathf.Max(0, BlueprintCount - 1));
        recorder.Event("refresh_blueprints", blueprints.Message);
    }

    public void SelectBlueprint(int index)
    {
        if (index < 0 || index >= BlueprintCount)
        {
            return;
        }

        selectedBlueprint = index;
        recorder.Event("select_blueprint", blueprints.Options[index].Label);
    }

    public bool DeploySelected()
    {
        if (!Application.isPlaying || BlueprintCount == 0)
        {
            return false;
        }

        EnemyLabBlueprints.Option option = blueprints.Options[selectedBlueprint];
        recorder.Event("deploy", option.Label);
        BlockDataList data = JsonUtility.FromJson<BlockDataList>(option.Json);
        if (!ModularUnitFactory.IsValidBlueprint(data, option.Label))
        {
            return false;
        }

        ControlUnit replacement = ModularUnitFactory.Spawn(
            data,
            "Navigation test / " + option.Label,
            startPosition,
            Quaternion.LookRotation(Vector3.right),
            UnitFaction.Enemy);
        if (replacement == null)
        {
            notice = "部署失败，保留当前测试舰";
            recorder.Event("deploy_failed", option.Label);
            return false;
        }

        SceneManager.MoveGameObjectToScene(replacement.gameObject, gameObject.scene);
        FinishTrial("replaced");
        ControlUnit old = ship;
        if (old != null)
        {
            old.CollisionEntered -= OnShipCollision;
            old.gameObject.SetActive(false);
            Destroy(old.gameObject);
        }

        blueprintName = option.Label;
        blueprintJson = option.Json;
        BindShip(replacement);
        runAll = false;
        ResetTrial();
        return true;
    }

    private void BindShip(ControlUnit value)
    {
        ship = value;
        body = ship.GetComponent<Rigidbody>();
        controller = ship.GetComponent<EnemyController>();
        controller.ConfigureDebugDisplay(true, false);
        ship.RefreshChildren();
        ship.cockpit.SetControlMode(UnitControlMode.Autonomous);
        ship.CollisionEntered -= OnShipCollision;
        ship.CollisionEntered += OnShipCollision;
        foreach (Turret turret in ship.GetComponentsInChildren<Turret>())
        {
            turret.SetStarted(false);
        }

        foreach (Durability health in ship.GetComponentsInChildren<Durability>())
        {
            health.SetCollisionDamageScale(0f);
        }
    }

    public void SelectLayout(int index)
    {
        if (index < 0 || index >= layouts.Length)
        {
            return;
        }

        recorder.Event("select_layout", layouts[index].name);
        FinishTrial("layout_changed");
        layoutIndex = index;
        EnemyAvoidanceCourse course = layouts[index].GetComponent<EnemyAvoidanceCourse>();
        if (course != null)
        {
            startPosition = course.Start;
            targetPosition = course.Goal;
            timeout = course.Duration;
        }

        runAll = false;
        ResetTrial();
    }

    [ContextMenu("Reset trial")]
    public void ResetTrial()
    {
        if (!Application.isPlaying || ship == null)
        {
            return;
        }

        FinishTrial("reset");
        for (int i = 0; i < layouts.Length; i++)
        {
            layouts[i].SetActive(i == layoutIndex);
        }

        controller.enabled = false;
        body.position = startPosition;
        body.rotation = Quaternion.LookRotation(Vector3.right);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();
        foreach (Thruster thruster in ship.GetComponentsInChildren<Thruster>())
        {
            thruster.thrust = 0f;
            thruster.lastThrustValue = 0f;
        }

        ship.transform.SetPositionAndRotation(body.position, body.rotation);
        target.transform.position = targetPosition;
        ship.SetMovementInput(Vector3.zero);
        controller.enabled = true;
        controller.SetPriorityTarget(target);
        Physics.SyncTransforms();
        contacts = obstacleContacts = targetContacts = groundContacts = 0;
        travel = 0f;
        previous = startPosition;
        running = true;
        started = Time.time;
        settledAt = -1f;
        nextSample = 0f;
        trace.Clear();
        displayedRoute = Array.Empty<Vector3>();
        Status = "Ready";
        notice = "起点 " + startPosition.ToString("F1") + " / 目标 " + targetPosition.ToString("F1");
        if (recordTrials)
        {
            recorder.Begin(
                layouts[layoutIndex].name,
                blueprintName,
                blueprintJson,
                controller,
                startPosition,
                targetPosition,
                recordInterval);
        }

        recorder.Event("reset", "start=" + startPosition + "; goal=" + targetPosition);
        Sample();
    }

    private void FixedUpdate()
    {
        if (!running || ship == null)
        {
            return;
        }

        float distance = Vector3.ProjectOnPlane(ship.transform.position - target.transform.position, Vector3.up).magnitude;
        travel += Vector3.Distance(previous, ship.transform.position);
        previous = ship.transform.position;
        if (distance <= arrivalRadius && settledAt < 0f)
        {
            settledAt = Time.time;
        }

        Status = $"{layouts[layoutIndex].name}: {Time.time - started:F1}s | {distance:F1}m | {body.linearVelocity.magnitude:F1}m/s | contacts {contacts} (walls {obstacleContacts})";
        if (Time.time >= nextSample)
        {
            Sample();
        }

        if ((settledAt >= 0f && Time.time - settledAt >= 2f) || Time.time - started >= timeout || !ship.HasValidCockpit)
        {
            FinishTrial(!ship.HasValidCockpit ? "lost_cockpit" : settledAt >= 0f ? "arrived" : "timeout");
            if (runAll && layoutIndex + 1 < layouts.Length)
            {
                int next = layoutIndex + 1;
                SelectLayout(next);
                runAll = true;
            }
        }
    }

    private void Sample()
    {
        lastNavigation = recorder.Sample(controller);
        if (lastNavigation.routeChanged)
        {
            displayedRoute = lastNavigation.route;
            if (routeLine != null)
            {
                routeLine.positionCount = displayedRoute.Length;
                routeLine.SetPositions(displayedRoute);
            }
        }

        if (trace.Count == 0 || Vector3.Distance(trace[trace.Count - 1], ship.transform.position) >= 0.4f)
        {
            bool shifted = trace.Count >= 12000;
            if (shifted)
            {
                trace.RemoveAt(0);
            }

            trace.Add(ship.transform.position);
            if (traceLine != null)
            {
                traceLine.positionCount = trace.Count;
                if (shifted)
                {
                    traceLine.SetPositions(trace.ToArray());
                }
                else
                {
                    traceLine.SetPosition(trace.Count - 1, ship.transform.position);
                }
            }
        }

        nextSample = Time.time + Mathf.Max(0.02f, recordInterval);
    }

    private void FinishTrial(string reason)
    {
        if (!running)
        {
            return;
        }

        if (controller == null || ship == null || target == null)
        {
            recorder.Finish(JsonUtility.ToJson(new Trial { reason = reason, seconds = Time.time - started, blueprint = blueprintName }));
            running = false;
            return;
        }

        recorder.Sample(controller);
        var trial = new Trial
        {
            layout = layouts[layoutIndex].name,
            blueprint = blueprintName,
            reason = reason,
            arrived = reason == "arrived",
            seconds = Time.time - started,
            distance = Vector3.ProjectOnPlane(ship.transform.position - target.transform.position, Vector3.up).magnitude,
            travel = travel,
            contacts = contacts,
            obstacleContacts = obstacleContacts,
            targetContacts = targetContacts,
            groundContacts = groundContacts,
            turns = controller.SteeringChanges,
            position = ship.transform.position
        };
        recorder.Finish(JsonUtility.ToJson(trial, true));
        running = false;
        Status = reason.ToUpperInvariant() + " " + Status;
    }

    private void OnShipCollision(Collision collision)
    {
        if (!running)
        {
            return;
        }

        contacts++;
        string scope;
        if (collision.collider.transform.IsChildOf(transform))
        {
            obstacleContacts++;
            scope = "obstacle";
        }
        else if (target != null && collision.collider.transform.IsChildOf(target.transform))
        {
            targetContacts++;
            scope = "target";
        }
        else if (collision.collider.name == "Ground")
        {
            groundContacts++;
            scope = "ground";
        }
        else
        {
            scope = "other";
        }

        recorder.Event(
            "contact",
            scope + ": " + collision.collider.name + "; relativeVelocity=" + collision.relativeVelocity);
    }

    public void StopTrial()
    {
        recorder.Event("stop", "panel or API");
        runAll = false;
        FinishTrial("stopped");
        controller.enabled = false;
        ship.SetMovementInput(Vector3.zero);
    }

    public void BeginSuite()
    {
        recorder.Event("run_suite", "all courses");
        SelectLayout(0);
        runAll = true;
    }

    public void SetGoal(Vector3 point)
    {
        recorder.Event("set_goal", point.ToString("F3"));
        targetPosition = point;
        if (!running)
        {
            ResetTrial();
            return;
        }

        target.transform.position = point;
        controller.SetPriorityTarget(target);
        settledAt = -1f;
    }

    public void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = value ? 0f : previousTimeScale;
        recorder.Event("pause", value.ToString());
    }

    private void Update()
    {
        panel?.PollInput();
    }

    private void OnGUI()
    {
        panel?.Draw();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 1; i < trace.Count; i++)
        {
            Gizmos.DrawLine(trace[i - 1], trace[i]);
        }

        Gizmos.color = new Color(1f, 0.6f, 0f);
        for (int i = 1; i < displayedRoute.Length; i++)
        {
            Gizmos.DrawLine(displayedRoute[i - 1], displayedRoute[i]);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, 2f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPosition, 2f);
    }

    private void OnDisable()
    {
        if (!initialized)
        {
            return;
        }

        FinishTrial("disabled");
        recorder.Dispose();
        if (ship != null)
        {
            ship.CollisionEntered -= OnShipCollision;
        }

        Time.timeScale = previousTimeScale;
        Application.runInBackground = oldRunInBackground;
    }

    [Serializable]
    private sealed class Trial
    {
        public string layout;
        public string blueprint;
        public string reason;
        public bool arrived;
        public float seconds;
        public float distance;
        public float travel;
        public int contacts;
        public int obstacleContacts;
        public int targetContacts;
        public int groundContacts;
        public int turns;
        public Vector3 position;
    }
}
