// 职责：制作独立的交错防线实战基地蓝图，保留示例基地与用户设计。
// 入口：Generate()；固定配方可重复预览，输出真实JSON资产，个人副本通过正常菜单出战。
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SiegeCombatBaseAuthoring
{
    public const string BlueprintPath = "Assets/Siege/Construction/CombatCourtyard.json";
    public const string CoreId = "courtyard-core";
    [MenuItem("Tools/HY-Sandbox/Siege/Generate combat courtyard base")]
    public static void Create()
    {
        Generate();
    }

    public static TextAsset Generate()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Generate the design outside Play Mode.");
        }

        // 再次调用只读取现有作品，避免覆盖玩家后续手工调整。
        if (File.Exists(BlueprintPath))
        {
            return AssetDatabase.LoadAssetAtPath<TextAsset>(BlueprintPath);
        }

        var design = new BlockDataList
        {
            siegeCoreId = CoreId
        };
        for (int x = -48; x <= 48; x += 8)
        {
            for (int z = -40; z <= 40; z += 8)
            {
                // 机库通过底部连接直接锚定虚拟地面，不能悬在装甲平台上。
                if (Mathf.Abs(x) >= 8 && Mathf.Abs(x) <= 40 && z >= -24 && z <= 0)
                {
                    continue;
                }

                Armor(design, new Vector3(x, 1f, z), new Vector3Int(8, 2, 8));
            }
        }

        // 两个机库门朝南，外部通道不设低墙，保留完整出入库净空。
        foreach (int x in new[]
        {
            -24,
            24
        }

        )
        {
            Add(design, "Hangars/Hangar", new Vector3(x, 6f, -12f), new Vector3Int(28, 12, 30), 180f);
        }

        foreach (int x in new[]
        {
            -40,
            40
        }

        )
        {
            foreach (int z in new[]
            {
                -32,
                32
            }

            )
            {
                Armor(design, new Vector3(x, 7f, z), new Vector3Int(8, 10, 8));
                Add(design, "Blocks/ProjectileTurretSingle", new Vector3(x + 2.5f, 12.5f, z + 2.5f));
                Add(design, "Blocks/PowerGeneratingUnit", new Vector3(x - 2, 13f, z));
                Add(design, "Blocks/PowerTransmissionDevice", new Vector3(x + .5f, 12.5f, z - 2.5f));
            }
        }

        // 分段外墙留下东西各24米、北面20米开口，内侧错位挡墙制造多种接近路线。
        foreach (int x in new[]
        {
            -50,
            50
        }

        )
        {
            foreach (int z in new[]
            {
                -27,
                27
            }

            )
            {
                Armor(design, new Vector3(x, 8f, z), new Vector3Int(4, 12, 30));
            }
        }

        foreach (int x in new[]
        {
            -31,
            31
        }

        )
        {
            Armor(design, new Vector3(x, 8f, 42f), new Vector3Int(42, 12, 4));
        }

        Armor(design, new Vector3(-16f, 9f, 10f), new Vector3Int(4, 14, 20));
        Armor(design, new Vector3(16f, 7f, 21f), new Vector3Int(4, 10, 18));
        // 核心舱为二层受保护空间；顶板由侧墙承重，不依赖发电机禁用的顶连接面。
        Armor(design, new Vector3(0f, 5f, 12f), new Vector3Int(16, 6, 16));
        Add(
            design,
            "Blocks/PowerGeneratingUnit",
            new Vector3(0f, 9f, 12f),
            null,
            0f,
            CoreId);
        foreach (int x in new[]
        {
            -7,
            7
        }

        )
        {
            Armor(design, new Vector3(x, 11f, 12f), new Vector3Int(2, 6, 16));
        }

        foreach (int z in new[]
        {
            5,
            19
        }

        )
        {
            Armor(design, new Vector3(0f, 11f, z), new Vector3Int(12, 6, 2));
        }

        Armor(design, new Vector3(0f, 15f, 12f), new Vector3Int(18, 2, 18));
        Add(design, "Blocks/PowerGeneratingUnit", new Vector3(0f, 17f, 12f));
        foreach (int x in new[]
        {
            -5,
            5
        }

        )
        {
            Add(design, "Blocks/ProjectileTurretSingle", new Vector3(x + .5f, 16.5f, 12.5f));
            Add(design, "Blocks/PowerTransmissionDevice", new Vector3(x + .5f, 16.5f, 14.5f));
        }

        design.foundation = BuildingFootprint.Calculate(design.blocks, 1);
        SiegeConstructionSettings settings = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>(SiegeConstructionAuthoring.SettingsPath);
        if (!settings.Validate(design, out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        using (var stream = new FileStream(BlueprintPath, FileMode.CreateNew, FileAccess.Write))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonUtility.ToJson(design, true));
            }
        }

        AssetDatabase.ImportAsset(BlueprintPath);
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(BlueprintPath);
        Selection.activeObject = asset;
        Debug.Log($"Combat courtyard saved: {design.blocks.Count} modules. {reason}");
        return asset;
    }

    private static void Armor(BlockDataList design, Vector3 position, Vector3Int size)
    {
        string resource = $"Blocks/CombatStructures/Armor{size.x}x{size.y}x{size.z}";
        string path = "Assets/Resources/" + resource + ".prefab";
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Blocks/CombatStructures"))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Blocks", "CombatStructures");
        }

        if (!File.Exists(path))
        {
            GameObject source = PrefabUtility.LoadPrefabContents("Assets/Resources/Blocks/2x2x2.prefab");
            try
            {
                // 原装甲是固定尺寸的CSV构件；派生结构件独立烘焙，不能继续绑定原2米规格。
                UnityEngine.Object.DestroyImmediate(source.GetComponent<BlockCsvBinding>());
                Block block = source.GetComponent<Block>();
                block.x = size.x;
                block.y = size.y;
                block.z = size.z;
                block.canSpawnConnector = true;
                block.resourcePath = resource;
                block.SetMassSettings(false, 0f);
                block.RefreshAuthoringData();
                source.transform.Find("Model").localScale = new Vector3(size.x, size.y, size.z) * .5f;
                source.name = $"Armor{size.x}x{size.y}x{size.z}";
                PrefabUtility.SaveAsPrefabAsset(source, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(source);
            }
        }

        Add(design, resource, position, size);
    }

    private static void Add(
        BlockDataList design,
        string resource,
        Vector3 position,
        Vector3Int? size = null,
        float yaw = 0f,
        string id = null)
    {
        Block template = Resources.Load<GameObject>(resource).GetComponent<Block>();
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        var data = new BlockData(template)
        {
            id = id ?? "courtyard-" + design.blocks.Count,
            resourcePath = resource,
            posX = position.x,
            posY = position.y,
            posZ = position.z,
            rotX = rotation.x,
            rotY = rotation.y,
            rotZ = rotation.z,
            rotW = rotation.w,
            cargo = null
        };
        if (size.HasValue)
        {
            data.x = size.Value.x;
            data.y = size.Value.y;
            data.z = size.Value.z;
        }

        design.blocks.Add(data);
    }
}
