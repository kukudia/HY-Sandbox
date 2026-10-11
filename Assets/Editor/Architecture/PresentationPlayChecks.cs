// 职责：在真实 Play 会话中验证镜头/面板协程取消与临时 VFX 的销毁生命周期。
// 入口：Run(checks)；外层探针逐步 MoveNext，Current 是以秒为单位的等待时长。
// 生命周期：仅创建隔离对象；恢复单例与 Random 状态，不控制 Editor 或改动场景资产。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

internal static class PresentationPlayChecks
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static IEnumerator Run(List<string> checks)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Presentation lifecycle checks require Play Mode.");
        }

        var failures = new List<string>();
        var ownedObjects = new List<GameObject>();
        VisualEffectsManager previousEffects = VisualEffectsManager.instance;
        MainUIPanels previousPanels = MainUIPanels.instance;
        Random.State previousRandom = Random.state;
        GameObject root = Own(ownedObjects, "Architecture presentation lifecycle probe");
        try
        {
            Random.InitState(1937);
            GameObject effectsObject = Own(ownedObjects, "Probe VFX runner", root.transform);
            effectsObject.SetActive(false);
            VisualEffectsManager.instance = null;
            VisualEffectsManager runner = effectsObject.AddComponent<VisualEffectsManager>();
            runner.enableSceneLook = false;
            effectsObject.SetActive(true);
            object shake = GetField(runner, "_cameraShake");
            if (shake == null)
            {
                shake = CreateRuntimeObject("CameraShake", runner);
                SetField(runner, "_cameraShake", shake);
            }

            GameObject firstCameraObject = Own(ownedObjects, "Probe camera A", root.transform);
            GameObject secondCameraObject = Own(ownedObjects, "Probe camera B", root.transform);
            Camera firstCamera = firstCameraObject.AddComponent<Camera>();
            Camera secondCamera = secondCameraObject.AddComponent<Camera>();
            firstCamera.enabled = false;
            secondCamera.enabled = false;
            Vector3 firstOrigin = new Vector3(2f, 3f, 4f);
            Vector3 secondOrigin = new Vector3(-4f, 1f, 7f);
            firstCamera.transform.localPosition = firstOrigin;
            secondCamera.transform.localPosition = secondOrigin;
            Invoke(shake, "Play", firstCamera, 0.5f, 2f);
            yield return 0.05f;
            Check(
                checks,
                failures,
                "Camera shake offsets its isolated camera during Play",
                !Near(firstCamera.transform.localPosition, firstOrigin));
            Invoke(shake, "Cancel");
            Check(
                checks,
                failures,
                "Camera shake cancellation restores the exact base position",
                Near(firstCamera.transform.localPosition, firstOrigin));
            Invoke(shake, "Play", firstCamera, 0.5f, 2f);
            Invoke(shake, "Play", secondCamera, 0.4f, 2f);
            Check(
                checks,
                failures,
                "Replacing the shaken camera clears the previous camera offset",
                Near(firstCamera.transform.localPosition, firstOrigin));
            runner.enabled = false;
            Check(
                checks,
                failures,
                "Disabling the VFX host cancels shake and restores the replacement camera",
                Near(secondCamera.transform.localPosition, secondOrigin));
            Invoke(shake, "Play", firstCamera, 0.5f, 2f);
            yield return 0.05f;
            Check(
                checks,
                failures,
                "A disabled VFX host cannot start a new shake",
                Near(firstCamera.transform.localPosition, firstOrigin));
            runner.enabled = true;
            Invoke(shake, "Play", firstCamera, 0.5f, 0.04f);
            yield return 0.12f;
            Check(
                checks,
                failures,
                "A naturally completed shake removes its final offset",
                Near(firstCamera.transform.localPosition, firstOrigin));
            GameObject panelRunnerObject = Own(ownedObjects, "Probe panel runner", root.transform);
            MainUIPanels panelRunner = panelRunnerObject.AddComponent<MainUIPanels>();
            object transitions = GetField(panelRunner, "_transitions");
            GameObject panel = Own(ownedObjects, "Probe transition panel", root.transform);
            Invoke(transitions, "Show", panel, true, 0f);
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            Check(
                checks,
                failures,
                "Zero-duration show creates CanvasGroup and enables interaction immediately",
                panel.activeSelf && group != null && group.alpha == 1f && group.interactable && group.blocksRaycasts);
            Invoke(transitions, "Show", panel, false, 0f);
            Check(
                checks,
                failures,
                "Zero-duration hide disables panel and interaction immediately",
                !panel.activeSelf && group.alpha == 0f && !group.interactable && !group.blocksRaycasts);
            Invoke(transitions, "Show", panel, true, 2f);
            yield return 0.05f;
            Invoke(transitions, "CancelAll");
            float cancelledAlpha = group.alpha;
            yield return 0.08f;
            Check(
                checks,
                failures,
                "CancelAll stops alpha changes and clears active panel transitions",
                Mathf.Approximately(group.alpha, cancelledAlpha) && ((IDictionary)GetField(transitions, "_transitions")).Count == 0);
            Invoke(transitions, "Show", panel, true, 2f);
            Invoke(transitions, "Show", panel, false, 0f);
            yield return 0.05f;
            Check(
                checks,
                failures,
                "Immediate replacement prevents an older fade from re-showing the panel",
                !panel.activeSelf && group.alpha == 0f && ((IDictionary)GetField(transitions, "_transitions")).Count == 0);
            Invoke(transitions, "Show", panel, true, 2f);
            panelRunner.enabled = false;
            float disabledAlpha = group.alpha;
            yield return 0.08f;
            Check(
                checks,
                failures,
                "Disabling the panel host cancels fades without later alpha writes",
                Mathf.Approximately(group.alpha, disabledAlpha) && ((IDictionary)GetField(transitions, "_transitions")).Count == 0);
            panelRunner.enabled = true;
            Invoke(transitions, "Show", panel, true, 0.04f);
            yield return 0.12f;
            Check(
                checks,
                failures,
                "A completed fade reaches its final alpha and removes the transition",
                group.alpha == 1f && ((IDictionary)GetField(transitions, "_transitions")).Count == 0);
            var existingBeams = new HashSet<int>();
            foreach (BeamFade beam in Object.FindObjectsByType<BeamFade>(FindObjectsSortMode.None))
            {
                existingBeams.Add(beam.GetInstanceID());
            }

            var existingLights = new HashSet<int>();
            foreach (LightFade light in Object.FindObjectsByType<LightFade>(FindObjectsSortMode.None))
            {
                existingLights.Add(light.GetInstanceID());
            }

            Type effectsType = RuntimeType("TransientEffects");
            InvokeStatic(
                effectsType,
                "CreateLineStreak",
                Vector3.zero,
                Vector3.forward,
                Color.cyan,
                0.05f,
                0.02f);
            InvokeStatic(
                effectsType,
                "CreateRadialStreakBurst",
                Vector3.zero,
                Vector3.up,
                Color.cyan,
                3,
                0.5f,
                0.05f,
                0.02f);
            InvokeStatic(
                effectsType,
                "CreateLightFlash",
                Vector3.zero,
                Color.cyan,
                3f,
                1f,
                0.05f);
            var transientObjects = new List<GameObject>();
            foreach (BeamFade beam in Object.FindObjectsByType<BeamFade>(FindObjectsSortMode.None))
            {
                if (!existingBeams.Contains(beam.GetInstanceID()))
                {
                    beam.transform.SetParent(root.transform, true);
                    transientObjects.Add(beam.gameObject);
                    ownedObjects.Add(beam.gameObject);
                }
            }

            foreach (LightFade light in Object.FindObjectsByType<LightFade>(FindObjectsSortMode.None))
            {
                if (!existingLights.Contains(light.GetInstanceID()))
                {
                    light.transform.SetParent(root.transform, true);
                    transientObjects.Add(light.gameObject);
                    ownedObjects.Add(light.gameObject);
                }
            }

            Check(
                checks,
                failures,
                "Transient line, three radial streaks and flash create five lifetime-owned objects",
                transientObjects.Count == 5);
            yield return 0.2f;
            bool allDestroyed = true;
            foreach (GameObject transient in transientObjects)
            {
                allDestroyed &= transient == null;
            }

            Check(
                checks,
                failures,
                "Transient beam and light fade objects are destroyed after their real Play lifetime",
                allDestroyed);
            GameObject missingBeamObject = Own(ownedObjects, "Probe missing beam target", root.transform);
            missingBeamObject.AddComponent<BeamFade>();
            GameObject missingLightObject = Own(ownedObjects, "Probe missing light target", root.transform);
            missingLightObject.AddComponent<LightFade>();
            GameObject removedLightObject = Own(ownedObjects, "Probe removed light target", root.transform);
            Light removedLight = removedLightObject.AddComponent<Light>();
            removedLightObject.AddComponent<LightFade>().Initialize(removedLight, 10f, 1f);
            Object.Destroy(removedLight);
            yield return 0.12f;
            Check(
                checks,
                failures,
                "Missing or destroyed beam/light targets clean up their lifetime host objects",
                missingBeamObject == null && missingLightObject == null && removedLightObject == null);
            Invoke(shake, "Play", firstCamera, 0.5f, 2f);
            Object.Destroy(effectsObject);
            yield return 0.08f;
            Check(
                checks,
                failures,
                "Destroying the VFX host restores any remaining camera offset",
                Near(firstCamera.transform.localPosition, firstOrigin));
            if (failures.Count != 0)
            {
                throw new InvalidOperationException(string.Join("; ", failures));
            }
        }
        finally
        {
            // Cancel before deferred destruction, so restored scene singletons cannot inherit probe activity.
            if (VisualEffectsManager.instance != null && VisualEffectsManager.instance != previousEffects)
            {
                VisualEffectsManager.instance.enabled = false;
            }

            if (MainUIPanels.instance != null && MainUIPanels.instance != previousPanels)
            {
                MainUIPanels.instance.enabled = false;
            }

            foreach (GameObject owned in ownedObjects)
            {
                if (owned != null)
                {
                    Object.Destroy(owned);
                }
            }

            VisualEffectsManager.instance = previousEffects;
            MainUIPanels.instance = previousPanels;
            Random.state = previousRandom;
        }
    }

    private static GameObject Own(List<GameObject> owned, string name, Transform parent = null)
    {
        GameObject gameObject = new GameObject(name);
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        owned.Add(gameObject);
        return gameObject;
    }

    private static Type RuntimeType(string name)
    {
        return typeof(VisualEffectsManager).Assembly.GetType(name, true);
    }

    private static object CreateRuntimeObject(string name, MonoBehaviour runner)
    {
        return Activator.CreateInstance(RuntimeType(name), InstanceMembers, null, new object[] { runner }, null);
    }

    private static object GetField(object target, string name)
    {
        return target.GetType().GetField(name, InstanceMembers).GetValue(target);
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, InstanceMembers).SetValue(target, value);
    }

    private static void Invoke(object target, string name, params object[] arguments)
    {
        target.GetType().GetMethod(name, InstanceMembers).Invoke(target, arguments);
    }

    private static void InvokeStatic(Type type, string name, params object[] arguments)
    {
        type.GetMethod(name, StaticMembers).Invoke(null, arguments);
    }

    private static bool Near(Vector3 actual, Vector3 expected)
    {
        return (actual - expected).sqrMagnitude <= 0.000001f;
    }

    private static void Check(List<string> checks, List<string> failures, string name, bool passed)
    {
        checks.Add((passed ? "PASS: " : "FAIL: ") + name);
        if (!passed)
        {
            failures.Add(name);
        }
    }
}
