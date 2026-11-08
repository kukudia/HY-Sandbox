// 职责：保留未保存的建造原稿与镜头，给探索创建独立副本；返回仅迁移既有货仓结果。
// 入口：Park() / CreateRuntimeCopy() / Restore()；原稿不参加运行时控制单元清理。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class PlayDraft
{
    private Transform original;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private bool originalActive;
    private Dictionary<string, List<CargoItem>> returnedCargo;
    private bool parked;
    public bool HasDraft => parked;

    public void Park()
    {
        if (parked)
        {
            return;
        }

        parked = true;
        original = GameManager.instance.blocksParent;
        if (CameraController.instance != null)
        {
            CameraController.instance.StopCameraMotion();
            cameraPosition = CameraController.instance.transform.position;
            cameraRotation = CameraController.instance.transform.rotation;
        }

        if (original != null)
        {
            originalActive = original.gameObject.activeSelf;
            PlayManager.instance.allControlUnits.Remove(original.GetComponent<ControlUnit>());
            original.gameObject.SetActive(false);
        }

        GameManager.instance.blocksParent = null;
        PlayManager.instance.blocksParent = null;
    }

    public void CreateRuntimeCopy(Scene scene)
    {
        Park();
        GameObject copy = Object.Instantiate(original.gameObject);
        copy.name = original.name + " (Play)";
        SceneManager.MoveGameObjectToScene(copy, scene);
        GameManager.instance.blocksParent = copy.transform;
        PlayManager.instance.blocksParent = copy.transform;
        copy.SetActive(true);
    }

    public void Restore()
    {
        if (!parked)
        {
            return;
        }

        if (original != null)
        {
            if (returnedCargo != null)
            {
                foreach (CargoHold hold in original.GetComponentsInChildren<CargoHold>(true))
                {
                    string id = hold.GetComponent<Block>().uniqueId;
                    hold.RestoreContents(returnedCargo.TryGetValue(id, out List<CargoItem> contents) ? contents : new List<CargoItem>());
                }
            }

            original.gameObject.SetActive(originalActive);
            GameManager.instance.blocksParent = original;
            PlayManager.instance.RegisterControlUnit(original.GetComponent<ControlUnit>());
        }

        if (CameraController.instance != null)
        {
            CameraController.instance.StopCameraMotion();
            CameraController.instance.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }

        original = null;
        parked = false;
        returnedCargo = null;
    }

    public void CaptureReturnCargo(bool survived)
    {
        returnedCargo = CargoPersistence.CapturePlayerContents(survived);
    }
}
