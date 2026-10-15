// 职责：在返回/死亡时持久化或清空玩家货仓，保持蓝图几何数据。
// 入口：SaveReturnCargo()。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CargoPersistence
{
    // Preserve blueprint geometry; only surviving player holds bring contents back to build mode.
    public static bool SaveReturnCargo(bool survived)
    {
        return SaveReturnCargo(survived, out _);
    }

    public static bool SaveReturnCargo(bool survived, out string failureReason)
    {
        failureReason = string.Empty;
        if (SaveManager.instance == null || string.IsNullOrWhiteSpace(SaveManager.instance.currentSaveName))
        {
            return true;
        }

        try
        {
            string path = SaveManager.instance.GetSavePath(SaveManager.instance.currentSaveName);
            if (!File.Exists(path))
            {
                // A named save disappearing must not turn a failed return into destructive session cleanup.
                throw new FileNotFoundException("Player blueprint is missing; restore the save before returning.", path);
            }

            BlockDataList data = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            if (data == null || data.blocks == null)
            {
                throw new InvalidDataException("Invalid player blueprint.");
            }

            var contents = new Dictionary<string, List<CargoItem>>();
            if (survived)
            {
                foreach (CargoHold hold in CargoHold.Active)
                {
                    ControlUnit unit = hold.Owner;
                    if (unit == null || !unit.IsPlayer || !unit.HasValidCockpit)
                    {
                        continue;
                    }

                    Block block = hold.GetComponent<Block>();
                    if (!string.IsNullOrEmpty(block.uniqueId))
                    {
                        contents[block.uniqueId] = hold.CaptureContents();
                    }
                }
            }

            foreach (BlockData block in data.blocks)
            {
                block.cargo = contents.TryGetValue(block.id, out List<CargoItem> cargo) ? cargo : new List<CargoItem>();
            }

            string temporary = path + ".cargo.tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            File.Replace(temporary, path, path + ".cargo.bak");
            SaveManager.instance.cachedData = data;
            return true;
        }
        catch (Exception exception)
        {
            failureReason = exception is FileNotFoundException ? "The selected save is missing. Restore it, then retry Return." : exception is ArgumentException || exception is InvalidDataException ? "The selected save is damaged. Restore a valid copy, then retry Return." : "The save could not be written. Check available space, file access or locks, then retry Return.";
            Debug.LogError("Cargo return could not be saved; current session kept: " + exception.Message);
            return false;
        }
    }
}
