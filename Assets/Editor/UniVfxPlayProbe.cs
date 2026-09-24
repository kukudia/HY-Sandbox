using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Tests the real burst factory and detached-body trail lifecycle in isolation.</summary>
[InitializeOnLoad]
public static class UniVfxPlayProbe
{
    private const string Pending = "HY.UniVfxProbe.Pending";
    private const string Previous = "HY.UniVfxProbe.Previous";
    private static readonly List<string> Errors = new List<string>();
    private static readonly List<string> Checks = new List<string>();
    private static AssetParticleEffect _trail;
    private static float _start;
    private static double _wallStart;
    private static int _phase;
    private static bool _background;

    static UniVfxPlayProbe() => EditorApplication.playModeStateChanged += State;

    [MenuItem("Tools/HY Sandbox/UNI VFX/Run Play Mode Probe")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save scene and exit Play Mode first.");
        SessionState.SetString(Previous, SceneManager.GetActiveScene().path);
        SessionState.SetBool(Pending, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
            EditorSceneManager.OpenScene(SessionState.GetString(Previous, ""));
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        Errors.Clear(); Checks.Clear(); _phase = 0;
        _background = Application.runInBackground; Application.runInBackground = true;
        Time.timeScale = 1; EditorApplication.isPaused = false;
        _start = Time.time; _wallStart = EditorApplication.timeSinceStartup;
        Application.logMessageReceived += Capture;
        try
        {
            foreach (var kind in new[] { BlockVfxLibrary.Effect.Explosion, BlockVfxLibrary.Effect.Break, BlockVfxLibrary.Effect.Impact, BlockVfxLibrary.Effect.Smoke })
                BlockVfxLibrary.Play(kind, Vector3.zero, Quaternion.identity);
            _trail = Object.Instantiate(BlockVfxLibrary.Instance.DetachedSmoke);
            _trail.SetIntensity(1f);
            var body = new GameObject("Moving wreck probe").AddComponent<Rigidbody>();
            body.useGravity = false; body.linearVelocity = Vector3.right * 4f;
            DetachedPartSmokeTrail.Attach(body, Vector3.zero, 1f);
            EditorApplication.update += Tick;
        }
        catch (Exception e) { Errors.Add(e.ToString()); Finish(); }
    }

    private static void Tick()
    {
        try
        {
            float elapsed = Time.time - _start;
            if (EditorApplication.timeSinceStartup - _wallStart > 40) throw new TimeoutException("Play Mode did not advance 10 seconds.");
            if (_phase == 0 && elapsed >= 0.3f)
            {
                var effects = Object.FindObjectsByType<AssetParticleEffect>(FindObjectsSortMode.None);
                Check(effects.Length == 6, "Library factories and moving wreck created all six effects");
                Check(effects.All(e => e.GetComponentsInChildren<ParticleSystem>().Any(p => p.particleCount > 0)), "Every effect emitted in Play Mode");
                _trail.SetIntensity(0); _phase++;
            }
            if (_phase == 1 && elapsed >= 3.6f)
            {
                Check(Object.FindObjectsByType<AssetParticleEffect>(FindObjectsSortMode.None).Length == 2, "Four bursts automatically released after playback");
                Check(_trail.GetComponentsInChildren<ParticleSystem>().All(p => p.particleCount == 0), "Stopped fire/smoke trail dissipated");
                _trail.SetIntensity(0.25f); _phase++;
            }
            if (_phase == 2 && elapsed >= 4.2f)
            {
                Check(_trail.GetComponentsInChildren<ParticleSystem>().All(p => p.particleCount > 0), "Quarter-intensity fire, smoke and embers restarted");
                Object.Destroy(_trail.gameObject); _phase++;
            }
            if (_phase == 3 && elapsed >= 10f)
            {
                Check(Object.FindObjectsByType<AssetParticleEffect>(FindObjectsSortMode.None).Length == 0, "Detached wreck trail released including residual particles");
                Check(AssetParticleEffect.CanSpawnBurst, "Burst capacity returned after destruction");
                Finish();
            }
        }
        catch (Exception e) { Errors.Add(e.ToString()); Finish(); }
    }

    private static void Check(bool condition, string description) { if (condition) Checks.Add(description); else Errors.Add(description); }
    private static void Capture(string message, string stack, LogType type)
    { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) Errors.Add(message); }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Capture;
        Application.runInBackground = _background;
        File.WriteAllText(UniVfxIntegration.Root + "/PlayModeValidation.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { checks = Checks, errors = Errors }, Newtonsoft.Json.Formatting.Indented));
        EditorApplication.isPlaying = false;
    }
}
