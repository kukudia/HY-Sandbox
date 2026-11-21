// 职责：保留旧蓝图的中继序列化字段。
// 入口：OnEnable 隐藏已停用的范围及连线。
// 供电由所属 ControlUnit 分配。
using System.Collections.Generic;
using UnityEngine;

public class PowerTransmissionDevice : MonoBehaviour
{
    public List<PowerGeneratingUnit> connectedGenerators = new List<PowerGeneratingUnit>();
    public List<PowerTransmissionDevice> connectedDevices = new List<PowerTransmissionDevice>();
    [Min(0)]
    [Tooltip("供电盒的世界坐标半边长；范围内已启动负载按需求参与功率分配。")]
    public int powerRange = 5;
    [Min(0)]
    [Tooltip("连接盒的世界坐标半边长；中继互连采用两端较小值。")]
    public int maxConnectionDistance = 10;
    public GameObject debugCube;
    public LineRenderer lineRenderer;
    public float availableNetworkPower
    {
        get; internal set;
    }
    public bool showPowerRange => false;
    public bool showPowerConnections => false;
    internal Material DebugRangeMaterial => null;

    private void OnEnable()
    {
        if (debugCube == null)
        {
            debugCube = transform.Find("Debug/Cube")?.gameObject;
        }

        if (debugCube != null)
        {
            debugCube.SetActive(false);
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
}
