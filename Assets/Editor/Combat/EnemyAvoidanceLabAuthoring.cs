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
            var settings = new SerializedObject(lab);
            SerializedProperty list = settings.FindProperty("layouts");
            list.arraySize = layouts.Length;
            for (int i = 0; i < layouts.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = layouts[i];
            }

            settings.ApplyModifiedPropertiesWithoutUndo();
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
            AddRoofedTrial();
            UpgradeTestField();
            EnemyLabComplexAuthoring.Upgrade();
            EnemyLabExpansionAuthoring.Upgrade();
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

    [MenuItem("Tools/HY-Sandbox/Combat/Add Roofed A Star Trial")]
    public static void AddRoofedTrial()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
        {
            throw new InvalidOperationException("Open the lab in Edit Mode.");
        }

        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        if (lab.transform.Find("Roofed U trap") != null)
        {
            return;
        }

        var layout = new GameObject("Roofed U trap");
        Undo.RegisterCreatedObjectUndo(layout, "Add roofed navigation trial");
        layout.transform.SetParent(lab.transform);
        Material material = LabMaterial("NavigationLabWall", new Color(0.8f, 0.29f, 0.08f));
        Box("Back", layout.transform, new Vector3(0f, 10f, 0f), new Vector3(4f, 20f, 42f), material);
        Box("Upper", layout.transform, new Vector3(-13f, 10f, 21f), new Vector3(30f, 20f, 4f), material);
        Box("Lower", layout.transform, new Vector3(-13f, 10f, -21f), new Vector3(30f, 20f, 4f), material);
        Box("Roof", layout.transform, new Vector3(0f, 32f, 0f), new Vector3(100f, 2f, 100f), material);
        layout.SetActive(false);
        var serialized = new SerializedObject(lab);
        SerializedProperty layouts = serialized.FindProperty("layouts");
        int index = layouts.arraySize++;
        layouts.GetArrayElementAtIndex(index).objectReferenceValue = layout;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        EditorSceneManager.SaveScene(lab.gameObject.scene);
    }

    [MenuItem("Tools/HY-Sandbox/Combat/Upgrade Navigation Test Field")]
    public static void UpgradeTestField()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
        {
            throw new InvalidOperationException("Open the lab in Edit Mode.");
        }

        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        Material walls = LabMaterial("NavigationLabWall", new Color(0.8f, 0.29f, 0.08f));
        var settings = new SerializedObject(lab);
        SerializedProperty list = settings.FindProperty("layouts");
        string[] names =
        {
            "Open flight",
            "Tall wall bypass",
            "Underpass",
            "Pillar field",
            "Switchback corridor",
            "Sealed chamber"
        };
        for (int i = 0; i < names.Length; i++)
        {
            if (lab.transform.Find(names[i]) != null)
            {
                continue;
            }

            var layout = new GameObject(names[i]);
            Undo.RegisterCreatedObjectUndo(layout, "Expand navigation field");
            layout.transform.SetParent(lab.transform);
            layout.SetActive(false);
            Transform root = layout.transform;
            if (i == 1)
            {
                Box("Tall wall", root, new Vector3(0f, 45f, 0f), new Vector3(5f, 90f, 50f), walls);
            }

            if (i == 2)
            {
                Box("Elevated beam", root, new Vector3(0f, 27f, 0f), new Vector3(5f, 26f, 90f), walls);
                Box("Low roof", root, new Vector3(0f, 43f, 0f), new Vector3(120f, 2f, 100f), walls);
            }

            if (i == 3)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Box("Pillar " + x + "/" + z, root, new Vector3(x * 28f, 26f, z * 35f), new Vector3(8f, 52f, 8f), walls);
                    }
                }
            }

            if (i == 4)
            {
                Box("North turn", root, new Vector3(-24f, 32f, 25f), new Vector3(5f, 64f, 80f), walls);
                Box("South turn", root, new Vector3(24f, 32f, -25f), new Vector3(5f, 64f, 80f), walls);
            }

            if (i == 5)
            {
                Box("Front", root, new Vector3(-15f, 24f, 0f), new Vector3(4f, 48f, 90f), walls);
                Box("Rear", root, new Vector3(-100f, 24f, 0f), new Vector3(4f, 48f, 90f), walls);
                Box("North", root, new Vector3(-57.5f, 24f, 45f), new Vector3(90f, 48f, 4f), walls);
                Box("South", root, new Vector3(-57.5f, 24f, -45f), new Vector3(90f, 48f, 4f), walls);
                Box("Roof", root, new Vector3(-57.5f, 49f, 0f), new Vector3(90f, 2f, 94f), walls);
            }

            int index = list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = layout;
        }

        settings.ApplyModifiedProperties();
        for (int i = 0; i < list.arraySize; i++)
        {
            GameObject layout = (GameObject)list.GetArrayElementAtIndex(i).objectReferenceValue;
            if (layout.GetComponent<EnemyAvoidanceCourse>() != null)
            {
                continue;
            }

            EnemyAvoidanceCourse course = Undo.AddComponent<EnemyAvoidanceCourse>(layout);
            var config = new SerializedObject(course);
            bool extended = i >= 5;
            bool underpass = layout.name == "Underpass";
            config.FindProperty("start").vector3Value = new Vector3(extended ? -65f : -45f, underpass ? 18f : 6f, 0f);
            config.FindProperty("goal").vector3Value = new Vector3(extended ? 65f : 45f, underpass ? 15f : 3f, 0f);
            config.FindProperty("duration").floatValue = layout.name == "Sealed chamber" ? 30f : 150f;
            config.FindProperty("description").stringValue = layout.name == "Sealed chamber" ? "封闭失败测试：预期找不到路线，用于观察重试和停滞，不要求到达。" : underpass ? "上方封顶，检查下穿净空与高度恢复。" : "真实推进器导航测试；可切换蓝图并点选起点/目标。";
            config.ApplyModifiedProperties();
        }

        foreach (MeshRenderer roof in lab.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (roof.name.IndexOf("roof", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            Bounds bounds = roof.bounds;
            LineRenderer outline = TraceLine(roof.transform, "Ceiling Outline", new Color(1f, 0.7f, 0.1f));
            outline.positionCount = 5;
            outline.SetPositions(new[] { new Vector3(bounds.min.x, bounds.min.y, bounds.min.z), new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), new Vector3(bounds.max.x, bounds.min.y, bounds.max.z), new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), new Vector3(bounds.min.x, bounds.min.y, bounds.min.z) });
        }

        GameObject ground = SceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == "Ground");
        ground.transform.localScale = new Vector3(300f, 1f, 300f);
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.SetPositionAndRotation(new Vector3(0f, 190f, -100f), Quaternion.Euler(65f, 0f, 0f));
            camera.orthographicSize = 140f;
        }

        settings.Update();
        settings.FindProperty("traceLine").objectReferenceValue = TraceLine(lab.transform, "Flight Trace", new Color(0.1f, 0.9f, 1f));
        settings.FindProperty("routeLine").objectReferenceValue = TraceLine(lab.transform, "Planned Route", new Color(1f, 0.7f, 0.1f));
        settings.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        EditorSceneManager.SaveScene(lab.gameObject.scene);
    }

    private static LineRenderer TraceLine(Transform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.GetComponent<LineRenderer>();
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = LabMaterial(name.Replace(" ", "") + "Material", color);
        line.startColor = line.endColor = color;
        line.widthMultiplier = 0.4f;
        line.useWorldSpace = true;
        line.positionCount = 0;
        return line;
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
