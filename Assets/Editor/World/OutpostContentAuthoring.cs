// 职责：制作三套普通建筑与三套敌机轮廓底座JSON，保留可重建的项目源。
// 入口：Bake()；只写项目Resources，不修改用户蓝图或存档；已有JSON保留，编辑后可单独删除重建。
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class OutpostContentAuthoring
{
    private const string Root = "Assets/Resources/WorldBlueprints";
    [MenuItem("Tools/HY-Sandbox/Outposts/Create missing authored content")]
    public static void Bake()
    {
        Bake(false);
    }

    [MenuItem("Tools/HY-Sandbox/Outposts/Rebuild six bundled templates")]
    public static void Rebuild()
    {
        Bake(true);
    }

    private static void Bake(bool overwrite)
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        Directory.CreateDirectory(Root + "/Buildings");
        Directory.CreateDirectory(Root + "/Enemies");
        string[] enemies =
        {
            "CODEX_E01_Finch",
            "CODEX_E02_Pike",
            "CODEX_E03_Manta"
        };
        foreach (string name in enemies)
        {
            string destination = Root + "/Enemies/" + name + ".json";
            if (!File.Exists(destination))
            {
                File.Copy("Blueprints/CODEX_Enemies/" + name + ".json", destination);
            }
        }

        Write("Field Workshop", Workshop(), overwrite);
        Write("Abandoned Storehouse", Storehouse(), overwrite);
        Write("Relay Ruins", Ruins(), overwrite);
        foreach (string enemy in enemies)
        {
            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(Root + "/Enemies/" + enemy + ".json"));
            Write(enemy + " Outpost", OutpostBaseDesigner.Create(blueprint, enemy), overwrite);
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/HY-Sandbox/Outposts/Export matching bases to BuildingBlueprints")]
    public static void ExportMatchingBases()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        string directory = Path.Combine(Application.persistentDataPath, "BuildingBlueprints");
        Directory.CreateDirectory(directory);
        foreach (TextAsset enemy in Resources.LoadAll<TextAsset>("WorldBlueprints/Enemies"))
        {
            string path = Path.Combine(directory, enemy.name + " Outpost.json");
            if (File.Exists(path))
            {
                continue;
            }

            BlockDataList data = OutpostBaseDesigner.Create(JsonUtility.FromJson<BlockDataList>(enemy.text), enemy.name);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(JsonUtility.ToJson(data, true));
                }
            }
        }
    }

    private static BlockDataList Base(int width, int length)
    {
        var data = new BlockDataList
        {
            foundation = new BuildingFoundationData
            {
                width = width,
                length = length
            }
        };
        Add(
            data,
            "1x1x1",
            new Vector3(width * 0.5f - 2f, 0.5f, length * 0.5f - 2f),
            width,
            1,
            length);
        return data;
    }

    private static BlockDataList Workshop()
    {
        BlockDataList data = Base(10, 8);
        Add(
            data,
            "1x1x1",
            new Vector3(-1.5f, 2.5f, 2f),
            1,
            3,
            8);
        Add(
            data,
            "1x1x1",
            new Vector3(3f, 2.5f, 5.5f),
            8,
            3,
            1);
        Add(
            data,
            "1x1x1",
            new Vector3(6.5f, 2.5f, 2f),
            1,
            3,
            8);
        Add(
            data,
            "1x1x1",
            new Vector3(3f, 4.5f, 2f),
            10,
            1,
            8);
        Add(data, "PowerGeneratingUnit", new Vector3(0f, 2f, 3f));
        Loot(data, "CargoHold", new Vector3(4f, 2f, 3f), CargoKind.SpecialPart, 6);
        Add(data, "Rack", new Vector3(3f, 1.5f, -0.5f));
        return data;
    }

    private static BlockDataList Storehouse()
    {
        BlockDataList data = Base(12, 10);
        for (int i = 0; i < 3; i++)
        {
            Add(
                data,
                "1x1x1",
                new Vector3(-1.5f + i * 5f, 3f, 2.5f),
                1,
                4,
                9);
        }

        Add(
            data,
            "1x1x1",
            new Vector3(3.5f, 5.5f, 2.5f),
            12,
            1,
            10);
        Loot(data, "CoinHold", new Vector3(0f, 2f, 5f), CargoKind.Coins, 75);
        Loot(data, "CargoHold", new Vector3(5f, 2f, 5f), CargoKind.SpecialPart, 8);
        Add(data, "Door", new Vector3(4f, 2f, -1f));
        return data;
    }

    private static BlockDataList Ruins()
    {
        BlockDataList data = Base(8, 8);
        Add(
            data,
            "1x1x1",
            new Vector3(-1f, 2.5f, 3f),
            2,
            3,
            5);
        Add(
            data,
            "1x1x1",
            new Vector3(3f, 1.5f, 5f),
            4,
            1,
            2);
        Add(
            data,
            "1x1x1",
            new Vector3(2f, 5f, 2f),
            2,
            8,
            2);
        Add(data, "PowerTransmissionDevice", new Vector3(2f, 9.5f, 2f));
        Loot(data, "TechnologyHold", new Vector3(4f, 2f, 0f), CargoKind.Technology, 12);
        return data;
    }

    private static void Loot(BlockDataList data, string resource, Vector3 position, CargoKind kind, int amount)
    {
        position.y = 1f + Resources.Load<GameObject>("Blocks/" + resource).GetComponent<Block>().y * 0.5f;
        Add(data, resource, position);
        data.blocks[data.blocks.Count - 1].cargo = new System.Collections.Generic.List<CargoItem>
        {
            new CargoItem
            {
                kind = kind,
                amount = amount,
                resourcePath = kind == CargoKind.SpecialPart ? "Blocks/1x1x1" : null
            }
        };
    }

    private static void Add(
        BlockDataList data,
        string resource,
        Vector3 position,
        int x = 0,
        int y = 0,
        int z = 0)
    {
        Block prefab = Resources.Load<GameObject>("Blocks/" + resource).GetComponent<Block>();
        int sx = x > 0 ? x : prefab.x;
        int sy = y > 0 ? y : prefab.y;
        int sz = z > 0 ? z : prefab.z;
        string path = resource == "1x1x1" && x > 0 ? CreateStructuralPart(sx, sy, sz) : "Blocks/" + resource;
        position = SnapPosition(position, sx, sy, sz);
        data.blocks.Add(new BlockData(prefab) { id = "part-" + data.blocks.Count, resourcePath = path, posX = position.x, posY = position.y, posZ = position.z, rotX = 0, rotY = 0, rotZ = 0, rotW = 1, x = sx, y = sy, z = sz });
    }

    private static Vector3 SnapPosition(Vector3 position, int x, int y, int z)
    {
        Vector3 offset = new Vector3((x & 1) * 0.5f, (y & 1) * 0.5f, (z & 1) * 0.5f);
        return new Vector3(
            Mathf.Round(position.x - offset.x),
            Mathf.Round(position.y - offset.y),
            Mathf.Round(position.z - offset.z)) + offset;
    }

    [MenuItem("Tools/HY-Sandbox/Outposts/Upgrade authored structural geometry")]
    public static void UpgradeGeometry()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        foreach (string path in Directory.GetFiles(Root + "/Buildings", "*.json"))
        {
            BlockDataList data = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            bool changed = false;
            foreach (BlockData block in data.blocks)
            {
                if (block.resourcePath == "Blocks/1x1x1" && (block.x > 1 || block.y > 1 || block.z > 1))
                {
                    block.resourcePath = CreateStructuralPart(block.x, block.y, block.z);
                    changed = true;
                }
            }

            if (!changed)
            {
                continue;
            }

            foreach (BlockData block in data.blocks)
            {
                Vector3 position = SnapPosition(new Vector3(block.posX, block.posY, block.posZ), block.x, block.y, block.z);
                if (block.resourcePath.Contains("Turret"))
                {
                    position.y -= 1f;
                }

                block.posX = position.x;
                block.posY = position.y;
                block.posZ = position.z;
            }

            WorldBuildingValidation.ValidateBlueprint(data);
            File.WriteAllText(path, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
        }

        AssetDatabase.Refresh();
    }

    private static string CreateStructuralPart(int x, int y, int z)
    {
        string resource = "WorldBlueprints/Parts/Structure_" + x + "x" + y + "x" + z;
        string path = "Assets/Resources/" + resource + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return resource;
        }

        Directory.CreateDirectory(Root + "/Parts");
        GameObject root = PrefabUtility.LoadPrefabContents("Assets/Resources/Blocks/1x1x1.prefab");
        try
        {
            BlockCsvBinding binding = root.GetComponent<BlockCsvBinding>();
            if (binding != null)
            {
                UnityEngine.Object.DestroyImmediate(binding);
            }

            Block block = root.GetComponent<Block>();
            block.x = x;
            block.y = y;
            block.z = z;
            block.canSpawnConnector = true;
            block.resourcePath = resource;
            block.SetMassSettings(false, 0f);
            Transform model = root.transform.Find("Model");
            model.localScale = Vector3.Scale(model.localScale, new Vector3(x, y, z));
            root.GetComponent<Durability>().maxDurability = 100f * Mathf.Pow(x * y * z, 0.65f);
            block.RefreshAuthoringData();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return resource;
    }

    private static void Write(string name, BlockDataList data, bool overwrite)
    {
        WorldBuildingValidation.ValidateBlueprint(data);
        string path = Root + "/Buildings/" + name + ".json";
        if (overwrite || !File.Exists(path))
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
        }
    }
}
