// 职责：管理逐帧蓝图恢复、请求所有权、取消、统计及连接重建。
// 入口：Load() / Cancel()；依赖由 BuildManager 装配，UI 通过 StatisticsChanged 消费结果。
// 生命周期：新请求使旧版本失效；结束、取消及异常均释放镜头与会话状态。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

internal sealed class BlueprintLoader
{
    private readonly MonoBehaviour _runner;
    private readonly SaveManager _saves;
    private readonly GameManager _game;
    private readonly PlayManager _play;
    private readonly BlueprintLoadCamera _camera = new BlueprintLoadCamera();
    private Coroutine _routine;
    private int _version;
    private BuildTargetContext _target;
    private string _path;
    private Transform _parent;
    public bool IsLoading
    {
        get; private set;
    }

    public event Action<BlueprintStatistics> StatisticsChanged;
    public BlueprintLoader(MonoBehaviour runner, SaveManager saves, GameManager game, PlayManager play)
    {
        _runner = runner;
        _saves = saves;
        _game = game;
        _play = play;
    }

    /// <summary>验证文件后开始新会话；相同目标的重复请求保持当前进度。</summary>
    public void Load(
        BuildTargetContext target,
        GameObject parentPrefab,
        BlueprintLoadSettings settings,
        Camera camera,
        Action createInitialBlock,
        Action<Transform, BlockDataList> prepareParent = null)
    {
        if (IsLoadingTarget(target))
        {
            return;
        }

        Cancel();
        string path = target.GetSavePath(_saves);
        if (!TryRead(path, out BlockDataList data))
        {
            return;
        }

        if (parentPrefab == null)
        {
            Debug.LogError("Blueprint parent Prefab is missing.");
            return;
        }

        ReplaceParent(parentPrefab, target.Name);
        _saves.cachedData = data;
        prepareParent?.Invoke(_parent, data);
        _target = target;
        _path = path;
        var blocks = new List<BlockData>(data.blocks);
        _camera.Begin(_parent.gameObject, blocks, settings, camera);
        IsLoading = true;
        StatisticsChanged?.Invoke(default);
        int version = _version;
        Coroutine routine = _runner.StartCoroutine(LoadRoutine(version, blocks, settings, createInitialBlock));
        // StartCoroutine can finish synchronously for an empty blueprint; do not retain a completed handle.
        if (IsLoading && version == _version)
        {
            _routine = routine;
        }
    }

