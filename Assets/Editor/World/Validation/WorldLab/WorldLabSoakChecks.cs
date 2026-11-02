// 职责：在真实帧后台持续爆破/重生成，周期落盘并检查分块/网格有界及输入遮罩下帧推进。
// 入口：Begin(minutes)；最多15分钟，Stop()或离开Play安全结束，不接触真实玩家存档。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WorldLabSoakChecks
{
    private static ProceduralVoxelWorld world;
    private static DateTimeOffset deadline;
    private static DateTimeOffset nextAction;
    private static DateTimeOffset nextCheckpoint;
    private static DateTimeOffset lastFrameAt;
    private static int lastFrame;
    private static int expectedChunks;
    private static uint initialFingerprint;
    private static System.Random random;
    private static Report report;
    static WorldLabSoakChecks()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
    }

    public static string Begin(int minutes = 15)
    {
        if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != WorldLabAuthoring.ScenePath || report != null)
        {
            throw new InvalidOperationException("Start one soak in WorldLab Play Mode.");
        }

        world = Object.FindFirstObjectByType<ProceduralVoxelWorld>();
        world.Generate();
        expectedChunks = world.GetComponentsInChildren<VoxelWorldChunk>().Length;
        initialFingerprint = world.Data.Fingerprint();
        report = new Report
        {
            startedUtc = DateTimeOffset.UtcNow.ToString("o"),
            status = "running"
        };
        deadline = DateTimeOffset.UtcNow.AddMinutes(Mathf.Clamp(minutes, 1, 15));
        nextAction = DateTimeOffset.UtcNow.AddSeconds(1);
        nextCheckpoint = DateTimeOffset.UtcNow;
        lastFrameAt = DateTimeOffset.UtcNow;
        lastFrame = Time.frameCount;
        random = new System.Random(1510);
        EditorApplication.update += Tick;
        WriteCheckpoint();
        return "Real-frame soak started until " + deadline.ToString("o");
    }

    private static void Tick()
    {
        if (report == null)
        {
            return;
        }

        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!EditorApplication.isPlaying || world == null)
            {
                Stop();
                return;
            }

            if (Time.frameCount != lastFrame)
            {
                report.observedFrames += Time.frameCount - lastFrame;
                lastFrame = Time.frameCount;
                lastFrameAt = now;
            }

            report.longestFrameStallSeconds = Math.Max(report.longestFrameStallSeconds, (now - lastFrameAt).TotalSeconds);
            if ((now - lastFrameAt).TotalSeconds > 15)
            {
                report.failure = "No real Unity frame advance for more than15 seconds.";
                Finish(false);
                return;
            }

            if (now >= deadline)
            {
                Finish(true);
                return;
            }

            if (now >= nextAction && !EditorApplication.isPaused)
            {
                if (report.blasts > 0 && report.blasts % 120 == 0)
                {
                    world.Generate();
                    report.regenerations++;
                }

                int x = random.Next(4, world.Data.Size.x - 4);
                int z = random.Next(4, world.Data.Size.z - 4);
                int y = world.Data.Size.y - 1;
                while (y > 0 && world.Data.Get(new Vector3Int(x, y, z)) == 0)
                {
                    y--;
                }

                report.removedCells += world.DamageSphere(world.CellCenter(new Vector3Int(x, y, z)), 8f, 1000f);
                report.blasts++;
                report.maximumPendingChunks = Math.Max(report.maximumPendingChunks, world.PendingChunks);
                nextAction = now.AddSeconds(0.5);
            }

            if (now >= nextCheckpoint)
            {
                int chunks = world.GetComponentsInChildren<VoxelWorldChunk>().Length;
                report.maximumLiveVoxelMeshes = Math.Max(
                    report.maximumLiveVoxelMeshes,
                    Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name.StartsWith("VoxelChunk_")));
                if (chunks != expectedChunks || world.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh == null))
                {
                    report.failure = "Chunk ownership or mesh reference changed unexpectedly.";
                    Finish(false);
                    return;
                }

                WriteCheckpoint();
                nextCheckpoint = now.AddSeconds(30);
            }
        }
        catch (Exception error)
        {
            report.failure = error.ToString();
            Finish(false);
        }
    }

    public static void Stop()
    {
        if (report != null)
        {
            report.failure = "Stopped before full requested duration; partial evidence only.";
            Finish(false);
        }
    }

    private static void Finish(bool complete)
    {
        EditorApplication.update -= Tick;
        report.status = complete ? "completed" : "incomplete";
        if (world != null && EditorApplication.isPlaying)
        {
            world.Generate();
            report.seedRestored = world.Data.Fingerprint() == initialFingerprint;
        }

        WriteCheckpoint();
        report = null;
        world = null;
    }

    private static void WriteCheckpoint()
    {
        report.updatedUtc = DateTimeOffset.UtcNow.ToString("o");
        string path = "Docs/Validation/2026-10-15-world-soak.json";
        File.WriteAllText(path + ".tmp", JsonUtility.ToJson(report, true));
        File.Copy(path + ".tmp", path, true);
        File.Delete(path + ".tmp");
    }

    [Serializable]
    private sealed class Report
    {
        public string methodology = "Real Unity frames,15-minute background stress, radius8m/energy1000 blast every0.5 wall seconds, regenerate every120 blasts. Check ownership/references and checkpoint every30s. Mesh peak includes pending deferred-destroy meshes and loaded baked assets; not proof of zero allocations or GPU FPS. No input injected.";
        public string startedUtc;
        public string updatedUtc;
        public string status;
        public string failure;
        public int observedFrames;
        public int blasts;
        public int regenerations;
        public int removedCells;
        public int maximumPendingChunks;
        public int maximumLiveVoxelMeshes;
        public double longestFrameStallSeconds;
        public bool seedRestored;
    }
}
