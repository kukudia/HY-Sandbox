// 职责：在真实Play帧验证场景泊位点击、无副作用预览、精确扣款、友舰接管及Workshop隔离往返。
// 入口：Run()；使用临时输入设备和阻挡夹具，不写用户蓝图。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeSceneInteractionChecks
{
    private static bool running;
    public static void Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || running || session?.Match?.Wave != 0 || session.Fleet.Ships.Any(ship => ship != null))
        {
            throw new InvalidOperationException("Requires a fresh Siege Play session.");
        }

        running = true;
        session.StartCoroutine(RunChecks(session));
    }

    private static IEnumerator RunChecks(SiegeSession session)
    {
        var checks = new List<string>();
        int best = SiegeProgress.BestWave;
        int totalKills = SiegeProgress.TotalKills;
        int runs = SiegeProgress.TotalRuns;
        Mouse previous = Mouse.current;
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        InputSettings.BackgroundBehavior background = InputSystem.settings.backgroundBehavior;
        GameObject blocker = null;
        string report = "Temp/Siege/scene-interaction-checks.txt";
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText(report, "Running");
        try
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.EnableDevice(mouse);
            mouse.MakeCurrent();
            SiegeDeploymentView view = Object.FindFirstObjectByType<SiegeDeploymentView>();
            int cheapest = Array.FindIndex(session.DefenderOptions, option => option.Price == session.DefenderOptions.Min(item => item.Price));
            session.SelectDefender(cheapest);
            long balance = session.Match.Coins;
            yield return null;
            yield return null;
            Button marker = view.GetComponentsInChildren<Button>().First(button => button.name.StartsWith("Deployment Marker"));
            yield return Click(marker, mouse);
            int dock = view.SelectedDock;
            Check(
                checks,
                "actual pointer click selects a world deployment marker",
                dock >= 0 && view.Preview != null);
            Check(
                checks,
                "preview has no behaviours or colliders",
                view.Preview.GetComponentsInChildren<MonoBehaviour>().Length == 0 && view.Preview.GetComponentsInChildren<Collider>().Length == 0);
            Check(
                checks,
                "preview spends no credits and creates no fleet entry",
                session.Match.Coins == balance && session.Fleet.Ships.All(ship => ship == null));
            yield return Click(Named(view, "Cancel Selection"), mouse);
            Check(
                checks,
                "actual cancel removes preview without spending",
                view.Preview == null && view.SelectedDock < 0 && session.Match.Coins == balance);
            view.SelectDock(dock);
            yield return null;
            yield return Click(Named(view, "Confirm Deployment"), mouse);
            SiegeFleetEntry first = session.Fleet.Ships[dock];
            Check(
                checks,
                "confirmation deploys exactly at the selected point",
                first?.IsAlive == true && first.DockPosition == session.Fleet.GetDeploymentPoint(dock).position);
            Check(
                checks,
                "confirmation debits exactly once",
                session.Match.Coins == balance - session.DefenderOptions[cheapest].Price);
            balance = session.Match.Coins;
            Check(
                checks,
                "occupied point rejects duplicate deployment",
                !session.TryDeployAt(cheapest, dock, out _) && session.Match.Coins == balance);
            int secondDock = (dock + 1) % session.Fleet.DeploymentCount;
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Temporary deployment obstruction";
            blocker.transform.position = session.Fleet.GetDeploymentPoint(secondDock).position;
            blocker.transform.localScale = Vector3.one * 40f;
            Physics.SyncTransforms();
            Check(
                checks,
                "specified obstructed point never falls back or charges",
                !session.TryDeployAt(cheapest, secondDock, out string reason) && reason.Contains("Blocked") && session.Match.Coins == balance);
            blocker.SetActive(false);
            Object.Destroy(blocker);
            blocker = null;
            Check(checks, "second support ship deploys", session.TryDeployAt(cheapest, secondDock, out _));
            yield return null;
            yield return null;
            SiegeFleetEntry second = session.Fleet.Ships[secondDock];
            view.SelectShip(second.Id);
            yield return Click(Named(view, "Pilot Ship"), mouse);
            Check(
                checks,
                "ship card transfers unique pilot ownership",
                session.Fleet.Driven == second && second.Unit.AcceptsPlayerInput && !first.Unit.AcceptsPlayerInput);
            view.CycleShip();
            Check(checks, "Tab action cycles surviving ships", session.Fleet.Driven == first);
            Durability health = second.Cockpit.GetComponent<Durability>();
            health.currentDurability -= 10f;
            long quote = SiegeRefit.Quote(second, session.Settings);
            balance = session.Match.Coins;
            view.SelectShip(second.Id);
            yield return Click(Named(view, "Repair Ship"), mouse);
            Check(
                checks,
                "selected non-pilot repair restores damage and exact debit",
                health.currentDurability == health.maxDurability && session.Match.Coins == balance - quote && session.Fleet.Driven == first);
            Durability core = session.Fleet.Base.Core.GetComponent<Durability>();
            core.currentDurability -= 17f;
            float damage = core.currentDurability;
            int layer = core.gameObject.layer;
            int mask = Camera.main.cullingMask;
            balance = session.Match.Coins;
            Check(checks, "workshop opens from intermission", session.Workshop.Open());
            Check(
                checks,
                "battlefield is in a separate unsimulated physics scene",
                session.Fleet.Base.Unit.gameObject.scene.GetPhysicsScene() != Physics.defaultPhysicsScene);
            Check(
                checks,
                "editing preserves damage and isolates rendering",
                core.currentDurability == damage && (Camera.main.cullingMask & (1 << core.gameObject.layer)) == 0);
            Check(
                checks,
                "editing rejects all spending and wave actions",
                !session.TryDeployAt(cheapest, 0, out _) && !session.TryRepair(true) && !session.TryTakeControl(0) && !session.BeginNextWave());
            yield return null;
            yield return null;
            Check(
                checks,
                "workshop closes and cancels active blueprint loading",
                session.Workshop.Close() && !BuildManager.instance.IsLoadingBlocks);
            yield return null;
            yield return null;
            Check(
                checks,
                "return restores scene layers camera and exact match state",
                core.currentDurability == damage && core.gameObject.layer == layer && Camera.main.cullingMask == mask && session.Match.Coins == balance && session.Fleet.Driven == first && first.Unit.gameObject.scene == session.gameObject.scene);
            Check(checks, "starting wave uses production entry", session.BeginNextWave());
            yield return null;
            yield return null;
            Check(
                checks,
                "combat hides empty pads and deployment preview",
                view.Preview == null && !view.GetComponentsInChildren<Button>().Any(button => button.name.StartsWith("Deployment Marker")));
            balance = session.Match.Coins;
            Check(
                checks,
                "combat rejects deployment and repair without spending",
                !session.TryDeployAt(cheapest, 0, out _) && !session.TryRepair(false) && session.Match.Coins == balance);
            session.LoseBase();
            yield return null;
            yield return null;
            view.SelectShip(first.Id);
            Check(checks, "defeat rejects ship selection", view.SelectedShipId == null);
            File.WriteAllText(report, "PASS " + checks.Count + " checks\n" + string.Join("\n", checks));
        }
        finally
        {
            SiegeProgress.RestoreForProbe(best, totalKills, runs);
            if (session.IsEditingBlueprint)
            {
                session.Workshop.Close();
            }

            if (blocker != null)
            {
                Object.Destroy(blocker);
            }

            InputSystem.RemoveDevice(mouse);
            previous?.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
            running = false;
            if (File.ReadAllText(report) == "Running")
            {
                File.WriteAllText(report, "FAILED\n" + string.Join("\n", checks));
            }
        }
    }

    private static Button Named(SiegeDeploymentView view, string name)
    {
        return view.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
    }

    private static IEnumerator Click(Button button, Mouse mouse)
    {
        Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4];
        button.GetComponent<RectTransform>().GetWorldCorners(corners);
        Vector2 point = (corners[0] + corners[2]) * 0.5f;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
        yield return null;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        checks.Add((passed ? "PASS " : "FAIL ") + name);
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }
    }
}
