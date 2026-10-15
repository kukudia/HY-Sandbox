// 职责：检查种子、网格边界与真实PhysX破坏/填补/炮弹命中，并将结果写入验证文件。
// 入口：RunDataChecks() / RunPlayChecks()；只在独立WorldLab执行运行检查，不操作玩家存档。
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WorldLabChecks
{
    private static readonly List<string> Passed = new List<string>();
    private static void Check(bool result, string name)
    {
        if (!result)
        {
            throw new System.InvalidOperationException(name);
        }

        Passed.Add(name);
    }

    public static string RunDataChecks()
    {
        Passed.Clear();
        var first = new VoxelWorldData();
        var second = new VoxelWorldData();
        var size = new Vector3Int(32, 24, 32);
        first.Generate(size, 14721);
        second.Generate(size, 14721);
        Check(first.Fingerprint() == second.Fingerprint(), "same seed gives same voxel content");
        second.Generate(size, 14722);
        Check(first.Fingerprint() != second.Fingerprint(), "different seed changes content");
        Check(
            first.SolidCount > 1000 && first.SolidCount < size.x * size.y * size.z,
            "bounded generated density");
        Check(first.Get(new Vector3Int(-1, 0, 0)) == 0, "out of bounds reads air");
        Check(!first.Set(new Vector3Int(-1, 0, 0), 2), "out of bounds write rejected");
        Check(!first.Set(Vector3Int.zero, 4), "invalid material rejected");
        uint fingerprint = first.Fingerprint();
        string json = JsonUtility.ToJson(first);
        var restored = JsonUtility.FromJson<VoxelWorldData>(json);
        restored.Recount();
        Check(
            restored.Fingerprint() == fingerprint && restored.SolidCount == first.SolidCount,
            "serialized bake retains cells and count");
        Clear(first);
        first.Set(new Vector3Int(15, 4, 4), 2);
        first.Set(new Vector3Int(16, 4, 4), 3);
        Mesh left = VoxelSurfaceMesher.Build(first, Vector3Int.zero, 16, 2);
        Mesh right = VoxelSurfaceMesher.Build(first, new Vector3Int(16, 0, 0), 16, 2);
        Check(left.vertexCount == 20 && right.vertexCount == 20, "cross-chunk internal faces culled");
        Check(
            left.GetTriangles(1).Length == 30 && right.GetTriangles(2).Length == 30,
            "material submeshes retain faces");
        Check(left.normals.All(n => Mathf.Approximately(n.magnitude, 1)), "unit outward face normals");
        Object.DestroyImmediate(left);
        Object.DestroyImmediate(right);
        first.Set(new Vector3Int(16, 4, 4), 0);
        left = VoxelSurfaceMesher.Build(first, Vector3Int.zero, 16, 2);
        Check(left.vertexCount == 24, "removed neighbor exposes seam face");
        Object.DestroyImmediate(left);
        Check(first.SolidCount == 1, "cell counter tracks removal");
        return Save("data");
    }

    private static void Clear(VoxelWorldData data)
    {
        Vector3Int size = data.Size;
        for (int z = 0; z < size.z; z++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    data.Set(new Vector3Int(x, y, z), 0);
                }
            }
        }
    }

    public static string RunPlayChecks()
    {
        if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != WorldLabAuthoring.ScenePath)
        {
            throw new System.InvalidOperationException("Run in WorldLab Play Mode only.");
        }

        Passed.Clear();
        ProceduralVoxelWorld world = Object.FindFirstObjectByType<ProceduralVoxelWorld>();
        Check(world != null && world.Data.IsValid, "baked world loads in Play");
        uint original = world.Data.Fingerprint();
        var a = new Vector3Int(15, 22, 15);
        var b = new Vector3Int(16, 22, 15);
        world.SetCell(a, 2);
        world.SetCell(b, 3);
        Check(world.PendingChunks <= 4 && world.PendingChunks >= 2, "seam edit queues only affected neighbors");
        world.FlushDirty(1);
        Check(world.LastRebuiltChunks == 1, "per-frame rebuild budget enforced");
        world.FlushDirty();
        Physics.SyncTransforms();
        Vector3 center = world.CellCenter(a);
        Check(
            Physics.Raycast(center + Vector3.up * 10, Vector3.down, out RaycastHit hit, 12) && hit.collider.GetComponent<VoxelWorldChunk>() != null,
            "placed cell has real PhysX surface");
        Check(world.DamageSphere(center, 0, 1000) == 0, "zero radius ignored");
        Check(
            world.DamageSphere(center, 0.8f, 30) == 0 && world.Data.Get(a) == 2,
            "sub-threshold hit retains rock");
        Check(
            world.DamageSphere(center, 0.8f, 30) == 1 && world.Data.Get(a) == 0,
            "damage accumulates to remove rock");
        world.FlushDirty();
        Physics.SyncTransforms();
        Check(!Physics.Raycast(center + Vector3.up * 10, Vector3.down, 12), "removed rock no longer collides");
        world.SetCell(a, 2);
        world.FlushDirty();
        Physics.SyncTransforms();
        Check(Physics.Raycast(center + Vector3.up * 10, Vector3.down, out hit, 12), "fill restores collision");
        // Editor checks live in another assembly; reflection exercises the existing internal production entry.
        typeof(LaserTurret).Assembly.GetType("TurretHitResolver").GetMethod("Apply", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(
            null,
            new object[] { hit.collider, hit.point, hit.normal, Vector3.down, null, UnitFaction.Player, 1000f, 0f });
        Check(world.Data.Get(a) == 0, "production turret hit damages terrain");
        world.FlushDirty();
        int removed = world.DamageSphere(world.CellCenter(b), 6f, 1000);
        Check(removed >= 1 && world.PendingChunks > 0, "sphere blast changes data and queues chunks");
        world.FlushDirty();
        Physics.SyncTransforms();
        Check(
            world.GetComponentsInChildren<MeshFilter>().All(f => f.sharedMesh != null),
            "all chunk mesh references valid after edits");
        world.Generate();
        Check(
            world.Data.Fingerprint() == original && world.RemovedCells == 0,
            "regenerate restores seed content and counters");
        var bodyObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bodyObject.name = "WorldLab physics probe";
        bodyObject.transform.position = new Vector3(64, 80, 64);
        var body = bodyObject.AddComponent<Rigidbody>();
        body.mass = 10f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        WorldLabPhysicsProbe probe = bodyObject.AddComponent<WorldLabPhysicsProbe>();
        probe.Begin("Docs/Validation/2026-10-15-world-physics.json");
        return Save("play");
    }

    private static string Save(string phase)
    {
        Directory.CreateDirectory("Docs/Validation");
        string path = "Docs/Validation/2026-10-15-world-" + phase + ".json";
        File.WriteAllText(
            path,
            JsonUtility.ToJson(new Result { phase = phase, checks = Passed.ToArray(), passed = Passed.Count }, true));
        return path + ": " + Passed.Count + " passed";
    }

    [System.Serializable]
    private sealed class Result
    {
        public string phase;
        public int passed;
        public string[] checks;
    }
}
