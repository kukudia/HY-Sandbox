// 职责：将自由观察组件与使用提示保存到真实SiegeGameplay场景，保留已有布局与手工参数。
// 入口：Install()；可重复绑定，仅新增自己拥有的Camera Help节点，保存前创建场景备份。
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SiegeObserverCameraAuthoring
{
    private const string ScenePath = "Assets/Scenes/SiegeGameplay.unity";
    [MenuItem("Tools/HY-Sandbox/Siege/Install free observer camera")]
    public static string Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before installing the observer camera.");
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (!opened && scene.isDirty)
        {
            throw new InvalidOperationException("Save SiegeGameplay before installing the observer camera.");
        }

        string backup = "Library/SiegeObserver/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(backup);
        File.Copy(ScenePath, backup + "/SiegeGameplay.unity");
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            SiegeGameplayStage stage = SceneObjectQuery.FindInScene<SiegeGameplayStage>(scene);
            if (stage == null)
            {
                throw new InvalidOperationException("SiegeGameplay requires its authored stage.");
            }

            SiegeObserverCamera observer = stage.GetComponent<SiegeObserverCamera>();
            if (observer == null)
            {
                observer = Undo.AddComponent<SiegeObserverCamera>(stage.gameObject);
            }

            Transform canvas = stage.transform.Find("Siege Gameplay UI");
            if (canvas == null)
            {
                throw new InvalidOperationException("SiegeGameplay requires its authored UI Canvas.");
            }

            Transform help = canvas.Find("Camera Help");
            Text text;
            if (help == null)
            {
                var obj = new GameObject("Camera Help", typeof(RectTransform), typeof(Text));
                Undo.RegisterCreatedObjectUndo(obj, "Add Siege camera help");
                obj.transform.SetParent(canvas, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -38f);
                rect.sizeDelta = new Vector2(1180f, 28f);
                text = obj.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 15;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.raycastTarget = false;
            }
            else
            {
                text = help.GetComponent<Text>();
            }

            text.text = "FREE VIEW  |  Hold RMB + WASD / Q E  |  Shift fast  |  Wheel speed  |  F overview";
            using (var values = new SerializedObject(observer))
            {
                values.FindProperty("hint").objectReferenceValue = text;
                values.ApplyModifiedPropertiesWithoutUndo();
            }

            using (var values = new SerializedObject(stage))
            {
                SerializedProperty reference = values.FindProperty("observerCamera");
                if (reference == null)
                {
                    throw new InvalidOperationException("Stage must declare its serialized observerCamera reference before installation.");
                }

                reference.objectReferenceValue = observer;
                values.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "SiegeGameplay observer saved; backup " + backup;
        }
        finally
        {
            if (opened)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
