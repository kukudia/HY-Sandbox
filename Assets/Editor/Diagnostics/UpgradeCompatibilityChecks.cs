// 职责：验证引擎升级后的场景查询、VFX 制作桥接和受影响 FBX 材质迁移。
// 入口：CheckSceneQueries() / CheckVfxBridge() / MigrateExhaustImporter()；结果写 Logs/UpgradeFollowup。
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class UpgradeCompatibilityChecks
{
    private const string Output = "Logs/UpgradeFollowup";
    // Serialized pre-6.6 value; the named External enum member is itself obsolete.
    private const ModelImporterMaterialLocation LegacyExternalLocation = (ModelImporterMaterialLocation)0;
    public static object CheckInputLifecycle()
    {
        Require(EditorApplication.isPlaying, "Input checks require Play Mode");
        var asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
        Keyboard keyboard = null;
        Gamepad pad = null;
        try
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            pad = InputSystem.AddDevice<Gamepad>();
            asset.devices = new InputDevice[]
            {
                keyboard,
                pad
            };
            InputAction move = asset.FindAction("Player/Move", true);
            asset.Enable();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.Update();
            Require(move.ReadValue<Vector2>().y > 0.9f, "Project keyboard binding");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0.8f, 0f) });
            InputSystem.Update();
            Require(move.ReadValue<Vector2>().x > 0.5f, "Project gamepad binding");
            for (int index = 0; index < 3; index++)
            {
                move.actionMap.Disable();
                Require(move.ReadValue<Vector2>() == Vector2.zero, "Disabled action releases state");
                move.actionMap.Enable();
                InputSystem.Update();
                Require(move.ReadValue<Vector2>().x > 0.5f, "Reenabled action restores device state");
            }

            InputSystem.RemoveDevice(pad);
            pad = InputSystem.AddDevice<Gamepad>();
            asset.devices = new InputDevice[]
            {
                keyboard,
                pad
            };
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(-0.8f, 0f) });
            InputSystem.Update();
            Require(move.ReadValue<Vector2>().x < -0.5f, "Reconnected gamepad resolves bindings");
            Require(
                SceneObjectQuery.FindInActiveScene<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null,
                "Scene UI Input System module");
            return Write(
                "input-" + SceneManager.GetActiveScene().name,
                new
{
    passed = true,
    checks = 10,
    scope = "Isolated clone of project actions with synthetic keyboard/gamepad; physical devices and end-to-end UI focus excluded."
});
        }
        finally
        {
            asset.Disable();
            Object.DestroyImmediate(asset);
            if (keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
            }

            if (pad != null && pad.added)
            {
                InputSystem.RemoveDevice(pad);
            }
        }
    }

    public static object EnableVfxMeshReadability()
    {
        string root = NativeVfxLibraryBuilder.Root + "/UNI/Realistic Explosions, Fire & Smoke/Models/";
        string[] paths =
        {
            root + "uni_firestreak.fbx",
            root + "uni_shockwave_ground.fbx",
            root + "uni_third.fbx"
        };
        Directory.CreateDirectory(Output);
        foreach (string path in paths)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            string before = VfxMeshSnapshot(path);
            if (!importer.isReadable)
            {
                string backup = Output + "/" + Path.GetFileName(path) + "-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff") + ".meta.backup";
                File.Copy(path + ".meta", backup);
                importer.isReadable = true;
                importer.SaveAndReimport();
                Require(before == VfxMeshSnapshot(path), "Readable mesh preserves identity and geometry: " + path);
            }

            Require(
                AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().All(mesh => mesh.isReadable),
                "VFX mesh readability");
        }

        return Write("vfx-readable-meshes", new
        {
            passed = true,
            paths
        });
    }

    private static string VfxMeshSnapshot(string path)
    {
        return JsonConvert.SerializeObject(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Select(mesh => new { reference = Reference(mesh), mesh.vertexCount, bounds = mesh.bounds.ToString("F6"), indices = Enumerable.Range(0, mesh.subMeshCount).Select(index => mesh.GetIndexCount(index)).ToArray() }).ToArray());
    }

    public static object CheckSceneQueries()
    {
        Require(EditorApplication.isPlaying, "Scene-query lifecycle checks require Play Mode");
        Scene original = SceneManager.GetActiveScene();
        Scene first = SceneManager.CreateScene("Query first");
        Scene second = SceneManager.CreateScene("Query second");
        var a = new GameObject("Owner A");
        var b = new GameObject("Owner B");
        SceneManager.MoveGameObjectToScene(a, first);
        SceneManager.MoveGameObjectToScene(b, second);
        try
        {
            Require(SceneObjectQuery.FindInScene<Transform>(first) == a.transform, "Explicit scene isolation");
            SceneManager.SetActiveScene(second);
            Require(SceneObjectQuery.FindInActiveScene<Transform>() == b.transform, "Active scene ownership");
            b.SetActive(false);
            Require(SceneObjectQuery.FindInActiveScene<Transform>() == null, "Inactive exclusion");
            Require(
                SceneObjectQuery.FindInActiveScene<Transform>(FindObjectsInactive.Include) == b.transform,
                "Inactive inclusion");
            b.SetActive(true);
            SceneManager.MoveGameObjectToScene(b, first);
            bool duplicateRejected = false;
            try
            {
                SceneObjectQuery.FindInScene<Transform>(first);
            }
            catch (InvalidOperationException error)
            {
                duplicateRejected = error.Message.Contains("multiple Transform");
            }

            Require(duplicateRejected, "Duplicate owner rejected");
            Require(SceneObjectQuery.FindInScene<Transform>(default) == null, "Invalid scene is absent");
            return Write("queries-play", new
            {
                passed = true,
                checks = 6
            });
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            SceneManager.UnloadSceneAsync(first);
            SceneManager.UnloadSceneAsync(second);
        }
    }

    public static object CheckVfxBridge()
    {
        const string scratch = "Assets/Editor/Diagnostics/UpgradeVfxScratch";
        Require(!AssetDatabase.IsValidFolder(scratch), "Scratch path must be unused");
        AssetDatabase.CreateFolder("Assets/Editor/Diagnostics", "UpgradeVfxScratch");
        string[] sources = AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { NativeVfxLibraryBuilder.Root }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        try
        {
            for (int index = 0; index < sources.Length; index++)
            {
                string copy = scratch + "/" + index + ".vfx";
                Require(AssetDatabase.CopyAsset(sources[index], copy), "Copy graph " + sources[index]);
                object graph = VfxGraphAuthoring.Graph(copy);
                Require(VfxGraphAuthoring.Items(graph, "children").Length > 0, "Graph children");
                VfxGraphAuthoring.Describe(copy);
                object probe = VfxGraphAuthoring.Parameter(graph, "UpgradeCompatibilityProbe", 0.375f);
                // Exercise block creation, settings and slot linking only on disposable copies.
                object output = VfxGraphAuthoring.Items(graph, "children").First(child => child.GetType().GetProperty("contextType") != null && VfxGraphAuthoring.Get(child, "contextType").ToString() == "Output");
                object alpha = VfxGraphAuthoring.Attribute(output, "alpha", 1f, true);
                VfxGraphAuthoring.Value(
                    alpha,
                    (string)VfxGraphAuthoring.Get(VfxGraphAuthoring.Items(alpha, "inputSlots")[0], "name"),
                    0.5f);
                VfxGraphAuthoring.Link(probe, VfxGraphAuthoring.Items(alpha, "inputSlots")[0]);
                VfxGraphAuthoring.Save(copy);
                object reloaded = VfxGraphAuthoring.Graph(copy);
                object parameter = VfxGraphAuthoring.Items(reloaded, "children").Single(child => child.GetType().Name == "VFXParameter" && (string)VfxGraphAuthoring.Get(child, "exposedName") == "UpgradeCompatibilityProbe");
                Require(Math.Abs((float)VfxGraphAuthoring.Get(parameter, "value") - 0.375f) < 0.001f, "Saved parameter");
                Require(
                    AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(copy) != null && VfxGraphAuthoring.Contents(copy).Length > 0,
                    "Compiled graph resource");
            }

            Require(sources.Length > 0, "At least one graph tested");
            return Write("vfx-bridge", new
            {
                passed = true,
                graphs = sources
            });
        }
        finally
        {
            AssetDatabase.DeleteAsset(scratch);
        }
    }

    public static object MigrateExhaustImporter()
    {
        const string path = "Assets/Art/SpaceKit/Models/Cube/FBX/FX Exhaust.FBX";
        var external = AssetDatabase.FindAssets("t:Model").Select(AssetDatabase.GUIDToAssetPath).Where(asset => asset.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)).Select(asset => (ModelImporter)AssetImporter.GetAtPath(asset)).Where(importer => importer.materialLocation == LegacyExternalLocation).Select(importer => new { importer.assetPath, importer.materialImportMode }).ToArray();
        Write("fbx-audit-before", external);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        string before = ModelSnapshot(path);
        if (importer.materialLocation == LegacyExternalLocation)
        {
            Directory.CreateDirectory(Output);
            string backup = Output + "/exhaust-importer-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff") + ".meta.backup";
            File.Copy(path + ".meta", backup);
            // Preserve the resolved external material explicitly before removing legacy search-on-import.
            foreach (Material material in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>(true).SelectMany(renderer => renderer.sharedMaterials).Distinct())
            {
                Require(material != null && AssetDatabase.GetAssetPath(material) != path, "Resolved external material");
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), material);
            }

            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();
            if (before != ModelSnapshot(path))
            {
                File.Copy(backup, path + ".meta", true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                throw new InvalidOperationException("FBX migration changed resolved mesh/material references; restored importer backup.");
            }
        }

        return Write(
            "fbx-migration",
            new
{
    passed = true,
    path,
    externalAudited = external.Length,
    externalWithMaterialImport = external.Count(item => item.materialImportMode != ModelImporterMaterialImportMode.None),
    unchangedMeshAndMaterialReferences = before == ModelSnapshot(path),
    snapshot = JsonConvert.DeserializeObject(before)
});
    }

    private static string ModelSnapshot(string path)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return JsonConvert.SerializeObject(new
        {
            guid = AssetDatabase.AssetPathToGUID(path),
            renderers = model.GetComponentsInChildren<Renderer>(true).Select(renderer => new { renderer.name, materials = renderer.sharedMaterials.Select(Reference).ToArray() }).ToArray(),
            meshes = model.GetComponentsInChildren<MeshFilter>(true).Select(filter => new { reference = Reference(filter.sharedMesh), filter.sharedMesh.vertexCount, indices = filter.sharedMesh.triangles.Length, bounds = filter.sharedMesh.bounds.ToString("F6") }).ToArray()
        });
    }

    private static object Reference(Object asset)
    {
        Require(asset != null, "Asset reference");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);
        return new
        {
            guid,
            localId
        };
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }
    }

    private static object Write(string name, object result)
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/" + name + ".json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }
}
