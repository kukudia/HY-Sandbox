// 职责：为阵地战场景建立夜色竞技场外观：程序化天空盒、雾色、环境光与主光，并保持可编辑资产。
// 入口：ApplyLook()；只改Siege场景与其天空盒材质，复用Main既有色调默认值，不改Main场景。
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class SiegeArenaLookAuthoring
{
    public const string SkyMaterialPath = "Assets/Siege/NightArenaSky.mat";
    private static readonly Color SkyTint = new Color(0.07f, 0.10f, 0.16f, 1f);
    private static readonly Color GroundTint = new Color(0.05f, 0.05f, 0.08f, 1f);
    private static readonly Color FogColor = new Color(0.07f, 0.09f, 0.13f, 1f);
    private static readonly Color AmbientColor = new Color(0.16f, 0.20f, 0.26f, 1f);
    private static readonly Color SunColor = new Color(0.62f, 0.78f, 0.96f, 1f);
    [MenuItem("HY-Sandbox/Siege/Apply Night Arena Look")]
    public static void ApplyLook()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != SiegeSceneAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Open the saved Siege scene in Edit Mode before changing its look.");
        }

        Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        if (sky == null || sky.shader == null || sky.shader.name != "Skybox/Procedural")
        {
            if (sky != null)
            {
                AssetDatabase.DeleteAsset(SkyMaterialPath);
            }

            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                throw new InvalidOperationException("Skybox/Procedural shader is missing from this project.");
            }

            sky = new Material(shader)
            {
                name = "NightArenaSky"
            };
            if (!AssetDatabase.IsValidFolder("Assets/Siege"))
            {
                AssetDatabase.CreateFolder("Assets", "Siege");
            }

            AssetDatabase.CreateAsset(sky, SkyMaterialPath);
        }

        sky.SetColor("_SkyTint", SkyTint);
        sky.SetColor("_GroundColor", GroundTint);
        sky.SetFloat("_Exposure", 0.6f);
        sky.SetFloat("_AtmosphereThickness", 0.25f);
        sky.SetFloat("_SunSize", 0.02f);
        sky.SetFloat("_SunSizeConvergence", 3f);
        sky.SetFloat("_SunDisk", 0f);
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogDensity = 0.0035f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(AmbientColor.r * 1.4f, AmbientColor.g * 1.4f, AmbientColor.b * 1.4f, 1f);
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        // 竞技场地面延伸到雾程之外，去掉可见的直角地平线；仍是单个静态碰撞体。
        GameObject ground = GameObject.Find("Arena Ground");
        if (ground != null)
        {
            ground.transform.localScale = new Vector3(900f, 1f, 900f);
        }

        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
            {
                light.color = SunColor;
                light.intensity = 1.7f;
                light.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            }
        }

        VisualEffectsManager effects = UnityEngine.Object.FindFirstObjectByType<VisualEffectsManager>();
        if (effects != null)
        {
            var serialized = new SerializedObject(effects);
            serialized.FindProperty("sceneFogColor").colorValue = FogColor;
            serialized.FindProperty("sceneFogDensity").floatValue = 0.0035f;
            serialized.FindProperty("sceneAmbientMode").intValue = (int)AmbientMode.Flat;
            serialized.FindProperty("sceneAmbientColor").colorValue = AmbientColor;
            serialized.FindProperty("sceneAmbientBlend").floatValue = 0.75f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        Pylons();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Siege night arena look applied: procedural sky, matched fog, cooler ambient and sun.");
    }

    /// <summary>沿竞技场外圈生成12座带信标塔，给夜色场地提供尺度与纵深；纯装饰、无碰撞体，可重复重建。</summary>
    private static void Pylons()
    {
        for (int index = 0; index < 32; index++)
        {
            foreach (string suffix in new[]
            {
                string.Empty,
                " Beacon"
            }

            )
            {
                GameObject stale = GameObject.Find("Arena Pylon " + index + suffix);
                if (stale != null)
                {
                    UnityEngine.Object.DestroyImmediate(stale);
                }
            }
        }

        Material body = Material("ArenaPylon", new Color(0.045f, 0.06f, 0.08f), null);
        Material beacon = Material("ArenaBeacon", new Color(0.02f, 0.03f, 0.04f), new Color(0.12f, 0.68f, 0.85f) * 1.6f);
        const int count = 12;
        const float radius = 250f;
        for (int index = 0; index < count; index++)
        {
            float angle = index * Mathf.PI * 2f / count;
            var origin = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
            float height = index % 2 == 0 ? 30f : 20f;
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Arena Pylon " + index;
            shaft.transform.position = origin + Vector3.up * (height * 0.5f);
            shaft.transform.rotation = Quaternion.LookRotation(-new Vector3(origin.x, 0f, origin.z));
            shaft.transform.localScale = new Vector3(8f, height, 8f);
            shaft.GetComponent<Renderer>().sharedMaterial = body;
            UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Arena Pylon " + index + " Beacon";
            // 不挂到非等比缩放的塔身上，否则信标会继承 7x40x7 的世界缩放而变成巨块。
            cap.transform.position = origin + Vector3.up * (height + 1.2f);
            cap.transform.localScale = new Vector3(3.6f, 3.6f, 3.6f);
            cap.GetComponent<Renderer>().sharedMaterial = beacon;
            UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
        }
    }

    private static Material Material(string name, Color color, Color? emission)
    {
        string path = "Assets/Siege/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader)
            {
                name = name
            };
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.25f);
        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        EditorUtility.SetDirty(material);
        return material;
    }
}
