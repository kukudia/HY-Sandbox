// 职责：验证真实机库资产、存档恢复、非原点蓝图适配和物理门安全，不写玩家存档。
// 入口：RunEdit() 同步资产检查；Run() 在 Play 开始跨帧门检查，Result/报告保存最终结果。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class HangarAssetChecks
{
    [Serializable]
    private sealed class Report
    {
        public bool completed;
        public bool passed;
        public List<string> checks = new List<string>();
        public string error;
    }

    private static Report report;
    private static HangarModule subject;
    private static GameObject obstacle;
    private static readonly List<GameObject> owned = new List<GameObject>();
    private static int step;
    private static double deadline;
    public static string Result => report == null ? "Not started" : JsonUtility.ToJson(report, true);

    public static string RunEdit()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("RunEdit requires Edit Mode.");
        }

        var result = new Report();
        var preview = EditorSceneManager.NewPreviewScene();
        GameObject instance = null;
        try
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HangarPrefabAuthoring.PrefabPath);
            Check(result, "saved editable hangar prefab exists", prefab != null);
            Check(
                result,
                "saved world-space resize buttons are present",
                prefab.GetComponentsInChildren<HangarResizeButton>(true).Length == 24);
            Check(
                result,
                "shared hangar quantity configuration is six",
                Resources.Load<HangarLimits>("Hangars/Limits").MaximumCount == 6);
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            HangarModule module = instance.GetComponent<HangarModule>();
            module.RefreshGeometry();
            GeometryChecks(module, result);
            result.completed = true;
            result.passed = true;
        }
        catch (Exception error)
        {
            result.completed = true;
            result.error = error.ToString();
        }
        finally
        {
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }

            EditorSceneManager.ClosePreviewScene(preview);
        }

        Directory.CreateDirectory("Temp/Hangars");
        string json = JsonUtility.ToJson(result, true);
        File.WriteAllText("Temp/Hangars/edit-asset-checks.json", json);
        return json;
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying || Time.timeScale <= 0f || subject != null)
        {
            throw new InvalidOperationException("Run requires unpaused Play Mode and no running hangar probe.");
        }

        report = new Report();
        try
        {
            GameObject prefab = Resources.Load<GameObject>("Hangars/Hangar");
            Check(report, "runtime hangar resource loads", prefab != null);
            GameObject instance = Object.Instantiate(prefab, new Vector3(8000f, 2000f, 8000f), Quaternion.identity);
            instance.name = "Hangar isolated asset probe";
            owned.Add(instance);
            subject = instance.GetComponent<HangarModule>();
            GeometryChecks(subject, report);
            RestoreChecks(subject, report);
            subject.SetCombat(true);
            Physics.SyncTransforms();
            Check(report, "closed physical door blocks entrance", EntranceBlocked());
            subject.SetDoorOpen(true);
            step = 0;
            deadline = EditorApplication.timeSinceStartup + 2.5d;
            EditorApplication.update += Tick;
            Write();
            return Result;
        }
        catch (Exception error)
        {
            Finish(error);
            return Result;
        }
    }

    private static void GeometryChecks(HangarModule module, Report result)
    {
        Block block = module.GetComponent<Block>();
        block.x = 12;
        block.y = 7;
        block.z = 16;
        block.RefreshRuntimeData(true);
        Check(
            result,
            "all three edited dimensions rebuild actual clear space",
            module.InnerSize == new Vector3(10f, 6f, 14f));
        Check(
            result,
            "floor retains half-metre physical thickness",
            Mathf.Approximately(module.transform.Find("Dark Floor").localScale.y, 0.5f));
        Check(
            result,
            "bottom support connector follows edited height",
            Mathf.Approximately(block.connectors[0].localPos.y, -3.5f));
        var ship = new BlockDataList();
        ship.blocks.Add(new BlockData(block) { id = "offset-fitting-ship", x = 10, y = 6, z = 14, posX = 101f, posY = 42f, posZ = -37f, rotX = 0f, rotY = 0f, rotZ = 0f, rotW = 1f });
        Check(result, "equal-capacity ship is accepted despite far blueprint origin", module.Fits(ship));
        Pose dock = module.GetDockPose(ship);
        Vector3 centre = dock.position + dock.rotation * new Vector3(101f, 42f, -37f);
        Check(
            result,
            "offset blueprint is centred in physical bay",
            Vector3.Distance(centre, module.transform.position) < 0.002f);
        ship.blocks[0].x++;
        Check(result, "oversize width is rejected", !module.Fits(ship));
        ship.blocks[0].x--;
        ship.blocks[0].y++;
        Check(result, "oversize height is rejected", !module.Fits(ship));
        ship.blocks[0].y--;
        ship.blocks[0].z++;
        Check(result, "oversize depth is rejected", !module.Fits(ship));
        module.SetCombat(false);
        Check(
            result,
            "editing shows translucent reserved space",
            module.transform.Find("Capacity Volume (Editor)").gameObject.activeSelf);
        Check(result, "editing reserves the full build volume", block.raycastCollider.enabled);
        module.SetCombat(true);
        Check(
            result,
            "combat hides translucent volume and centre label",
            !module.transform.Find("Capacity Volume (Editor)").gameObject.activeSelf && !module.transform.Find("Capacity Label").gameObject.activeSelf);
        Check(
            result,
            "combat disables full-volume collider but retains shell",
            !block.raycastCollider.enabled && module.transform.Find("Dark Floor").GetComponent<Collider>().enabled);
        Check(
            result,
            "combat shows physical external walls",
            module.transform.Find("Left Wall").GetComponent<Renderer>().enabled);
    }

    private static void RestoreChecks(HangarModule module, Report result)
    {
        var blueprint = new BlockDataList
        {
            foundation = new BuildingFoundationData
            {
                width = 32,
                length = 32
            }
        };
        var data = new BlockData(module.GetComponent<Block>());
        data.id = "hangar-roundtrip";
        data.posX = 12f;
        data.posY = data.y * 0.5f;
        data.posZ = 12f;
        data.rotX = 0f;
        data.rotY = 0f;
        data.rotZ = 0f;
        data.rotW = 1f;
        blueprint.blocks.Add(data);
        blueprint = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(blueprint));
        var owner = new GameObject("Hangar isolated restoration probe");
        owner.SetActive(false);
        owned.Add(owner);
        WorldBuildingManager manager = owner.AddComponent<WorldBuildingManager>();
        var state = new WorldBuildingState
        {
            instanceId = "isolated-hangar-probe",
            blueprintName = "Hangar JSON round trip",
            faction = UnitFaction.Player,
            position = new Vector3(8200f, 2000f, 8200f),
            rotation = Quaternion.Euler(0f, 90f, 0f),
            blueprint = blueprint,
            durability = new[]
            {
                1200f
            },
            anchored = true
        };
        BuildingRuntime restored = manager.RestoreBuilding(state);
        HangarModule copy = restored.GetComponentInChildren<HangarModule>(true);
        Check(
            result,
            "production building factory restores hangar after JSON round trip",
            copy != null && copy.InnerSize == module.InnerSize);
        Check(
            result,
            "restored shell uses saved width rather than prefab default",
            Mathf.Approximately(copy.transform.Find("Dark Floor").localScale.x, data.x));
        Check(
            result,
            "restored identity remains tied to building and original block",
            copy.GetComponent<Block>().uniqueId == "isolated-hangar-probe:hangar-roundtrip");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || subject == null)
        {
            Finish(new InvalidOperationException("Play stopped before the door probe completed."));
            return;
        }

        if (EditorApplication.timeSinceStartup < deadline)
        {
            return;
        }

        try
        {
            if (step == 0)
            {
                Check(report, "real fixed-update door animation completes", subject.DoorIsOpen);
                Check(report, "open physical door permits centre passage", !EntranceBlocked());
                obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = "Door safety probe obstruction";
                obstacle.transform.position = subject.transform.TransformPoint(new Vector3(0f, 0f, (subject.GetComponent<Block>().z - 1f) * 0.5f));
                owned.Add(obstacle);
                Physics.SyncTransforms();
                subject.SetDoorOpen(false);
                deadline = EditorApplication.timeSinceStartup + 2d;
                step = 1;
            }
            else if (step == 1)
            {
                Check(report, "occupied doorway prevents physical door closure", subject.DoorIsOpen);
                obstacle.SetActive(false);
                Physics.SyncTransforms();
                deadline = EditorApplication.timeSinceStartup + 2d;
                step = 2;
            }
            else
            {
                Check(
                    report,
                    "door closes when obstruction has cleared",
                    subject.DoorFraction < 0.01f && EntranceBlocked());
                Finish(null);
            }
        }
        catch (Exception error)
        {
            Finish(error);
        }
    }

    private static bool EntranceBlocked()
    {
        Block block = subject.GetComponent<Block>();
        Vector3 origin = subject.transform.TransformPoint(new Vector3(0f, 0f, block.z * 0.5f + 0.2f));
        return Physics.Raycast(origin, -subject.transform.forward, 1.5f, ~0, QueryTriggerInteraction.Ignore);
    }

    private static void Check(Report result, string name, bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }

        result.checks.Add(name);
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        foreach (GameObject item in owned)
        {
            if (item != null)
            {
                item.SetActive(false);
                Object.Destroy(item);
            }
        }

        owned.Clear();
        subject = null;
        obstacle = null;
        if (report != null)
        {
            report.completed = true;
            report.passed = error == null;
            report.error = error?.ToString();
            Write();
        }
    }

    private static void Write()
    {
        Directory.CreateDirectory("Temp/Hangars");
        File.WriteAllText("Temp/Hangars/play-asset-checks.json", Result);
    }
}
