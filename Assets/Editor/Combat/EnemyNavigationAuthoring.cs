// 职责：把共享导航组件保存到实际玩法场景，保留原场景层级与 Inspector 参数。
// 入口：Install() / Tools 菜单；已打开的脏场景拒绝保存，避免夹带手工改动。
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyNavigationAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Combat/Install Enemy Navigation")]
    public static void Install()
    {
        string[] paths =
        {
            "Assets/Scenes/Main.unity",
            "Assets/Scenes/Siege.unity",
            "Assets/Scenes/WorldLab.unity"
        };
        foreach (string path in paths)
        {
            Scene existing = SceneManager.GetSceneByPath(path);
            if (existing.IsValid() && existing.isLoaded && existing.isDirty)
            {
                throw new InvalidOperationException("Save or discard manual scene changes first: " + path);
            }
        }

        Scene active = SceneManager.GetActiveScene();
        foreach (string path in paths)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            try
            {
                bool found = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    found |= root.GetComponentInChildren<EnemyNavigationWorld>(true) != null;
                }

                if (!found)
                {
                    var owner = new GameObject("Enemy Navigation");
                    SceneManager.MoveGameObjectToScene(owner, scene);
                    Undo.RegisterCreatedObjectUndo(owner, "Install enemy navigation");
                    Undo.AddComponent<EnemyNavigationWorld>(owner);
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

        if (active.IsValid() && active.isLoaded)
        {
            SceneManager.SetActiveScene(active);
        }
    }
}
