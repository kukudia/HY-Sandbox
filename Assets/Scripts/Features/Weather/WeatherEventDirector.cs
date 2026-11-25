// 职责：按玩法波次抽取天气，拥有预警、事件实例和清理。
// 入口：BeginWave() / Tick() / TickExploration() / Clear()。
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class WeatherEventDirector : MonoBehaviour
{
    public const float WarningSeconds = 30f;
    private const float MeteorFlightSeconds = 1.7f;
    [SerializeField, Range(0f, 1f)]
    private float eventChance = .4f;
    [SerializeField, Min(1f)]
    private float explorationWaveInterval = 90f;
    [SerializeField, Min(1f)]
    private float eventDuration = 25f;
    [SerializeField]
    private TMP_Text warning;
    [SerializeField]
    private GameObject warningBanner;
    [SerializeField]
    private Rigidbody meteorPrefab;
    [SerializeField]
    private WeatherTornado tornadoPrefab;
    [SerializeField, Min(5f)]
    private float meteorRadius = 42f;
    [SerializeField, Min(.2f)]
    private float meteorInterval = .30f;
    [SerializeField, Min(0f)]
    private float meteorHorizontalSpeed = 36f;
    private enum Weather
    {
        None,
        MeteorShower,
        Tornado
    }

    private readonly List<Rigidbody> meteors = new List<Rigidbody>();
    private System.Random random;
    private Weather current;
    private float startsAt;
    private float endsAt;
    private float nextMeteor;
    private float nextExplorationWave = float.PositiveInfinity;
    private int shownSecond = -1;
    private WeatherTornado tornado;
    public bool HasEvent => current != Weather.None;

    public void StartExploration(float now, int seed)
    {
        Clear();
        random = new System.Random(seed);
        nextExplorationWave = now;
    }

    public void TickExploration(float now, Vector3 focus)
    {
        if (now >= nextExplorationWave)
        {
            nextExplorationWave = now + Mathf.Max(1f, explorationWaveInterval);
            BeginWave(now);
        }

        Tick(now, focus);
    }

    public void BeginWave(float now)
    {
        if (!isActiveAndEnabled || HasEvent)
        {
            return;
        }

        random ??= new System.Random();
        if (random.NextDouble() >= eventChance)
        {
            return;
        }

        current = random.Next(2) == 0 ? Weather.MeteorShower : Weather.Tornado;
        startsAt = now + WarningSeconds;
        endsAt = startsAt + Mathf.Max(1f, eventDuration);
        nextMeteor = startsAt;
        Present((int)WarningSeconds);
    }

    public void Tick(float now, Vector3 focus)
    {
        if (!isActiveAndEnabled || !HasEvent)
        {
            return;
        }

        if (now < startsAt)
        {
            Present(Mathf.CeilToInt(startsAt - now));
            return;
        }

        if (now >= endsAt)
        {
            Finish();
            return;
        }

        Present(0);
        if (current == Weather.Tornado)
        {
            if (tornado == null && tornadoPrefab != null)
            {
                tornado = Instantiate(tornadoPrefab, transform);
                tornado.Begin(focus, (float)random.NextDouble() * Mathf.PI * 2f);
            }
        }
        else if (now >= nextMeteor && endsAt - now > MeteorFlightSeconds)
        {
            nextMeteor = now + Mathf.Max(.2f, meteorInterval);
            SpawnMeteor(focus);
        }
    }

    private void SpawnMeteor(Vector3 focus)
    {
        if (meteorPrefab == null)
        {
            return;
        }

        float angle = (float)random.NextDouble() * Mathf.PI * 2f;
        float radius = Mathf.Sqrt((float)random.NextDouble()) * meteorRadius;
        Vector3 target = focus + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        Vector3 horizontalVelocity = new Vector3(16f, 0f, 9f).normalized * meteorHorizontalSpeed;
        // Offset the launch point so faster drift still crosses the chosen target.
        Vector3 drift = horizontalVelocity * MeteorFlightSeconds;
        Rigidbody meteor = Instantiate(meteorPrefab, target - drift + Vector3.up * 85f, UnityEngine.Random.rotation, transform);
        float size = Mathf.Lerp(1.6f, 3.8f, (float)random.NextDouble());
        meteor.transform.localScale *= size;
        meteor.mass = size * 65f;
        meteor.linearVelocity = horizontalVelocity + Vector3.down * (85f / MeteorFlightSeconds);
        meteor.angularVelocity = Vector3.one * 2f;
        meteors.RemoveAll(IsDestroyed);
        meteors.Add(meteor);
        Destroy(meteor.gameObject, 7f);
    }

    private static bool IsDestroyed(Rigidbody body)
    {
        return body == null;
    }

    private void Present(int seconds)
    {
        if (warning == null || warningBanner == null || seconds == shownSecond)
        {
            return;
        }

        shownSecond = seconds;
        string title = current == Weather.MeteorShower ? "METEOR SHOWER" : "TORNADO";
        warning.text = seconds > 0 ? $"{title}  /  IN {seconds:00}s" : $"{title}  /  ACTIVE";
        warningBanner.SetActive(true);
    }

    public void Clear()
    {
        nextExplorationWave = float.PositiveInfinity;
        random = null;
        Finish();
    }

    private void Finish()
    {
        current = Weather.None;
        shownSecond = -1;
        if (warningBanner != null)
        {
            warningBanner.SetActive(false);
        }

        if (tornado != null)
        {
            Retire(tornado.gameObject);
            tornado = null;
        }

        foreach (Rigidbody meteor in meteors)
        {
            if (meteor != null)
            {
                Retire(meteor.gameObject);
            }
        }

        meteors.Clear();
    }

    private static void Retire(GameObject instance)
    {
        instance.SetActive(false);
        Destroy(instance);
    }

    private void OnDisable()
    {
        Clear();
    }
}
