// 职责：在Main Play隔离物理场景逐套验证建筑模型/连接/供电/驻军数据并渲染预览。
// 入口：Run()；仅写Temp，不保存场景或用户数据。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class OutpostContentChecks
{
    public static string Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Run in Play Mode.");
        }

        Directory.CreateDirectory("Temp/OutpostContent");
        // BlockConnections uses default-scene Physics queries. Separate ownership/height,
        // while retaining the production physics scene so connector checks exercise real code.
        Scene scene = SceneManager.CreateScene("OutpostContentChecks");
        var checks = new List<string>();
        try
        {
            var host = new GameObject("Isolated content manager");
            SceneManager.MoveGameObjectToScene(host, scene);
            WorldBuildingManager manager = host.AddComponent<WorldBuildingManager>();
            typeof(WorldBuildingManager).GetField("initializeOnStart", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, false);
            manager.enabled = false;
            foreach (TextAsset asset in Resources.LoadAll<TextAsset>("WorldBlueprints/Buildings"))
            {
                BlockDataList data = JsonUtility.FromJson<BlockDataList>(asset.text);
                WorldBuildingValidation.ValidateBlueprint(data);
                var state = new WorldBuildingState
                {
                    instanceId = Guid.NewGuid().ToString("N"),
                    blueprintName = asset.name,
                    faction = UnitFaction.Enemy,
                    position = Vector3.up * 2000f,
                    blueprint = data,
                    durability = Enumerable.Repeat(float.MaxValue, data.blocks.Count).ToArray()
                };
                BuildingRuntime building = manager.RestoreBuilding(state);
                building.Activate();
                Physics.SyncTransforms();
                Check(checks, asset.name + " restores every authored block", building.Blocks.Count == data.blocks.Count);
                foreach (Block block in building.Blocks)
                {
                    block.CheckConnection();
                    if (block.resourcePath.StartsWith("WorldBlueprints/Parts/", StringComparison.Ordinal))
                    {
                        Check(
                            checks,
                            asset.name + " structure visual matches dimensions: " + block.uniqueId,
                            block.transform.Find("Model").localScale == new Vector3(block.x, block.y, block.z));
                        Check(
                            checks,
                            asset.name + " structure exposes full connection grid: " + block.uniqueId,
                            block.connectors.Count == 2 * (block.x * block.y + block.x * block.z + block.y * block.z));
                    }
                }

                MethodInfo grounded = typeof(BuildingRuntime).GetMethod("HasGroundConnector", BindingFlags.Instance | BindingFlags.NonPublic);
                var connected = new HashSet<Block>(building.Blocks.Where(block => (bool)grounded.Invoke(building, new object[] { block })));
                var queue = new Queue<Block>(connected);
                while (queue.Count > 0)
                {
                    foreach (Block neighbour in queue.Dequeue().Neighbors())
                    {
                        if (connected.Add(neighbour))
                        {
                            queue.Enqueue(neighbour);
                        }
                    }
                }

                string disconnected = string.Join(
                    ", ",
                    building.Blocks.Where(block => !connected.Contains(block)).Select(block => block.name + "@" + block.transform.localPosition));
                Check(
                    checks,
                    asset.name + " all modules connect to foundation: " + disconnected,
                    connected.Count == building.Blocks.Count);
                foreach (Turret turret in building.GetComponentsInChildren<Turret>())
                {
                    turret.GetComponent<Power>().SetWorkRequested(turret, true);
                }

                var network = new PowerNetwork();
                network.Rebuild(
                    building.GetComponentsInChildren<PowerTransmissionDevice>(),
                    building.GetComponentsInChildren<PowerGeneratingUnit>(),
                    building.GetComponentsInChildren<Power>());
                Check(
                    checks,
                    asset.name + " every defensive turret receives power",
                    building.GetComponentsInChildren<Turret>().All(turret => turret.GetComponent<Power>().isWorking));
                Check(
                    checks,
                    asset.name + " capture retains outpost identity",
                    building.Capture().blueprint.IsOutpost == data.IsOutpost);
                if (data.IsOutpost)
                {
                    foreach (OutpostGarrisonData guard in data.outpost.garrison)
                    {
                        Bounds envelope = OutpostBaseDesigner.BoundsOf(guard.blueprint.blocks, Quaternion.Euler(0f, guard.yaw, 0f));
                        envelope.center += guard.position;
                        Check(
                            checks,
                            asset.name + " resident fits above its own base footprint",
                            envelope.min.x >= -2f && envelope.min.z >= -2f && envelope.max.x <= data.foundation.width - 2f && envelope.max.z <= data.foundation.length - 2f && Mathf.Abs(envelope.min.y - 1.15f) < .001f);
                        GameObject resident = BlueprintModelPreview.Create(guard.blueprint.blocks, building.transform, "Preview resident");
                        resident.transform.SetLocalPositionAndRotation(guard.position, Quaternion.Euler(0f, guard.yaw, 0f));
                        Check(
                            checks,
                            asset.name + " solid preview has no gameplay components",
                            resident.GetComponentsInChildren<MeshFilter>().Length > 50 && resident.GetComponentsInChildren<Component>().All(c => c is Transform || c is MeshFilter || c is MeshRenderer));
                        Bounds[] baseParts = data.blocks.Select(part => OutpostBaseDesigner.BoundsOf(new[] { part }, Quaternion.identity)).ToArray();
                        foreach (BlockData part in guard.blueprint.blocks)
                        {
                            Bounds residentPart = OutpostBaseDesigner.BoundsOf(new[] { part }, Quaternion.Euler(0f, guard.yaw, 0f));
                            residentPart.center += guard.position;
                            Check(
                                checks,
                                asset.name + " resident clears base " + part.id,
                                !baseParts.Any(b => b.Intersects(residentPart)));
                        }
                    }
                }

                Render(building, "Temp/OutpostContent/" + asset.name + ".png");
                Object.DestroyImmediate(building.gameObject);
            }

            File.WriteAllText("Temp/OutpostContent/checks.txt", string.Join("\n", checks) + "\nALL PASSED");
            return string.Join("\n", checks);
        }
        catch (Exception error)
        {
            File.WriteAllText("Temp/OutpostContent/checks.txt", string.Join("\n", checks) + "\n" + error);
            throw;
        }
        finally
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            SceneManager.UnloadSceneAsync(scene);
        }
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add("PASS: " + name);
    }

    private static void Render(BuildingRuntime building, string path)
    {
        Bounds bounds = new Bounds(building.transform.position, Vector3.zero);
        foreach (Renderer renderer in building.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(renderer.bounds);
        }

        var cameraObject = new GameObject("Content camera");
        SceneManager.MoveGameObjectToScene(cameraObject, building.gameObject.scene);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.transform.position = bounds.center + new Vector3(1.2f, 0.9f, -1.5f).normalized * bounds.size.magnitude * 1.3f;
        camera.transform.LookAt(bounds.center);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.17f, 0.2f);
        var lightObject = new GameObject("Content light");
        SceneManager.MoveGameObjectToScene(lightObject, building.gameObject.scene);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2f;
        light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        var target = new RenderTexture(960, 720, 24);
        var pixels = new Texture2D(960, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(lightObject);
        }
    }
}
