// 职责：从真实阵地战场景复制可编辑六舰压力夹具，隔离测试经济与正常游戏参数。
// 入口：Create()；仅首次创建，已有场景/配置不覆盖，保存后恢复原活动场景。
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SiegeStressAuthoring
{
    public const string ScenePath = "Assets/Scenes/SiegeStress.unity";
    public const string SettingsPath = "Assets/Siege/SiegeStress.asset";
    [MenuItem("HY-Sandbox/Siege/Create Editable Stress Profile")]
    public static void Create()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Save the current scene and leave Play before creating a stress profile.");
        }

        if (File.Exists(ScenePath))
        {
            if (AssetDatabase.LoadAssetAtPath<SiegeSettings>(SettingsPath) == null)
            {
                throw new InvalidOperationException("Existing stress scene has no profile asset; inspect its configuration before rebuilding.");
            }

            Debug.Log("Existing SiegeStress scene/profile preserved.");
            return;
        }

        Scene previous = SceneManager.GetActiveScene();
        Scene source = SceneManager.GetSceneByPath(SiegeSceneAuthoring.ScenePath);
        bool openedSource = !source.IsValid() || !source.isLoaded;
        if (openedSource)
        {
            source = EditorSceneManager.OpenScene(SiegeSceneAuthoring.ScenePath, OpenSceneMode.Additive);
        }

        if (source.isDirty)
        {
            throw new InvalidOperationException("Save the source Siege scene before copying its authored content.");
        }

        Scene target = default;
        try
        {
            SiegeSession sourceSession = FindSession(source);
            SiegeSettings settings = AssetDatabase.LoadAssetAtPath<SiegeSettings>(SettingsPath);
            if (settings == null)
            {
                settings = UnityEngine.Object.Instantiate(sourceSession.Settings);
                settings.name = "SiegeStress";
                var config = new SerializedObject(settings);
                config.FindProperty("startingCoins").intValue = 100000;
                config.FindProperty("firstWaveSize").intValue = 6;
                config.FindProperty("waveGrowth").intValue = 2;
                config.FindProperty("maximumActiveEnemies").intValue = 6;
                config.FindProperty("spawnInterval").floatValue = 0.5f;
                config.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            if (!EditorSceneManager.SaveScene(source, ScenePath, true))
            {
                throw new IOException("Could not save the stress scene copy.");
            }

            target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
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
            Debug.Log("Saved SiegeStress: 100000 test coins, first wave6, active cap6, spawn interval0.5s. Normal Siege unchanged.");
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
