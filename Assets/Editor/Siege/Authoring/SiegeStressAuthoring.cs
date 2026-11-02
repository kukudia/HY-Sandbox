// 职责：从阵地战场景制作六舰/2048模块建筑压力资产，隔离测试经济与正常游戏参数。
// 入口：Create() / CreateLarge()；首次创建不覆盖既有资产，保存后恢复原活动场景。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SiegeStressAuthoring
{
    public const string ScenePath = "Assets/Scenes/SiegeStress.unity";
    public const string SettingsPath = "Assets/Siege/SiegeStress.asset";
    public const string LargeScenePath = "Assets/Scenes/SiegeLargeStress.unity";
    public const string LargeSettingsPath = "Assets/Siege/SiegeLargeStress.asset";
    public const string LargeBlueprintPath = "Assets/Siege/BastionBase2048.json";
    [MenuItem("HY-Sandbox/Siege/Create Editable Stress Profile")]
    public static void Create()
    {
        CreateProfile(ScenePath, SettingsPath, SiegeSceneAuthoring.ScenePath, false);
    }

    [MenuItem("HY-Sandbox/Siege/Create Editable 2048-Module Stress Profile")]
    public static void CreateLarge()
    {
        Create();
        CreateProfile(LargeScenePath, LargeSettingsPath, ScenePath, true);
    }

    private static void CreateProfile(string scenePath, string settingsPath, string sourcePath, bool large)
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Save the current scene and leave Play before creating a stress profile.");
        }

        if (File.Exists(scenePath))
        {
            if (AssetDatabase.LoadAssetAtPath<SiegeSettings>(settingsPath) == null)
            {
                throw new InvalidOperationException("Existing stress scene has no profile asset; inspect its configuration before rebuilding.");
            }

            Debug.Log("Existing stress scene/profile preserved: " + scenePath);
            return;
        }

        Scene previous = SceneManager.GetActiveScene();
        Scene source = SceneManager.GetSceneByPath(sourcePath);
        bool openedSource = !source.IsValid() || !source.isLoaded;
        if (openedSource)
        {
            source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
        }

        if (source.isDirty)
        {
            throw new InvalidOperationException("Save the source Siege scene before copying its authored content.");
        }

        Scene target = default;
        try
        {
            SiegeSession sourceSession = FindSession(source);
            SiegeSettings settings = AssetDatabase.LoadAssetAtPath<SiegeSettings>(settingsPath);
            if (settings == null)
            {
                settings = UnityEngine.Object.Instantiate(sourceSession.Settings);
                settings.name = large ? "SiegeLargeStress" : "SiegeStress";
                var config = new SerializedObject(settings);
                config.FindProperty("startingCoins").intValue = 100000;
                config.FindProperty("firstWaveSize").intValue = 6;
                config.FindProperty("waveGrowth").intValue = 2;
                config.FindProperty("maximumActiveEnemies").intValue = 6;
                config.FindProperty("spawnInterval").floatValue = 0.5f;
                if (large)
                {
                    config.FindProperty("baseBlueprint").objectReferenceValue = GenerateLargeBlueprint(sourceSession.Settings.BaseBlueprint);
                }

                config.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            if (!EditorSceneManager.SaveScene(source, scenePath, true))
            {
                throw new IOException("Could not save the stress scene copy.");
            }

            target = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(target);
            var sessionConfig = new SerializedObject(FindSession(target));
            sessionConfig.FindProperty("settings").objectReferenceValue = settings;
            sessionConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(target);
            if (!EditorSceneManager.SaveScene(target))
            {
                throw new IOException("Could not save the stress profile reference.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Saved " + scenePath + ": 100000 test coins, first wave6, active cap6, spawn interval0.5s.");
        }
        finally
        {
            if (target.IsValid() && target.isLoaded)
            {
                EditorSceneManager.CloseScene(target, true);
            }

            if (openedSource && source.IsValid() && source.isLoaded)
            {
                EditorSceneManager.CloseScene(source, true);
            }

            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
        }
    }

    private static TextAsset GenerateLargeBlueprint(TextAsset source)
    {
        if (!File.Exists(LargeBlueprintPath))
        {
            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(source.text);
            BlockData[] armor = blueprint.blocks.Where(block => block.resourcePath == "Blocks/2x2x2").Take(237).ToArray();
            if (blueprint.blocks.Count != 388 || armor.Length != 237 || armor.Any(block => block.x != 2 || block.y != 2 || block.z != 2 || block.rotX != 0f || block.rotY != 0f || block.rotZ != 0f || block.rotW != 1f))
            {
                throw new InvalidOperationException("The large stress recipe requires the authored 388-module Bastion. Existing content was not replaced.");
            }

            Block cube = Resources.Load<GameObject>("Blocks/1x1x1").GetComponent<Block>();
            foreach (BlockData original in armor)
            {
                blueprint.blocks.Remove(original);
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            blueprint.blocks.Add(new BlockData(cube) { id = original.id + "-cell-" + x + y + z, resourcePath = "Blocks/1x1x1", posX = original.posX + x - 0.5f, posY = original.posY + y - 0.5f, posZ = original.posZ + z - 0.5f, rotW = 1f, cargo = null });
                        }
                    }
                }
            }

            // 237组细分使388变为2047；独立顶层测试块有真实地板支撑，不改变核心或炮塔。
            blueprint.blocks.Add(new BlockData(cube) { id = "stress-supported-cap", resourcePath = "Blocks/1x1x1", posX = 7.5f, posY = 2.5f, posZ = 7.5f, rotW = 1f, cargo = null });
            WorldBuildingValidation.ValidateBlueprint(blueprint);
            if (blueprint.blocks.Count != WorldBuildingValidation.MaximumBlocks)
            {
                throw new InvalidOperationException("Large stress recipe did not reach the current building limit.");
            }

            File.WriteAllText(LargeBlueprintPath, JsonUtility.ToJson(blueprint, true));
            AssetDatabase.ImportAsset(LargeBlueprintPath);
        }

        return AssetDatabase.LoadAssetAtPath<TextAsset>(LargeBlueprintPath);
    }

    private static SiegeSession FindSession(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            SiegeSession session = root.GetComponentInChildren<SiegeSession>(true);
            if (session != null)
            {
                return session;
            }
        }

        throw new InvalidOperationException("The source scene has no SiegeSession.");
    }
}
