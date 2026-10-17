// 职责：显式Player烟测中验证真实据点部署、受损休眠恢复、驾驶舱击败与不重刷。
// 入口：Run()；只由-world-smoke-outposts启用，不写用户蓝图或永久进度。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class OutpostPlayerChecks
{
    public static IEnumerator Run(
        WorldSessionManager session,
        ControlUnit player,
        Action<bool, string> check,
        Action<string> capture = null,
        bool salvage = false,
        bool recoverAllParts = false,
        bool movingTarget = false)
    {
        return Flatten(Scenario(
            session,
            player,
            check,
            capture,
            salvage,
            recoverAllParts,
            movingTarget));
    }

    private static IEnumerator Flatten(IEnumerator root)
    {
        var work = new Stack<IEnumerator>();
        work.Push(root);
        try
        {
            while (work.Count > 0)
            {
                IEnumerator current = work.Peek();
                if (!current.MoveNext())
                {
                    (work.Pop() as IDisposable)?.Dispose();
                }
                else if (current.Current is IEnumerator nested)
                {
                    work.Push(nested);
                }
                else
                {
                    yield return current.Current;
                }
            }
        }
        finally
        {
            while (work.Count > 0)
            {
                (work.Pop() as IDisposable)?.Dispose();
            }
        }
    }

    private static IEnumerator Scenario(
        WorldSessionManager session,
        ControlUnit player,
        Action<bool, string> check,
        Action<string> capture,
        bool salvage,
        bool recoverAllParts,
        bool movingTarget)
    {
        check(
            Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None).All(item => !item.IsAutomaticSpawningAllowed),
            "World session suppresses global EnemySpawner scheduling");
        Vector3 previous = player.transform.position;
        BuildingRuntime site = session.Buildings.Buildings.FirstOrDefault(item => item != null && item.Outpost != null);
        for (int waypoint = 0; waypoint < 8 && site == null; waypoint++)
        {
            Vector3 offset = Quaternion.Euler(0f, waypoint * 45f, 0f) * Vector3.right * 240f;
            yield return TravelTo(player.transform, previous + offset + Vector3.up * 25f);
            for (int frame = 0; frame < 120 && site == null; frame++)
            {
                site = session.Buildings.Buildings.FirstOrDefault(item => item != null && item.Outpost != null);
                yield return null;
            }
        }

        check(site != null, "Generated world contains authored enemy outposts");
        Debug.Log("Outpost smoke selected " + site.BlueprintName + " at " + site.transform.position);
        string siteId = site.InstanceId;
        string name = site.BlueprintName;
        EnemyOutpostData data = site.Outpost;
        Vector3 origin = site.transform.position;
        yield return TravelTo(player.transform, origin + Vector3.up * 25f);
        int count = 0;
        for (int frame = 0; frame < 600; frame++)
        {
            count = data.garrison.Count(guard => session.Outposts.TryGetDefender(siteId, guard.id, out _) || session.Outposts.IsDefeated(siteId, guard.id));
            if (count == data.garrison.Count)
            {
                break;
            }

            yield return null;
        }

        check(count == data.garrison.Count, "Authored finite garrison deploys through production director");
        OutpostGarrisonData first = data.garrison.First(guard => !session.Outposts.IsDefeated(siteId, guard.id));
        check(
            session.Outposts.TryGetDefender(siteId, first.id, out ControlUnit defender),
            "Live defender retains stable site and slot identity");
        // Observe every authored type before the controlled lifecycle assertions below.
        var observations = new List<GuardObservation>();
        foreach (OutpostGarrisonData guard in data.garrison)
        {
            if (session.Outposts.TryGetDefender(siteId, guard.id, out ControlUnit unit))
            {
                observations.Add(new GuardObservation(unit, guard.name));
            }
        }

        Vector3 initial = defender.transform.position;
        for (int i = 0; i < 180; i++)
        {
            yield return null;
        }

        check(
            defender != null && defender.HasValidCockpit && defender.GetComponent<EnemyController>() != null,
            "Outpost defender survives natural AI physics interval");
        check(
            (defender.transform.position - initial).sqrMagnitude > 0.01f,
            "Outpost defender uses actual moving AI rather than a static decoration");
        Durability targetHealth = player.cockpit.GetComponent<Durability>();
        float initialTargetHealth = targetHealth.currentDurability;
        bool sawGarrisonDamage = false;
        float combatUntil = Time.time + 30f;
        while (Time.time < combatUntil && defender != null && defender.HasValidCockpit && !sawGarrisonDamage)
        {
            sawGarrisonDamage = targetHealth.currentDurability < initialTargetHealth && targetHealth.LastAttacker == defender;
            foreach (GuardObservation observation in observations)
            {
                observation.Sample(player, targetHealth, initialTargetHealth);
            }

            yield return null;
        }

        check(
            sawGarrisonDamage,
            "Natural outpost garrison weapons damage player with the correct attacker identity");
        float groupUntil = Time.time + 30f;
        while (Time.time < groupUntil && observations.Any(item => !item.Complete))
        {
            foreach (GuardObservation observation in observations)
            {
                observation.Sample(player, targetHealth, initialTargetHealth);
            }

            yield return null;
        }

        foreach (GuardObservation observation in observations)
        {
            Debug.Log(observation.Describe());
            check(
                observation.Unit != null && observation.Unit.HasValidCockpit,
                "Authored guard survives natural group combat: " + observation.Name);
            check(
                observation.Moved && observation.Targeted && observation.Powered,
                "Authored guard acquires target and flies with powered hover: " + observation.Name);
            check(observation.DamagedPlayer, "Authored guard weapons naturally damage player: " + observation.Name);
        }

        if (movingTarget)
        {
            yield return OutpostPursuitPlayerChecks.Run(player, observations.Select(item => item.Unit).ToArray(), check);
        }

        OutpostHud hud = Object.FindFirstObjectByType<OutpostHud>();
        UnityEngine.UI.Text hudLabel = hud.GetComponent<UnityEngine.UI.Text>();
        check(hudLabel.text.Contains("m\nDefenders"), "Live outpost HUD separates distance and defender counts");
        string currentLabel = hudLabel.text;
        hudLabel.text = hud.Format(new OutpostEncounterStatus(
            new string('W', 120),
            225f,
            8,
            8,
            2048,
            false), 12);
        Canvas.ForceUpdateCanvases();
        check(
            hudLabel.preferredHeight <= hudLabel.rectTransform.rect.height && hudLabel.preferredWidth <= hudLabel.rectTransform.rect.width,
            "Long outpost names and all threat counts fit the saved HUD bounds");
        check(
            !hudLabel.supportRichText && hudLabel.GetComponent<UnityEngine.UI.Shadow>() != null,
            "Outpost names render literally with authored contrast shadow");
        hudLabel.text = currentLabel;
        capture?.Invoke("outpost");
        foreach (OutpostGarrisonData guard in data.garrison)
        {
            if (session.Outposts.TryGetDefender(siteId, guard.id, out ControlUnit unit))
            {
                Freeze(unit);
            }
        }

        Block armor = defender.GetComponentsInChildren<Block>().First(block => block.GetComponent<Cockpit>() == null && block.GetComponent<Durability>() != null);
        string armorId = armor.uniqueId;
        Durability health = armor.GetComponent<Durability>();
        health.ApplyDamage(5f, player);
        float damaged = health.currentDurability;
        EnemyIdentity survivingIdentity = defender.cockpit.GetComponent<EnemyIdentity>();
        int regroupIdentity = survivingIdentity.GetInstanceID();
        Vector3 cockpitBeforeRegroup = survivingIdentity.transform.position;
        PlayManager.instance.RefreshGroup(defender);
        yield return null;
        bool trackedAfterRegroup = session.Outposts.TryGetDefender(siteId, first.id, out ControlUnit regrouped);
        Debug.Log("Outpost regroup: tracked=" + trackedAfterRegroup + ", expected=" + regroupIdentity + ", actual=" + (regrouped != null && regrouped.cockpit != null ? regrouped.cockpit.GetComponent<EnemyIdentity>().GetInstanceID() : 0) + ", identityAlive=" + (survivingIdentity != null) + ", identityPosition=" + (survivingIdentity != null ? survivingIdentity.transform.position.ToString() : "destroyed") + ", player=" + player.transform.position);
        check(
            trackedAfterRegroup && regrouped != null && regrouped.cockpit.GetComponent<EnemyIdentity>().GetInstanceID() == regroupIdentity,
            "Damaged defender remains tracked after block regroup");
        check(
            Vector3.Distance(survivingIdentity.transform.position, cockpitBeforeRegroup) < 2f,
            "Interpolated regroup preserves cockpit world pose across the next rendered frame");
        Freeze(regrouped);
        yield return TravelTo(player.transform, origin + Vector3.right * 250f + Vector3.up * 25f);
        for (int i = 0; i < 10; i++)
        {
            yield return null;
        }

        check(
            !session.Outposts.TryGetDefender(siteId, first.id, out _),
            "Far live defender hibernates without being marked defeated");
        check(!session.Outposts.IsDefeated(siteId, first.id), "Unloading is not a kill");
        yield return TravelTo(player.transform, origin + Vector3.up * 25f);
        for (int i = 0; i < 900 && !session.Outposts.TryGetDefender(siteId, first.id, out defender); i++)
        {
            yield return null;
        }

        check(defender != null, "Live defender returns when revisiting streamed terrain");
        Block restoredArmor = defender.GetComponentsInChildren<Block>().FirstOrDefault(block => block.uniqueId == armorId);
        check(
            restoredArmor != null && Mathf.Abs(restoredArmor.GetComponent<Durability>().currentDurability - damaged) < 0.001f,
            "Revisit preserves defender module identity and damage");
        Freeze(defender);
        yield return TravelWithDefender(player.transform, defender, origin + Vector3.right * 280f + Vector3.up * 25f);
        yield return TravelTo(player.transform, origin + Vector3.up * 25f);
        check(
            !session.Outposts.TryGetDefender(siteId, first.id, out _),
            "Pursuing guard hibernates at its distant last position");
        check(!session.Outposts.IsDefeated(siteId, first.id), "Distant pursuit is not a defeat");
        yield return TravelTo(player.transform, origin + Vector3.right * 200f + Vector3.up * 25f);
        for (int i = 0; i < 900 && !session.Outposts.TryGetDefender(siteId, first.id, out defender); i++)
        {
            yield return null;
        }

        check(
            defender != null,
            "Pursuing guard resumes near its last position outside original site activation range");
        Freeze(defender);
        yield return TravelWithDefender(player.transform, defender, origin + Vector3.up * 25f);
        foreach (OutpostGarrisonData guard in data.garrison)
        {
            for (int i = 0; i < 600 && !session.Outposts.TryGetDefender(siteId, guard.id, out _) && !session.Outposts.IsDefeated(siteId, guard.id); i++)
            {
                yield return null;
            }

            if (session.Outposts.TryGetDefender(siteId, guard.id, out ControlUnit unit) && unit.cockpit != null)
            {
                unit.cockpit.GetComponent<Durability>().ApplyDamage(1000000f, player);
                yield return null;
            }

            check(
                session.Outposts.IsDefeated(siteId, guard.id),
                "Cockpit defeat permanently consumes slot for this expedition: " + guard.id);
        }

        yield return TravelTo(player.transform, origin + Vector3.right * 250f + Vector3.up * 25f);
        for (int i = 0; i < 180; i++)
        {
            yield return null;
        }

        yield return TravelTo(player.transform, origin + Vector3.up * 25f);
        for (int i = 0; i < 180; i++)
        {
            yield return null;
        }

        check(
            data.garrison.All(guard => session.Outposts.IsDefeated(siteId, guard.id)),
            "Revisit never refreshes defeated garrison");
        check(
            data.garrison.All(guard => !session.Outposts.TryGetDefender(siteId, guard.id, out ControlUnit unit) || !unit.HasValidCockpit),
            "Defeated slots have no replacement enemy");
        BuildingRuntime revisited = session.Buildings.Buildings.FirstOrDefault(item => item != null && item.InstanceId == siteId);
        if (revisited != null)
        {
            foreach (Block block in revisited.Blocks.Where(block => block != null && block.GetComponent<Turret>() != null).ToArray())
            {
                block.GetComponent<Durability>().ApplyDamage(1000000f, player);
            }
        }

        yield return null;
        check(session.Outposts.ClearedCount > 0, "Outpost HUD can show cleared encounter state");
        for (int frame = 0; frame < 30; frame++)
        {
            yield return null;
        }

        check(
            hudLabel.text.Contains("CLEARED") && hudLabel.preferredHeight <= hudLabel.rectTransform.rect.height,
            "Cleared outpost status and expedition summary remain visible");
        capture?.Invoke("outpost-cleared");
        if (salvage)
        {
            yield return OutpostSalvagePlayerChecks.Run(revisited, player, check, recoverAllParts);
        }

        yield return TravelTo(player.transform, previous);
    }

    private static IEnumerator TravelTo(Transform player, Vector3 destination)
    {
        for (int frame = 0; frame < 6000 && Vector3.Distance(player.position, destination) > 0.2f; frame++)
        {
            player.position = Vector3.MoveTowards(player.position, destination, 1f);
            yield return null;
        }

        if (Vector3.Distance(player.position, destination) > 1f)
        {
            throw new InvalidOperationException("Outpost route did not finish streaming the destination.");
        }
    }

    private static void Freeze(ControlUnit unit)
    {
        // Start schedules a delayed refresh that would re-enable hover on this controlled body.
        // Cancel only that fixture's pending refresh; natural combat above keeps normal lifecycle.
        unit.CancelInvoke(nameof(ControlUnit.RefreshChildren));
        unit.GetComponent<EnemyController>().enabled = false;
        if (unit.hoverFlightController != null)
        {
            unit.hoverFlightController.enabled = false;
        }

        unit.GetComponent<Rigidbody>().isKinematic = true;
    }

    private sealed class GuardObservation
    {
        internal readonly ControlUnit Unit;
        internal readonly string Name;
        private readonly Vector3 initial;
        internal bool Moved;
        internal bool Targeted;
        internal bool Powered;
        internal bool DamagedPlayer;
        internal bool Complete => Moved && Targeted && Powered && DamagedPlayer;

        internal GuardObservation(ControlUnit unit, string name)
        {
            Unit = unit;
            Name = name;
            initial = unit.transform.position;
        }

        internal void Sample(ControlUnit player, Durability health, float initialHealth)
        {
            if (Unit == null || !Unit.HasValidCockpit)
            {
                return;
            }

            Moved |= Vector3.Distance(initial, Unit.transform.position) > 1f;
            Targeted |= Unit.target == player.transform && Unit.MovementInput.sqrMagnitude > 0.01f;
            Powered |= Unit.hoverThrusters.Any(item => item != null && item.thrust > 1f);
            DamagedPlayer |= health.currentDurability < initialHealth && health.LastAttacker == Unit;
        }

        internal string Describe()
        {
            return $"Outpost group combat [{Name}]: moved={Moved}, target={Targeted}, hover={Powered}, damage={DamagedPlayer}";
        }
    }

    private static IEnumerator TravelWithDefender(Transform player, ControlUnit unit, Vector3 destination)
    {
        // Controlled transport isolates streaming state from AI navigation. Both move through loaded terrain.
        Freeze(unit);
        Vector3 offset = Vector3.up * 20f;
        for (int frame = 0; frame < 6000 && Vector3.Distance(player.position, destination) > 0.2f; frame++)
        {
            player.position = Vector3.MoveTowards(player.position, destination, 1f);
            unit.transform.position = player.position + offset;
            yield return null;
        }
    }
}
