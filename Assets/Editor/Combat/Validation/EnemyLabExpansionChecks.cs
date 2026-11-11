// 职责：验证双层场地的持久资产、动态障碍物理运动及试验复位生命周期。
// 入口：ValidateSaved()用于Edit Mode；Begin()用于Play Mode，Pending返回实际检查结果。
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class EnemyLabExpansionChecks
{
    public static Task<string> Pending
    {
        get; private set;
    }

    public static string ValidateSaved()
    {
        var checks = new List<string>();
        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        Transform district = lab.transform.Find("Complex obstacle district");
        int count = district.GetComponentsInChildren<Transform>(true).Length;
        EnemyLabExpansionAuthoring.Upgrade();
        Check(
            checks,
            "Repeated authoring preserves existing objects",
            count == district.GetComponentsInChildren<Transform>(true).Length);
        EnemyAvoidanceCourse course = district.GetComponent<EnemyAvoidanceCourse>();
        Check(checks, "Ten ground and upper deployment locations persist", course.LocationCount == 10);
        Check(
            checks,
            "Upper deck saved as real colliders",
            district.Find("Upper deck").GetComponentsInChildren<BoxCollider>().Length >= 16);
        EnemyLabMovingObstacle[] obstacles = district.GetComponentsInChildren<EnemyLabMovingObstacle>(true);
        Check(checks, "Four moving obstacles persist", obstacles.Length == 4);
        foreach (EnemyLabMovingObstacle obstacle in obstacles)
        {
            Rigidbody body = obstacle.GetComponent<Rigidbody>();
            Check(
                checks,
                obstacle.name + " has a nonstatic kinematic collision body",
                !obstacle.gameObject.isStatic && body.isKinematic && !body.useGravity && !obstacle.GetComponent<Collider>().isTrigger);
            Check(
                checks,
                obstacle.name + " has no missing scripts",
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obstacle.gameObject) == 0);
        }

        Physics.SyncTransforms();
        for (int i = 6; i < course.LocationCount; i++)
        {
            Vector3 point = course.Location(i).position;
            Check(checks, "Upper deployment " + i + " has clear center", !Physics.CheckBox(point, Vector3.one * 3f));
            Check(checks, "Upper deployment " + i + " has deck beneath", Physics.Raycast(point, Vector3.down, 12f));
        }

        return Save("expansion-edit-checks.txt", checks);
    }

    public static void Begin()
    {
        Pending = Run();
    }

    private static async Task<string> Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires navigation lab Play Mode.");
        }

        var checks = new List<string>();
        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        lab.SelectLayout(lab.LayoutCount - 1);
        lab.SetPaused(true);
        EnemyLabMovingObstacle[] obstacles = lab.GetComponentsInChildren<EnemyLabMovingObstacle>();
        var positions = new Vector3[obstacles.Length];
        var rotations = new Quaternion[obstacles.Length];
        for (int i = 0; i < obstacles.Length; i++)
        {
            positions[i] = obstacles[i].GetComponent<Rigidbody>().position;
            rotations[i] = obstacles[i].GetComponent<Rigidbody>().rotation;
        }

        try
        {
            lab.SetPaused(false);
            float began = Time.time;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (Time.time - began < 2f && Time.realtimeSinceStartup < deadline)
            {
                await Task.Delay(100);
            }

            Check(checks, "Simulation advances for two seconds", Time.time - began >= 2f);
            lab.SetPaused(true);
            for (int i = 0; i < obstacles.Length; i++)
            {
                Rigidbody body = obstacles[i].GetComponent<Rigidbody>();
                Check(
                    checks,
                    obstacles[i].name + " moves through physics",
                    Vector3.Distance(positions[i], body.position) > 1f || Quaternion.Angle(rotations[i], body.rotation) > 5f);
                Collider[] hits = Physics.OverlapBox(body.position, Vector3.one);
                Check(
                    checks,
                    obstacles[i].name + " remains queryable at its moved position",
                    Array.Exists(hits, hit => hit == obstacles[i].GetComponent<Collider>()));
            }

            Vector3 frozen = obstacles[0].GetComponent<Rigidbody>().position;
            await Task.Delay(350);
            Check(
                checks,
                "Pause freezes moving obstacles",
                Vector3.Distance(frozen, obstacles[0].GetComponent<Rigidbody>().position) < 0.001f);
            lab.ResetTrial();
            for (int i = 0; i < obstacles.Length; i++)
            {
                Rigidbody body = obstacles[i].GetComponent<Rigidbody>();
                Check(
                    checks,
                    obstacles[i].name + " resets position and rotation",
                    Vector3.Distance(positions[i], body.position) < 0.001f && Quaternion.Angle(rotations[i], body.rotation) < 0.01f);
            }

            string metadata = File.ReadAllText(Path.Combine(lab.RecordingDirectory, "metadata.json"));
            Check(
                checks,
                "Recording includes all moving bodies and their motion configuration",
                Array.TrueForAll(obstacles, obstacle => metadata.Contains(obstacle.name)) && metadata.Contains("motionSettings") && metadata.Contains("swingAngle"));
            lab.SelectLayout(0);
            Check(
                checks,
                "Switching layout disables every moving obstacle",
                Array.TrueForAll(obstacles, obstacle => !obstacle.gameObject.activeInHierarchy));
            lab.SelectLayout(lab.LayoutCount - 1);
            Check(
                checks,
                "Returning to district restores original phase",
                Vector3.Distance(positions[0], obstacles[0].GetComponent<Rigidbody>().position) < 0.001f);
            for (int i = 6; i < lab.LocationCount; i++)
            {
                Check(
                    checks,
                    "Upper start and target selectable " + i,
                    lab.SelectLocation(i, true) && lab.SelectLocation(i, false));
            }

            return Save("expansion-play-checks.txt", checks);
        }
        finally
        {
            lab.SelectLocation(0, true);
            lab.SelectLocation(1, false);
            lab.ResetTrial();
            lab.SetPaused(false);
        }
    }

    private static void Check(List<string> checks, string label, bool condition)
    {
        checks.Add((condition ? "PASS " : "FAIL ") + label);
        if (!condition)
        {
            throw new InvalidOperationException(string.Join("\n", checks));
        }
    }

    private static string Save(string file, List<string> checks)
    {
        Directory.CreateDirectory("Temp/EnemyAvoidanceLab");
        string result = string.Join("\n", checks);
        File.WriteAllText(Path.Combine("Temp/EnemyAvoidanceLab", file), result);
        return result;
    }
}
