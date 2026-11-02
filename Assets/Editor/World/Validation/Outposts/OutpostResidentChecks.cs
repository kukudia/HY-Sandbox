// 职责：在隔离真实流式地形验证停机显示、原位激活和击败后不复活的据点生命周期。
// 入口：Run()由建筑编辑Play探针逐帧驱动；finally清理实例，不写用户数据或场景。
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class OutpostResidentChecks
{
    public static IEnumerator Run(Action<string, bool> check)
    {
        var root = new GameObject("Resident lifecycle fixture");
        root.SetActive(false);
        root.transform.position = Vector3.up * 3000f;
        WorldGenerationSettings settings = ScriptableObject.CreateInstance<WorldGenerationSettings>();
        OutpostEncounterDirector director = null;
        ProceduralVoxelWorld world = root.AddComponent<ProceduralVoxelWorld>();
        JsonUtility.FromJsonOverwrite("{\"streaming\":true}", world);
        WorldBuildingManager manager = root.AddComponent<WorldBuildingManager>();
        typeof(WorldBuildingManager).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, world);
        JsonUtility.FromJsonOverwrite("{\"initializeOnStart\":false}", manager);
        manager.enabled = false;
        try
        {
            root.SetActive(true);
            world.InitializeStreaming(settings, 14721);
            for (int z = 0; z < 2; z++)
            {
                for (int x = 0; x < 2; x++)
                {
                    IEnumerator load = world.LoadColumn(new Vector2Int(x, z));
                    while (load.MoveNext())
                    {
                        yield return null;
                    }
                }
            }

            BlockDataList data = JsonUtility.FromJson<BlockDataList>(Resources.Load<TextAsset>("WorldBlueprints/Buildings/CODEX_E01_Finch Outpost").text);
            Vector3 origin = root.transform.position + new Vector3(4f, world.SurfaceHeight(2, 2), 4f);
            BuildingRuntime building = manager.RestoreBuilding(new WorldBuildingState { instanceId = "resident-lifecycle", blueprintName = "Finch base", blueprint = data, position = origin, rotation = Quaternion.identity, faction = UnitFaction.Enemy, durability = Enumerable.Repeat(float.MaxValue, data.blocks.Count).ToArray() });
            building.Activate();
            director = new OutpostEncounterDirector(manager);
            director.Tick(origin + Vector3.up * 500f);
            Transform parked = root.GetComponentsInChildren<Transform>().FirstOrDefault(item => item.name.StartsWith("Parked resident "));
            OutpostGarrisonData resident = data.outpost.garrison[0];
            Vector3 expected = origin + resident.position;
            check(
                "Distant loaded base visibly includes its resident before combat activates",
                parked != null && Vector3.Distance(parked.position, expected) < .001f && director.ActiveCount == 0);
            int visibleMeshes = parked.GetComponentsInChildren<MeshFilter>().Length;
            director.Tick(expected);
            check(
                "Resident activates in authored base pose without terrain lift",
                director.TryGetDefender(building.InstanceId, resident.id, out ControlUnit unit) && Vector3.Distance(unit.transform.position, expected) < .001f);
            check(
                "Activation replaces parked display with the same solid geometry",
                !parked.gameObject.activeSelf && unit.GetComponentsInChildren<MeshFilter>().Length >= visibleMeshes);
            unit.cockpit.GetComponent<EnemyIdentity>().ReportDefeated();
            Object.DestroyImmediate(unit.gameObject);
            director.Tick(expected);
            director.Tick(origin + Vector3.up * 500f);
            director.Tick(expected);
            check(
                "Defeated resident has neither live unit nor parked model after revisiting",
                director.IsDefeated(building.InstanceId, resident.id) && !director.TryGetDefender(building.InstanceId, resident.id, out _) && !root.GetComponentsInChildren<Transform>().Any(item => item.name.StartsWith("Parked resident ") && item.gameObject.activeInHierarchy));
            director.Clear();
            director.Tick(origin + Vector3.up * 500f);
            check(
                "Fresh encounter restores the parked resident without permanent defeat data",
                root.GetComponentsInChildren<Transform>().Any(item => item.name.StartsWith("Parked resident ") && item.gameObject.activeInHierarchy));
            manager.RemoveBuilding(building);
            director.Tick(expected);
            check(
                "Unloaded base cannot create an orphan resident",
                director.ActiveCount == 0 && !root.GetComponentsInChildren<Transform>().Any(item => item.name.StartsWith("Parked resident ") && item.gameObject.activeInHierarchy));
        }
        finally
        {
            director?.Clear();
            world.ClearStreaming();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(settings);
        }
    }
}
