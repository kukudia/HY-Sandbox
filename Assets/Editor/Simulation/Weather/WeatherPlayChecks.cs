// 职责：从两份正式场景重新读取天气绑定，并在 Play Mode 强制覆盖两种随机结果。
// 入口：Tools/HY-Sandbox/Weather/Verify，在 Play Mode 调用。
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WeatherPlayChecks
{
    [MenuItem("Tools/HY-Sandbox/Weather/Verify")]
    public static void Verify()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Run the weather checks in Play Mode.");
        }

        VerifyScene(
            GameModeAuthoring.ExplorationPath,
            true,
            () => VerifyScene(GameModeAuthoring.SiegePath, false, VerifyWind));
    }

    private static void VerifyScene(string path, bool exploration, Action completed)
    {
        AsyncOperation loading = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
        loading.completed += _ =>
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool passed = false;
            try
            {
                Component host = exploration ? SceneObjectQuery.FindInScene<WorldSessionManager>(scene) : SceneObjectQuery.FindInScene<SiegeGameplayStage>(scene);
                WeatherEventDirector weather = (WeatherEventDirector)new SerializedObject(host).FindProperty("weather").objectReferenceValue;
                Require(weather != null && weather.gameObject.scene == scene, "Missing scene weather reference.");
                VerifyEvents(weather, exploration);
                passed = true;
            }
            finally
            {
                AsyncOperation unloading = SceneManager.UnloadSceneAsync(scene);
                if (passed)
                {
                    unloading.completed += _ => completed();
                }
            }
        };
    }

    private static void VerifyEvents(WeatherEventDirector weather, bool exploration)
    {
        float now = Time.time;
        Vector3 focus = new Vector3(280f, 500f, 180f);
        SerializedObject config = new SerializedObject(weather);
        config.FindProperty("eventChance").floatValue = 1f;
        config.ApplyModifiedPropertiesWithoutUndo();
        TMP_Text label = weather.GetComponentInChildren<TMP_Text>(true);
        var results = new HashSet<string>();
        for (int seed = 0; seed < 20 && results.Count < 2; seed++)
        {
            weather.StartExploration(now, seed);
            if (exploration)
            {
                weather.TickExploration(now, focus);
            }
            else
            {
                weather.BeginWave(now);
            }

            string title = label.text.Split('/')[0].Trim();
            results.Add(title);
            Require(
                weather.HasEvent && label.isActiveAndEnabled && label.text.Contains("30s"),
                "Weather must warn at wave start.");
            weather.Tick(now + 29.9f, focus);
            Require(
                label.text.Contains("01s") && CountActiveHazards(weather) == 0,
                "Weather began before the warning elapsed.");
            SiegeMatch match = new SiegeMatch(
                0,
                1,
                0,
                1,
                2,
                10);
            match.EnableReturnSettlement();
            match.BeginNextWave();
            match.SetWaveObjectivePending(weather.HasEvent);
            match.RegisterAttacker("raider", 1);
            match.ResolveAttacker("raider", true);
            Require(
                match.Phase == SiegePhase.Battle && match.PendingCoins == 2,
                "Clearing the enemy early should retain the weather wave.");
            weather.Tick(now + 30f, focus);
            Require(
                label.text.Contains("ACTIVE") && CountActiveHazards(weather) == 1,
                "Expected one active hazard when the countdown expires.");
            if (title == "METEOR SHOWER")
            {
                Rigidbody body = weather.GetComponentInChildren<Rigidbody>();
                Require(
                    body != null && body.linearVelocity.y < -1f && body.position.y > focus.y,
                    "Meteor must fall near the focus.");
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
                float measuredSpeed = horizontalVelocity.magnitude;
                Require(Mathf.Abs(measuredSpeed - 36f) < .1f, "Meteor horizontal speed must be 36 m/s.");
                weather.Tick(now + 30.29f, focus);
                Require(CountActiveHazards(weather) == 1, "Meteor interval must not elapse before 0.30 seconds.");
                weather.Tick(now + 30.31f, focus);
                Require(CountActiveHazards(weather) == 2, "Expected a second meteor at the new interval.");
            }
            else
            {
                WeatherTornado tornado = weather.GetComponentInChildren<WeatherTornado>();
                Require(
                    tornado != null && tornado.transform.position.y == focus.y,
                    "Tornado must start at the focus altitude.");
            }

            weather.Tick(now + 56f, focus);
            match.SetWaveObjectivePending(weather.HasEvent);
            Require(
                !label.isActiveAndEnabled && CountActiveHazards(weather) == 0,
                "Weather must clean up its alert and spawned objects.");
            Require(
                match.Phase == SiegePhase.Returning && match.PendingCoins == 12,
                "Weather completion must start return and settle the wave once.");
        }

        Require(results.SetEquals(new[] { "TORNADO", "METEOR SHOWER" }), "Both weather types must be reachable.");
        weather.BeginWave(now);
        weather.Clear();
        Require(!weather.HasEvent && !label.isActiveAndEnabled, "Cancellation must dismiss the warning.");
    }

    private static void VerifyWind()
    {
        const string path = "Assets/Art/Weather/Weather Tornado.prefab";
        GameObject root = new GameObject("Weather wind checks");
        WeatherTornado tornado = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform).GetComponent<WeatherTornado>();
        tornado.Begin(new Vector3(280f, 500f, 180f), 0f);
        tornado.StartCoroutine(VerifyWindAfterPhysics(root, tornado.transform.position));
    }

    private static IEnumerator VerifyWindAfterPhysics(GameObject root, Vector3 center)
    {
        try
        {
            Rigidbody inner = CreateWindBody(root.transform, center + Vector3.forward * 10f, false);
            Rigidbody outer = CreateWindBody(root.transform, center + Vector3.forward * 34f, false);
            Rigidbody above = CreateWindBody(root.transform, center + Vector3.up * 60f + Vector3.forward * 10f, false);
            Vector3 fixedPosition = center + Vector3.forward * 10f;
            Rigidbody fixedBody = CreateWindBody(root.transform, fixedPosition, true);
            Rigidbody outside = CreateWindBody(root.transform, center + Vector3.forward * 50f, false);
            float duration = 3 * Time.fixedDeltaTime;
            for (int step = 0; step < 3; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            Require(inner.linearVelocity.z < -14f * duration, "A collider-free body should receive the stronger pull.");
            Require(
                outer.linearVelocity.z < 0f && above.linearVelocity.z < 0f,
                "Bodies in the extended wind field should be pulled.");
            Require(fixedBody.position == fixedPosition, "Wind must leave kinematic bodies in place.");
            Require(outside.linearVelocity == Vector3.zero, "Wind must leave bodies beyond its range alone.");
            Debug.Log("Weather checks passed: both scenes, warning, meteor cadence and speed, tornado pull and range, cleanup and Siege early-clear gate.");
        }
        finally
        {
            UnityEngine.Object.Destroy(root);
        }
    }

    private static Rigidbody CreateWindBody(Transform root, Vector3 position, bool kinematic)
    {
        GameObject probe = new GameObject("Weather wind probe");
        probe.transform.SetParent(root);
        probe.transform.position = position;
        Rigidbody body = probe.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = kinematic;
        return body;
    }

    private static int CountActiveHazards(WeatherEventDirector weather)
    {
        return weather.GetComponentsInChildren<Rigidbody>().Length + weather.GetComponentsInChildren<WeatherTornado>().Length;
    }

    private static void Require(bool passed, string message)
    {
        if (!passed)
        {
            throw new InvalidOperationException(message);
        }
    }
}
