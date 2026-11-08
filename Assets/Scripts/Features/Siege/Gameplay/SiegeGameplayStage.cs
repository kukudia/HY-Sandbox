// 职责：新阵地战副场景的可编辑入口骨架；只展示制作场地与返回，不启动旧SiegeSession或旧HUD。
// 入口：Enter() / Exit()；Main显式注入共享宿主，战斗规则将在后续迁移。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeGameplayStage : MonoBehaviour
{
    [SerializeField]
    private Transform overview;
    [SerializeField]
    private Button returnButton;
    private readonly Dictionary<GameObject, bool> environment = new Dictionary<GameObject, bool>();
    private GameModeController owner;
    public void Enter(GameModeController host, GameObject[] buildEnvironment)
    {
        owner = host;
        foreach (GameObject item in buildEnvironment)
        {
            if (item != null && !environment.ContainsKey(item))
            {
                environment.Add(item, item.activeSelf);
                item.SetActive(false);
            }
        }

        CameraController camera = CameraController.instance;
        camera.StopCameraMotion();
        camera.currentMode = CameraMode.Lock;
        camera.transform.SetPositionAndRotation(overview.position, overview.rotation);
        returnButton.onClick.AddListener(owner.ReturnToBuild);
    }

    public void Exit()
    {
        foreach (KeyValuePair<GameObject, bool> item in environment)
        {
            if (item.Key != null)
            {
                item.Key.SetActive(item.Value);
            }
        }

        environment.Clear();
        if (returnButton != null && owner != null)
        {
            returnButton.onClick.RemoveListener(owner.ReturnToBuild);
        }

        owner = null;
    }

    private void OnDestroy()
    {
        Exit();
    }
}
