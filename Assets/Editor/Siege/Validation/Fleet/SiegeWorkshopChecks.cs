// 职责：在真实Siege Play帧验证Main工作台、异步取消、存档写入和战场隔离。
// 入口：Run()；只创建唯一临时Saves蓝图，finally清理，绝不写用户已有蓝图。
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

public static class SiegeWorkshopChecks
{
    private static bool running;
    [MenuItem("HY-Sandbox/Siege/Check Main Workshop In Play")]
    public static void Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || running || session?.Workshop == null || !session.Match.CanRefit || session.IsEditingBlueprint)
        {
            throw new InvalidOperationException("Start a fresh normal Siege Play session first.");
        }

        running = true;
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/workshop-checks.txt", "Running");
        session.StartCoroutine(CheckRoutine(session));
    }

    private static IEnumerator CheckRoutine(SiegeSession session)
    {
        var checks = new List<string>();
        string name = "__SiegeWorkshopCheck_" + Guid.NewGuid().ToString("N");
        string path = SaveManager.instance.GetSavePath(name);
        Keyboard previousKeyboard = Keyboard.current;
        Mouse previousMouse = Mouse.current;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        InputSettings.BackgroundBehavior background = InputSystem.settings.backgroundBehavior;
        try
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.EnableDevice(keyboard);
            InputSystem.EnableDevice(mouse);
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch").text);
            blueprint.blocks = blueprint.blocks.Where(block => block.resourcePath.Contains("Cockpit")).Take(1).ToList();
            File.WriteAllText(path, JsonUtility.ToJson(blueprint, true));
            int cheapest = Array.FindIndex(session.DefenderOptions, option => option.Price == session.DefenderOptions.Min(item => item.Price));
            Check(checks, "deploy real defender before editing", session.TryDeploy(cheapest, out _));
            // Block.Start replaces prefab preview renderers with runtime geometry on the next frame.
            yield return null;
            yield return null;
            SiegeFleetEntry driven = session.Fleet.Driven;
            Durability core = session.Fleet.Base.Core.GetComponent<Durability>();
            core.currentDurability -= 25f;
            float health = core.currentDurability;
            long coins = session.Match.Coins;
            ControlUnit[] units = ControlUnit.RuntimeUnits.Where(unit => unit != null && unit.gameObject.scene == session.gameObject.scene).ToArray();
            Renderer untouchedRenderer = driven.Unit.GetComponentsInChildren<Renderer>().First(renderer => renderer.enabled);
            Collider untouchedCollider = driven.Unit.GetComponentsInChildren<Collider>().First(collider => collider.enabled);
            untouchedRenderer.enabled = false;
            untouchedCollider.enabled = false;
            Check(checks, "workshop opens during deployment", session.Workshop.Open(name));
            Check(
                checks,
                "live fleet and core survive entry",
                units.All(unit => unit != null && unit.gameObject.activeInHierarchy) && driven.IsAlive && core.currentDurability == health);
            Check(
                checks,
                "battle and spending operations blocked while editing",
                !session.BeginNextWave() && !session.TryDeploy(cheapest, out _) && !session.TryRepair(true) && !session.TryTakeControl(0));
            float deadline = Time.realtimeSinceStartup + 20f;
            while (BuildManager.instance.IsLoadingBlocks && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Check(
                checks,
                "Main asynchronous loader restores preview",
                !BuildManager.instance.IsLoadingBlocks && SaveManager.instance.blocks.Count == 1 && GameManager.instance.blocksParent.name == name);
            BuildManager.instance.CreateBlock(
                Resources.Load<GameObject>("Blocks/1x1x1"),
                "Blocks/1x1x1",
                new Vector3(2f, 2f, 0f),
                Quaternion.identity);
            Check(
                checks,
                "Main build writes only test blueprint",
                JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path)).blocks.Count == 2);
            ActionManager.instance.Undo();
            Check(
                checks,
                "undo persists removal",
                JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path)).blocks.Count == 1);
            yield return null;
            ActionManager.instance.Redo();
            Check(
                checks,
                "redo persists reconstruction",
                JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path)).blocks.Count == 2);
            yield return null;
            // Use P through the real InputManager.Update; the preview must not enter PlayManager.PlayStart.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Check(
                checks,
                "P returns to siege without starting simulation",
                !session.IsEditingBlueprint && !PlayManager.instance.playMode && !BuildManager.instance.enabled);
            Check(
                checks,
                "return preserves coins damage and driver",
                session.Match.Coins == coins && core.currentDurability == health && session.Fleet.Driven == driven && PlayManager.instance.blocksParent == driven.Unit.transform);
            Check(
                checks,
                "return restores original runtime objects",
                units.All(unit => unit != null && unit.gameObject.activeInHierarchy));
            Check(
                checks,
                "return preserves pre-existing disabled components",
                !untouchedRenderer.enabled && !untouchedCollider.enabled);
            untouchedRenderer.enabled = true;
            untouchedCollider.enabled = true;
            Check(
                checks,
                "Main integrity uses deployed hull maximum",
                PlayManager.instance.maxHealth == driven.MaximumDurability && driven.MaximumDurability > 0f);
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.B));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Check(
                checks,
                "B opens Main workshop through real input",
                session.IsEditingBlueprint && MainUIPanels.instance.buildPanel.activeInHierarchy);
            // Replace the initial load immediately; close while the two-block fixture is still loading.
            SaveManager.instance.LoadSave(name);
            Check(checks, "preview load starts for cancellation probe", BuildManager.instance.IsLoadingBlocks);
            session.Workshop.Close();
            yield return null;
            yield return null;
            Check(
                checks,
                "closing cancels loader and removes preview",
                !BuildManager.instance.IsLoadingBlocks && Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None).All(unit => unit.name != name));
            Check(
                checks,
                "cancellation never deletes fleet or heals base",
                units.All(unit => unit != null) && driven.IsAlive && core.currentDurability == health && session.Match.Coins == coins);
            Button entry = Object.FindFirstObjectByType<SiegeHud>().GetComponentsInChildren<Button>().Single(button => button.name == "Open Blueprint Workshop");
            var corners = new Vector3[4];
            entry.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector2 center = (corners[0] + corners[2]) * 0.5f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center, buttons = 1 });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center });
            yield return null;
            yield return null;
            Check(checks, "actual pointer click opens blueprint workshop", session.IsEditingBlueprint);
            session.Workshop.Close();
            yield return null;
            yield return null;
            // Keyboard starts an actual wave; Alt must release the pointer, matching Main controls.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
            Check(
                checks,
                "P begins wave from strategy mode",
                session.Match.Phase == SiegePhase.Battle && PlayManager.instance.playMode);
            Check(
                checks,
                "combat locks pointer and denies workshop",
                Cursor.lockState == CursorLockMode.Locked && !session.Workshop.Open(name));
            bool thrustersOpen = MainUIPanels.instance.ThrusterInfoPanel.IsOpen;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F1));
            yield return null;
            yield return null;
            Check(
                checks,
                "F1 toggles Main thruster panel",
                MainUIPanels.instance.ThrusterInfoPanel.IsOpen != thrustersOpen);
            MainUIPanels.instance.ThrusterInfoPanel.Close();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftAlt));
            yield return null;
            yield return null;
            Check(
                checks,
                "Alt releases pointer during battle",
                Cursor.visible && Cursor.lockState != CursorLockMode.Locked && CameraController.instance.currentMode == CameraMode.ThirdPersonLock);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            File.WriteAllText("Temp/Siege/workshop-checks.txt", "PASS " + checks.Count + " checks\n" + string.Join("\n", checks));
            Debug.Log("Siege Main workshop: " + checks.Count + " checks passed.");
        }
        finally
        {
            if (session.IsEditingBlueprint)
            {
                session.Workshop.Close();
            }

            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            previousKeyboard?.MakeCurrent();
            previousMouse?.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            running = false;
            if (File.ReadAllText("Temp/Siege/workshop-checks.txt") == "Running")
            {
                File.WriteAllText("Temp/Siege/workshop-checks.txt", "FAILED\n" + string.Join("\n", checks));
            }
        }
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
