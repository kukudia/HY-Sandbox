// 职责：驱动可编辑避障试验场，复用真实 EnemyController、供电与推进器，记录飞行结果。
// 入口：Start() / ResetTrial()；Inspector选择布局，Game面板可切换或重试，不读写玩家存档。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class EnemyAvoidanceLab : MonoBehaviour
{
    [SerializeField]
    private ControlUnit ship;
    [SerializeField]
    private ControlUnit target;
    [SerializeField]
    private GameObject[] layouts;
    [SerializeField, Range(0, 4)]
    private int layoutIndex;
    [SerializeField]
    private Vector3 startPosition = new Vector3(-45f, 6f, 0f);
    [SerializeField]
    private Vector3 targetPosition = new Vector3(45f, 3f, 0f);
    [SerializeField, Min(5f)]
    private float timeout = 90f;
    [SerializeField, Min(1f)]
    private float arrivalRadius = 22f;
    [SerializeField]
    private bool runAll;
    private Rigidbody body;
    private float started;
    private float settledAt;
    private float travel;
    private Vector3 previous;
    private bool running;
    private int contacts;
    private readonly List<Trial> results = new List<Trial>();
    public string Status { get; private set; } = "Ready";
    public ControlUnit Ship => ship;

    private void Start()
    {
        Application.runInBackground = true;
        PlayManager.instance.playMode = true;
        body = ship.GetComponent<Rigidbody>();
        ship.RefreshChildren();
        target.RefreshChildren();
        target.GetComponent<EnemyController>().enabled = false;
        target.GetComponent<Rigidbody>().isKinematic = true;
        ResetTrial();
    }

    [ContextMenu("Reset trial")]
    public void ResetTrial()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        for (int i = 0; i < layouts.Length; i++)
        {
            layouts[i].SetActive(i == layoutIndex);
        }

        EnemyController ai = ship.GetComponent<EnemyController>();
        ai.enabled = false;
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
        ai.enabled = true;
        ai.SetPriorityTarget(target);
        Physics.SyncTransforms();
        contacts = 0;
        travel = 0f;
        previous = startPosition;
        running = true;
        started = Time.time;
        settledAt = -1f;
        Status = "Ready";
    }

    private void FixedUpdate()
    {
        if (!running)
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

        Status = $"{layouts[layoutIndex].name}: {Time.time - started:F1}s | distance {distance:F1}m | speed {body.linearVelocity.magnitude:F1}m/s | contacts {contacts}";
        if ((settledAt >= 0f && Time.time - settledAt >= 2f) || Time.time - started >= timeout || !ship.HasValidCockpit)
        {
            results.Add(new Trial { layout = layouts[layoutIndex].name, arrived = settledAt >= 0f, seconds = Time.time - started, distance = distance, travel = travel, contacts = contacts, turns = ship.GetComponent<EnemyController>().SteeringChanges, position = ship.transform.position });
            Directory.CreateDirectory("Temp/EnemyAvoidanceLab");
            File.WriteAllText("Temp/EnemyAvoidanceLab/trials.json", JsonUtility.ToJson(new Report { trials = results }, true));
            running = false;
            Status = (settledAt >= 0f ? "ARRIVED " : "TIMEOUT ") + Status;
            if (runAll && ++layoutIndex < layouts.Length)
            {
                ResetTrial();
            }
            else
            {
                layoutIndex = Mathf.Clamp(layoutIndex, 0, layouts.Length - 1);
            }
        }
    }

    public void RecordContact()
    {
        if (running)
        {
            contacts++;
        }
    }

    public void BeginSuite()
    {
        results.Clear();
        layoutIndex = 0;
        runAll = true;
        ResetTrial();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(16f, 16f, 800f, 110f), GUI.skin.box);
        GUILayout.Label("ENEMY AVOIDANCE LAB | cyan: target | magenta: intent | white: smoothed input");
        GUILayout.Label(Status);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < layouts.Length; i++)
        {
            if (GUILayout.Button(layouts[i].name))
            {
                runAll = false;
                layoutIndex = i;
                ResetTrial();
            }
        }

        if (GUILayout.Button("Run all"))
        {
            BeginSuite();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    [Serializable]
    private sealed class Report
    {
        public List<Trial> trials;
    }

    [Serializable]
    private sealed class Trial
    {
        public string layout;
        public bool arrived;
        public float seconds;
        public float distance;
        public float travel;
        public int contacts;
        public int turns;
        public Vector3 position;
    }
}
