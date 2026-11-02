// 职责：在真实Play环境验证共用蓝图恢复、阵营与驾驶权隔离、AI接管及旧敌机入口。
// 入口：Run()；创建隔离舰艇，结束后停用并销毁，不保存玩家文件。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeFleetChecks
{
    public static string Run()
    {
        if (!EditorApplication.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Fleet checks require Play Mode with PlayManager.");
        }

        var checks = new List<string>();
        var units = new List<ControlUnit>();
        try
        {
            TextAsset source = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch");
            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(source.text);
            ControlUnit player = ModularUnitFactory.Spawn(
                blueprint,
                "Fleet validation player",
                new Vector3(1200f, 200f, 1200f),
                Quaternion.Euler(0f, 90f, 0f),
                UnitFaction.Player);
            units.Add(player);
            Check(
                checks,
                "shared factory restores exact module count",
                player != null && player.GetComponentsInChildren<Block>().Length == blueprint.blocks.Count);
            Check(
                checks,
                "player faction is restored on cockpit and owner",
                player.faction == UnitFaction.Player && player.cockpit.faction == UnitFaction.Player);
            Check(checks, "legacy player cockpit defaults to player control", player.AcceptsPlayerInput);
            player.cockpit.SetControlMode(UnitControlMode.Autonomous);
            player.RefreshChildren();
            Check(
                checks,
                "autonomous ally stays player faction without reading player input",
                !player.AcceptsPlayerInput && player.IsPlayer && player.GetComponent<EnemyController>() != null);
            foreach (Thruster thruster in player.GetComponentsInChildren<Thruster>())
            {
                Check(
                    checks,
                    "autonomous thruster rejects manual control: " + thruster.name,
                    !thruster.IsPlayerControllable);
            }

            player.cockpit.SetControlMode(UnitControlMode.Player);
            Check(checks, "taking control immediately restores manual source", player.AcceptsPlayerInput);
            float mass = 0f;
            foreach (Block block in player.GetComponentsInChildren<Block>())
            {
                mass += block.mass;
            }

            Check(
                checks,
                "rigidbody mass equals restored module mass",
                Mathf.Approximately(player.GetComponent<Rigidbody>().mass, mass));
            Check(
                checks,
                "spawn initializes rigidbody at requested world pose",
                Vector3.Distance(player.GetComponent<Rigidbody>().position, new Vector3(1200f, 200f, 1200f)) < 0.001f);
            ControlUnit enemy = EnemyBlueprintFactory.Spawn(blueprint, "Legacy validation enemy", new Vector3(1250f, 200f, 1200f), Quaternion.identity);
            units.Add(enemy);
            Check(
                checks,
                "legacy enemy factory preserves autonomous enemy behavior",
                enemy != null && enemy.faction == UnitFaction.Enemy && !enemy.AcceptsPlayerInput && enemy.GetComponent<EnemyController>() != null);
            EnemyIdentity identity = enemy.cockpit.GetComponent<EnemyIdentity>();
            Check(
                checks,
                "legacy enemy identity and spawn health survive delegation",
                identity != null && identity.DisplayName == "Legacy validation enemy" && identity.SpawnMaxHealth > 0f);
            Cockpit driven = player.cockpit;
            driven.SetControlMode(UnitControlMode.Autonomous);
            player.RefreshChildren();
            Check(
                checks,
                "refresh does not reset cockpit control identity",
                player.cockpit == driven && !player.AcceptsPlayerInput);
        }
        finally
        {
            foreach (ControlUnit unit in units)
            {
                if (unit != null)
                {
                    unit.gameObject.SetActive(false);
                    Object.Destroy(unit.gameObject);
                }
            }
        }

        string report = JsonUtility.ToJson(new Report { checks = checks }, true);
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/fleet.json", report);
        return report;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Siege fleet check failed: " + name);
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}
