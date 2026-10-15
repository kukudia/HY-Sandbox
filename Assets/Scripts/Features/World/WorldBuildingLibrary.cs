// 职责：读取用户建筑目录的全部有效JSON，计算生成平台所需范围，坏文件单独报告而不阻断世界。
// 入口：Load()；每次新会话重新读取，不修改用户蓝图文件。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal sealed class WorldBuildingLibrary
{
    internal readonly List<BlockDataList> Templates = new List<BlockDataList>();
    internal readonly List<string> Names = new List<string>();
    internal readonly List<string> Errors = new List<string>();
    internal float PlatformRadius
    {
        get; private set;
    }
    internal static string DirectoryPath => Path.Combine(Application.persistentDataPath, "BuildingBlueprints");

    internal void Load(string directory)
    {
        Templates.Clear();
        Names.Clear();
        Errors.Clear();
        PlatformRadius = 0f;
        if (!Directory.Exists(directory))
        {
            return;
        }

        string[] files = Directory.GetFiles(directory, "*.json");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        foreach (string path in files)
        {
            try
            {
                if (new FileInfo(path).Length > 4 * 1024 * 1024)
                {
                    throw new InvalidOperationException("Blueprint exceeds 4 MB.");
                }

                BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
                WorldBuildingValidation.ValidateBlueprint(blueprint);
                if (blueprint.blocks.Count == 0)
                {
                    throw new InvalidOperationException("Blueprint has no blocks.");
                }

                float radius = Mathf.Max(2f, blueprint.foundation.width - 2f, blueprint.foundation.length - 2f);
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

                        radius = Mathf.Max(radius, Mathf.Abs(point.x), Mathf.Abs(point.z));
                    }
                }

                Templates.Add(blueprint);
                Names.Add(Path.GetFileNameWithoutExtension(path));
                PlatformRadius = Mathf.Max(PlatformRadius, radius);
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException || error is IOException || error is UnauthorizedAccessException)
            {
                Errors.Add(Path.GetFileName(path) + ": " + error.Message);
            }
        }
    }
}
