// 职责：独立WorldLab差量JSON的原子保存、上一份备份与有大小限制的读取。
// 入口：Save() / Load()；仅写调用者传入路径，不接触玩家载具蓝图和货仓。
using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class WorldLabPersistence
{
    public static void Save(string path, VoxelWorldEdits edits)
    {
        WriteJson(path, JsonUtility.ToJson(edits, true));
    }

    public static void SaveSnapshot(string path, WorldLabSnapshot snapshot)
    {
        WriteJson(path, JsonUtility.ToJson(snapshot, true));
    }

    private static void WriteJson(string path, string json)
    {
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            if (bytes.Length > 16 * 1024 * 1024)
            {
                throw new InvalidDataException("World snapshot exceeds 16 MB.");
            }

            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(path))
            {
                File.Replace(temporary, path, path + ".previous");
            }
            else
            {
                File.Move(temporary, path);
            }
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    public static VoxelWorldEdits Load(string path)
    {
        var edits = JsonUtility.FromJson<VoxelWorldEdits>(ReadJson(path));
        if (edits == null)
        {
            throw new InvalidDataException("World edit file is empty.");
        }

        return edits;
    }

    public static WorldLabSnapshot LoadSnapshot(string path, float legacyCellSize)
    {
        string json = ReadJson(path);
        var snapshot = JsonUtility.FromJson<WorldLabSnapshot>(json);
        if (snapshot == null)
        {
            throw new InvalidDataException("World snapshot is empty.");
        }

        if (snapshot.version == 1)
        {
            // 旧格式是顶层体素差量；迁移为空建筑列表，不猜测或读取其他蓝图。
            return new WorldLabSnapshot
            {
                cellSize = legacyCellSize,
                terrain = JsonUtility.FromJson<VoxelWorldEdits>(json)
            };
        }

        return snapshot;
    }

    private static string ReadJson(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (stream.Length > 16 * 1024 * 1024)
            {
                throw new InvalidDataException("World edit file exceeds 16 MB.");
            }

            using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
