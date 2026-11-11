// 职责：将测试元数据、操作事件及导航采样增量写入独立会话目录，避免长测常驻大日志。
// 入口：Begin() / Event() / Sample() / Finish()；文件错误只关闭记录，不影响飞行。
using System;
using System.IO;
using UnityEngine;

internal sealed class EnemyLabRecorder : IDisposable
{
    private StreamWriter writer;
    private StreamWriter operations;
    private string sessionDirectory;
    private int trial;
    private bool failed;
    private float sessionBegan;
    private float began;
    private float lastFlush;
    private int routeRevision = -1;
    public string DirectoryPath
    {
        get; private set;
    }
    public string Error
    {
        get; private set;
    }
    internal bool Enabled { get; set; } = true;
    public bool Active => writer != null;

    [Serializable]
    private sealed class Entry
    {
        public string kind;
        public float seconds;
        public int frame;
        public int trial;
        public float sessionSeconds;
        public float realtime;
        public string action;
        public string detail;
        public EnemyNavigationSnapshot navigation;
    }

    [Serializable]
    private sealed class Metadata
    {
        public int schema = 1;
        public string utc;
        public string unity;
        public string course;
        public string blueprint;
        public string aiSettings;
        public Vector3 start;
        public Vector3 goal;
        public float fixedDeltaTime;
        public float timeScale;
        public float sampleInterval;
        public Geometry[] geometry;
        public Vector2 resolution;
        public Vector3 cameraPosition;
        public Quaternion cameraRotation;
        public float cameraSize;
    }

    [Serializable]
    private sealed class Geometry
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Bounds bounds;
    }

    public void Begin(
        string course,
        string blueprint,
        string json,
        EnemyController ai,
        Vector3 start,
        Vector3 goal,
        float interval)
    {
        CloseTrial();
        if (!Enabled)
        {
            return;
        }

        failed = false;
        Error = null;
        EnsureSession();
        began = Time.time;
        routeRevision = -1;
        if (sessionDirectory == null || failed)
        {
            return;
        }

        DirectoryPath = Path.Combine(sessionDirectory, "trial-" + (++trial).ToString("D4"));
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var geometry = new System.Collections.Generic.List<Geometry>();
            foreach (Collider collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            {
                if (collider.gameObject.scene == ai.gameObject.scene && collider.attachedRigidbody == null && !collider.isTrigger)
                {
                    geometry.Add(new Geometry { name = collider.name, position = collider.transform.position, rotation = collider.transform.rotation, scale = collider.transform.lossyScale, bounds = collider.bounds });
                }
            }

            File.WriteAllText(
                Path.Combine(DirectoryPath, "metadata.json"),
                JsonUtility.ToJson(
                    new Metadata { utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion, course = course, blueprint = blueprint, aiSettings = JsonUtility.ToJson(ai), start = start, goal = goal, fixedDeltaTime = Time.fixedDeltaTime, timeScale = Time.timeScale, sampleInterval = interval, resolution = new Vector2(Screen.width, Screen.height), cameraPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero, cameraRotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity, cameraSize = Camera.main != null ? Camera.main.orthographicSize : 0f, geometry = geometry.ToArray() },
                    true));
            File.WriteAllText(Path.Combine(DirectoryPath, "blueprint.json"), json ?? "{}");
            writer = new StreamWriter(Path.Combine(DirectoryPath, "timeline.jsonl"), false);
            Event("begin", course);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Fail(exception);
        }
    }

    public void Event(string action, string detail)
    {
        EnsureSession();
        Write(new Entry { kind = "operation", action = action, detail = detail });
    }

    public EnemyNavigationSnapshot Sample(EnemyController ai)
    {
        EnemyNavigationSnapshot snapshot = ai.CaptureNavigation(routeRevision);
        routeRevision = snapshot.routeRevision;
        Write(new Entry { kind = "navigation", navigation = snapshot });
        return snapshot;
    }

    public void Finish(string json)
    {
        Event("finish", json);
        if (writer != null)
        {
            try
            {
                writer.Flush();
                File.WriteAllText(Path.Combine(DirectoryPath, "result.json"), json);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Fail(exception);
            }
        }

        CloseTrial();
    }

    private void EnsureSession()
    {
        if (!Enabled || operations != null || failed)
        {
            return;
        }

        try
        {
            sessionDirectory = Path.Combine(
                Application.persistentDataPath,
                "NavigationLabRecords",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            Directory.CreateDirectory(sessionDirectory);
            operations = new StreamWriter(Path.Combine(sessionDirectory, "operations.jsonl"), false);
            sessionBegan = Time.unscaledTime;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Fail(exception);
        }
    }

    private void Write(Entry entry)
    {
        if (writer == null && (entry.kind != "operation" || operations == null))
        {
            return;
        }

        try
        {
            entry.trial = trial;
            entry.sessionSeconds = Time.unscaledTime - sessionBegan;
            entry.seconds = Time.time - began;
            entry.frame = Time.frameCount;
            entry.realtime = Time.unscaledTime;
            string json = JsonUtility.ToJson(entry);
            writer?.WriteLine(json);
            if (entry.kind == "operation")
            {
                operations?.WriteLine(json);
            }

            if (Time.unscaledTime - lastFlush >= 1f)
            {
                writer?.Flush();
                operations?.Flush();
                lastFlush = Time.unscaledTime;
            }
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Fail(exception);
        }
    }

    private void Fail(Exception exception)
    {
        failed = true;
        Error = exception.Message;
        Debug.LogWarning("Navigation lab recording stopped: " + Error);
        Dispose();
    }

    private void CloseTrial()
    {
        if (writer != null)
        {
            try
            {
                writer.Dispose();
            }
            catch (IOException exception)
            {
                Error = exception.Message;
            }

            writer = null;
        }
    }

    public void Dispose()
    {
        CloseTrial();
        if (operations == null)
        {
            return;
        }

        try
        {
            operations.Dispose();
        }
        catch (IOException exception)
        {
            Error = exception.Message;
        }

        operations = null;
    }
}
