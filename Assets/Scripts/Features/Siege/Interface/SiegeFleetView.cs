// 职责：将唯一驾驶权绑定到共用镜头和玩家状态；不创建舰船或改变经济。
// 入口：Focus() / FocusBase()；物理重组后可重新绑定同一舰船。
using UnityEngine;

public static class SiegeFleetView
{
    public static void Focus(ControlUnit unit, float maximum = 0f)
    {
        if (unit == null)
        {
            return;
        }

        PlayManager.instance.blocksParent = unit.transform;
        GameManager.instance.blocksParent = unit.transform;
        unit.TryGetTotalDurability(out _, out float actualMaximum);
        PlayManager.instance.maxHealth = maximum > 0f ? maximum : actualMaximum;
        PlayManager.instance.lastHeight = unit.transform.position.y;
        if (CameraController.instance != null)
        {
            CameraController.instance.playerBody = unit.transform;
            CameraController.instance.currentMode = CameraMode.ThirdPersonLock;
        }
    }

    public static void FocusBase(SiegeBase station)
    {
        Focus(station.Unit);
        if (CameraController.instance != null && station.Core != null)
        {
            CameraController.instance.playerBody = station.Core.transform;
        }
    }
}
