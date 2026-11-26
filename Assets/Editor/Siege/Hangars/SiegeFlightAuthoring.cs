// 职责：将已验证起降预设保存到真实Siege场景，保留显式改过的参数和无关对象。
// 入口：UpgradeDefaults()；只将原8/6预设升级12/12，保存前备份原场景。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SiegeFlightAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Siege/Upgrade hangar flight defaults")]
    public static string UpgradeDefaults()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        string backup = "Library/SiegeFlight/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(backup);
        int changed = 0;
        foreach (string path in new[]
        {
            "Assets/Scenes/SiegeGameplay.unity",
        }

        )
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (!opened && scene.isDirty)
            {
                throw new InvalidOperationException("Unsaved changes: " + path);
            }

            if (opened)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            try
            {
                bool dirty = false;
                foreach (MonoBehaviour component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Where(item => item is SiegeSession || item is SiegeGameplayStage))
                {
                    using var serialized = new SerializedObject(component);
                    var settings = serialized.FindProperty("flightSettings");
                    var speed = settings.FindPropertyRelative("speed");
                    var acceleration = settings.FindPropertyRelative("acceleration");
                    if (Mathf.Approximately(speed.floatValue, 8f) && Mathf.Approximately(acceleration.floatValue, 6f))
                    {
                        speed.floatValue = 12f;
                        acceleration.floatValue = 12f;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        dirty = true;
                        changed++;
                    }
                }

                if (dirty)
                {
                    File.Copy(path, backup + "/" + Path.GetFileName(path));
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (opened)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        return "Updated " + changed + " scene flight presets; backup " + backup;
    }
}
