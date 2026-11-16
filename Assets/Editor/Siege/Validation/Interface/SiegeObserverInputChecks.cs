// 职责：通过真实InputSystem设备和Main的EventSystem验证观察相机键鼠、UI/菜单阻挡与光标释放。
// 入口：RunLive()；异步报告Temp/SiegeCamera/input.json，恢复设备/输入/相机/菜单显示，不写玩家蓝图。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeObserverInputChecks
{
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public string error;
        public bool focused;
    }

    private static readonly Report report = new Report();
    private static IEnumerator routine;
    private static double next;
    private static double deadline;
    private static Keyboard physicalKeyboard;
    private static Mouse physicalMouse;
    private static bool keyboardEnabled;
    private static bool mouseEnabled;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static bool background;
    private static bool inputEnabled;
    private static GameObject previousSelection;
    private static CameraController camera;
    private static CameraMode previousMode;
    private static Vector3 previousPosition;
    private static Quaternion previousRotation;
    private static CursorLockMode previousLock;
    private static bool previousVisible;
    private static GameObject root;
    private static SiegeObserverCamera observer;
    private static Transform overview;
    private static GameObject menuPresentation;
    private static bool presentationVisible;
    private static GameObject blocker;
    private static InputField textInput;
    private static Vector2 fieldPoint;
    private static Vector2 uiPoint;
    private static readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    private static readonly List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();
    public static void RunLive()
    {
        if (!EditorApplication.isPlaying || routine != null || CameraController.instance == null || EventSystem.current == null)
        {
            throw new InvalidOperationException("Requires Main Play Mode with camera/EventSystem and no active input check.");
        }

        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        camera = CameraController.instance;
        previousMode = camera.currentMode;
        previousPosition = camera.transform.position;
        previousRotation = camera.transform.rotation;
        previousLock = Cursor.lockState;
        previousVisible = Cursor.visible;
        previousSelection = EventSystem.current.currentSelectedGameObject;
        inputEnabled = InputManager.instance != null && InputManager.instance.enabled;
        if (InputManager.instance != null)
        {
            InputManager.instance.enabled = false;
        }

        background = Application.runInBackground;
        Application.runInBackground = true;
        physicalKeyboard = Keyboard.current;
        physicalMouse = Mouse.current;
        keyboardEnabled = physicalKeyboard != null && physicalKeyboard.enabled;
        mouseEnabled = physicalMouse != null && physicalMouse.enabled;
        if (keyboardEnabled)
        {
            InputSystem.DisableDevice(physicalKeyboard);
        }

        if (mouseEnabled)
        {
            InputSystem.DisableDevice(physicalMouse);
        }

        keyboard = InputSystem.AddDevice<Keyboard>("Siege observer check keyboard");
        mouse = InputSystem.AddDevice<Mouse>("Siege observer check mouse");
        report.checks.Clear();
        report.status = "running";
        report.error = null;
        routine = Scenario();
        next = 0;
        deadline = EditorApplication.timeSinceStartup + 45;
        EditorApplication.update += Tick;
        Save();
    }

    private static IEnumerator Scenario()
    {
        // Main's build catalog is intentionally absent during real Siege. Hide its
        // canvas/raycasters during this Main-hosted fixture, preserving exact enable states.
        hiddenCanvases.Clear();
        disabledRaycasters.Clear();
        foreach (Canvas existing in Object.FindObjectsByType<Canvas>())
        {
            if (existing.enabled)
            {
                hiddenCanvases.Add(existing);
                existing.enabled = false;
            }
        }

        foreach (GraphicRaycaster existing in Object.FindObjectsByType<GraphicRaycaster>())
        {
            if (existing.enabled)
            {
                disabledRaycasters.Add(existing);
                existing.enabled = false;
            }
        }

        root = new GameObject("Owned Siege observer input fixture");
        overview = new GameObject("Owned overview").transform;
        overview.SetParent(root.transform, false);
        overview.SetPositionAndRotation(new Vector3(20f, 50f, -70f), Quaternion.Euler(25f, 15f, 0f));
        MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
        if (menu == null)
        {
            GameObject service = new GameObject("Owned disabled real menu consumer");
            service.transform.SetParent(root.transform, false);
            menu = service.AddComponent<MainMenuController>();
            menu.enabled = false;
            menuPresentation = new GameObject("Owned menu presentation");
            menuPresentation.transform.SetParent(service.transform, false);
            typeof(MainMenuController).GetField("presentation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, menuPresentation);
            menuPresentation.SetActive(false);
        }
        else
        {
            menuPresentation = (GameObject)typeof(MainMenuController).GetField("presentation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
        }

        presentationVisible = menuPresentation.activeSelf;
        menuPresentation.SetActive(false);
        GameObject canvasRoot = new GameObject("Owned camera input UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(root.transform, false);
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        blocker = new GameObject("Owned pointer blocker", typeof(RectTransform), typeof(Image));
        blocker.transform.SetParent(canvasRoot.transform, false);
        RectTransform area = blocker.GetComponent<RectTransform>();
        area.anchorMin = area.anchorMax = area.pivot = new Vector2(.5f, .5f);
        area.sizeDelta = new Vector2(200f, 160f);
        blocker.GetComponent<Image>().color = new Color(.1f, .1f, .1f, .5f);
        GameObject inputRoot = new GameObject("Owned text focus", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputRoot.transform.SetParent(canvasRoot.transform, false);
        inputRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(-300f, -200f);
        textInput = inputRoot.GetComponent<InputField>();
        var textObject = new GameObject("Owned input text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(inputRoot.transform, false);
        Text inputText = textObject.GetComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.raycastTarget = false;
        textInput.textComponent = inputText;
        observer = root.AddComponent<SiegeObserverCamera>();
        observer.Enter(camera, overview);
        observer.ResetOverview();
        EventSystem.current.SetSelectedGameObject(null);
        Canvas.ForceUpdateCanvases();
        uiPoint = RectTransformUtility.WorldToScreenPoint(null, area.position);
        fieldPoint = new Vector2(Screen.width * .82f, Screen.height * .60f);
        Queue(false, uiPoint);
        yield return null;
        yield return null;
        Require(Application.isFocused, "real Game View has focus for device input");
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = uiPoint }, hits);
        Require(hits.Count > 0 && hits[0].gameObject == blocker, "real UI graphic is the top pointer hit");
        Vector3 before = camera.transform.position;
        Queue(true, uiPoint, new[] { Key.W });
        yield return null;
        yield return null;
        Require(
            !observer.Looking && Vector3.Distance(before, camera.transform.position) < .01f,
            "right-button press starting over UI cannot capture or move camera");
        blocker.SetActive(false);
        Canvas.ForceUpdateCanvases();
        fieldPoint = FindOpenPoint();
        Require(!HasGraphicHit(fieldPoint), "free-flight press position has no actual UI graphic hit");
        Queue(false, fieldPoint);
        yield return null;
        yield return null;
        Queue(true, fieldPoint);
        yield return null;
        Require(
            observer.Looking && Cursor.lockState == CursorLockMode.Locked && !Cursor.visible,
            "right-button press over playfield captures real pointer");
        Quaternion rotation = camera.transform.rotation;
        Queue(true, fieldPoint, null, new Vector2(36f, -18f));
        yield return null;
        Require(Quaternion.Angle(rotation, camera.transform.rotation) > 1f, "actual mouse delta rotates the view");
        Queue(true, fieldPoint);
        yield return null;
        foreach (Key key in new[]
        {
            Key.W,
            Key.S,
            Key.A,
            Key.D,
            Key.Q,
            Key.E
        }

        )
        {
            before = camera.transform.position;
            Vector3 expected = key == Key.W ? camera.transform.forward : key == Key.S ? -camera.transform.forward : key == Key.A ? -camera.transform.right : key == Key.D ? camera.transform.right : key == Key.Q ? Vector3.down : Vector3.up;
            Queue(true, fieldPoint, new[] { key });
            yield return null;
            Vector3 travelled = camera.transform.position - before;
            Require(
                travelled.magnitude > .01f && Vector3.Dot(travelled.normalized, expected) > .98f,
                "real " + key + " input produces the expected world displacement");
        }

        Queue(true, fieldPoint);
        yield return null;
        before = camera.transform.position;
        float start = Time.unscaledTime;
        Queue(true, fieldPoint, new[] { Key.W });
        yield return null;
        float normal = Vector3.Distance(before, camera.transform.position) / Mathf.Max(.001f, Time.unscaledTime - start);
        before = camera.transform.position;
        start = Time.unscaledTime;
        Queue(true, fieldPoint, new[] { Key.W, Key.LeftShift });
        yield return null;
        float fast = Vector3.Distance(before, camera.transform.position) / Mathf.Max(.001f, Time.unscaledTime - start);
        Require(
            fast > normal * 1.7f && fast < normal * 4.5f,
            "Shift increases measured travel speed rather than changing a UI value only");
        float speed = observer.CurrentSpeed;
        Queue(true, fieldPoint, null, default, new Vector2(0f, 120f));
        yield return null;
        Require(observer.CurrentSpeed > speed, "real wheel input adjusts free-flight speed");
        Queue(false, fieldPoint);
        yield return null;
        Require(
            !observer.Looking && Cursor.lockState != CursorLockMode.Locked,
            "right-button release restores pointer for UI");
        Queue(false, fieldPoint, new[] { Key.F });
        yield return null;
        Require(
            Vector3.Distance(camera.transform.position, overview.position) < .01f && Quaternion.Angle(camera.transform.rotation, overview.rotation) < .01f,
            "real F key restores authored overview pose");
        Queue(false, fieldPoint);
        yield return null;
        Queue(true, fieldPoint);
        yield return null;
        Require(observer.Looking, "camera can recapture after overview reset");
        menuPresentation.SetActive(true);
        before = camera.transform.position;
        Queue(true, fieldPoint, new[] { Key.W });
        yield return null;
        Require(
            !observer.Looking && Vector3.Distance(camera.transform.position, before) < .01f,
            "visible real menu consumer releases capture and blocks held movement");
        menuPresentation.SetActive(false);
        Queue(false, fieldPoint);
        yield return null;
        Queue(true, fieldPoint);
        yield return null;
        Require(observer.Looking, "observer captures before keyboard focus interrupts observation");
        EventSystem.current.SetSelectedGameObject(inputRoot);
        textInput.ActivateInputField();
        yield return null;
        Require(
            EventSystem.current.currentSelectedGameObject == inputRoot && !observer.Looking,
            "actual enabled input field owns focus and releases existing camera capture");
        // A new pointer press outside a field legitimately ends text selection.
        // Keep RMB held and test keyboard input while the field actually owns focus.
        before = camera.transform.position;
        Queue(true, fieldPoint, new[] { Key.W, Key.F });
        yield return null;
        Require(
            EventSystem.current.currentSelectedGameObject == inputRoot && !observer.Looking && Vector3.Distance(camera.transform.position, before) < .01f,
            "held pointer and keyboard input preserve text focus without observer movement or recapture");
        EventSystem.current.SetSelectedGameObject(null);
        Queue(false, fieldPoint);
        yield return null;
        Queue(true, fieldPoint);
        yield return null;
        Queue(true, fieldPoint, new[] { Key.Escape });
        yield return null;
        Require(!observer.Looking, "Escape releases a captured camera");
    }

    private static bool HasGraphicHit(Vector2 point)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        return hits.Exists(hit => hit.module is GraphicRaycaster);
    }

    private static Vector2 FindOpenPoint()
    {
        foreach (float x in new[]
        {
            .82f,
            .95f,
            .65f,
            .35f
        }

        )
        {
            foreach (float y in new[]
            {
                .60f,
                .80f,
                .25f,
                .95f
            }

            )
            {
                Vector2 point = new Vector2(Screen.width * x, Screen.height * y);
                if (!HasGraphicHit(point))
                {
                    return point;
                }
            }
        }

        throw new InvalidOperationException("No real UI-free point exists for observer input fixture.");
    }

    private static void Queue(bool right, Vector2 position, Key[] keys = null, Vector2 delta = default, Vector2 scroll = default)
    {
        MouseState state = new MouseState
        {
            position = position,
            delta = delta,
            scroll = scroll
        };
        if (right)
        {
            state = state.WithButton(MouseButton.Right);
        }

        InputSystem.QueueStateEvent(mouse, state);
        InputSystem.QueueStateEvent(keyboard, keys == null ? new KeyboardState() : new KeyboardState(keys));
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
                throw new TimeoutException("Camera input checks interrupted or timed out.");
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

    private static void Require(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add(name);
        Save();
    }

    private static void Save()
    {
        Directory.CreateDirectory("Temp/SiegeCamera");
        report.focused = Application.isFocused;
        File.WriteAllText("Temp/SiegeCamera/input.json", JsonUtility.ToJson(report, true));
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        routine = null;
        observer?.Exit();
        if (menuPresentation != null)
        {
            menuPresentation.SetActive(presentationVisible);
        }

        if (camera != null)
        {
            camera.currentMode = previousMode;
            camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(previousSelection);
        }

        if (root != null)
        {
            root.SetActive(false);
            Object.Destroy(root);
        }

        foreach (Canvas existing in hiddenCanvases)
        {
            if (existing != null)
            {
                existing.enabled = true;
            }
        }

        foreach (GraphicRaycaster existing in disabledRaycasters)
        {
            if (existing != null)
            {
                existing.enabled = true;
            }
        }

        hiddenCanvases.Clear();
        disabledRaycasters.Clear();
        if (keyboard != null && keyboard.added)
        {
            InputSystem.RemoveDevice(keyboard);
        }

        if (mouse != null && mouse.added)
        {
            InputSystem.RemoveDevice(mouse);
        }

        if (physicalKeyboard != null && physicalKeyboard.added)
        {
            if (keyboardEnabled)
            {
                InputSystem.EnableDevice(physicalKeyboard);
            }

            physicalKeyboard.MakeCurrent();
        }

        if (physicalMouse != null && physicalMouse.added)
        {
            if (mouseEnabled)
            {
                InputSystem.EnableDevice(physicalMouse);
            }

            physicalMouse.MakeCurrent();
        }

        if (InputManager.instance != null)
        {
            InputManager.instance.enabled = inputEnabled;
        }

        Application.runInBackground = background;
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
        report.status = error == null ? "passed" : "failed";
        report.error = error;
        Save();
        Debug.Log("Siege observer input checks " + report.status + ": " + report.checks.Count);
    }
}
