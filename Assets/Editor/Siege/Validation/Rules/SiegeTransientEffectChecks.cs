// 职责：检查短时效果复用、重置、闲置上限及场景卸载；不替代自然画面验证。
// 入口：Run() / Status；卸载异步完成后写出最终报告。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SiegeTransientEffectChecks
{
    private static readonly List<string> checks = new List<string>();
    private static AsyncOperation unload;
    private static GameObject ownedRoot;
    private static BeamFade ownedBeam;
    private static LightFade ownedLight;
    private static Exception failure;
    public static string Status { get; private set; } = "Not run";

    public static string Run()
    {
        if (!EditorApplication.isPlaying || Object.FindFirstObjectByType<TransientEffectPool>() != null || unload != null)
        {
            throw new InvalidOperationException("Requires fresh Play without a transient pool.");
        }

        checks.Clear();
        failure = null;
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/transient-checks.json", "{\"status\":\"running\"}");
        Scene original = SceneManager.GetActiveScene();
        Scene fixture = SceneManager.CreateScene("Owned transient lifecycle fixture");
        SceneManager.SetActiveScene(fixture);
        try
        {
            BeamFade beam = TransientEffectPool.RentBeam();
            ownedRoot = beam.transform.parent.gameObject;
            ownedBeam = beam;
            StylizedBeamEffect visual = beam.GetComponent<StylizedBeamEffect>();
            visual.SetEndpoints(Vector3.one * 10000f, Vector3.one * 10000f + Vector3.forward);
            visual.SetIntensity(0.2f);
            visual.SetVisible(true);
            beam.Initialize(visual, Color.cyan, 0.1f);
            Expire(beam);
            Check(
                "beam expires inactive with stopped GPU emission",
                !beam.gameObject.activeSelf && !visual.GetComponentInChildren<VfxEffect>(true).IsEmitting);
            Expire(beam);
            BeamFade reused = TransientEffectPool.RentBeam();
            Check("expired beam reuses same object", reused == beam);
            BeamFade concurrent = TransientEffectPool.RentBeam();
            Check("duplicate expiration cannot duplicate a lease", concurrent != reused);
            reused.Initialize(visual, Color.red, 2f);
            Check(
                "beam initialization resets elapsed and lifetime",
                Read<float>(reused, "elapsed") == 0f && Read<float>(reused, "duration") == 2f);
            Expire(reused);
            Expire(concurrent);
            LightFade light = TransientEffectPool.RentLight(Vector3.one * 10000f);
            ownedLight = light;
            Light lamp = light.GetComponent<Light>();
            lamp.intensity = 8f;
            light.Initialize(lamp, 0.1f, 8f);
            Expire(light);
            Check("light expires inactive at zero intensity", !light.gameObject.activeSelf && lamp.intensity == 0f);
            LightFade next = TransientEffectPool.RentLight(Vector3.one * 10001f);
            Check(
                "light reuses same object at new position",
                next == light && next.transform.position == Vector3.one * 10001f);
            next.Initialize(lamp, 3f, 4f);
            Check(
                "light initialization resets elapsed intensity and lifetime",
                Read<float>(next, "elapsed") == 0f && Read<float>(next, "duration") == 3f && Read<float>(next, "startIntensity") == 4f);
            Expire(next);
            Type transient = typeof(VisualEffectsManager).Assembly.GetType("TransientEffects");
            MethodInfo createBeam = transient.GetMethod("CreateLineStreak");
            Vector3 origin = Vector3.one * 10000f;
            createBeam.Invoke(null, new object[] { origin, origin + Vector3.forward * 2f, Color.red, 0.2f, 0.08f });
            BeamFade first = Object.FindObjectsByType<BeamFade>(FindObjectsSortMode.None).Single(item => item.gameObject.scene == fixture);
            Expire(first);
            createBeam.Invoke(null, new object[] { origin + Vector3.right, origin + Vector3.up, Color.green, 1f, 0.02f });
            BeamFade second = Object.FindObjectsByType<BeamFade>(FindObjectsSortMode.None).Single(item => item.gameObject.scene == fixture);
            StylizedBeamEffect secondVisual = second.GetComponent<StylizedBeamEffect>();
            Check(
                "production beam resets intensity color and width",
                second == first && Read<float>(secondVisual, "_intensity") == 1f && Read<float>(secondVisual, "_width") == 0.02f && Read<Color>(secondVisual, "_color") == Color.green);
            Check(
                "production beam resets endpoints and lifetime",
                Read<Vector3>(secondVisual, "_start") == origin + Vector3.right && Read<Vector3>(secondVisual, "_end") == origin + Vector3.up && Read<float>(second, "elapsed") == 0f && Read<float>(second, "duration") == 1f);
            Expire(second);
            MethodInfo createLight = transient.GetMethod("CreateLightFlash");
            createLight.Invoke(null, new object[] { origin, Color.red, 8f, 4f, 0.1f });
            LightFade firstLight = Object.FindObjectsByType<LightFade>(FindObjectsSortMode.None).Single(item => item.gameObject.scene == fixture);
            Expire(firstLight);
            createLight.Invoke(null, new object[] { origin + Vector3.up, Color.blue, 2f, 7f, 1f });
            LightFade secondLight = Object.FindObjectsByType<LightFade>(FindObjectsSortMode.None).Single(item => item.gameObject.scene == fixture);
            Light secondLamp = secondLight.GetComponent<Light>();
            Check(
                "production light resets color intensity range and position",
                secondLight == firstLight && secondLamp.color == Color.blue && secondLamp.intensity == 2f && secondLamp.range == 7f && secondLight.transform.position == origin + Vector3.up);
            Expire(secondLight);
            var beams = new List<BeamFade>();
            var lights = new List<LightFade>();
            for (int i = 0; i < 40; i++)
            {
                beams.Add(TransientEffectPool.RentBeam());
                lights.Add(TransientEffectPool.RentLight(Vector3.one * 10000f));
            }

            Check("active effects are not capped by idle budgets", beams.Count == 40 && lights.Count == 40);
            foreach (BeamFade item in beams)
            {
                Expire(item);
            }

            foreach (LightFade item in lights)
            {
                Expire(item);
            }

            TransientEffectPool pool = ownedRoot.GetComponent<TransientEffectPool>();
            Check("idle beams bounded to32", Read<System.Collections.ICollection>(pool, "beams").Count == 32);
            Check("idle lights bounded to16", Read<System.Collections.ICollection>(pool, "lights").Count == 16);
            Check("pool is owned by fixture scene", ownedRoot.scene == fixture);
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            unload = SceneManager.UnloadSceneAsync(fixture);
            Status = "Waiting for scene unload";
            EditorApplication.update += CompleteUnload;
        }

        return Status;
    }

    private static void CompleteUnload()
    {
        if (unload != null && !unload.isDone)
        {
            return;
        }

        EditorApplication.update -= CompleteUnload;
        unload = null;
        try
        {
            Check(
                "scene unload destroys active and idle effects",
                ownedRoot == null && ownedBeam == null && ownedLight == null);
            Check("scene unload removes pool singleton", Object.FindFirstObjectByType<TransientEffectPool>() == null);
        }
        catch (Exception error)
        {
            failure = error;
        }

        Status = "Temp/Siege/transient-checks.json";
        File.WriteAllText(
            Status,
            JsonUtility.ToJson(
                new Report { status = failure == null ? "passed" : "failed", error = failure?.ToString(), checks = checks },
                true));
    }

    private static T Read<T>(object owner, string field)
    {
        return (T)owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
    }

    private static void Expire(MonoBehaviour fade)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        fade.GetType().GetField("elapsed", flags).SetValue(fade, 100f);
        fade.GetType().GetMethod("Update", flags).Invoke(fade, null);
    }

    private static void Check(string label, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(label);
        }

        checks.Add(label);
    }

    [Serializable]
    private sealed class Report
    {
        public string status;
        public string error;
        public List<string> checks;
    }
}
