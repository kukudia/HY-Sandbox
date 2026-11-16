// 职责：在真实Play的独立PhysicsScene验证舰船/建筑/体素互伤、归因、去重、连锁及冲量隔离。
// 入口：Run()；只创建自有夹具，不写玩家存档，报告Temp/Combat/explosion-integration.json。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ExplosionIntegrationChecks
{
    [Serializable]
    private sealed class Report
    {
        public List<string> checks = new List<string>();
        public string error;
    }

    public static string Run()
    {
        if (!Application.isPlaying || PlayManager.instance == null || DestroyManager.instance == null)
        {
            throw new InvalidOperationException("Run in Main Play Mode.");
        }

        var report = new Report();
        Scene scene = SceneManager.CreateScene("Explosion integration owned", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        Scene other = SceneManager.CreateScene("Explosion isolation owned", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        bool previous = PlayManager.instance.playMode;
        try
        {
            PlayManager.instance.playMode = true;
            PhysicsSandboxProbe.PrepareSceneEffects(scene);
            WorldBuildingManager manager = Manager(scene);
            Vector3 center = new Vector3(7000f, 1000f, 7000f);
            Block attacker = Ship(scene, center + Vector3.right * 50f);
            ControlUnit attackerUnit = attacker.GetComponentInParent<ControlUnit>();
            attackerUnit.faction = UnitFaction.Player;
            Block source = Ship(scene, center);
            Block shipTarget = Ship(scene, center + Vector3.right * 2f);
            BuildingRuntime building = Building(manager, center + Vector3.forward * 2f);
            Block isolated = Ship(other, center + Vector3.right * 2f);
            Durability health = shipTarget.GetComponent<Durability>();
            health.ProtectFromExplosionCollisions(3f);
            shipTarget.gameObject.AddComponent<BoxCollider>();
            Configure(source, 120f, 6f);
            Physics.SyncTransforms();
            source.GetComponent<Durability>().ApplyDamage(200f, attackerUnit);
            Check(
                report,
                "ship blast damages another ship once despite multiple colliders",
                Mathf.Abs(health.currentDurability - 20f) < .01f);
            Check(report, "collision protection does not block direct blast damage", health.currentDurability < 100f);
            Check(
                report,
                "ship blast damages anchored building",
                Mathf.Abs(building.Blocks[0].GetComponent<Durability>().currentDurability - 20f) < .01f);
            Check(
                report,
                "blast preserves the original attacker for collateral",
                health.LastAttacker == attackerUnit && building.Blocks[0].GetComponent<Durability>().LastAttacker == attackerUnit);
            Check(
                report,
                "another PhysicsScene at identical coordinates receives no damage",
                Mathf.Approximately(isolated.GetComponent<Durability>().currentDurability, 100f));
            scene.GetPhysicsScene().Simulate(.02f);
            other.GetPhysicsScene().Simulate(.02f);
            Check(
                report,
                "another PhysicsScene receives no impulse",
                isolated.GetComponentInParent<Rigidbody>().linearVelocity.sqrMagnitude < .000001f);
            Check(
                report,
                "cross-unit impulse follows the source PhysicsScene",
                shipTarget.GetComponentInParent<Rigidbody>().linearVelocity.magnitude > .001f);
            float once = health.currentDurability;
            DestroyManager.instance.ExplodeBlock(source);
            Check(
                report,
                "repeated source request cannot detonate twice",
                Mathf.Approximately(health.currentDurability, once));
            center += Vector3.right * 100f;
            building = Building(manager, center);
            source = building.Blocks[0];
            shipTarget = Ship(scene, center + Vector3.right * 2f);
            Configure(source, 120f, 6f);
            source.GetComponent<Durability>().ApplyDamage(200f, attackerUnit);
            Check(
                report,
                "building blast damages a ship through the same damage queue",
                Mathf.Abs(shipTarget.GetComponent<Durability>().currentDurability - 20f) < .01f);
            Check(report, "building death removes its source before damage scanning", building.Blocks.Count == 0);
            center += Vector3.right * 100f;
            source = Ship(scene, center);
            Block chain = Ship(scene, center + Vector3.right * 2f);
            shipTarget = Ship(scene, center + Vector3.right * 5f);
            Configure(source, 120f, 4f);
            Configure(chain, 100f, 4f);
            chain.GetComponent<Durability>().currentDurability = 50f;
            source.GetComponent<Durability>().ApplyDamage(200f, attackerUnit);
            Check(
                report,
                "secondary explosive source is queued and destroyed",
                chain.GetComponent<Durability>().currentDurability == 0f);
            Check(
                report,
                "secondary blast reaches a ship outside the first blast",
                shipTarget.GetComponent<Durability>().currentDurability < 100f);
            Check(
                report,
                "secondary blast carries the original attacker",
                shipTarget.GetComponent<Durability>().LastAttacker == attackerUnit);
            TerrainCheck(scene, report);
            return Save(report);
        }
        catch (Exception error)
        {
            report.error = error.ToString();
            Save(report);
            throw;
        }
        finally
        {
            PlayManager.instance.playMode = previous;
            foreach (Scene owned in new[]
            {
                scene,
                other
            }

            )
            {
                foreach (GameObject root in owned.GetRootGameObjects())
                {
                    root.SetActive(false);
                    Object.DestroyImmediate(root);
                }

                SceneManager.UnloadSceneAsync(owned);
            }
        }
    }

    private static void TerrainCheck(Scene scene, Report report)
    {
        var root = new GameObject("Explosion voxel fixture");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = new Vector3(7500f, 1000f, 7000f);
        ProceduralVoxelWorld world = root.AddComponent<ProceduralVoxelWorld>();
        var data = new SerializedObject(world);
        data.FindProperty("dimensions").vector3IntValue = new Vector3Int(16, 16, 16);
        data.FindProperty("cellSize").floatValue = 1f;
        data.FindProperty("chunkSize").intValue = 16;
        data.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
        Vector3Int cell = new Vector3Int(8, 8, 8);
        world.SetCell(cell, 2);
        Block source = Ship(scene, world.CellCenter(cell));
        Configure(source, 150f, 3f);
        source.GetComponent<Durability>().ApplyDamage(200f, null);
        Check(
            report,
            "ship blast damages voxel terrain using the shared blast",
            world.Data.Get(cell) == 0 && world.RemovedCells > 0);
    }

    private static WorldBuildingManager Manager(Scene scene)
    {
        var root = new GameObject("Explosion building owner");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
        WorldBuildingManager manager = root.AddComponent<WorldBuildingManager>();
        var data = new SerializedObject(manager);
        data.FindProperty("initializeOnStart").boolValue = false;
        data.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
        return manager;
    }

    private static BuildingRuntime Building(WorldBuildingManager manager, Vector3 center)
    {
        var blueprint = new BlockDataList
        {
            foundation = new BuildingFoundationData
            {
                width = 4,
                length = 4
            },
            blocks = new List<BlockData>
            {
                new BlockData(Resources.Load<GameObject>("Blocks/1x1x1").GetComponent<Block>())
                {
                    id = Guid.NewGuid().ToString("N"),
                    resourcePath = "Blocks/1x1x1",
                    x = 1,
                    y = 1,
                    z = 1,
                    posX = .5f,
                    posY = .5f,
                    posZ = .5f,
                    rotW = 1f
                }
            }
        };
        BuildingRuntime building = manager.RestoreBuilding(new WorldBuildingState { instanceId = Guid.NewGuid().ToString("N"), blueprintName = "Explosion fixture", blueprint = blueprint, faction = UnitFaction.Enemy, anchored = true, position = center - Vector3.one * .5f, rotation = Quaternion.identity, durability = new[] { 100f } });
        Block block = building.Blocks.Single();
        block.canExplode = false;
        block.GetComponent<Durability>().maxDurability = block.GetComponent<Durability>().currentDurability = 100f;
        return building;
    }

    private static Block Ship(Scene scene, Vector3 position)
    {
        GameObject root = Object.Instantiate(Resources.Load<GameObject>("BlocksParent/BlocksParent"), position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(root, scene);
        ControlUnit unit = root.GetComponent<ControlUnit>();
        unit.enabled = false;
        unit.faction = UnitFaction.Enemy;
        Rigidbody body = root.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = false;
        body.linearDamping = 0f;
        body.mass = 10f;
        GameObject part = Object.Instantiate(Resources.Load<GameObject>("Blocks/1x1x1"), root.transform);
        part.transform.localPosition = Vector3.zero;
        Block block = part.GetComponent<Block>();
        block.canExplode = false;
        Durability health = part.GetComponent<Durability>();
        health.maxDurability = health.currentDurability = 100f;
        unit.RefreshChildren();
        unit.EnsureRuntimeUnitId();
        unit.AssignRuntimeOwnershipToBlocks(true);
        return block;
    }

    private static void Configure(Block block, float damage, float radius)
    {
        block.canExplode = true;
        block.explosionDamage = damage;
        block.explosionRadius = radius;
        block.explosionForce = 500f;
        block.explosionDisconnectProbability = 0f;
    }

    private static void Check(Report report, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add("PASS " + name);
    }

    private static string Save(Report report)
    {
        string text = JsonUtility.ToJson(report, true);
        Directory.CreateDirectory("Temp/Combat");
        File.WriteAllText("Temp/Combat/explosion-integration.json", text);
        return text;
    }
}
