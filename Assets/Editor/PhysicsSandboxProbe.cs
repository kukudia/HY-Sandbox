using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Real PhysX probes, isolated from Main objects, saves and input.</summary>
public static class PhysicsSandboxProbe
{
    public static string Run()
    {
        if (!Application.isPlaying || MainUIPanels.instance == null) throw new InvalidOperationException("Run in Main Play Mode.");
        var checks = new List<string>();
        bool previousMode = PlayManager.instance.playMode;
        Scene scene = SceneManager.CreateScene("PhysicsSandboxProbe", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            PlayManager.instance.playMode = true;
            Check(checks, "Reduced mass: equal dynamic pair", Mathf.Approximately(ImpactPhysics.ReducedMass(10f, 10f, false), 5f));
            Check(checks, "Static target uses full moving mass", Mathf.Approximately(ImpactPhysics.ReducedMass(10f, 0f, true), 10f));
            Check(checks, "Safe-speed threshold rejects gentle contact", ImpactPhysics.DamageEnergy(10f, 3f, 5f) == 0f);
            float basic = Collide(scene, 10f, 10f, checks, true);
            float heavy = Collide(scene, 20f, 10f, checks, false);
            float fast = Collide(scene, 10f, 20f, checks, false);
            Check(checks, "Double mass doubles damage", Mathf.Abs(heavy / basic - 2f) < 0.03f);
            Check(checks, "Squared speed and threshold energy", Mathf.Abs(fast / basic - 5f) < 0.04f);
            Rigidbody light = Body(scene, "Light impulse", 10f, Vector3.zero);
            Rigidbody heavyBody = Body(scene, "Heavy impulse", 20f, Vector3.right * 5f);
            light.AddForceAtPosition(Vector3.forward * 10f, light.position + Vector3.right * 0.4f, ForceMode.Impulse);
            heavyBody.AddForce(Vector3.forward * 10f, ForceMode.Impulse);
            scene.GetPhysicsScene().Simulate(0.02f);
            Check(checks, "Impulse respects inverse mass", Mathf.Abs(light.linearVelocity.z / heavyBody.linearVelocity.z - 2f) < 0.03f);
            Check(checks, "Off-centre impulse produces rotation", light.angularVelocity.magnitude > 0.1f);
            foreach (GameObject root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
            var meshObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(meshObject, scene);
            UnityEngine.Object.DestroyImmediate(meshObject.GetComponent<BoxCollider>());
            MeshCollider collider = meshObject.AddComponent<MeshCollider>(); collider.sharedMesh = meshObject.GetComponent<MeshFilter>().sharedMesh; collider.convex = false;
            Vector3 nearest = ImpactPhysics.ClosestPoint(collider, Vector3.right * 5f);
            Check(checks, "Non-convex collider has finite closest point", !float.IsNaN(nearest.x));
            var hud = MainUIPanels.instance.playPanel.GetComponent<PhysicsTelemetryHud>();
            Check(checks, "Physics HUD is persisted under PlayPanel", hud != null && MainUIPanels.instance.playPanel.transform.Find("PhysicsTelemetry/Motion") != null);
            var safetyObject = new GameObject("Collision safety"); SceneManager.MoveGameObjectToScene(safetyObject, scene);
            Durability safety = safetyObject.AddComponent<Durability>();
            safety.ApplyCollisionEnergy(10000000f, 1000f, 1f);
            Check(checks, "Extreme impact capped at eight percent", Mathf.Abs(safety.currentDurability - 92f) < 0.001f);
            safety.ApplyCollisionEnergy(10000000f, 1000f, 1f);
            Check(checks, "Repeated collision cooldown", Mathf.Abs(safety.currentDurability - 92f) < 0.001f);
            safety.enabled = false; safety.enabled = true;
            safety.ProtectFromExplosionCollisions(3f);
            safety.ApplyCollisionEnergy(10000000f, 1000f, 1f);
            Check(checks, "Blast protection prevents collision damage", Mathf.Approximately(safety.currentDurability, 100f));
            var blast = new GameObject("Probe explosion"); SceneManager.MoveGameObjectToScene(blast, scene);
            Block source = blast.AddComponent<Block>(); source.explosionRadius = 8f; source.explosionForce = 10000f;
            Rigidbody fragment = Body(scene, "Blast fragment", 1f, Vector3.right * 2f);
            ControlUnit fragmentUnit = fragment.gameObject.AddComponent<ControlUnit>(); fragmentUnit.enabled = false;
            var armorObject = new GameObject("Fragment armor"); armorObject.transform.SetParent(fragment.transform, false);
            Durability armor = armorObject.AddComponent<Durability>(); armor.debugLog = false;
            // ApplyExplosionForce queries the default scene; use the saved ownership path for this isolated scene.
            fragmentUnit.EnsureRuntimeUnitId();
            Block fragmentBlock = armorObject.AddComponent<Block>();
            RuntimeUnitMember.Ensure(armorObject, fragmentUnit.runtimeUnitId, UnitFaction.Enemy);
            Physics.SyncTransforms();
            typeof(DestroyManager).GetMethod("ApplyExplosionForce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(DestroyManager.Instance, new object[] { source, fragmentUnit.runtimeUnitId, UnitFaction.Enemy, Vector3.zero });
            scene.GetPhysicsScene().Simulate(0.02f);
            Check(checks, "Explosion speed change capped at 3 m/s", fragment.linearVelocity.magnitude > 0.01f && fragment.linearVelocity.magnitude <= 3.01f);
            Check(checks, "Explosion does not directly damage armor", Mathf.Approximately(armor.currentDurability, 100f));
            Check(checks, "Explosion marks outgoing contacts as protected", fragmentUnit.ExplosionCollisionProtectedUntil > Time.time);
            return Save(checks, null);
        }
        catch (Exception error) { Save(checks, error.ToString()); throw; }
        finally
        {
            PlayManager.instance.playMode = previousMode;
            foreach (GameObject root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
            SceneManager.UnloadSceneAsync(scene);
        }
    }

    private static float Collide(Scene scene, float mass, float speed, List<string> checks, bool checkContacts)
    {
        var host = new GameObject("Impact construct"); SceneManager.MoveGameObjectToScene(host, scene);
        Rigidbody body = host.AddComponent<Rigidbody>(); body.mass = mass; body.useGravity = false; body.linearDamping = 0; body.angularDamping = 0;
        body.constraints = RigidbodyConstraints.FreezeRotation; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ControlUnit unit = host.AddComponent<ControlUnit>(); unit.enabled = false; unit.faction = UnitFaction.Enemy;
        var parts = new List<Durability>();
        for (int i = 0; i < 2; i++)
        {
            var part = new GameObject("Contact armor " + i); part.transform.SetParent(host.transform, false); part.transform.localPosition = new Vector3(i == 0 ? -0.55f : 0.55f, 0, 0);
            part.AddComponent<BoxCollider>(); Durability health = part.AddComponent<Durability>(); health.debugLog = false; health.maxDurability = health.currentDurability = 1000f; parts.Add(health);
        }
        GameObject wall = new GameObject("Static wall"); SceneManager.MoveGameObjectToScene(wall, scene); wall.transform.position = new Vector3(0, 0, 2f); wall.AddComponent<BoxCollider>().size = new Vector3(10, 10, 1);
        body.linearVelocity = Vector3.forward * speed;
        Physics.SyncTransforms();
        for (int i = 0; i < 20; i++) scene.GetPhysicsScene().Simulate(0.02f);
        float damage = 2000f - parts[0].currentDurability - parts[1].currentDurability;
        if (checkContacts)
        {
            Check(checks, "Only load-bearing contact modules receive damage", parts[0].currentDurability < 1000f || parts[1].currentDurability < 1000f);
            Check(checks, "Real collision damage matches energy (" + damage + ")", Mathf.Abs(damage - 0.0375f) < 0.001f);
        }
        UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(wall);
        return damage;
    }

    private static Rigidbody Body(Scene scene, string name, float mass, Vector3 position)
    {
        var obj = new GameObject(name); SceneManager.MoveGameObjectToScene(obj, scene); obj.transform.position = position; obj.AddComponent<BoxCollider>();
        Rigidbody body = obj.AddComponent<Rigidbody>(); body.mass = mass; body.useGravity = false; body.linearDamping = body.angularDamping = 0f; return body;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed) throw new InvalidOperationException("FAIL: " + name);
        checks.Add("PASS: " + name);
    }

    private static string Save(List<string> checks, string error)
    {
        Directory.CreateDirectory("Temp/PhysicsUI");
        string result = string.Join("\n", checks) + (error == null ? "\nALL PASSED" : "\n" + error);
        File.WriteAllText("Temp/PhysicsUI/Validation.txt", result); return result;
    }
}
