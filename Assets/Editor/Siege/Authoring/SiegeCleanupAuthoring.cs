// 职责：制作扫描线、回收粒子Prefab及原生HUD，将真实组件引用保存到全部Siege宿主。
// 入口：Install()/Configure()；独立备份场景，不覆盖已有手调特效参数。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SiegeCleanupAuthoring
{
    private const string Folder = "Assets/Siege/Cleanup";
    [MenuItem("HY-Sandbox/Siege/Install Interwave Salvage Sweep")]
    public static string Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save scenes and exit Play before cleanup authoring.");
        }

        string backup = "Library/SiegeCleanup/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff");
        Directory.CreateDirectory(backup);
        string[] paths =
        {
            "Assets/Scenes/SiegeGameplay.unity",
            "Assets/Scenes/Research/Siege.unity",
            "Assets/Scenes/SiegeStress.unity",
            "Assets/Scenes/Research/SiegeMixedStress.unity",
            "Assets/Scenes/Research/SiegeLargeStress.unity"
        };
        foreach (string path in paths)
        {
            File.Copy(path, backup + "/" + Path.GetFileName(path));
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            try
            {
                Configure(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        AssetDatabase.SaveAssets();
        return backup;
    }

    public static void Configure(Scene scene)
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        Material material = Surface();
        MaterialBreakEffect effect = Effect(material);
        SiegeCleanupSweep sweep = SceneObjectQuery.FindInScene<SiegeCleanupSweep>(scene, FindObjectsInactive.Include);
        if (sweep == null)
        {
            var root = new GameObject("Interwave Salvage Sweep");
            SceneManager.MoveGameObjectToScene(root, scene);
            sweep = root.AddComponent<SiegeCleanupSweep>();
            LineRenderer scan = Line(
                root.transform,
                "Scan Band",
                material,
                2,
                .55f,
                false);
            LineRenderer perimeter = Line(
                root.transform,
                "Perimeter",
                material,
                4,
                .12f,
                true);
            GameObject ui = new GameObject("Salvage HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            ui.transform.SetParent(root.transform, false);
            Canvas canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12;
            CanvasScaler scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            GameObject textObject = new GameObject("Scan Status", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(ui.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -180);
            rect.sizeDelta = new Vector2(820, 68);
            Text text = textObject.GetComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.3f, 1f, .85f);
            text.raycastTarget = false;
            textObject.SetActive(false);
            using var data = new SerializedObject(sweep);
            data.FindProperty("scanLine").objectReferenceValue = scan;
            data.FindProperty("perimeter").objectReferenceValue = perimeter;
            data.FindProperty("status").objectReferenceValue = text;
            data.FindProperty("salvageEffect").objectReferenceValue = effect;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        SiegeGameplayStage stage = SceneObjectQuery.FindInScene<SiegeGameplayStage>(scene);
        SiegeSession session = SceneObjectQuery.FindInScene<SiegeSession>(scene);
        using var host = new SerializedObject(stage != null ? (UnityEngine.Object)stage : session);
        host.FindProperty("cleanup").objectReferenceValue = sweep;
        host.ApplyModifiedPropertiesWithoutUndo();
    }

    private static LineRenderer Line(
        Transform parent,
        string name,
        Material material,
        int count,
        float width,
        bool loop)
    {
        GameObject root = new GameObject(name, typeof(LineRenderer));
        root.transform.SetParent(parent, false);
        LineRenderer line = root.GetComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.positionCount = count;
        line.useWorldSpace = true;
        line.loop = loop;
        line.widthMultiplier = width;
        line.startColor = line.endColor = new Color(.12f, 1f, .75f, .8f);
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    private static Material Surface()
    {
        string path = Folder + "/SalvageScan.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetColor("_BaseColor", new Color(.08f, 1f, .65f, .85f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }

    private static MaterialBreakEffect Effect(Material material)
    {
        string path = Folder + "/SalvageBurst.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            return prefab.GetComponent<MaterialBreakEffect>();
        }

        GameObject root = new GameObject("SalvageBurst", typeof(ParticleSystem), typeof(MaterialBreakEffect));
        try
        {
            ParticleSystem particles = root.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = .35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(.08f, .2f);
            main.startColor = new Color(.15f, 1f, .65f, .9f);
            main.gravityModifier = -.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2f;
            renderer.velocityScale = .15f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<MaterialBreakEffect>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
