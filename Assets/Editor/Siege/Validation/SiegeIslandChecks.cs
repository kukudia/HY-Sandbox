// 职责：验证浮空岛确定性、空气边界、基地支撑及DestroyManager到地形网格/碰撞的生产路径。
// 入口：RunData() / RunLive()；实战检查仅用于临时示例会话，会在基地外制造一个坑。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeIslandChecks
{
    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public List<string> checks = new List<string>();
        public string error;
    }

    public static string RunData()
    {
        var report = new Report();
        try
        {
            Vector3Int size = new Vector3Int(64, 32, 64);
            VoxelWorldData data = FloatingIslandTerrain.Generate(size, 160611, 28, new Vector2(12, 12), 20);
            Check(
                report,
                "island is finite and partially solid",
                data.IsValid && !data.IsStreaming && data.SolidCount > 0 && data.SolidCount < 64 * 32 * 64 / 2);
            Check(
                report,
                "same seed exactly reproduces island data",
                data.Fingerprint() == FloatingIslandTerrain.Generate(size, 160611, 28, new Vector2(12, 12), 20).Fingerprint());
            Check(
                report,
                "another seed changes the cliff profile",
                data.Fingerprint() != FloatingIslandTerrain.Generate(size, 7, 28, new Vector2(12, 12), 20).Fingerprint());
            bool edges = true;
            bool bottom = true;
            for (int x = 0; x < 64; x++)
            {
                for (int z = 0; z < 64; z++)
                {
                    bottom &= data.Get(new Vector3Int(x, 0, z)) == 0;
                    for (int y = 0; y < 32; y++)
                    {
                        if (x == 0 || x == 63 || z == 0 || z == 63)
                        {
                            edges &= data.Get(new Vector3Int(x, y, z)) == 0;
                        }
                    }
                }
            }

            Check(report, "all four edges are air through the full height", edges);
            Check(report, "air exists below the entire suspended rock mass", bottom);
            bool plateau = true;
            for (int x = 21; x < 43; x++)
            {
                for (int z = 21; z < 43; z++)
                {
                    plateau &= data.Get(new Vector3Int(x, 27, z)) == 1 && data.Get(new Vector3Int(x, 28, z)) == 0;
                }
            }

            Check(report, "the deployment plateau is flat with unobstructed air above", plateau);
            Check(report, "deep interior uses World rock material", data.Get(new Vector3Int(32, 20, 32)) == 2);
            report.passed = true;
        }
        catch (Exception error)
        {
            report.error = error.ToString();
        }

        return Save(report, "data");
    }

    public static string RunLive()
    {
        var report = new Report();
        GameObject source = null;
        bool previousPlay = PlayManager.instance != null && PlayManager.instance.playMode;
        try
        {
            SiegeGameplayStage stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
            if (!EditorApplication.isPlaying || stage?.Base == null)
            {
                throw new InvalidOperationException("Launch the temporary official example through the main menu first.");
            }

            SiegeIsland island = stage.Island;
            ProceduralVoxelWorld world = island.World;
            Check(
                report,
                "live Siege uses the shared finite World pipeline",
                island.IsGenerated && world.GetComponentsInChildren<VoxelWorldChunk>().Length > 0);
            Check(
                report,
                "no legacy arena floor remains",
                !stage.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Any(item => item.name == "Arena Ground"));
            Check(
                report,
                "the whole real base foundation has voxel ground support",
                BlueprintStructurePlacement.Support(world, stage.Base.Building.Footprint, stage.Base.Building.transform) == 1f);
            Vector3 outside = island.PlayableBounds.center + new Vector3(island.PlayableBounds.extents.x + 10, 40, 0);
            Check(
                report,
                "outside the island has no invisible catch floor",
                !Physics.Raycast(outside, Vector3.down, 200, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
            Check(
                report,
                "normal flight above the island is recoverable",
                !island.BeyondRecovery(island.PlayableBounds.center + Vector3.up * 50));
            Check(
                report,
                "falling below the void boundary is unrecoverable",
                island.BeyondRecovery(island.PlayableBounds.center + Vector3.down * 100));
            bool spawns = true;
            for (int i = 0; i < 16; i++)
            {
                Vector3 spawn = island.SpawnPosition(i * Mathf.PI / 8, 10000);
                Vector3 relative = spawn - island.PlayableBounds.center;
                spawns &= Mathf.Abs(relative.x) < island.PlayableBounds.extents.x && Mathf.Abs(relative.z) < island.PlayableBounds.extents.z && relative.y >= 10;
            }

            Check(report, "enemy ingress remains inside finite bounds at every heading", spawns);
            Vector3 center = island.PlayableBounds.center + new Vector3(60, -1, 0);
            Check(
                report,
                "crater probe starts on a real terrain surface",
                Physics.Raycast(center + Vector3.up * 40, Vector3.down, out RaycastHit before, 80) && before.collider.GetComponent<VoxelWorldChunk>() != null);
            source = Object.Instantiate(Resources.Load<GameObject>("Blocks/2x2x2"), center, Quaternion.identity);
            Block blast = source.GetComponent<Block>();
            blast.explosionDamage = 1000;
            blast.explosionRadius = 7;
            blast.explosionForce = 0;
            int count = world.Data.SolidCount;
            PlayManager.instance.playMode = true;
            DestroyManager.instance.ExplodeBlock(blast);
            source.SetActive(false);
            Check(
                report,
                "DestroyManager explosion removes floating island voxels",
                world.Data.SolidCount < count && world.RemovedCells > 0);
            world.FlushDirty();
            Physics.SyncTransforms();
            Check(
                report,
                "shared dirty chunk rebuilding refreshes the crater collider",
                Physics.Raycast(center + Vector3.up * 40, Vector3.down, out RaycastHit after, 80) && after.point.y < before.point.y - 2);
            Check(
                report,
                "distant base survives the terrain destruction probe",
                stage.Base.IsAlive && stage.Base.Building.Anchored);
            report.passed = true;
        }
        catch (Exception error)
        {
            report.error = error.ToString();
        }
        finally
        {
            if (PlayManager.instance != null)
            {
                PlayManager.instance.playMode = previousPlay;
            }

            if (source != null)
            {
                source.SetActive(false);
                Object.Destroy(source);
            }
        }

        return Save(report, "live");
    }

    public static string RunBoundary()
    {
        var report = new Report();
        try
        {
            SiegeGameplayStage stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
            if (!EditorApplication.isPlaying || stage?.Match.CanRefit != true)
            {
                throw new InvalidOperationException("Boundary probe requires a fresh temporary example in deployment.");
            }

            BlockDataList finch = JsonUtility.FromJson<BlockDataList>(Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch").text);
            var option = new SiegeShipOption("Boundary Finch", JsonUtility.ToJson(finch), 1, finch.blocks.Count);
            typeof(SiegeGameplayStage).GetField("defenders", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stage, new[] { option });
            typeof(SiegeGameplayStage).GetField("attackers", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stage, new[] { option });
            Check(report, "a real friendly ship deploys before the void probe", stage.TryDeployAt(0, 0, out _));
            SiegeFleetEntry defender = stage.Fleet.Ships[0];
            defender.Unit.transform.position = stage.Island.PlayableBounds.center + Vector3.down * 150;
            Check(
                report,
                "the production fleet boundary retires a fallen friendly",
                stage.Island.RecoverFleet(stage.Fleet, stage.Base) && stage.Fleet.Ships[0] == defender && !defender.IsAlive);
            Check(report, "the real host begins the boundary test wave", stage.BeginWave());
            var director = (SiegeWaveDirector)typeof(SiegeGameplayStage).GetField("waves", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(stage);
            director.Tick(Time.time);
            ControlUnit enemy = ControlUnit.RuntimeUnits.First(unit => unit != null && unit.gameObject.scene == stage.gameObject.scene && unit.faction == UnitFaction.Enemy && unit.HasAnyCockpit);
            int active = stage.Match.ActiveAttackers;
            int kills = stage.Match.DefeatedAttackers;
            enemy.transform.position = stage.Island.PlayableBounds.center + Vector3.down * 150;
            director.Tick(Time.time);
            Check(
                report,
                "a fallen attacker releases its registered wave slot",
                !enemy.gameObject.activeSelf && stage.Match.ActiveAttackers == active - 1);
            Check(report, "void removal grants no fabricated kill reward", stage.Match.DefeatedAttackers == kills);
            stage.Base.Building.transform.position += Vector3.down * 150;
            typeof(SiegeGameplayStage).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(stage, null);
            Check(
                report,
                "a base core lost into the void ends the real host",
                stage.Settled && stage.Match.Phase == SiegePhase.Defeat);
            report.passed = true;
        }
        catch (Exception error)
        {
            report.error = error.ToString();
        }

        return Save(report, "boundary");
    }

    private static string Save(Report report, string name)
    {
        Directory.CreateDirectory("Temp/SiegeIsland");
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Temp/SiegeIsland/" + name + ".json", json);
        return json;
    }

    private static void Check(Report report, string name, bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add(name);
    }
}
