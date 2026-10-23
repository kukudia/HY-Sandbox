// 职责：用真实两层建筑夹具测量完整支撑与单次范围损毁，保留生产同步开销与正确性计数。
// 入口：Start() / Status；远离战场、无物理步，不代表大规模交火帧率。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeBuildingBenchmark
{
    public static string Status { get; private set; } = "Idle";

    private static bool running;
    public static string Start(string label = "geometry-baseline")
    {
        if (running || !EditorApplication.isPlaying || PlayManager.instance == null || PlayManager.instance.playMode)
        {
            throw new InvalidOperationException("Requires idle Siege deployment Play and no running benchmark.");
        }

        running = true;
        Status = "Scheduled";
        double readyAt = EditorApplication.timeSinceStartup + 1.0;
        EditorApplication.CallbackFunction scheduled = null;
        scheduled = () =>
        {
            if (EditorApplication.timeSinceStartup < readyAt)
            {
                return;
            }

            EditorApplication.update -= scheduled;
            Execute(label);
        };
        EditorApplication.update += scheduled;
        return Status;
    }

    private static void Execute(string label)
    {
        SiegeSession siege = PlayManager.instance.SiegeSession;
        bool metrics = SimulationWorkMetrics.Enabled;
        try
        {
            PlayManager.instance.BindSiege(null);
            var report = new Report
            {
                label = label,
                utc = DateTime.UtcNow.ToString("O"),
                unity = Application.unityVersion
            };
            foreach (int count in new[]
            {
                100,
                256,
                1024
            }

            )
            {
                for (int repeat = 0; repeat < 4; repeat++)
                {
                    Sample sample = Measure(count);
                    if (repeat > 0)
                    {
                        report.samples.Add(sample);
                    }
                }
            }

            Directory.CreateDirectory("Temp/Siege");
            string path = "Temp/Siege/building-benchmark-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json";
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Status = path;
        }
        catch (Exception error)
        {
            Status = "Failed: " + error.Message;
            UnityEngine.Debug.LogException(error);
        }
        finally
        {
            SimulationWorkMetrics.Enabled = metrics;
            PlayManager.instance.BindSiege(siege);
            running = false;
        }
    }

    private static Sample Measure(int count)
    {
        var owner = new GameObject("Owned building CPU fixture");
        owner.SetActive(false);
        WorldBuildingManager manager = owner.AddComponent<WorldBuildingManager>();
        var config = new SerializedObject(manager);
        config.FindProperty("initializeOnStart").boolValue = false;
        config.ApplyModifiedPropertiesWithoutUndo();
        try
        {
            owner.SetActive(true);
            int width = count == 100 ? 10 : count == 256 ? 16 : 32;
            int length = count / width / 2;
            var design = new BlockDataList
            {
                foundation = new BuildingFoundationData
                {
                    width = width,
                    length = length
                }
            };
            Block prefab = Resources.Load<GameObject>("Blocks/1x1x1").GetComponent<Block>();
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < length; z++)
                    {
                        var data = new BlockData(prefab)
                        {
                            resourcePath = "Blocks/1x1x1",
                            id = y + ":" + x + ":" + z,
                            x = 1,
                            y = 1,
                            z = 1,
                            posX = x - 1.5f,
                            posY = y + 0.5f,
                            posZ = z - 1.5f,
                            rotX = 0f,
                            rotY = 0f,
                            rotZ = 0f,
                            rotW = 1f,
                            cargo = null
                        };
                        design.blocks.Add(data);
                    }
                }
            }

            BuildingRuntime building = manager.RestoreBuilding(new WorldBuildingState { instanceId = Guid.NewGuid().ToString("N"), blueprintName = "CPU fixture", faction = UnitFaction.Player, position = new Vector3(10000f, 1000f, 0f), rotation = Quaternion.identity, anchored = true, blueprint = design, durability = Enumerable.Repeat(float.MaxValue, count).ToArray() });
            foreach (Block block in building.Blocks)
            {
                block.canExplode = false;
            }

            Physics.SyncTransforms();
            SimulationWorkMetrics.Reset();
            SimulationWorkMetrics.Enabled = true;
            var timer = Stopwatch.StartNew();
            PlayManager.instance.RefreshGroup(building.GetComponent<ControlUnit>());
            timer.Stop();
            if (building.Blocks.Count != count || !building.Anchored)
            {
                throw new InvalidOperationException("Intact building support mismatch at " + count);
            }

            var sample = new Sample
            {
                blocks = count,
                intactMs = timer.Elapsed.TotalMilliseconds,
                intactSupport = SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingSupport),
                intactQueries = SimulationWorkMetrics.PhysicsNeighborQueries
            };
            SimulationWorkMetrics.Reset();
            Vector3 center = building.transform.TransformPoint(new Vector3(width / 2f - 1.5f, 1f, length / 2f - 1.5f));
            timer.Restart();
            manager.DamageSphere(center, 2.6f, 10000f);
            timer.Stop();
            sample.damageMs = timer.Elapsed.TotalMilliseconds;
            sample.survivors = building.Blocks.Count;
            sample.support = SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingSupport);
            sample.damage = SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingDamage);
            sample.damageQueries = SimulationWorkMetrics.PhysicsNeighborQueries;
            if (sample.survivors >= count || sample.survivors == 0 || !building.Anchored)
            {
                throw new InvalidOperationException("Blast fixture did not retain a damaged anchored building.");
            }

            return sample;
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Serializable]
    private sealed class Report
    {
        public string methodology = "Editor Play, two-layer 1m-block building on virtual foundation at remote coordinates. One warmup plus three fresh fixtures per size. Timed intact production RefreshGroup and one radius2.6 damage10000 blast, including production VFX requests/sync/splitting. Creation, validation, cleanup excluded. No physics steps. Support is nested in damage, never sum. Not battle FPS. Siege damage gate temporarily unbound only during synchronous fixture, restored in finally.";
        public string label;
        public string utc;
        public string unity;
        public List<Sample> samples = new List<Sample>();
    }

    [Serializable]
    private sealed class Sample
    {
        public int blocks;
        public int survivors;
        public double intactMs;
        public double damageMs;
        public long intactQueries;
        public long damageQueries;
        public SimulationWorkMetrics.Counters intactSupport;
        public SimulationWorkMetrics.Counters support;
        public SimulationWorkMetrics.Counters damage;
    }
}
