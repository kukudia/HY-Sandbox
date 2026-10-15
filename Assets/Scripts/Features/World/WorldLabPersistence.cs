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
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(edits, true));
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
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (stream.Length > 16 * 1024 * 1024)
            {
                throw new InvalidDataException("World edit file exceeds 16 MB.");
            }

            using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            {
                var edits = JsonUtility.FromJson<VoxelWorldEdits>(reader.ReadToEnd());
                if (edits == null)
                {
                    throw new InvalidDataException("World edit file is empty.");
                }

                return edits;
            }
        }
    }
}
