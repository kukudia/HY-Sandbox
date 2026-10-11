// 职责：在临时运行对象上验证按工作需求分配、停机释放、断电恢复和真实推进/入库启动。
// 入口：Run()；需要已连接 Editor 的 Play Mode，由武器回归 probe 调用并汇总结果。
// 生命周期：仅创建临时对象，退出销毁全部对象并恢复原模拟开关，不保存场景或资产。
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ActivePowerPlayChecks
{
    public static string[] Run()
    {
        if (!Application.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Active power checks require Play Mode and PlayManager.");
        }

        var checks = new List<string>();
        var root = new GameObject("Active power regression probe");
        root.transform.position = new Vector3(4000f, 4000f, 4000f);
        bool previousMode = PlayManager.instance.playMode;
        try
        {
            PlayManager.instance.playMode = true;
            CheckNetwork(root.transform, checks);
            CheckThruster(root.transform, checks);
            CheckCargo(root.transform, checks);
            CheckCurrencyAttraction(root.transform, checks);
            CheckBots(root.transform, checks);
            return checks.ToArray();
        }
        finally
        {
            PlayManager.instance.playMode = previousMode;
            Object.DestroyImmediate(root);
        }
    }

    private static void CheckNetwork(Transform parent, List<string> checks)
    {
        var network = new PowerNetwork();
        PowerTransmissionDevice relay = Child<PowerTransmissionDevice>(parent, "Relay");
        relay.powerRange = 10;
        relay.maxConnectionDistance = 0;
        PowerGeneratingUnit generator = Child<PowerGeneratingUnit>(parent, "Generator");
        generator.outputPower = 200f;
        Power first = Child<Power>(parent, "100 power load");
        first.minWorkingPower = 10f;
        first.standardWorkingPower = 100f;
        Power second = Child<Power>(parent, "300 power load");
        second.minWorkingPower = 10f;
        second.standardWorkingPower = 300f;
        Power[] loads =
        {
            first,
            second
        };
        PowerTransmissionDevice[] relays =
        {
            relay
        };
        PowerGeneratingUnit[] generators =
        {
            generator
        };
        network.Rebuild(relays, generators, loads);
        Check(
            checks,
            "Idle loads consume zero power and cannot run",
            first.currentPower == 0f && second.currentPower == 0f && !first.isWorking);
        first.SetWorkRequested(first, true);
        second.SetWorkRequested(second, true);
        network.Rebuild(relays, generators, loads);
        Check(
            checks,
            "Shortage allocates in demand ratio (50 / 150)",
            Near(first.currentPower, 50f) && Near(second.currentPower, 150f));
        second.SetWorkRequested(second, false);
        Check(
            checks,
            "Stopping clears allocated power immediately",
            second.currentPower == 0f && !second.isWorking);
        network.Rebuild(relays, generators, loads);
        Check(
            checks,
            "Remaining active load is capped at its 100 demand",
            Near(first.currentPower, 100f) && second.currentPower == 0f);
        generator.outputPower = 0f;
        network.Rebuild(relays, generators, loads);
        Check(
            checks,
            "Power loss preserves work intent but clears supply",
            first.IsWorkRequested && !first.isWorking && first.currentPower == 0f);
        generator.outputPower = 200f;
        network.Rebuild(relays, generators, loads);
        Check(
            checks,
            "Restored output restarts requesting load without power gate deadlock",
            first.isWorking && Near(first.currentPower, 100f));
        first.SetWorkRequested(parent.gameObject, true);
        first.SetWorkRequested(first, false);
        Check(checks, "One requester stopping preserves another request", first.IsWorkRequested);
        first.SetWorkRequested(parent.gameObject, false);
        Check(
            checks,
            "Last requester stopping releases load",
            !first.IsWorkRequested && first.currentPower == 0f);
        first.SetWorkRequested(first, true);
        relay.transform.localPosition = Vector3.left * 6f;
        relay.powerRange = 6;
        generator.transform.localPosition = relay.transform.localPosition;
        generator.outputPower = 40f;
        PowerTransmissionDevice otherRelay = Child<PowerTransmissionDevice>(parent, "Independent relay");
        otherRelay.transform.localPosition = Vector3.right * 6f;
        otherRelay.powerRange = 6;
        otherRelay.maxConnectionDistance = 0;
        PowerGeneratingUnit otherGenerator = Child<PowerGeneratingUnit>(parent, "Independent generator");
        otherGenerator.transform.localPosition = otherRelay.transform.localPosition;
        otherGenerator.outputPower = 80f;
        network.Rebuild(new[] { relay, otherRelay }, new[] { generator, otherGenerator }, loads);
        Check(
            checks,
            "Independent networks add supply and cap at remaining demand",
            Near(first.currentPower, 100f));
        otherRelay.transform.localPosition = relay.transform.localPosition;
        generator.outputPower = 40f;
        network.Rebuild(new[] { relay, otherRelay }, new[] { generator }, loads);
        Check(
            checks,
            "Overlapping relays sharing generator never duplicate output",
            Near(first.currentPower, 40f));
        network.Clear(loads);
        Check(
            checks,
            "Network clear removes residual power",
            first.currentPower == 0f && second.currentPower == 0f);
    }

    private static void CheckCurrencyAttraction(Transform parent, List<string> checks)
    {
        ControlUnit owner = Child<ControlUnit>(parent, "Currency owner");
        owner.faction = UnitFaction.Player;
        owner.enabled = false;
        owner.cockpit = Child<Cockpit>(owner.transform, "Cockpit");
        owner.hasValidCockpit = true;
        GameObject holdObject = Object.Instantiate(Resources.Load<GameObject>("Blocks/CoinHold"), owner.transform);
        Power power = holdObject.GetComponent<Power>();
        CargoHold hold = holdObject.GetComponent<CargoHold>();
        LootDrop drop = LootDrop.Spawn(
            new CargoItem { kind = CargoKind.Coins, amount = 5 },
            holdObject.transform.position + Vector3.forward);
        try
        {
            typeof(LootDrop).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(drop, null);
            Check(
                checks,
                "Currency attraction reserves idle hold and requests supply",
                drop.Destination == hold && power.IsWorkRequested && power.currentPower == 0f);
            Vector3 waiting = drop.transform.position;
            typeof(LootDrop).GetField("_scatterRemaining", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(drop, 0f);
            typeof(LootDrop).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(drop, null);
            Check(checks, "Unpowered currency attraction waits without moving", drop.transform.position == waiting);
            power.ReceivePower(power.RequestedPower);
            Check(
                checks,
                "Currency delivery succeeds after requested supply and releases demand",
                drop.Deliver(hold) && hold.Used == 5 && !power.IsWorkRequested);
        }
        finally
        {
            if (drop != null)
            {
                Object.DestroyImmediate(drop.gameObject);
            }
        }
    }

    private static void CheckThruster(Transform parent, List<string> checks)
    {
        ControlUnit owner = CreateOwner(parent, "Thrust owner");
        Rigidbody body = owner.gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        MainThruster thruster = Child<MainThruster>(owner.transform, "Main thruster");
        thruster.SetRuntimeReferences(owner, body);
        Power power = thruster.GetComponent<Power>();
        Tick(thruster);
        Check(checks, "Main thruster idle input requests no power", !power.IsWorkRequested);
        owner.movementInput = Vector3.forward;
        Tick(thruster);
        Check(
            checks,
            "Main thruster input requests power before receiving any",
            power.IsWorkRequested && thruster.thrust == 0f);
        power.ReceivePower(power.RequestedPower);
        Tick(thruster);
        Check(
            checks,
            "Powered main thruster starts after requested supply arrives",
            power.isWorking && thruster.thrust > 0f);
        owner.movementInput = Vector3.back;
        Tick(thruster);
        Check(
            checks,
            "Opposite input leaves directional thruster idle",
            !power.IsWorkRequested && power.currentPower == 0f);
        owner.movementInput = Vector3.forward;
        Tick(thruster);
        power.ReceivePower(power.RequestedPower);
        thruster.enabled = false;
        Check(
            checks,
            "Disabling main thruster releases power and output",
            !power.IsWorkRequested && thruster.thrust == 0f);
    }

    private static void CheckCargo(Transform parent, List<string> checks)
    {
        ControlUnit owner = CreateOwner(parent, "Cargo owner");
        Power power = Child<Power>(owner.transform, "Cargo hold");
        power.gameObject.AddComponent<BoxCollider>();
        power.gameObject.AddComponent<Durability>();
        CargoHold hold = power.gameObject.AddComponent<CargoHold>();
        var item = new CargoItem
        {
            kind = CargoKind.SpecialPart,
            resourcePath = "Blocks/Block",
            amount = 1
        };
        Check(
            checks,
            "Idle cargo hold can accept reservations without consuming power",
            hold.CanReceive(item) && !power.IsWorkRequested);
        LootDrop drop = Child<LootDrop>(parent, "Reserved drop");
        drop.Initialize(item);
        CollectionBot claimant = Child<CollectionBot>(parent, "Reservation claimant");
        claimant.enabled = false;
        Check(
            checks,
            "Cargo reservation starts work request without initial power",
            drop.Claim(claimant, hold) && power.IsWorkRequested);
        Check(checks, "Cargo cannot store before supply arrives", hold.Store(item, drop) == 0);
        power.ReceivePower(power.RequestedPower);
        Check(checks, "Cargo stores once requested supply arrives", hold.Store(item, drop) == 1);
        drop.Release();
        Check(
            checks,
            "Releasing final cargo reservation releases power",
            !power.IsWorkRequested && power.currentPower == 0f);
        Object.DestroyImmediate(drop.gameObject);
    }

    private static ControlUnit CreateOwner(Transform parent, string name)
    {
        ControlUnit owner = Child<ControlUnit>(parent, name);
        owner.enabled = false;
        owner.cockpit = Child<Cockpit>(owner.transform, "Cockpit");
        owner.cockpit.enabled = false;
        owner.hasValidCockpit = true;
        owner.faction = UnitFaction.Enemy;
        return owner;
    }

    private static void CheckBots(Transform parent, List<string> checks)
    {
        ControlUnit owner = CreateOwner(parent, "Bot owner");
        Power bay = Child<Power>(owner.transform, "Repair bay");
        RepairBot repair = Child<RepairBot>(bay.transform, "Repair bot");
        repair.home = bay.transform;
        TickBot(repair, "Start");
        TickBot(repair, "FixedUpdate");
        Check(checks, "Docked repair bot without target requests no power", !bay.IsWorkRequested);
        Durability target = Child<Durability>(owner.transform, "Damaged target");
        target.currentDurability = 50f;
        target.maxDurability = 100f;
        repair.currentTarget = target;
        TickBot(repair, "FixedUpdate");
        Check(
            checks,
            "Repair mission requests power without initial supply",
            bay.IsWorkRequested && !bay.isWorking);
        repair.ClearTarget();
        Check(checks, "Cancelling docked repair mission releases demand", !bay.IsWorkRequested);
        repair.currentTarget = target;
        TickBot(repair, "FixedUpdate");
        bay.ReceivePower(bay.RequestedPower);
        repair.enabled = false;
        Check(checks, "Disabling repair bot releases bay supply", !bay.IsWorkRequested && bay.currentPower == 0f);
        Power collectionBay = Child<Power>(owner.transform, "Collection bay");
        CollectionBot collection = Child<CollectionBot>(collectionBay.transform, "Collection bot");
        collection.home = collectionBay.transform;
        TickBot(collection, "Start");
        TickBot(collection, "FixedUpdate");
        Check(checks, "Docked collection bot without cargo requests no power", !collectionBay.IsWorkRequested);
        Power holdPower = Child<Power>(owner.transform, "Bot destination");
        holdPower.gameObject.AddComponent<BoxCollider>();
        holdPower.gameObject.AddComponent<Durability>();
        CargoHold hold = holdPower.gameObject.AddComponent<CargoHold>();
        LootDrop drop = Child<LootDrop>(parent, "Bot mission drop");
        drop.Initialize(new CargoItem { kind = CargoKind.SpecialPart, resourcePath = "Blocks/Block", amount = 1 });
        Check(checks, "Collection task can be claimed while bay is idle", drop.Claim(collection, hold));
        typeof(CollectionBot).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(collection, drop);
        typeof(CollectionBot).GetField("_destination", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(collection, hold);
        typeof(CollectionBot).GetField("_missionStarted", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(collection, Time.time);
        TickBot(collection, "FixedUpdate");
        Check(
            checks,
            "Collection mission requests power before supply and preserves claim",
            collectionBay.IsWorkRequested && drop.ClaimedBy == collection);
        collection.enabled = false;
        Check(
            checks,
            "Disabling collection bot releases both bay and cargo reservations",
            !collectionBay.IsWorkRequested && !holdPower.IsWorkRequested && drop.ClaimedBy == null);
    }

    private static void TickBot(Bot bot, string method)
    {
        bot.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bot, null);
    }

    private static T Child<T>(Transform parent, string name)
        where T : Component
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.AddComponent<T>();
    }

    private static void Tick(MainThruster thruster)
    {
        typeof(MainThruster).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(thruster, null);
    }

    private static bool Near(float first, float second)
    {
        return Mathf.Abs(first - second) < 0.001f;
    }

    private static void Check(List<string> checks, string description, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(description);
        }

        checks.Add(description);
    }
}
