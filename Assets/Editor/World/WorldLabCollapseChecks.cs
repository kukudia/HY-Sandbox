// 职责：验证六邻接支撑、尺寸/数量上限及真实世界片段转刚体、再生清理。
// 入口：RunData() / RunPlay()；动态检查仅在WorldLab，真实下落由配套跨帧探针记录。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WorldLabCollapseChecks
{
    private static readonly List<string> Checks = new List<string>();
    private static void Check(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidOperationException(name);
        }

        Checks.Add(name);
    }

    public static string RunData()
    {
        Checks.Clear();
        var data = new VoxelWorldData();
        data.InitializeEmpty(new Vector3Int(16, 16, 16));
        data.Set(new Vector3Int(4, 1, 4), 2);
        data.Set(new Vector3Int(4, 2, 4), 3);
        data.Set(new Vector3Int(4, 3, 4), 3);
        Check(VoxelStructureSupport.FindDetached(data, 32, 12).Count == 0, "rock-supported metal remains static");
        data.Set(new Vector3Int(4, 1, 4), 0);
        var detached = VoxelStructureSupport.FindDetached(data, 32, 12);
        Check(detached.Count == 1 && detached[0].Count == 2, "removed support detaches one connected component");
        Check(
            VoxelStructureSupport.FindDetached(data, 1, 12).Count == 0,
            "oversized component retained as static");
        Check(
            VoxelStructureSupport.FindDetached(data, 32, 0).Count == 0,
            "zero group budget produces no fragments");
        data.Set(new Vector3Int(8, 8, 8), 3);
        Check(VoxelStructureSupport.FindDetached(data, 32, 1).Count == 1, "group count capped");
        data.Set(new Vector3Int(8, 7, 7), 2);
        Check(
            VoxelStructureSupport.FindDetached(data, 32, 12).Count == 2,
            "diagonal contact does not support metal");
        data.Set(new Vector3Int(8, 7, 8), 1);
        Check(VoxelStructureSupport.FindDetached(data, 32, 12).Count == 1, "soil neighbor supports component");
        data.Set(new Vector3Int(2, 0, 2), 3);
        Check(VoxelStructureSupport.FindDetached(data, 32, 12).Count == 1, "world floor anchors metal");
        return Save("data");
    }

    public static string RunPlay()
    {
        if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != WorldLabAuthoring.ScenePath)
        {
            throw new InvalidOperationException("WorldLab Play Mode required.");
        }

        Checks.Clear();
        var world = Object.FindFirstObjectByType<ProceduralVoxelWorld>();
        var collapse = world.GetComponent<VoxelStructureCollapse>();
        if (collapse == null)
        {
            collapse = world.gameObject.AddComponent<VoxelStructureCollapse>();
        }

        collapse.enabled = false;
        world.Generate();
        var a = new Vector3Int(4, 22, 4);
        var b = new Vector3Int(4, 23, 4);
        world.SetCell(a, 3);
        world.SetCell(b, 3);
        world.FlushDirty();
        Physics.SyncTransforms();
        collapse.ResolveDetached();
        Check(world.Data.Get(a) == 0 && world.Data.Get(b) == 0, "detached metal removed from static data");
        Check(collapse.ActivePieces >= 1 && collapse.ActivePieces <= 12, "fragment count bounded");
        VoxelDebrisPiece[] fragments = Object.FindObjectsByType<VoxelDebrisPiece>(FindObjectsSortMode.None);
        VoxelDebrisPiece target = null;
        foreach (VoxelDebrisPiece fragment in fragments)
        {
            if (Vector3.Distance(fragment.transform.position, world.CellCenter(a) - Vector3.one * world.CellSize * 0.5f) < 0.01f)
            {
                target = fragment;
            }
        }

        Check(target != null, "two-cell fragment created at source minimum");
        Check(target.GetComponents<BoxCollider>().Length == 2, "compound collider matches both cells");
        Check(target.GetComponent<MeshFilter>().sharedMesh.vertexCount == 40, "fragment internal face culled");
        Check(
            !target.GetComponent<Rigidbody>().isKinematic && target.GetComponent<Rigidbody>().useGravity,
            "fragment uses real dynamic gravity");
        Check(world.PendingChunks == 0, "source collision removed before next physics step");
        WorldLabDebrisProbe probe = target.gameObject.AddComponent<WorldLabDebrisProbe>();
        probe.Begin(collapse, world);
        return Save("play");
    }

    private static string Save(string phase)
    {
        string path = "Docs/Validation/2026-10-15-world-collapse-" + phase + ".json";
        File.WriteAllText(path, JsonUtility.ToJson(new Result { checks = Checks.ToArray(), passed = Checks.Count }, true));
        return Checks.Count + " collapse " + phase + " checks passed";
    }

    [Serializable]
    private sealed class Result
    {
        public int passed;
        public string[] checks;
    }
}
