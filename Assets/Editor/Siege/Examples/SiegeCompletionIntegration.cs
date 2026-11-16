// 职责：为既有Main模式选择绑定三例目录；保留场景和旧示例制作入口引用。
// 入口：Install()；仅Edit模式、无脏场景，通过Editor API保存引用。
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SiegeCompletionIntegration
{
    public static string Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before installation.");
        }

        const string path = "Assets/Scenes/Main.unity";
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (!opened && scene.isDirty)
        {
            throw new InvalidOperationException("Save Main before installation.");
        }

        string backup = "Library/SiegeCompletion/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
        Directory.CreateDirectory(backup);
        File.Copy(path, backup + "/Main.unity");
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        try
        {
            GameModePanel panel = SceneObjectQuery.FindInScene<GameModePanel>(scene);
            if (panel == null)
            {
                throw new InvalidOperationException("Main requires a mode panel.");
            }

            using (var values = new SerializedObject(panel))
            {
                values.FindProperty("siegeSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>("Assets/Siege/Construction/Settings.asset");
                values.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (opened)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        return "Mode panel example catalog saved; backup " + backup;
    }
}
