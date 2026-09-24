using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

/// <summary>Captures real, automatically rendered game frames; VFX GPU dispatch requires the player loop.</summary>
[InitializeOnLoad]
public static class NativeVfxPlaybackProbe
{
    private const string Key = "HY.NativeVfxProbe.";
    private static readonly float[] Samples = { 0.12f, 0.4f, 0.9f, 1.8f, 3.2f, 5f, 5.2f };
    private static readonly List<object> Rows = new List<object>();
    private static GameObject _effect;
    private static Camera _camera;
    private static RenderTexture _target;
    private static Texture2D _image;
    private static float _start;
    private static int _sample;
    private static bool _background;
    private static double _wallStart;
    private static string _output;

    static NativeVfxPlaybackProbe()
    {
        EditorApplication.playModeStateChanged += State;
        EditorApplication.update += NextQueued;
    }
    private static double _nextQueued;
    private static void NextQueued()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating
            || SessionState.GetBool(Key + "Pending", false) || EditorApplication.timeSinceStartup < _nextQueued) return;
        string queue = SessionState.GetString(Key + "Queue", "");
        if (string.IsNullOrEmpty(queue)) return;
        string name = queue.Split(',')[0];
        SessionState.SetString(Key + "Queue", string.Join(",", queue.Split(',').Skip(1)));
        Run(BlockVfxBaker.Root + "/" + name + ".prefab", NativeVfxLibraryBuilder.Root + "/Preview/" + name);
    }

    [MenuItem("Tools/HY Sandbox/VFX Graph/5 Capture Lifecycle Suite")]
    public static void RunSuite()
    {
        SessionState.SetString(Key + "Queue", "BreakBurst,ImpactBurst,SmokeBurst,DetachedSmoke,ThrusterJet,RepairContact,EnergyBeam,EnergyTrail,BuildBurst");
        Run(BlockVfxBaker.Root + "/Explosion.prefab", NativeVfxLibraryBuilder.Root + "/Preview/Explosion");
    }

    public static void Run(string prefab, string output)
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save and exit Play Mode first.");
        SessionState.SetString(Key + "Scene", SceneManager.GetActiveScene().path);
        SessionState.SetString(Key + "Prefab", prefab);
        SessionState.SetString(Key + "Output", output);
        SessionState.SetBool(Key + "Pending", true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + "Pending", false)) return;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key + "Pending", false);
            EditorSceneManager.OpenScene(SessionState.GetString(Key + "Scene", ""));
            // Starting Play again inside the exit callback can race Unity's transition and
            // enter the restored user scene. Wait for a settled edit-mode update instead.
            _nextQueued = EditorApplication.timeSinceStartup + 2;
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _output = SessionState.GetString(Key + "Output", "Temp/NativeVfxProbe");
        Directory.CreateDirectory(_output);
        _background = Application.runInBackground;
        Application.runInBackground = true; Time.timeScale = 1; EditorApplication.isPaused = false;
        _camera = new GameObject("Native VFX capture camera").AddComponent<Camera>();
        _camera.transform.position = new Vector3(7, 4, -9); _camera.transform.LookAt(new Vector3(0, 0.5f, 0));
        _camera.orthographic = true; _camera.orthographicSize = 5f;
        _camera.clearFlags = CameraClearFlags.SolidColor; _camera.backgroundColor = new Color(0.12f, 0.14f, 0.17f);
        _target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGBHalf);
        _camera.targetTexture = _target;
        _image = new Texture2D(800, 600, TextureFormat.RGB24, false);
        _effect = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SessionState.GetString(Key + "Prefab", "")));
        foreach (var graph in _effect.GetComponentsInChildren<VisualEffect>(true))
        {
            graph.GetComponent<VFXRenderer>().enabled = true;
            graph.initialEventName = "ControlledStart"; graph.pause = false;
            graph.resetSeedOnPlay = false; graph.startSeed = 42;
            graph.Reinit();
        }
        var controller = _effect.GetComponent<VfxEffect>();
        if (controller != null) controller.PlayOnce();
        else foreach (var graph in _effect.GetComponentsInChildren<VisualEffect>()) graph.Play();
        if (_effect.name.Contains("Jet") || _effect.name.Contains("Flight")) _camera.orthographicSize = 1f;
        if (_effect.name.Contains("Energy") || _effect.name.Contains("Repair")) _camera.orthographicSize = 1.5f;
        if (_effect.name.Contains("EnergyBeam")) _effect.transform.localScale = new Vector3(0.04f, 0.04f, 2f);
        Rows.Clear(); _sample = 0; _start = Time.time; _wallStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup - _wallStart > 30) { Finish(); return; }
        float elapsed = Time.time - _start;
        if (_effect != null && _effect.name.Contains("EnergyTrail")) _effect.transform.position = new Vector3(Mathf.Sin(elapsed * 3f) * 0.6f, 0, 0);
        if (_sample >= Samples.Length || elapsed < Samples[_sample]) return;
        // Read the previous automatically completed camera frame, never Camera.Render inside an SRP render pass.
        var previous = RenderTexture.active;
        RenderTexture.active = _target;
        _image.ReadPixels(new Rect(0, 0, 800, 600), 0, 0); _image.Apply();
        var pixels = _image.GetPixels();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = pixels[i].gamma;
        _image.SetPixels(pixels); _image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(_output + "/" + _sample + ".png", _image.EncodeToPNG());
        Rows.Add(new { elapsed, frame = Time.frameCount, effects = _effect.GetComponentsInChildren<VisualEffect>().Select(g =>
            new { g.name, g.aliveParticleCount, g.culled, bounds = g.GetComponent<VFXRenderer>().bounds.ToString() }).ToArray() });
        _sample++;
        var controller = _effect.GetComponent<VfxEffect>();
        if (controller != null)
        {
            if (_sample == 4) controller.SetIntensity(0f);
            if (_sample == 5) controller.Clear();
            if (_sample == 6) controller.PlayOnce();
        }
        if (_sample == Samples.Length) Finish();
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        File.WriteAllText(_output + "/Frames.json", Newtonsoft.Json.JsonConvert.SerializeObject(Rows, Newtonsoft.Json.Formatting.Indented));
        _camera.targetTexture = null;
        Object.Destroy(_target); Object.Destroy(_image);
        Application.runInBackground = _background;
        EditorApplication.isPlaying = false;
    }
}
