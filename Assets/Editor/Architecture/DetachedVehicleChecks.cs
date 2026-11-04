// 职责：以真实玩家帧区分载具断片的即时销毁、延时清理和跨场景/保护状态丢失。
// 入口：Run()；要求已在Play Mode，报告Temp/Reuse/detached-vehicle.json，不操作Editor播放状态。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DetachedVehicleChecks
{
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static IEnumerator routine;
    private static int lastFrame;
    private static double deadline;
    public static string Run()
    {
        if (!Application.isPlaying || routine != null)
        {
            throw new InvalidOperationException("Requires Play Mode without another detached vehicle probe.");
        }

        Checks.Clear();
        Errors.Clear();
        routine = Scenario();
        lastFrame = -1;
        deadline = EditorApplication.timeSinceStartup + 45d;
        EditorApplication.update += Tick;
        return "Started: Temp/Reuse/detached-vehicle.json";
    }

    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (!Application.isPlaying || EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Detached vehicle probe interrupted or timed out.");
            }

            if (Time.frameCount == lastFrame)
            {
                return;
            }

            lastFrame = Time.frameCount;
            if (routine.MoveNext())
            {
                return;
            }
        }
        catch (Exception error)
        {
            Errors.Add(error.ToString());
        }

        EditorApplication.update -= Tick;
        try
        {
            (routine as IDisposable)?.Dispose();
        }
        catch (Exception error)
        {
            Errors.Add("Cleanup: " + error);
        }

        routine = null;
        Directory.CreateDirectory("Temp/Reuse");
        File.WriteAllText(
            "Temp/Reuse/detached-vehicle.json",
            JsonUtility.ToJson(
                new Report { passed = Errors.Count == 0, checks = Checks.ToArray(), errors = Errors.ToArray() },
                true));
    }

    private static IEnumerator Scenario()
    {
        PlayManager originalPlay = PlayManager.instance;
        GameManager originalGame = GameManager.instance;
        DestroyManager originalDestroy = DestroyManager.instance;
        VisualEffectsManager originalEffects = VisualEffectsManager.instance;
        MainUIPanels originalPanels = MainUIPanels.instance;
        CameraController camera = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        Transform originalCameraBody = camera != null ? camera.playerBody : null;
        bool cameraEnabled = camera != null && camera.enabled;
        var owned = new List<GameObject>();
        Scene local = default;
        try
        {
            if (camera != null)
            {
                camera.enabled = false;
            }

            GameObject inactive = new GameObject("Inactive detached vehicle fixture managers");
            owned.Add(inactive);
            inactive.SetActive(false);
            PlayManager play = inactive.AddComponent<PlayManager>();
            GameManager game = inactive.AddComponent<GameManager>();
            VisualEffectsManager effects = inactive.AddComponent<VisualEffectsManager>();
            effects.enableRuntimeVfx = false;
            GameObject destroyObject = new GameObject("Detached vehicle fixture destruction");
            owned.Add(destroyObject);
            DestroyManager destruction = destroyObject.AddComponent<DestroyManager>();
            PlayManager.instance = play;
            GameManager.instance = game;
            DestroyManager.instance = destruction;
            VisualEffectsManager.instance = effects;
            MainUIPanels.instance = null;
            play.playMode = true;
            play.maxUselessControlUnitGroups = 32;
            GameObject source = Object.Instantiate(
                Resources.Load<GameObject>("BlocksParent/BlocksParent"),
                new Vector3(19000, 2000, 0),
                Quaternion.identity);
            owned.Add(source);
            Rigidbody sourceBody = source.GetComponent<Rigidbody>();
            sourceBody.useGravity = false;
            ControlUnit unit = source.GetComponent<ControlUnit>();
            play.blocksParent = game.blocksParent = source.transform;
            var blocks = new List<Block>();
            for (int i = 0; i < 3; i++)
            {
                Block block = Object.Instantiate(
                    Resources.Load<GameObject>("Blocks/1x1x1"),
                    source.transform.position + Vector3.right * i,
                    Quaternion.identity,
                    source.transform).GetComponent<Block>();
                block.RefreshRuntimeData(true);
                block.connectorPrefab = null;
                block.canExplode = false;
                blocks.Add(block);
            }

            Physics.SyncTransforms();
            foreach (Block block in blocks)
            {
                block.CheckConnection();
            }

            unit.RefreshChildren();
            unit.AssignRuntimeOwnershipToBlocks(false);
            unit.ProtectExplosionContacts(20f);
            float protection = unit.ExplosionCollisionProtectedUntil;
            blocks[1].GetComponent<Durability>().ApplyDamage(100000f, null);
            ControlUnit detached = blocks[2].GetComponentInParent<ControlUnit>();
            Check(
                "Bridge destruction creates two live separate physical groups synchronously",
                detached != null && detached != unit && blocks[0] != null && blocks[2] != null);
            if (detached == null)
            {
                yield break;
            }

            owned.Add(detached.gameObject);
            foreach (ControlUnit group in play.allControlUnits)
            {
                group.GetComponent<Rigidbody>().useGravity = false;
            }

            Check(
                "New detached group inherits remaining source contact protection",
                detached.ExplosionCollisionProtectedUntil >= protection);
            yield return null;
            Check(
                "Only destroyed bridge disappears at end of player frame",
                blocks[1] == null && blocks[0] != null && blocks[2] != null);
            float waitUntil = Time.time + Mathf.Max(6.5f, (WreckSalvage.Settings != null ? WreckSalvage.Settings.cleanupDelay : 10f) + 1f);
            int startFrame = Time.frameCount;
            while (Time.time < waitUntil)
            {
                yield return null;
            }

            Check(
                "Near detached groups survive beyond salvage delay without cockpit death or budget pressure",
                Time.frameCount > startFrame + 3 && blocks[0] != null && blocks[2] != null && blocks[2].GetComponent<Durability>().currentDurability > 0f);
            local = SceneManager.CreateScene(
                "Detached physics fixture " + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            GameObject localRoot = Object.Instantiate(
                Resources.Load<GameObject>("BlocksParent/BlocksParent"),
                new Vector3(20000, 2000, 0),
                Quaternion.identity);
            owned.Add(localRoot);
            SceneManager.MoveGameObjectToScene(localRoot, local);
            var separate = new List<Block>();
            for (int i = 0; i < 2; i++)
            {
                Block block = Object.Instantiate(
                    Resources.Load<GameObject>("Blocks/1x1x1"),
                    localRoot.transform.position + Vector3.right * i * 3f,
                    Quaternion.identity,
                    localRoot.transform).GetComponent<Block>();
                block.RefreshRuntimeData(true);
                block.connectorPrefab = null;
                separate.Add(block);
            }

            Durability damaged = separate[1].GetComponent<Durability>();
            damaged.ApplyDamage(7f, unit);
            float damagedHealth = damaged.currentDurability;
            play.AssignBlocksToParentGroups(separate);
            ControlUnit localFragment = separate[1].GetComponentInParent<ControlUnit>();
            Check(
                "Detached group remains in source local physics scene",
                localFragment != null && localFragment.gameObject.scene == local);
            Check(
                "Scene-aware regroup preserves damage and attacker attribution",
                Mathf.Approximately(damaged.currentDurability, damagedHealth) && damaged.LastAttacker == unit);
            if (localFragment != null && localFragment.gameObject != localRoot)
            {
                owned.Add(localFragment.gameObject);
                Rigidbody fragmentBody = localFragment.GetComponent<Rigidbody>();
                fragmentBody.useGravity = false;
                fragmentBody.linearVelocity = Vector3.right;
                float previousX = fragmentBody.position.x;
                local.GetPhysicsScene().Simulate(0.04f);
                Check(
                    "Source local physics scene actually advances detached body",
                    fragmentBody.position.x > previousX + 0.01f);
            }

            yield return null;
        }
        finally
        {
            foreach (GameObject obj in owned)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            PlayManager.instance = originalPlay;
            GameManager.instance = originalGame;
            DestroyManager.instance = originalDestroy;
            VisualEffectsManager.instance = originalEffects;
            MainUIPanels.instance = originalPanels;
            if (camera != null)
            {
                camera.playerBody = originalCameraBody;
                camera.enabled = cameraEnabled;
            }

            if (local.IsValid() && local.isLoaded)
            {
                SceneManager.UnloadSceneAsync(local);
            }
        }
    }

    private static void Check(string label, bool result)
    {
        (result ? Checks : Errors).Add(label);
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string[] checks;
        public string[] errors;
    }
}
