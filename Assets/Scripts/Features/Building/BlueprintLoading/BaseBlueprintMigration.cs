// 职责：将旧建筑库中已标记核心的基地迁移到独立目录，保留原始字节备份且不覆盖同名设计。
// 入口：Run()；复制校验完成后才归档源文件，重复执行可安全恢复中断的迁移。
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

internal static class BaseBlueprintMigration
{
    public static void Run(string buildings, string bases)
    {
        Directory.CreateDirectory(bases);
        if (!Directory.Exists(buildings))
        {
            return;
        }

        foreach (string source in Directory.GetFiles(buildings, "*.json"))
        {
            try
            {
                JObject document = JObject.Parse(File.ReadAllText(source));
                if (string.IsNullOrWhiteSpace((string)document["siegeCoreId"]) || document["outpost"]?["enabled"]?.Value<bool>() == true)
                {
                    continue;
                }

                byte[] bytes = File.ReadAllBytes(source);
                string name = Path.GetFileNameWithoutExtension(source);
                string target = Path.Combine(bases, name + ".json");
                for (int suffix = 1; File.Exists(target) && !File.ReadAllBytes(target).SequenceEqual(bytes); suffix++)
                {
                    target = Path.Combine(bases, name + " (migrated " + suffix + ").json");
                }

                if (!File.Exists(target))
                {
                    using (var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                if (!File.ReadAllBytes(target).SequenceEqual(bytes))
                {
                    throw new IOException("Base migration verification failed: " + target);
                }

                string archive = Path.Combine(Path.GetDirectoryName(buildings), "BlueprintMigrationBackups");
                Directory.CreateDirectory(archive);
                string backup = Path.Combine(archive, Path.GetFileName(target));
                for (int suffix = 1; File.Exists(backup) && !File.ReadAllBytes(backup).SequenceEqual(bytes); suffix++)
                {
                    backup = Path.Combine(archive, name + " (" + suffix + ").json");
                }

                if (!File.Exists(backup))
                {
                    File.Copy(source, backup, false);
                }

                if (File.ReadAllBytes(backup).SequenceEqual(bytes))
                {
                    File.Delete(source);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is Newtonsoft.Json.JsonException || error is ArgumentException)
            {
                Debug.LogWarning("Base migration left the original intact: " + source + ": " + error.Message);
            }
        }
    }
}
