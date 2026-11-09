// 职责：验证返航经济与真实刚体导引，不修改玩家蓝图或持久场景。
// 入口：Run()，需在有 PlayManager 的 Play Mode 运行；独立 PhysicsScene 手动推进物理。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SiegeReturnChecks
{
    private sealed class Host : ISiegeDeploymentHost
    {
        public SiegeMatch Match { get; } = new SiegeMatch(
            1000,
            1,
            0,
            1,
            20,
            50);
        public SiegeSettings Settings => null;
        public SiegeFleet Fleet
        {
            get; set;
        }
        public SiegeReturnFlight FlightGuidance
        {
            get; set;
        }
        public SiegeShipOption[] DefenderOptions => Array.Empty<SiegeShipOption>();
        public SiegeShipOption[] AttackerOptions => Array.Empty<SiegeShipOption>();
        public bool IsEditingBlueprint => false;
        public bool IsRunning => true;
        public int SelectedDefender => -1;
        public int SelectedAttacker => -1;

        public event Action RefitChanged
        {
            add
            {
            }

            remove
            {
            }
        }

        public void SelectDefender(int index)
        {
        }

        public void SelectAttacker(int index)
        {
        }

        public bool RefreshBlueprints() => false;
        public bool TryDeployAt(int optionIndex, int dock, out string reason)
        {
            reason = "test host";
            return false;
        }

        public bool TryReplaceAt(int optionIndex, int dock, out string reason) => Fleet.ReplaceAt(optionIndex, dock, out reason);
        public bool TryCancelDeployment(int dock, out string reason) => Fleet.CancelDeployment(dock, out reason);
        public bool TryTakeControl(int slot) => false;
        public bool TryTakeControl(string id) => false;
        public bool TryRepairShip(string id, bool repairBase = false) => false;
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks = new List<string>();
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Return checks require Play Mode with PlayManager.");
        }

        var report = new Report();
        CheckEconomy(report);
        Scene scene = SceneManager.CreateScene("Hangar return isolated physics", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        bool previousSimulation = PlayManager.instance.playMode;
        SiegeReturnFlight flight = null;
        try
        {
            PlayManager.instance.playMode = true;
            PhysicsScene physics = scene.GetPhysicsScene();
            var dock = new GameObject("Validation dock");
            SceneManager.MoveGameObjectToScene(dock, scene);
            dock.transform.position = new Vector3(1200f, 200f, 1200f);
            var host = new Host();
            host.Fleet = new SiegeFleet(host, new[] { dock.transform }, null, false);
            var settings = new SiegeFlightSettings
            {
                timeout = 90f,
                disabledGrace = 0.2f,
                speed = 12f,
                acceleration = 10f
            };
            flight = new SiegeReturnFlight(host.Fleet, settings, PlayManager.instance);
            host.FlightGuidance = flight;
            flight.BeginReturn();
            Check(report, "empty fleet cannot stall return settlement", flight.IsComplete && flight.TotalCount == 0);
            SiegeFleetEntry entry = Spawn(scene, dock.transform);
            host.Fleet.Ships[0] = entry;
            Rigidbody body = entry.Unit.GetComponent<Rigidbody>();
            // 平移/制动隔离测试不依赖悬浮调参；电源分配边界在每物理步显式注入实际工作功率。
            body.useGravity = false;
            flight.BeginLaunch();
            Vector3 start = body.position;
            Step(flight, physics, true, 1);
            Check(report, "guidance never teleports at launch", Vector3.Distance(start, body.position) < 0.2f);
            Step(flight, physics, true, 4500);
            Check(
                report,
                "powered ship physically exits its bay: " + (body != null ? body.position.ToString() : "scrapped") + ", completed=" + flight.CompletedCount + ", failed=" + flight.FailedCount,
                flight.IsComplete && flight.CompletedCount == 1 && Vector3.Distance(start, body.position) > 10f);
            flight.BeginReturn();
            Step(flight, physics, true, 4500);
            Check(
                report,
                "powered ship navigates approach and docks: " + (body != null ? body.position.ToString() : "scrapped") + ", completed=" + flight.CompletedCount + ", failed=" + flight.FailedCount,
                flight.IsComplete && flight.CompletedCount == 1 && entry.IsAlive);
            Check(
                report,
                "docked ship is parked within tolerance",
                body.isKinematic && Vector3.Distance(body.position, entry.DockPosition) <= settings.dockingTolerance + 0.01f);
            Check(
                report,
                "autopilot releases power work requests",
                !Array.Exists(
                    entry.Unit.GetComponentsInChildren<Thruster>(),
                    engine => engine.GetComponent<Power>().IsWorkRequested));
            // 丧失推进能力的舰船留出电网刷新宽限，之后报废而非卡死结算。
            flight.Stop();
            Object.DestroyImmediate(entry.Unit.gameObject);
            entry = Spawn(scene, dock.transform);
            host.Fleet.Ships[0] = entry;
            body = entry.Unit.GetComponent<Rigidbody>();
            body.position += new Vector3(20f, 0f, 20f);
            body.useGravity = false;
            foreach (Thruster engine in entry.Unit.GetComponentsInChildren<Thruster>())
            {
                engine.enabled = false;
            }

            flight.BeginReturn();
            Step(flight, physics, false, 1);
            Check(report, "lost propulsion receives a grace period", entry.IsAlive && !flight.IsComplete);
            Step(flight, physics, false, 30);
            Check(
                report,
                "disabled ship is scrapped and return completes",
                !entry.IsAlive && flight.IsComplete && flight.FailedCount == 1);
        }
        finally
        {
            flight?.Stop();
            PlayManager.instance.playMode = previousSimulation;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                root.SetActive(false);
                Object.DestroyImmediate(root);
            }

            SceneManager.UnloadSceneAsync(scene);
        }

        string json = JsonUtility.ToJson(report, true);
        Directory.CreateDirectory("Temp/Hangars");
        File.WriteAllText("Temp/Hangars/return-checks.json", json);
        return json;
    }

    private static SiegeFleetEntry Spawn(Scene scene, Transform dock)
    {
        TextAsset source = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch");
        BlockDataList blueprint = SiegeFleet.PrepareBlueprint(source, out string id);
        ControlUnit unit = ModularUnitFactory.Spawn(blueprint, "Return validation ship", dock.position, dock.rotation, UnitFaction.Player);
        if (unit == null)
        {
            throw new InvalidOperationException("Return fixture could not restore a ship.");
        }

        SceneManager.MoveGameObjectToScene(unit.gameObject, scene);
        unit.runtimeUnitId = id;
        unit.AssignRuntimeOwnershipToBlocks(true);
        return new SiegeFleetEntry(
            id,
            "Return validation ship",
            blueprint,
            unit.cockpit,
            1,
            dock.position,
            dock.rotation);
    }

    private static void Step(SiegeReturnFlight flight, PhysicsScene physics, bool powered, int maximumSteps)
    {
        const float dt = 0.02f;
        for (int step = 0; step < maximumSteps && !flight.IsComplete; step++)
        {
            if (powered)
            {
                foreach (Power power in Power.ActiveBlocks)
                {
                    if (power != null && power.gameObject.scene.GetPhysicsScene() == physics)
                    {
                        power.ReceivePower(power.RequestedPower);
                    }
                }
            }

            flight.Tick(dt);
            physics.Simulate(dt);
        }
    }

    private static void CheckEconomy(Report report)
    {
        var match = new SiegeMatch(
            100,
            1,
            0,
            1,
            20,
            50);
        Check(report, "return settlement enabled before first wave", match.EnableReturnSettlement());
        Check(
            report,
            "wave starts and attacker registers",
            match.BeginNextWave() && match.RegisterAttacker("enemy", 1));
        Check(
            report,
            "last attacker enters returning without issuing coins",
            match.ResolveAttacker("enemy", true) && match.Phase == SiegePhase.Returning && match.Coins == 100 && match.PendingCoins == 70);
        Check(
            report,
            "duplicate attacker resolution cannot duplicate pending coins",
            !match.ResolveAttacker("enemy", true) && match.PendingCoins == 70);
        Check(report, "return phase locks purchases and next wave", !match.TrySpend(1) && !match.BeginNextWave());
        Check(
            report,
            "last-kill combo remains pending during return",
            match.GrantBonus(10) && match.PendingCoins == 80 && match.Coins == 100);
        Check(
            report,
            "settlement occurs once after return",
            match.CompleteReturn() && match.Coins == 180 && match.PendingCoins == 0 && !match.CompleteReturn());
        Check(
            report,
            "next wave does not re-enable first-wave configuration",
            !match.EnableReturnSettlement() && match.BeginNextWave());
        match.RegisterAttacker("second", 1);
        match.ResolveAttacker("second", true);
        match.LoseBase();
        Check(
            report,
            "defeat discards unissued rewards",
            match.Phase == SiegePhase.Defeat && match.Coins == 180 && match.PendingCoins == 0 && !match.CompleteReturn());
        var legacy = new SiegeMatch(
            100,
            1,
            0,
            1,
            20,
            50);
        legacy.BeginNextWave();
        legacy.RegisterAttacker("legacy", 1);
        legacy.ResolveAttacker("legacy", true);
        Check(
            report,
            "legacy pure match retains immediate settlement",
            legacy.Phase == SiegePhase.Intermission && legacy.Coins == 170);
    }

    private static void Check(Report report, string description, bool success)
    {
        if (!success)
        {
            throw new InvalidOperationException("Return check failed: " + description);
        }

        report.checks.Add("PASS " + description);
    }
}
