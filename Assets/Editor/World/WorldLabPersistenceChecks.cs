// 职责：验证差量保存/读取、原子替换备份与损坏文件拒绝，使用独立临时路径。
// 入口：Run()；不读写玩家或WorldLab真实存档；结束删除本次测试文件。
using System;
using System.IO;
using UnityEngine;

public static class WorldLabPersistenceChecks
{
    public static string Run()
    {
        int count = 0;
        var names = new System.Collections.Generic.List<string>();
        void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException(name);
            }

            count++;
            names.Add(name);
        }

        void Reject(Action action, string name)
        {
            bool rejected = false;
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Check(rejected, name);
        }

        string directory = Path.Combine(Path.GetTempPath(), "HY-WorldLab-check-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "world.json");
        var data = new VoxelWorldData();
        data.Generate(new Vector3Int(32, 24, 32), 14721);
        uint baseline = data.Fingerprint();
        VoxelWorldEdits empty = VoxelWorldEdits.Capture(data, 14721);
        Check(
            empty.edits.Length == 0 && empty.Restore(14721, data.Size).Fingerprint() == baseline,
            "empty delta reproduces baseline");
        var removed = new Vector3Int(4, 1, 4);
        var filled = new Vector3Int(8, 23, 8);
        data.Set(removed, 0);
        data.Set(filled, 3);
        VoxelWorldEdits delta = VoxelWorldEdits.Capture(data, 14721);
        Check(delta.edits.Length == 2, "only changed cells stored");
        Check(
            delta.Restore(14721, data.Size).Fingerprint() == data.Fingerprint(),
            "mixed removal and fill roundtrip");
        try
        {
            WorldLabPersistence.Save(path, delta);
            Check(
                WorldLabPersistence.Load(path).Restore(14721, data.Size).Fingerprint() == data.Fingerprint(),
                "disk UTF8 roundtrip");
            WorldLabPersistence.Save(path, empty);
            Check(WorldLabPersistence.Load(path).edits.Length == 0, "atomic replace writes new delta");
            Check(WorldLabPersistence.Load(path + ".previous").edits.Length == 2, "previous save retained");
            WorldLabPersistence.Save(path, delta);
            Check(WorldLabPersistence.Load(path + ".previous").edits.Length == 0, "repeated backup replacement");
            Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "successful saves leave no temporary files");
            Reject(() => delta.Restore(99, data.Size), "wrong seed rejected");
            Reject(() => delta.Restore(14721, new Vector3Int(16, 24, 16)), "wrong dimensions rejected");
            delta.version = 2;
            Reject(() => delta.Restore(14721, data.Size), "future version rejected");
            delta.version = 1;
            string identity = delta.baselineFingerprint;
            delta.baselineFingerprint = "00000000";
            Reject(() => delta.Restore(14721, data.Size), "changed generator rejected");
            delta.baselineFingerprint = identity;
            VoxelWorldEdits.CellEdit[] valid = delta.edits;
            delta.edits = new[]
            {
                valid[0],
                valid[0]
            };
            Reject(() => delta.Restore(14721, data.Size), "duplicate cells rejected");
            delta.edits = new[]
            {
                new VoxelWorldEdits.CellEdit
                {
                    cell = removed,
                    material = 999
                }
            };
            Reject(() => delta.Restore(14721, data.Size), "illegal material rejected");
            delta.edits = new[]
            {
                new VoxelWorldEdits.CellEdit
                {
                    cell = new Vector3Int(-1, 0, 0),
                    material = 0
                }
            };
            Reject(() => delta.Restore(14721, data.Size), "out of range rejected");
            Check(data.Get(removed) == 0 && data.Get(filled) == 3, "failed restore never mutates original data");
            File.WriteAllText(path, "{broken json");
            bool malformedRejected = false;
            try
            {
                WorldLabPersistence.Load(path);
            }
            catch (ArgumentException)
            {
                malformedRejected = true;
            }

            Check(malformedRejected, "malformed JSON rejected");
            Check(
                WorldLabPersistence.Load(path + ".previous").Restore(14721, data.Size).Fingerprint() == baseline,
                "corrupt current file leaves backup readable");
        }
        finally
        {
            // Delete only exact files owned by this unique test, never recursively remove user data.
            foreach (string file in new[]
            {
                path,
                path + ".previous"
            }

            )
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            if (Directory.Exists(directory) && Directory.GetFiles(directory).Length == 0)
            {
                Directory.Delete(directory);
            }
        }

        string resultPath = "Docs/Validation/2026-10-15-world-persistence.json";
        File.WriteAllText(resultPath, JsonUtility.ToJson(new Result { passed = count, checks = names.ToArray() }, true));
        return count + " persistence checks passed";
    }

    [Serializable]
    private sealed class Result
    {
        public int passed;
        public string[] checks;
    }
}
