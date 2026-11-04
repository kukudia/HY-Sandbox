// 职责：在隔离场景回归共用包围盒、编辑动作恢复和建筑残骸运动继承。
// 入口：Main Play调用Run()，Editor update推进真实帧；报告Temp/Reuse/shared-mechanism-checks.json。
// 隔离：仅同步动作期间替换管理器实例，文件仓定向Temp；finally恢复静态状态并卸载测试场景。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SharedMechanismChecks
{
    private const string ReportPath = "Temp/Reuse/shared-mechanism-checks.json";
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static IEnumerator scenario;
    private static Scene scene;
    private static int lastFrame;
    private static double deadline;
    private static BuildManager build;
    private static SaveManager saves;
    private static GameManager game;
    private static VisualEffectsManager effects;
    public static string Run()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity")
        {
            throw new InvalidOperationException("Open Main in Play Mode before running shared mechanism checks.");
        }

        if (scenario != null || BuildManager.instance == null || BuildManager.instance.IsLoadingBlocks)
        {
            throw new InvalidOperationException("Wait for the current blueprint/checks to finish.");
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
                throw new TimeoutException("Shared mechanism checks stopped or exceeded 60 seconds.");
            }

            // Destroy must complete through a real player frame before the next action.
            if (Time.frameCount == lastFrame)
            {
                return;
            }

            lastFrame = Time.frameCount;
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
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(
                ReportPath,
                JsonUtility.ToJson(
                    new Report { passed = Errors.Count == 0, checks = Checks.ToArray(), errors = Errors.ToArray() },
                    true));
        }
    }

    private static IEnumerator RunRoutine()
    {
        scene = SceneManager.CreateScene(
            "Shared mechanisms " + Guid.NewGuid().ToString("N"),
            new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            GameObject managers = NewObject("Inactive probe managers");
            managers.SetActive(false);
            build = managers.AddComponent<BuildManager>();
            saves = managers.AddComponent<SaveManager>();
            game = managers.AddComponent<GameManager>();
            effects = managers.AddComponent<VisualEffectsManager>();
            effects.enableRuntimeVfx = false;
            game.blocksParent = NewObject("Probe construct").transform;
            build.currentSaveName = "action-fixture";
            build.blockLayer = ~0;
            Type storeType = RuntimeType("BlueprintFileStore");
            string directory = Path.GetFullPath("Temp/Reuse/Actions-" + Guid.NewGuid().ToString("N"));
            Set(saves, "_playerFiles", Activator.CreateInstance(storeType, new object[] { directory }));
            BoundsChecks();
            MotionChecks();
            IEnumerator actions = ActionChecks();
            while (actions.MoveNext())
            {
                yield return actions.Current;
            }
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    private static void BoundsChecks()
    {
        GameObject sample = NewObject("Bounds sample");
        Block block = sample.AddComponent<Block>();
        block.enabled = false;
        BlockData data = new BlockData(block)
        {
            x = 2,
            y = 4,
            z = 6,
            posX = 10f,
            posY = 3f,
            posZ = 2f,
            rotX = 0f,
            rotY = 0f,
            rotZ = Mathf.Sqrt(0.5f),
            rotW = Mathf.Sqrt(0.5f)
        };
        var blocks = new List<BlockData>
        {
            data
        };
        Bounds ordinary = new Bounds(new Vector3(10f, 3f, 2f), new Vector3(4f, 2f, 6f));
        Check(
            "Shared blueprint geometry respects a rotated non-cubic block",
            Close(BlockBounds.FromBlueprint(data, Matrix4x4.identity), ordinary));
        Check(
            "Base designer preserves outer blueprint rotation",
            Close(
                OutpostBaseDesigner.BoundsOf(blocks, Quaternion.Euler(0f, 90f, 0f)),
                new Bounds(new Vector3(2f, 3f, -10f), new Vector3(6f, 2f, 4f))));
        Matrix4x4 placement = Matrix4x4.TRS(new Vector3(4f, 5f, 6f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2f, 1f, 3f));
        Bounds deployed = (Bounds)Call(RuntimeType("OutpostDeploymentPreview"), null, "CalculateBounds", blocks, placement);
        Check(
            "Outpost preview respects rotated non-uniform deployment scale",
            Close(deployed, new Bounds(new Vector3(10f, 8f, -14f), new Vector3(18f, 2f, 8f))));
        Check(
            "Empty blueprint bounds stay empty without implicit origin translation",
            Close(BlockBounds.FromBlueprint(new List<BlockData>(), placement), default));
        Bounds spawn = (Bounds)Call(
            typeof(OutpostEncounterDirector),
            null,
            "DeploymentBounds",
            new BlockDataList { blocks = blocks },
            Vector3.zero,
            Quaternion.identity);
        Check(
            "Encounter streaming bounds intentionally include the birth origin",
            Close(spawn, new Bounds(new Vector3(6f, 2f, 2f), new Vector3(12f, 4f, 6f))) && !ordinary.Contains(Vector3.zero));
        Transform construct = NewObject("Scaled construct").transform;
        construct.SetPositionAndRotation(new Vector3(20f, 6f, -4f), Quaternion.Euler(0f, 90f, 0f));
        construct.localScale = new Vector3(2f, 1f, 3f);
        block.transform.SetParent(construct, false);
        block.transform.SetLocalPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 0f, 90f));
        block.x = 2;
        block.y = 4;
        block.z = 6;
        Bounds world = (Bounds)Call(typeof(WorldSessionManager), null, "ConstructBounds", construct);
        Bounds local = (Bounds)Call(RuntimeType("WorldPlayerStreamingGuard"), null, "CaptureLocalBounds", construct);
        Check(
            "World session reads actual scaled construct geometry",
            Close(world, new Bounds(new Vector3(29f, 8f, -6f), new Vector3(18f, 2f, 8f))));
        Check(
            "Streaming guard preserves construct-local geometry under parent scale",
            Close(local, new Bounds(new Vector3(1f, 2f, 3f), new Vector3(4f, 2f, 6f))));
        Transform empty = NewObject("Empty construct").transform;
        empty.position = new Vector3(11f, 12f, 13f);
        Check(
            "Empty world construct uses its world origin",
            Close(
                (Bounds)Call(typeof(WorldSessionManager), null, "ConstructBounds", empty),
                new Bounds(empty.position, Vector3.zero)));
        Check(
            "Empty local construct uses local zero",
            Close((Bounds)Call(RuntimeType("WorldPlayerStreamingGuard"), null, "CaptureLocalBounds", empty), default));
        object camera = Activator.CreateInstance(RuntimeType("BlueprintLoadCamera"), true);
        object settings = Activator.CreateInstance(RuntimeType("BlueprintLoadSettings"), true);
        Set(settings, "GridSize", 2f);
        Set(camera, "_settings", settings);
        data.x = 0;
        data.y = 2;
        data.z = 3;
        data.rotZ = 0f;
        data.rotW = 0f;
        Bounds loading = (Bounds)Call(camera.GetType(), camera, "CalculateBlockDataBounds", data);
        Check(
            "Loading camera preserves minimum grid dimensions and invalid rotation fallback",
            Close(loading, new Bounds(new Vector3(10f, 3f, 2f), new Vector3(2f, 4f, 6f))));
        object[] arguments =
        {
            new List<BlockData>
            {
                null
            },
            new Vector3(9f, 8f, 7f),
            default(Bounds)
        };
        bool found = (bool)Call(camera.GetType(), camera, "TryCalculateLoadingCameraOrbitBounds", arguments);
        Check(
            "Loading camera keeps null-only input fallback and reports no bounds",
            !found && Close((Bounds)arguments[2], new Bounds(new Vector3(9f, 8f, 7f), Vector3.zero)));
    }

    private static void MotionChecks()
    {
        Rigidbody source = NewObject("Moving source").AddComponent<Rigidbody>();
        source.useGravity = false;
        source.linearVelocity = new Vector3(3f, 4f, 5f);
        source.angularVelocity = new Vector3(0f, 2f, 0f);
        source.centerOfMass = Vector3.zero;
        source.linearDamping = 0.15f;
        source.angularDamping = 0.35f;
        source.maxDepenetrationVelocity = 6f;
        source.maxLinearVelocity = 100f;
        source.maxAngularVelocity = 20f;
        object motion = Activator.CreateInstance(RuntimeType("RigidbodyMotion"), Members, null, new object[] { source }, null);
        WorldBuildingManager manager = NewObject("Debris manager").AddComponent<WorldBuildingManager>();
        manager.enabled = false;
        Block first = NewObject("First fragment").AddComponent<Block>();
        Block second = NewObject("Second fragment").AddComponent<Block>();
        first.transform.position = new Vector3(2f, 0f, 0f);
        second.transform.position = new Vector3(-2f, 0f, 0f);
        first.transform.SetParent(source.transform, true);
        second.transform.SetParent(source.transform, true);
        PlayManager original = PlayManager.instance;
        try
        {
            PlayManager.instance = null;
            Call(
                typeof(WorldBuildingManager),
                manager,
                "CreateDebris",
                new List<Block> { first },
                source,
                motion);
            source.linearVelocity = Vector3.one * 90f;
            source.angularVelocity = Vector3.one * 9f;
            source.centerOfMass = new Vector3(10f, 0f, 0f);
            Call(
                typeof(WorldBuildingManager),
                manager,
                "CreateDebris",
                new List<Block> { second },
                source,
                motion);
        }
        finally
        {
            PlayManager.instance = original;
        }

        Rigidbody a = first.GetComponentInParent<Rigidbody>();
        Rigidbody b = second.GetComponentInParent<Rigidbody>();
        Check(
            "Real building debris inherits linear plus angular point velocity",
            Near(a.linearVelocity, new Vector3(3f, 4f, 1f)) && Near(a.angularVelocity, new Vector3(0f, 2f, 0f)));
        Check(
            "Second fragment uses the original captured motion after source state changes",
            Near(b.linearVelocity, new Vector3(3f, 4f, 9f)) && Near(b.angularVelocity, new Vector3(0f, 2f, 0f)));
        Check(
            "Debris inherits common damping and depenetration settings",
            Mathf.Approximately(a.linearDamping, 0.15f) && Mathf.Approximately(b.angularDamping, 0.35f) && Mathf.Approximately(a.maxDepenetrationVelocity, 6f));
        GroupPhysicsSettings capped = new GroupPhysicsSettings(0.4f, 0.6f, 2f, 1f, 1f);
        Call(motion.GetType(), motion, "Restore", a, capped);
        Check(
            "Shared motion inheritance applies tangential and angular caps",
            Near(a.linearVelocity, new Vector3(3f, 4f, 4f)) && Near(a.angularVelocity, Vector3.up));
        Call(typeof(GroupPhysicsSettings), capped, "ApplyTo", a);
        Check(
            "Common body settings apply identically independent of unit type",
            Mathf.Approximately(a.linearDamping, 0.4f) && Mathf.Approximately(a.angularDamping, 0.6f) && Mathf.Approximately(a.maxDepenetrationVelocity, 2f));
    }

    private static IEnumerator ActionChecks()
    {
        GameObject prefab = Resources.Load<GameObject>("Blocks/CoinHold");
        if (prefab == null)
        {
            throw new InvalidOperationException("CoinHold fixture prefab is missing.");
        }

        Block block = Object.Instantiate(prefab, game.blocksParent).GetComponent<Block>();
        foreach (MonoBehaviour behaviour in block.GetComponentsInChildren<MonoBehaviour>())
        {
            behaviour.enabled = false;
        }

        block.resourcePath = "Assets/Resources/Blocks/CoinHold.prefab";
        block.x = 2;
        block.y = 3;
        block.z = 4;
        block.RefreshRuntimeData(true);
        Vector3 position = new Vector3(17.123f, 7.234f, -11.345f);
        Quaternion rotation = Quaternion.Euler(12f, 37f, 23f);
        block.transform.SetPositionAndRotation(position, rotation);
        string id = block.uniqueId;
        float mass = block.mass;
        CargoHold cargo = block.GetComponent<CargoHold>();
        cargo.RestoreContents(new List<CargoItem> { new CargoItem { kind = CargoKind.Coins, amount = 1 } });
        CreateBlockAction created = new CreateBlockAction(block);
        // 创建后装入的货物也必须跟随撤销/重做，不只恢复动作构造时的仓库内容。
        cargo.RestoreContents(new List<CargoItem> { new CargoItem { kind = CargoKind.Coins, amount = 3 } });
        WithManagers(() => build.SaveBlock(block));
        for (int cycle = 0; cycle < 3; cycle++)
        {
            WithManagers(created.Undo);
            yield return null;
            Check(
                "Create undo destroys the instance and persists removal " + cycle,
                game.blocksParent.childCount == 0 && saves.cachedData.blocks.Count == 0);
            WithManagers(created.Redo);
            block = build.lastSaveBlock;
            foreach (MonoBehaviour behaviour in block.GetComponentsInChildren<MonoBehaviour>())
            {
                behaviour.enabled = false;
            }

            CheckRestored(
                "Create redo " + cycle,
                block,
                id,
                mass,
                position,
                rotation,
                3);
            yield return null;
        }

        DeleteBlockAction deleted = new DeleteBlockAction(block);
        for (int cycle = 0; cycle < 3; cycle++)
        {
            WithManagers(deleted.Redo);
            yield return null;
            Check(
                "Delete redo destroys the instance and persists removal " + cycle,
                game.blocksParent.childCount == 0 && saves.cachedData.blocks.Count == 0);
            WithManagers(deleted.Undo);
            block = build.lastSaveBlock;
            foreach (MonoBehaviour behaviour in block.GetComponentsInChildren<MonoBehaviour>())
            {
                behaviour.enabled = false;
            }

            CheckRestored(
                "Delete undo " + cycle,
                block,
                id,
                mass,
                position,
                rotation,
                3);
            yield return null;
        }
    }

    private static void CheckRestored(
        string label,
        Block block,
        string id,
        float mass,
        Vector3 position,
        Quaternion rotation,
        int coins)
    {
        Check(
            label + " preserves identity without duplicate saved entries",
            block.uniqueId == id && saves.cachedData.blocks.Count == 1 && saves.cachedData.blocks[0].id == id);
        Check(
            label + " restores dimensions mass collider and cargo",
            block.x == 2 && block.y == 3 && block.z == 4 && Mathf.Approximately(block.mass, mass) && Near(block.raycastCollider.size, new Vector3(1.9f, 2.9f, 3.9f)) && block.GetComponent<CargoHold>().Used == coins);
        Check(
            label + " restores precise unsnapped world pose",
            Near(block.transform.position, position) && Quaternion.Angle(block.transform.rotation, rotation) < 0.01f);
    }

    private static void WithManagers(Action operation)
    {
        BuildManager oldBuild = BuildManager.instance;
        SaveManager oldSaves = SaveManager.instance;
        GameManager oldGame = GameManager.instance;
        VisualEffectsManager oldEffects = VisualEffectsManager.instance;
        BlueprintUIPanel oldPanel = BlueprintUIPanel.instance;
        try
        {
            BuildManager.instance = build;
            SaveManager.instance = saves;
            GameManager.instance = game;
            VisualEffectsManager.instance = effects;
            BlueprintUIPanel.instance = null;
            operation();
        }
        finally
        {
            BuildManager.instance = oldBuild;
            SaveManager.instance = oldSaves;
            GameManager.instance = oldGame;
            VisualEffectsManager.instance = oldEffects;
            BlueprintUIPanel.instance = oldPanel;
        }
    }

    private static GameObject NewObject(string name)
    {
        GameObject result = new GameObject(name);
        SceneManager.MoveGameObjectToScene(result, scene);
        return result;
    }

    private static Type RuntimeType(string name)
    {
        return typeof(Block).Assembly.GetType(name, true);
    }

    private static object Call(Type type, object target, string name, params object[] arguments)
    {
        return type.GetMethod(name, Members).Invoke(target, arguments);
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, Members).SetValue(target, value);
    }

    private static bool Near(Vector3 actual, Vector3 expected)
    {
        return (actual - expected).sqrMagnitude < 0.000001f;
    }

    private static bool Close(Bounds actual, Bounds expected)
    {
        return Near(actual.center, expected.center) && Near(actual.size, expected.size);
    }

    private static void Check(string name, bool passed)
    {
        (passed ? Checks : Errors).Add(name);
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string[] checks;
        public string[] errors;
    }
}
