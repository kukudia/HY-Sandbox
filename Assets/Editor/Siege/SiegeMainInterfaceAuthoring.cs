// 职责：将 Main 的真实建造UI、管理器与轴工具迁移至Siege，保留Main源场景与战场布局。
// 入口：Install()；显式菜单保存资产，可在Inspector继续编辑。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeMainInterfaceAuthoring
{
    [MenuItem("HY-Sandbox/Siege/Install Main Interface And Workshop")]
    public static void Install()
    {
        Scene siege = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || siege.path != SiegeSceneAuthoring.ScenePath || siege.isDirty)
        {
            throw new InvalidOperationException("Open the saved Siege scene in Edit Mode.");
        }

        if (Object.FindFirstObjectByType<SiegeWorkshop>() != null)
        {
            Debug.Log("Main interface already installed; edit the saved components directly.");
            return;
        }

        GameObject oldManagers = siege.GetRootGameObjects().Single(root => root.name == "Managers");
        GameObject oldCamera = siege.GetRootGameObjects().Single(root => root.name == "Main Camera");
        Scene main = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
        GameObject clone = null;
        try
        {
            SceneManager.SetActiveScene(main);
            var source = new GameObject("Siege Main Interface");
            foreach (string name in new[]
            {
                "Managers",
                "MainCanvas",
                "Others",
                "Main Camera"
            }

            )
            {
                main.GetRootGameObjects().Single(root => root.name == name).transform.SetParent(source.transform, true);
            }

            // Clone together so serialized references across UI, camera and managers remap to this copy.
            clone = Object.Instantiate(source);
            clone.name = "Main Interface";
            SceneManager.MoveGameObjectToScene(clone, siege);
        }
        finally
        {
            EditorSceneManager.CloseScene(main, true);
            SceneManager.SetActiveScene(siege);
        }

        Object.DestroyImmediate(oldManagers);
        Object.DestroyImmediate(oldCamera);
        GameManager game = clone.GetComponentInChildren<GameManager>(true);
        game.enabled = false;
        game.blocksParent = null;
        BuildManager build = clone.GetComponentInChildren<BuildManager>(true);
        build.enabled = false;
        clone.GetComponentInChildren<InputManager>(true).enabled = true;
        PlayManager play = clone.GetComponentInChildren<PlayManager>(true);
        var playData = new SerializedObject(play);
        playData.FindProperty("worldSession").objectReferenceValue = null;
        playData.FindProperty("siegeSession").objectReferenceValue = Object.FindFirstObjectByType<SiegeSession>();
        playData.ApplyModifiedPropertiesWithoutUndo();
        play.blocksParent = null;
        play.groupCleanupDistance = 500f;
        EnemySpawner spawner = clone.GetComponentInChildren<EnemySpawner>(true);
        if (spawner != null)
        {
            Object.DestroyImmediate(spawner.gameObject);
        }

        MeteorShower meteor = clone.GetComponentInChildren<MeteorShower>(true);
        if (meteor != null)
        {
            Object.DestroyImmediate(meteor.gameObject);
        }

        Camera camera = clone.GetComponentInChildren<Camera>(true);
        camera.transform.SetPositionAndRotation(new Vector3(0f, 35f, -65f), Quaternion.Euler(25f, 0f, 0f));
        camera.farClipPlane = 600f;
        CameraController controller = camera.GetComponent<CameraController>();
        controller.thirdPersonOffset = new Vector3(0f, 8f, -38f);
        controller.minZoom = -15f;
        controller.maxZoom = -90f;
        controller.playerBody = null;
        MainUIPanels panels = clone.GetComponentInChildren<MainUIPanels>(true);
        panels.buildPanel.SetActive(true);
        panels.playPanel.SetActive(false);
        panels.createPanel.SetActive(false);
        panels.deletePanel.SetActive(false);
        panels.deathPanel.SetActive(false);
        SiegeHud hud = Object.FindFirstObjectByType<SiegeHud>();
        CanvasGroup group = hud.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = hud.gameObject.AddComponent<CanvasGroup>();
        }

        MainUIButtons buttons = clone.GetComponentInChildren<MainUIButtons>(true);
        // The strategy entry uses Main's real button styling; its return action is wired by the workshop.
        Button entry = Object.Instantiate(buttons.playButton, hud.transform);
        entry.name = "Open Blueprint Workshop";
        entry.onClick = new Button.ButtonClickedEvent();
        entry.GetComponentInChildren<Text>().text = "BUILD / BLUEPRINTS  [B]";
        RectTransform rect = entry.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 24f);
        rect.sizeDelta = new Vector2(340f, 54f);
        SiegeWorkshop workshop = clone.AddComponent<SiegeWorkshop>();
        var data = new SerializedObject(workshop);
        data.FindProperty("session").objectReferenceValue = Object.FindFirstObjectByType<SiegeSession>();
        data.FindProperty("panels").objectReferenceValue = panels;
        data.FindProperty("strategyHud").objectReferenceValue = group;
        data.FindProperty("buildButton").objectReferenceValue = entry;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(siege);
        EditorSceneManager.SaveScene(siege);
        AssetDatabase.SaveAssets();
        FitLayout();
        Debug.Log("Siege now uses Main's saved UI, build tools and input, with an isolated between-wave workshop.");
    }

    [MenuItem("HY-Sandbox/Siege/Fit Main Interface Layout")]
    public static void FitLayout()
    {
        SiegeInterfaceAuthoring.Apply();
    }
}
