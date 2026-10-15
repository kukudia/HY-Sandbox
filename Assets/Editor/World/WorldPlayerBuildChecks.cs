// 职责：构建与发布配置相同的Windows验证包，结果写Temp；不递增版本或覆盖已有发布包。
// 入口：Build()从已保存Main的Edit Mode调用；生成Builds/WorldPerformanceValidation。
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
        try
        {
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64) || PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).First() != GraphicsDeviceType.Direct3D11)
            {
                throw new InvalidOperationException("Windows must default to the verified D3D11 backend.");
            }

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(), locationPathName = "Builds/WorldPerformanceValidation/HY-Sandbox.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            File.WriteAllText(
                "Temp/WorldPlayer/build.txt",
                $"{report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; duration={report.summary.totalTime}; bytes={report.summary.totalSize}");
        }
        catch (Exception error)
        {
            File.WriteAllText("Temp/WorldPlayer/build.txt", error.ToString());
            Debug.LogException(error);
        }
    }
}
