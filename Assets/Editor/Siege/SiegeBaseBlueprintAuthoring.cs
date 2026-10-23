// 职责：制作32×32m建筑蓝图基地，并通过Editor迁移已有Siege场景的建筑管理器/核心配置。
// 入口：UpgradeScene() / Generate()；写入真实资产，保留已有GUID，不修改Main或用户建筑目录。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SiegeBaseBlueprintAuthoring
{
    public const string CoreId = "bastion-core";
    public const string BlueprintPath = "Assets/Siege/BastionBase.json";
    [MenuItem("HY-Sandbox/Siege/Upgrade Base To Building Blueprint")]
    public static void UpgradeScene()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Exit Play and save the current scene before upgrading the base.");
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != SiegeSceneAuthoring.ScenePath)
        {
            EditorSceneManager.OpenScene(SiegeSceneAuthoring.ScenePath);
        }

        SiegeSession session = UnityEngine.Object.FindFirstObjectByType<SiegeSession>();
        var serialized = new SerializedObject(session);
        var settings = (SiegeSettings)serialized.FindProperty("settings").objectReferenceValue;
        WorldBuildingManager buildings = session.GetComponentInChildren<WorldBuildingManager>();
        if (buildings == null)
        {
            var owner = new GameObject("Base Building Manager");
            owner.transform.SetParent(session.transform, false);
            buildings = Undo.AddComponent<WorldBuildingManager>(owner);
        }

        ConfigureManager(buildings);
        serialized.FindProperty("buildings").objectReferenceValue = buildings;
        var anchor = (Transform)serialized.FindProperty("baseDock").objectReferenceValue;
        Undo.RecordObject(anchor, "Place building foundation");
        anchor.position = new Vector3(-14f, 0f, -14f);
        serialized.ApplyModifiedProperties();
        var config = new SerializedObject(settings);
        config.FindProperty("baseBlueprint").objectReferenceValue = Generate(true);
        config.FindProperty("baseCoreBlockId").stringValue = CoreId;
        config.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
        EditorSceneManager.SaveScene(session.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Siege upgraded to a real large building blueprint; existing ship bays and UI preserved.");
    }

    internal static void ConfigureManager(WorldBuildingManager buildings)
    {
        var serialized = new SerializedObject(buildings);
        serialized.FindProperty("initializeOnStart").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static TextAsset Generate(bool replace = false)
    {
        if (replace || !File.Exists(BlueprintPath))
        {
            if (File.Exists(BlueprintPath))
            {
                Directory.CreateDirectory("Temp/Siege");
                File.Copy(
                    BlueprintPath,
                    "Temp/Siege/BastionBase-before-building-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".json");
            }

            var blueprint = new BlockDataList
            {
                foundation = new BuildingFoundationData
                {
                    width = 32,
                    length = 32
                }
            };
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    Add(blueprint, "2x2x2", new Vector3(x * 2 - 1, 1f, z * 2 - 1));
                    bool edge = x == 0 || x == 15 || z == 0 || z == 15;
                    bool gate = z == 0 && x >= 6 && x <= 9;
                    if (edge && !gate)
                    {
                        Add(blueprint, "2x2x2", new Vector3(x * 2 - 1, 3f, z * 2 - 1));
                        Add(blueprint, "2x2x2", new Vector3(x * 2 - 1, 5f, z * 2 - 1));
                    }
                }
            }

            Add(blueprint, "PowerGeneratingUnit", new Vector3(15f, 3f, 15f), CoreId);
            foreach (Vector3 armor in new[]
            {
                Vector3.left,
                Vector3.right,
                Vector3.forward,
                Vector3.back,
                Vector3.up
            }

            )
            {
                Add(blueprint, "2x2x2", new Vector3(15f, 3f, 15f) + armor * 2f);
            }

            // 发电机顶面不允许连接；顶盖从两侧护墙获得支撑，不能悬挂在禁用连接面上。
            Add(blueprint, "2x2x2", new Vector3(13f, 5f, 15f));
            Add(blueprint, "2x2x2", new Vector3(17f, 5f, 15f));
            foreach (Vector2 corner in new[]
            {
                new Vector2(5f, 5f),
                new Vector2(23f, 5f),
                new Vector2(5f, 23f),
                new Vector2(23f, 23f)
            }

            )
            {
                Add(blueprint, "PowerGeneratingUnit", new Vector3(corner.x, 3f, corner.y));
            }

            foreach (Vector2 tower in new[]
            {
                new Vector2(-0.5f, -0.5f),
                new Vector2(28.5f, -0.5f),
                new Vector2(-0.5f, 28.5f),
                new Vector2(28.5f, 28.5f)
            }

            )
            {
                Add(blueprint, "ProjectileTurretSingle", new Vector3(tower.x, 6.5f, tower.y));
                Add(
                    blueprint,
                    "PowerTransmissionDevice",
                    new Vector3(tower.x < 0 ? 3.5f : 24.5f, 2.5f, tower.y < 0 ? 3.5f : 24.5f));
            }

            WorldBuildingValidation.ValidateBlueprint(blueprint);
            File.WriteAllText(BlueprintPath, JsonUtility.ToJson(blueprint, true));
            AssetDatabase.ImportAsset(BlueprintPath);
        }

        return AssetDatabase.LoadAssetAtPath<TextAsset>(BlueprintPath);
    }

    private static void Add(BlockDataList blueprint, string resource, Vector3 position, string id = null)
    {
        Block prefab = Resources.Load<GameObject>("Blocks/" + resource).GetComponent<Block>();
        blueprint.blocks.Add(new BlockData(prefab) { id = id ?? "base-" + blueprint.blocks.Count, posX = position.x, posY = position.y, posZ = position.z, rotX = 0f, rotY = 0f, rotZ = 0f, rotW = 1f, resourcePath = "Blocks/" + resource, cargo = null });
    }
}
