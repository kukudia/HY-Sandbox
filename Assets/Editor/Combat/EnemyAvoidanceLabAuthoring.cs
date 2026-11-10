// 职责：制作独立避障场景及持久化AI调试开关；不覆盖用户方块表和蓝图。
// 入口：Create() / EnableDebug()；重用既有服务配置，保存真实舰体和可编辑障碍布局。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyAvoidanceLabAuthoring
{
    public const string ScenePath = "Assets/Scenes/EnemyAvoidanceLab.unity";
    [MenuItem("Tools/HY-Sandbox/Combat/Create Avoidance Lab")]
    public static void Create()
    {
        if (EditorApplication.isPlaying || System.IO.File.Exists(ScenePath))
        {
            throw new InvalidOperationException("Exit Play; existing lab is preserved. Open it to edit manually.");
        }

        Scene previous = SceneManager.GetActiveScene();
        Scene source = EditorSceneManager.OpenScene("Assets/Scenes/Siege.unity", OpenSceneMode.Additive);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            foreach (GameObject root in source.GetRootGameObjects())
            {
                if (root.name == "Managers" || root.name == "Directional Light")
                {
                    GameObject clone = UnityEngine.Object.Instantiate(root);
                    clone.name = root.name;
                    SceneManager.MoveGameObjectToScene(clone, scene);
                }
            }

            var services = new GameObject("Lab Services");
            PlayManager play = services.AddComponent<PlayManager>();
            services.AddComponent<DestroyManager>();
            services.AddComponent<VisualEffectsManager>().enableSceneLook = false;
            DebugManager debug = services.AddComponent<DebugManager>();
            var overlay = new GameObject("Status Overlay", typeof(RectTransform), typeof(Canvas));
            overlay.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            Set(debug, "statusIconRoot", overlay.GetComponent<RectTransform>());
            play.playMode = true;
            play.BindSiege(null);
            play.blocksParent = null;
            foreach (MonoBehaviour behaviour in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (behaviour is SaveManager || behaviour is GameManager || behaviour is InputManager || behaviour is BuildManager)
                {
                    behaviour.enabled = false;
                }
            }

            Material ground = LabMaterial("NavigationLabGround", new Color(0.16f, 0.22f, 0.28f));
            Material walls = LabMaterial("NavigationLabWall", new Color(0.8f, 0.29f, 0.08f));
            Box("Ground", null, new Vector3(0f, -0.5f, 0f), new Vector3(150f, 1f, 150f), ground);
            var owner = new GameObject("Avoidance Lab");
            EnemyAvoidanceLab lab = owner.AddComponent<EnemyAvoidanceLab>();
            string[] names =
            {
                "Single wall",
                "U trap",
                "Slalom",
                "Narrow gate"
            };
            GameObject[] layouts = names.Select(n => new GameObject(n)).ToArray();
            foreach (GameObject layout in layouts)
            {
                layout.transform.SetParent(owner.transform);
            }

            Box("Wall", layouts[0].transform, new Vector3(0f, 10f, 0f), new Vector3(4f, 20f, 40f), walls);
            Box("Back", layouts[1].transform, new Vector3(0f, 10f, 0f), new Vector3(4f, 20f, 42f), walls);
            Box("Upper", layouts[1].transform, new Vector3(-13f, 10f, 21f), new Vector3(30f, 20f, 4f), walls);
            Box("Lower", layouts[1].transform, new Vector3(-13f, 10f, -21f), new Vector3(30f, 20f, 4f), walls);
            Box(
                "North baffle",
                layouts[2].transform,
                new Vector3(-15f, 10f, 23f),
                new Vector3(4f, 20f, 62f),
                walls);
            Box(
                "South baffle",
                layouts[2].transform,
                new Vector3(15f, 10f, -23f),
                new Vector3(4f, 20f, 62f),
                walls);
            Box("Gate north", layouts[3].transform, new Vector3(0f, 10f, 28f), new Vector3(5f, 20f, 40f), walls);
            Box("Gate south", layouts[3].transform, new Vector3(0f, 10f, -28f), new Vector3(5f, 20f, 40f), walls);
            for (int i = 0; i < layouts.Length; i++)
            {
                layouts[i].SetActive(i == 0);
            }

            EnemyNavigationWorld navigation = new GameObject("Enemy Navigation").AddComponent<EnemyNavigationWorld>();
            Set(navigation, "navigationRadius", 110f);
            Set(navigation, "navigationHeight", 80f);
            var blueprint = JsonUtility.FromJson<BlockDataList>(Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch").text);
            ControlUnit ship = ModularUnitFactory.Spawn(
                blueprint,
                "Finch navigation test ship",
                new Vector3(-45f, 6f, 0f),
                Quaternion.LookRotation(Vector3.right),
                UnitFaction.Enemy);
            SceneManager.MoveGameObjectToScene(ship.gameObject, scene);
            ship.cockpit.SetControlMode(UnitControlMode.Autonomous);
            foreach (Turret turret in ship.GetComponentsInChildren<Turret>())
            {
                turret.SetStarted(false);
            }

            ControlUnit target = ModularUnitFactory.Spawn(blueprint, "Stationary target", new Vector3(45f, 3f, 0f), Quaternion.identity, UnitFaction.Player);
            SceneManager.MoveGameObjectToScene(target.gameObject, scene);
            foreach (Thruster thruster in target.GetComponentsInChildren<Thruster>())
            {
                thruster.enabled = false;
            }

            target.GetComponent<EnemyController>().enabled = false;
            target.GetComponent<Rigidbody>().isKinematic = true;
            foreach (Turret turret in target.GetComponentsInChildren<Turret>())
            {
                turret.SetStarted(false);
            }

            foreach (Durability health in ship.GetComponentsInChildren<Durability>().Concat(target.GetComponentsInChildren<Durability>()))
            {
                // 专注导航：保留真实碰撞/惯性并计数，不让前次撞击摧毁下一轮的推进器。
                Set(health, "_collisionDamageScale", 0f);
            }

            Set(ship.GetComponent<EnemyController>(), "drawDebug", true);
            Set(lab, "ship", ship);
            Set(lab, "target", target);
            Set(lab, "navigation", navigation);
            var settings = new SerializedObject(lab);
            SerializedProperty list = settings.FindProperty("layouts");
            list.arraySize = layouts.Length;
            for (int i = 0; i < layouts.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = layouts[i];
            }

            settings.ApplyModifiedPropertiesWithoutUndo();
            Set(ship.gameObject.AddComponent<EnemyAvoidanceContact>(), "lab", lab);
            Camera camera = new GameObject("Lab Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0f, 125f, -65f), Quaternion.Euler(64f, 0f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 85f;
            camera.farClipPlane = 400f;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AudioListener>();
            play.mainCamera = camera;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);
            SetDebug(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(previous);
        }

        EditorSceneManager.OpenScene(ScenePath);
        FrameLab();
    }

    public static void FrameLab()
    {
        SceneView view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
        view.drawGizmos = true;
        view.LookAtDirect(Vector3.zero, Quaternion.Euler(75f, 0f, 0f), 100f);
        view.orthographic = true;
        var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        gameView.GetType().GetProperty(
            "drawGizmos",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.SetValue(gameView, true);
        view.Repaint();
    }

    public static void RefreshPresentation()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Open the lab in Edit Mode.");
        }

        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        Set(lab, "arrivalRadius", 22f);
        Material walls = LabMaterial("NavigationLabWall", new Color(0.8f, 0.29f, 0.08f));
        Material ground = LabMaterial("NavigationLabGround", new Color(0.16f, 0.22f, 0.28f));
        foreach (MeshRenderer renderer in lab.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = walls;
        }

        scene.GetRootGameObjects().First(g => g.name == "Ground").GetComponent<MeshRenderer>().sharedMaterial = ground;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        FrameLab();
    }

    private static Material LabMaterial(string name, Color color)
    {
        string path = "Assets/Art/Combat/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }

    [MenuItem("Tools/HY-Sandbox/Combat/Enable AI Debug")]
    public static void EnableDebug()
    {
        string path = "Assets/Resources/BlocksParent/BlocksParent.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Set(prefab.GetComponent<EnemyController>(), "drawDebug", true);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        foreach (string scenePath in new[]
        {
            "Assets/Scenes/Main.unity",
            "Assets/Scenes/Siege.unity",
            "Assets/Scenes/WorldLab.unity"
        }

        )
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty)
            {
                throw new InvalidOperationException("Scene has unsaved edits: " + scenePath);
            }

            if (opened)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            SetDebug(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        foreach (SceneView view in SceneView.sceneViews)
        {
            view.drawGizmos = true;
        }
    }

    private static void SetDebug(Scene scene)
    {
        foreach (DebugManager debug in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DebugManager>(true)))
        {
            Set(debug, "showFriendlyAI", true);
            Set(debug, "showEnemyAI", true);
        }
    }

    private static void Set(UnityEngine.Object owner, string field, object value)
    {
        var serialized = new SerializedObject(owner);
        SerializedProperty property = serialized.FindProperty(field);
        if (value is bool flag)
        {
            property.boolValue = flag;
        }
        else if (value is float number)
        {
            property.floatValue = number;
        }
        else
        {
            property.objectReferenceValue = (UnityEngine.Object)value;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        box.isStatic = true;
    }
}
