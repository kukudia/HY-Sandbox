// 职责：显式Player路线读取既有敌机蓝图，验证自然AI飞行、供电和对玩家伤害。
// 入口：Run()；不改蓝图参数、不保存文件，结束清理本次敌机。
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class EnemyFlightPlayerChecks
{
    internal static IEnumerator Run(ControlUnit player, EnemySpawner spawner, string blueprintName, Action<bool, string> check)
    {
        // The smoke construct was added this frame; allow Start registration and delayed child refresh.
        float readyUntil = Time.time + 3f;
        while (!player.HasValidCockpit && Time.time < readyUntil)
        {
            yield return null;
        }

        check(player.HasValidCockpit, "smoke target completes real ControlUnit startup before enemy flight");
        SaveManager saves = SaveManager.instance;
        check(
            saves.enemyBlueprints.Contains(blueprintName),
            "requested natural-flight enemy exists in current blueprint library");
        BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(saves.GetEnemyBlueprintPath(blueprintName)));
        Vector3 position = player.transform.position + new Vector3(0f, 6f, 35f);
        ControlUnit enemy = spawner.SpawnBlockData(blueprint, blueprintName, position, Quaternion.identity);
        check(enemy != null, "existing blueprint restores through production spawner for natural flight");
        try
        {
            Rigidbody body = enemy.GetComponent<Rigidbody>();
            EnemyController ai = enemy.GetComponent<EnemyController>();
            check(
                !body.isKinematic && ai != null && ai.enabled && enemy.hoverThrusters.Length > 0,
                "enemy uses dynamic Rigidbody enabled AI and authored hover modules");
            Durability playerHealth = player.cockpit.GetComponent<Durability>();
            float initialHealth = playerHealth.currentDurability;
            bool sawTarget = false;
            bool sawIntent = false;
            bool sawHover = false;
            bool sawPower = false;
            bool sawHostileDamage = false;
            float maximumDistance = 0f;
            float until = Time.time + 30f;
            while (enemy != null && enemy.HasValidCockpit && Time.time < until)
            {
                sawTarget |= enemy.target == player.transform;
                sawIntent |= enemy.MovementInput.sqrMagnitude > .01f;
                sawHover |= enemy.hoverThrusters.Any(thruster => thruster != null && thruster.thrust > 1f);
                sawPower |= enemy.GetComponentsInChildren<Power>().Any(power => power.isWorking);
                sawHostileDamage |= playerHealth.currentDurability < initialHealth && playerHealth.LastAttacker != null && playerHealth.LastAttacker.faction == UnitFaction.Enemy;
                maximumDistance = Mathf.Max(maximumDistance, Vector3.Distance(position, enemy.transform.position));
                check(IsFinite(enemy.transform.position), "natural enemy flight remains finite");
                yield return null;
            }

            Debug.Log($"Natural enemy flight [{blueprintName}]: distance={maximumDistance:F2}, target={sawTarget}, intent={sawIntent}, power={sawPower}, hover={sawHover}, hostileDamage={sawHostileDamage}");
            check(sawTarget && sawIntent, "natural EnemyController acquires player and produces movement intent");
            check(sawPower && sawHover, "authored enemy power network supplies real hover output");
            check(maximumDistance > 2f, "enemy dynamic body moves in actual physics frames");
            check(
                enemy != null && enemy.HasValidCockpit,
                "existing enemy retains operational cockpit throughout 30 seconds");
            check(sawHostileDamage, "natural enemy weapons damage player with hostile attacker attribution");
        }
        finally
        {
            if (enemy != null)
            {
                Object.Destroy(enemy.gameObject);
            }
        }
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
    }
}
