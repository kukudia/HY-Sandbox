// 职责：构建以Siege为入口的独立Windows D3D11验证包，不改全局构建场景或用户发布包。
// 入口：Build() / BuildStress() / BuildLargeStress() / BuildMixedStress() / BuildLargeProfiling()；独立Development包用于引擎归因。
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
    private static bool pending;
    [MenuItem("HY-Sandbox/Siege/Build Windows Validation Player")]
    public static void Build()
    {
        Schedule(SiegeSceneAuthoring.ScenePath, Output, StatusPath);
    }

    [MenuItem("HY-Sandbox/Siege/Build Windows Stress Player")]
    public static void BuildStress()
    {
        SiegeStressAuthoring.Create();
        Schedule(
            SiegeStressAuthoring.ScenePath,
            "Builds/SiegeStress/HY-Sandbox.exe",
            "Temp/Siege/stress-player-build.txt");
    }

    [MenuItem("HY-Sandbox/Siege/Build Windows 2048-Module Stress Player")]
    public static void BuildLargeStress()
    {
        SiegeStressAuthoring.CreateLarge();
        Schedule(
            SiegeStressAuthoring.LargeScenePath,
            "Builds/SiegeLargeStress/HY-Sandbox.exe",
            "Temp/Siege/large-stress-player-build.txt");
    }

    [MenuItem("HY-Sandbox/Siege/Build Windows 2048-Module Profiling Player")]
    public static void BuildLargeProfiling()
    {
        SiegeStressAuthoring.CreateLarge();
        Schedule(
            SiegeStressAuthoring.LargeScenePath,
            "Builds/SiegeLargeProfiling/HY-Sandbox.exe",
            "Temp/Siege/large-profiling-player-build.txt",
            BuildOptions.Development);
    }

    [MenuItem("HY-Sandbox/Siege/Build Windows Mixed 10k Stress Player")]
    public static void BuildMixedStress()
    {
        SiegeMixedStressAuthoring.Create();
        Schedule(
            SiegeMixedStressAuthoring.ScenePath,
            "Builds/SiegeMixedStress/HY-Sandbox.exe",
            "Temp/Siege/mixed-stress-player-build.txt");
    }

    private static void Schedule(string scene, string output, string statusPath, BuildOptions options = BuildOptions.None)
    {
        if (pending || EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty || BuildPipeline.isBuildingPlayer)
        {
            throw new InvalidOperationException("Save the scene and leave Play before building.");
        }

        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText(statusPath, "Scheduled");
        pending = true;
        // Unfocused Editor windows may defer delayCall indefinitely during unattended validation.
        EditorApplication.CallbackFunction build = null;
        build = () =>
        {
            EditorApplication.update -= build;
            BuildNow(scene, output, statusPath, options);
        };
        EditorApplication.update += build;
    }

    private static void BuildNow(string scene, string output, string statusPath, BuildOptions options)
    {
        try
        {
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64) || PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).First() != GraphicsDeviceType.Direct3D11)
            {
                throw new InvalidOperationException("Validation requires the configured Windows D3D11 backend.");
            }

            File.WriteAllText(statusPath, "Building");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scene }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = options });
            File.WriteAllText(
                statusPath,
                $"{report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; seconds={report.summary.totalTime.TotalSeconds:F2}; bytes={report.summary.totalSize}; output={output}; scene={scene}; options={options}");
        }
        catch (Exception error)
        {
            File.WriteAllText(statusPath, "Failed: " + error);
            Debug.LogException(error);
        }
        finally
        {
            pending = false;
        }
    }
}
