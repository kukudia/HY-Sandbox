// 职责：验证真实菜单模板在首次启用的同一帧可接收点击。
// 入口：Tools/HY-Sandbox/UI/Verify dropdown lifecycle。
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class DropdownLifecycleProbe
{
    [MenuItem("Tools/HY-Sandbox/UI/Verify dropdown lifecycle")]
    public static void Verify()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Start Play Mode first.");
        }

        Object.FindAnyObjectByType<MainMenuController>().StartCoroutine(Run());
    }

    private static IEnumerator Run()
    {
        var root = new GameObject("Dropdown regression canvas", typeof(Canvas));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        try
        {
            var click = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            };
            GameObject legacy = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            legacy.transform.SetParent(root.transform, false);
            bool reproduced = false;
            try
            {
                legacy.GetComponent<Dropdown>().OnPointerClick(click);
            }
            catch (NullReferenceException error) when (error.StackTrace.Contains("AlphaFadeList"))
            {
                reproduced = true;
            }

            Object.Destroy(legacy);
            Require(reproduced, "Legacy Dropdown must reproduce the before-Start exception.");
            int count = 0;
            foreach (var path in new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Build.unity",
                "Assets/Scenes/SiegeGameplay.unity"
            }

            )
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool loaded = scene.isLoaded;
                if (!loaded)
                {
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
                    scene = SceneManager.GetSceneByPath(path);
                }

                try
                {
                    var controls = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<ReadyDropdown>(true)).Where(item => item.gameObject.scene == scene).ToArray();
                    Require(controls.Length >= 2, $"Expected at least two dropdowns in {path}, found {controls.Length}.");
                    foreach (var control in controls)
                    {
                        VerifyControl(control, root.transform, click);
                        count++;
                    }
                }
                finally
                {
                    if (!loaded)
                    {
                        SceneManager.UnloadSceneAsync(scene);
                    }
                }
            }

            foreach (string path in new[]
            {
                "Assets/Art/CombatHud/SharedCombatHud.prefab",
                "Assets/Resources/BuildingEditor/EditorPanel.prefab"
            }

            )
            {
                var controls = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<ReadyDropdown>(true);
                Require(controls.Length > 0, "Missing prefab dropdowns " + path);
                foreach (var control in controls)
                {
                    VerifyControl(control, root.transform, click);
                    count++;
                }
            }

            Debug.Log($"Dropdown lifecycle verified: legacy NullReferenceException reproduced, {count} authored controls opened, selected, closed and reopened.");
        }
        finally
        {
            Object.Destroy(root);
        }
    }

    private static void VerifyControl(ReadyDropdown source, Transform root, PointerEventData click)
    {
        var clone = Object.Instantiate(source.gameObject, root, false);
        try
        {
            var dropdown = clone.GetComponent<ReadyDropdown>();
            dropdown.enabled = true;
            dropdown.interactable = true;
            clone.SetActive(true);
            dropdown.onValueChanged = new Dropdown.DropdownEvent();
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string> { "Initial", "Selected" });
            int selected = 0;
            dropdown.onValueChanged.AddListener(value => selected = value);
            Require(ExecuteEvents.Execute(clone, click, ExecuteEvents.pointerClickHandler), "Click was not delivered.");
            var list = clone.transform.Find("Dropdown List");
            Require(
                list != null && !dropdown.template.gameObject.activeSelf,
                "List did not open or source template stayed enabled.");
            list.GetComponentsInChildren<Toggle>()[1].isOn = true;
            Require(dropdown.value == 1 && selected == 1, "Selection callback did not fire.");
            clone.SetActive(false);
            clone.SetActive(true);
            ExecuteEvents.Execute(clone, click, ExecuteEvents.pointerClickHandler);
            Require(
                clone.GetComponentsInChildren<Canvas>().Any(item => item.name == "Dropdown List"),
                "List did not reopen.");
        }
        finally
        {
            clone.SetActive(false);
            Object.Destroy(clone);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
