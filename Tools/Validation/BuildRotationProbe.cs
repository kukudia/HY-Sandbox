// 职责：在隔离空场景 Play Mode 验证放置后的奇偶尺寸中心、世界轴旋转、阻挡和 Undo。
// 入口：unity command run_script --file Tools/Validation/BuildRotationProbe.cs --entry BuildRotationProbe.Run。
// 隔离：只使用临时对象/虚拟输入设备，保存回调仅采样内存，不写用户蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class BuildRotationProbe
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Type GeometryType = typeof(Block).Assembly.GetType("BuildPlacementGeometry");
    private static readonly Type ManipulationType = typeof(Block).Assembly.GetType("BuildManipulation");

    public static string Run()
    {
        if (!Application.isPlaying || Object.FindAnyObjectByType<SaveManager>() != null)
        {
            throw new InvalidOperationException("Run in an empty Play scene without user save services.");
        }

        var checks = new List<string>();
        GameObject root = new GameObject("Build rotation probe");
        Keyboard previousKeyboard = Keyboard.current;
        Mouse previousMouse = Mouse.current;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        ActionManager previousActions = ActionManager.instance;
        BuildManager previousBuild = BuildManager.instance;
        VisualEffectsManager previousVfx = VisualEffectsManager.instance;
        try
        {
            ActionManager actions = root.AddComponent<ActionManager>();
            GameObject managerObject = Child(root, "Inactive undo save endpoint");
            managerObject.SetActive(false);
            BuildManager.instance = managerObject.AddComponent<BuildManager>();
            Camera camera = Child(root, "Drag camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.pixelRect = new Rect(0, 0, 800, 600);
            object geometry = Activator.CreateInstance(GeometryType, true);
            GeometryType.GetField("gridSize").SetValue(geometry, 1f);
            GeometryType.GetField("blockLayer").SetValue(geometry, (LayerMask)(1 << 30));
            int saveCount = 0;
            Action<Block> save = block => { saveCount++; };
            var manipulation = Activator.CreateInstance(ManipulationType, new object[]
            {
                geometry, save, new Func<GameObject, string, Vector3, Quaternion, Block>((p, s, v, q) => null)
            });
            Set(manipulation, "mainCamera", camera);

            foreach (string resource in new[] { "Blocks/1x1x1", "Blocks/2x1x1", "Blocks/2x2x1", "Blocks/2x2x2" })
            {
                GameObject prefab = Resources.Load<GameObject>(resource);
                Require(prefab != null, resource + " exists", checks);
                Block definition = prefab.GetComponent<Block>();
                // A unit cube's upper socket. Even-length faces require a half-cell tangent offset.
                object[] placement = { definition, Quaternion.identity, new Vector3(0, 0.5f, 0), Vector3.up, Vector3.zero };
                bool aligned = (bool)GeometryType.GetMethod("TryCalculateConnectorAlignedPosition").Invoke(geometry, placement);
                Require(aligned, resource + " connector placement", checks);
                Vector3 center = (Vector3)placement[4];
                Block block = Object.Instantiate(prefab, center, Quaternion.identity, root.transform).GetComponent<Block>();
                foreach (Collider collider in block.GetComponentsInChildren<Collider>())
                {
                    collider.gameObject.layer = 30;
                }
                Set(manipulation, "selectedBlock", block);
                Require(block.transform.position == center, resource + " creation preserves connector center", checks);

                Quaternion initial = Quaternion.Euler(0, 90, 0);
                block.transform.rotation = initial;
                Set(manipulation, "axisRight", Vector3.right);
                Set(manipulation, "axisUp", Vector3.up);
                Set(manipulation, "axisForward", Vector3.forward);
                actions.Clear();
                for (int turn = 1; turn <= 4; turn++)
                {
                    KeyboardState(keyboard, new KeyboardState());
                    KeyboardState(keyboard, new KeyboardState(Key.W));
                    Invoke(manipulation, "HandleRotation");
                    Quaternion expected = Quaternion.AngleAxis(90 * turn, Vector3.right) * initial;
                    Require(block.transform.position == center && Quaternion.Angle(block.transform.rotation, expected) < 0.02f,
                        resource + " keyboard world-axis turn " + turn, checks);
                }
                Require(actions.GetUndoCount() == 4, resource + " keyboard undo entries", checks);
                actions.Undo();
                Require(block.transform.position == center && Quaternion.Angle(block.transform.rotation,
                    Quaternion.AngleAxis(270, Vector3.right) * initial) < 0.02f, resource + " undo center and orientation", checks);
                actions.Redo();
                Require(block.transform.position == center && Quaternion.Angle(block.transform.rotation, initial) < 0.02f,
                    resource + " redo center and orientation", checks);
                KeyboardState(keyboard, new KeyboardState());

                RotateAxisHandle handle = Child(root, "right drag handle").AddComponent<RotateAxisHandle>();
                Set(manipulation, "activeRotateHandle", handle);
                Set(manipulation, "blockStartRot", initial);
                Set(manipulation, "blockStartRotatePos", center);
                Set(manipulation, "rotateLastRot", initial);
                Set(manipulation, "rotateLastPos", center);
                camera.transform.SetPositionAndRotation(center + Vector3.right * 10,
                    Quaternion.LookRotation(Vector3.left, Vector3.up));
                Vector3 from = camera.transform.right * 2;
                Set(manipulation, "rotateDragStart", center + from);
                actions.Clear();
                int beforeDrag = saveCount;
                foreach (float angle in new[] { 15f, 45f, 90f, 135f, 0f, -45f, 90f })
                {
                    Vector3 pointer = camera.WorldToScreenPoint(center + Quaternion.AngleAxis(angle, Vector3.right) * from);
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer, buttons = 1 });
                    InputSystem.Update();
                    Invoke(manipulation, "HandleRotateAxisDrag");
                    Require(block.transform.position == center && Quaternion.Angle(block.transform.rotation,
                        Quaternion.AngleAxis(angle, Vector3.right) * initial) < 0.02f,
                        resource + " fixed-center drag " + angle, checks);
                }
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                Invoke(manipulation, "HandleRotateAxisDrag");
                Require(saveCount == beforeDrag + 1 && actions.GetUndoCount() == 1,
                    resource + " drag commits once on release", checks);
                actions.Undo();
                Require(block.transform.position == center && Quaternion.Angle(block.transform.rotation, initial) < 0.02f,
                    resource + " drag undo", checks);
                actions.Redo();
                Require(block.transform.position == center && Quaternion.Angle(block.transform.rotation,
                    Quaternion.AngleAxis(90, Vector3.right) * initial) < 0.02f, resource + " drag redo", checks);

                GameObject obstacle = Child(root, "Rotation blocker");
                obstacle.layer = 30;
                obstacle.transform.position = center;
                obstacle.AddComponent<Block>();
                obstacle.AddComponent<BoxCollider>();
                Quaternion beforeBlocked = block.transform.rotation;
                int undoCount = actions.GetUndoCount();
                int savesBeforeBlocked = saveCount;
                KeyboardState(keyboard, new KeyboardState(Key.W));
                Invoke(manipulation, "HandleRotation");
                Require(block.transform.position == center && block.transform.rotation == beforeBlocked &&
                    actions.GetUndoCount() == undoCount && saveCount == savesBeforeBlocked,
                    resource + " blocked rotation changes neither pose nor history", checks);
                KeyboardState(keyboard, new KeyboardState());
                Object.DestroyImmediate(obstacle);
                Object.DestroyImmediate(block.gameObject);
                Object.DestroyImmediate(handle.gameObject);
            }

            // Capture a numeric counterexample for the replaced AABB-min-corner rotation path.
            Block even = Resources.Load<GameObject>("Blocks/2x1x1").GetComponent<Block>();
            Vector3 anchor = new Vector3(0.5f, 1, 0);
            Vector3 oldResult = (Vector3)GeometryType.GetMethod("SnapCenterByMinCorner").Invoke(geometry,
                new object[] { anchor, Quaternion.Euler(0, 15, 0), even });
            Require(Vector3.Distance(anchor, oldResult) > 0.01f, "Old rotation snap reproduces center drift", checks);
            string report = JsonUtility.ToJson(new Report
            {
                unity = Application.unityVersion,
                passed = checks.Count,
                checks = checks.ToArray(),
                oldSnapCenterDrift = oldResult - anchor
            }, true);
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/build-rotation-probe.json", report);
            return report;
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (previousVfx == null && VisualEffectsManager.instance != null)
            {
                Object.DestroyImmediate(VisualEffectsManager.instance.gameObject);
            }
            ActionManager.instance = previousActions;
            BuildManager.instance = previousBuild;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            previousKeyboard?.MakeCurrent();
            previousMouse?.MakeCurrent();
        }
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child;
    }

    private static void KeyboardState(Keyboard keyboard, KeyboardState state)
    {
        InputSystem.QueueStateEvent(keyboard, state);
        InputSystem.Update();
    }

    private static void Set(object instance, string field, object value)
    {
        ManipulationType.GetField(field, Private).SetValue(instance, value);
    }

    private static void Invoke(object instance, string method)
    {
        ManipulationType.GetMethod(method, Private).Invoke(instance, null);
    }

    private static void Require(bool condition, string label, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
        checks.Add(label);
    }

    [Serializable]
    private sealed class Report
    {
        public string unity;
        public int passed;
        public string[] checks;
        public Vector3 oldSnapCenterDrift;
    }
}
