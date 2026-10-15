// 职责：在一次临时 Play Mode 会话中回归蓝图加载、文件命名和实体恢复的架构契约。
// 入口：Tools/Architecture/Run Play Mode validation；结果写入 Temp/Architecture/Validation.json。
// 隔离：暂停场景业务，反射装配真实加载器；只写随机前缀临时目录，finally 恢复管理器状态。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ArchitecturePlayProbe
{
    private const string PendingKey = "HY.ArchitecturePlayProbe";
    private const string ReportPath = "Temp/Architecture/Validation.json";
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static readonly List<string> ResourcesUsed = new List<string>();
    private static readonly List<BlueprintStatistics> Progress = new List<BlueprintStatistics>();
    private static IEnumerator _scenario;
    private static object _loader;
    private static object _settings;
    private static BuildManager _build;
    private static SaveManager _saves;
    private static GameManager _game;
    private static PlayManager _play;
    private static Snapshot _snapshot;
    private static string _prefix;
    private static string _temporaryDirectory;
    private static string _expectedError;
    private static double _resume;
    private static double _deadline;
    private static bool _running;
    private static bool _completed;
    private static bool _allowPresentationUpdates;
    private static bool _background;
    private static float _timeScale;
    static ArchitecturePlayProbe()
    {
        EditorApplication.playModeStateChanged += StateChanged;
    }

    [MenuItem("Tools/Architecture/Run Play Mode validation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Exit Play Mode before running the architecture probe.");
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity")
        {
            throw new InvalidOperationException("Open Assets/Scenes/Main.unity before testing.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        if (File.Exists(ReportPath))
        {
            File.Delete(ReportPath);
        }

        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(PendingKey, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Checks.Clear();
            Errors.Clear();
            ResourcesUsed.Clear();
            Progress.Clear();
            _snapshot = null;
            _loader = null;
            _temporaryDirectory = null;
            _completed = false;
            _allowPresentationUpdates = false;
            _background = Application.runInBackground;
            _timeScale = Time.timeScale;
            Application.runInBackground = true;
            Time.timeScale = 1f;
            _running = true;
            Application.logMessageReceived += Capture;
            _scenario = Scenario();
            _resume = 0d;
            _deadline = EditorApplication.timeSinceStartup + 90d;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && _running)
        {
            Errors.Add("Play Mode exited before the probe finished.");
            Finish(false);
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(PendingKey, false);
        }
    }

    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > _deadline)
            {
                throw new TimeoutException("Architecture probe exceeded its 90 second deadline.");
            }

            if (EditorApplication.timeSinceStartup < _resume)
            {
                return;
            }

            if (!_scenario.MoveNext())
            {
                _completed = true;
                Finish(true);
                return;
            }

            if (!_allowPresentationUpdates)
            {
                PauseSceneUpdates(false);
            }

            _resume = EditorApplication.timeSinceStartup + (_scenario.Current is float delay ? delay : 0.05f);
        }
        catch (Exception exception)
        {
            Errors.Add(exception.ToString());
            Finish(true);
        }
    }

    private static void Capture(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
        {
            return;
        }

        if (_expectedError != null && message.Contains(_expectedError))
        {
            Checks.Add("Expected validation error: " + _expectedError);
            return;
        }

        Errors.Add(message + "\n" + stack);
    }

    private static void Finish(bool exitPlayMode)
    {
        if (!_running)
        {
            return;
        }

        _running = false;
        EditorApplication.update -= Tick;
        try
        {
            (_scenario as IDisposable)?.Dispose();
        }
        catch (Exception exception)
        {
            Errors.Add("Cleanup failed: " + exception);
        }
        finally
        {
            Application.logMessageReceived -= Capture;
            Application.runInBackground = _background;
            Time.timeScale = _timeScale;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(
                ReportPath,
                JsonUtility.ToJson(
                    new Report { passed = _completed && Errors.Count == 0, completed = _completed, unity = Application.unityVersion, checks = Checks.ToArray(), errors = Errors.ToArray(), resources = ResourcesUsed.ToArray(), scope = "Real Play Mode blueprint loading, file rules, hover output and isolated presentation lifecycles; authored scene updates paused; full camera navigation excluded." },
                    true));
            if (exitPlayMode)
            {
                EditorApplication.isPlaying = false;
            }
        }
    }

    private static IEnumerator Scenario()
    {
        try
        {
            PrepareIsolation();
            VerifyFileStore();
            BlockData[] ordinary = CreateOrdinaryBlueprint();
            string firstName = WriteBlueprint("ordinary", ordinary);
            Load(firstName);
            Check("Progress begins with exactly one restored block", IsLoading && _saves.blocks.Count == 1);
            Transform firstParent = _game.blocksParent;
            int firstCount = _saves.blocks.Count;
            int firstEvents = Progress.Count;
            Load(firstName);
            Check(
                "Duplicate target request preserves parent and progress",
                _game.blocksParent == firstParent && _saves.blocks.Count == firstCount && Progress.Count == firstEvents);
            while (IsLoading)
            {
                yield return 0.05f;
            }

            Check(
                "Ordinary blueprint restores every saved ID",
                _saves.blocks.Count == ordinary.Length && ordinary.All(data => _saves.blocks.Any(block => block.uniqueId == data.id)));
            Check(
                "Progress is incremental and ends at the restored block count",
                Progress.Count == ordinary.Length + 1 && Progress.Select(value => value.BlockCount).SequenceEqual(Enumerable.Range(0, ordinary.Length + 1)));
            BlueprintStatistics final = BlueprintStatistics.Calculate(_saves.cachedData.blocks, _game.blocksParent);
            BlueprintStatistics incremental = Progress[Progress.Count - 1];
            Check(
                "Full and incremental statistics agree",
                final.BlockCount == incremental.BlockCount && Mathf.Approximately(final.Mass, incremental.Mass) && Mathf.Approximately(final.RequiredPower, incremental.RequiredPower) && Mathf.Approximately(final.GeneratorOutput, incremental.GeneratorOutput));
            VerifyRestoredComponents(ordinary, UnitFaction.Player);
            string cancelName = WriteBlueprint("cancel", Enumerable.Range(0, 6).Select(index => MakeBlock("Blocks/1x1x1", index)).ToArray());
            Load(cancelName);
            Transform canceledParent = _game.blocksParent;
            int canceledCount = _saves.blocks.Count;
            Invoke(_loader, "Cancel");
            yield return 0.35f;
            Check(
                "Canceled load cannot continue restoring blocks",
                !IsLoading && _game.blocksParent == canceledParent && _saves.blocks.Count == canceledCount && canceledCount == 1);
            Load(cancelName);
            Transform replacedParent = _game.blocksParent;
            string replacementName = WriteBlueprint("replacement", new[] { MakeBlock("Blocks/1x1x1", 50) });
            Load(replacementName);
            Transform replacementParent = _game.blocksParent;
            yield return 0.35f;
            Check(
                "New request owns the replacement parent and registry",
                !IsLoading && replacementParent != replacedParent && _game.blocksParent == replacementParent && _saves.blocks.Count == 1 && _saves.blocks[0].uniqueId == _saves.cachedData.blocks[0].id);
            Check("Replaced parent is destroyed after Unity's delayed destruction", replacedParent == null);
            string emptyName = WriteBlueprint("empty", Array.Empty<BlockData>());
            int initialBlockCalls = 0;
            Load(emptyName, false, () => initialBlockCalls++);
            Check(
                "Empty blueprint finishes synchronously and invokes initial callback once",
                !IsLoading && initialBlockCalls == 1 && _saves.blocks.Count == 0 && Field(_loader, "_routine") == null);
            Invoke(_loader, "Cancel");
            Check("Cancel after synchronous completion keeps the loader idle", !IsLoading);
            BlockData[] invalid =
            {
                MakeBlock("Blocks/__ArchitectureMissingPrefab", 0),
                MakeBlock(null, 1),
                MakeBlock(string.Empty, 2),
                MakeBlock("Blocks/1x1x1", 3)
            };
            string invalidName = WriteBlueprint("invalid", invalid);
            Load(invalidName);
            while (IsLoading)
            {
                yield return 0.05f;
            }

            BlockDataList repaired = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(_saves.GetSavePath(invalidName)));
            Check(
                "Missing Prefab and empty resource paths are removed from cache and temporary JSON",
                repaired.blocks.Count == 1 && _saves.cachedData.blocks.Count == 1 && _saves.blocks.Count == 1 && repaired.blocks[0].id == invalid[3].id);
            Transform beforeCorrupt = _game.blocksParent;
            BlockDataList beforeCorruptData = _saves.cachedData;
            string corruptName = _prefix + "corrupt";
            string corruptPath = _saves.GetSavePath(corruptName);
            File.WriteAllText(corruptPath, "{ malformed blueprint JSON");
            Load(corruptName);
            Check(
                "Corrupt JSON preserves the current parent and cache",
                !IsLoading && _game.blocksParent == beforeCorrupt && ReferenceEquals(_saves.cachedData, beforeCorruptData));
            string nullListName = _prefix + "null_list";
            File.WriteAllText(_saves.GetSavePath(nullListName), "{\"blocks\":null}");
            Load(nullListName);
            Check("Null block list preserves the current parent", !IsLoading && _game.blocksParent == beforeCorrupt);
            _expectedError = "Blueprint parent Prefab is missing";
            try
            {
                Invoke(
                    _loader,
                    "Load",
                    BuildTargetContext.PlayerSave(firstName, firstName),
                    null,
                    _settings,
                    _build.mainCamera,
                    null,
                    null);
            }
            finally
            {
                _expectedError = null;
            }

            Check(
                "Missing parent Prefab preserves the current parent",
                !IsLoading && _game.blocksParent == beforeCorrupt);
            Checks.AddRange(HoverControlPlayChecks.Run());
            // Existing scene objects remain paused; new probe-owned effects need their real Update/coroutines.
            _allowPresentationUpdates = true;
            IEnumerator presentation = PresentationPlayChecks.Run(Checks);
            try
            {
                while (presentation.MoveNext())
                {
                    yield return presentation.Current;
                }
            }
            finally
            {
                (presentation as IDisposable)?.Dispose();
                _allowPresentationUpdates = false;
            }

            string enemyName = WriteBlueprint("enemy", ordinary, true);
            Load(enemyName, true);
            while (IsLoading)
            {
                yield return 0.05f;
            }

            VerifyRestoredComponents(ordinary, UnitFaction.Enemy);
            Check(
                "Enemy target uses its isolated blueprint directory",
                _saves.GetEnemyBlueprintPath(enemyName) != _saves.GetSavePath(enemyName));
        }
        finally
        {
            RestoreIsolation();
        }
    }

    private static void PrepareIsolation()
    {
        _build = BuildManager.instance;
        _saves = SaveManager.instance;
        _game = GameManager.instance;
        _play = PlayManager.instance;
        Check(
            "Scene managers and authored parent Prefab exist",
            _build != null && _saves != null && _game != null && _play != null && _build.blocksParentPrefab != null);
        object startupLoader = Field(_build, "_blueprintLoader");
        if (startupLoader != null)
        {
            Invoke(startupLoader, "Cancel");
        }

        PauseSceneUpdates(true);
        _snapshot = new Snapshot
        {
            playerFiles = Field(_saves, "_playerFiles"),
            enemyFiles = Field(_saves, "_enemyFiles"),
            saveName = _saves.currentSaveName,
            enemyName = _saves.currentEnemyBlueprintName,
            cached = _saves.cachedData,
            blocks = _saves.blocks,
            parent = _game.blocksParent,
            playParent = _play.blocksParent,
            parentActive = _game.blocksParent != null && _game.blocksParent.gameObject.activeSelf
        };
        if (_snapshot.parent != null)
        {
            _snapshot.parent.gameObject.SetActive(false);
        }

        _game.blocksParent = null;
        _play.blocksParent = null;
        _saves.blocks = new List<Block>();
        _saves.cachedData = new BlockDataList();
        _prefix = "__ArchitectureProbe_" + Guid.NewGuid().ToString("N") + "_";
        _temporaryDirectory = Path.GetFullPath(Path.Combine("Temp/Architecture", _prefix));
        Directory.CreateDirectory(_temporaryDirectory);
        Type fileStore = RuntimeType("BlueprintFileStore");
        SetField(
            _saves,
            "_playerFiles",
            Activator.CreateInstance(fileStore, new object[] { Path.Combine(_temporaryDirectory, "Player") }));
        SetField(
            _saves,
            "_enemyFiles",
            Activator.CreateInstance(fileStore, new object[] { Path.Combine(_temporaryDirectory, "Enemy") }));
        _loader = Activator.CreateInstance(RuntimeType("BlueprintLoader"), new object[] { _build, _saves, _game, _play });
        _settings = Activator.CreateInstance(RuntimeType("BlueprintLoadSettings"), true);
        foreach (string field in new[]
        {
            "InitialInterval",
            "MinimumInterval"
        }

        )
        {
            _settings.GetType().GetField(field).SetValue(_settings, 0.12f);
        }

        _settings.GetType().GetField("IntervalHalfLife").SetValue(_settings, 100f);
        _settings.GetType().GetField("GridSize").SetValue(_settings, 1f);
        _loader.GetType().GetEvent("StatisticsChanged").AddEventHandler(_loader, new Action<BlueprintStatistics>(value => Progress.Add(value)));
    }

    private static void PauseSceneUpdates(bool stopStartupCoroutines)
    {
        foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            behaviour.enabled = false;
            if (stopStartupCoroutines || behaviour != _build)
            {
                behaviour.StopAllCoroutines();
            }
        }
    }

    private static void VerifyFileStore()
    {
        object store = Field(_saves, "_playerFiles");
        string name = WriteBlueprint("files", Array.Empty<BlockData>());
        string source = _saves.GetSavePath(name);
        string bytes = File.ReadAllText(source);
        object[] copyArguments =
        {
            name,
            null,
            null
        };
        bool copied = (bool)Invoke(store, "TryDuplicate", copyArguments);
        string copyName = (string)copyArguments[1];
        Check(
            "File store duplicate preserves the source JSON",
            copied && File.ReadAllText(source) == bytes && File.ReadAllText(_saves.GetSavePath(copyName)) == bytes);
        object[] renameArguments =
        {
            copyName,
            name,
            null
        };
        Check(
            "Rename refuses to overwrite an existing blueprint",
            !(bool)Invoke(store, "TryRename", renameArguments) && File.Exists(_saves.GetSavePath(copyName)) && File.ReadAllText(source) == bytes);
        renameArguments = new object[]
        {
            copyName,
            _prefix + "renamed",
            null
        };
        Check(
            "Rename moves only the selected temporary blueprint",
            (bool)Invoke(store, "TryRename", renameArguments) && !File.Exists(_saves.GetSavePath(copyName)) && File.Exists(_saves.GetSavePath((string)renameArguments[1])));
    }

    private static BlockData[] CreateOrdinaryBlueprint()
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Blocks");
        GameObject cargo = prefabs.FirstOrDefault(prefab => prefab.GetComponent<CargoHold>() != null);
        GameObject cockpit = prefabs.FirstOrDefault(prefab => prefab.GetComponent<Cockpit>() != null);
        GameObject generator = prefabs.FirstOrDefault(prefab => prefab.GetComponent<PowerGeneratingUnit>() != null);
        Check(
            "Authored cargo, cockpit and generator resources exist",
            cargo != null && cockpit != null && generator != null);
        var result = new[]
        {
            MakeBlock("Assets/Resources/Blocks/1x1x1.prefab", 0),
            MakeBlock("Blocks/" + cargo.name, 1),
            MakeBlock("Blocks/" + cockpit.name, 2),
            MakeBlock("Blocks/" + generator.name, 3)
        };
        CargoHold hold = cargo.GetComponent<CargoHold>();
        result[1].cargo = new List<CargoItem>
        {
            new CargoItem
            {
                kind = hold.Kind,
                amount = 1,
                resourcePath = "Blocks/1x1x1"
            }
        };
        return result;
    }

    private static BlockData MakeBlock(string resourcePath, int index)
    {
        GameObject prefab = Resources.Load<GameObject>("Blocks/1x1x1");
        Check("Basic cube resource exists", prefab != null && prefab.GetComponent<Block>() != null);
        var data = new BlockData(prefab.GetComponent<Block>())
        {
            id = _prefix + "block_" + index + "_" + Guid.NewGuid().ToString("N"),
            resourcePath = resourcePath,
            posX = index * 4,
            posY = 1000,
            posZ = 0,
            rotX = 0,
            rotY = 0,
            rotZ = 0,
            rotW = 1
        };
        if (!string.IsNullOrWhiteSpace(resourcePath))
        {
            string shortPath = resourcePath.Replace("Assets/Resources/", string.Empty).Replace(".prefab", string.Empty);
            GameObject actual = Resources.Load<GameObject>(shortPath);
            if (actual != null && actual.TryGetComponent(out Block block))
            {
                data.x = block.x;
                data.y = block.y;
                data.z = block.z;
                if (!ResourcesUsed.Contains(shortPath))
                {
                    ResourcesUsed.Add(shortPath);
                }
            }
        }

        return data;
    }

    private static void VerifyRestoredComponents(BlockData[] data, UnitFaction faction)
    {
        Block cargoBlock = _saves.blocks.Single(block => block.uniqueId == data[1].id);
        Check(
            "Cargo and saved IDs survive restoration for " + faction,
            cargoBlock.GetComponent<CargoHold>().Used == 1 && data.All(item => _saves.blocks.Any(block => block.uniqueId == item.id)));
        Check(
            "Restored durability starts at authored maximum for " + faction,
            _saves.blocks.All(block => block.TryGetComponent(out Durability durability) && Mathf.Approximately(durability.currentDurability, durability.maxDurability)));
        Block cockpitBlock = _saves.blocks.Single(block => block.uniqueId == data[2].id);
        Check(
            "Cockpit faction follows build target " + faction,
            cockpitBlock.GetComponent<Cockpit>().faction == faction);
        Check(
            "Saved dimensions and poses survive restoration for " + faction,
            data.All(item =>
        {
            Block block = _saves.blocks.Single(value => value.uniqueId == item.id);
            return block.x == item.x && block.y == item.y && block.z == item.z && Vector3.Distance(block.transform.position, new Vector3(item.posX, item.posY, item.posZ)) < 0.001f;
        }));
    }

    private static string WriteBlueprint(string suffix, BlockData[] blocks, bool enemy = false)
    {
        string name = _prefix + suffix;
        string path = enemy ? _saves.GetEnemyBlueprintPath(name) : _saves.GetSavePath(name);
        File.WriteAllText(path, JsonUtility.ToJson(new BlockDataList { blocks = new List<BlockData>(blocks) }, true));
        return name;
    }

    private static void Load(string name, bool enemy = false, Action initialBlock = null)
    {
        bool duplicate = IsLoading && (bool)Invoke(
            _loader,
            "IsLoadingTarget",
            enemy ? BuildTargetContext.EnemyBlueprint(name, name) : BuildTargetContext.PlayerSave(name, name));
        if (!duplicate)
        {
            Progress.Clear();
        }

        _saves.currentSaveName = name;
        _saves.currentEnemyBlueprintName = name;
        BuildTargetContext context = enemy ? BuildTargetContext.EnemyBlueprint(name, name) : BuildTargetContext.PlayerSave(name, name);
        Invoke(
            _loader,
            "Load",
            context,
            _build.blocksParentPrefab,
            _settings,
            _build.mainCamera,
            initialBlock,
            null);
    }

    private static bool IsLoading => _loader != null && (bool)_loader.GetType().GetProperty("IsLoading").GetValue(_loader);

    private static Type RuntimeType(string name)
    {
        return typeof(BuildManager).Assembly.GetType(name, true);
    }

    private static object Invoke(object target, string method, params object[] arguments)
    {
        return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, arguments);
    }

    private static object Field(object target, string field)
    {
        return target.GetType().GetField(field, InstanceFields).GetValue(target);
    }

    private static void SetField(object target, string field, object value)
    {
        target.GetType().GetField(field, InstanceFields).SetValue(target, value);
    }

    private static void Check(string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        Checks.Add(name);
    }

    private static void RestoreIsolation()
    {
        try
        {
            if (_loader != null)
            {
                Invoke(_loader, "Cancel");
            }
        }
        catch (Exception exception)
        {
            // Cancellation failure must not prevent restoration of the actual save directories and cache.
            Errors.Add("Loader cancellation during cleanup failed: " + exception);
        }
        finally
        {
            _loader = null;
        }

        if (_snapshot != null)
        {
            Transform probeParent = _game.blocksParent;
            _game.blocksParent = _snapshot.parent;
            _play.blocksParent = _snapshot.playParent;
            _saves.currentSaveName = _snapshot.saveName;
            _saves.currentEnemyBlueprintName = _snapshot.enemyName;
            _saves.cachedData = _snapshot.cached;
            _saves.blocks = _snapshot.blocks;
            SetField(_saves, "_playerFiles", _snapshot.playerFiles);
            SetField(_saves, "_enemyFiles", _snapshot.enemyFiles);
            if (probeParent != null && probeParent != _snapshot.parent)
            {
                Object.Destroy(probeParent.gameObject);
            }

            if (_snapshot.parent != null)
            {
                _snapshot.parent.gameObject.SetActive(_snapshot.parentActive);
            }

            _snapshot = null;
        }

        if (_temporaryDirectory != null && Directory.Exists(_temporaryDirectory))
        {
            string root = Path.GetFullPath("Temp/Architecture") + Path.DirectorySeparatorChar;
            if (!_temporaryDirectory.StartsWith(root, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(_temporaryDirectory) != _prefix)
            {
                throw new InvalidOperationException("Refusing to remove a directory outside the probe scope.");
            }

            Directory.Delete(_temporaryDirectory, true);
            _temporaryDirectory = null;
        }
    }

    private sealed class Snapshot
    {
        public object playerFiles;
        public object enemyFiles;
        public string saveName;
        public string enemyName;
        public BlockDataList cached;
        public List<Block> blocks;
        public Transform parent;
        public Transform playParent;
        public bool parentActive;
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public bool completed;
        public string unity;
        public string scope;
        public string[] checks;
        public string[] errors;
        public string[] resources;
    }
}
