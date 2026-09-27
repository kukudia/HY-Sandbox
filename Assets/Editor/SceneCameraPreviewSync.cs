using UnityEditor;
using UnityEngine;

/// <summary>Keeps the Scene view at the active scene's MainCamera while the menu toggle is enabled.</summary>
[InitializeOnLoad]
public static class SceneCameraPreviewSync
{
    private const string MenuPath = "Tools/HY-Sandbox/Scene View/Follow Main Camera";
    private static readonly string PreferenceKey = "HY-Sandbox.SceneViewFollowMainCamera." + Application.dataPath;
    private static bool _enabled;

    static SceneCameraPreviewSync()
    {
        _enabled = EditorPrefs.GetBool(PreferenceKey, false);
        Menu.SetChecked(MenuPath, _enabled);
        EditorApplication.update += Update;
    }

    [MenuItem(MenuPath)]
    public static void Toggle()
    {
        _enabled = !_enabled;
        EditorPrefs.SetBool(PreferenceKey, _enabled);
        Menu.SetChecked(MenuPath, _enabled);
        if (_enabled) SyncNow();
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateToggle()
    {
        Menu.SetChecked(MenuPath, _enabled);
        return true;
    }

    private static void Update()
    {
        if (_enabled) SyncNow();
    }

    public static bool SyncNow()
    {
        SceneView view = SceneView.lastActiveSceneView;
        Camera main = Camera.main;
        if (view == null || main == null || !main.gameObject.scene.IsValid()) return false;

        bool projectionChanged = false;
        if (view.in2DMode)
        {
            view.in2DMode = false;
            projectionChanged = true;
        }
        if (view.orthographic != main.orthographic)
        {
            view.orthographic = main.orthographic;
            projectionChanged = true;
        }
        SceneView.CameraSettings settings = view.cameraSettings;
        if (!main.orthographic && !Mathf.Approximately(settings.fieldOfView, main.fieldOfView))
        {
            settings.fieldOfView = main.fieldOfView;
            view.cameraSettings = settings;
            projectionChanged = true;
        }

        if (main.orthographic && !Mathf.Approximately(view.size, main.orthographicSize))
        {
            view.size = main.orthographicSize;
            projectionChanged = true;
        }

        // SceneView stores an orbit pivot, so offset it by its own camera distance
        // to place the preview camera exactly at MainCamera's transform.
        Quaternion rotation = main.transform.rotation;
        Vector3 pivot = main.transform.position + rotation * Vector3.forward * view.cameraDistance;
        if (Vector3.SqrMagnitude(view.pivot - pivot) < 0.000001f
            && Quaternion.Angle(view.rotation, rotation) < 0.01f)
        {
            if (projectionChanged) view.Repaint();
            return true;
        }

        view.LookAtDirect(pivot, rotation, view.size);
        view.Repaint();
        return true;
    }
}
