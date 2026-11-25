// 职责：从两份正式场景重新读取天气绑定，并在 Play Mode 强制覆盖两种随机结果。
// 入口：Tools/HY-Sandbox/Weather/Verify，在 Play Mode 调用。
using System;
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
            () => VerifyScene(
                GameModeAuthoring.SiegePath,
                false,
                () =>
        {
            Debug.Log("Weather checks passed: both scene references, 30-second warnings, two weather prefabs, cleanup and Siege early-clear gate.");
        }));
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
