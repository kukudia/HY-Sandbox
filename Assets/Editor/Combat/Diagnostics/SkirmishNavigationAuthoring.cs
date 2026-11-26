// 职责：给现有随机5v5保存五组敌舰导航追踪、轨迹/路线与调试面板；不重建地图或阵容。
// 入口：Install()菜单 / Ensure(scene)；幂等，已有追踪配置保留，显式保存目标场景。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SkirmishNavigationAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Combat/Install Arena Navigation Debug")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != SkirmishArenaAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Open Random5v5Arena in Edit Mode.");
        }

        Ensure(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Ensure(Scene scene)
    {
        SkirmishMatch match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SkirmishMatch>(true)).Single();
        SkirmishNavigationDebug debug = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SkirmishNavigationDebug>(true)).SingleOrDefault();
        bool created = debug == null;
        if (created)
        {
            var owner = new GameObject("Arena Navigation Debug");
            SceneManager.MoveGameObjectToScene(owner, scene);
            Undo.RegisterCreatedObjectUndo(owner, "Create arena navigation tracking");
            debug = Undo.AddComponent<SkirmishNavigationDebug>(owner);
        }

        var data = new SerializedObject(debug);
        data.FindProperty("match").objectReferenceValue = match;
        if (data.FindProperty("viewCamera").objectReferenceValue == null)
        {
            data.FindProperty("viewCamera").objectReferenceValue = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First();
        }

        if (data.FindProperty("debugFont").objectReferenceValue == null)
        {
            data.FindProperty("debugFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ChakraPetch-Medium.ttf");
        }

        SerializedProperty tracks = data.FindProperty("enemyTracks");
        tracks.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            if (tracks.GetArrayElementAtIndex(i).objectReferenceValue != null)
            {
                continue;
            }

            var owner = new GameObject("RED-" + (i + 1) + " Navigation Track");
            Undo.RegisterCreatedObjectUndo(owner, "Create enemy trace");
            owner.transform.SetParent(debug.transform, false);
            NavigationTrack track = Undo.AddComponent<NavigationTrack>(owner);
            var trackData = new SerializedObject(track);
            trackData.FindProperty("traceLine").objectReferenceValue = Line(owner.transform, "Flight Trace", Color.white, 0.75f);
            trackData.FindProperty("routeLine").objectReferenceValue = Line(owner.transform, "Planned Route", new Color(1f, 0.6f, 0f), 0.5f);
            trackData.ApplyModifiedPropertiesWithoutUndo();
            tracks.GetArrayElementAtIndex(i).objectReferenceValue = track;
        }

        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static LineRenderer Line(Transform parent, string name, Color color, float width)
    {
        const string path = "Assets/Art/Combat/NavigationDebugTrace.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = "NavigationDebugTrace"
            };
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, path);
        }

        var item = new GameObject(name);
        item.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(item, "Create navigation line");
        LineRenderer line = Undo.AddComponent<LineRenderer>(item);
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.positionCount = 0;
        return line;
    }
}
