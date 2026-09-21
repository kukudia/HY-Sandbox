using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Explicit, save-once migration. Existing ArtIntegration markers protect later hand edits.</summary>
public static class BlockArtIntegrator
{
    private const string Kit = "Assets/Art/SpaceKit/Prefabs/";
    private const string Blocks = "Assets/Resources/Blocks/";
    private const string Marker = "ArtIntegration_SpaceKit_v1";

    [MenuItem("Tools/HY Sandbox/Block Art/Complete Missing Block Integration")]
    public static void Integrate()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        BlockVfxBaker.BakeMissing();
        // Standalone Bot first; the container then receives that completed, completely unpacked copy.
        IntegratePrefab(Blocks + "Bot.prefab");
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Blocks.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("/Bot.prefab")) IntegratePrefab(path);
        }
        AssetDatabase.SaveAssets();
    }

    private static void IntegratePrefab(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.transform.Find(Marker) != null) return;
            if (root.name == "Connector" || char.IsDigit(root.name[0])) return;
            BlockArtDependencies.Unpack(root);
            Transform model = root.name == "Bot" ? root.transform : root.transform.Find("Model");
            if (model == null) throw new InvalidOperationException(path + " is missing Model.");

            // Salvage authored asset children, including the radar placed inside old LOD0.
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true).ToArray())
                if (t.name.StartsWith("SM_") && !t.parent.name.StartsWith("SM_") && t.parent != model
                    && (root.name == "Bot" || t.GetComponentInParent<RepairBot>() == null))
                    t.SetParent(model, true);
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true).ToArray())
                if (t != null && t.name == "IndustrialVisual") Object.DestroyImmediate(t.gameObject);
            foreach (var motion in root.GetComponentsInChildren<IndustrialPartMotion>(true)) Object.DestroyImmediate(motion);

            switch (root.name)
            {
                case "Bot": ConfigureBot(root); break;
                case "Cockpit": Fit(Add("Modules/Cockpits/Polygon/SM_Veh_Part_Cockpit_03.prefab", model), new Vector3(1.85f, 1.8f, 1.85f), Vector3.zero); break;
                case "Door":
                    Fit(Add("Construction/Panels/Polygon/SM_Bld_Wall_Doorframe_01.prefab", model), new Vector3(0.95f, 1.95f, 0.35f), Vector3.zero);
                    Fit(Add("Props/Cargo/Polygon/SM_Prop_Crate_Shield_01.prefab", model, "Assets/Art/Temp/SM_Prop_Crate_Shield_01.prefab"), new Vector3(0.63f, 1.57f, 0.16f), Vector3.zero, false);
                    break;
                case "HoverFlightController":
                    Fit(Add("Modules/Sensors/Polygon/SM_Prop_Radar_Panel_02.prefab", model), new Vector3(0.85f, 0.9f, 0.85f), Vector3.zero);
                    break;
                case "Rack":
                    Fit(Add("", model, "Assets/Art/Temp/SM_Prop_Crate_Wide_01.prefab"), new Vector3(0.86f, 0.36f, 0.82f), new Vector3(0f, -0.26f, 0f), false);
                    foreach (float x in new[] { -0.39f, 0.39f })
                        Fit(Add("Construction/Structures/Polygon/SM_Prop_Detail_Struts_01.prefab", model), new Vector3(0.14f, 0.9f, 0.82f), new Vector3(x, 0f, 0f), false);
                    break;
                case "Stairs": Fit(Add("Construction/Structures/Polygon/SM_Prop_Stairs_03.prefab", model), new Vector3(0.92f, 0.94f, 0.94f), Vector3.zero, false); break;
                case "MainThruster":
                    var engine = Add("Modules/Thrusters/Polygon/SM_Veh_Part_Engine_03.prefab", model);
                    engine.localScale = new Vector3(0.23f, 0.42f, 0.15f);
                    ConfigureMain(root, model); break;
                case "MainThrusterBig": ConfigureMain(root, model); break;
                case "HoverThruster": case "HoverThrusterBig": ConfigureHover(root, model); break;
                case "UniversalThruster": ConfigureUniversal(root, model); break;
                case "UniversalThrusterBig":
                    var universal = Add("", model, "Assets/Art/Temp/SM_Prop_Turret_Base_Double_010.prefab");
                    universal.localPosition = new Vector3(0f, -0.6f, 0f);
                    universal.localScale = Vector3.one * 0.9f;
                    ConfigureUniversal(root, model); break;
                case "Turret": ConfigureTurret(root); break;
                case "RepairBotContianer": ConfigureBay(root, model); break;
                case "PowerGeneratingUnit": StatusLights(root, model, true); break;
                case "PowerTransmissionDevice":
                    var transmission = root.GetComponent<PowerTransmissionDevice>();
                    if (transmission.debugCube != null) transmission.debugCube.SetActive(false);
                    break;
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                if (collider.transform != root.transform && collider.GetComponent<RepairBot>() == null) Object.DestroyImmediate(collider);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.layer = root.layer;
                if (renderer is MeshRenderer) renderer.receiveShadows = true;
            }
            var marker = new GameObject(Marker); marker.transform.SetParent(root.transform, false);
            BlockArtDependencies.Unpack(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Transform Add(string relative, Transform parent, string fallback = null)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + relative);
        if (source == null && fallback != null) source = AssetDatabase.LoadAssetAtPath<GameObject>(fallback);
        if (source == null) throw new FileNotFoundException(relative + " / " + fallback);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        BlockArtDependencies.Unpack(instance);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return instance.transform;
    }

    private static void Fit(Transform model, Vector3 size, Vector3 center, bool uniform = true)
    {
        var meshes = model.GetComponentsInChildren<MeshFilter>(true);
        var bounds = new Bounds(); bool first = true;
        foreach (var mesh in meshes)
        {
            if (mesh.sharedMesh == null) continue;
            Bounds b = mesh.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var point = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                point = model.InverseTransformPoint(mesh.transform.TransformPoint(point));
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
            }
        }
        Vector3 scale = new Vector3(size.x / Mathf.Max(bounds.size.x, 0.001f), size.y / Mathf.Max(bounds.size.y, 0.001f), size.z / Mathf.Max(bounds.size.z, 0.001f));
        if (uniform) scale = Vector3.one * Mathf.Min(scale.x, scale.y, scale.z);
        model.localScale = scale;
        model.localPosition = center - Vector3.Scale(bounds.center, scale);
    }

    private static Transform Socket(Transform parent, string name, Vector3 position, Vector3 forward)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false); t.localPosition = position;
        t.localRotation = Quaternion.LookRotation(forward, Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up);
        return t;
    }

    private static AssetParticleEffect Effect(Transform socket, string name, float scale)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(BlockVfxBaker.Load(name), socket);
        BlockArtDependencies.Unpack(go);
        go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
        Vector3 inherited = socket.lossyScale;
        go.transform.localScale = new Vector3(scale / Mathf.Abs(inherited.x), scale / Mathf.Abs(inherited.y), scale / Mathf.Abs(inherited.z));
        return go.GetComponent<AssetParticleEffect>();
    }

    private static void BindThruster(GameObject root, Transform movingModel)
    {
        root.GetComponent<Thruster>().model = movingModel;
        var controller = root.GetComponent<ThrusterVisualEffect>();
        if (controller == null) controller = root.AddComponent<ThrusterVisualEffect>();
        BlockVfxBaker.SetObjects(controller, "_nozzles", movingModel.GetComponentsInChildren<AssetParticleEffect>(true));
    }

    private static void ConfigureMain(GameObject root, Transform model)
    {
        foreach (var engine in model.GetComponentsInChildren<MeshFilter>(true).Where(m => m.name.StartsWith("SM_Veh_Part_Engine_03")))
            foreach (float x in new[] { -1.0675f, 1.0675f })
                Effect(Socket(engine.transform, x < 0 ? "Nozzle_Left" : "Nozzle_Right", new Vector3(x, 0f, -2.9854f), Vector3.back), "ThrusterJet", root.name.EndsWith("Big") ? 1.5f : 0.78f);
        BindThruster(root, model);
    }

    private static void ConfigureHover(GameObject root, Transform model)
    {
        var vent = model.GetComponentsInChildren<MeshFilter>(true).First(m => m.name.StartsWith("SM_Prop_AirVent"));
        // The vent's front is +Z; the user's X=90 rotation turns it into the downward-facing outlet.
        Effect(Socket(vent.transform, "Nozzle_Down", new Vector3(0f, 0f, 0.5257f), Vector3.forward), "HoverJet", root.name.EndsWith("Big") ? 2.7f : 1.35f);
        BindThruster(root, model);
    }

    private static void ConfigureUniversal(GameObject root, Transform model)
    {
        var head = model.GetComponentsInChildren<Transform>(true).First(t => t.name == "SM_Prop_Turret_MissileLarge_Base_Double_01");
        foreach (float x in new[] { -0.588f, 0.588f })
            Effect(Socket(head, x < 0 ? "Nozzle_Left" : "Nozzle_Right", new Vector3(x, -0.019f, -0.787f), Vector3.back), "ThrusterJet", root.name.EndsWith("Big") ? 1.6f : 0.8f);
        BindThruster(root, head);
    }

    private static void ConfigureTurret(GameObject root)
    {
        var weapon = root.GetComponent<TurretWeapon>();
        weapon.horizontalAxis = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "SM_Prop_Turret_Large_Top_01");
        weapon.verticalAxis = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "SM_Prop_Turret_Large_Barrel_01");
        weapon.aimPivot = weapon.verticalAxis;
        // Measured from the barrel end ring vertices, rather than the renderer's enclosing box.
        weapon.muzzle = Socket(weapon.verticalAxis, "Muzzle", new Vector3(0f, 0.0645f, 4.479f), Vector3.forward);
        BlockVfxBaker.SetObject(weapon, "_muzzleFlash", Effect(weapon.muzzle, "MuzzleFlash", 0.45f));
    }

    private static void ConfigureBot(GameObject root)
    {
        var renderer = root.GetComponent<MeshRenderer>();
        if (renderer != null) Object.DestroyImmediate(renderer);
        var filter = root.GetComponent<MeshFilter>();
        if (filter != null) Object.DestroyImmediate(filter);
        var bot = root.GetComponent<RepairBot>();
        var drone = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "SM_Veh_Drone_Repair_01");
        // The imported drone faces -Z, while navigation steers transform.forward (+Z).
        drone.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var origin = Socket(drone, "RepairOrigin", new Vector3(-0.0205f, 0.14f, -0.998f), Vector3.back);
        BlockVfxBaker.SetObject(bot, "_repairOrigin", origin);
        var flight = new GameObject("Flight Effects"); flight.transform.SetParent(drone, false);
        foreach (float x in new[] { -0.8175f, 0.8175f })
            Effect(Socket(flight.transform, x < 0 ? "Exhaust_Left" : "Exhaust_Right", new Vector3(x, 0.149f, 0.985f), Vector3.forward), "BotFlight", 0.65f);
        var controller = flight.AddComponent<AssetParticleEffect>();
        BlockVfxBaker.SetObjects(controller, "_particles", flight.GetComponentsInChildren<ParticleSystem>(true));
        BlockVfxBaker.SetObject(bot, "_flightEffect", controller);
        var impact = Effect(Socket(root.transform, "RepairTargetEffect", Vector3.zero, Vector3.forward), "RepairContact", 1f);
        BlockVfxBaker.SetObject(bot, "_repairImpact", impact);
        for (int i = 0; i < 2; i++)
        {
            var t = Socket(drone, i == 0 ? "FlightTrail" : "FlightTrailCore", new Vector3(0f, 0f, 0.8f), Vector3.forward);
            var trail = t.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/BlockVisuals/Materials/Beam and Trail.mat");
            trail.time = i == 0 ? 0.5f : 0.3f; trail.widthMultiplier = i == 0 ? 0.065f : 0.025f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.startColor = new Color(0.1f, 0.8f, 1f, 0.6f); trail.endColor = new Color(0.1f, 0.4f, 1f, 0f);
            trail.minVertexDistance = 0.04f; trail.emitting = false;
            BlockVfxBaker.SetObject(bot, i == 0 ? "_flightTrail" : "_flightCoreTrail", trail);
        }
    }

    private static void ConfigureBay(GameObject root, Transform model)
    {
        var old = model.GetComponentInChildren<RepairBot>(true);
        Vector3 position = old.transform.localPosition;
        Vector3 scale = old.transform.localScale;
        Object.DestroyImmediate(old.gameObject);
        var drone = Add("", model, Blocks + "Bot.prefab");
        drone.name = "Bot"; drone.localPosition = position; drone.localScale = scale;
        var bot = drone.GetComponent<RepairBot>();
        bot.home = model; bot.homeOffset = position; bot.outside = root.transform.Find("Outside");
        if (bot.outside == null) bot.outside = Socket(root.transform, "Outside", Vector3.zero, Vector3.forward);
        StatusLights(root, model, false);
    }

    private static void StatusLights(GameObject root, Transform model, bool generator)
    {
        var lights = new Light[2];
        for (int i = 0; i < 2; i++)
        {
            float sign = i == 0 ? -1f : 1f;
            Vector3 position = generator ? new Vector3(sign * 0.65f, 0.25f, -0.48f) : new Vector3(sign * 0.34f, -0.23f, -0.32f);
            var lamp = Add("Props/Lights/Polygon/SM_Prop_Light_Small_01.prefab", model);
            Fit(lamp, Vector3.one * (generator ? 0.2f : 0.12f), position);
            var glow = Socket(model, "StatusLight_" + i, position + Vector3.back * 0.06f, Vector3.back);
            var light = glow.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.color = new Color(0.12f, 0.8f, 1f);
            light.intensity = generator ? 1.4f : 1f; light.range = generator ? 2.2f : 1.1f; light.shadows = LightShadows.None;
            lights[i] = light;
        }
        var status = root.AddComponent<BlockStatusLight>();
        BlockVfxBaker.SetObjects(status, "_lights", lights);
        BlockVfxBaker.SetObject(status, "_generator", root.GetComponent<PowerGeneratingUnit>());
        BlockVfxBaker.SetObject(status, "_bot", root.GetComponentInChildren<RepairBot>(true));
    }
}
