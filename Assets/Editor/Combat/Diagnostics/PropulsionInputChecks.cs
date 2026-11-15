// 职责：用用户Type 03蓝图采样真实直航、模拟输入与通用推进器朝向，避免只验证导航期望方向。
// 入口：Begin(label)；独立EnemyAvoidanceLab Play会话，结果写入Temp，不改源蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class PropulsionInputChecks
{
    public static Task<string> Pending
    {
        get; private set;
    }

    [Serializable]
    private sealed class Sample
    {
        public float seconds;
        public float range;
        public Vector3 course;
        public Vector3 input;
        public Vector3 velocity;
        public Vector3 nozzle;
        public Vector3 reference;
        public Vector3 drive;
        public float throttle;
        public bool braking;
        public float thrust;
    }

    [Serializable]
    private sealed class Report
    {
        public string directory;
        public string result;
        public List<Sample> samples = new List<Sample>();
    }

    public static void Begin(string label)
    {
        Pending = Capture(label);
    }

    public static string Validate()
    {
        var lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        if (!Application.isPlaying || lab == null || lab.Ship == null)
        {
            throw new InvalidOperationException("Requires a deployed lab ship in Play Mode.");
        }

        ControlUnit owner = lab.Ship.GetComponent<ControlUnit>();
        Rigidbody body = owner.GetComponent<Rigidbody>();
        UniversalThruster nozzle = owner.GetComponentInChildren<UniversalThruster>();
        Vector3 oldInput = owner.MovementInput;
        Vector3 oldDirection = owner.PropulsionDirection;
        Vector3 oldVelocity = body.linearVelocity;
        Quaternion oldRotation = nozzle.model.rotation;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        MethodInfo calculate = typeof(UniversalThruster).GetMethod("CalculateInputThrust", flags);
        Type solverType = typeof(EnemyController).Assembly.GetType("FlightPropulsionControl", true);
        object solver = Activator.CreateInstance(solverType, true);
        MethodInfo evaluate = solverType.GetMethod("Evaluate", flags);
        try
        {
            owner.SetPropulsionInput(Vector3.right, 0f);
            Require(
                owner.MovementInput == Vector3.zero && owner.PropulsionDirection == Vector3.right,
                "Coasting retains aim without thrust");
            nozzle.model.rotation = Quaternion.LookRotation(Vector3.right);
            float low = (float)calculate.Invoke(nozzle, new object[] { Vector3.right * 0.1f, true });
            float high = (float)calculate.Invoke(nozzle, new object[] { Vector3.right, true });
            Require(
                Mathf.Abs(low - nozzle.maxThrust * 0.1f) < 0.01f && Mathf.Abs(high - nozzle.maxThrust) < 0.01f,
                "Proportional universal thrust");
            float idle = (float)calculate.Invoke(nozzle, new object[] { Vector3.zero, false });
            Require(idle == 0f, "Zero throttle requests no thrust");
            nozzle.model.rotation = Quaternion.LookRotation(Vector3.left);
            float misaligned = (float)calculate.Invoke(nozzle, new object[] { Vector3.right, true });
            Require(misaligned == 0f, "Misaligned nozzle cannot request sideways thrust");
            Vector3 Evaluate(Vector3 velocity, Vector3 reference, bool brake)
            {
                body.linearVelocity = velocity;
                object command = evaluate.Invoke(solver, new object[] { owner, body, reference, brake, 6f, 4f, Time.fixedDeltaTime });
                return (Vector3)command.GetType().GetField("Direction", flags).GetValue(command);
            }

            for (int i = 0; i < 20; i++)
            {
                Vector3 velocity = Vector3.right * (i % 2 == 0 ? 5.95f : 6.05f) + Vector3.forward * (i % 2 == 0 ? 0.1f : -0.1f);
                Vector3 heading = Evaluate(velocity, Vector3.right * 6f, false);
                Require(
                    Vector3.Angle(heading, Vector3.right) < 0.001f,
                    "Small speed/lateral error must never steer cruise nozzle");
            }

            Vector3 turn = Evaluate(Vector3.right * 6f, Vector3.forward * 6f, false);
            Require(turn.z > 0f && turn.x < 0f, "A real turn counters old momentum");
            Vector3 stopped = Evaluate(Vector3.right * 6f, Vector3.zero, false);
            Require(Vector3.Angle(stopped, Vector3.left) < 0.001f, "Stopping explicitly brakes");
            Vector3 emergency = Evaluate(Vector3.right * 6f, Vector3.right * 2f, true);
            Require(Vector3.Angle(emergency, Vector3.left) < 0.001f, "Safety slowdown explicitly brakes");
            owner.SetMovementInput(Vector3.zero);
            Require(owner.PropulsionDirection == Vector3.zero, "Legacy clear removes coasting aim");
            return "PASS proportional throttle; coasting aim; zero request; misalignment gate; 20 alternating cruise errors; genuine turn; stop; safety braking; legacy clear";
        }
        finally
        {
            owner.SetPropulsionInput(oldDirection, oldInput.magnitude);
            body.linearVelocity = oldVelocity;
            nozzle.model.rotation = oldRotation;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static async Task<string> Capture(string label)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires lab Play Mode.");
        }

        var lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        object catalog = typeof(EnemyAvoidanceLab).GetProperty("Blueprints", flags).GetValue(lab);
        var options = (System.Collections.IList)catalog.GetType().GetField("Options", flags).GetValue(catalog);
        int selected = -1;
        for (int i = 0; i < options.Count; i++)
        {
            string name = (string)options[i].GetType().GetField("Label", flags).GetValue(options[i]);
            if (name == "Saves / Type 03")
            {
                selected = i;
                break;
            }
        }

        if (selected < 0)
        {
            throw new InvalidOperationException("Read-only user blueprint Saves / Type 03 was not found.");
        }

        bool background = Application.runInBackground;
        try
        {
            Application.runInBackground = true;
            lab.SelectLayout(5);
            lab.SelectBlueprint(selected);
            if (!lab.DeploySelected())
            {
                throw new InvalidOperationException("Type 03 deployment failed.");
            }

            var report = new Report
            {
                directory = lab.RecordingDirectory
            };
            float began = Time.time;
            double deadline = Time.realtimeSinceStartupAsDouble + 90f;
            UniversalThruster thruster = lab.Ship.GetComponentInChildren<UniversalThruster>();
            if (thruster == null)
            {
                throw new InvalidOperationException("Type 03 has no universal thruster.");
            }

            EnemyController controller = lab.Ship.GetComponent<EnemyController>();
            while (lab.Running && Time.realtimeSinceStartupAsDouble < deadline)
            {
                EnemyNavigationSnapshot value = controller.CaptureNavigation();
                report.samples.Add(new Sample { seconds = Time.time - began, range = Vector3.ProjectOnPlane(value.position - value.targetPosition, Vector3.up).magnitude, course = value.steering, input = value.appliedInput, velocity = value.velocity, reference = value.referenceVelocity, drive = value.propulsionDirection, throttle = value.propulsionThrottle, braking = value.propulsionBraking, nozzle = thruster.model != null ? thruster.model.forward : thruster.transform.forward, thrust = thruster.thrust });
                await Task.Delay(100);
            }

            if (!lab.Running)
            {
                report.result = File.ReadAllText(Path.Combine(report.directory, "result.json"));
            }
            else
            {
                lab.StopTrial();
                report.result = "timeout";
            }

            Directory.CreateDirectory("Temp/PropulsionInput");
            string path = "Temp/PropulsionInput/" + label + ".json";
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            return path;
        }
        finally
        {
            Application.runInBackground = background;
        }
    }
}
