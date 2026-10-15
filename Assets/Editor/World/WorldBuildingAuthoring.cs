// 职责：为独立 WorldLab 安装建筑库、示例模板和场景组合；已有模板与布局不覆盖。
// 入口：Tools/HY-Sandbox/World/Install building integration；只保存 WorldLab 与 Assets/WorldLab/Buildings。
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorldBuildingAuthoring
{
    private const string Root = "Assets/WorldLab/Buildings";
    [MenuItem("Tools/HY-Sandbox/World/Install building integration")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before installing building assets.");
        }

        var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (current.isDirty)
        {
            throw new InvalidOperationException("Save the active scene before installing WorldLab buildings.");
        }

        Directory.CreateDirectory(Root);
        string samplePath = Root + "/Outpost.json";
        if (!File.Exists(samplePath))
        {
            File.WriteAllText(samplePath, JsonUtility.ToJson(CreateSample(), true), new System.Text.UTF8Encoding(false));
        }

        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(WorldLabAuthoring.ScenePath);
        ProceduralVoxelWorld world = UnityEngine.Object.FindFirstObjectByType<ProceduralVoxelWorld>();
        Camera camera = UnityEngine.Object.FindFirstObjectByType<WorldLabController>().GetComponent<Camera>();
        string catalogPath = Root + "/Catalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<WorldBuildingCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<WorldBuildingCatalog>();
            TextAsset sample = AssetDatabase.LoadAssetAtPath<TextAsset>(samplePath);
            catalog.blueprints = new[]
            {
                sample
            };
            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(sample.text);
            Vector3 initial = FindFlatPosition(world, blueprint);
            catalog.initialBuildings = new[]
            {
                new WorldBuildingCatalog.InitialBuilding
                {
                    blueprint = sample,
                    worldLocalPosition = initial,
                    faction = UnitFaction.Enemy
                }
            };
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        WorldBuildingManager manager = UnityEngine.Object.FindFirstObjectByType<WorldBuildingManager>();
        if (manager == null)
        {
            manager = new GameObject("WorldLab Buildings").AddComponent<WorldBuildingManager>();
        }

        var settings = new SerializedObject(manager);
        settings.FindProperty("world").objectReferenceValue = world;
        if (settings.FindProperty("catalog").objectReferenceValue == null)
        {
            settings.FindProperty("catalog").objectReferenceValue = catalog;
        }

        settings.ApplyModifiedPropertiesWithoutUndo();
        WorldBuildingPalette palette = camera.GetComponent<WorldBuildingPalette>();
        if (palette == null)
        {
            palette = camera.gameObject.AddComponent<WorldBuildingPalette>();
        }

        var paletteSettings = new SerializedObject(palette);
        paletteSettings.FindProperty("manager").objectReferenceValue = manager;
        paletteSettings.ApplyModifiedPropertiesWithoutUndo();
        var controller = new SerializedObject(camera.GetComponent<WorldLabController>());
        controller.FindProperty("buildings").objectReferenceValue = manager;
        controller.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
        EditorSceneManager.SaveScene(world.gameObject.scene);
        AssetDatabase.SaveAssets();
        Selection.activeObject = manager;
    }

    public static BlockDataList CreateSample()
    {
        var blueprint = new BlockDataList
        {
            foundation = new BuildingFoundationData()
        };
        Add(blueprint, "PowerGeneratingUnit", new Vector3(-1f, 1f, -1f));
        Add(blueprint, "CargoHold", new Vector3(1f, 1f, 1f));
        Add(blueprint, "PowerTransmissionDevice", new Vector3(0.5f, 0.5f, -1.5f));
        Add(blueprint, "1x1x1", new Vector3(-0.5f, 0.5f, 0.5f));
        Add(blueprint, "Turret", new Vector3(-0.5f, 2.5f, -0.5f));
        return blueprint;
    }

    private static void Add(BlockDataList blueprint, string resource, Vector3 position)
    {
        Block prefab = Resources.Load<GameObject>("Blocks/" + resource).GetComponent<Block>();
        blueprint.blocks.Add(new BlockData(prefab) { id = "part-" + blueprint.blocks.Count, resourcePath = "Assets/Resources/Blocks/" + resource + ".prefab", posX = position.x, posY = position.y, posZ = position.z, rotX = 0f, rotY = 0f, rotZ = 0f, rotW = 1f });
    }

    private static Vector3 FindFlatPosition(ProceduralVoxelWorld world, BlockDataList blueprint)
    {
        for (int z = 8; z < world.Data.Size.z - 4; z += 2)
        {
            for (int x = 8; x < world.Data.Size.x - 4; x += 2)
            {
                for (int y = world.Data.Size.y - 2; y > 0; y--)
                {
                    Vector3 local = new Vector3(x, y, z) * world.CellSize;
                    if (WorldBuildingPlacement.Check(world, blueprint, world.transform.TransformPoint(local), world.transform.rotation, out _))
                    {
                        return local;
                    }
                }
            }
        }

        throw new InvalidOperationException("No flat position for sample outpost; configure a layout manually.");
    }
}
