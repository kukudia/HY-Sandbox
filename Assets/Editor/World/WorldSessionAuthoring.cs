// 职责：保留世界配置与制作入口，安装委托模式场景迁移，预览通过正式模式入口加载探索。
// 入口：Install()；重复执行保留配置、Prefab 参数及已存在的界面布局。
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorldSessionAuthoring
{
    public const string MainScenePath = "Assets/Scenes/Main.unity";
    public const string SettingsPath = "Assets/World/WorldGeneration.asset";
    public const string PrefabPath = "Assets/World/WorldSession.prefab";
    [MenuItem("Tools/HY-Sandbox/World/Install Main world session")]
    public static void Install()
    {
        GameModeAuthoring.Install();
    }

    [MenuItem("Tools/HY-Sandbox/World/Select world configuration")]
    public static void SelectConfiguration()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>(SettingsPath);
    }

    [MenuItem("Tools/HY-Sandbox/World/Preview or regenerate session in Play Mode")]
    public static void RegeneratePreview()
    {
        if (!EditorApplication.isPlaying || (PlayManager.instance != null && PlayManager.instance.playMode))
        {
            throw new InvalidOperationException("Enter Main Play Mode and stay in build mode before regenerating the world preview.");
        }

        WorldSessionManager session = PlayManager.instance.WorldSession;
        if (session == null)
        {
            PlayManager.instance.Modes.ShowSelection();
            PlayManager.instance.Modes.SelectExploration();
            return;
        }

        session.BeginPreparation();
        Selection.activeGameObject = session.gameObject;
    }
}
