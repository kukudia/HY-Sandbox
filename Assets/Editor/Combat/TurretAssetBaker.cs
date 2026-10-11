// 职责：从指定 SpaceKit 源制作单管/双管炮塔、真实炮弹及独立建造目录资产。
// 入口：Bake()；只保存拥有的 Prefab/材质/图标，不保存当前场景。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TurretAssetBaker
{
    private const string Blocks = "Assets/Resources/Blocks/";
    private const string Art = "Assets/Art/Combat";
    private const string Entries = "Assets/Resources/BuildPaletteEntries";
    [MenuItem("Tools/HY Sandbox/Combat/Refresh turret thumbnails")]
    public static void RefreshTurretIcons()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before rendering thumbnails.");
        }

        foreach (string name in new[]
        {
            "ProjectileTurretSingle",
            "ProjectileTurretDouble"
        }

        )
        {
            Sprite icon = RenderIcon(name);
            string path = Entries + "/" + name + ".prefab";
            GameObject entry = PrefabUtility.LoadPrefabContents(path);
            try
            {
                entry.transform.Find("Preview").GetComponent<Image>().sprite = icon;
                PrefabUtility.SaveAsPrefabAsset(entry, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(entry);
            }
        }
    }

    [MenuItem("Tools/HY Sandbox/Combat/Configure barrel recoil")]
    public static void ConfigureRecoil()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before configuring recoil.");
        }

        foreach (string name in new[]
        {
            "ProjectileTurretSingle",
            "ProjectileTurretDouble"
        }

        )
        {
            string path = Blocks + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ProjectileTurret turret = root.GetComponent<ProjectileTurret>();
                SerializedObject serialized = new SerializedObject(turret);
                SerializedProperty barrels = serialized.FindProperty("_recoilBarrels");
                if (barrels.arraySize == 0)
                {
                    barrels.arraySize = turret.Muzzles.Length;
                    for (int i = 0; i < turret.Muzzles.Length; i++)
                    {
                        barrels.GetArrayElementAtIndex(i).objectReferenceValue = turret.Muzzles[i].parent;
                    }

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    [MenuItem("Tools/HY Sandbox/Combat/Bake projectile turrets")]
    public static void Bake()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before baking turret assets.");
        }

        Directory.CreateDirectory(Art);
        Directory.CreateDirectory(Entries);
        AssetDatabase.Refresh();
        TurretProjectile projectile = BakeProjectile();
        ConfigureLaser();
        BakeTurret("ProjectileTurretSingle", "Single", 100f, projectile);
        BakeTurret("ProjectileTurretDouble", "Double", 150f, projectile);
        AssetDatabase.Refresh();
        BakeEntry("ProjectileTurretSingle");
        BakeEntry("ProjectileTurretDouble");
        Debug.Log("Saved laser power, single/double shell turrets, projectile and build entries; open scene preserved.");
    }

    private static TurretProjectile BakeProjectile()
    {
        string path = Art + "/TurretShell.prefab";
        GameObject root = new GameObject("TurretShell");
        try
        {
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 0.2f;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            root.AddComponent<SphereCollider>().radius = 0.035f;
            root.GetComponent<SphereCollider>().isTrigger = true;
            root.AddComponent<TurretProjectile>();
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "ShellVisual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(0.065f, 0.12f, 0.065f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            string materialPath = Art + "/TurretShell.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.SetColor("_BaseColor", new Color(1f, 0.53f, 0.08f));
            material.SetFloat("_Metallic", 0.7f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1.5f, 0.35f, 0.025f));
            visual.GetComponent<Renderer>().sharedMaterial = material;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<TurretProjectile>();
    }

    private static void ConfigureLaser()
    {
        string path = Blocks + "Turret.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponent<LaserTurret>() == null)
            {
                throw new InvalidOperationException("Old turret script GUID must resolve to LaserTurret before baking.");
            }

            Power power = root.GetComponent<Power>();
            power.minWorkingPower = 150f;
            power.standardWorkingPower = 300f;
            root.GetComponent<StatusIcon>().description = "Laser turret: instant hit, no impulse; active power 300.";
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BakeTurret(string name, string variant, float demand, TurretProjectile projectile)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(Blocks + "Turret.prefab");
        try
        {
            root.name = name;
            Object.DestroyImmediate(root.GetComponent<LaserTurret>());
            foreach (IndustrialPartMotion motion in root.GetComponentsInChildren<IndustrialPartMotion>(true))
            {
                Object.DestroyImmediate(motion);
            }

            Transform model = root.transform.Find("Model");
            foreach (Transform child in model.Cast<Transform>().ToArray())
            {
                Object.DestroyImmediate(child.gameObject);
            }

            string sourcePath = "Assets/Art/SpaceKit/Prefabs/Modules/Turrets/Polygon/SM_Prop_Turret_Base_" + variant + "_01.prefab";
            GameObject source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath), model);
            source.name = Path.GetFileNameWithoutExtension(sourcePath);
            source.transform.localPosition = new Vector3(0f, -0.26f, 0f);
            source.transform.localRotation = Quaternion.identity;
            source.transform.localScale = Vector3.one * 0.55f;
            foreach (Collider collider in source.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            Transform housing = source.transform.GetChild(0);
            Transform yaw = new GameObject("Horizontal").transform;
            yaw.SetParent(source.transform, false);
            yaw.localPosition = housing.localPosition;
            Transform pitch = new GameObject("Vertical").transform;
            pitch.SetParent(yaw, false);
            housing.SetParent(pitch, true);
            housing.localPosition = Vector3.zero;
            Transform[] barrels = housing.GetComponentsInChildren<MeshFilter>().Where(m => m.name.Contains("Barrel")).Select(m => m.transform).ToArray();
            Transform[] muzzles = new Transform[barrels.Length];
            for (int i = 0; i < barrels.Length; i++)
            {
                // 最前端环的顶点确定炮口，原始桶的局部旋转仍由美术资产保留。
                Mesh mesh = barrels[i].GetComponent<MeshFilter>().sharedMesh;
                Vector3[] front = mesh.vertices.Where(v => v.z > mesh.bounds.max.z - 0.001f).Distinct().ToArray();
                Vector3 center = front.Aggregate(Vector3.zero, (sum, v) => sum + v) / front.Length;
                muzzles[i] = new GameObject("Muzzle_" + (i + 1)).transform;
                muzzles[i].SetParent(barrels[i], false);
                muzzles[i].localPosition = center + Vector3.forward * 0.015f;
            }

            if (muzzles.Length != (variant == "Double" ? 2 : 1))
            {
                throw new InvalidOperationException("Unexpected source barrel count: " + sourcePath);
            }

            ProjectileTurret turret = root.AddComponent<ProjectileTurret>();
            turret.horizontalAxis = yaw;
            turret.verticalAxis = pitch;
            turret.aimPivot = pitch;
            turret.muzzle = muzzles[0];
            turret.damage = 12f;
            turret.fireInterval = 0.6f;
            turret.range = 45f;
            SerializedObject serialized = new SerializedObject(turret);
            SerializedProperty sockets = serialized.FindProperty("_muzzles");
            SerializedProperty recoil = serialized.FindProperty("_recoilBarrels");
            recoil.arraySize = barrels.Length;
            sockets.arraySize = muzzles.Length;
            for (int i = 0; i < muzzles.Length; i++)
            {
                sockets.GetArrayElementAtIndex(i).objectReferenceValue = muzzles[i];
                recoil.GetArrayElementAtIndex(i).objectReferenceValue = barrels[i];
            }

            serialized.FindProperty("_projectilePrefab").objectReferenceValue = projectile;
            serialized.FindProperty("_projectileSpeed").floatValue = 42f;
            serialized.FindProperty("_projectileLifetime").floatValue = 2f;
            serialized.FindProperty("_impactImpulse").floatValue = 10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Power power = root.GetComponent<Power>();
            power.minWorkingPower = demand * 0.5f;
            power.standardWorkingPower = demand;
            StatusIcon info = root.GetComponent<StatusIcon>();
            info.blockName = name;
            info.description = variant + " shell turret: one physical shell per barrel per volley.";
            root.GetComponent<Block>().resourcePath = "Blocks/" + name;
            PrefabUtility.SaveAsPrefabAsset(root, Blocks + name + ".prefab");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Sprite RenderIcon(string name)
    {
        string iconPath = "Assets/Art/BuildPalette/Icons/" + name + ".png";
        BlockArtPreview.Render(Blocks + name + ".prefab", iconPath, front: true, transparent: true);
        AssetDatabase.ImportAsset(iconPath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    }

    private static void BakeEntry(string name)
    {
        Sprite iconSprite = RenderIcon(name);
        GameObject entry = new GameObject(name + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(BuildPaletteItem));
        try
        {
            Image outline = entry.GetComponent<Image>();
            outline.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/BuildPalette/RoundedOutline.png");
            outline.type = Image.Type.Sliced;
            Button button = entry.GetComponent<Button>();
            button.targetGraphic = outline;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.9f, 1f);
            colors.pressedColor = new Color(0.2f, 0.65f, 0.85f);
            button.colors = colors;
            SerializedObject serialized = new SerializedObject(entry.GetComponent<BuildPaletteItem>());
            serialized.FindProperty("_palette").objectReferenceValue = null;
            serialized.FindProperty("_outline").objectReferenceValue = outline;
            serialized.FindProperty("_blockName").stringValue = name;
            serialized.FindProperty("_info").stringValue = AssetDatabase.LoadAssetAtPath<GameObject>(Blocks + name + ".prefab").GetComponent<Block>().GetDisplayInfo();
            serialized.FindProperty("_category").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            RectTransform preview = new GameObject("Preview", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            preview.SetParent(entry.transform, false);
            preview.anchorMin = Vector2.zero;
            preview.anchorMax = Vector2.one;
            preview.offsetMin = new Vector2(3f, 3f);
            preview.offsetMax = new Vector2(-3f, -3f);
            Image image = preview.GetComponent<Image>();
            image.sprite = iconSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            entry.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(entry, Entries + "/" + name + ".prefab");
        }
        finally
        {
            Object.DestroyImmediate(entry);
        }
    }
}
