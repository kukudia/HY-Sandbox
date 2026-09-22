using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SalvageAssetBaker
{
    private const string Art = "Assets/Art/Salvage";
    private const string Blocks = "Assets/Resources/Blocks/";
    private static Material _frame, _glass, _lamp, _gold, _blue;

    [MenuItem("Tools/Salvage/Bake assets (create missing)")]
    public static void Bake()
    {
        Directory.CreateDirectory(Art + "/Materials");
        Directory.CreateDirectory("Assets/Resources/Salvage");
        AssetDatabase.Refresh();
        _frame = Material("Frame", new Color(0.075f, 0.11f, 0.15f), false, false);
        _glass = Material("Glass", new Color(0.23f, 0.72f, 0.8f, 0.12f), true, false);
        _lamp = Material("Status", new Color(0.1f, 0.85f, 1f), false, true);
        _gold = Material("Gold", new Color(1f, 0.58f, 0.025f), false, true);
        _blue = Material("Technology", new Color(0.03f, 0.32f, 1f), false, true);
        Hold("CargoHold", CargoKind.SpecialPart, 2, 8, true, true);
        Hold("CoinHold", CargoKind.Coins, 1, 100, false, false);
        Hold("TechnologyHold", CargoKind.Technology, 1, 100, false, false);
        CollectionBay();
        DropPrefab();
        if (!File.Exists("Assets/Resources/Salvage/Settings.asset"))
        {
            var settings = ScriptableObject.CreateInstance<SalvageSettings>();
            settings.specialPartResources = new[] { "Blocks/PowerGeneratingUnit", "Blocks/Turret", "Blocks/UniversalThruster", "Blocks/HoverThruster", "Blocks/RepairBotContianer" };
            AssetDatabase.CreateAsset(settings, "Assets/Resources/Salvage/Settings.asset");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Salvage assets baked. Existing assets and authored overrides preserved.");
    }

    private static Material Material(string name, Color color, bool transparent, bool emissive)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", transparent ? 0.85f : 0.45f);
        material.SetFloat("_Metallic", transparent ? 0f : 0.45f);
        if (emissive) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.5f); }
        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 2f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Hold(string name, CargoKind kind, int size, int capacity, bool explosive, bool enemyDrops)
    {
        string path = Blocks + name + ".prefab";
        if (File.Exists(path)) return;
        GameObject root = PrefabUtility.LoadPrefabContents(Blocks + (size == 2 ? "2x2x2" : "1x1x1") + ".prefab");
        try
        {
            root.name = name;
            Transform old = root.transform.Find("Model");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            foreach (Renderer renderer in root.GetComponents<Renderer>()) Object.DestroyImmediate(renderer);
            var model = new GameObject("Model").transform; model.SetParent(root.transform, false);
            Block block = root.GetComponent<Block>();
            block.canExplode = explosive; block.resourcePath = "Blocks/" + name; block.uniqueId = string.Empty;
            var power = root.GetComponent<Power>() ?? root.AddComponent<Power>();
            power.minWorkingPower = 10f; power.standardWorkingPower = 20f;
            var cargo = root.AddComponent<CargoHold>();
            var settings = new SerializedObject(cargo);
            settings.FindProperty("_kind").enumValueIndex = (int)kind;
            settings.FindProperty("_capacity").intValue = capacity;
            settings.FindProperty("_dropEnemyContents").boolValue = enemyDrops;
            settings.ApplyModifiedPropertiesWithoutUndo();
            float edge = size * 0.46f;
            float beam = size * 0.065f;
            for (int axis = 0; axis < 3; axis++)
                foreach (float a in new[] { -edge, edge }) foreach (float b in new[] { -edge, edge })
                {
                    Vector3 position = Vector3.zero, scale = Vector3.one * beam;
                    scale[axis] = size * 0.96f; position[(axis + 1) % 3] = a; position[(axis + 2) % 3] = b;
                    Cube("Frame rail", model, position, scale, _frame);
                }
            foreach (float sign in new[] { -1f, 1f })
            {
                Cube("Glass front/back", model, new Vector3(0f, 0f, sign * edge), new Vector3(size * 0.87f, size * 0.87f, 0.018f), _glass);
                Cube("Glass side", model, new Vector3(sign * edge, 0f, 0f), new Vector3(0.018f, size * 0.87f, size * 0.87f), _glass);
            }
            Cube("Base", model, Vector3.down * edge, new Vector3(size * 0.89f, beam, size * 0.89f), _frame);
            var lamp = Cube("Cargo status strip", model, new Vector3(0, edge, -edge - beam * 0.6f), new Vector3(size * 0.5f, beam * 0.55f, 0.025f), _lamp);
            var contents = new GameObject("Contents display").transform; contents.SetParent(model, false);
            var view = root.AddComponent<CargoHoldView>();
            Set(view, "_contentsRoot", contents); Set(view, "_statusLamp", lamp.GetComponent<Renderer>());
            var viewSettings = new SerializedObject(view);
            viewSettings.FindProperty("_displaySize").vector3Value = Vector3.one * (size * 0.81f);
            viewSettings.ApplyModifiedPropertiesWithoutUndo();
            if (kind != CargoKind.SpecialPart)
            {
                var liquid = Cube("Liquid fill", model, Vector3.down * 0.36f, new Vector3(0.8f, 0.08f, 0.8f), kind == CargoKind.Coins ? _gold : _blue);
                Set(view, "_liquid", liquid.transform);
                liquid.SetActive(false);
                for (int i = 1; i <= 4; i++) Cube("Capacity tick " + i, model, new Vector3(0.27f, -0.4f + i * 0.16f, -0.473f), new Vector3(0.13f, 0.018f, 0.025f), _lamp);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void CollectionBay()
    {
        string path = Blocks + "CollectionBotContainer.prefab";
        if (File.Exists(path)) return;
        GameObject root = PrefabUtility.LoadPrefabContents(Blocks + "RepairBotContianer.prefab");
        try
        {
            root.name = "CollectionBotContainer";
            // Unpack the nested drone so changing its job does not mutate the repair prefab.
            RepairBot repair = root.GetComponentInChildren<RepairBot>(true);
            if (PrefabUtility.IsPartOfPrefabInstance(repair.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(repair.gameObject), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            CollectionBot collector = repair.gameObject.AddComponent<CollectionBot>();
            var source = new SerializedObject(repair);
            var destination = new SerializedObject(collector);
            var iterator = source.GetIterator();
            while (iterator.NextVisible(true))
                if (iterator.propertyPath != "m_Script" && destination.FindProperty(iterator.propertyPath) != null) destination.CopyFromSerializedProperty(iterator);
            destination.ApplyModifiedPropertiesWithoutUndo();
            collector.targetRange = 60;
            collector.findTargetInterval = 0.5f;
            collector.movementSpeed = 8f;
            collector.obstacleMask = LayerMask.GetMask("Block", "Default");
            collector.showAvoidanceZones = collector.showAvoidanceRays = collector.showDirectionVectors = false;
            foreach (BlockStatusLight status in root.GetComponentsInChildren<BlockStatusLight>(true)) Set(status, "_bot", collector);
            Object.DestroyImmediate(repair);
            Transform impact = collector.transform.Find("RepairTargetEffect");
            if (impact != null) Object.DestroyImmediate(impact.gameObject);
            var beam = collector.GetComponent<StylizedBeamEffect>();
            if (beam != null) Object.DestroyImmediate(beam);
            var legacy = collector.GetComponent<LineRenderer>();
            if (legacy != null) Object.DestroyImmediate(legacy);
            var socket = new GameObject("Cargo socket").transform; socket.SetParent(collector.transform, false);
            Set(collector, "_carrySocket", socket);
            Cube("Recovery identification", root.transform.Find("Model"), new Vector3(0f, 0.37f, -0.35f), new Vector3(0.5f, 0.06f, 0.06f), _gold);
            Power power = root.GetComponent<Power>() ?? root.AddComponent<Power>();
            power.minWorkingPower = 10f; power.standardWorkingPower = 20f;
            root.GetComponent<Block>().resourcePath = "Blocks/CollectionBotContainer";
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void DropPrefab()
    {
        string path = "Assets/Resources/Salvage/LootDrop.prefab";
        if (File.Exists(path)) return;
        var root = new GameObject("LootDrop");
        try
        {
            var drop = root.AddComponent<LootDrop>();
            var display = new GameObject("Item display").transform; display.SetParent(root.transform, false);
            var beacon = Cube("Loot beacon", root.transform, Vector3.down * 0.45f, new Vector3(0.38f, 0.08f, 0.38f), _lamp);
            beacon.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Set(drop, "_displayRoot", display); Set(drop, "_beacon", beacon.GetComponent<Renderer>());
            var label = new GameObject("Loot label").AddComponent<TextMesh>();
            label.transform.SetParent(root.transform, false); label.transform.localPosition = Vector3.up * 0.6f;
            label.anchor = TextAnchor.MiddleCenter; label.characterSize = 0.055f; label.fontSize = 48; label.color = Color.white;
            Set(drop, "_label", label);
            var burstObject = new GameObject("Coin burst"); burstObject.transform.SetParent(root.transform, false);
            var burst = burstObject.AddComponent<ParticleSystem>(); burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = burst.main; main.playOnAwake = false; main.loop = false; main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f); main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = 0.1f; main.maxParticles = 16; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = burst.emission; emission.enabled = false;
            var shape = burst.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.15f;
            burst.GetComponent<ParticleSystemRenderer>().sharedMaterial = _gold;
            Set(drop, "_burst", burst);
            var trail = root.AddComponent<TrailRenderer>(); trail.sharedMaterial = _gold; trail.time = 0.22f;
            trail.startWidth = 0.09f; trail.endWidth = 0f; trail.minVertexDistance = 0.1f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name; cube.transform.SetParent(parent, false); cube.transform.localPosition = position; cube.transform.localScale = scale;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.GetComponent<Renderer>().sharedMaterial = material;
        cube.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return cube;
    }

    public static void Set(Object target, string property, Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Salvage/Render prefab previews")]
    public static void Preview()
    {
        foreach (string name in new[] { "CargoHold", "CoinHold", "TechnologyHold", "CollectionBotContainer" })
            BlockArtPreview.Render(Blocks + name + ".prefab", Art + "/Previews/" + name + ".png");
        foreach (string name in new[] { "CargoHold", "CoinHold", "TechnologyHold" })
            BlockArtPreview.Render(Blocks + name + ".prefab", Art + "/Previews/" + name + "-loaded.png", configure: root =>
            {
                CargoHold hold = root.GetComponent<CargoHold>();
                hold.SendMessage("Awake");
                root.GetComponent<Power>().currentPower = 1000f;
                hold.RestoreContents(hold.Kind == CargoKind.SpecialPart
                    ? new System.Collections.Generic.List<CargoItem> { new CargoItem { kind = CargoKind.SpecialPart, resourcePath = "Blocks/UniversalThruster", amount = 4 }, new CargoItem { kind = CargoKind.SpecialPart, resourcePath = "Blocks/PowerGeneratingUnit", amount = 4 } }
                    : new System.Collections.Generic.List<CargoItem> { new CargoItem { kind = hold.Kind, amount = hold.Kind == CargoKind.Coins ? 75 : 35 } });
                CargoHoldView display = root.GetComponent<CargoHoldView>(); display.SendMessage("Awake"); display.SendMessage("LateUpdate");
            });
        AssetDatabase.Refresh();
    }
}
