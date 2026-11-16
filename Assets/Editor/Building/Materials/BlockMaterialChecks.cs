// 职责：验证真实材质资产、存档恢复、旧蓝图兼容、健康比例、表现与有界碎片。
// 入口：Run()；Main Play中创建自有夹具，不保存玩家蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BlockMaterialChecks
{
    [Serializable]
    private sealed class Report
    {
        public List<string> checks = new List<string>();
        public string error;
    }

    public static string Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Run in Play Mode.");
        }

        var report = new Report();
        var root = new GameObject("Material validation owned");
        root.transform.position = new Vector3(9000, 9000, 9000);
        var effects = new List<MaterialBreakEffect>();
        try
        {
            var catalog = BlockMaterialCatalog.Instance;
            Check(
                report,
                catalog != null && catalog.Materials.Length == 4,
                "Four authored material definitions loaded");
            var prefab = Resources.Load<GameObject>("Blocks/2x2x1");
            Block template = prefab.GetComponent<Block>();
            var legacy = new BlockData(template)
            {
                id = "legacy",
                resourcePath = "Blocks/2x2x1",
                materialId = null,
                posX = 9000,
                posY = 9000,
                posZ = 9000
            };
            Block old = Restore(legacy, root.transform);
            Check(
                report,
                Mathf.Approximately(old.mass, template.mass) && old.cost == template.cost && old.GetComponent<Durability>().maxDurability == template.GetComponent<Durability>().maxDurability,
                "Missing material preserves original mass cost and durability");
            foreach (var definition in catalog.Materials)
            {
                Block block = Restore(legacy, root.transform);
                block.GetComponent<BlockMaterial>().SetMaterial(definition.id);
                Check(
                    report,
                    Mathf.Approximately(block.mass, 4 * definition.density) && block.cost == 4 * definition.coinsPerCell && block.GetComponent<Durability>().maxDurability == 4 * definition.durabilityPerCell,
                    definition.id + " derives mass health and price from volume");
                Check(
                    report,
                    block.transform.Find("Model").GetComponentInChildren<MeshRenderer>().sharedMaterial == definition.surface,
                    definition.id + " binds the authored render material");
                var saved = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(new BlockData(block)));
                Block restored = Restore(saved, root.transform);
                Check(
                    report,
                    restored.GetComponent<BlockMaterial>().MaterialId == definition.id && Mathf.Approximately(restored.mass, block.mass) && restored.GetComponent<Durability>().currentDurability == restored.GetComponent<Durability>().maxDurability,
                    definition.id + " JSON factory roundtrip restores full design health and material");
                Durability health = restored.GetComponent<Durability>();
                health.currentDurability = health.maxDurability * .4f;
                restored.RefreshRuntimeData();
                Check(
                    report,
                    Mathf.Abs(health.currentDurability / health.maxDurability - .4f) < .0001f,
                    definition.id + " refresh never grants free repair");
            }

            var glass = catalog.Find("glass");
            Check(
                report,
                glass.surface.renderQueue == 3000 && glass.surface.GetFloat("_Surface") == 1 && glass.breakEffect.GetComponent<ParticleSystemRenderer>().mesh.vertexCount == 4,
                "Glass uses transparent URP and thin mesh shards");
            Block active = Restore(legacy, root.transform);
            active.GetComponent<BlockMaterial>().SetMaterial("glass");
            MaterialBreakEffect.Play(glass.breakEffect, active);
            MaterialBreakEffect spawned = Object.FindObjectsByType<MaterialBreakEffect>().First();
            effects.Add(spawned);
            Check(
                report,
                spawned.GetComponent<ParticleSystem>().isPlaying && spawned.GetComponent<Collider>() == null && spawned.GetComponent<Rigidbody>() == null,
                "Glass fragments actually play and have no gameplay physics");
            for (int i = 0; i < 40; i++)
            {
                MaterialBreakEffect.Play(glass.breakEffect, active);
            }

            effects = Object.FindObjectsByType<MaterialBreakEffect>().ToList();
            Check(
                report,
                MaterialBreakEffect.ActiveCount == 32 && effects.Count == 32,
                "Material burst budget caps concurrent cosmetic instances at32");
            GameObject preview = BlueprintModelPreview.Create(new[] { new BlockData(active) }, root.transform, "Material preview owned");
            Check(
                report,
                preview.GetComponentsInChildren<MeshRenderer>().All(r => r.sharedMaterial == glass.surface),
                "Static blueprint preview retains selected material");
            var unsupported = new BlockData(Resources.Load<GameObject>("Blocks/MainThruster").GetComponent<Block>())
            {
                resourcePath = "Blocks/MainThruster",
                materialId = "glass"
            };
            bool refused = false;
            try
            {
                Restore(unsupported, root.transform);
            }
            catch (ArgumentException)
            {
                refused = true;
            }

            Check(report, refused, "Unsupported functional module material rejected before instantiation");
            var invalid = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(legacy));
            invalid.materialId = "future-unknown";
            refused = false;
            try
            {
                Restore(invalid, root.transform);
            }
            catch (ArgumentException)
            {
                refused = true;
            }

            Check(report, refused, "Unknown material rejected rather than silently repricing");
            var inactive = new GameObject("Inactive restore owned");
            inactive.transform.SetParent(root.transform, false);
            inactive.SetActive(false);
            var delayed = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(legacy));
            delayed.materialId = "wood";
            delayed.x = 4;
            Block deferred = Restore(delayed, inactive.transform);
            inactive.SetActive(true);
            Check(
                report,
                Mathf.Approximately(deferred.mass, 8 * catalog.Find("wood").density) && deferred.GetComponent<Durability>().maxDurability == 8 * catalog.Find("wood").durabilityPerCell,
                "Delayed CSV Awake retains restored material and custom dimensions");
            var managerObject = new GameObject("Material building manager owned");
            managerObject.SetActive(false);
            managerObject.transform.SetParent(root.transform, false);
            WorldBuildingManager manager = managerObject.AddComponent<WorldBuildingManager>();
            using (var settings = new UnityEditor.SerializedObject(manager))
            {
                settings.FindProperty("initializeOnStart").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            managerObject.SetActive(true);
            var design = new BlockDataList
            {
                foundation = new BuildingFoundationData(),
                siegeCoreId = "wood-module"
            };
            design.blocks.Add(new BlockData(template) { id = "wood-module", resourcePath = "Blocks/2x2x1", x = 2, y = 2, z = 1, posX = 0, posY = 1, posZ = 0, rotX = 0, rotY = 0, rotZ = 0, rotW = 1, materialId = "wood" });
            var state = new WorldBuildingState
            {
                instanceId = Guid.NewGuid().ToString("N"),
                blueprintName = "Material owned",
                blueprint = design,
                durability = new[]
                {
                    17f
                },
                faction = UnitFaction.Player,
                position = new Vector3(9500, 9500, 9500),
                rotation = Quaternion.identity,
                anchored = true
            };
            BuildingRuntime building = manager.RestoreBuilding(state);
            Check(
                report,
                building.Blocks.Count == 1 && building.Blocks[0].GetComponent<Durability>().currentDurability == 17f && building.Blocks[0].GetComponent<BlockMaterial>().MaterialId == "wood",
                "Real building restore preserves damaged health and authored material");
            WorldBuildingState capture = building.Capture();
            Check(
                report,
                capture.blueprint.blocks[0].materialId == "wood" && capture.durability[0] == 17f,
                "Building snapshot captures material and damage independently");
            var construction = ScriptableObject.CreateInstance<SiegeConstructionSettings>();
            try
            {
                Check(
                    report,
                    construction.Cost(design) == 4 * catalog.Find("wood").coinsPerCell,
                    "Siege quote uses selected material volume price");
            }
            finally
            {
                Object.DestroyImmediate(construction);
            }

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
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    Object.DestroyImmediate(effect.gameObject);
                }
            }

            Object.DestroyImmediate(root);
        }
    }

    private static Block Restore(BlockData data, Transform parent)
    {
        object[] args =
        {
            data,
            parent,
            UnitFaction.Player,
            null
        };
        bool restored;
        try
        {
            restored = (bool)typeof(Block).Assembly.GetType("BlueprintBlockFactory").GetMethod("TryRestore").Invoke(null, args);
        }
        catch (System.Reflection.TargetInvocationException error) when (error.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }

        if (!restored)
        {
            throw new InvalidOperationException("Failed restore.");
        }

        return (Block)args[3];
    }

    private static void Check(Report report, bool valid, string description)
    {
        if (!valid)
        {
            throw new InvalidOperationException(description);
        }

        report.checks.Add(description);
    }

    private static string Save(Report report)
    {
        Directory.CreateDirectory("Temp/Building");
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Temp/Building/material-checks.json", json);
        return json;
    }
}
