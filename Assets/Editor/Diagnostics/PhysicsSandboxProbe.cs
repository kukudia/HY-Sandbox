// 职责：在 Play Mode 检查沙盒物理、控制和损伤相关场景。
// 入口：Run()。
// 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。
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
        if (!Application.isPlaying || MainUIPanels.instance == null)
        {
            throw new InvalidOperationException("Run in Main Play Mode.");
        }

        var checks = new List<string>();
        bool previousMode = PlayManager.instance.playMode;
        Scene scene = SceneManager.CreateScene("PhysicsSandboxProbe", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            PlayManager.instance.playMode = true;
            Check(
                checks,
                "Reduced mass: equal dynamic pair",
                Mathf.Approximately(ImpactPhysics.ReducedMass(10f, 10f, false), 5f));
            Check(
                checks,
                "Static target uses full moving mass",
                Mathf.Approximately(ImpactPhysics.ReducedMass(10f, 0f, true), 10f));
            Check(
                checks,
                "Safe-speed threshold rejects gentle contact",
                ImpactPhysics.DamageEnergy(10f, 3f, 5f) == 0f);
            Check(checks, "Gentle real contact causes no damage", Collide(scene, 10f, 1f, checks, false) == 0f);
            float basic = Collide(scene, 10f, 10f, checks, true);
            float glancing = Collide(
                scene,
                10f,
                10f,
                checks,
                false,
                0f,
                10f);
            Check(checks, "Tangential speed adds no crash damage", Mathf.Abs(glancing - basic) < 0.05f);
            float elastic = Collide(
                scene,
                10f,
                10f,
                checks,
                false,
                1f);
            Check(checks, "Elastic real rebound causes no permanent damage", elastic < 0.001f);
            float partial = Collide(
                scene,
                10f,
                10f,
                checks,
                false,
                0.5f);
            Check(
                checks,
                "Half restitution dissipates three quarters energy",
                Mathf.Abs(partial / basic - 0.75f) < 0.03f);
            Check(
                checks,
                "Lighter body receives larger share",
                Mathf.Abs(ImpactPhysics.BodyEnergyShare(10f, 30f, false) - 0.75f) < 0.001f);
            Check(
                checks,
                "Dynamic pair shares conserve energy",
                Mathf.Abs(ImpactPhysics.BodyEnergyShare(10f, 30f, false) + ImpactPhysics.BodyEnergyShare(30f, 10f, false) - 1f) < 0.001f);
            float heavy = Collide(scene, 20f, 10f, checks, false);
            float fast = Collide(scene, 10f, 20f, checks, false);
            Check(checks, "Double mass doubles damage", Mathf.Abs(heavy / basic - 2f) < 0.03f);
            Check(checks, "Squared speed and threshold energy", Mathf.Abs(fast / basic - 4.125f) < 0.04f);
            CollideDynamicPair(scene, checks);
            Rigidbody light = Body(scene, "Light impulse", 10f, Vector3.zero);
            Rigidbody heavyBody = Body(scene, "Heavy impulse", 20f, Vector3.right * 5f);
            light.AddForceAtPosition(Vector3.forward * 10f, light.position + Vector3.right * 0.4f, ForceMode.Impulse);
            heavyBody.AddForce(Vector3.forward * 10f, ForceMode.Impulse);
            scene.GetPhysicsScene().Simulate(0.02f);
            Check(
                checks,
                "Impulse respects inverse mass",
                Mathf.Abs(light.linearVelocity.z / heavyBody.linearVelocity.z - 2f) < 0.03f);
            Check(checks, "Off-centre impulse produces rotation", light.angularVelocity.magnitude > 0.1f);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            var meshObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(meshObject, scene);
            UnityEngine.Object.DestroyImmediate(meshObject.GetComponent<BoxCollider>());
            MeshCollider collider = meshObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshObject.GetComponent<MeshFilter>().sharedMesh;
            collider.convex = false;
            Vector3 nearest = ImpactPhysics.ClosestPoint(collider, Vector3.right * 5f);
            Check(checks, "Non-convex collider has finite closest point", !float.IsNaN(nearest.x));
            var hud = MainUIPanels.instance.playPanel.GetComponent<PhysicsTelemetryHud>();
            Check(
                checks,
                "Physics HUD is persisted under PlayPanel",
                hud != null && MainUIPanels.instance.playPanel.transform.Find("PhysicsTelemetry/Motion") != null);
            var safetyObject = new GameObject("Collision safety");
            SceneManager.MoveGameObjectToScene(safetyObject, scene);
            Durability safety = safetyObject.AddComponent<Durability>();
            safety.ApplyCollisionEnergy(10000000f, 1000f, 1f);
            Check(
                checks,
                "Extreme impact can destroy in one hit",
                safety.currentDurability == 0f && safety.DestroyedByCollision);
            safety.enabled = false;
            safety.enabled = true;
            safety.ApplyCollisionEnergy(1f, 10f, 1f);
            safety.ApplyCollisionEnergy(1f, 10f, 1f);
            Check(
                checks,
                "Separate impacts are not discarded by cooldown",
                Mathf.Abs(safety.currentDurability - 90.4f) < 0.001f);
            safety.enabled = false;
            safety.enabled = true;
            safety.ProtectFromExplosionCollisions(3f);
            safety.ApplyCollisionEnergy(10000000f, 1000f, 1f);
            Check(
                checks,
                "Blast protection prevents collision damage",
                Mathf.Approximately(safety.currentDurability, 100f));
            var blast = new GameObject("Probe explosion");
            SceneManager.MoveGameObjectToScene(blast, scene);
            Block source = blast.AddComponent<Block>();
            source.explosionRadius = 8f;
            source.explosionForce = 10000f;
            Rigidbody fragment = Body(scene, "Blast fragment", 1f, Vector3.right * 2f);
            ControlUnit fragmentUnit = fragment.gameObject.AddComponent<ControlUnit>();
            fragmentUnit.enabled = false;
            var armorObject = new GameObject("Fragment armor");
            armorObject.transform.SetParent(fragment.transform, false);
            Durability armor = armorObject.AddComponent<Durability>();
            armor.debugLog = false;
            // ApplyExplosionForce queries the default scene; use the saved ownership path for this isolated scene.
            fragmentUnit.EnsureRuntimeUnitId();
            Block fragmentBlock = armorObject.AddComponent<Block>();
            RuntimeUnitMember.Ensure(armorObject, fragmentUnit.runtimeUnitId, UnitFaction.Enemy);
            Physics.SyncTransforms();
            Type propagation = typeof(DestroyManager).Assembly.GetType("ExplosionPropagation", true);
            propagation.GetMethod(
                "ApplyExplosionForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(
                    null,
                    new object[] { source, fragmentUnit.runtimeUnitId, UnitFaction.Enemy, Vector3.zero, 1.2f, 0.15f, 3f, 3f });
            scene.GetPhysicsScene().Simulate(0.02f);
            Check(
                checks,
                "Explosion speed change capped at 3 m/s",
                fragment.linearVelocity.magnitude > 0.01f && fragment.linearVelocity.magnitude <= 3.01f);
            Check(
                checks,
                "Explosion does not directly damage armor",
                Mathf.Approximately(armor.currentDurability, 100f));
            Check(
                checks,
                "Explosion marks outgoing contacts as protected",
                fragmentUnit.ExplosionCollisionProtectedUntil > Time.time);
            return Save(checks, null);
        }
        catch (Exception error)
        {
            Save(checks, error.ToString());
            throw;
        }
        finally
        {
            PlayManager.instance.playMode = previousMode;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            SceneManager.UnloadSceneAsync(scene);
        }
    }

    private static float Collide(
        Scene scene,
        float mass,
        float speed,
        List<string> checks,
        bool checkContacts,
        float restitution = 0f,
        float tangentSpeed = 0f)
    {
        var host = new GameObject("Impact construct");
        SceneManager.MoveGameObjectToScene(host, scene);
        Rigidbody body = host.AddComponent<Rigidbody>();
        body.mass = mass;
        body.useGravity = false;
        body.linearDamping = 0;
        body.angularDamping = 0;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ControlUnit unit = host.AddComponent<ControlUnit>();
        unit.enabled = false;
        unit.faction = UnitFaction.Enemy;
        var parts = new List<Durability>();
        for (int i = 0; i < 2; i++)
        {
            var part = new GameObject("Contact armor " + i);
            part.transform.SetParent(host.transform, false);
            part.transform.localPosition = new Vector3(i == 0 ? -0.55f : 0.55f, 0, 0);
            part.AddComponent<BoxCollider>();
            Durability health = part.AddComponent<Durability>();
            health.debugLog = false;
            health.maxDurability = health.currentDurability = 1000f;
            parts.Add(health);
        }

        GameObject wall = new GameObject("Static wall");
        SceneManager.MoveGameObjectToScene(wall, scene);
        wall.transform.position = new Vector3(0, 0, 2f);
        wall.AddComponent<BoxCollider>().size = new Vector3(10, 10, 1);
        var material = new PhysicsMaterial("Probe restitution")
        {
            bounciness = restitution,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            dynamicFriction = 0f,
            staticFriction = 0f
        };
        wall.GetComponent<Collider>().sharedMaterial = material;
        foreach (Durability part in parts)
        {
            part.GetComponent<Collider>().sharedMaterial = material;
        }

        body.linearVelocity = Vector3.forward * speed + Vector3.right * tangentSpeed;
        Physics.SyncTransforms();
        for (int i = 0; i < 20; i++)
        {
            scene.GetPhysicsScene().Simulate(0.02f);
        }

        float damage = 2000f - parts[0].currentDurability - parts[1].currentDurability;
        if (checkContacts)
        {
            Check(
                checks,
                "Only load-bearing contact modules receive damage",
                parts[0].currentDurability < 1000f || parts[1].currentDurability < 1000f);
            Check(checks, "Real collision damage matches energy (" + damage + ")", Mathf.Abs(damage - 48f) < 0.05f);
        }

        UnityEngine.Object.DestroyImmediate(host);
        UnityEngine.Object.DestroyImmediate(wall);
        UnityEngine.Object.DestroyImmediate(material);
        return damage;
    }

    private static void CollideDynamicPair(Scene scene, List<string> checks)
    {
        Rigidbody first = Body(scene, "Light collision body", 10f, Vector3.zero);
        Rigidbody second = Body(scene, "Heavy collision body", 30f, Vector3.forward * 2f);
        foreach (Rigidbody body in new[]
        {
            first,
            second
        }

        )
        {
            body.constraints = RigidbodyConstraints.FreezeRotation;
            ControlUnit unit = body.gameObject.AddComponent<ControlUnit>();
            unit.enabled = false;
            unit.faction = UnitFaction.Enemy;
            Durability health = body.gameObject.AddComponent<Durability>();
            health.debugLog = false;
            health.maxDurability = health.currentDurability = 1000f;
        }

        first.linearVelocity = Vector3.forward * 10f;
        Physics.SyncTransforms();
        for (int i = 0; i < 20; i++)
        {
            scene.GetPhysicsScene().Simulate(0.02f);
        }

        float lightDamage = 1000f - first.GetComponent<Durability>().currentDurability;
        float heavyDamage = 1000f - second.GetComponent<Durability>().currentDurability;
        Check(
            checks,
            "Real unequal pair gives light body three times damage",
            Mathf.Abs(lightDamage / heavyDamage - 3f) < 0.03f);
        Check(
            checks,
            "Real unequal pair conserves normal damage budget",
            Mathf.Abs(lightDamage + heavyDamage - 36f) < 0.05f);
        UnityEngine.Object.DestroyImmediate(first.gameObject);
        UnityEngine.Object.DestroyImmediate(second.gameObject);
    }

    private static Rigidbody Body(Scene scene, string name, float mass, Vector3 position)
    {
        var obj = new GameObject(name);
        SceneManager.MoveGameObjectToScene(obj, scene);
        obj.transform.position = position;
        obj.AddComponent<BoxCollider>();
        Rigidbody body = obj.AddComponent<Rigidbody>();
        body.mass = mass;
        body.useGravity = false;
        body.linearDamping = body.angularDamping = 0f;
        return body;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("FAIL: " + name);
        }

        checks.Add("PASS: " + name);
    }

    private static string Save(List<string> checks, string error)
    {
        Directory.CreateDirectory("Temp/PhysicsUI");
        string result = string.Join("\n", checks) + (error == null ? "\nALL PASSED" : "\n" + error);
        File.WriteAllText("Temp/PhysicsUI/Validation.txt", result);
        return result;
    }
}
