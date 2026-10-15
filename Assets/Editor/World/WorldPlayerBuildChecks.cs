// 职责：构建D3D11发布验证包或独立Development制作工具验证包，结果写Temp，不覆盖用户发布包。
// 入口：Build()/BuildAuthoring()从已保存Main的Edit Mode调用，分别写Builds下两个验证目录。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class WorldPlayerBuildChecks
{
    public static void Build()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Save the scene and leave Play Mode before building.");
        }

        Directory.CreateDirectory("Temp/WorldPlayer");
        File.WriteAllText("Temp/WorldPlayer/build.txt", "scheduled");
        EditorApplication.delayCall += BuildNow;
    }

    private static void BuildNow()
    {
        BuildPlayer(
            "Builds/WorldPerformanceValidation/HY-Sandbox.exe",
            "Temp/WorldPlayer/build.txt",
            BuildOptions.None);
    }

    [MenuItem("Tools/HY-Sandbox/Outposts/Build Development authoring validation Player")]
    public static void BuildAuthoring()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Save the scene and leave Play Mode before building.");
        }

        EditorApplication.delayCall += BuildAuthoringNow;
    }

    private static void BuildAuthoringNow()
    {
        BuildPlayer(
            "Builds/OutpostAuthoringValidation/HY-Sandbox.exe",
            "Temp/WorldPlayer/authoring-build.txt",
            BuildOptions.Development);
    }

    private static void BuildPlayer(string output, string reportPath, BuildOptions options)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64) || PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).First() != GraphicsDeviceType.Direct3D11)
            {
                throw new InvalidOperationException("Windows must default to the verified D3D11 backend.");
            }

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(), locationPathName = output, target = BuildTarget.StandaloneWindows64, options = options });
            File.WriteAllText(
                reportPath,
                $"{report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; duration={report.summary.totalTime}; bytes={report.summary.totalSize}");
        }
        catch (Exception error)
        {
            File.WriteAllText(reportPath, error.ToString());
            Debug.LogException(error);
        }
    }
}
