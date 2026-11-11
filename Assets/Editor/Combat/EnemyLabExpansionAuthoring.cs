// 职责：在既有综合城区上追加可编辑高架层、部署点和运动学障碍。
// 入口：Upgrade()菜单；只创建缺失分组，重复执行保留人工参数和布局。
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyLabExpansionAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Combat/Add Upper Deck And Moving Obstacles")]
    public static void Upgrade()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != EnemyAvoidanceLabAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Open the navigation lab in Edit Mode.");
        }

        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        Transform district = lab.transform.Find("Complex obstacle district");
        if (district == null)
        {
            throw new InvalidOperationException("Build the complex navigation field first.");
        }

        Material deck = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/LabDistrictBuilding.mat");
        Material accent = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/LabDistrictPad.mat");
        Material warning = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/NavigationLabWall.mat");
        Material stripe = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/LabDistrictStripe.mat");
        if (district.Find("Upper deck") == null)
        {
            Transform upper = Group(district, "Upper deck");
            // 开放中央天井和两端升降区，给地面航线留下充足净空及垂直绕行空间。
            foreach (int side in new[]
            {
                -1,
                1
            }

            )
            {
                Box(upper, "Sky platform " + side, new Vector3(side * 88f, 59f, 0f), new Vector3(38f, 2f, 166f), deck);
                Box(upper, "Cross bridge " + side, new Vector3(0f, 59f, side * 65f), new Vector3(138f, 2f, 30f), deck);
                Box(
                    upper,
                    "Outer parapet " + side,
                    new Vector3(side * 108f, 63f, 0f),
                    new Vector3(2f, 6f, 168f),
                    accent);
                Box(
                    upper,
                    "Bridge baffle " + side,
                    new Vector3(side * 32f, 69f, side * 75f),
                    new Vector3(5f, 18f, 10f),
                    warning);
                foreach (int end in new[]
                {
                    -1,
                    1
                }

                )
                {
                    Box(
                        upper,
                        "Deck support " + side + "/" + end,
                        new Vector3(side * 88f, 28.5f, end * 72f),
                        new Vector3(5f, 57f, 5f),
                        deck);
                    Box(
                        upper,
                        "Landing pad " + side + "/" + end,
                        new Vector3(side * 88f, 60.1f, end * 65f),
                        new Vector3(24f, 0.15f, 24f),
                        accent);
                }

                for (int i = -3; i <= 3; i++)
                {
                    GameObject marking = Box(
                        upper,
                        "Upper lane stripe",
                        new Vector3(i * 18f, 60.1f, side * 65f),
                        new Vector3(7f, 0.1f, 0.6f),
                        stripe);
                    UnityEngine.Object.DestroyImmediate(marking.GetComponent<Collider>());
                }
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                Vector3 position = new Vector3(-155f, 135f, -175f);
                Undo.RecordObject(camera.transform, "Frame expanded district");
                camera.transform.SetPositionAndRotation(position, Quaternion.Euler(24f, 40f, 0f));
                EnemyLabCamera flight = camera.GetComponent<EnemyLabCamera>();
                if (flight != null)
                {
                    var cameraSettings = new SerializedObject(flight);
                    cameraSettings.FindProperty("freePosition").vector3Value = position;
                    cameraSettings.ApplyModifiedProperties();
                }
            }
        }

        AddLocations(district.GetComponent<EnemyAvoidanceCourse>(), accent);
        if (district.Find("Moving obstacles") == null)
        {
            Transform moving = Group(district, "Moving obstacles");
            Moving(
                moving,
                "West crossing shutter",
                new Vector3(-35f, 12f, 0f),
                new Vector3(6f, 22f, 16f),
                new Vector3(0f, 0f, 25f),
                18f,
                0f,
                warning);
            Moving(
                moving,
                "East lifting barrier",
                new Vector3(40f, 34f, 0f),
                new Vector3(8f, 18f, 26f),
                new Vector3(0f, 23f, 0f),
                22f,
                0f,
                warning);
            Moving(
                moving,
                "Upper bridge sweeper",
                new Vector3(0f, 71f, -65f),
                new Vector3(34f, 18f, 5f),
                Vector3.zero,
                20f,
                75f,
                warning);
            Moving(
                moving,
                "Upper crossing shuttle",
                new Vector3(0f, 71f, 65f),
                new Vector3(10f, 18f, 12f),
                new Vector3(40f, 0f, 0f),
                24f,
                0f,
                warning);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void AddLocations(EnemyAvoidanceCourse course, Material material)
    {
        var settings = new SerializedObject(course);
        SerializedProperty locations = settings.FindProperty("locations");
        foreach (int side in new[]
        {
            -1,
            1
        }

        )
        {
            foreach (int end in new[]
            {
                -1,
                1
            }

            )
            {
                string name = "Upper " + (side < 0 ? "West" : "East") + (end < 0 ? " South" : " North");
                Transform marker = course.transform.Find(name);
                if (marker != null)
                {
                    continue;
                }

                marker = Group(course.transform, name);
                marker.position = new Vector3(side * 88f, 68f, end * 65f);
                LineRenderer ring = marker.gameObject.AddComponent<LineRenderer>();
                ring.sharedMaterial = material;
                ring.widthMultiplier = 0.6f;
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = 32;
                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = i * Mathf.PI * 2f / ring.positionCount;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 5f, 0f, Mathf.Sin(angle) * 5f));
                }

                TextMesh label = Group(marker, "Location Label").gameObject.AddComponent<TextMesh>();
                label.transform.localPosition = Vector3.up * 3f;
                label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                label.text = name;
                label.characterSize = 0.9f;
                label.fontSize = 48;
                label.anchor = TextAnchor.MiddleCenter;
                int index = locations.arraySize++;
                locations.GetArrayElementAtIndex(index).objectReferenceValue = marker;
            }
        }

        settings.FindProperty("description").stringValue = "双层综合城区：地面街区、高架环形连桥、中央天井及四组橙色动态障碍。十个起终点支持地面/高架跨层组合；重试重置障碍相位，暂停停止运动。大型蓝图请先检查桥面及闸门净空。";
        settings.ApplyModifiedProperties();
    }

    private static void Moving(
        Transform root,
        string name,
        Vector3 position,
        Vector3 size,
        Vector3 travel,
        float period,
        float angle,
        Material material)
    {
        GameObject obstacle = Box(root, name, position, size, material);
        obstacle.isStatic = false;
        Rigidbody body = obstacle.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        EnemyLabMovingObstacle motion = obstacle.AddComponent<EnemyLabMovingObstacle>();
        var settings = new SerializedObject(motion);
        settings.FindProperty("travel").vector3Value = travel;
        settings.FindProperty("period").floatValue = period;
        settings.FindProperty("swingAngle").floatValue = angle;
        settings.ApplyModifiedProperties();
    }

    private static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Expand avoidance lab");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject Box(Transform root, string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(box, "Expand avoidance lab");
        box.name = name;
        box.transform.SetParent(root, false);
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        box.isStatic = true;
        return box;
    }
}
