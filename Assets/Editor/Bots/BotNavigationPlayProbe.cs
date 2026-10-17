// 职责：隔离 Play 场景验证 Bot 过滤、墙面、U 形围挡、惯性制动和移动 Home 导航。
// 入口：Run()；结果保存在 Temp/BotNavigation/PlayValidation.json；不修改作者场景或资产。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BotNavigationPlayProbe
{
    private const string Pending = "HY.BotNavigationProbe.Pending";
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static BotNavigationProbeDriver _driver;
    private static GameObject _caseRoot;
    private static int _case;
    private static double _deadline;
    private static bool _background;
    private static float _timeScale;
    public static string LastResult => File.Exists("Temp/BotNavigation/PlayValidation.json") ? File.ReadAllText("Temp/BotNavigation/PlayValidation.json") : "pending";

    static BotNavigationPlayProbe()
    {
        EditorApplication.playModeStateChanged += StateChanged;
    }

    [MenuItem("Tools/Bots/Run navigation Play validation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before running navigation validation.");
        }

        if (File.Exists("Temp/BotNavigation/PlayValidation.json"))
        {
            File.Delete("Temp/BotNavigation/PlayValidation.json");
        }

        SessionState.SetBool(Pending + ".Background", Application.runInBackground);
        SessionState.SetFloat(Pending + ".TimeScale", Time.timeScale);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            try
            {
                Checks.Clear();
                Errors.Clear();
                _case = 0;
                _background = SessionState.GetBool(Pending + ".Background", false);
                _timeScale = SessionState.GetFloat(Pending + ".TimeScale", 1f);
                Application.runInBackground = true;
                Time.timeScale = 1f;
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    root.SetActive(false);
                }

                Application.logMessageReceived += Capture;
                _deadline = EditorApplication.timeSinceStartup + 140;
                ValidateBotFiltering();
                StartCase();
                EditorApplication.update += Tick;
            }
            catch (Exception exception)
            {
                Errors.Add(exception.ToString());
                Finish();
            }
        }

        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Errors.Add("Navigation probe interrupted by manual Play Mode exit.");
            WriteReport();
            Restore();
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
        }
    }

    private static void Capture(string condition, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            Errors.Add(condition + "\n" + stack);
        }
    }

    private static void Tick()
    {
        try
        {
            TickCase();
        }
        catch (Exception exception)
        {
            Errors.Add(exception.ToString());
            Finish();
        }
    }

    private static void TickCase()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup > _deadline)
        {
            Errors.Add("Navigation probe exceeded 140 seconds.");
            Finish();
            return;
        }

        if (_driver == null || !_driver.Finished)
        {
            return;
        }

        string name = _driver.CaseName;
        if (!_driver.Reached)
        {
            Errors.Add(name + " failed to reach target; distance=" + _driver.RemainingDistance);
        }
        else
        {
            Checks.Add(name + " reached target through real Rigidbody physics");
        }

        if (_driver.ObstacleContacts > 0)
        {
            Errors.Add(name + " collided with obstacle " + _driver.ObstacleContacts + " times");
        }
        else
        {
            Checks.Add(name + " did not collide with walls");
        }

        if (_case == 1 && _driver.MaximumHeight > 6.4f)
        {
            Checks.Add("CollectionBot U enclosure escaped vertically");
        }

        Object.DestroyImmediate(_caseRoot);
        _case++;
        if (_case >= 5)
        {
            Finish();
        }
        else
        {
            StartCase();
        }
    }

    private static void ValidateBotFiltering()
    {
        GameObject root = new GameObject("BotFilteringProbe");
        root.transform.position = new Vector3(10000f, 100f, 0f);
        try
        {
            GameObject drone = new GameObject("FilteringDrone");
            drone.transform.SetParent(root.transform, false);
            drone.AddComponent<SphereCollider>().radius = 0.2f;
            GameObject target = new GameObject("FilteringTarget");
            target.transform.SetParent(root.transform, false);
            target.transform.localPosition = Vector3.forward * 10f;
            BotAvoidanceSettings settings = new BotAvoidanceSettings
            {
                ObstacleMask = ~0,
                ClearanceRadius = 0.4f,
                PredictiveRange = 15f,
                BrakingAcceleration = 12f,
                RayCount = 12,
                StuckTimeout = 1.5f,
                ProgressDistance = 0.25f,
                DetourHoldTime = 1f
            };
            BotObstacleAvoidance navigation = new BotObstacleAvoidance(drone.transform);
            Vector3 velocity = Vector3.forward * 6f;
            foreach (Type botType in new[]
            {
                typeof(RepairBot),
                typeof(CollectionBot)
            }

            )
            {
                GameObject other = new GameObject(botType.Name + " filtering neighbor");
                other.transform.SetParent(root.transform, false);
                other.transform.localPosition = Vector3.forward * 2f;
                Bot bot = (Bot)other.AddComponent(botType);
                bot.enabled = false;
                GameObject shape = new GameObject("Bot child collider");
                shape.transform.SetParent(other.transform, false);
                BoxCollider collider = shape.AddComponent<BoxCollider>();
                foreach (int layer in new[]
                {
                    0,
                    LayerMask.NameToLayer("Block")
                }

                )
                {
                    shape.layer = layer;
                    navigation.Reset();
                    Physics.SyncTransforms();
                    BotAvoidanceResult result = navigation.Evaluate(
                        target.transform,
                        velocity,
                        Vector3.forward,
                        1f,
                        settings,
                        10f);
                    Check(
                        botType.Name + " child layer " + layer + " does not trigger detour",
                        !result.OverrideDirection && result.ObstacleCount == 0);
                    Check(
                        botType.Name + " child layer " + layer + " does not brake",
                        (navigation.ConstrainVelocity(velocity, 0.02f, velocity, 12f) - velocity).sqrMagnitude < 0.0001f);
                }

                other.transform.localPosition = Vector3.forward * 0.3f;
                navigation.Reset();
                Physics.SyncTransforms();
                BotAvoidanceResult overlap = navigation.Evaluate(
                    target.transform,
                    velocity,
                    Vector3.forward,
                    1f,
                    settings,
                    10f);
                Check(botType.Name + " overlap does not trigger escape", !overlap.OverrideDirection);
                Check(
                    botType.Name + " overlap does not redirect velocity",
                    (navigation.ConstrainVelocity(velocity, 0.02f, velocity, 12f) - velocity).sqrMagnitude < 0.0001f);
                GameObject wall = new GameObject("Filtering real wall");
                wall.transform.SetParent(root.transform, false);
                wall.transform.localPosition = Vector3.forward * 2f;
                wall.AddComponent<BoxCollider>().size = new Vector3(8f, 8f, 0.5f);
                navigation.Reset();
                Physics.SyncTransforms();
                BotAvoidanceResult blocked = navigation.Evaluate(
                    target.transform,
                    velocity,
                    Vector3.forward,
                    1f,
                    settings,
                    10f);
                Check(
                    botType.Name + " overlap does not hide wall",
                    blocked.OverrideDirection && blocked.ObstacleCount > 0);
                Check(
                    botType.Name + " overlap keeps wall braking",
                    navigation.ConstrainVelocity(velocity, 0.02f, velocity, 12f).z < velocity.z - 0.1f);
                Object.DestroyImmediate(wall);
                shape.transform.SetParent(root.transform, false);
                shape.transform.localPosition = Vector3.forward * 2f;
                other.transform.localPosition = Vector3.right * 20f;
                navigation.Reset();
                Physics.SyncTransforms();
                BotAvoidanceResult released = navigation.Evaluate(
                    target.transform,
                    velocity,
                    Vector3.forward,
                    1f,
                    settings,
                    10f);
                Check(
                    botType.Name + " detached collider becomes obstacle again",
                    released.OverrideDirection && released.ObstacleCount > 0);
                Object.DestroyImmediate(collider.gameObject);
                Object.DestroyImmediate(other);
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void Check(string name, bool passed)
    {
        if (passed)
        {
            Checks.Add(name);
        }
        else
        {
            Errors.Add(name);
        }
    }

    private static void StartCase()
    {
        _caseRoot = new GameObject("BotNavigationProbeCase");
        string asset = _case == 1 ? "Blocks/CollectionBotContainer" : "Blocks/RepairBotContianer";
        Bot authored = Resources.Load<GameObject>(asset).GetComponentInChildren<Bot>(true);
        GameObject target = new GameObject("NavigationTarget");
        target.transform.SetParent(_caseRoot.transform);
        target.transform.position = new Vector3(0f, 3f, _case == 1 ? 12f : 8f);
        GameObject drone = new GameObject("NavigationProbeDrone");
        drone.transform.SetParent(_caseRoot.transform);
        drone.transform.position = new Vector3(0f, 3f, _case == 1 ? 1f : _case == 4 ? -0.55f : -10f);
        drone.AddComponent<SphereCollider>().radius = 0.2f;
        Rigidbody body = drone.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.linearDamping = 2f;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        if (_case == 2)
        {
            body.linearVelocity = Vector3.forward * authored.movementSpeed;
        }

        Rigidbody home = null;
        if (_case == 3)
        {
            home = target.AddComponent<Rigidbody>();
            home.useGravity = false;
            home.isKinematic = true;
        }

        Wall(new Vector3(0f, 3f, _case == 1 ? 5f : 0f), new Vector3(_case == 1 ? 8f : 16f, 6f, 1f));
        if (_case == 1)
        {
            Wall(new Vector3(-4f, 3f, 1f), new Vector3(1f, 6f, 9f));
            Wall(new Vector3(4f, 3f, 1f), new Vector3(1f, 6f, 9f));
        }

        _driver = drone.AddComponent<BotNavigationProbeDriver>();
        _driver.Initialize(
            _case == 1 ? "CollectionBot U enclosure" : _case == 2 ? "RepairBot inertia braking" : _case == 3 ? "RepairBot moving Home behind wall" : _case == 4 ? "RepairBot initial overlap escape" : "RepairBot wall crossing",
            target.transform,
            body,
            home,
            authored);
        Physics.SyncTransforms();
    }

    private static void Wall(Vector3 position, Vector3 scale)
    {
        GameObject wall = new GameObject("NavigationProbeObstacle");
        wall.transform.SetParent(_caseRoot.transform);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.AddComponent<BoxCollider>();
    }

    private static void Finish()
    {
        WriteReport();
        Restore();
        EditorApplication.isPlaying = false;
    }

    private static void Restore()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Capture;
        Application.runInBackground = SessionState.GetBool(Pending + ".Background", _background);
        Time.timeScale = SessionState.GetFloat(Pending + ".TimeScale", _timeScale);
        SessionState.SetBool(Pending, false);
    }

    private static void WriteReport()
    {
        Directory.CreateDirectory("Temp/BotNavigation");
        File.WriteAllText(
            "Temp/BotNavigation/PlayValidation.json",
            JsonUtility.ToJson(
                new Report { passed = Errors.Count == 0, checks = Checks.ToArray(), errors = Errors.ToArray() },
                true));
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string[] checks;
        public string[] errors;
    }
}
