// 职责：菜单中的只读蓝图目录、示例和出战校验；明确编辑示例时才创建独立副本。
// 入口：Read() / Inspect() / CopyExample()；与工坊共用蓝图目录和建造/部署规则。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public sealed class MenuBlueprintEntry
{
    public string Name
    {
        get;
    }
    public bool Building
    {
        get;
    }
    public TextAsset Example
    {
        get;
    }
    public bool IsExample => Example != null;
    public string Label => Name + (IsExample ? "  [EXAMPLE]" : string.Empty);

    public MenuBlueprintEntry(string name, bool building, TextAsset example = null)
    {
        Name = name;
        Building = building;
        Example = example;
    }
}

public static class MenuBlueprintLibrary
{
    public static List<MenuBlueprintEntry> Read(bool building, TextAsset example)
    {
        var entries = new List<MenuBlueprintEntry>();
        if (example != null)
        {
            entries.Add(new MenuBlueprintEntry(building ? "Starter Bastion" : "Starter Finch", building, example));
        }

        entries.AddRange(Files(building).GetNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase).Select(n => new MenuBlueprintEntry(n, building)));
        return entries;
    }

    internal static BlueprintFileStore Files(bool building)
    {
        return building ? SaveManager.GetBaseBlueprintFiles() : SaveManager.GetPlayerBlueprintFiles();
    }

    public static BlockDataList Inspect(
        MenuBlueprintEntry entry,
        SiegeConstructionSettings siege,
        out bool playable,
        out string reason,
        bool forEditing = false)
    {
        string json = entry.IsExample ? entry.Example.text : File.ReadAllText(Files(entry.Building).GetPath(entry.Name));
        if (!(JToken.Parse(json) is JObject document) || !(document["blocks"] is JArray))
        {
            throw new InvalidDataException("Blueprint must contain a blocks array.");
        }

        BlockDataList data = JsonUtility.FromJson<BlockDataList>(json);
        if (data == null || data.blocks == null)
        {
            throw new InvalidDataException("Blueprint has no module list.");
        }

        // Workshop access must not depend on deployment limits or a finished design.
        if (forEditing)
        {
            playable = false;
            reason = "Open in the workshop to continue or repair this design.";
            return data;
        }

        if (entry.Building)
        {
            data.foundation = BuildingFootprint.Calculate(data.blocks, BuildManager.instance != null ? BuildManager.instance.FootprintPadding : 1);
            WorldBuildingValidation.ValidateBlueprint(data);
            playable = siege.Validate(data, out reason);
        }
        else
        {
            // The menu must reject missing Prefabs too: the loader otherwise skips them and can change a saved design.
            var identities = new HashSet<string>();
            if (data.blocks.Count > WorldBuildingValidation.MaximumBlocks)
            {
                throw new InvalidDataException("Blueprint exceeds the module limit.");
            }

            foreach (BlockData block in data.blocks)
            {
                if (block == null || string.IsNullOrEmpty(block.resourcePath) || BlockPrefabCache.GetPrefab(BlueprintBlockFactory.ToResourcePath(block.resourcePath)) == null)
                {
                    throw new InvalidDataException("Blueprint references a missing module.");
                }

                GameObject prefab = BlockPrefabCache.GetPrefab(BlueprintBlockFactory.ToResourcePath(block.resourcePath));
                if (prefab.GetComponent<Block>() == null || block.x < 1 || block.y < 1 || block.z < 1 || block.x > 64 || block.y > 64 || block.z > 64 || string.IsNullOrEmpty(block.id) || !identities.Add(block.id) || !WorldBuildingValidation.Finite(new Vector3(block.posX, block.posY, block.posZ)) || !WorldBuildingValidation.ValidRotation(new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW)))
                {
                    throw new InvalidDataException("Blueprint contains invalid module dimensions, identity or pose.");
                }
            }

            playable = data.blocks.Count > 0 && ModularUnitFactory.IsValidBlueprint(data, entry.Name);
            reason = playable ? "Ready for exploration." : "Exploration requires a valid vehicle with exactly one cockpit. You can edit this design.";
        }

        return data;
    }

    public static string CopyExample(MenuBlueprintEntry entry)
    {
        if (!entry.IsExample)
        {
            return entry.Name;
        }

        BlueprintFileStore files = Files(entry.Building);
        string name = entry.Name;
        for (int suffix = 2; File.Exists(files.GetPath(name)); suffix++)
        {
            name = entry.Name + " " + suffix;
        }

        using (var stream = new FileStream(files.GetPath(name), FileMode.CreateNew, FileAccess.Write))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(entry.Example.text);
            }
        }

        return name;
    }
}
