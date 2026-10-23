// 职责：构建以Siege为入口的独立Windows D3D11验证包，不改全局构建场景或用户发布包。
// 入口：Build()；通过Temp状态文件报告真实构建结果，显式命令行才能启动Player自动检查。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class SiegePlayerBuild
{
    public const string Output = "Builds/SiegeValidation/HY-Sandbox.exe";
    public const string StatusPath = "Temp/Siege/player-build.txt";
    [MenuItem("HY-Sandbox/Siege/Build Windows Validation Player")]
    public static void Build()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty || BuildPipeline.isBuildingPlayer)
        {
            throw new InvalidOperationException("Save the scene and leave Play before building.");
        }

        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText(StatusPath, "Scheduled");
        EditorApplication.delayCall += BuildNow;
    }

    private static void BuildNow()
    {
        try
        {
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64) || PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).First() != GraphicsDeviceType.Direct3D11)
            {
                throw new InvalidOperationException("Validation requires the configured Windows D3D11 backend.");
            }

            File.WriteAllText(StatusPath, "Building");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { SiegeSceneAuthoring.ScenePath }, locationPathName = Output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            File.WriteAllText(
                StatusPath,
                $"{report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; seconds={report.summary.totalTime.TotalSeconds:F2}; bytes={report.summary.totalSize}; output={Output}");
        }
        catch (Exception error)
        {
            File.WriteAllText(StatusPath, "Failed: " + error);
            Debug.LogException(error);
        }
    }
}
