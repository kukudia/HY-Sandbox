// 职责：验证显式组件迁移的资产完整性、场景入口及真实 Play 生命周期。
// 入口：AuditAssets() / RunPlay()；不写玩家存档，隔离夹具结束后恢复活动场景。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class EditableComponentChecks
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const string ReportFolder = "Temp/EditableComponentsChecks";
    private static Rigidbody fallingDebris;
    private static float debrisStartHeight;
    public static string RunWorkshop()
    {
        SiegeWorkshop workshop = SceneObjectQuery.FindInActiveScene<SiegeWorkshop>();
        Require(Application.isPlaying && workshop != null && workshop.Open(), "Workshop opens in preparation");
        var checks = new List<string>();
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            Require(
                SceneObjectQuery.FindInScene<VfxEffectPool>(scene) != null && SceneObjectQuery.FindInScene<TransientEffectPool>(scene) != null && SceneObjectQuery.FindInScene<IndustrialMotionDriver>(scene) != null && VisualEffectsManager.EnsureInstance() != null,
                "Runtime workshop scene instantiates its authored services");
            checks.Add("PASS workshop opens with scene-owned VFX, transient effects, motion and visual effects services");
        }
        finally
        {
            Require(workshop.Close(), "Workshop closes and restores battle view");
        }

        checks.Add("PASS workshop closes and restores original scene and UI without saving player data");
        return WriteReport("workshop", checks);
    }

    public static string RunWorldPlay()
    {
        Require(
            Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/Scenes/WorldLab.unity",
            "WorldLab Play required");
        var checks = new List<string>();
        ProceduralVoxelWorld world = SceneObjectQuery.FindInActiveScene<ProceduralVoxelWorld>();
        world.Generate();
        VoxelWorldChunk[] chunks = world.GetComponentsInChildren<VoxelWorldChunk>();
        Require(chunks.Length > 0, "Generated chunks exist");
        foreach (VoxelWorldChunk chunk in chunks)
        {
            Mesh mesh = chunk.GetComponent<MeshFilter>().sharedMesh;
            Require(
                mesh != null && (mesh.vertexCount == 0 || chunk.GetComponent<MeshCollider>().sharedMesh == mesh),
                "Chunk prefab retains render and physics composition");
        }

        checks.Add("PASS terrain generation instantiates chunk templates with valid render/collision meshes");
        VoxelStructureCollapse collapse = world.GetComponent<VoxelStructureCollapse>();
        Require(collapse != null, "Authored collapse component exists");
        collapse.enabled = false;
        var first = new Vector3Int(4, 22, 4);
        var second = new Vector3Int(4, 23, 4);
        world.SetCell(first, 3);
        world.SetCell(second, 3);
        world.FlushDirty();
        Physics.SyncTransforms();
        collapse.ResolveDetached();
        Require(world.Data.Get(first) == 0 && world.Data.Get(second) == 0, "Detached cells removed from source");
        foreach (VoxelDebrisPiece piece in Object.FindObjectsByType<VoxelDebrisPiece>())
        {
            if (Vector3.Distance(piece.transform.position, world.CellCenter(first) - Vector3.one * world.CellSize * 0.5f) < 0.01f)
            {
                Mesh mesh = piece.GetComponent<MeshFilter>().sharedMesh;
                Require(
                    piece.GetComponents<BoxCollider>().Length == 2 && mesh.triangles.Length == 36 && mesh.bounds.size == new Vector3(world.CellSize, world.CellSize * 2f, world.CellSize),
                    "Two-cell fragment preserves exact geometry and compound collision");
                fallingDebris = piece.GetComponent<Rigidbody>();
                debrisStartHeight = piece.transform.position.y;
                Require(
                    Vector3.Distance(fallingDebris.position, piece.transform.position) < 0.001f,
                    "Prefab rigidbody starts at the requested world pose before its first physics step");
                Require(!fallingDebris.isKinematic && fallingDebris.useGravity, "Debris remains physical");
            }
        }

        Require(fallingDebris != null && world.PendingChunks == 0, "Source collision updated before simulation");
        checks.Add("PASS detached voxel template preserves compound colliders, culled internal faces and source collision removal");
        return WriteReport("world", checks);
    }

    public static string VerifyWorldMotion()
    {
        Require(
            fallingDebris != null && fallingDebris.position.y < debrisStartHeight - 0.05f,
            "Instantiated fragment falls in real PhysX frames");
        return WriteReport("world-motion", new List<string> { "PASS authored voxel debris falls across real Play frames" });
    }

    public static string AuditAssets()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Audit saved assets in Edit Mode.");
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var checks = new List<string>();
        int prefabs = 0;
        int blocks = 0;
        int scenes = 0;
        try
        {
            var library = new SerializedObject(RuntimeObjectTemplates.Instance);
            SerializedProperty property = library.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Require(property.objectReferenceValue != null, "Template reference: " + property.name);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources", "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                CheckTree(root, path);
                blocks += root.GetComponentsInChildren<Block>(true).Length;
                prefabs++;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes", "Assets/Art/Industrial/Preview" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    CheckTree(root, path);
                }

                Require(SceneObjectQuery.FindInScene<IndustrialMotionDriver>(scene) != null, path + " motion driver");
                Require(SceneObjectQuery.FindInScene<GlobalTextStyler>(scene) != null, path + " typography");
                Require(SceneObjectQuery.FindInScene<TurretProjectilePool>(scene) != null, path + " projectile pool");
                Require(SceneObjectQuery.FindInScene<TransientEffectPool>(scene) != null, path + " transient pool");
                VfxEffectPool pool = SceneObjectQuery.FindInScene<VfxEffectPool>(scene);
                Require(
                    pool != null && new SerializedObject(pool).FindProperty("_inactiveRoot").objectReferenceValue != null,
                    path + " VFX pool root");
                DebugManager debug = SceneObjectQuery.FindInScene<DebugManager>(scene);
                Require(
                    debug != null && debug.GetComponent<DurabilityDebugLabels>() != null && debug.StatusIconRoot != null,
                    path + " debug entry");
                Require(!debug.gameObject.hideFlags.HasFlag(HideFlags.DontSave), path + " visible debug host");
                foreach (SiegeThreatMarkers markers in Object.FindObjectsByType<SiegeThreatMarkers>(FindObjectsInactive.Include))
                {
                    if (markers.gameObject.scene == scene)
                    {
                        Require(markers.transform.Find("Threat Marker 1/Distance") != null, path + " saved threat markers");
                    }
                }

                checks.Add("PASS " + path);
                scenes++;
            }

            checks.Add($"PASS {prefabs} owned prefabs, {blocks} blocks, {scenes} saved scenes and all object template references");
            return WriteReport("assets", checks);
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static void CheckTree(GameObject root, string path)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            Require(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                path + " missing script: " + child.name);
        }

        foreach (Block block in root.GetComponentsInChildren<Block>(true))
        {
            Require(
                block.GetComponent<RuntimeUnitMember>() != null && block.GetComponent<WreckSalvage>() != null && block.GetComponent<Durability>() != null && block.GetComponent<BoxCollider>() != null,
                path + " block dependencies");
        }

        foreach (ControlUnit unit in root.GetComponentsInChildren<ControlUnit>(true))
        {
            Require(unit.GetComponent<EnemyController>() != null, path + " authored AI");
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            Require(
                body.GetComponent<FragmentLifetime>() != null && body.GetComponent<DetachedPartSmokeTrail>() != null && body.GetComponent<AbnormalImpulseBodyProbe>() != null,
                path + " fragment/diagnostic dependencies");
        }

        foreach (Cockpit cockpit in root.GetComponentsInChildren<Cockpit>(true))
        {
            Require(cockpit.GetComponent<EnemyIdentity>() != null, path + " cockpit identity");
        }
    }

    public static string RunPlay()
    {
        if (!Application.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Run in Main or Siege Play Mode.");
        }

        var checks = new List<string>();
        Scene previous = SceneManager.GetActiveScene();
        TurretProjectilePool productionPool = SceneObjectQuery.FindInScene<TurretProjectilePool>(previous);
        VfxEffectPool productionVfx = SceneObjectQuery.FindInScene<VfxEffectPool>(previous);
        Require(productionPool != null && productionVfx != null, "Scene-owned pools exist before first use");
        checks.Add(AiUpgradePlayChecks.Run());
        checks.Add(SiegeFleetChecks.Run());
        Scene fixture = SceneManager.CreateScene("Editable components isolated checks", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        GameObject services = null;
        Rigidbody debris = null;
        try
        {
            SceneManager.SetActiveScene(fixture);
            checks.Add(CombatVfxChecks.RunPlay());
            checks.Add(VfxPoolChecks.RunPlay());
            services = Object.Instantiate(RuntimeObjectTemplates.Instance.WorkshopLight).gameObject;
            services.GetComponent<Light>().enabled = false;
            debris = Object.Instantiate(RuntimeObjectTemplates.Instance.BuildingDebris);
            DetachedPartSmokeTrail adapter = debris.GetComponent<DetachedPartSmokeTrail>();
            DetachedPartSmokeTrail.Attach(debris, Vector3.zero, 1f);
            Require(adapter.enabled, "Authored smoke adapter activates");
            typeof(DetachedPartSmokeTrail).GetMethod("StopAndRelease", Instance).Invoke(adapter, null);
            Require(adapter != null && !adapter.enabled, "Smoke stop preserves authored component");
            DetachedPartSmokeTrail.Attach(debris, Vector3.one, 0.8f);
            Require(
                adapter == debris.GetComponent<DetachedPartSmokeTrail>() && adapter.enabled,
                "Smoke adapter reinitializes");
            checks.Add("PASS authored smoke adapter activate / stop / reinitialize");
            Type lifetime = typeof(FragmentLifetime);
            MethodInfo remaining = lifetime.GetMethod("Remaining", BindingFlags.Static | BindingFlags.NonPublic);
            Require(
                (float)remaining.Invoke(null, new object[] { debris.gameObject }) == 0f,
                "Unmarked authored root is not fresh debris");
            lifetime.GetMethod("Mark", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { debris.gameObject });
            Require(
                (float)remaining.Invoke(null, new object[] { debris.gameObject }) > 0f,
                "Retention starts at detachment");
            checks.Add("PASS fragment retention starts at Mark, not prefab Awake");
            BeamFade beam = TransientEffectPool.RentBeam();
            beam.Initialize(beam.GetComponent<StylizedBeamEffect>(), 0.01f);
            typeof(BeamFade).GetField("elapsed", Instance).SetValue(beam, 1f);
            typeof(BeamFade).GetMethod("Update", Instance).Invoke(beam, null);
            Require(TransientEffectPool.RentBeam() == beam, "Authored beam prefab returns and rents again");
            LightFade light = TransientEffectPool.RentLight(Vector3.zero);
            light.Initialize(light.GetComponent<Light>(), 0.01f, 2f);
            typeof(LightFade).GetField("elapsed", Instance).SetValue(light, 1f);
            typeof(LightFade).GetMethod("Update", Instance).Invoke(light, null);
            Require(
                TransientEffectPool.RentLight(Vector3.one) == light,
                "Authored light prefab returns and rents again");
            checks.Add("PASS transient beam/light templates and pool reuse");
            DebugManager debug = SceneObjectQuery.FindInScene<DebugManager>(previous);
            var settings = new SerializedObject(debug);
            bool friendly = settings.FindProperty("showFriendlyAI").boolValue;
            bool enemy = settings.FindProperty("showEnemyAI").boolValue;
            try
            {
                settings.FindProperty("showFriendlyAI").boolValue = true;
                settings.FindProperty("showEnemyAI").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
                Require(
                    debug.ShowsAI(UnitFaction.Player) && !debug.ShowsAI(UnitFaction.Enemy) && !debug.ShowsAI(UnitFaction.Neutral),
                    "Persistent scene debug switches separate factions");
            }
            finally
            {
                settings.FindProperty("showFriendlyAI").boolValue = friendly;
                settings.FindProperty("showEnemyAI").boolValue = enemy;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            Require(
                productionPool != null && productionVfx != null,
                "Isolated pool cleanup preserves production Managers");
            checks.Add("PASS faction debug switches and scene-owned service isolation");
            return WriteReport("play", checks);
        }
        finally
        {
            if (debris != null)
            {
                Object.DestroyImmediate(debris.gameObject);
            }

            if (services != null)
            {
                Object.DestroyImmediate(services);
            }

            SceneManager.SetActiveScene(previous);
            SceneManager.UnloadSceneAsync(fixture);
        }
    }

    private static string WriteReport(string name, List<string> checks)
    {
        Directory.CreateDirectory(ReportFolder);
        string report = string.Join("\n", checks);
        File.WriteAllText(ReportFolder + "/" + name + ".txt", report);
        return report;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
