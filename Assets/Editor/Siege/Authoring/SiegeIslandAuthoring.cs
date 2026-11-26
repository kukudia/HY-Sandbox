// 职责：为真实Siege场景保存浮空岛组件/配置及World地形材质，移除已替代的无限平板。
// 入口：Install() / Configure()；迁移前唯一备份，不改玩家基地或世界存档。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SiegeIslandAuthoring
{
    private const string SettingsPath = "Assets/Siege/FloatingIsland.asset";
    [MenuItem("HY-Sandbox/Siege/Preview Island For Current Base")]
    public static void PreviewCurrent()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Preview authoring requires Edit Mode.");
        }

        Scene scene = SceneManager.GetActiveScene();
        SiegeIsland island = SceneObjectQuery.FindInScene<SiegeIsland>(scene);
        SiegeGameplayStage stage = SceneObjectQuery.FindInScene<SiegeGameplayStage>(scene);
        SiegeSession session = SceneObjectQuery.FindInScene<SiegeSession>(scene);
        if (island == null || stage == null && session == null)
        {
            throw new InvalidOperationException("Open an installed Siege scene to preview its island.");
        }

        TextAsset source;
        Transform anchor;
        if (stage != null)
        {
            var data = new SerializedObject(stage);
            source = ((SiegeConstructionSettings)data.FindProperty("settings").objectReferenceValue).Starter;
            anchor = (Transform)data.FindProperty("baseSpawn").objectReferenceValue;
        }
        else
        {
            source = session.Settings.BaseBlueprint;
            anchor = (Transform)new SerializedObject(session).FindProperty("baseDock").objectReferenceValue;
        }

        BlockDataList design = JsonUtility.FromJson<BlockDataList>(source.text);
        Vector3 position = stage != null ? anchor.position - anchor.rotation * design.foundation.Center : anchor.position;
        Undo.RegisterFullObjectHierarchyUndo(island.gameObject, "Preview Siege island");
        island.GenerateFor(design.foundation, position, anchor.rotation);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = island.gameObject;
    }

    [MenuItem("HY-Sandbox/Siege/Bake Current Island Preview")]
    public static string BakeCurrent()
    {
        PreviewCurrent();
        Scene scene = SceneManager.GetActiveScene();
        SiegeIsland island = SceneObjectQuery.FindInScene<SiegeIsland>(scene);
        const string folder = "Assets/Art/Siege/IslandMeshes";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Siege", "IslandMeshes");
        }

        foreach (VoxelWorldChunk chunk in island.GetComponentsInChildren<VoxelWorldChunk>())
        {
            Mesh source = chunk.GetComponent<MeshFilter>().sharedMesh;
            string path = folder + "/" + scene.name + "-" + chunk.Start.x + "-" + chunk.Start.y + "-" + chunk.Start.z + ".asset";
            Mesh baked = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (baked == null)
            {
                baked = UnityEngine.Object.Instantiate(source);
                AssetDatabase.CreateAsset(baked, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, baked);
                EditorUtility.SetDirty(baked);
            }

            chunk.AdoptBakedMesh(baked);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        return "Saved island parameters, data, scene and mesh assets for " + scene.name;
    }

    [MenuItem("HY-Sandbox/Siege/Install Floating Islands")]
    public static string Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save scenes and exit Play before island migration.");
        }

        string backup = "Library/SiegeIsland/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
        Directory.CreateDirectory(backup);
        string[] paths =
        {
            "Assets/Scenes/SiegeGameplay.unity",
        };
        foreach (string path in paths)
        {
            File.Copy(path, backup + "/" + System.IO.Path.GetFileName(path));
            Scene existing = SceneManager.GetSceneByPath(path);
            bool opened = !existing.isLoaded;
            Scene scene = opened ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : existing;
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
        SiegeIslandSettings settings = AssetDatabase.LoadAssetAtPath<SiegeIslandSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<SiegeIslandSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        SiegeIsland island = SceneObjectQuery.FindInScene<SiegeIsland>(scene, FindObjectsInactive.Include);
        if (island == null)
        {
            var root = new GameObject("Siege Floating Island");
            SceneManager.MoveGameObjectToScene(root, scene);
            island = root.AddComponent<SiegeIsland>();
        }

        ProceduralVoxelWorld world = island.GetComponentInChildren<ProceduralVoxelWorld>(true);
        if (world == null)
        {
            var terrain = new GameObject("Voxel Terrain");
            terrain.transform.SetParent(island.transform, false);
            world = terrain.AddComponent<ProceduralVoxelWorld>();
        }

        var terrainData = new SerializedObject(world);
        terrainData.FindProperty("streaming").boolValue = true; // Await deployment; never generate WorldLab from Awake.
        SerializedProperty materials = terrainData.FindProperty("materials");
        materials.arraySize = 3;
        string[] names =
        {
            "Ochre",
            "Basalt",
            "Alloy"
        };
        for (int i = 0; i < names.Length; i++)
        {
            materials.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Terrain/" + names[i] + ".mat");
        }

        terrainData.ApplyModifiedPropertiesWithoutUndo();
        var islandData = new SerializedObject(island);
        islandData.FindProperty("world").objectReferenceValue = world;
        islandData.FindProperty("settings").objectReferenceValue = settings;
        islandData.ApplyModifiedPropertiesWithoutUndo();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Arena Ground").ToArray())
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }

        SiegeGameplayStage stage = SceneObjectQuery.FindInScene<SiegeGameplayStage>(scene, FindObjectsInactive.Include);
        SiegeSession session = SceneObjectQuery.FindInScene<SiegeSession>(scene, FindObjectsInactive.Include);
        Component host = stage != null ? (Component)stage : session;
        if (host != null)
        {
            var hostData = new SerializedObject(host);
            hostData.FindProperty("island").objectReferenceValue = island;
            hostData.ApplyModifiedPropertiesWithoutUndo();
        }

        BlueprintStructureRegistry buildings = stage != null ? stage.GetComponent<BlueprintStructureRegistry>() : session != null ? session.Buildings : null;
        if (buildings != null)
        {
            var buildingData = new SerializedObject(buildings);
            buildingData.FindProperty("world").objectReferenceValue = world;
            buildingData.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }
}
