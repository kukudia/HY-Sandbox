// 职责：以可复现配方重做官方机库示例；仅替换 StarterHangar 资产并保留带时间戳备份。
// 入口：Generate()；不清空个人基地库，核心ID与官方菜单引用保持稳定。
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ModularBaseExampleAuthoring
{
    private const string Path = "Assets/Siege/Construction/StarterHangar.json";
    [MenuItem("HY-Sandbox/Siege/Hangars/Rebuild Modular Base Example")]
    public static string Generate()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before rebuilding the official example.");
        }

        var design = new BlockDataList
        {
            siegeCoreId = "bastion-core"
        };
        // One full-size bay, an independent core tower and four low gun platforms.
        Add(design, "example-hangar", "Hangars/Hangar", new Vector3(0, 5, 2));
        for (int y = 1; y <= 5; y += 2)
        {
            Add(design, "core-support-" + y, "Blocks/2x2x2", new Vector3(0, y, -16), "stone");
        }

        Add(design, design.siegeCoreId, "Blocks/2x2x2", new Vector3(0, 7, -16), "metal");
        foreach (int x in new[]
        {
            -15,
            15
        }

        )
        {
            foreach (int z in new[]
            {
                -9,
                11
            }

            )
            {
                string id = x + "/" + z;
                Add(design, "gun-support-" + id, "Blocks/2x2x2", new Vector3(x, 1, z), "stone");
                Add(design, "gun-" + id, "Blocks/ProjectileTurretSingle", new Vector3(x + .5f, 2.5f, z + .5f));
                Add(design, "generator-" + id, "Blocks/PowerGeneratingUnit", new Vector3(x, 1, z - 4));
                Add(design, "relay-" + id, "Blocks/PowerTransmissionDevice", new Vector3(x + 3.5f, .5f, z - 2.5f));
            }
        }

        design.foundation = BuildingFootprint.Calculate(design.blocks, 2);
        SiegeConstructionSettings settings = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>("Assets/Siege/Construction/Settings.asset");
        if (!settings.Validate(design, out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        string backup = "Library/ModularHangar/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
        Directory.CreateDirectory(backup);
        if (File.Exists(Path))
        {
            File.Copy(Path, backup + "/StarterHangar.json");
        }

        File.WriteAllText(Path, JsonUtility.ToJson(design, true));
        AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
        return reason;
    }

    private static void Add(BlockDataList design, string id, string resource, Vector3 position, string material = null)
    {
        Block block = Resources.Load<GameObject>(resource).GetComponent<Block>();
        design.blocks.Add(new BlockData(block) { id = id, resourcePath = resource, materialId = material, posX = position.x, posY = position.y, posZ = position.z, rotX = 0, rotY = 0, rotZ = 0, rotW = 1 });
    }
}
