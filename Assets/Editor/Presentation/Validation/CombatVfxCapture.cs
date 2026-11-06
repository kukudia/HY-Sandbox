// 职责：在隔离 Play 场景自动拍摄残骸尾烟、激光和弹道，记录销毁后烟雾存活证据。
// 入口：StartCapture()；完成/超时恢复原场景，释放截图资源，不保存用户场景。
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

[InitializeOnLoad]
public static class CombatVfxCapture
{
    private const string Key = "HY.CombatVfxCapture.";
    private static readonly float[] Times =
    {
        0.6f,
        1.4f,
        2.4f,
        3.4f,
        5.4f
    };
    private static readonly List<object> Rows = new List<object>();
    private static Camera camera;
    private static RenderTexture target;
    private static Texture2D image;
    private static Rigidbody debris;
    private static GameObject[] shells;
    private static float[] lastPositions;
    private static StylizedBeamEffect laser;
    private static float started;
    private static double wallStarted;
    private static int sample;
    private static bool background;
    private static bool sawTail;
    static CombatVfxCapture()
    {
        EditorApplication.playModeStateChanged += State;
    }

    [MenuItem("Tools/HY Sandbox/资源管理/拍摄战斗特效")]
    public static void StartCapture()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                throw new InvalidOperationException("Save scene edits before capture.");
            }
        }

        SessionState.SetString(Key + "Scene", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key + "Pending", true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    public static void StartEnvelopeCapture()
    {
        StartCapture();
        SessionState.SetBool(Key + "Envelope", true);
    }

    private static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + "Pending", false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key + "Pending", false);
            SessionState.SetBool(Key + "Envelope", false);
            string path = SessionState.GetString(Key + "Scene", "");
            if (!string.IsNullOrEmpty(path))
            {
                EditorSceneManager.OpenScene(path);
            }
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Setup();
        }
    }

    private static void Setup()
    {
        Directory.CreateDirectory("Logs/CombatVfxPreview");
        File.WriteAllText("Logs/CombatVfxPreview/checks.txt", CombatVfxChecks.RunPlay());
        background = Application.runInBackground;
        Application.runInBackground = true;
        camera = new GameObject("Combat VFX camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 3, -22);
        camera.transform.LookAt(new Vector3(0, 1, 0));
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.backgroundColor = new Color(0.16f, 0.19f, 0.24f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        target = new RenderTexture(1440, 900, 24, RenderTextureFormat.ARGBHalf);
        camera.targetTexture = target;
        image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
        GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fragment.name = "Moving debris";
        fragment.transform.position = new Vector3(-6, 3.4f, 0);
        fragment.transform.localScale = SessionState.GetBool(Key + "Envelope", false) ? new Vector3(1.4f, 0.65f, 0.8f) : Vector3.one * 0.28f;
        fragment.GetComponent<Collider>().enabled = false;
        debris = fragment.AddComponent<Rigidbody>();
        debris.useGravity = false;
        debris.linearVelocity = new Vector3(3.2f, 0, 0);
        debris.angularVelocity = new Vector3(1, 2, 0);
        DetachedPartSmokeTrail.Attach(debris, fragment.transform.position, 1f);
        laser = new GameObject("Laser").AddComponent<StylizedBeamEffect>();
        laser.Configure(0.16f, "LaserBeam");
        laser.SetColor(new Color(6f, 1.1f, 0.15f));
        laser.SetEndpoints(new Vector3(-6, 0, 0), new Vector3(6, 0, 0));
        laser.SetVisible(true);
        shells = new GameObject[3];
        lastPositions = new float[3];
        for (int i = 0; i < shells.Length; i++)
        {
            shells[i] = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Combat/TurretShell.prefab"));
            shells[i].GetComponent<TurretProjectile>().enabled = false;
            var body = shells[i].GetComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            shells[i].GetComponent<Collider>().enabled = false;
            shells[i].transform.rotation = Quaternion.LookRotation(Vector3.right);
            shells[i].GetComponent<ProjectileTracer>().Begin();
        }

        var light = new GameObject("Key light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        light.intensity = 1.2f;
        Rows.Clear();
        sample = 0;
        sawTail = false;
        started = Time.time;
        wallStarted = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        float elapsed = Time.time - started;
        if (debris != null && elapsed > 2.2f)
        {
            Object.Destroy(debris.gameObject);
        }

        for (int i = 0; i < shells.Length; i++)
        {
            float x = Mathf.Repeat(elapsed * (i == 0 ? 20 : i == 1 ? 42 : 85) + i * 3, 12) - 6;
            shells[i].transform.position = new Vector3(x, -1.7f - i * 0.8f, 0);
            if (x < lastPositions[i])
            {
                shells[i].GetComponent<ProjectileTracer>().Begin();
            }

            lastPositions[i] = x;
        }

        if (sample < Times.Length && elapsed >= Times[sample])
        {
            Capture(elapsed);
            sample++;
            if (sample == 2)
            {
                camera.orthographicSize = 12f;
            }
        }

        if (sample == Times.Length || EditorApplication.timeSinceStartup - wallStarted > 45)
        {
            Finish();
        }
    }

    private static void Capture(float elapsed)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        image.Apply();
        Color[] pixels = image.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = pixels[i].gamma;
        }

        image.SetPixels(pixels);
        image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes("Logs/CombatVfxPreview/" + sample + ".png", image.EncodeToPNG());
        VisualEffect[] smoke = Object.FindObjectsByType<VisualEffect>().Where(effect => effect.name == "Graph - DebrisSmoke").ToArray();
        VisualEffect[] fire = Object.FindObjectsByType<VisualEffect>().Where(effect => effect.name == "Graph - DebrisEnvelope").ToArray();
        sawTail |= debris == null && smoke.Any(effect => effect.aliveParticleCount > 0);
        Rows.Add(new
        {
            elapsed,
            frame = Time.frameCount,
            debrisAlive = debris != null,
            smokeParticles = smoke.Select(effect => effect.aliveParticleCount).ToArray(),
            fireParticles = fire.Select(effect => effect.aliveParticleCount).ToArray(),
            fireCenterError = debris != null && fire.Length > 0 ? Vector3.Distance(debris.transform.position, fire[0].transform.position) : 0f,
            fireRotationError = debris != null && fire.Length > 0 ? Quaternion.Angle(debris.transform.rotation, fire[0].transform.rotation) : 0f,
            flameSize = fire.Select(effect => effect.GetFloat("FlameSize")).ToArray()
        });
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        File.WriteAllText(
            "Logs/CombatVfxPreview/result.json",
            Newtonsoft.Json.JsonConvert.SerializeObject(new
{
    sawTailAfterDestruction = sawTail,
    samples = Rows
}, Newtonsoft.Json.Formatting.Indented));
        camera.targetTexture = null;
        target.Release();
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(image);
        Application.runInBackground = background;
        EditorApplication.isPlaying = false;
    }
}
