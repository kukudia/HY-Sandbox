// 职责：从SaveManager共用文件仓读取建筑快照，合并内置模板；World与WorldLab共用解析、覆盖和错误隔离。
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
        TextAsset[] bundled = Resources.LoadAll<TextAsset>("WorldBlueprints/Buildings");
        var resources = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in ReadSources(files, bundled, false, resources))
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

        foreach (string name in ReadSources(files, bundled, true, null))
        {
            yield return null;
        }

        Progress = 1f;
    }

    internal void Load(BlueprintFileStore files, IEnumerable<TextAsset> bundled)
    {
        // The standalone palette refresh is synchronous; the world yields between the same reads.
        foreach (string name in ReadSources(files, bundled, false, null))
        {
        }
    }

    private IEnumerable<string> ReadSources(
        BlueprintFileStore files,
        IEnumerable<TextAsset> bundled,
        bool forWorldGeneration,
        HashSet<string> warmupResources)
    {
        Templates.Clear();
        Names.Clear();
        Errors.Clear();
        foreach (TextAsset asset in bundled)
        {
            if (asset != null)
            {
                ReadSource(asset.name, asset.text, null, forWorldGeneration, warmupResources);
                yield return asset.name;
            }
        }

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
            ReadSource(name, null, files.GetPath(name), forWorldGeneration, warmupResources);
            yield return name;
        }
    }

    private void ReadSource(string name, string json, string path, bool forWorldGeneration, HashSet<string> warmupResources)
    {
        try
        {
            if (path != null && new FileInfo(path).Length > 4 * 1024 * 1024)
            {
                throw new InvalidOperationException("Blueprint exceeds 4 MB.");
            }

            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(json ?? File.ReadAllText(path));
            if (warmupResources != null)
            {
                // Warm both bundled and user sources, including the fallback of an invalid override.
                CollectBlueprintResources(blueprint, warmupResources);
                return;
            }

            WorldBuildingValidation.ValidateBlueprint(blueprint);
            if (forWorldGeneration)
            {
                ValidateWorldTemplate(blueprint);
            }

            // Only fully valid user data can replace a bundled fallback.
            int existing = Names.IndexOf(name);
            if (existing >= 0)
            {
                Templates[existing] = blueprint;
            }
            else
            {
                Templates.Add(blueprint);
                Names.Add(name);
            }
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

    private static void ValidateWorldTemplate(BlockDataList blueprint)
    {
        if (blueprint.blocks.Count == 0)
        {
            throw new InvalidOperationException("Blueprint has no blocks.");
        }

        foreach (BlockData block in blueprint.blocks)
        {
            Vector3 half = new Vector3(block.x, block.y, block.z) * 0.5f;
            Quaternion rotation = new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = rotation * Vector3.Scale(
                    half,
                    new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                point += new Vector3(block.posX, block.posY, block.posZ);
                if (point.y < -0.01f || Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.z)) > 128f)
                {
                    throw new InvalidOperationException("Building extends below its foundation or beyond the 128 m platform radius.");
                }
            }
        }
    }
}
