// 职责：在WorldLab测量真实生成、球形破坏和脏块网格/碰撞重建的同步CPU成本。
// 入口：Run()；固定种子/爆点和样本记录，不把微基准当作帧率或GPU性能。
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WorldLabPerformanceChecks
{
    public static string Run()
    {
        if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != WorldLabAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Run in WorldLab Play Mode only.");
        }

        var world = Object.FindFirstObjectByType<ProceduralVoxelWorld>();
        var timer = new Stopwatch();
        var report = new Report();
        world.Generate();
        int expectedChunks = world.GetComponentsInChildren<VoxelWorldChunk>().Length;
        uint fingerprint = world.Data.Fingerprint();
        for (int sample = 0; sample < report.generationMs.Length; sample++)
        {
            timer.Restart();
            world.Generate();
            timer.Stop();
            report.generationMs[sample] = timer.Elapsed.TotalMilliseconds;
        }

        var random = new System.Random(5129);
        for (int sample = 0; sample < report.damageMs.Length; sample++)
        {
            int x = random.Next(4, world.Data.Size.x - 4);
            int z = random.Next(4, world.Data.Size.z - 4);
            int y = world.Data.Size.y - 1;
            while (y > 0 && world.Data.Get(new Vector3Int(x, y, z)) == 0)
            {
                y--;
            }

            Vector3 point = world.CellCenter(new Vector3Int(x, y, z));
            timer.Restart();
            report.removed[sample] = world.DamageSphere(point, 8f, 1000f);
            timer.Stop();
            report.damageMs[sample] = timer.Elapsed.TotalMilliseconds;
            report.affectedChunks[sample] = world.PendingChunks;
            timer.Restart();
            world.FlushDirty();
            timer.Stop();
            report.rebuildMs[sample] = timer.Elapsed.TotalMilliseconds;
        }

        report.generationMedianMs = Median(report.generationMs);
        report.damageMedianMs = Median(report.damageMs);
        report.rebuildMedianMs = Median(report.rebuildMs);
        report.rebuildMaximumMs = report.rebuildMs.Max();
        report.maximumAffectedChunks = report.affectedChunks.Max();
        report.totalRemoved = report.removed.Sum();
        world.Generate();
        report.chunksAfterCycles = world.GetComponentsInChildren<VoxelWorldChunk>().Length;
        report.restoredFingerprint = world.Data.Fingerprint() == fingerprint;
        report.validReferences = world.GetComponentsInChildren<MeshFilter>().All(f => f.sharedMesh != null);
        report.vertices = world.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.vertexCount);
        report.triangles = world.GetComponentsInChildren<MeshFilter>().Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3 + (int)f.sharedMesh.GetIndexCount(1) / 3 + (int)f.sharedMesh.GetIndexCount(2) / 3);
        if (report.chunksAfterCycles != expectedChunks || !report.restoredFingerprint || !report.validReferences || report.totalRemoved == 0)
        {
            throw new InvalidOperationException("Repeated destruction did not preserve world ownership/data invariants.");
        }

        Directory.CreateDirectory("Docs/Validation");
        string path = "Docs/Validation/2026-10-15-world-performance.json";
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path;
    }

    private static double Median(double[] values)
    {
        double[] copy = (double[])values.Clone();
        Array.Sort(copy);
        return copy[copy.Length / 2];
    }

    [Serializable]
    private sealed class Report
    {
        public string methodology = "WorldLab64x24x64 seed14721 cell2m chunk16. One warmup, 5 complete rebuild samples; 32 cumulative radius8m energy1000 blasts with deterministic surface positions (seed5129). Stopwatch synchronous CPU costs include MeshCollider assignment/cooking, but exclude end-of-frame deferred mesh destruction, draw/GPU/GC. No whole-game FPS or zero-allocation claim.";
        public double[] generationMs = new double[5];
        public double[] damageMs = new double[32];
        public double[] rebuildMs = new double[32];
        public int[] removed = new int[32];
        public int[] affectedChunks = new int[32];
        public double generationMedianMs;
        public double damageMedianMs;
        public double rebuildMedianMs;
        public double rebuildMaximumMs;
        public int maximumAffectedChunks;
        public int totalRemoved;
        public int chunksAfterCycles;
        public bool restoredFingerprint;
        public bool validReferences;
        public int vertices;
        public int triangles;
    }
}
