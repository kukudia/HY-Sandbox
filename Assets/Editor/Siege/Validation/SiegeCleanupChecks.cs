// 职责：验证真实扫描组件的停留期、幂等掉落、阵营/场景保护与暂停模拟下回收。
// 入口：RunLive()；隔离临时场景与真实宿主，恢复回收参数/随机数，绝不保存玩家数据。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SiegeCleanupChecks
{
    private static GameObject timingRoot;
    private static GameObject timingView;
    private static SiegeCleanupSweep timingSweep;
    private static Coroutine timingCoroutine;
    private static double timingStarted;
    private static bool timerCompleted;
    private static bool timingReleased;
    private static bool timingBackground;
    public static void RunTiming()
    {
        SiegeCleanupSweep source = Object.FindAnyObjectByType<SiegeCleanupSweep>();
        if (!EditorApplication.isPlaying || source == null || timingRoot != null)
        {
            throw new InvalidOperationException("Fresh Siege Play required for timing check.");
        }

        timingBackground = Application.runInBackground;
        Application.runInBackground = true;
        timingRoot = new GameObject("Owned cleanup timer fixture");
        SceneManager.MoveGameObjectToScene(timingRoot, source.gameObject.scene);
        ControlUnit enemy = Create(timingRoot.transform, "timer owned enemy", UnitFaction.Enemy, 1);
        timingView = Object.Instantiate(source.gameObject);
        SceneManager.MoveGameObjectToScene(timingView, source.gameObject.scene);
        timingSweep = timingView.GetComponent<SiegeCleanupSweep>();
        timingSweep.Begin(100, new Bounds(Vector3.zero, Vector3.one * 100), Time.time);
        Type cleanupType = typeof(DestroyManager).Assembly.GetType("SalvageCleanupSession");
        IEnumerator wait = (IEnumerator)cleanupType.GetMethod("WaitForFragment", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { enemy });
        timerCompleted = timingReleased = false;
        timingCoroutine = DestroyManager.instance.StartCoroutine(WaitAndMark(wait));
        timingStarted = EditorApplication.timeSinceStartup;
        EditorApplication.update += TimingTick;
    }

    private static IEnumerator WaitAndMark(IEnumerator productionWait)
    {
        yield return productionWait;
        timerCompleted = true;
    }

    private static void TimingTick()
    {
        string error = null;
        double elapsed = EditorApplication.timeSinceStartup - timingStarted;
        if (!EditorApplication.isPlaying)
        {
            error = "Play interrupted timing check";
        }
        else if (elapsed < 1.2)
        {
            return;
        }
        else if (!timingReleased)
        {
            if (timerCompleted)
            {
                error = "Production delayed cleanup completed while sweep owned enemy";
            }
            else
            {
                timingSweep.Stop();
                timingReleased = true;
                return;
            }
        }
        else if (elapsed < 2.2)
        {
            return;
        }
        else if (!timerCompleted)
        {
            error = "Production delayed cleanup failed to resume after sweep cancellation";
        }

        EditorApplication.update -= TimingTick;
        if (timingCoroutine != null && DestroyManager.instance != null)
        {
            DestroyManager.instance.StopCoroutine(timingCoroutine);
        }

        Object.Destroy(timingRoot);
        Object.Destroy(timingView);
        timingRoot = timingView = null;
        Application.runInBackground = timingBackground;
        Directory.CreateDirectory("Temp/SiegeCleanup");
        File.WriteAllText(
            "Temp/SiegeCleanup/timing.json",
            JsonUtility.ToJson(
                new Report { passed = error == null, error = error, checks = new List<string> { "production coroutine remains pending during scan ownership", "production coroutine resumes after cancellation" } },
                true));
    }

    public static string RunLive()
    {
        SiegeSession session = Object.FindAnyObjectByType<SiegeSession>();
        SiegeGameplayStage stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
        SiegeCleanupSweep source = session != null ? session.Cleanup : stage != null ? stage.Cleanup : null;
        if (!EditorApplication.isPlaying || source == null)
        {
            throw new InvalidOperationException("Open a fresh Siege host in Play Mode.");
        }

        var report = new Report();
        Scene fixture = SceneManager.CreateScene("Owned interwave cleanup fixture");
        Scene foreign = SceneManager.CreateScene("Owned foreign salvage fixture");
        GameObject root = new GameObject("Owned interwave cleanup");
        SceneManager.MoveGameObjectToScene(root, fixture);
        GameObject view = Object.Instantiate(source.gameObject);
        SceneManager.MoveGameObjectToScene(view, fixture);
        SiegeCleanupSweep sweep = view.GetComponent<SiegeCleanupSweep>();
        SalvageSettings settings = WreckSalvage.Settings;
        float chance = settings.coinDropChance;
        float specialChance = settings.specialPartChance;
        float minimum = settings.minimumFragmentLifetime;
        int packetLimit = settings.maxCurrencyPackets;
        bool playing = PlayManager.instance.playMode;
        UnityEngine.Random.State random = UnityEngine.Random.state;
        SiegeMatch match = session != null ? session.Match : stage.Match;
        long coins = match.Coins;
        int kills = match.DefeatedAttackers;
        SiegePhase phase = match.Phase;
        var existingDrops = new HashSet<LootDrop>(Object.FindObjectsByType<LootDrop>());
        var effects = new HashSet<MaterialBreakEffect>(Object.FindObjectsByType<MaterialBreakEffect>());
        try
        {
            settings.coinDropChance = 1f;
            settings.specialPartChance = 0f;
            settings.minimumFragmentLifetime = 2f;
            settings.maxCurrencyPackets = 1;
            PlayManager.instance.playMode = false;
            ControlUnit friend = Create(root.transform, "friendly wreck", UnitFaction.Player, 1);
            ControlUnit enemy = Create(root.transform, "enemy two modules", UnitFaction.Enemy, 2);
            ControlUnit live = Create(root.transform, "live enemy", UnitFaction.Enemy, 1);
            live.GetComponentInChildren<Block>().gameObject.AddComponent<Cockpit>();
            ControlUnit mixed = Create(root.transform, "reclaimed fragment", UnitFaction.Enemy, 1);
            mixed.GetComponentInChildren<RuntimeUnitMember>().ownerFaction = UnitFaction.Player;
            ControlUnit outside = Create(root.transform, "foreign scene fragment", UnitFaction.Enemy, 1);
            outside.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(outside.gameObject, foreign);
            LootDrop foreignCoin = LootDrop.Spawn(new CargoItem { kind = CargoKind.Coins, amount = 7 }, Vector3.zero, foreign);
            Check(report, foreignCoin.gameObject.scene == foreign, "explicit drop belongs to foreign scene");
            sweep.Begin(1, new Bounds(Vector3.zero, new Vector3(100, 0, 100)), 100);
            sweep.Tick(100);
            Check(
                report,
                SiegeCleanupSweep.OwnsRetirement(enemy) && !SiegeCleanupSweep.OwnsRetirement(friend),
                "active sweep takes only eligible enemy retirement ownership");
            Check(
                report,
                sweep.IsActive && sweep.PendingGroups == 1 && enemy.gameObject.activeSelf,
                "scan counts only eligible enemy wreck and preserves newborn");
            sweep.Tick(101);
            Check(
                report,
                enemy.gameObject.activeSelf && sweep.RetiredGroups == 0,
                "minimum lifetime prevents early retirement");
            sweep.Tick(104);
            Check(
                report,
                !enemy.gameObject.activeSelf && sweep.RetiredGroups == 1 && sweep.RetiredBlocks == 2,
                "paused simulation retires eligible group after retention");
            LootDrop[] localDrops = Object.FindObjectsByType<LootDrop>().Where(drop => drop.gameObject.scene == fixture).ToArray();
            Check(
                report,
                localDrops.Length == 1 && localDrops[0].Item.amount == settings.coinsPerBlock * 2,
                "existing per-block salvage creates exactly one merged local packet");
            Check(report, foreignCoin.Item.amount == 7, "packet budget never merges across scenes");
            Check(
                report,
                friend.gameObject.activeSelf && live.gameObject.activeSelf && mixed.gameObject.activeSelf && outside.gameObject.activeSelf,
                "friendly live reclaimed and foreign units survive");
            var blocks = new List<Block>();
            Check(
                report,
                !EnemyWreckRetirement.Retire(enemy, fixture, blocks, true),
                "duplicate retire rejected after immediate deactivation");
            sweep.Begin(1, new Bounds(Vector3.one, Vector3.one), 200);
            sweep.Tick(200);
            Check(
                report,
                sweep.RetiredGroups == 1 && !sweep.IsActive && localDrops[0].Item.amount == settings.coinsPerBlock * 2,
                "same wave cannot restart or issue duplicate drops");
            Check(
                report,
                match.Coins == coins && match.DefeatedAttackers == kills && match.Phase == phase,
                "cleanup grants no match coins kills or phase changes");
            ControlUnit changed = Create(root.transform, "ownership changes after scan", UnitFaction.Enemy, 1);
            sweep.Begin(2, new Bounds(Vector3.zero, new Vector3(100, 0, 100)), 300);
            sweep.Tick(300);
            changed.GetComponentInChildren<RuntimeUnitMember>().ownerFaction = UnitFaction.Player;
            sweep.Tick(304);
            Check(
                report,
                changed.gameObject.activeSelf && sweep.RetiredGroups == 0,
                "ownership is rechecked at retirement time");
            sweep.Stop();
            Check(report, !SiegeCleanupSweep.OwnsRetirement(changed), "stop releases delayed retirement ownership");
            Check(
                report,
                !sweep.IsActive && !sweep.GetComponentsInChildren<LineRenderer>(true).Any(line => line.enabled),
                "stop cancels presentation without deleting protected wrecks");
            Check(
                report,
                session != null ? session.Fleet.Base.IsAlive : stage.Base.IsAlive,
                "real host base remains alive");
            report.passed = true;
        }
        catch (Exception error)
        {
            report.error = error.ToString();
        }
        finally
        {
            settings.coinDropChance = chance;
            settings.specialPartChance = specialChance;
            settings.minimumFragmentLifetime = minimum;
            settings.maxCurrencyPackets = packetLimit;
            PlayManager.instance.playMode = playing;
            UnityEngine.Random.state = random;
            foreach (LootDrop drop in Object.FindObjectsByType<LootDrop>())
            {
                if (!existingDrops.Contains(drop))
                {
                    Object.DestroyImmediate(drop.gameObject);
                }
            }

            foreach (MaterialBreakEffect effect in Object.FindObjectsByType<MaterialBreakEffect>())
            {
                if (!effects.Contains(effect))
                {
                    Object.DestroyImmediate(effect.gameObject);
                }
            }

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(view);
            SceneManager.UnloadSceneAsync(fixture);
            SceneManager.UnloadSceneAsync(foreign);
        }

        Directory.CreateDirectory("Temp/SiegeCleanup");
        string path = "Temp/SiegeCleanup/" + (session != null ? "prototype" : "gameplay") + "-checks.json";
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path;
    }

    private static ControlUnit Create(Transform root, string name, UnitFaction faction, int count)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(root, false);
        obj.transform.position = new Vector3(0, 5, 0);
        obj.AddComponent<Rigidbody>().isKinematic = true;
        ControlUnit unit = obj.AddComponent<ControlUnit>();
        unit.faction = faction;
        for (int i = 0; i < count; i++)
        {
            GameObject module = new GameObject("salvage module " + i);
            module.transform.SetParent(obj.transform, false);
            module.AddComponent<Block>();
            RuntimeUnitMember.Ensure(module, "cleanup-owned", faction);
        }

        return unit;
    }

    private static void Check(Report report, bool passed, string name)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add(name);
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public List<string> checks = new List<string>();
        public string error;
    }
}
