// 职责：创建可编辑的天气资产并把同一套预警与效果装入两个玩法场景。
// 入口：Tools/HY-Sandbox/Weather/Install。重复运行只补齐缺少的资产和场景绑定。
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WeatherEventAuthoring
{
    private const string Folder = "Assets/Art/Weather";
    private const string DirectorPath = Folder + "/Weather Events.prefab";
    private const string TornadoPath = Folder + "/Weather Tornado.prefab";
    private const string MeteorPath = Folder + "/Weather Meteor.prefab";
    private const string MaterialPath = Folder + "/Funnel Dust.mat";
    private const string TexturePath = Folder + "/Funnel Dust.asset";
    [MenuItem("Tools/HY-Sandbox/Weather/Install")]
    public static void Install()
    {
        EnsureEditMode();
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Art", "Weather");
        }

        GameObject tornado = BuildPrefab(TornadoPath, CreateTornado);
        GameObject meteor = BuildPrefab(MeteorPath, CreateMeteor);
        GameObject director = BuildPrefab(DirectorPath, () => CreateDirector(tornado, meteor));
        BindScene<WorldSessionManager>(GameModeAuthoring.ExplorationPath, director);
        BindScene<SiegeGameplayStage>(GameModeAuthoring.SiegePath, director);
        AssetDatabase.SaveAssets();
        Debug.Log("Weather events installed in Exploration and SiegeGameplay.");
    }

    private static void EnsureEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Install weather assets in Edit Mode.");
        }
    }

    private static GameObject BuildPrefab(string path, Func<GameObject> create)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;
        }

        GameObject root = create();
        try
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab == null)
            {
                throw new InvalidOperationException("Could not save " + path);
            }

            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateMeteor()
    {
        GameObject sphere = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Sphere.prefab");
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(sphere);
        root.name = "Weather Meteor";
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        return root;
    }

    private static GameObject CreateTornado()
    {
        Material material = BuildDustMaterial();
        GameObject root = new GameObject("Weather Tornado");
        WeatherTornado tornado = root.AddComponent<WeatherTornado>();
        GameObject dust = new GameObject("Funnel Dust");
        dust.transform.SetParent(root.transform, false);
        ParticleSystem particles = dust.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 192;
        var emission = particles.emission;
        emission.enabled = false;
        var lifetimeColor = particles.colorOverLifetime;
        lifetimeColor.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .18f), new GradientAlphaKey(0f, 1f) });
        lifetimeColor.color = gradient;
        ParticleSystemRenderer renderer = dust.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        SetReference(tornado, "funnel", particles);
        return root;
    }

    private static Material BuildDustMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            throw new InvalidOperationException("URP particle shader is unavailable.");
        }

        material = new Material(shader)
        {
            name = "Funnel Dust"
        };
        material.SetTexture("_BaseMap", BuildDustTexture());
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static Texture2D BuildDustTexture()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (texture != null)
        {
            return texture;
        }

        const int size = 64;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = "Funnel Dust",
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y) / (size - 1) * 2f - Vector2.one;
                float noise = Mathf.PerlinNoise(point.x * 3f + 7f, point.y * 3f + 13f);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - point.magnitude));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * Mathf.Lerp(.6f, 1f, noise)));
            }
        }

        texture.Apply();
        AssetDatabase.CreateAsset(texture, TexturePath);
        return texture;
    }

    private static GameObject CreateDirector(GameObject tornado, GameObject meteor)
    {
        GameObject root = new GameObject("Weather Events");
        WeatherEventDirector director = root.AddComponent<WeatherEventDirector>();
        GameObject overlay = new GameObject("Weather Warning", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        overlay.transform.SetParent(root.transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 35;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        GameObject banner = new GameObject("Warning Banner", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(overlay.transform, false);
        RectTransform rect = (RectTransform)banner.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -105f);
        rect.sizeDelta = new Vector2(450f, 39f);
        Image backdrop = banner.GetComponent<Image>();
        backdrop.color = new Color(.075f, .10f, .13f, .93f);
        backdrop.raycastTarget = false;
        GameObject labelObject = new GameObject("Event and Countdown", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(banner.transform, false);
        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/CombatHud/ChakraPetch SDF.asset");
        label.fontSize = 21f;
        label.characterSpacing = 2f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, .72f, .33f);
        label.raycastTarget = false;
        label.text = $"METEOR SHOWER  /  IN {WeatherEventDirector.WarningSeconds:00}s";
        banner.SetActive(false);
        SetReference(director, "warning", label);
        SetReference(director, "warningBanner", banner);
        SetReference(director, "meteorPrefab", meteor.GetComponent<Rigidbody>());
        SetReference(director, "tornadoPrefab", tornado.GetComponent<WeatherTornado>());
        return root;
    }

    private static void BindScene<T>(string path, GameObject prefab)
        where T : Component
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
        else if (scene.isDirty)
        {
            throw new InvalidOperationException("Save the target scene first: " + path);
        }

        try
        {
            T owner = SceneObjectQuery.FindInScene<T>(scene);
            if (owner == null)
            {
                throw new InvalidOperationException("Weather host is missing: " + path);
            }

            SerializedObject settings = new SerializedObject(owner);
            SerializedProperty weather = settings.FindProperty("weather");
            if (weather.objectReferenceValue != null)
            {
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            weather.objectReferenceValue = instance.GetComponent<WeatherEventDirector>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Could not save " + path);
            }
        }
        finally
        {
            if (opened)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void SetReference(Component target, string field, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
