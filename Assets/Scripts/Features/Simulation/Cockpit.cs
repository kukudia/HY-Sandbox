// 职责：保存驾驶舱阵营与驾驶来源；驾驶权随驾驶舱迁移，不随物理父组重建丢失。
using UnityEngine;

public enum UnitControlMode
{
    Player,
    Autonomous
}

public class Cockpit : MonoBehaviour
{
    public UnitFaction faction = UnitFaction.Player;
    [SerializeField, Tooltip("玩家阵营中仅被接管的驾驶舱读取键盘；其余由AI提供驾驶意图。")]
    private UnitControlMode controlMode;
    public UnitControlMode ControlMode => controlMode;

    public void SetControlMode(UnitControlMode mode)
    {
        controlMode = mode;
    }
}
