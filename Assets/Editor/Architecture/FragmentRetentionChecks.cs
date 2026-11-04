// 职责：以真实 Play 帧回归建筑断片、旧清理计时器和驾驶舱恢复的最短保留期。
// 入口：Main 编辑态玩法下调用 Run()；报告 Temp/Reuse/fragment-retention.json。
// 隔离：独立 PhysicsScene，设置仅内存暂存；finally 恢复并卸载，不替代生产延迟清理。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class FragmentRetentionChecks
{
    private const string ReportPath = "Temp/Reuse/fragment-retention.json";
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static IEnumerator scenario;
    private static Scene scene;
    private static int lastFrame;
    private static double deadline;
    public static string Run()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity" || PlayManager.instance == null || PlayManager.instance.playMode || Time.timeScale <= 0f || scenario != null)
        {
            throw new InvalidOperationException("Run once in Main Play with construct simulation idle and time advancing.");
        }

        Checks.Clear();
        Errors.Clear();
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        if (File.Exists(ReportPath))
        {
            File.Delete(ReportPath);
        }

        scenario = RunRoutine();
        lastFrame = -1;
        deadline = EditorApplication.timeSinceStartup + 60d;
        EditorApplication.update += Tick;
        return "Started: " + ReportPath;
    }

    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (!Application.isPlaying || EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Fragment checks stopped or exceeded 60 seconds.");
            }

            if (lastFrame == Time.frameCount)
            {
                return;
            }

            lastFrame = Time.frameCount;
            if (scene.IsValid() && scene.isLoaded)
            {
                float remaining = Time.deltaTime;
                while (remaining > 0f)
                {
                    float step = Mathf.Min(remaining, 0.02f);
                    scene.GetPhysicsScene().Simulate(step);
                    remaining -= step;
                }
            }

            if (!scenario.MoveNext())
            {
                Finish();
            }
        }
        catch (Exception error)
        {
            Errors.Add(error.ToString());
            Finish();
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        try
        {
            (scenario as IDisposable)?.Dispose();
        }
        catch (Exception error)
        {
            Errors.Add("Cleanup: " + error);
        }
        finally
        {
            scenario = null;
            File.WriteAllText(
                ReportPath,
                JsonUtility.ToJson(
                    new Report { passed = Errors.Count == 0, checks = Checks.ToArray(), errors = Errors.ToArray() },
                    true));
        }
    }

    private static IEnumerator RunRoutine()
    {
        var settings = Resources.Load<SalvageSettings>("Salvage/Settings");
        Require(settings != null, "Production salvage settings available");
        float savedMinimum = settings.minimumFragmentLifetime;
        float savedDelay = settings.cleanupDelay;
        bool wasDirty = EditorUtility.IsDirty(settings);
        scene = SceneManager.CreateScene(
            "Fragment retention " + Guid.NewGuid().ToString("N"),
            new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        WorldBuildingManager host = null;
        try
        {
            settings.minimumFragmentLifetime = 0.7f;
            settings.cleanupDelay = 0.35f;
            host = NewObject("Fragment check host").AddComponent<WorldBuildingManager>();
            host.enabled = false;
            Set(host, "initializeOnStart", false);
            Set(host, "debrisLifetime", 0.1f);
            Set(host, "maximumDebrisPieces", 12);
            Set(host, "maximumBlocksPerDebris", 32);
            GameObject large = CreateDebris(host, 33, 50f);
            Set(host, "maximumDebrisPieces", 0);
            GameObject zeroBudget = CreateDebris(host, 1, 80f);
            Set(host, "maximumDebrisPieces", 12);
            GameObject ordinary = CreateDebris(host, 1, 110f);
            float born = Time.time;
            float initialY = large.transform.position.y;
            Require(
                large.GetComponentsInChildren<Block>(true).Length == 33,
                "Over-32 block fragment retains all 33 blocks");
            Renderer[] renderers = large.GetComponentsInChildren<Renderer>();
            Require(
                renderers.Length == 33 && Array.TrueForAll(renderers, renderer => renderer.enabled && renderer.gameObject.activeInHierarchy),
                "Large fragment retains 33 enabled visible meshes");
            Require(zeroBudget != null, "Zero debris budget still creates a physical fragment");
            Rigidbody body = large.GetComponent<Rigidbody>();
            Require(body != null && !body.isKinematic && body.useGravity, "Large fragment is dynamic with gravity");
            while (Time.time - born < 0.4f)
            {
                yield return null;
            }

            Require(
                large != null && zeroBudget != null && ordinary != null,
                "All budget paths survive at least 0.4 seconds despite 0.1 second debris lifetime");
            Require(
                large.transform.position.y < initialY - 0.1f,
                "Large fragment actually falls under isolated PhysX simulation");
            while (Time.time - born < 0.95f)
            {
                yield return null;
            }

            yield return null;
            Require(
                large == null && zeroBudget == null && ordinary == null,
                "All debris paths retire after the 0.7 second minimum");
            ControlUnit group = NewUnit("Scheduled group");
            ControlUnit recoveredGroup = NewUnit("Recovered group");
            var cleanup = Activator.CreateInstance(RuntimeType("SalvageCleanupSession"), Members, null, new object[] { host }, null);
            Call(cleanup, "ScheduleGroup", group, 0.35f);
            Call(cleanup, "ScheduleGroup", recoveredGroup, 0.35f);
            string ownerId = Guid.NewGuid().ToString("N");
            string recoveredId = Guid.NewGuid().ToString("N");
            Call(cleanup, "ScheduleUnit", group.gameObject, ownerId, 0.35f);
            Call(cleanup, "ScheduleUnit", recoveredGroup.gameObject, recoveredId, 0.35f);
            float scheduled = Time.time;
            while (Time.time - scheduled < 0.15f)
            {
                yield return null;
            }

            Mark(group.gameObject);
            Mark(recoveredGroup.gameObject);
            ControlUnit unit = NewUnit("Later unit fragment");
            ControlUnit recoveredUnit = NewUnit("Later recovered fragment");
            GameObject member = NewBlock("Later original-owner block", unit.transform).gameObject;
            GameObject recoveredMember = NewBlock("Later recovered-owner block", recoveredUnit.transform).gameObject;
            RuntimeUnitMember.Ensure(member, ownerId, UnitFaction.Player);
            RuntimeUnitMember.Ensure(recoveredMember, recoveredId, UnitFaction.Player);
            Mark(unit.gameObject);
            Mark(recoveredUnit.gameObject);
            born = Time.time;
            // Start assigns current unit ownership to unassigned blocks only; these explicit original IDs must survive.
            while (Time.time - born < 0.4f)
            {
                yield return null;
            }

            Require(
                group != null && recoveredGroup != null,
                "Older ScheduleGroup timers preserve newly marked fragments at age 0.4");
            Require(
                member != null && recoveredMember != null,
                "Older ScheduleUnit timers preserve newly born member fragments at age 0.4");
            Require(
                member.GetComponent<RuntimeUnitMember>().ownerUnitId == ownerId,
                "Original owner ID survives normal ControlUnit startup");
            NewObject("Restored group cockpit", recoveredGroup.transform).AddComponent<Cockpit>();
            NewObject("Restored unit cockpit", recoveredUnit.transform).AddComponent<Cockpit>();
            while (Time.time - born < 0.95f)
            {
                yield return null;
            }

            yield return null;
            Require(group == null, "ScheduleGroup retires unrecovered fragment after minimum lifetime");
            Require(member == null, "ScheduleUnit retires original-owner member after minimum lifetime");
            Require(
                recoveredGroup != null && recoveredGroup.HasAnyCockpit,
                "Restored cockpit prevents pending group destruction");
            Require(
                recoveredMember != null && recoveredUnit.HasAnyCockpit,
                "Restored cockpit prevents pending original-unit member destruction");
            Call(cleanup, "Clear");
        }
        finally
        {
            if (host != null)
            {
                host.StopAllCoroutines();
            }

            settings.minimumFragmentLifetime = savedMinimum;
            settings.cleanupDelay = savedDelay;
            Require(EditorUtility.IsDirty(settings) == wasDirty, "Settings dirty state unchanged; no asset saved");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    private static GameObject CreateDebris(WorldBuildingManager host, int count, float height)
    {
        GameObject source = NewObject("Source");
        source.transform.position = new Vector3(10000f, height, 10000f);
        Rigidbody sourceBody = source.AddComponent<Rigidbody>();
        sourceBody.useGravity = true;
        sourceBody.isKinematic = true;
        var blocks = new List<Block>();
        for (int i = 0; i < count; i++)
        {
            Block block = NewBlock("Block " + i, source.transform);
            block.transform.localPosition = new Vector3(i % 6, 0f, i / 6);
            blocks.Add(block);
        }

        object motion = Activator.CreateInstance(RuntimeType("RigidbodyMotion"), Members, null, new object[] { sourceBody }, null);
        PlayManager previous = PlayManager.instance;
        try
        {
            // Synchronous call only: avoid salvage/game session effects; source settings exercise production fallback.
            PlayManager.instance = null;
            Call(host, "CreateDebris", blocks, sourceBody, motion);
        }
        finally
        {
            PlayManager.instance = previous;
        }

        return blocks[0].transform.parent.gameObject;
    }

    private static ControlUnit NewUnit(string name)
    {
        ControlUnit unit = NewObject(name).AddComponent<ControlUnit>();
        unit.EnsureRuntimeUnitId();
        NewObject("Retained payload", unit.transform);
        return unit;
    }

    private static Block NewBlock(string name, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        SceneManager.MoveGameObjectToScene(cube, scene);
        cube.transform.SetParent(parent, false);
        Block block = cube.AddComponent<Block>();
        block.enabled = false;
        return block;
    }

    private static GameObject NewObject(string name, Transform parent = null)
    {
        var result = new GameObject(name);
        SceneManager.MoveGameObjectToScene(result, scene);
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Mark(GameObject root)
    {
        RuntimeType("FragmentLifetime").GetMethod("Mark", Members).Invoke(null, new object[] { root });
    }

    private static Type RuntimeType(string name) => typeof(Block).Assembly.GetType(name, true);
    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, Members).SetValue(target, value);
    }

    private static void Call(object target, string method, params object[] arguments)
    {
        target.GetType().GetMethod(method, Members).Invoke(target, arguments);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }

        Checks.Add(message);
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string[] checks;
        public string[] errors;
    }
}
