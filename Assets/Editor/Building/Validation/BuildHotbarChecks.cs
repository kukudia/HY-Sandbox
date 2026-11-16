// 职责：在真实Main界面验证快捷栏输入、拖放、持久化及不干扰文字/战斗的契约。
// 入口：RunLive()；恢复原PlayerPrefs与输入设备，不写玩家蓝图。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildHotbarChecks
{
    private static IEnumerator routine;
    private static double next;
    private static double deadline;
    private static readonly List<string> results = new List<string>();
    private static string previous;
    private static bool hadPreference;
    private static bool background;
    private static Keyboard keyboard;
    private static Keyboard physical;
    private static Mouse mouse;
    private static Mouse physicalMouse;
    private static GameObject clone;
    public static void RunLive()
    {
        if (!EditorApplication.isPlaying || routine != null)
        {
            throw new InvalidOperationException("Open Main in Play Mode before running checks.");
        }

        hadPreference = PlayerPrefs.HasKey(BuildHotbar.PreferenceKey);
        previous = PlayerPrefs.GetString(BuildHotbar.PreferenceKey);
        background = Application.runInBackground;
        Application.runInBackground = true;
        physical = Keyboard.current;
        if (physical != null)
        {
            InputSystem.DisableDevice(physical);
        }

        keyboard = InputSystem.AddDevice<Keyboard>("BuildHotbarChecks");
        physicalMouse = Mouse.current;
        if (physicalMouse != null)
        {
            InputSystem.DisableDevice(physicalMouse);
        }

        mouse = InputSystem.AddDevice<Mouse>("BuildHotbarMouse");
        results.Clear();
        routine = Scenario();
        deadline = EditorApplication.timeSinceStartup + 60;
        next = 0;
        EditorApplication.update += Tick;
    }

    private static IEnumerator Scenario()
    {
        BuildHotbar bar = Object.FindAnyObjectByType<BuildHotbar>();
        BuildManager build = BuildManager.instance;
        while (build != null && build.IsLoadingBlocks)
        {
            yield return null;
        }

        Check(
            bar != null && bar.CanUse && bar.GetComponentsInChildren<BuildHotbarSlot>().Length == 8,
            "saved Main exposes eight usable slots");
        Check(bar.Catalog.Entries.Length > 20, "catalog contains real resource parts");
        Canvas.ForceUpdateCanvases();
        var hits = new List<RaycastResult>();
        var hitPointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(
                null,
                bar.GetComponentsInChildren<BuildHotbarSlot>()[0].transform.position + new Vector3(20, 20, 0))
        };
        EventSystem.current.RaycastAll(hitPointer, hits);
        Check(
            hits.Count > 0 && hits[0].gameObject.GetComponent<BuildHotbarSlot>() != null,
            "saved slot is topmost mouse raycast target");
        RectTransform stats = BlueprintUIPanel.instance.GetComponent<RectTransform>();
        Vector3[] statsCorners = new Vector3[4];
        Vector3[] barCorners = new Vector3[4];
        stats.GetWorldCorners(statsCorners);
        bar.GetComponent<RectTransform>().GetWorldCorners(barCorners);
        Check(statsCorners[0].y > barCorners[1].y, "blueprint statistics stays above hotbar without overlap");
        Check(bar.Assign(0, "Blocks/2x2x1", "glass") && bar.Choose(0), "structural assignment accepted");
        Check(
            build.currentBlockResourcePath == "Blocks/2x2x1" && build.CurrentMaterialId == "glass",
            "selection restores part and material");
        Check(
            !bar.Assign(0, "Blocks/2x2x1", "missing") && bar.GetSlot(0).materialId == "glass",
            "unknown material rejects without replacing slot");
        Check(!bar.Assign(0, "Blocks/missing", "metal"), "unknown resource rejects");
        Check(
            bar.Assign(1, "Blocks/Cockpit", "wood") && string.IsNullOrEmpty(bar.GetSlot(1).materialId),
            "functional module does not inherit structural material");
        EventSystem events = EventSystem.current;
        BuildHotbarSlot[] slots = bar.GetComponentsInChildren<BuildHotbarSlot>();
        BuildPaletteItem item = Object.FindObjectsByType<BuildPaletteItem>(FindObjectsInactive.Include).First(value => value.BlockName == "2x2x2");
        var pointer = new PointerEventData(events)
        {
            pointerDrag = item.gameObject
        };
        ExecuteEvents.Execute(slots[2].gameObject, pointer, ExecuteEvents.dropHandler);
        Check(
            bar.GetSlot(2).resourcePath == "Blocks/2x2x2" && bar.GetSlot(2).materialId == "glass",
            "directory drop records current material");
        pointer.pointerDrag = slots[2].gameObject;
        ExecuteEvents.Execute(slots[3].gameObject, pointer, ExecuteEvents.dropHandler);
        Check(bar.GetSlot(3).resourcePath == "Blocks/2x2x2", "slot drop swaps stored identities");
        pointer.button = PointerEventData.InputButton.Right;
        ExecuteEvents.Execute(slots[2].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Check(string.IsNullOrEmpty(bar.GetSlot(2).resourcePath), "right click clears only chosen slot");
        Vector2 sourcePoint = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)item.transform).TransformPoint(((RectTransform)item.transform).rect.center));
        Vector2 targetPoint = RectTransformUtility.WorldToScreenPoint(
            null,
            ((RectTransform)slots[6].transform).TransformPoint(((RectTransform)slots[6].transform).rect.center));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = sourcePoint });
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = sourcePoint }.WithButton(MouseButton.Left));
        yield return null;
        InputSystem.QueueStateEvent(
            mouse,
            new MouseState { position = Vector2.Lerp(sourcePoint, targetPoint, .5f) }.WithButton(MouseButton.Left));
        yield return null;
        Check(events.currentInputModule != null, "real uGUI input module consumes simulated pointer");
        InputSystem.QueueStateEvent(mouse, new MouseState { position = targetPoint }.WithButton(MouseButton.Left));
        yield return null;
        Image preview = bar.transform.Find("DragPreview").GetComponent<Image>();
        Check(
            preview.gameObject.activeInHierarchy && preview.sprite != null,
            "held pointer displays authored drag preview");
        InputSystem.QueueStateEvent(mouse, new MouseState { position = targetPoint });
        yield return null;
        yield return null;
        Check(
            bar.GetSlot(6).resourcePath == "Blocks/2x2x2",
            "full mouse press move release assigns directory part");
        Check(!preview.gameObject.activeSelf, "release hides drag preview");
        events.SetSelectedGameObject(null);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit2));
        yield return null;
        yield return null;
        Check(build.currentBlockResourcePath == "Blocks/Cockpit", "real digit2 input selects module");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        build.SetCurrentBlockResource("Blocks/2x1x1");
        build.SelectMaterial("stone");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.Digit5));
        yield return null;
        yield return null;
        Check(
            bar.GetSlot(4).resourcePath == "Blocks/2x1x1" && bar.GetSlot(4).materialId == "stone",
            "real Shift+5 pins current selection");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        // An inactive popup field is deselected by uGUI before Update; use a real active field.
        clone = Object.Instantiate(MainUIPanels.instance.inputName.gameObject, bar.transform.parent);
        clone.SetActive(true);
        InputField text = clone.GetComponent<InputField>();
        text.Select();
        text.ActivateInputField();
        yield return null;
        Check(
            events.currentSelectedGameObject == text.gameObject && text.isActiveAndEnabled,
            "active input field has real EventSystem focus");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
        yield return null;
        yield return null;
        Check(build.currentBlockResourcePath == "Blocks/2x1x1", "typing focus blocks numeric hotkeys");
        events.SetSelectedGameObject(null);
        Object.Destroy(clone);
        clone = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        InputManager.instance.keyInput = false;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
        yield return null;
        yield return null;
        Check(build.currentBlockResourcePath == "Blocks/2x1x1", "modal input gate blocks numeric hotkeys");
        InputManager.instance.keyInput = true;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        clone = Object.Instantiate(bar.gameObject, bar.transform.parent);
        BuildHotbar restored = clone.GetComponent<BuildHotbar>();
        restored.enabled = false;
        Check(
            restored.GetSlot(4).resourcePath == "Blocks/2x1x1" && restored.GetSlot(4).materialId == "stone",
            "new view reloads saved stable identities");
        Object.Destroy(clone);
        clone = null;
        yield return null;
        PlayerPrefs.SetString(BuildHotbar.PreferenceKey, "malformed");
        clone = Object.Instantiate(bar.gameObject, bar.transform.parent);
        restored = clone.GetComponent<BuildHotbar>();
        restored.enabled = false;
        Check(
            restored.GetSlot(0).resourcePath == "Blocks/2x2x1",
            "corrupt preference falls back to authored defaults");
        Object.Destroy(clone);
        clone = null;
        yield return null;
        PlayManager.instance.playMode = true;
        Check(
            !bar.Choose(0) && !bar.Assign(0, "Blocks/1x1x1", "metal"),
            "combat rejects hotbar selection and assignment");
        PlayManager.instance.playMode = false;
        Check(bar.Choose(0), "build mode can resume after combat");
        yield return null;
        Dropdown material = Object.FindObjectsByType<BuildMaterialSelector>().First().GetComponent<Dropdown>();
        Check(
            material.options[material.value].text.StartsWith("GLASS", StringComparison.Ordinal),
            "material dropdown follows hotbar material");
        Directory.CreateDirectory("Temp/BuildHotbar");
        ScreenCapture.CaptureScreenshot("Temp/BuildHotbar/hotbar.png");
        yield return null;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next)
        {
            return;
        }

        next = EditorApplication.timeSinceStartup + .12;
        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Hotbar check interrupted or timed out.");
            }

            if (!routine.MoveNext())
            {
                Finish(null);
            }
        }
        catch (Exception error)
        {
            Finish(error.ToString());
        }
    }

    private static void Check(bool passed, string name)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        results.Add(name);
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        routine = null;
        if (clone != null)
        {
            Object.Destroy(clone);
        }

        if (keyboard != null && keyboard.added)
        {
            InputSystem.RemoveDevice(keyboard);
        }

        if (mouse != null && mouse.added)
        {
            InputSystem.RemoveDevice(mouse);
        }

        if (physicalMouse != null && physicalMouse.added)
        {
            InputSystem.EnableDevice(physicalMouse);
            physicalMouse.MakeCurrent();
        }

        if (physical != null && physical.added)
        {
            InputSystem.EnableDevice(physical);
            physical.MakeCurrent();
        }

        Application.runInBackground = background;
        if (hadPreference)
        {
            PlayerPrefs.SetString(BuildHotbar.PreferenceKey, previous);
        }
        else
        {
            PlayerPrefs.DeleteKey(BuildHotbar.PreferenceKey);
        }

        PlayerPrefs.Save();
        Directory.CreateDirectory("Temp/BuildHotbar");
        File.WriteAllText(
            "Temp/BuildHotbar/live.json",
            JsonUtility.ToJson(new Report { passed = error == null, checks = results.ToArray(), error = error }, true));
        if (error != null)
        {
            Debug.LogError(error);
        }
        else
        {
            Debug.Log("Build hotbar live checks passed: " + results.Count);
        }
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string[] checks;
        public string error;
    }
}
