// 职责：显式Player路线观察真实驻军追击移动目标、脱离索敌及重新接敌，不搬运或冻结敌机。
// 入口：Run()；玩家是2m/s移动的运动学测试目标，finally恢复其原位置，不改驻军参数。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal static class OutpostPursuitPlayerChecks
{
    internal static IEnumerator Run(ControlUnit player, IReadOnlyList<ControlUnit> defenders, Action<bool, string> check)
    {
        Vector3 original = player.transform.position;
        var observations = new List<Observation>();
        foreach (ControlUnit defender in defenders)
        {
            observations.Add(new Observation(defender));
        }

        check(observations.Count > 0, "Moving-target route starts with authored live defenders");
        Durability health = player.cockpit.GetComponent<Durability>();
        float originalHealth = health.currentDurability;
        float originalMaximum = health.maxDurability;
        float initialHealth = health.currentDurability;
        try
        {
            health.maxDurability = health.currentDurability = 100000000f;
            float until = Time.time + 20f;
            while (Time.time < until)
            {
                player.transform.position += Vector3.forward * (2f * Time.deltaTime);
                foreach (Observation observation in observations)
                {
                    observation.Sample(player, health, initialHealth);
                }

                yield return null;
            }

            foreach (Observation observation in observations)
            {
                Debug.Log(observation.Describe());
                check(
                    observation.Unit != null && observation.Unit.HasValidCockpit,
                    "Moving-target combat preserves operational guard: " + observation.Name);
                check(
                    observation.Targeted && observation.MaximumDisplacement > 10f,
                    "Authored guard physically pursues moving target: " + observation.Name);
                check(
                    observation.DamagedPlayer,
                    "Authored guard hits moving player through natural weapon simulation: " + observation.Name);
            }

            ControlUnit first = observations[0].Unit;
            player.transform.position = first.transform.position + Vector3.right * 120f;
            until = Time.time + 2f;
            while (Time.time < until)
            {
                yield return null;
            }

            check(first != null && first.HasValidCockpit, "Disengaged guard remains alive within streaming range");
            check(first.target == null, "Outpost guard releases target outside authored detection radius");
            player.transform.position = first.transform.position + Vector3.up * 15f;
            until = Time.time + 5f;
            while (first.target != player.transform && Time.time < until)
            {
                yield return null;
            }

            check(
                first.target == player.transform,
                "Returning player is naturally reacquired without spawning replacement guard");
        }
        finally
        {
            health.maxDurability = originalMaximum;
            health.currentDurability = Mathf.Min(originalHealth, originalMaximum);
            player.transform.position = original;
        }
    }

    private sealed class Observation
    {
        internal readonly ControlUnit Unit;
        internal readonly string Name;
        private readonly Vector3 origin;
        internal bool Targeted;
        internal bool DamagedPlayer;
        internal float MaximumDisplacement;
        internal Observation(ControlUnit unit)
        {
            Unit = unit;
            Name = unit.cockpit.GetComponent<EnemyIdentity>().DisplayName;
            origin = unit.transform.position;
        }

        internal void Sample(ControlUnit player, Durability health, float initialHealth)
        {
            if (Unit == null || !Unit.HasValidCockpit)
            {
                return;
            }

            MaximumDisplacement = Mathf.Max(MaximumDisplacement, Vector3.Distance(origin, Unit.transform.position));
            Targeted |= Unit.target == player.transform;
            DamagedPlayer |= health.currentDurability < initialHealth && health.LastAttacker == Unit;
        }

        internal string Describe()
        {
            return $"Outpost moving-target [{Name}]: displacement={MaximumDisplacement:F2}, targeted={Targeted}, damage={DamagedPlayer}";
        }
    }
}
