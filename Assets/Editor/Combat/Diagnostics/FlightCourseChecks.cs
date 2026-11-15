// 职责：验证连续小幅航向修正、紧急接管及限角速度契约，并采集两艘船的直线飞行。
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
        FieldInfo direction = typeof(EnemyController).GetField("steeringDirection", flags);
        FieldInfo urgent = typeof(EnemyController).GetField("urgentAvoidance", flags);
        FieldInfo navigation = typeof(EnemyController).GetField("navigationOwnsDirection", flags);
        FieldInfo target = typeof(EnemyController).GetField("currentTarget", flags);
        object oldDirection = direction.GetValue(controller);
        object oldUrgent = urgent.GetValue(controller);
        object oldNavigation = navigation.GetValue(controller);
        object oldTarget = target.GetValue(controller);
        int oldTurns = controller.SteeringChanges;
        try
        {
            direction.SetValue(controller, Vector3.right);
            urgent.SetValue(controller, false);
            navigation.SetValue(controller, false);
            target.SetValue(controller, null);
            Vector3 desired = Quaternion.AngleAxis(8f, Vector3.up) * Vector3.right;
            Vector3 result = (Vector3)stabilize.Invoke(controller, new object[] { desired });
            float angle = Vector3.Angle(Vector3.right, result);
            Require(
                angle > 0.1f && angle < 7.5f,
                "Small heading change follows continuously without waiting for the old dead zone");
            direction.SetValue(controller, Vector3.right);
            float totalTurn = 0f;
            Vector3 previous = Vector3.right;
            for (int i = 0; i < 20; i++)
            {
                desired = Quaternion.AngleAxis(i % 2 == 0 ? 8f : -8f, Vector3.up) * Vector3.right;
                result = (Vector3)stabilize.Invoke(controller, new object[] { desired });
                totalTurn += Vector3.Angle(previous, result);
                previous = result;
            }

            Require(totalTurn < 65f, "Alternating small target offsets are attenuated instead of copied into zigzags");
            urgent.SetValue(controller, true);
            result = (Vector3)stabilize.Invoke(controller, new object[] { Vector3.back });
            Require(Vector3.Angle(result, Vector3.back) < 0.01f, "Emergency steering bypasses ordinary smoothing");
        }
        finally
        {
            direction.SetValue(controller, oldDirection);
            urgent.SetValue(controller, oldUrgent);
            navigation.SetValue(controller, oldNavigation);
            target.SetValue(controller, oldTarget);
            typeof(EnemyController).GetProperty("SteeringChanges").SetValue(controller, oldTurns);
        }

        Type math = typeof(EnemyController).Assembly.GetType("FlightControlMath", true);
        MethodInfo step = math.GetMethod("StepCourse", BindingFlags.Static | BindingFlags.NonPublic);
        Vector3 course = (Vector3)step.Invoke(null, new object[] { Vector3.right * 6f, Vector3.left * 6f, 90f, 6f, 0.02f });
        Require(
            Mathf.Abs(course.magnitude - 6f) < 0.001f && Vector3.Angle(Vector3.right, course) <= 1.81f,
            "Course reversal preserves speed and respects angular rate");
        return "PASS continuous small heading corrections; alternating offsets attenuated; immediate emergency; bounded course reversal";
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
