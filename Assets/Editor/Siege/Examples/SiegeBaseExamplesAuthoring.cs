// 职责：制作可独立编辑的官方基地示例，使用真实材质、仓储、无人机、电网和模块机库。
// 入口：Generate() / CreateDesign()；仅更新声明的官方资源，先备份，不改个人蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SiegeBaseExamplesAuthoring
{
    public static readonly string[] Paths =
    {
        "Assets/Siege/Construction/StarterHangar.json",
        "Assets/Siege/Construction/Examples/LogisticsRoof.json",
        "Assets/Siege/Construction/Examples/TwinFoundry.json",
        "Assets/Siege/Construction/Examples/MeridianAirworks.json"
    };
    public static readonly string[] Names =
    {
        "Starter Bastion",
        "Skyline Logistics",
        "Twin Foundry",
        "Meridian Airworks"
    };
    private const string SettingsPath = "Assets/Siege/Construction/Settings.asset";
    [MenuItem("HY-Sandbox/Siege/Examples/Generate Base Examples")]
    public static string Generate()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before authoring official bases.");
        }

        SiegeConstructionSettings settings = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>(SettingsPath);
        var designs = new BlockDataList[Paths.Length];
        for (int i = 0; i < designs.Length; i++)
        {
            designs[i] = CreateDesign(i);
            if (!settings.Validate(designs[i], out string reason))
            {
                throw new InvalidOperationException(Names[i] + ": " + reason);
            }
        }

        string backup = "Library/SiegeBaseExamples/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
        Directory.CreateDirectory(backup);
        var manifest = new List<string>();
        foreach (string path in Paths.Concat(new[] { SettingsPath }))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            File.Copy(path, backup + "/" + System.IO.Path.GetFileName(path));
            manifest.Add(path);
        }

        File.WriteAllLines(backup + "/original-paths.txt", manifest);
        for (int i = 0; i < designs.Length; i++)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Paths[i]));
            string json = JsonUtility.ToJson(designs[i], true);
            if (!File.Exists(Paths[i]) || File.ReadAllText(Paths[i]) != json)
            {
                File.WriteAllText(Paths[i], json);
                AssetDatabase.ImportAsset(Paths[i], ImportAssetOptions.ForceUpdate);
            }
        }

        var serialized = new SerializedObject(settings);
        SerializedProperty examples = serialized.FindProperty("examples");
        if (examples == null)
        {
            throw new InvalidOperationException("Settings requires the authored examples catalog.");
        }

        examples.arraySize = Paths.Length;
        for (int i = 0; i < Paths.Length; i++)
        {
            SerializedProperty item = examples.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("name").stringValue = Names[i];
            item.FindPropertyRelative("blueprint").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Paths[i]);
        }

        serialized.FindProperty("starter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Paths[0]);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "Official bases saved; originals backed up to " + backup;
    }

    public static BlockDataList CreateDesign(int index)
    {
        if (index < 0 || index >= Names.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var design = new BlockDataList
        {
            siegeCoreId = "example-" + index + "-core"
        };
        if (index == 3)
        {
            BuildAirworks(design);
            design.foundation = BuildingFootprint.Calculate(design.blocks, 2);
            return design;
        }

        if (index == 2)
        {
            Hangar(design, "west-bay", new Vector3(-13, 5, 2), HangarEntrance.Front);
            Hangar(design, "east-bay", new Vector3(13, 5, 2), HangarEntrance.Roof);
        }
        else
        {
            Hangar(design, "main-bay", new Vector3(0, 5, 2), index == 0 ? HangarEntrance.Front : HangarEntrance.Roof);
        }

        float flank = index == 2 ? 29f : 16f;
        float rear = -18f;
        Core(design, rear);
        foreach (float x in new[]
        {
            -flank,
            flank
        }

        )
        {
            foreach (float z in new[]
            {
                -8f,
                10f
            }

            )
            {
                Defense(design, x, z, index != 0);
            }

            Service(design, x, rear, index);
        }

        // The coloured annexes are usable grounded structures, not material overrides on functional parts.
        // Keeping them behind the bay preserves the entire front or roof clearance volume.
        // A 1m-thick upright plate sits on the half-cell depth of a 2m pillar,
        // so opposite connector positions meet instead of merely touching visually.
        for (int x = -6; x <= 6; x += 2)
        {
            Add(design, "rear-stone-" + x, "Blocks/2x2x2", new Vector3(x, 1, -23), "stone");
            Add(design, "rear-wood-" + x, "Blocks/2x2x2", new Vector3(x, 3, -23), "wood");
            Add(design, "rear-glass-" + x, "Blocks/2x2x1", new Vector3(x, 5, -23.5f), "glass");
            Add(design, "rear-metal-" + x, "Blocks/2x2x1", new Vector3(x, 7, -23.5f), "metal");
        }

        if (index == 1)
        {
            for (int x = -6; x <= 6; x += 2)
            {
                Add(design, "warehouse-" + x, "Blocks/CargoHold", new Vector3(x, 1, -28));
            }

            // The rear warehouse is outside both flank networks; give its holds actual
            // local supply rather than relying on decorative distant generators.
            Add(design, "warehouse-generator", "Blocks/PowerGeneratingUnit", new Vector3(0, 1, -31));
            Add(design, "warehouse-relay-west", "Blocks/PowerTransmissionDevice", new Vector3(-2.5f, .5f, -25.5f));
            Add(design, "warehouse-relay-east", "Blocks/PowerTransmissionDevice", new Vector3(2.5f, .5f, -25.5f));
        }

        design.foundation = BuildingFootprint.Calculate(design.blocks, 2);
        return design;
    }

    private static void BuildAirworks(BlockDataList design)
    {
        Core(design, 0);
        // Set back the glazed watch room above two open galleries. The core shares
        // their central axis while corner columns carry the upper floors.
        for (int level = 0; level < 3; level++)
        {
            int radius = level == 2 ? 4 : 6;
            int floor = 7 + level * 6;
            for (int x = -radius; x <= radius; x += 2)
            {
                for (int z = -radius; z <= radius; z += 2)
                {
                    string tag = level + "/" + x + "/" + z;
                    Add(design, "deck-" + tag, "Blocks/2x2x2", new Vector3(x, floor, z), level == 1 ? "wood" : "metal");
                    bool corner = Mathf.Abs(x) == radius && Mathf.Abs(z) == radius;
                    bool window = level == 2 && (Mathf.Abs(x) == radius || Mathf.Abs(z) == radius);
                    if (corner || window)
                    {
                        for (int y = floor - 4; y < floor; y += 2)
                        {
                            Add(design, "frame-" + tag + "/" + y, "Blocks/2x2x2", new Vector3(x, y, z), corner ? "stone" : "glass");
                        }

                        if (level == 0)
                        {
                            Add(design, "foot-" + tag, "Blocks/2x2x2", new Vector3(x, 1, z), "stone");
                        }
                    }
                }
            }
        }

        foreach (int side in new[] { -1, 1 })
        {
            int x = side * 22;
            BlockData large = Add(design, "heavy-bay-" + side, "Hangars/Hangar", new Vector3(x, 5, 14));
            large.x = 20;
            large.y = 10;
            large.z = 24;
            BlockData small = Add(design, "lift-bay-" + side, "Hangars/Hangar", new Vector3(x, 3, -18));
            small.x = 12;
            small.y = 6;
            small.z = 16;
            small.hangarEntrance = HangarEntrance.Roof;

            // Repeated loading docks serve the lane between each pair of hangars.
            for (int dock = 0; dock < 2; dock++)
            {
                int serviceX = side * (15 + 14 * dock);
                Service(design, serviceX, -3, 2);
                Add(design, "secondary-store-" + serviceX, "Blocks/CargoHold", new Vector3(serviceX + 4, 1, -7));
            }

            foreach (int z in new[] { -22, -2, 22 })
            {
                Defense(design, side * 39, z, z != -2);
            }

            // Low inset guide strips mark the apron edges without crossing a bay mouth.
            for (int z = 5; z <= 25; z += 4)
            {
                Add(design, "apron-" + side + "/" + z, "Blocks/2x1x1", new Vector3(side * 9, .5f, z + .5f), "stone");
            }
        }
    }

    private static void Hangar(BlockDataList design, string id, Vector3 position, HangarEntrance entrance)
    {
        BlockData bay = Add(design, id, "Hangars/Hangar", position);
        bay.hangarEntrance = entrance;
    }

    private static void Core(BlockDataList design, float z)
    {
        Add(design, "core-plinth", "Blocks/2x2x2", new Vector3(0, 1, z), "stone");
        Add(design, design.siegeCoreId, "Blocks/2x2x2", new Vector3(0, 3, z), "metal");
        Add(design, "core-cap", "Blocks/2x2x2", new Vector3(0, 5, z), "stone");
        foreach (int x in new[]
        {
            -2,
            2
        }

        )
        {
            for (int y = 1; y <= 5; y += 2)
            {
                Add(design, "core-shield-" + x + "-" + y, "Blocks/2x2x2", new Vector3(x, y, z), "metal");
            }
        }
    }

    private static void Defense(BlockDataList design, float x, float z, bool heavy)
    {
        string tag = x + "/" + z;
        Add(design, "gun-plinth-" + tag, "Blocks/2x2x2", new Vector3(x, 1, z), "stone");
        Add(
            design,
            "gun-" + tag,
            heavy ? "Blocks/ProjectileTurretDouble" : "Blocks/ProjectileTurretSingle",
            new Vector3(x + .5f, 2.5f, z + .5f));
        Add(design, "generator-" + tag, "Blocks/PowerGeneratingUnit", new Vector3(x, 1, z - 4));
        Add(design, "relay-" + tag, "Blocks/PowerTransmissionDevice", new Vector3(x + 2.5f, .5f, z - 1.5f));
    }

    private static void Service(BlockDataList design, float x, float z, int index)
    {
        string tag = x.ToString();
        Add(design, "service-generator-" + tag, "Blocks/PowerGeneratingUnit", new Vector3(x, 1, z));
        Add(
            design,
            "service-relay-" + tag,
            "Blocks/PowerTransmissionDevice",
            new Vector3(x + 1.5f, .5f, z + .5f));
        Add(design, "cargo-" + tag, "Blocks/CargoHold", new Vector3(x, 1, z - 4));
        Add(design, "coins-" + tag, "Blocks/CoinHold", new Vector3(x + 2.5f, .5f, z - 3.5f));
        Add(design, "technology-" + tag, "Blocks/TechnologyHold", new Vector3(x + 2.5f, .5f, z - 2.5f));
        Add(design, "repair-bot-" + tag, "Blocks/RepairBotContianer", new Vector3(x - 2.5f, .5f, z + 1.5f));
        Add(
            design,
            "collection-bot-" + tag,
            "Blocks/CollectionBotContainer",
            new Vector3(x + 2.5f, .5f, z + 2.5f));
        Add(design, "wood-store-" + tag, "Blocks/2x2x2", new Vector3(x - 3, 1, z - 4), "wood");
        Add(design, "glass-store-" + tag, "Blocks/2x2x1", new Vector3(x - 3, 3, z - 4.5f), "glass");
        if (index == 2)
        {
            Add(design, "reserve-generator-" + tag, "Blocks/PowerGeneratingUnit", new Vector3(x, 1, z + 4));
        }
    }

    private static BlockData Add(BlockDataList design, string id, string resource, Vector3 position, string material = null)
    {
        GameObject prefab = Resources.Load<GameObject>(resource);
        if (prefab == null || !prefab.TryGetComponent(out Block block))
        {
            throw new InvalidOperationException("Missing authored example module: " + resource);
        }

        var data = new BlockData(block)
        {
            id = id,
            resourcePath = resource,
            materialId = material,
            cargo = null,
            posX = position.x,
            posY = position.y,
            posZ = position.z,
            rotX = 0,
            rotY = 0,
            rotZ = 0,
            rotW = 1
        };
        design.blocks.Add(data);
        return data;
    }
}
