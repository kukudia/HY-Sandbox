#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Records unusually large physics impulses during Editor Play Mode.</summary>
public sealed class AbnormalImpulseRecorder : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _minimumVelocityChange = 8f;
    [SerializeField, Min(0f)] private float _minimumImpulse = 250f;
    [SerializeField, Min(0.1f)] private float _discoveryInterval = 0.5f;
    [SerializeField, Min(0f)] private float _repeatInterval = 0.5f;

    private static AbnormalImpulseRecorder _instance;
    private readonly Dictionary<Rigidbody, Vector3> _previousVelocities = new Dictionary<Rigidbody, Vector3>();
    private readonly Dictionary<Rigidbody, float> _lastWarningTimes = new Dictionary<Rigidbody, float>();
    private readonly List<Rigidbody> _missingBodies = new List<Rigidbody>();
    private readonly List<Rigidbody> _sampledBodies = new List<Rigidbody>();
    private float _nextDiscovery;
    private string _logPath;

    internal static void StartSession()
    {
        if (!Application.isPlaying || _instance != null) return;
        GameObject host = new GameObject("Abnormal Impulse Recorder (Editor only)");
        host.hideFlags = HideFlags.DontSave;
        _instance = host.AddComponent<AbnormalImpulseRecorder>();
    }

    public static void RecordCollision(Rigidbody body, Collision collision)
    {
        if (_instance == null || body == null || collision == null) return;
        float magnitude = collision.impulse.magnitude;
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : body.worldCenterOfMass;
        _instance.Record(body, "PhysX collision", magnitude, magnitude / Mathf.Max(body.mass, 0.001f),
            point, collision.collider != null ? collision.collider.name : null);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp", "AbnormalImpulseLogs");
        Directory.CreateDirectory(folder);
        _logPath = Path.Combine(folder, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv");
        File.WriteAllText(_logPath, "utc,frame,fixedTime,source,body,other,massKg,impulseNs,deltaVelocityMps,point,velocity\n");
        Debug.Log("[ImpulseRecorder] CSV: " + _logPath, this);
    }

    private void FixedUpdate()
    {
        if (Time.time >= _nextDiscovery)
        {
            _nextDiscovery = Time.time + _discoveryInterval;
            foreach (Rigidbody body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
                if (body != null && !_previousVelocities.ContainsKey(body))
                {
                    _previousVelocities.Add(body, body.linearVelocity);
                    if (body.GetComponent<AbnormalImpulseBodyProbe>() == null)
                        body.gameObject.AddComponent<AbnormalImpulseBodyProbe>();
                }
            _missingBodies.Clear();
            foreach (Rigidbody body in _previousVelocities.Keys)
                if (body == null) _missingBodies.Add(body);
            foreach (Rigidbody body in _missingBodies)
            {
                _previousVelocities.Remove(body);
                _lastWarningTimes.Remove(body);
            }
        }

        // Updating a Dictionary value invalidates its key enumerator in Unity's runtime.
        // Reuse a snapshot so the sampler cannot throw every physics step.
        _sampledBodies.Clear();
        _sampledBodies.AddRange(_previousVelocities.Keys);
        foreach (Rigidbody body in _sampledBodies)
        {
            if (body == null || body.isKinematic) continue;
            Vector3 velocity = body.linearVelocity;
            Vector3 gravity = body.useGravity ? Physics.gravity * Time.fixedDeltaTime : Vector3.zero;
            float deltaVelocity = (velocity - _previousVelocities[body] - gravity).magnitude;
            Record(body, "unattributed velocity jump", body.mass * deltaVelocity, deltaVelocity,
                body.worldCenterOfMass, null);
            _previousVelocities[body] = velocity;
        }
    }

    private void Record(Rigidbody body, string source, float impulse, float deltaVelocity,
        Vector3 point, string other)
    {
        if (impulse < _minimumImpulse || deltaVelocity < _minimumVelocityChange) return;
        if (_lastWarningTimes.TryGetValue(body, out float last) && Time.time - last < _repeatInterval) return;
        _lastWarningTimes[body] = Time.time;
        string name = HierarchyPath(body.transform);
        string message = string.Format(CultureInfo.InvariantCulture,
            "[ImpulseRecorder] {0}: {1}; {2:0.##} N s, delta-v={3:0.##} m/s, mass={4:0.##} kg, point={5}, other={6}; CSV: {7}",
            source, name, impulse, deltaVelocity, body.mass, point, other ?? "-", _logPath);
        Debug.LogWarning(message, body);
        string row = string.Join(",", Csv(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            Time.frameCount.ToString(CultureInfo.InvariantCulture),
            Time.fixedTime.ToString("R", CultureInfo.InvariantCulture), Csv(source), Csv(name), Csv(other ?? ""),
            body.mass.ToString("R", CultureInfo.InvariantCulture), impulse.ToString("R", CultureInfo.InvariantCulture),
            deltaVelocity.ToString("R", CultureInfo.InvariantCulture), Csv(point.ToString("F3")),
            Csv(body.linearVelocity.ToString("F3")));
        try { File.AppendAllText(_logPath, row + "\n"); }
        catch (IOException exception) { Debug.LogWarning("[ImpulseRecorder] CSV write failed: " + exception.Message, this); }
    }

    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    private static string HierarchyPath(Transform node)
    {
        string path = node.name;
        while (node.parent != null) { node = node.parent; path = node.name + "/" + path; }
        return path;
    }

    private void OnDestroy() { if (_instance == this) _instance = null; }
}

[InitializeOnLoad]
internal static class AbnormalImpulseRecorderBootstrap
{
    static AbnormalImpulseRecorderBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += EnsureSession;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) AbnormalImpulseRecorder.StartSession();
    }

    private static void EnsureSession()
    {
        if (EditorApplication.isPlaying) AbnormalImpulseRecorder.StartSession();
    }
}
#endif
