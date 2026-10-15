// 职责：显式Player路线验证正式据点货仓的原有内容掉落、吸附和机器人回收。
// 入口：Run()；仅-world-smoke-outpost-salvage启用，临时玩家模块在finally清理，不写用户数据。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class OutpostSalvagePlayerChecks
{
    internal static IEnumerator Run(
        BuildingRuntime building,
        ControlUnit player,
        Action<bool, string> check,
        bool recoverAllParts = false)
    {
        check(building != null, "Cleared outpost remains present for authored cargo recovery");
        CargoHold[] source = building.GetComponentsInChildren<CargoHold>().Where(hold => hold.Used > 0).ToArray();
        check(source.Length > 0, "Generated outpost contains actual authored cargo");
        Vector3 previous = player.transform.position;
        var fixtures = new List<GameObject>();
        var receivers = new List<CargoHold>();
        var expected = new Dictionary<CargoKind, int>();
        try
        {
            // A stable recovery platform tests production navigation in the real building geometry.
            player.transform.position = source[0].transform.position + Vector3.up * 12f;
            Spawn("PowerGeneratingUnit", player, new Vector3(2f, 0f, 0f), fixtures);
            Spawn("PowerTransmissionDevice", player, new Vector3(-2f, 0f, 0f), fixtures);
            CollectionBot bot = Spawn("CollectionBotContainer", player, new Vector3(0f, 0f, -3f), fixtures).GetComponentInChildren<CollectionBot>();
            for (int i = 0; i < source.Length; i++)
            {
                CargoHold hostile = source[i];
                expected.TryGetValue(hostile.Kind, out int count);
                expected[hostile.Kind] = count + hostile.Used;
                string resource = hostile.Kind == CargoKind.Coins ? "CoinHold" : hostile.Kind == CargoKind.Technology ? "TechnologyHold" : "CargoHold";
                CargoHold receiver = Spawn(resource, player, new Vector3(4f + i * 3f, 0f, 0f), fixtures).GetComponent<CargoHold>();
                receiver.RestoreContents(new List<CargoItem>());
                receivers.Add(receiver);
                Spawn("PowerTransmissionDevice", player, new Vector3(4f + i * 3f, 0f, 3f), fixtures);
            }

            if (recoverAllParts)
            {
                // Natural combat already dropped other modules. Give the fixture real room for
                // those opportunistic pickups as well, without changing targeting or item ownership.
                expected.TryGetValue(CargoKind.SpecialPart, out int authoredParts);
                int required = authoredParts + LootDrop.Active.Where(drop => drop != null && drop.Item.kind == CargoKind.SpecialPart).Sum(drop => drop.Item.amount);
                int free = receivers.Where(hold => hold.Kind == CargoKind.SpecialPart).Sum(hold => hold.Free);
                for (int slot = source.Length; free < required; slot++)
                {
                    Vector3 local = new Vector3(4f + slot * 3f, 0f, 0f);
                    CargoHold receiver = Spawn("CargoHold", player, local, fixtures).GetComponent<CargoHold>();
                    receiver.RestoreContents(new List<CargoItem>());
                    receivers.Add(receiver);
                    Spawn("PowerTransmissionDevice", player, local + Vector3.forward * 3f, fixtures);
                    free += receiver.Free;
                }

                Debug.Log($"Outpost full recovery capacity: required={required}, available={free}");
            }

            player.RefreshChildren();
            player.AssignRuntimeOwnershipToBlocks(true);
            yield return null;
            // Take the ledger after startup frames so unrelated arrivals cannot change its baseline.
            var priorDrops = new HashSet<LootDrop>(LootDrop.Active);
            var priorAmounts = LootDrop.Active.Where(drop => drop != null).GroupBy(drop => drop.Item.kind).ToDictionary(group => group.Key, group => group.Sum(drop => drop.Item.amount));
            foreach (CargoHold hostile in source)
            {
                hostile.GetComponent<Durability>().ApplyDamage(1000000f, player);
            }

            LootDrop[] authoredDrops = LootDrop.Active.Where(drop => drop != null && !priorDrops.Contains(drop)).ToArray();
            foreach (KeyValuePair<CargoKind, int> item in expected)
            {
                priorAmounts.TryGetValue(item.Key, out int before);
                int after = LootDrop.Active.Where(drop => drop != null && drop.Item.kind == item.Key).Sum(drop => drop.Item.amount);
                Debug.Log($"Outpost cargo release [{item.Key}]: expected={item.Value}, before={before}, after={after}, newPackets={authoredDrops.Count(drop => drop.Item.kind == item.Key)}");
                check(
                    after - before >= item.Value,
                    "Destroyed outpost holds release authored content including merged packets: " + item.Key);
            }

            LootDrop[] parts = authoredDrops.Where(drop => drop.Item.kind == CargoKind.SpecialPart).ToArray();
            bool sawOriginalCarry = false;
            var carriedParts = new bool[parts.Length];
            bool sawDockedDelivery = parts.Length == 0;
            float until = Time.time + (recoverAllParts ? 300f : 100f);
            while (Time.time < until)
            {
                sawOriginalCarry |= parts.Any(drop => drop != null && drop.IsCarried && drop.ClaimedBy == bot);
                for (int i = 0; i < parts.Length; i++)
                {
                    carriedParts[i] |= parts[i] != null && parts[i].IsCarried && parts[i].ClaimedBy == bot;
                }

                bool partDelivered = parts.Length == 0 || (recoverAllParts ? parts.All(drop => drop == null) : parts.Any(drop => drop == null));
                bool allCurrency = expected.Where(item => item.Key != CargoKind.SpecialPart).All(item => receivers.Where(hold => hold.Kind == item.Key).Sum(hold => hold.Used) >= item.Value);
                sawDockedDelivery |= (parts.Length == 0 || parts.Any(drop => drop == null)) && bot.transform.parent == bot.home;
                if (allCurrency && partDelivered && sawDockedDelivery)
                {
                    break;
                }

                yield return null;
            }

            Debug.Log($"Outpost recovery: botState={bot.currentState}, carriedOriginal={sawOriginalCarry}, dockedDelivery={sawDockedDelivery}, bot={bot.transform.position}, home={bot.home.position}");
            foreach (CargoHold receiver in receivers)
            {
                Debug.Log($"Outpost receiver [{receiver.Kind}]: used={receiver.Used}, power={receiver.GetComponent<Power>().isWorking}, position={receiver.transform.position}");
            }

            if (recoverAllParts)
            {
                Debug.Log($"Outpost complete recovery: authored={parts.Length}, observedCarried={carriedParts.Count(value => value)}, remaining={parts.Count(drop => drop != null)}");
                foreach (LootDrop drop in parts.Where(drop => drop != null))
                {
                    Debug.Log($"Outpost unrecovered module: {drop.Item.resourcePath}, position={drop.transform.position}, claimed={drop.ClaimedBy != null}, carried={drop.IsCarried}");
                }
            }

            foreach (KeyValuePair<CargoKind, int> item in expected.Where(item => item.Key != CargoKind.SpecialPart))
            {
                check(
                    receivers.Where(hold => hold.Kind == item.Key).Sum(hold => hold.Used) >= item.Value,
                    "Authored outpost currency reaches matching powered player holds: " + item.Key);
            }

            if (parts.Length > 0)
            {
                check(sawOriginalCarry, "Collection robot physically carries a drop from the actual outpost hold");
                check(
                    sawDockedDelivery && parts.Any(drop => drop == null),
                    "Authored outpost part is consumed only after robot home docking");
                check(
                    receivers.Any(hold => hold.Kind == CargoKind.SpecialPart && hold.Used > 0),
                    "Player cargo contains recovered outpost module");
                if (recoverAllParts)
                {
                    int delivered = receivers.Where(hold => hold.Kind == CargoKind.SpecialPart).Sum(hold => hold.Used);
                    check(
                        carriedParts.All(value => value) && parts.All(drop => drop == null) && delivered >= expected[CargoKind.SpecialPart],
                        "Every authored outpost module is physically carried and delivered into player cargo");
                }
            }
        }
        finally
        {
            foreach (GameObject fixture in fixtures)
            {
                if (fixture != null)
                {
                    foreach (Bot bot in fixture.GetComponentsInChildren<Bot>(true))
                    {
                        bot.PrepareForHomeDestruction();
                    }

                    Object.Destroy(fixture);
                }
            }

            player.transform.position = previous;
            player.RefreshChildren();
        }
    }

    private static GameObject Spawn(string resource, ControlUnit owner, Vector3 local, List<GameObject> fixtures)
    {
        GameObject obj = Object.Instantiate(Resources.Load<GameObject>("Blocks/" + resource), owner.transform);
        obj.transform.localPosition = local;
        obj.transform.localRotation = Quaternion.identity;
        fixtures.Add(obj);
        return obj;
    }
}
