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

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Siege night arena look applied: procedural sky, matched fog, cooler ambient and sun.");
    }
}
