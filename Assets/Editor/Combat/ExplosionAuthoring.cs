// 职责：为现有方块CSV增加独立爆炸伤害列，并通过现有备份/校验链保存真实Prefab。
// 入口：InstallDamageColumn()；重复运行保留已经编辑的伤害值，不写玩家蓝图或场景。
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ExplosionAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Combat/Install independent explosion damage")]
    public static void Install()
    {
        Debug.Log(InstallDamageColumn());
    }

    public static string InstallDamageColumn()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before migrating explosion authoring.");
        }

        var records = BlockCsvDocument.ParseRecords(File.ReadAllText(BlockCsvDocument.AssetPath, Encoding.UTF8).TrimStart('\uFEFF'));
        const string header = "Block.explosionDamage | 爆炸伤害";
        if (!records[0].Contains(header))
        {
            int force = records[0].FindIndex(column => column.StartsWith("Block.explosionForce |", StringComparison.Ordinal));
            if (force < 0)
            {
                throw new InvalidDataException("The existing explosion force column is missing.");
            }

            // Legacy force is an impulse setting, not a health unit. Start with a conservative
            // tenth of its magnitude to avoid turning every engine loss into a whole-ship wipe.
            for (int row = 1; row < records.Count; row++)
            {
                float damage = records[row][force].Length > 0 ? Mathf.Clamp(float.Parse(records[row][force], CultureInfo.InvariantCulture) * .1f, 0f, 100f) : 0f;
                records[row].Insert(force + 1, damage.ToString("R", CultureInfo.InvariantCulture));
            }

            records[0].Insert(force + 1, header);
            string text = BlockCsvDocument.WriteRecords(records.Select(row => row.AsEnumerable()));
            BlockCsvDocument.Parse(text);
            File.WriteAllText(BlockCsvDocument.AssetPath, text, new UTF8Encoding(true));
            AssetDatabase.ImportAsset(BlockCsvDocument.AssetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        BlockCsvAuthoring.Bake();
        return BlockCsvAuthoring.LastReport;
    }
}
