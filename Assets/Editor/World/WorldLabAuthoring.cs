// 职责：创建独立WorldLab场景并将生成网格保存为真实资产；Inspector支持种子与分块参数。
// 入口：Tools/HY-Sandbox/World/Create or open World Lab；不保存或改写Main。
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldLabAuthoring
{
    public const string ScenePath = "Assets/Scenes/WorldLab.unity";
    private const string AssetRoot = "Assets/WorldLab";
    [MenuItem("Tools/HY-Sandbox/World/Create or open World Lab")]
    public static void CreateOrOpen()
    {
        if (EditorApplication.isPlaying)
        {
            throw new System.InvalidOperationException("Exit Play Mode first.");
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }

        Directory.CreateDirectory(AssetRoot + "/Meshes");
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("ProceduralWorld");
        var world = root.AddComponent<ProceduralVoxelWorld>();
        var serialized = new SerializedObject(world);
        SerializedProperty materials = serialized.FindProperty("materials");
        materials.arraySize = 3;
        materials.GetArrayElementAtIndex(0).objectReferenceValue = MaterialAt("Ochre", new Color(0.46f, 0.25f, 0.12f), 0.05f);
        materials.GetArrayElementAtIndex(1).objectReferenceValue = MaterialAt("Basalt", new Color(0.14f, 0.19f, 0.23f), 0.15f);
        materials.GetArrayElementAtIndex(2).objectReferenceValue = MaterialAt("Alloy", new Color(0.18f, 0.48f, 0.52f), 0.65f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        world.Generate();
        BakeMeshes(world);
        var cameraObject = new GameObject("WorldLab Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.065f, 0.11f);
        camera.farClipPlane = 600f;
        cameraObject.transform.position = new Vector3(154f, 118f, -35f);
        cameraObject.transform.LookAt(new Vector3(64f, 12f, 64f));
        var controller = cameraObject.AddComponent<WorldLabController>();
        var controlData = new SerializedObject(controller);
        controlData.FindProperty("world").objectReferenceValue = world;
        controlData.ApplyModifiedPropertiesWithoutUndo();
        var light = new GameObject("WorldLab Sun").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.5f;
        light.color = new Color(1f, 0.87f, 0.7f);
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.46f, 0.6f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0028f;
        RenderSettings.fogColor = camera.backgroundColor;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(64, 12, 64), new Vector3(150, 50, 150)), false);
    }

    private static Material MaterialAt(string name, Color color, float metallic)
    {
        string path = AssetRoot + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            name = name
        };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", 0.28f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    public static void BakeMeshes(ProceduralVoxelWorld world)
    {
        if (world.gameObject.scene.path != ScenePath && !string.IsNullOrEmpty(world.gameObject.scene.path))
        {
            throw new System.InvalidOperationException("WorldLab Bake only owns its WorldLab scene. Duplicate worlds need a separate asset directory.");
        }

        Directory.CreateDirectory(AssetRoot + "/Meshes");
        AssetDatabase.Refresh();
        foreach (VoxelWorldChunk chunk in world.GetComponentsInChildren<VoxelWorldChunk>())
        {
            Mesh source = chunk.GetComponent<MeshFilter>().sharedMesh;
            string path = AssetRoot + "/Meshes/" + chunk.name + ".asset";
            Mesh target = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (target == null)
            {
                target = Object.Instantiate(source);
                AssetDatabase.CreateAsset(target, path);
            }
            else if (source != target)
            {
                EditorUtility.CopySerialized(source, target);
                EditorUtility.SetDirty(target);
            }

            chunk.AdoptBakedMesh(target);
        }

        EditorUtility.SetDirty(world);
        EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
}
