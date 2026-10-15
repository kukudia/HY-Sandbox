// 职责：隔离验证默认关闭镜头碰撞后的墙体行为和图标空闲/淡出/恢复分支。
// 入口：Run()；Play Mode 调用真实方法，不替代视觉手感验收。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PresentationPerformanceChecks
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    public static string[] Run()
    {
        GameObject root = new GameObject("Presentation isolated checks");
        CameraController controller = Object.FindFirstObjectByType<CameraController>();
        FieldInfo playerField = typeof(CameraController).GetField("playerBody", BindingFlags.Public | PrivateInstance);
        object previousPlayer = playerField.GetValue(controller);
        List<string> checks = new List<string>();
        try
        {
            Vector3 pivot = Vector3.one * 5000;
            GameObject player = new GameObject("Camera owner");
            player.transform.SetParent(root.transform);
            player.transform.position = pivot + Vector3.back * 1.5f;
            player.AddComponent<BoxCollider>();
            playerField.SetValue(controller, player.transform);
            GameObject wall = new GameObject("Camera wall");
            wall.transform.SetParent(root.transform);
            wall.transform.position = pivot + Vector3.back * 3;
            wall.AddComponent<BoxCollider>().size = new Vector3(8, 8, 0.5f);
            Physics.SyncTransforms();
            MethodInfo resolve = typeof(CameraController).GetMethod("ResolveThirdPersonCollision", PrivateInstance);
            Vector3 desired = pivot + Vector3.back * 6;
            Vector3 safe = (Vector3)resolve.Invoke(controller, new object[] { pivot, desired });
            Check(safe == desired, "Disabled camera collision preserves desired distance through walls", checks);
            wall.SetActive(false);
            Physics.SyncTransforms();
            Check(
                (Vector3)resolve.Invoke(controller, new object[] { pivot, desired }) == desired,
                "Unobstructed desired distance restored",
                checks);
            GameObject iconObject = new GameObject("Icon owner");
            iconObject.transform.SetParent(root.transform);
            StatusIcon icon = iconObject.AddComponent<StatusIcon>();
            icon.enabled = false;
            GameObject iconRoot = new GameObject("Icon root", typeof(RectTransform), typeof(CanvasGroup));
            iconRoot.transform.SetParent(root.transform);
            Set(icon, "_iconRoot", iconRoot.GetComponent<RectTransform>());
            Set(icon, "_iconGroup", iconRoot.GetComponent<CanvasGroup>());
            MethodInfo update = typeof(StatusIcon).GetMethod("UpdateIconScreenPosition", PrivateInstance);
            Set(icon, "_hasVisibleIcons", false);
            Set(icon, "_visibility", 0f);
            update.Invoke(icon, new object[] { DebugManager.instance });
            Check(!iconRoot.activeSelf, "Idle icon root deactivated", checks);
            Set(icon, "_visibility", 0.5f);
            update.Invoke(icon, new object[] { DebugManager.instance });
            Check(
                (float)typeof(StatusIcon).GetField("_visibility", PrivateInstance).GetValue(icon) < 0.5f,
                "Previously visible icon still fades",
                checks);
            Camera camera = PlayManager.instance.mainCamera;
            iconObject.transform.position = camera.transform.position + camera.transform.forward * 3;
            Set(icon, "_hasVisibleIcons", true);
            Set(icon, "_visibility", 0f);
            Set(icon, "_isOutsideViewport", false);
            update.Invoke(icon, new object[] { DebugManager.instance });
            Check(iconRoot.activeSelf, "New status resumes projection and visibility", checks);
            File.WriteAllText("Docs/Validation/2026-10-14-presentation-checks.txt", string.Join("\n", checks));
            return checks.ToArray();
        }
        finally
        {
            playerField.SetValue(controller, previousPlayer);
            Object.DestroyImmediate(root);
        }
    }

    private static void Set(StatusIcon icon, string name, object value)
    {
        typeof(StatusIcon).GetField(name, PrivateInstance).SetValue(icon, value);
    }

    private static void Check(bool condition, string name, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}
