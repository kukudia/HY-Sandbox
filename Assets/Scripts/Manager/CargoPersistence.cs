using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CargoPersistence
{
    // Preserve blueprint geometry; only surviving player holds bring contents back to build mode.
    public static bool SaveReturnCargo(bool survived)
    {
        if (SaveManager.instance == null || string.IsNullOrWhiteSpace(SaveManager.instance.currentSaveName)) return true;
        string path = SaveManager.instance.GetSavePath(SaveManager.instance.currentSaveName);
        try
        {
            if (!File.Exists(path)) return true;
            BlockDataList data = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            if (data == null || data.blocks == null) throw new InvalidDataException("Invalid player blueprint.");
            var contents = new Dictionary<string, List<CargoItem>>();
            if (survived) foreach (CargoHold hold in CargoHold.Active)
            {
                ControlUnit unit = hold.Owner;
                if (unit == null || !unit.IsPlayer || !unit.HasValidCockpit) continue;
                Block block = hold.GetComponent<Block>();
                if (!string.IsNullOrEmpty(block.uniqueId)) contents[block.uniqueId] = hold.CaptureContents();
            }
            foreach (BlockData block in data.blocks)
                block.cargo = contents.TryGetValue(block.id, out List<CargoItem> cargo) ? cargo : new List<CargoItem>();
            string temporary = path + ".cargo.tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            File.Replace(temporary, path, path + ".cargo.bak");
            SaveManager.instance.cachedData = data;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Cargo return could not be saved; current session kept: " + exception.Message);
            return false;
        }
    }
}
