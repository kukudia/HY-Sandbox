// 职责：制作四栋建筑/六舰/十二敌舰的独立混合压力Scene和配置，保留正常阵地战资产。
// 入口：Create()；首次创建后可编辑保存，已有目标不覆盖，恢复原活动场景。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SiegeMixedStressAuthoring
{
    public const string ScenePath = "Assets/Scenes/SiegeMixedStress.unity";
    public const string SettingsPath = "Assets/Siege/SiegeMixedStress.asset";
    [MenuItem("HY-Sandbox/Siege/Create Editable Mixed 10k Stress Profile")]
    public static void Create()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Save the current scene and leave Play before creating stress assets.");
        }

        SiegeStressAuthoring.CreateLarge();
        if (File.Exists(ScenePath))
        {
            if (AssetDatabase.LoadAssetAtPath<SiegeSettings>(SettingsPath) == null)
            {
                throw new InvalidOperationException("Existing mixed scene is missing its settings asset.");
            }

            Debug.Log("Existing mixed stress scene preserved.");
            return;
        }

        Scene previous = SceneManager.GetActiveScene();
        Scene source = SceneManager.GetSceneByPath(SiegeStressAuthoring.LargeScenePath);
        bool opened = !source.IsValid() || !source.isLoaded;
        Scene target = default;
        try
        {
            if (opened)
            {
                source = EditorSceneManager.OpenScene(SiegeStressAuthoring.LargeScenePath, OpenSceneMode.Additive);
            }

            if (source.isDirty)
            {
                throw new InvalidOperationException("Save the source stress scene first.");
            }

            SiegeSettings settings = AssetDatabase.LoadAssetAtPath<SiegeSettings>(SettingsPath);
            if (settings == null)
            {
                settings = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<SiegeSettings>(SiegeStressAuthoring.LargeSettingsPath));
                settings.name = "SiegeMixedStress";
                var values = new SerializedObject(settings);
                values.FindProperty("firstWaveSize").intValue = 12;
                values.FindProperty("maximumActiveEnemies").intValue = 12;
                values.FindProperty("attackRadius").floatValue = 220f;
                values.FindProperty("guardRadius").floatValue = 180f;
                values.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            if (!EditorSceneManager.SaveScene(source, ScenePath, true))
            {
                throw new IOException("Could not save mixed stress scene copy.");
            }

            target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(target);
            SiegeSession session = target.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SiegeSession>(true)).Single();
            var sessionValues = new SerializedObject(session);
            sessionValues.FindProperty("settings").objectReferenceValue = settings;
            Transform baseDock = (Transform)sessionValues.FindProperty("baseDock").objectReferenceValue;
            sessionValues.ApplyModifiedPropertiesWithoutUndo();
            var root = new GameObject("Mixed stress population");
            SceneManager.MoveGameObjectToScene(root, target);
            SiegeStressPopulation population = root.AddComponent<SiegeStressPopulation>();
            var populationValues = new SerializedObject(population);
            populationValues.FindProperty("session").objectReferenceValue = session;
            populationValues.FindProperty("buildingBlueprint").objectReferenceValue = settings.BaseBlueprint;
            Vector3[] offsets =
            {
                new Vector3(-80f, 0f, -80f),
                new Vector3(80f, 0f, -80f),
                new Vector3(0f, 0f, 100f)
            };
            SerializedProperty anchors = populationValues.FindProperty("anchors");
            anchors.arraySize = offsets.Length;
            for (int index = 0; index < offsets.Length; index++)
            {
                var anchor = new GameObject("Satellite building " + (index + 1));
                anchor.transform.SetParent(root.transform, false);
                anchor.transform.SetPositionAndRotation(baseDock.position + offsets[index], baseDock.rotation);
                anchors.GetArrayElementAtIndex(index).objectReferenceValue = anchor.transform;
            }

            populationValues.ApplyModifiedPropertiesWithoutUndo();
            CameraController camera = target.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<CameraController>(true)).Single();
            camera.thirdPersonOffset = new Vector3(0f, 150f, -230f);
            camera.maxZoom = -400f;
            camera.minZoom = -80f;
            EditorUtility.SetDirty(camera);
            EditorSceneManager.MarkSceneDirty(target);
            if (!EditorSceneManager.SaveScene(target))
            {
                throw new IOException("Could not save mixed stress population references.");
            }

            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (target.IsValid() && target.isLoaded)
            {
                EditorSceneManager.CloseScene(target, true);
            }

            if (opened && source.IsValid() && source.isLoaded)
            {
                EditorSceneManager.CloseScene(source, true);
            }

            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
        }
    }
}
