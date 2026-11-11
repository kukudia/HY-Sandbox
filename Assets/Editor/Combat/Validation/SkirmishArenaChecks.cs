// 职责：验证自动3v3场景持久配置、正式死亡归因、五秒倒计时与连续轮次清理。
// 入口：ValidateSaved()在Edit Mode执行；Begin()在Play启动检查，Pending保存实际结果。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class SkirmishArenaChecks
{
    public static Task<string> Pending
    {
        get; private set;
    }

    public static string ValidateSaved()
    {
        var checks = new List<string>();
        SkirmishMatch match = UnityEngine.Object.FindAnyObjectByType<SkirmishMatch>();
        var settings = new SerializedObject(match);
        Check(checks, "Six real editable spawn slots saved", settings.FindProperty("slots").arraySize == 6);
        Check(checks, "Default enemy blueprint pool saved", settings.FindProperty("blueprints").arraySize >= 3);
        Check(checks, "Five second restart delay saved", settings.FindProperty("restartDelay").floatValue == 5f);
        foreach (string field in new[]
        {
            "hud",
            "play",
            "destruction",
            "projectiles",
            "effects"
        }

        )
        {
            Check(checks, "Explicit dependency " + field, settings.FindProperty(field).objectReferenceValue != null);
        }

        Check(checks, "Spectator death ownership saved", PlayManagerReference(match).Skirmish == match);
        Check(
            checks,
            "Avoidance trial coordinator removed from copy",
            UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>() == null);
        Check(
            checks,
            "Map includes four moving obstacles",
            UnityEngine.Object.FindObjectsByType<EnemyLabMovingObstacle>().Length == 4);
        Check(
            checks,
            "Camera is perspective and spectator mode",
            !Camera.main.orthographic && new SerializedObject(Camera.main.GetComponent<EnemyLabCamera>()).FindProperty("lab").objectReferenceValue == null);
        Check(
            checks,
            "No saved pre-fight ships",
            UnityEngine.Object.FindObjectsByType<ControlUnit>().Length == 0);
        CombatHud hud = UnityEngine.Object.FindAnyObjectByType<CombatHud>();
        RectTransform name = (RectTransform)hud.transform.Find("EnemyNameplateTemplate/Name");
        RectTransform value = (RectTransform)hud.transform.Find("EnemyNameplateTemplate/Value");
        Check(
            checks,
            "Nameplate labels have positive width and do not overlap",
            name.rect.width > 0f && value.rect.width > 0f && name.offsetMax.x < value.offsetMin.x);
        Check(
            checks,
            "Kill notice accommodates both blueprint names",
            ((RectTransform)hud.transform.Find("KillNotice")).rect.width >= 900f);
        return Save("edit-checks.txt", checks);
    }

    public static void Begin()
    {
        Pending = Run();
    }

    private static async Task<string> Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires arena Play Mode.");
        }

        var checks = new List<string>();
        SkirmishMatch match = UnityEngine.Object.FindAnyObjectByType<SkirmishMatch>();
        float previousScale = Time.timeScale;
        try
        {
            Time.timeScale = 0f;
            Check(
                checks,
                "First round starts automatically",
                match.Round >= 1 && match.BlueAlive == 3 && match.RedAlive == 3);
            FreezeCombat(match);
            for (int i = 0; i < 6; i++)
            {
                Cockpit cockpit = match.CockpitAt(i);
                ControlUnit unit = cockpit.GetComponentInParent<ControlUnit>();
                Check(
                    checks,
                    "Slot " + i + " has an autonomous correct-team ship",
                    cockpit.ControlMode == UnitControlMode.Autonomous && unit.faction == (i < 3 ? UnitFaction.Player : UnitFaction.Enemy));
            }

            var oldUnits = Enumerable.Range(0, 6).Select(i => match.CockpitAt(i).GetComponentInParent<ControlUnit>()).ToArray();
            int firstRound = match.Round;
            var debris = new GameObject("Validation detached round debris");
            LootDrop drop = LootDrop.Spawn(new CargoItem { kind = CargoKind.Coins, amount = 1 }, new Vector3(0f, 110f, 0f));
            TurretProjectile template = Resources.Load<GameObject>("Blocks/ProjectileTurretSingle").GetComponent<ProjectileTurret>().ProjectilePrefab;
            TurretProjectile shot = template != null ? TurretProjectilePool.Rent(template, new Vector3(0f, 110f, 0f), Quaternion.identity) : null;
            int messages = match.Hud.KillMessages;
            Cockpit victim = match.CockpitAt(3);
            ControlUnit attacker = oldUnits[0];
            victim.GetComponent<Durability>().ApplyDamage(float.MaxValue, attacker);
            Check(
                checks,
                "Real cockpit damage publishes kill feed",
                match.Hud.KillMessages == messages + 1 && match.RedAlive == 2);
            match.NotifyCockpitDestroyed(victim, attacker);
            Check(
                checks,
                "Duplicate cockpit notifications do not double-count",
                match.Hud.KillMessages == messages + 1);
            match.CockpitAt(4).GetComponent<Durability>().ApplyDamage(float.MaxValue, attacker);
            match.CockpitAt(5).GetComponent<Durability>().ApplyDamage(float.MaxValue, attacker);
            await Task.Delay(150);
            Check(
                checks,
                "Team elimination enters settlement",
                match.Settling && match.RedAlive == 0 && match.BlueAlive == 3);
            Check(
                checks,
                "Player death UI did not take over",
                PlayManagerReference(match).playMode && match.isActiveAndEnabled);
            Check(
                checks,
                "Kill presentation includes attacker and victim",
                UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include).Any(text => text.name == "Kill Feed" && text.text.Contains("BLUE-") && text.text.Contains("RED-")));
            await Task.Delay(250);
            Check(checks, "Pause freezes the restart timer", match.Round == firstRound && debris != null);
            float settled = Time.time;
            Time.timeScale = 1f;
            await Until(() => Time.time - settled >= 4.1f, 15f);
            Check(
                checks,
                "Battlefield persists before the five-second boundary",
                match.Round == firstRound && debris != null);
            await Until(() => match.Round == firstRound + 1, 15f);
            Check(
                checks,
                "Next round begins after five simulation seconds",
                Time.time - settled >= 4.9f && match.BlueAlive == 3 && match.RedAlive == 3);
            Time.timeScale = 0f;
            FreezeCombat(match);
            Check(
                checks,
                "Old ships and detached debris are destroyed",
                oldUnits.All(unit => unit == null) && debris == null);
            Check(checks, "Old loot and pooled projectiles are cleared", drop == null && shot == null);
            Check(checks, "Kill feed resets for the new round", match.Hud.KillMessages == 0);
            Check(
                checks,
                "Exactly six fresh cockpits registered",
                PlayManagerReference(match).GetControlUnits().Count(unit => unit != null && unit.HasValidCockpit) == 6);
            for (int i = 0; i < 3; i++)
            {
                match.CockpitAt(i).GetComponent<Durability>().ApplyDamage(float.MaxValue, match.CockpitAt(3).GetComponentInParent<ControlUnit>());
            }

            await Task.Delay(150);
            Check(
                checks,
                "Blue team defeat also settles without player death screen",
                match.Settling && match.BlueAlive == 0 && PlayManagerReference(match).playMode);
            Time.timeScale = 1f;
            await Until(() => match.Round == firstRound + 2, 15f);
            Check(checks, "Third round refills both teams", match.BlueAlive == 3 && match.RedAlive == 3);
            return Save("play-checks.txt", checks);
        }
        finally
        {
            Time.timeScale = previousScale;
        }
    }

    private static PlayManager PlayManagerReference(SkirmishMatch match)
    {
        return (PlayManager)new SerializedObject(match).FindProperty("play").objectReferenceValue;
    }

    private static void FreezeCombat(SkirmishMatch match)
    {
        foreach (ControlUnit unit in PlayManagerReference(match).GetControlUnits())
        {
            if (unit != null)
            {
                unit.GetComponent<EnemyController>().enabled = false;
                unit.SetMovementInput(Vector3.zero);
                foreach (Turret turret in unit.GetComponentsInChildren<Turret>())
                {
                    turret.SetStarted(false);
                }
            }
        }
    }

    private static async Task Until(Func<bool> condition, float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                throw new TimeoutException("Arena condition timed out.");
            }

            await Task.Delay(50);
        }
    }

    private static void Check(List<string> checks, string label, bool condition)
    {
        checks.Add((condition ? "PASS " : "FAIL ") + label);
        if (!condition)
        {
            throw new InvalidOperationException(string.Join("\n", checks));
        }
    }

    private static string Save(string name, List<string> checks)
    {
        Directory.CreateDirectory("Temp/Random3v3Arena");
        string result = string.Join("\n", checks);
        File.WriteAllText(Path.Combine("Temp/Random3v3Arena", name), result);
        return result;
    }
}
