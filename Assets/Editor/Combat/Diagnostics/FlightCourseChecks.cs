// 职责：验证直航锁定与横偏恢复、紧急接管及限角速度契约，并采集两艘船的直线飞行。
// 入口：Validate()/Begin(label)；仅在导航试验场Play会话运行，不保存测试期配置。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class FlightCourseChecks
{
    public static Task<string> Pending
    {
        get; private set;
    }

    public static string Validate()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires lab Play Mode.");
        }

        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        EnemyController controller = lab.Ship.GetComponent<EnemyController>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        MethodInfo stabilize = typeof(EnemyController).GetMethod("StabilizeDirection", flags);
        string[] fields =
        {
            "steeringDirection",
            "steeringOrigin",
            "nextDirectionChange",
            "steeringPathRevision",
            "steeringWaypoint",
            "steeringFollowingPath",
            "steeringDetouring",
            "urgentAvoidance",
            "navigationOwnsDirection",
            "currentTarget",
            "navigation"
        };
        var saved = new Dictionary<FieldInfo, object>();
        foreach (string name in fields)
        {
            FieldInfo field = typeof(EnemyController).GetField(name, flags);
            saved.Add(field, field.GetValue(controller));
        }

        int oldTurns = controller.SteeringChanges;
        void Set(string name, object value) => typeof(EnemyController).GetField(name, flags).SetValue(controller, value);
        try
        {
            Type navigationType = typeof(EnemyController).Assembly.GetType("VesselNavigation", true);
            Set(
                "navigation",
                Activator.CreateInstance(navigationType, flags | BindingFlags.Public, null, new object[] { controller.transform }, null));
            Set("steeringDirection", Vector3.right);
            Set("steeringOrigin", controller.transform.position);
            Set("nextDirectionChange", 0f);
            Set("steeringFollowingPath", false);
            Set("steeringDetouring", false);
            Set("urgentAvoidance", false);
            Set("navigationOwnsDirection", false);
            Set("currentTarget", null);
            Vector3 previous = Vector3.right;
            for (int i = 0; i < 20; i++)
            {
                Vector3 desired = Quaternion.AngleAxis(i % 2 == 0 ? 8f : -8f, Vector3.up) * Vector3.right;
                Vector3 result = (Vector3)stabilize.Invoke(controller, new object[] { desired });
                Require(
                    (result - previous).sqrMagnitude < 1e-10f,
                    "Alternating small offsets never change locked straight heading");
            }

            Set("navigationOwnsDirection", true);
            Vector3 small = Quaternion.AngleAxis(8f, Vector3.up) * Vector3.right;
            Vector3 locked = (Vector3)stabilize.Invoke(controller, new object[] { small });
            Require(
                (locked - Vector3.right).sqrMagnitude < 1e-10f,
                "Navigation ownership alone cannot unlock micro steering");
            Set("navigationOwnsDirection", false);
            Vector3 changed = Quaternion.AngleAxis(25f, Vector3.up) * Vector3.right;
            Vector3 turn = (Vector3)stabilize.Invoke(controller, new object[] { changed });
            Require(Vector3.Angle(turn, changed) < 0.01f, "Significant bearing change selects a new straight heading");
            Vector3 held = (Vector3)stabilize.Invoke(controller, new object[] { Vector3.right });
            Require(
                Vector3.Angle(held, changed) < 0.01f,
                "Ordinary heading commitment prevents immediate reverse selection");
            Set("steeringDirection", Vector3.right);
            float tolerance = (float)typeof(EnemyController).GetField("straightCourseTolerance", flags).GetValue(controller);
            Set("steeringOrigin", controller.transform.position + Vector3.forward * (tolerance + 1f));
            Vector3 recovered = (Vector3)stabilize.Invoke(controller, new object[] { small });
            Require(
                Vector3.Angle(recovered, small) < 0.01f,
                "Actual lateral departure can rejoin the route despite a small bearing error");
            Set("urgentAvoidance", true);
            Vector3 emergency = (Vector3)stabilize.Invoke(controller, new object[] { Vector3.back });
            Require(Vector3.Angle(emergency, Vector3.back) < 0.01f, "Emergency steering bypasses heading commitment");
        }
        finally
        {
            foreach (var pair in saved)
            {
                pair.Key.SetValue(controller, pair.Value);
            }

            typeof(EnemyController).GetProperty("SteeringChanges").SetValue(controller, oldTurns);
        }

        Type math = typeof(EnemyController).Assembly.GetType("FlightControlMath", true);
        MethodInfo step = math.GetMethod("StepCourse", BindingFlags.Static | BindingFlags.NonPublic);
        Vector3 course = (Vector3)step.Invoke(null, new object[] { Vector3.right * 6f, Vector3.left * 6f, 90f, 6f, 0.02f });
        Require(
            Mathf.Abs(course.magnitude - 6f) < 0.001f && Vector3.Angle(Vector3.right, course) <= 1.81f,
            "Course reversal preserves speed and respects angular rate");
        return "PASS locked heading unchanged across 20 alternating offsets; navigation cannot micro steer; meaningful turn and commitment; lateral recovery; immediate emergency; bounded course reversal";
    }

    public static void Begin(string label)
    {
        Pending = Run(label);
    }

    private static async Task<string> Run(string label)
    {
        var lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        if (!Application.isPlaying || lab == null)
        {
            throw new InvalidOperationException("Requires lab Play Mode.");
        }

        bool background = Application.runInBackground;
        float scale = Time.timeScale;
        var results = new List<string>();
        try
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            foreach (int index in new[]
            {
                0,
                2
            }

            )
            {
                lab.SelectLayout(5);
                lab.SelectBlueprint(index);
                Require(lab.DeploySelected(), "Ship deployed");
                string directory = lab.RecordingDirectory;
                double deadline = Time.realtimeSinceStartupAsDouble + 80f;
                while (lab.Running && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    await Task.Delay(100);
                }

                Require(!lab.Running && File.Exists(Path.Combine(directory, "result.json")), "Straight course completed");
                string result = File.ReadAllText(Path.Combine(directory, "result.json"));
                results.Add(directory + "\n" + result);
                Require(result.Contains("\"arrived\": true"), "Straight course arrived");
            }

            Directory.CreateDirectory("Temp/FlightCourse");
            string path = "Temp/FlightCourse/" + label + ".txt";
            File.WriteAllText(path, string.Join("\n", results));
            return path;
        }
        finally
        {
            Application.runInBackground = background;
            Time.timeScale = scale;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
