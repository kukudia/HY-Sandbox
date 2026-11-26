// 职责：制作可重现的URP结构材质、碎片网格/粒子Prefab及目录配置，安装基础结构支持组件。
// 入口：Install()；现有材质/配置不覆盖，重复执行只补缺失资源，保留手动参数。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BlockMaterialAuthoring
{
    private const string Root = "Assets/Resources/Building/Materials";
    [MenuItem("Tools/HY-Sandbox/Building/Install structural materials")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before authoring materials.");
        }

        Directory.CreateDirectory(Root);
        AssetDatabase.Refresh();
        string catalogPath = "Assets/Resources/Building/BlockMaterials.asset";
        BlockMaterialCatalog catalog = AssetDatabase.LoadAssetAtPath<BlockMaterialCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BlockMaterialCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
            string[] ids =
            {
                "metal",
                "wood",
                "stone",
                "glass"
            };
            string[] names =
            {
                "金属",
                "木材",
                "石头",
                "玻璃"
            };
            Color[] colors =
            {
                new Color(.45f, .54f, .63f),
                new Color(.46f, .25f, .10f),
                new Color(.42f, .44f, .46f),
                new Color(.48f, .85f, .95f, .28f)
            };
            float[] densities =
            {
                2f,
                .6f,
                2.6f,
                1.2f
            };
            float[] durability =
            {
                150f,
                65f,
                190f,
                30f
            };
            int[] cost =
            {
                8,
                3,
                5,
                6
            };
            using var serialized = new SerializedObject(catalog);
            var items = serialized.FindProperty("materials");
            items.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                Material material = Surface(ids[i], colors[i]);
                MaterialBreakEffect effect = Break(ids[i], material);
                var item = items.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("id").stringValue = ids[i];
                item.FindPropertyRelative("displayName").stringValue = names[i];
                item.FindPropertyRelative("density").floatValue = densities[i];
                item.FindPropertyRelative("durabilityPerCell").floatValue = durability[i];
                item.FindPropertyRelative("coinsPerCell").intValue = cost[i];
                item.FindPropertyRelative("surface").objectReferenceValue = material;
                item.FindPropertyRelative("breakEffect").objectReferenceValue = effect;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (string name in new[]
        {
            "1x1x1",
            "2x1x1",
            "2x2x1",
            "2x2x2"
        }

        )
        {
            string path = "Assets/Resources/Blocks/" + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<BlockMaterial>() == null)
                {
                    BlockMaterial surface = root.AddComponent<BlockMaterial>();
                    using var data = new SerializedObject(surface);
                    var targets = data.FindProperty("surfaces");
                    MeshRenderer[] renderers = root.transform.Find("Model").GetComponentsInChildren<MeshRenderer>(true);
                    targets.arraySize = renderers.Length;
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        targets.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                    }

                    data.ApplyModifiedPropertiesWithoutUndo();
                    // Empty materialId deliberately preserves existing blueprints and Prefab values.
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Structural materials installed: four editable definitions and bounded cosmetic fragments.");
    }

    [MenuItem("Tools/HY-Sandbox/Building/Install material selector in Main")]
    public static void InstallSelector()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Build.unity");
        if (!scene.isLoaded || scene.isDirty)
        {
            throw new InvalidOperationException("Open clean Main before installing the selector.");
        }

        BuildPalette palette = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BuildPalette>(true)).Single();
        Transform parent = palette.transform;
        Directory.CreateDirectory("Temp/Building");
        File.Copy(scene.path, "Temp/Building/Main-before-material-selector.unity", true);
        GameObject control = parent.Find("MaterialSelector") != null ? parent.Find("MaterialSelector").gameObject : DropdownTemplateAuthoring.Create(new UnityEngine.UI.DefaultControls.Resources());
        control.name = "MaterialSelector";
        control.transform.SetParent(parent, false);
        var rect = (RectTransform)control.transform;
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.offsetMin = new Vector2(6, 4);
        rect.offsetMax = new Vector2(-14, 34);
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ChakraPetch-Medium.ttf");
        foreach (var text in control.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            text.font = font;
            text.fontSize = 14;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(.7f, .9f, 1f);
        }

        control.GetComponent<UnityEngine.UI.Image>().color = new Color(.025f, .06f, .09f, .98f);
        var dropdown = control.GetComponent<UnityEngine.UI.Dropdown>();
        dropdown.options = BlockMaterialCatalog.Instance.Materials.Select(item => new UnityEngine.UI.Dropdown.OptionData(item.id.ToUpperInvariant() + " · " + item.coinsPerCell + "/m³")).ToList();
        var label = dropdown.captionText.rectTransform;
        label.offsetMin = new Vector2(8, 0);
        label.offsetMax = new Vector2(-28, 0);
        dropdown.RefreshShownValue();
        foreach (var image in control.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            image.color = new Color(.025f, .06f, .09f, .98f);
        }

        Transform arrow = control.transform.Find("Arrow");
        arrow.GetComponent<UnityEngine.UI.Image>().enabled = false;
        Transform arrowLabel = arrow.Find("Label");
        if (arrowLabel == null)
        {
            arrowLabel = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)).transform;
            arrowLabel.SetParent(arrow, false);
            var area = (RectTransform)arrowLabel;
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
        }

        var arrowText = arrowLabel.GetComponent<UnityEngine.UI.Text>();
        arrowText.font = font;
        arrowText.text = "v";
        arrowText.fontSize = 15;
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.color = new Color(.7f, .9f, 1f);
        if (control.GetComponent<BuildMaterialSelector>() == null)
        {
            control.AddComponent<BuildMaterialSelector>();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    }

    private static Material Surface(string id, Color color)
    {
        string path = Root + "/" + id + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            name = id
        };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", id == "metal" ? .75f : 0f);
        material.SetFloat("_Smoothness", id == "glass" ? .95f : id == "metal" ? .6f : .15f);
        if (id == "glass")
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            Texture2D texture = Texture(id);
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", Vector2.one * 2f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Texture2D Texture(string id)
    {
        string path = Root + "/" + id + "-detail.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGB24, false);
            var random = new System.Random(id == "wood" ? 171 : id == "stone" ? 172 : 173);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float noise = (float)random.NextDouble();
                    float value = id == "wood" ? .66f + .2f * Mathf.Sin(x * 1.4f + Mathf.Sin(y * .16f)) + noise * .12f : id == "metal" ? .88f + noise * .10f : .65f + noise * .3f;
                    texture.SetPixel(x, y, new Color(value, value, value));
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static MaterialBreakEffect Break(string id, Material surface)
    {
        string path = Root + "/" + id + "-break.prefab";
        MaterialBreakEffect existing = AssetDatabase.LoadAssetAtPath<MaterialBreakEffect>(path);
        if (existing != null)
        {
            return existing;
        }

        string meshPath = Root + "/" + id + "-fragment.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = id + " fragment"
            };
            float thickness = id == "glass" ? .04f : id == "wood" ? .18f : .5f;
            mesh.vertices = new[]
            {
                new Vector3(-.5f, -.3f, 0),
                new Vector3(.5f, -.2f, 0),
                new Vector3(0, .6f, 0),
                new Vector3(0, 0, thickness)
            };
            mesh.triangles = new[]
            {
                0,
                2,
                1,
                0,
                1,
                3,
                1,
                2,
                3,
                2,
                0,
                3
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        var root = new GameObject(id + " fragments", typeof(ParticleSystem), typeof(MaterialBreakEffect));
        try
        {
            var system = root.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = .15f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(.12f, id == "glass" ? .38f : .3f);
            main.gravityModifier = .8f;
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(id == "glass" ? 24 : 14)) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.randomDirectionAmount = 1f;
            var spin = system.rotationOverLifetime;
            spin.enabled = true;
            spin.separateAxes = true;
            spin.x = new ParticleSystem.MinMaxCurve(-5f, 5f);
            spin.y = new ParticleSystem.MinMaxCurve(-7f, 7f);
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            renderer.sharedMaterial = surface;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<MaterialBreakEffect>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
