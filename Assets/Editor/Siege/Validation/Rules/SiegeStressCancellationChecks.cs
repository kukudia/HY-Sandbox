// 职责：在首次分帧恢复中取消压力组件，验证HUD恢复与未发布建筑的回滚。
// 入口：Start() / Status；全新普通Siege Play，两个实际帧后检查无残留根对象。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeStressCancellationChecks
{
    private static SiegeSession session;
    private static int frame;
    private static readonly List<string> checks = new List<string>();
    public static string Status { get; private set; } = "Not run";

    public static string Start()
    {
        session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0 || session.Buildings.Buildings.Count != 1)
        {
            throw new InvalidOperationException("Requires fresh normal Siege Play with only its base.");
        }

        checks.Clear();
        var root = new GameObject("Owned stress cancellation fixture");
        SiegeStressPopulation population = root.AddComponent<SiegeStressPopulation>();
        var values = new SerializedObject(population);
        values.FindProperty("session").objectReferenceValue = session;
        values.FindProperty("buildingBlueprint").objectReferenceValue = session.Settings.BaseBlueprint;
        SerializedProperty anchors = values.FindProperty("anchors");
        anchors.arraySize = 1;
        var anchor = new GameObject("Owned distant stress anchor");
        anchor.transform.SetParent(root.transform, false);
        anchor.transform.position = Vector3.one * 1000f;
        anchors.GetArrayElementAtIndex(0).objectReferenceValue = anchor.transform;
        values.ApplyModifiedPropertiesWithoutUndo();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        IEnumerator routine = (IEnumerator)typeof(SiegeStressPopulation).GetMethod("Start", flags).Invoke(population, null);
        try
        {
            Check(
                "restoration yields before publishing a building",
                routine.MoveNext() && !population.IsReady && population.Buildings.Count == 0 && session.Buildings.Buildings.Count == 1);
            Check("loading temporarily disables HUD listeners", !Object.FindFirstObjectByType<SiegeHud>().enabled);
            Object.DestroyImmediate(population);
            Check("component cancellation restores HUD", Object.FindFirstObjectByType<SiegeHud>().enabled);
        }
        finally
        {
            (routine as IDisposable)?.Dispose();
            Object.DestroyImmediate(root);
        }

        frame = Time.frameCount + 2;
        Status = "Waiting for deferred root destruction";
        EditorApplication.update -= Complete;
        EditorApplication.update += Complete;
        return Status;
    }

    private static void Complete()
    {
        if (EditorApplication.isPlaying && Time.frameCount < frame)
        {
            return;
        }

        EditorApplication.update -= Complete;
        var report = new Report();
        try
        {
            Check(
                "canceled partial building leaves no inactive root",
                session != null && session.Buildings.GetComponentsInChildren<BuildingRuntime>(true).Length == 1);
            Check(
                "base and world capacity remain intact",
                session.Fleet.Base.IsAlive && session.Buildings.Buildings.Count == 1 && session.Buildings.HasCapacity(1));
            report.status = "passed";
        }
        catch (Exception error)
        {
            report.status = "failed";
            report.error = error.ToString();
        }

        report.checks = checks;
        Status = "Temp/Siege/stress-cancellation.json";
        File.WriteAllText(Status, JsonUtility.ToJson(report, true));
    }

    private static void Check(string label, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(label);
        }

        checks.Add(label);
    }

    [Serializable]
    private sealed class Report
    {
        public string status;
        public string error;
        public List<string> checks;
    }
}
