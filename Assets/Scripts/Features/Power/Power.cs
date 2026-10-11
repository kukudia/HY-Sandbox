// 职责：保存负载需求及当前供电，向推进器、武器等提供工作状态与效率。
// 入口：PowerNetwork.Rebuild() 顺序调用 ResetPower() / ReceivePower() / CommitPowerStatus()。
// 生命周期：启用时登记负载；退出立即清零。独立电网的供电可在同一轮累计。
using System.Collections.Generic;
using UnityEngine;

public class Power : MonoBehaviour
{
    private static readonly HashSet<Power> ActivePowerBlocks = new HashSet<Power>();
    public float currentPower = 0f;
    [Tooltip("开始工作的最低供电功率；达到阈值后 isWorking 为 true。")]
    public float minWorkingPower = 50f;
    [Tooltip("达到满效率所需的功率；非正值时效率为 0。")]
    public float standardWorkingPower = 100f;
    private StatusIcon _icon;
    public bool isWorking => currentPower >= minWorkingPower;
    public float efficiency => standardWorkingPower > 0f ? Mathf.Clamp01(currentPower / standardWorkingPower) : 0f;
    public static IEnumerable<Power> ActiveBlocks => ActivePowerBlocks;

    private void OnEnable()
    {
        _icon = GetComponent<StatusIcon>();
        ActivePowerBlocks.Add(this);
        _icon?.CheckPowerStatus();
    }

    private void OnDisable()
    {
        ActivePowerBlocks.Remove(this);
        currentPower = 0f;
        _icon?.CheckPowerStatus();
    }

    /// <summary>开始一轮电网重建时清零；完整汇总后才通过 CommitPowerStatus 刷新状态图标。</summary>
    public void ResetPower()
    {
        currentPower = 0f;
    }

    /// <summary>累加非负供电，允许多个独立网络共同覆盖当前负载。</summary>
    public void ReceivePower(float suppliedPower)
    {
        currentPower += Mathf.Max(0f, suppliedPower);
    }

    public void CommitPowerStatus()
    {
        _icon?.CheckPowerStatus();
    }
}
