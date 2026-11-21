// 职责：保存发电量并登记已启用的发电机，不承担电网遍历或负载分配。
// 入口：OnEnable() / OnDisable()；PowerNetwork.Rebuild() 读取 ActiveUnits 和 outputPower。
// 生命周期：退出时清除中继的反向引用，下一次电网重建排除该发电机。
using System.Collections.Generic;
using UnityEngine;

public class PowerGeneratingUnit : MonoBehaviour
{
    private static readonly HashSet<PowerGeneratingUnit> ActiveGenerators = new HashSet<PowerGeneratingUnit>();
    [Min(0f)]
    [Tooltip("加入所连电网的总输出功率，按该电网中已启动负载的需求分配。")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("outputPower")]
    private float _outputPower = 1000f;
    public float outputPower
    {
        get => _outputPower;
        set
        {
            if (_outputPower != value)
            {
                _outputPower = value;
                NotifyPowerChanged();
            }
        }
    }

    public List<PowerTransmissionDevice> connectedDevices = new List<PowerTransmissionDevice>();
    public static IEnumerable<PowerGeneratingUnit> ActiveUnits => ActiveGenerators;

    private ControlUnit powerOwner;
    internal void NotifyPowerChanged()
    {
        if (powerOwner != null)
        {
            powerOwner.powerDirty = true;
        }
    }

    private void OnValidate()
    {
        NotifyPowerChanged();
    }

    internal void BindPowerOwner()
    {
        ControlUnit next = isActiveAndEnabled ? GetComponentInParent<ControlUnit>() : null;
        if (next == powerOwner)
        {
            return;
        }

        if (powerOwner != null)
        {
            powerOwner.generators.Remove(this);
            powerOwner.powerDirty = true;
        }

        powerOwner = next;
        if (next != null)
        {
            next.generators.Add(this);
            next.powerDirty = true;
        }
    }

    private void OnTransformParentChanged()
    {
        BindPowerOwner();
    }

    private void OnEnable()
    {
        ActiveGenerators.Add(this);
        BindPowerOwner();
    }

    private void OnDisable()
    {
        ActiveGenerators.Remove(this);
        BindPowerOwner();
    }
}
