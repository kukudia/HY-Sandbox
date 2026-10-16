// 职责：显式Development Player路线用隔离文件仓验证建筑编辑器快捷键、据点保存、完整模型与预览Shader。
// 入口：Run()；三个文件仓仅在测试期间反射替换，finally恢复，随后只读重载原玩家蓝图。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal static class OutpostEditorPlayerChecks
{
    internal static IEnumerator Run(Action<bool, string> check)
    {
        check(
            InputManager.instance.DeveloperToolsAvailable,
            "Authoring smoke requires an actual Development Player");
        SaveManager saves = SaveManager.instance;
        BuildManager build = BuildManager.instance;
        string originalName = saves.currentSaveName;
        BlockDataList originalData = saves.cachedData;
        var fields = new[]
        {
            "_playerFiles",
            "_enemyFiles",
            "_buildingFiles"
        }.Select(name => typeof(SaveManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)).ToArray();
        object[] stores = fields.Select(field => field.GetValue(saves)).ToArray();
        List<string> players = saves.saves;
        List<string> enemies = saves.enemyBlueprints;
        List<string> buildings = saves.buildingBlueprints;
        string directory = Path.Combine(Application.temporaryCachePath, "CodexOutpostEditor", Guid.NewGuid().ToString("N"));
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        InputSettings.BackgroundBehavior background = InputSystem.settings.backgroundBehavior;
        try
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.EnableDevice(keyboard);
            keyboard.MakeCurrent();
            // Let the loading mask restore input before sending a one-frame shortcut.
            yield return null;
            yield return null;
            check(
                InputManager.instance.keyInput && !build.IsLoadingBlocks,
                "Authoring shortcut waits for the startup input gate");
            // Test-only storage injection keeps every copy and edit outside user blueprint directories.
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].SetValue(saves, new BlueprintFileStore(Path.Combine(directory, i.ToString())));
            }

            saves.saves = new List<string>();
            saves.enemyBlueprints = new List<string>();
            saves.buildingBlueprints = new List<string>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.LeftAlt, Key.F));
            for (int frame = 0; frame < 15 && !build.IsEditingBuildingBlueprint; frame++)
            {
                yield return null;
            }

            Debug.Log($"Authoring input: current={Keyboard.current == keyboard}, enabled={keyboard.enabled}, pressed={keyboard.fKey.isPressed}, gate={InputManager.instance.keyInput}, component={InputManager.instance.enabled}, mode={InputSystem.settings.updateMode}, focused={Application.isFocused}, playRequested={PlayManager.instance.WorldSession.IsPlayRequested}");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            while (build.IsLoadingBlocks)
            {
                yield return null;
            }

            check(build.IsEditingBuildingBlueprint, "Actual Player keyboard shortcut opens building authoring");
            BuildingOutpostEditor editor = Object.FindFirstObjectByType<BuildingOutpostEditor>();
            check(editor != null, "Saved Main exposes the active outpost authoring panel in Player");
            Dropdown template = editor.transform.Find("BuildingTemplate").GetComponent<Dropdown>();
            int index = template.options.FindIndex(option => option.text == "CODEX_E03_Manta Outpost");
            check(index >= 0, "Development Player includes matching Manta base");
            template.value = index;
            editor.transform.Find("CopyTemplate").GetComponent<Button>().onClick.Invoke();
            yield return null;
            while (build.IsLoadingBlocks)
            {
                yield return null;
            }

            check(
                saves.cachedData.IsOutpost && saves.cachedData.outpost.garrison.Count == 1,
                "Player Copy template restores matching base and embedded resident");
            string path = saves.GetBuildingBlueprintPath(build.CurrentBuildingBlueprintName);
            check(
                Path.GetFullPath(path).StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                "Player authoring writes only inside its unique cache directory");
            Transform preview = GameManager.instance.blocksParent.Cast<Transform>().Last(child => child.name == "Outpost deployment preview" && child.gameObject.activeSelf);
            MeshRenderer[] renderers = preview.GetComponentsInChildren<MeshRenderer>();
            check(
                renderers.All(renderer => renderer.sharedMaterials.All(material => material != null && material.shader != null && material.shader.isSupported)),
                "Development Player retains supported resident and outline shaders");
            check(
                preview.GetComponentsInChildren<MeshFilter>().Length > 50 && preview.GetComponentsInChildren<Collider>().Length == 0,
                "Player previews the full resident model without creating collision bodies");
            InputField x = editor.transform.Find("X").GetComponent<InputField>();
            InputField yaw = editor.transform.Find("Yaw").GetComponent<InputField>();
            x.text = "-48";
            yaw.text = "90";
            editor.transform.Find("Pose").GetComponent<Button>().onClick.Invoke();
            BlockDataList saved = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            check(
                saved.outpost.garrison[0].position.x == -48f && saved.outpost.garrison[0].yaw == 90f,
                "Actual Player pose button saves numeric position and rotation atomically");
            check(!File.Exists(path + ".pending"), "Player authoring leaves no partial pending file");
            string name = build.CurrentBuildingBlueprintName;
            build.EnterBuildingBlueprintBuildMode(name);
            yield return null;
            while (build.IsLoadingBlocks)
            {
                yield return null;
            }

            check(
                saves.cachedData.outpost.garrison[0].position.x == -48f,
                "Development Player reload preserves edited defender deployment");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.LeftAlt, Key.F));
            for (int frame = 0; frame < 15 && build.IsEditingBuildingBlueprint; frame++)
            {
                yield return null;
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            while (build.IsLoadingBlocks)
            {
                yield return null;
            }

            check(!build.IsEditingDeveloperBlueprint, "Player shortcut exits authoring into normal build mode");
            check(
                !Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Any(filter => filter.sharedMesh != null && filter.sharedMesh.name == "Defender deployment bounds"),
                "Player mode exit removes active deployment geometry");
        }
        finally
        {
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.RemoveDevice(keyboard);
            build.ExitDeveloperBlueprintBuildMode(false);
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].SetValue(saves, stores[i]);
            }

            saves.saves = players;
            saves.enemyBlueprints = enemies;
            saves.buildingBlueprints = buildings;
            saves.currentSaveName = originalName;
            saves.cachedData = originalData;
        }

        saves.LoadSave(originalName);
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        check(
            GameManager.instance.blocksParent.GetComponentInChildren<Cockpit>() != null,
            "Authoring smoke restores the original read-only player construct");
    }
}
