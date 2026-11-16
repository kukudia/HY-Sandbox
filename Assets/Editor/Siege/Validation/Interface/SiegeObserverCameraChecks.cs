// 职责：验证真实共享相机的自由观察所有权、总览复位、重入及禁用清理，不保存场景。
// 入口：Run()；Main Play中运行，完成后恢复原相机模式、位置与旋转。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeObserverCameraChecks
{
    public static string Run()
    {
        if (!Application.isPlaying || CameraController.instance == null)
        {
            throw new InvalidOperationException("Requires Main Play Mode and its real shared camera.");
        }

        var checks = new List<string>();
        CameraController camera = CameraController.instance;
        CameraMode originalMode = camera.currentMode;
        Vector3 originalPosition = camera.transform.position;
        Quaternion originalRotation = camera.transform.rotation;
        GameObject root = new GameObject("Owned observer lifecycle check");
        root.SetActive(false);
        SiegeObserverCamera observer = root.AddComponent<SiegeObserverCamera>();
        Transform overview = new GameObject("Owned overview").transform;
        overview.SetParent(root.transform, false);
        overview.SetPositionAndRotation(new Vector3(10f, 50f, -70f), Quaternion.Euler(35f, 15f, 0f));
        try
        {
            root.SetActive(true);
            observer.Enter(camera, overview);
            Require(
                observer.IsActive && camera.currentMode == CameraMode.Lock,
                "observer owns transform while legacy camera input stays locked",
                checks);
            camera.transform.SetPositionAndRotation(new Vector3(85f, 18f, 42f), Quaternion.Euler(20f, 120f, 0f));
            observer.ResetOverview();
            Require(
                SamePose(camera.transform, overview.position, overview.rotation),
                "overview reset uses saved scene pose after free movement",
                checks);
            observer.Exit();
            Require(
                !observer.IsActive && camera.currentMode == originalMode && SamePose(camera.transform, originalPosition, originalRotation),
                "exit restores prior mode and pose",
                checks);
            observer.Enter(camera, overview);
            observer.Enter(camera, overview);
            camera.transform.position += Vector3.one * 25f;
            observer.Exit();
            Require(
                SamePose(camera.transform, originalPosition, originalRotation) && camera.currentMode == originalMode,
                "repeated Enter does not replace original recovery pose",
                checks);
            observer.Enter(camera, overview);
            camera.transform.position += Vector3.one * 13f;
            root.SetActive(false);
            Require(
                !observer.IsActive && camera.currentMode == originalMode && SamePose(camera.transform, originalPosition, originalRotation),
                "disabled scene controller releases camera without leaving hidden ownership",
                checks);
            observer.Exit();
            Require(
                SamePose(camera.transform, originalPosition, originalRotation),
                "repeated cleanup is idempotent",
                checks);
        }
        finally
        {
            observer.Exit();
            camera.currentMode = originalMode;
            camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
            root.SetActive(false);
            Object.Destroy(root);
        }

        string result = "PASS\n" + string.Join("\n", checks);
        Directory.CreateDirectory("Temp/SiegeCamera");
        File.WriteAllText("Temp/SiegeCamera/lifecycle.txt", result);
        return result;
    }

    private static bool SamePose(Transform view, Vector3 position, Quaternion rotation)
    {
        return Vector3.Distance(view.position, position) < .001f && Quaternion.Angle(view.rotation, rotation) < .01f;
    }

    private static void Require(bool value, string name, List<string> checks)
    {
        if (!value)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}
