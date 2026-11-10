// 职责：仅从SaveManager共用文件仓读取建筑快照；World与WorldLab共用解析和错误隔离。
// 入口：LoadRoutine()预热世界资源；Load()刷新WorldLab目录；仅世界生成检查平台范围，不修改源文件。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal sealed class WorldBuildingLibrary
{
    internal readonly List<BlockDataList> Templates = new List<BlockDataList>();
    internal readonly List<string> Names = new List<string>();
    internal readonly List<string> Errors = new List<string>();
    internal float Progress
    {
        get; private set;
    }

    internal IEnumerator LoadRoutine(BlueprintFileStore files)
    {
        Progress = 0f;
        var resources = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in ReadSources(files, false, resources))
        {
            yield return null;
        }

        int complete = 0;
        foreach (string resource in resources)
        {
            ResourceRequest request = Resources.LoadAsync<GameObject>(resource);
            while (!request.isDone)
            {
                Progress = (complete + request.progress) / Mathf.Max(1, resources.Count + 1);
                yield return null;
            }

            complete++;
            Progress = (float)complete / Mathf.Max(1, resources.Count + 1);
            yield return null;
        }

        foreach (string name in ReadSources(files, true, null))
        {
            yield return null;
        }

        Progress = 1f;
    }

    internal void Load(BlueprintFileStore files)
    {
        // The standalone palette refresh is synchronous; the world yields between the same reads.
        foreach (string name in ReadSources(files, false, null))
        {
        }
    }

    private IEnumerable<string> ReadSources(BlueprintFileStore files, bool forWorldGeneration, HashSet<string> warmupResources)
    {
        Templates.Clear();
        Names.Clear();
        Errors.Clear();
        var names = new List<string>();
        try
        {
            names.AddRange(files.GetNames());
            names.Sort(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Errors.Add(error.Message);
        }

        foreach (string name in names)
        {
            ReadSource(name, files.GetPath(name), forWorldGeneration, warmupResources);
            yield return name;
        }
    }

    private void ReadSource(string name, string path, bool forWorldGeneration, HashSet<string> warmupResources)
    {
        try
        {
            if (new FileInfo(path).Length > 4 * 1024 * 1024)
            {
                throw new InvalidOperationException("Blueprint exceeds 4 MB.");
            }

            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            if (warmupResources != null)
            {
                // Warm only files in the shared store; packaged examples never become runtime fallbacks.
                CollectBlueprintResources(blueprint, warmupResources);
                return;
            }

            blueprint.foundation = BuildingFootprint.Calculate(blueprint.blocks, 1);
            WorldBuildingValidation.ValidateBlueprint(blueprint);
            if (forWorldGeneration)
            {
                WorldBuildingValidation.ValidateWorldTemplate(blueprint);
            }

            Templates.Add(blueprint);
            Names.Add(name);
        }
        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException || error is IOException || error is UnauthorizedAccessException)
        {
            Errors.Add(name + ".json: " + error.Message);
        }
    }

    private static void CollectBlueprintResources(BlockDataList blueprint, HashSet<string> resources)
    {
        if (blueprint?.blocks == null)
        {
            return;
        }

        foreach (BlockData block in blueprint.blocks)
        {
            if (!string.IsNullOrWhiteSpace(block?.resourcePath))
            {
                resources.Add(BlueprintBlockFactory.ToResourcePath(block.resourcePath));
            }
        }

        if (blueprint.outpost?.garrison != null)
        {
            foreach (OutpostGarrisonData guard in blueprint.outpost.garrison)
            {
                if (guard?.blueprint?.blocks != null)
                {
                    // Only one embedded level is supported; validation rejects nested outposts.
                    foreach (BlockData block in guard.blueprint.blocks)
                    {
                        if (!string.IsNullOrWhiteSpace(block?.resourcePath))
                        {
                            resources.Add(BlueprintBlockFactory.ToResourcePath(block.resourcePath));
                        }
                    }
                }
            }
        }
    }
}
