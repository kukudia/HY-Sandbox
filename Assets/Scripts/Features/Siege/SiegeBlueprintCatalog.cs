// 职责：只读加载阵地战双方的用户蓝图快照，过滤不能部署的存档，不修改源文件。
// 入口：Load()；玩家来自Saves，攻方来自EnemyBlueprints，压力场可保留固定夹具。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SiegeBlueprintCatalog
{
    public static SiegeShipOption[] Load(string directory, int coinsPerBlock, out string message)
    {
        var options = new List<SiegeShipOption>();
        int rejected = 0;
        try
        {
            if (!Directory.Exists(directory))
            {
                message = "No blueprints in " + Path.GetFileName(directory) + ".";
                return options.ToArray();
            }

            string[] files = Directory.GetFiles(directory, "*.json");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    BlockDataList data = JsonUtility.FromJson<BlockDataList>(json);
                    // JsonUtility会为缺失的内联类创建默认对象，foundation非空不能判断舰船/建筑。
                    if (data == null || data.IsOutpost || !ModularUnitFactory.IsValidBlueprint(data, Path.GetFileName(file)))
                    {
                        rejected++;
                        continue;
                    }

                    int price = (int)Math.Min(int.MaxValue, (long)data.blocks.Count * Math.Max(1, coinsPerBlock));
                    options.Add(new SiegeShipOption(Path.GetFileNameWithoutExtension(file), json, price));
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
                {
                    rejected++;
                }
            }

            message = Path.GetFileName(directory) + ": " + options.Count + " ships" + (rejected > 0 ? "; skipped " + rejected + " invalid blueprints." : ".");
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            message = "Cannot read " + Path.GetFileName(directory) + ": " + exception.Message;
        }

        return options.ToArray();
    }
}