    public bool IsLoadingTarget(BuildTargetContext target)
    {
        return IsLoading && _parent != null && _game != null && _game.blocksParent == _parent && _target.Kind == target.Kind && _target.SaveName == target.SaveName && _target.EnemyBlueprintName == target.EnemyBlueprintName && string.Equals(_path, target.GetSavePath(_saves), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>停止恢复和镜头；保留当前父对象供调用者决定替换或继续编辑。</summary>
    public void Cancel()
    {
        _version++;
        Coroutine routine = _routine;
        _routine = null;
        if (routine != null && _runner != null)
        {
            _runner.StopCoroutine(routine);
        }

        FinishSession();
    }

    private IEnumerator LoadRoutine(int version, List<BlockData> blocks, BlueprintLoadSettings settings, Action createInitialBlock)
    {
        double start = Time.timeAsDouble;
        var missingIds = new HashSet<string>();
        var statistics = new BlueprintStatistics();
        try
        {
            for (int index = 0; index < blocks.Count; index++)
            {
                if (!OwnsParent(version))
                {
                    yield break;
                }

                BlockData data = blocks[index];
                if (BlueprintBlockFactory.TryRestore(data, _parent, _target.Faction, out Block block))
                {
                    _saves.blocks.Add(block);
                    statistics.Accumulate(block);
                    StatisticsChanged?.Invoke(statistics);
                }
                else
                {
                    if (data != null)
                    {
                        missingIds.Add(data.id);
                    }

                    Debug.LogWarning($"Blueprint block {index + 1} could not be restored: {data?.resourcePath}");
                }

                if (index < blocks.Count - 1)
                {
                    float interval = settings.GetInterval(Time.timeAsDouble - start);
                    yield return interval > 0f ? new WaitForSeconds(interval) : null;
                }
            }

            if (!OwnsParent(version))
            {
                yield break;
            }

            if (blocks.Count == 0 && !_target.IsBuildingBlueprint)
            {
                createInitialBlock?.Invoke();
            }

            RemoveMissingData(missingIds);
            IEnumerator connections = RebuildConnections(version);
            try
            {
                while (connections.MoveNext())
                {
                    yield return connections.Current;
                }
            }
            finally
            {
                (connections as IDisposable)?.Dispose();
            }

            Debug.Log($"加载{_path}完成，耗时{Time.timeAsDouble - start}s，共{blocks.Count}个方块，恢复成功{statistics.BlockCount}个。");
        }
        finally
        {
            // An old iterator must never clear a newer request's state.
            if (version == _version)
            {
                if (!OwnsParent(version))
                {
                    ReleaseInvalidParent();
                }

                FinishSession();
            }
        }
    }

    private bool TryRead(string path, out BlockDataList data)
    {
        data = null;
        try
        {
            if (!File.Exists(path))
            {
                data = new BlockDataList();
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
            }
            else
            {
                string json = File.ReadAllText(path);
                // JsonUtility can turn a null/missing list into an empty initialized list.
                // Check the stored shape before replacing a user's currently loaded construct.
                if (!(JToken.Parse(json) is JObject document) || !(document["blocks"] is JArray))
                {
                    Debug.LogWarning("Blueprint must contain a blocks array; keeping the current construct.");
                    return false;
                }

                data = JsonUtility.FromJson<BlockDataList>(json);
            }

            if (data != null && data.blocks != null)
            {
                return true;
            }

            Debug.LogWarning("保存文件为空或损坏，保留当前蓝图。");
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is JsonException)
        {
            Debug.LogWarning($"Cannot read blueprint {path}: {exception.Message}");
        }

        return false;
    }

    private void ReplaceParent(GameObject prefab, string name)
    {
        if (_game.blocksParent != null)
        {
            // Destroy 延迟到帧末；先退出物理查询，避免新蓝图连接到即将销毁的旧对象。
            _game.blocksParent.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_game.blocksParent.gameObject);
        }

        GameObject obj = UnityEngine.Object.Instantiate(prefab);
        obj.name = name;
        _parent = obj.transform;
        _game.blocksParent = _parent;
        _play.blocksParent = _parent;
        _saves.blocks.Clear();
        obj.GetComponent<Rigidbody>().isKinematic = true;
    }

    private bool OwnsParent(int version)
    {
        return version == _version && _parent != null && _game != null && _game.blocksParent == _parent;
    }

    private void RemoveMissingData(HashSet<string> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        _saves.cachedData.blocks.RemoveAll(data => data == null || ids.Contains(data.id));
        File.WriteAllText(_path, JsonUtility.ToJson(_saves.cachedData, true));
    }

    private IEnumerator RebuildConnections(int version)
    {
        var budget = System.Diagnostics.Stopwatch.StartNew();
        for (int index = 0; index < _saves.blocks.Count; index++)
        {
            if (!OwnsParent(version))
            {
                yield break;
            }

            Block block = _saves.blocks[index];
            if (block != null)
            {
                block.neighbors = block.Neighbors();
                block.CheckConnection();
            }

            // 大载具的末尾连接重建同样需要分帧，不能在模块逐帧恢复后再集中阻塞数秒。
            if (budget.Elapsed.TotalMilliseconds >= 2d)
            {
                yield return null;
                budget.Restart();
            }
        }
    }

    private void ReleaseInvalidParent()
    {
        // Only clear registries if they still belong to this load, not a replacement parent.
        if (_game != null && _game.blocksParent == _parent)
        {
            _game.blocksParent = null;
            _saves.blocks.Clear();
        }

        if (_play != null && _play.blocksParent == _parent)
        {
            _play.blocksParent = null;
        }

        if (_parent != null)
        {
            UnityEngine.Object.Destroy(_parent.gameObject);
        }
    }

    private void FinishSession()
    {
        IsLoading = false;
        _routine = null;
        _camera.Stop();
        _path = null;
        _parent = null;
        _target = default;
    }
}
