// 职责：把运行对象组件组成烘焙为真实 Prefab，并迁移场景 Managers、模块依赖及 UI。
// 入口：Bake()；只补缺失组件/引用，原资产先备份，重复执行不覆盖用户已配置模板。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class EditableComponentBaker
{
    private const string Folder = "Assets/Resources/Runtime";
    private static string backupFolder;
    private static readonly HashSet<string> BackedUp = new HashSet<string>();
    private static int additions;
    [MenuItem("Tools/HY Sandbox/Architecture/Bake Editable Components")]
    public static string Bake()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Bake in Edit Mode only.");
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                throw new InvalidOperationException("Save the open scene before component migration.");
            }
        }

        additions = 0;
        backupFolder = "Temp/EditableComponentsBackup/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        BackedUp.Clear();
        Directory.CreateDirectory(backupFolder);
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Runtime");
        }

        BakeTemplates();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources", "Assets/Art" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Backup(path);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int before = additions;
                MigrateTree(root);
                // Unity can silently materialize newly declared RequireComponent dependencies
                // while loading a prefab. Persist those too, even when Ensure found them present.
                bool hasDependencies = root.GetComponentInChildren<Block>(true) != null || root.GetComponentInChildren<ControlUnit>(true) != null || root.GetComponentInChildren<Bot>(true) != null || root.GetComponentInChildren<ProjectileTracer>(true) != null;
                if (before != additions || hasDependencies)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes", "Assets/Art/Industrial/Preview" }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (string path in paths)
            {
                Backup(path);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                GameObject managers = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    MigrateTree(root);
                    if (root.name == "Managers")
                    {
                        managers = root;
                    }
                }

                if (managers == null)
                {
                    managers = new GameObject("Managers");
                }

                ConfigureServices(scene, managers.transform);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        AssetDatabase.SaveAssets();
        string report = $"Baked editable components: {additions} additions; backups: {backupFolder}";
        File.WriteAllText("Temp/editable-components-bake.txt", report);
        return report;
    }

    private static void Backup(string path)
    {
        if (!File.Exists(path) || !BackedUp.Add(path))
        {
            return;
        }

        string destination = Path.Combine(backupFolder, path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(path, destination, false);
        using var sha = System.Security.Cryptography.SHA256.Create();
        string hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        File.AppendAllText(Path.Combine(backupFolder, "manifest.tsv"), path + "\t" + hash + "\n");
    }

    private static T Ensure<T>(GameObject owner)
        where T : Component
    {
        T existing = owner.GetComponent<T>();
        if (existing != null)
        {
            return existing;
        }

        additions++;
        return owner.AddComponent<T>();
    }

    private static void SetReference(Object owner, string field, Object value)
    {
        var serialized = new SerializedObject(owner);
        SerializedProperty property = serialized.FindProperty(field);
        if (property.objectReferenceValue == null)
        {
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            additions++;
        }
    }

    private static T Template<T>(string name, Action<GameObject> compose)
        where T : Component
    {
        string path = Folder + "/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing.GetComponent<T>();
        }

        var root = new GameObject(name);
        try
        {
            root.SetActive(false);
            compose(root);
            // Most templates start active; initialization still happens before the next update.
            // Buildings stay inactive until assembly is committed by BuildingRuntime.
            if (typeof(T) != typeof(BuildingRuntime))
            {
                root.SetActive(true);
            }

            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<T>();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void ConfigurePool(VfxEffectPool pool)
    {
        Transform inactive = pool.transform.Find("Inactive effects");
        if (inactive == null)
        {
            var root = new GameObject("Inactive effects");
            root.transform.SetParent(pool.transform, false);
            root.SetActive(false);
            inactive = root.transform;
        }

        SetReference(pool, "_inactiveRoot", inactive);
    }

    private static void BakeTemplates()
    {
        string path = Folder + "/Objects.asset";
        Backup(path);
        RuntimeObjectTemplates library = AssetDatabase.LoadAssetAtPath<RuntimeObjectTemplates>(path);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<RuntimeObjectTemplates>();
            AssetDatabase.CreateAsset(library, path);
        }

        SetReference(library, "building", Template<BuildingRuntime>("Building", root => Ensure<BuildingRuntime>(root)));
        SetReference(
            library,
            "buildingDebris",
            Template<Rigidbody>(
                "BuildingDebris",
                root =>
        {
            Ensure<Rigidbody>(root);
            Ensure<FragmentLifetime>(root);
            Ensure<DetachedPartSmokeTrail>(root).enabled = false;
        }));
        SetReference(
            library,
            "terrainChunk",
            Template<VoxelWorldChunk>("TerrainChunk", root => Ensure<VoxelWorldChunk>(root)));
        SetReference(
            library,
            "voxelDebris",
            Template<VoxelDebrisPiece>("VoxelDebris", root => Ensure<VoxelDebrisPiece>(root)));
        SetReference(
            library,
            "visualMesh",
            Template<MeshFilter>(
                "VisualMesh",
                root =>
        {
            Ensure<MeshFilter>(root);
            Ensure<MeshRenderer>(root);
        }));
        SetReference(
            library,
            "worldLabel",
            Template<TextMesh>(
                "WorldLabel",
                root =>
        {
            TextMesh text = Ensure<TextMesh>(root);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            text.richText = false;
            text.characterSize = 0.2f;
            text.fontSize = 32;
            text.anchor = TextAnchor.LowerCenter;
        }));
        SetReference(
            library,
            "statusGroup",
            Template<CanvasGroup>(
                "StatusGroup",
                root =>
        {
            Ensure<RectTransform>(root);
            CanvasGroup group = Ensure<CanvasGroup>(root);
            group.interactable = false;
            group.blocksRaycasts = false;
        }));
        SetReference(library, "statusIcon", Template<Image>("StatusIcon", root => Ensure<Image>(root)));
        SetReference(
            library,
            "transientBeam",
            Template<BeamFade>(
                "TransientBeam",
                root =>
        {
            Ensure<StylizedBeamEffect>(root);
            Ensure<BeamFade>(root);
        }));
        SetReference(
            library,
            "transientLight",
            Template<LightFade>(
                "TransientLight",
                root =>
        {
            Ensure<Light>(root).shadows = LightShadows.None;
            Ensure<LightFade>(root);
        }));
        SetReference(
            library,
            "meteorLight",
            Template<Light>(
                "MeteorLight",
                root =>
        {
            Ensure<Light>(root).shadows = LightShadows.None;
            Ensure<MeteorLightToken>(root);
        }));
        SetReference(
            library,
            "workshopLight",
            Template<Light>(
                "WorkshopServices",
                root =>
        {
            Light light = Ensure<Light>(root);
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            ConfigurePool(Ensure<VfxEffectPool>(root));
            Ensure<TransientEffectPool>(root);
            Ensure<IndustrialMotionDriver>(root);
        }));
        SetReference(
            library,
            "powerConnection",
            Template<LineRenderer>("PowerConnection", root => Ensure<LineRenderer>(root)));
    }

    private static void MigrateTree(GameObject root)
    {
        if (root.name == "WorkshopServices")
        {
            VisualEffectsManager existing = root.GetComponent<VisualEffectsManager>();
            if (existing == null)
            {
                Ensure<VisualEffectsManager>(root).enableSceneLook = false;
            }

            Ensure<TurretProjectilePool>(root);
            Ensure<GlobalTextStyler>(root);
        }

        foreach (Block block in root.GetComponentsInChildren<Block>(true))
        {
            Ensure<BoxCollider>(block.gameObject);
            Ensure<Durability>(block.gameObject);
            Ensure<RuntimeUnitMember>(block.gameObject);
            Ensure<WreckSalvage>(block.gameObject);
        }

        foreach (ControlUnit unit in root.GetComponentsInChildren<ControlUnit>(true))
        {
            EnemyController existing = unit.GetComponent<EnemyController>();
            if (existing == null)
            {
                Ensure<EnemyController>(unit.gameObject).enabled = false;
            }
        }

        foreach (Cockpit cockpit in root.GetComponentsInChildren<Cockpit>(true))
        {
            Ensure<EnemyIdentity>(cockpit.gameObject);
        }

        foreach (Bot bot in root.GetComponentsInChildren<Bot>(true))
        {
            Ensure<Rigidbody>(bot.gameObject);
            if (bot is RepairBot)
            {
                Ensure<StylizedBeamEffect>(bot.gameObject);
            }
        }

        foreach (Thruster thruster in root.GetComponentsInChildren<Thruster>(true))
        {
            Ensure<ThrusterVisualEffect>(thruster.gameObject);
        }

        foreach (LaserTurret turret in root.GetComponentsInChildren<LaserTurret>(true))
        {
            Ensure<StylizedBeamEffect>(turret.gameObject);
        }

        foreach (ProjectileTracer tracer in root.GetComponentsInChildren<ProjectileTracer>(true))
        {
            Ensure<StylizedBeamEffect>(tracer.gameObject);
        }

        foreach (BuildPaletteItem item in root.GetComponentsInChildren<BuildPaletteItem>(true))
        {
            Ensure<UIInteractionFeedback>(item.gameObject);
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            Ensure<FragmentLifetime>(body.gameObject);
            Ensure<AbnormalImpulseBodyProbe>(body.gameObject);
            if (body.GetComponent<DetachedPartSmokeTrail>() == null)
            {
                Ensure<DetachedPartSmokeTrail>(body.gameObject).enabled = false;
            }
        }

        foreach (Meteor meteor in root.GetComponentsInChildren<Meteor>(true))
        {
            Ensure<Rigidbody>(meteor.gameObject);
        }

        foreach (MeteorShower shower in root.GetComponentsInChildren<MeteorShower>(true))
        {
            if (shower.meteorPrefab != null && shower.meteorPrefab.GetComponent<Meteor>() == null)
            {
                throw new InvalidOperationException("Meteor prefab must contain Meteor before migration: " + shower.meteorPrefab.name);
            }
        }

        foreach (VfxEffect effect in root.GetComponentsInChildren<VfxEffect>(true))
        {
            // Only the detached smoke template can be rented by meteors.
            if (AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(effect)).Contains("DebrisSmoke") || root.name.Contains("DebrisSmoke"))
            {
                if (effect.GetComponent<MeteorTrailFollower>() == null)
                {
                    Ensure<MeteorTrailFollower>(effect.gameObject).enabled = false;
                }
            }
        }

        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            Shadow shadow = null;
            foreach (Shadow candidate in text.GetComponents<Shadow>())
            {
                if (!(candidate is Outline))
                {
                    shadow = candidate;
                    break;
                }
            }

            if (shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.015f, 0.035f, 0.055f, 0.68f);
                shadow.effectDistance = new Vector2(1f, -1f);
                additions++;
            }
        }

        foreach (MainUIPanels panels in root.GetComponentsInChildren<MainUIPanels>(true))
        {
            foreach (GameObject panel in new[]
            {
                panels.buildPanel,
                panels.playPanel,
                panels.createPanel,
                panels.deletePanel,
                panels.debugPanel,
                panels.deathPanel
            }

            )
            {
                if (panel != null)
                {
                    Ensure<CanvasGroup>(panel);
                }
            }
        }

        foreach (SiegeThreatMarkers markers in root.GetComponentsInChildren<SiegeThreatMarkers>(true))
        {
            BakeMarkers(markers);
        }
    }

    private static T Service<T>(Scene scene, Transform managers)
        where T : Component
    {
        T existing = SceneObjectQuery.FindInScene<T>(scene, FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        var root = new GameObject(typeof(T).Name);
        root.transform.SetParent(managers, false);
        return Ensure<T>(root);
    }

    private static void ConfigureServices(Scene scene, Transform managers)
    {
        Service<IndustrialMotionDriver>(scene, managers);
        Service<GlobalTextStyler>(scene, managers);
        Service<TurretProjectilePool>(scene, managers);
        Service<TransientEffectPool>(scene, managers);
        ConfigurePool(Service<VfxEffectPool>(scene, managers));
        Service<AbnormalImpulseRecorder>(scene, managers);
        VisualEffectsManager effects = Service<VisualEffectsManager>(scene, managers);
        if (SceneObjectQuery.FindInScene<GameManager>(scene, FindObjectsInactive.Include) == null)
        {
            effects.enableSceneLook = false;
        }

        DebugManager debug = Service<DebugManager>(scene, managers);
        Ensure<DurabilityDebugLabels>(debug.gameObject);
        var serialized = new SerializedObject(debug);
        if (serialized.FindProperty("statusIconRoot").objectReferenceValue == null)
        {
            var overlay = new GameObject("Block Status Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            overlay.transform.SetParent(debug.transform, false);
            overlay.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = -1;
            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            SetReference(debug, "statusIconRoot", overlay.transform);
        }
    }

    private static void BakeMarkers(SiegeThreatMarkers owner)
    {
        var settings = new SerializedObject(owner);
        int count = owner.AuthoredPoolSize;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < count; i++)
        {
            string name = "Threat Marker " + (i + 1);
            if (owner.transform.Find(name) != null)
            {
                continue;
            }

            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)root.transform;
            rect.SetParent(owner.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = settings.FindProperty("markerSize").vector2Value;
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            root.GetComponent<Image>().color = settings.FindProperty("markerColor").colorValue;
            root.GetComponent<Image>().raycastTarget = false;
            var label = new GameObject("Distance", typeof(RectTransform), typeof(Text));
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -6f);
            labelRect.sizeDelta = new Vector2(90f, 22f);
            Text text = label.GetComponent<Text>();
            text.font = font;
            text.fontSize = owner.AuthoredLabelSize;
            text.alignment = TextAnchor.UpperCenter;
            text.color = settings.FindProperty("labelColor").colorValue;
            text.raycastTarget = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Shadow shadow = label.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.015f, 0.035f, 0.055f, 0.68f);
            shadow.effectDistance = new Vector2(1f, -1f);
            root.SetActive(false);
            additions++;
        }
    }
}

// Editor-only observers remain visible in authoring scenes and are stripped from Player scenes.
public sealed class EditableComponentBuildStripper : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null)
        {
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (AbnormalImpulseBodyProbe probe in root.GetComponentsInChildren<AbnormalImpulseBodyProbe>(true))
            {
                Object.DestroyImmediate(probe);
            }

            foreach (AbnormalImpulseRecorder recorder in root.GetComponentsInChildren<AbnormalImpulseRecorder>(true))
            {
                Object.DestroyImmediate(recorder);
            }
        }
    }
}
