// 职责：为蓝图编辑建立独立预览场景，将战场暂存到不模拟的物理场景，保留组件启用状态和伤势。
// 入口：Open() / Dispose()；恢复原父节点、场景、镜头与活动场景，异步卸载仅限本对象创建的场景。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SiegeWorkshopStage : IDisposable
{
    private readonly List<Placement> battlefield = new List<Placement>();
    private readonly List<Placement> tools = new List<Placement>();
    private Scene original;
    private Scene parked;
    private Scene preview;
    private Camera camera;
    private int cameraMask;
    private readonly List<LayerState> layers = new List<LayerState>();
    private Scene cameraScene;
    public Scene PreviewScene => preview;

    public void Open(Scene scene)
    {
        using var measurement = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.SiegeWorkshop);
        original = scene;
        camera = Camera.main;
        cameraScene = camera.scene;
        cameraMask = camera.cullingMask;
        parked = SceneManager.CreateScene("Siege paused battlefield", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        preview = SceneManager.CreateScene("Siege blueprint workshop");
        // 单位可能位于建筑管理器下面；逐个保存父节点，不移动仍承担会话逻辑的管理器。
        foreach (ControlUnit unit in new List<ControlUnit>(ControlUnit.RuntimeUnits))
        {
            if (unit != null && unit.gameObject.scene == original)
            {
                Move(unit.gameObject, parked, battlefield);
            }
        }

        foreach (GameObject root in original.GetRootGameObjects())
        {
            if (root.name == "Arena Ground" || root.name == "Directional Light" || root.GetComponent<SiegeDeploymentPad>() != null || root.name.StartsWith("Arena Pylon"))
            {
                Move(root, parked, battlefield);
            }
        }

        // Camera.scene 在Player里不保证按场景剔除；显式屏蔽暂停场景的临时层，物理隔离由LocalPhysicsScene承担。
        foreach (Placement item in battlefield)
        {
            foreach (Transform node in item.Target.GetComponentsInChildren<Transform>(true))
            {
                layers.Add(new LayerState { Target = node.gameObject, Layer = node.gameObject.layer });
                node.gameObject.layer = 2;
            }
        }

        camera.cullingMask &= ~(1 << 2);
        BuildManager build = BuildManager.instance;
        if (build.moveAxis != null)
        {
            Move(build.moveAxis, preview, tools);
        }

        if (build.rotateAxis != null)
        {
            Move(build.rotateAxis, preview, tools);
        }

        SceneManager.SetActiveScene(preview);
        camera.scene = preview;
        var light = new GameObject("Workshop light");
        light.AddComponent<Light>().type = LightType.Directional;
        light.GetComponent<Light>().intensity = 1.3f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void Move(GameObject target, Scene scene, List<Placement> records)
    {
        records.Add(new Placement { Target = target, Parent = target.transform.parent, Scene = target.scene, Sibling = target.transform.GetSiblingIndex() });
        target.transform.SetParent(null, true);
        Transfer(target, scene);
    }

    private static void Transfer(GameObject target, Scene scene)
    {
        Durability[] health = target.GetComponentsInChildren<Durability>(true);
        foreach (Durability item in health)
        {
            item.PreserveStateForSceneTransfer = true;
        }

        try
        {
            SceneManager.MoveGameObjectToScene(target, scene);
        }
        finally
        {
            foreach (Durability item in health)
            {
                if (item != null)
                {
                    item.PreserveStateForSceneTransfer = false;
                }
            }
        }
    }

    public void Dispose()
    {
        using var measurement = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.SiegeWorkshop);
        foreach (LayerState item in layers)
        {
            if (item.Target != null)
            {
                item.Target.layer = item.Layer;
            }
        }

        layers.Clear();
        Restore(tools);
        Restore(battlefield);
        if (camera != null)
        {
            camera.scene = cameraScene;
            camera.cullingMask = cameraMask;
        }

        if (original.IsValid() && original.isLoaded)
        {
            SceneManager.SetActiveScene(original);
        }

        if (Application.isPlaying && preview.IsValid() && preview.isLoaded)
        {
            SceneManager.UnloadSceneAsync(preview);
        }

        if (Application.isPlaying && parked.IsValid() && parked.isLoaded)
        {
            SceneManager.UnloadSceneAsync(parked);
        }
    }

    private static void Restore(List<Placement> records)
    {
        foreach (Placement item in records)
        {
            if (item.Target == null || !item.Scene.IsValid() || !item.Scene.isLoaded)
            {
                continue;
            }

            item.Target.transform.SetParent(null, true);
            Transfer(item.Target, item.Scene);
            item.Target.transform.SetParent(item.Parent, true);
            item.Target.transform.SetSiblingIndex(item.Sibling);
        }

        records.Clear();
    }

    private struct LayerState
    {
        public GameObject Target;
        public int Layer;
    }

    private struct Placement
    {
        public GameObject Target;
        public Transform Parent;
        public Scene Scene;
        public int Sibling;
    }
}
