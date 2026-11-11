// 职责：保存综合障碍城区、可选起终点与自由相机，幂等扩展既有试验场。
// 入口：Upgrade()；只新增缺失对象，保留用户对已制作场地的手动调整。
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyLabComplexAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Combat/Build Complex Navigation Field")]
    public static void Upgrade()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != EnemyAvoidanceLabAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Open the navigation lab in Edit Mode.");
        }

        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        var settings = new SerializedObject(lab);
        SerializedProperty layouts = settings.FindProperty("layouts");
        Transform complex = lab.transform.Find("Complex obstacle district");
        Material walls = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/NavigationLabWall.mat");
        if (complex == null)
        {
            complex = new GameObject("Complex obstacle district").transform;
            Undo.RegisterCreatedObjectUndo(complex.gameObject, "Build complex navigation district");
            complex.SetParent(lab.transform);
            // 中央十字通道、错列楼群和侧面通道共同构成可反向测试的完整场地。
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }

                    float px = x * 55f + (z == 0 ? 0f : 10f);
                    float pz = z * 55f;
                    float height = 16f + ((x + z + 4) % 3) * 12f;
                    Box(
                        complex,
                        "Tower " + x + "/" + z,
                        new Vector3(px, height * 0.5f, pz),
                        new Vector3(23f, height, 24f),
                        walls);
                }
            }

            Box(complex, "Central divider", new Vector3(0f, 12f, 0f), new Vector3(5f, 24f, 46f), walls);
            Box(complex, "North low beam", new Vector3(-30f, 21f, 92f), new Vector3(45f, 12f, 6f), walls);
            Box(complex, "South bridge", new Vector3(30f, 28f, -92f), new Vector3(46f, 8f, 8f), walls);
            Box(complex, "West roof", new Vector3(-82f, 32f, 0f), new Vector3(32f, 2f, 70f), walls);
            Box(complex, "West channel wall", new Vector3(-80f, 10f, 28f), new Vector3(38f, 20f, 4f), walls);
            Box(complex, "East channel wall", new Vector3(82f, 10f, -28f), new Vector3(38f, 20f, 4f), walls);
            for (int i = 0; i < 4; i++)
            {
                Box(
                    complex,
                    "East pillars " + i,
                    new Vector3(95f, 15f, -72f + i * 48f),
                    new Vector3(7f, 30f, 7f),
                    walls);
            }

            EnemyAvoidanceCourse course = complex.gameObject.AddComponent<EnemyAvoidanceCourse>();
            var courseSettings = new SerializedObject(course);
            courseSettings.FindProperty("start").vector3Value = new Vector3(-120f, 8f, 0f);
            courseSettings.FindProperty("goal").vector3Value = new Vector3(120f, 5f, 0f);
            courseSettings.FindProperty("duration").floatValue = 240f;
            courseSettings.FindProperty("description").stringValue = "综合城区：高低楼群、中央隔墙、桥梁、顶棚、柱阵和侧通道。可从六个外围位置任选起终点；用于暴露跨区域规划问题，不保证所有蓝图均可通行。";
            courseSettings.ApplyModifiedProperties();
            int index = layouts.arraySize++;
            layouts.GetArrayElementAtIndex(index).objectReferenceValue = complex.gameObject;
        }

        AddDistrictPresentation(complex);
        settings.ApplyModifiedProperties();
        settings.Update();
        layouts = settings.FindProperty("layouts");
        int complexIndex = -1;
        for (int i = 0; i < layouts.arraySize; i++)
        {
            GameObject layout = (GameObject)layouts.GetArrayElementAtIndex(i).objectReferenceValue;
            if (layout.transform == complex)
            {
                complexIndex = i;
            }

            EnemyAvoidanceCourse course = layout.GetComponent<EnemyAvoidanceCourse>();
            if (course == null || course.LocationCount > 0)
            {
                continue;
            }

            bool district = layout.transform == complex;
            Vector3[] positions = district ? new[]
            {
                new Vector3(-120f, 8f, 0f),
                new Vector3(120f, 8f, 0f),
                new Vector3(-115f, 8f, 110f),
                new Vector3(115f, 8f, 110f),
                new Vector3(-115f, 8f, -110f),
                new Vector3(115f, 8f, -110f)
            }

            : new[]
            {
                course.Start,
                course.Goal + Vector3.up * 3f
            };
            string[] names = district ? new[]
            {
                "West Dock",
                "East Dock",
                "Northwest Pad",
                "Northeast Pad",
                "Southwest Pad",
                "Southeast Pad"
            }

            : new[]
            {
                "Default Start",
                "Default Goal"
            };
            var courseSettings = new SerializedObject(course);
            SerializedProperty locations = courseSettings.FindProperty("locations");
            locations.arraySize = positions.Length;
            for (int j = 0; j < positions.Length; j++)
            {
                var marker = new GameObject(names[j]);
                marker.transform.SetParent(layout.transform);
                marker.transform.position = positions[j];
                LineRenderer ring = marker.AddComponent<LineRenderer>();
                ring.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/FlightTraceMaterial.mat");
                ring.widthMultiplier = 0.5f;
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = 48;
                for (int k = 0; k < 48; k++)
                {
                    float angle = k * Mathf.PI * 2f / 48f;
                    ring.SetPosition(k, new Vector3(Mathf.Cos(angle) * 4f, 0f, Mathf.Sin(angle) * 4f));
                }

                var label = new GameObject("Location Label").AddComponent<TextMesh>();
                label.transform.SetParent(marker.transform, false);
                label.transform.localPosition = Vector3.up * 3f;
                label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                label.text = names[j];
                label.characterSize = 1.1f;
                label.fontSize = 48;
                label.anchor = TextAnchor.MiddleCenter;
                locations.GetArrayElementAtIndex(j).objectReferenceValue = marker.transform;
            }

            courseSettings.ApplyModifiedProperties();
        }

        const string traceMaterialPath = "Assets/Art/Combat/SpeedTraceMaterial.mat";
        Material traceMaterial = AssetDatabase.LoadAssetAtPath<Material>(traceMaterialPath);
        if (traceMaterial == null)
        {
            traceMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            traceMaterial.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(traceMaterial, traceMaterialPath);
        }

        lab.transform.Find("Flight Trace").GetComponent<LineRenderer>().sharedMaterial = traceMaterial;
        Camera camera = Camera.main;
        EnemyLabCamera flight = camera.GetComponent<EnemyLabCamera>();
        if (flight == null)
        {
            flight = Undo.AddComponent<EnemyLabCamera>(camera.gameObject);
        }

        var cameraSettings = new SerializedObject(flight);
        cameraSettings.FindProperty("lab").objectReferenceValue = lab;
        cameraSettings.ApplyModifiedProperties();
        camera.orthographic = false;
        camera.fieldOfView = 65f;
        camera.farClipPlane = 600f;
        camera.transform.SetPositionAndRotation(new Vector3(-105f, 65f, -125f), Quaternion.Euler(24f, 40f, 0f));
        settings.Update();
        settings.FindProperty("layoutIndex").intValue = complexIndex;
        settings.ApplyModifiedProperties();
        for (int i = 0; i < layouts.arraySize; i++)
        {
            ((GameObject)layouts.GetArrayElementAtIndex(i).objectReferenceValue).SetActive(i == complexIndex);
        }

        EditorSceneManager.MarkSceneDirty(lab.gameObject.scene);
        EditorSceneManager.SaveScene(lab.gameObject.scene);
    }

    private static void AddDistrictPresentation(Transform district)
    {
        if (district.Find("District presentation") != null)
        {
            return;
        }

        Transform presentation = new GameObject("District presentation").transform;
        presentation.SetParent(district);
        Material road = MaterialAsset("LabDistrictRoad", new Color(0.09f, 0.12f, 0.15f));
        Material stripe = MaterialAsset("LabDistrictStripe", new Color(0.8f, 0.74f, 0.4f));
        Material pad = MaterialAsset("LabDistrictPad", new Color(0.1f, 0.37f, 0.4f));
        Material building = MaterialAsset("LabDistrictBuilding", new Color(0.27f, 0.42f, 0.51f));
        Material low = MaterialAsset("LabDistrictLowBuilding", new Color(0.41f, 0.51f, 0.53f));
        foreach (MeshRenderer renderer in district.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.name.StartsWith("Tower", StringComparison.Ordinal))
            {
                renderer.sharedMaterial = renderer.bounds.size.y > 30f ? building : low;
            }
        }

        for (int i = -1; i <= 1; i++)
        {
            Decoration(
                presentation,
                "East West lane " + i,
                new Vector3(0f, 0.025f, i * 110f),
                new Vector3(260f, 0.04f, 24f),
                road);
            Decoration(
                presentation,
                "North South lane " + i,
                new Vector3(i * 110f, 0.03f, 0f),
                new Vector3(24f, 0.04f, 260f),
                road);
            for (int j = -6; j <= 6; j++)
            {
                Decoration(
                    presentation,
                    "Lane stripe EW",
                    new Vector3(j * 20f, 0.065f, i * 110f),
                    new Vector3(8f, 0.02f, 0.45f),
                    stripe);
                Decoration(
                    presentation,
                    "Lane stripe NS",
                    new Vector3(i * 110f, 0.07f, j * 20f),
                    new Vector3(0.45f, 0.02f, 8f),
                    stripe);
            }
        }

        foreach (Vector3 point in new[]
        {
            new Vector3(-120f, 0.08f, 0f),
            new Vector3(120f, 0.08f, 0f),
            new Vector3(-115f, 0.08f, 110f),
            new Vector3(115f, 0.08f, 110f),
            new Vector3(-115f, 0.08f, -110f),
            new Vector3(115f, 0.08f, -110f)
        }

        )
        {
            Decoration(presentation, "Deployment apron", point, new Vector3(22f, 0.03f, 22f), pad);
        }
    }

    private static Material MaterialAsset(string name, Color color)
    {
        string path = "Assets/Art/Combat/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }

    private static void Decoration(Transform root, string name, Vector3 position, Vector3 size, Material material)
    {
        Box(root, name, position, size, material);
        UnityEngine.Object.DestroyImmediate(root.GetChild(root.childCount - 1).GetComponent<Collider>());
    }

    private static void Box(Transform root, string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root);
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        box.isStatic = true;
    }
}
