// 职责：记录各使用者的工作意图及当前供电，空闲负载不占功率，供电短缺提供效率。
// 入口：使用者先 SetWorkRequested()；PowerNetwork.Rebuild() 顺序调用 ResetPower() / ReceivePower() / CommitPowerStatus()。
// 生命周期：启用时登记负载；最后请求撤销或退出立即清零；独立电网累加供电但不超出需求。
using System.Collections.Generic;
using UnityEngine;

public class Power : MonoBehaviour
{
    private static readonly HashSet<Power> ActivePowerBlocks = new HashSet<Power>();
    public float currentPower = 0f;
    [Tooltip("开始工作的最低供电功率；达到阈值后 isWorking 为 true。")]
    public float minWorkingPower = 50f;
    [Tooltip("启动后占用的额定功率；供电短缺时按需求比例分配。")]
    public float standardWorkingPower = 100f;
    private StatusIcon _icon;
    private readonly HashSet<Object> _workRequesters = new HashSet<Object>();
    public bool IsWorkRequested
    {
        get
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            foreach (Object requester in _workRequesters)
            {
                if (requester != null && (!(requester is Behaviour behaviour) || behaviour.isActiveAndEnabled))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public float RequestedPower => IsWorkRequested ? Mathf.Max(0f, Mathf.Max(minWorkingPower, standardWorkingPower)) : 0f;
    public bool isWorking => IsWorkRequested && currentPower > 0f && currentPower >= Mathf.Max(0f, minWorkingPower);
    public float efficiency => IsWorkRequested && standardWorkingPower > 0f ? Mathf.Clamp01(currentPower / standardWorkingPower) : 0f;
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
        _workRequesters.Clear();
        currentPower = 0f;
        _icon?.CheckPowerStatus();
    }

    /// <summary>先提交工作意图再读取供电，避免断电后无法再次启动；同一负载允许多个使用者独立请求。</summary>
    public void SetWorkRequested(Object requester, bool requested)
    {
        if (requester == null)
        {
            return;
        }

        if (requested)
        {
            _workRequesters.Add(requester);
        }
        else
        {
            _workRequesters.Remove(requester);
        }

        if (!IsWorkRequested)
        {
            currentPower = 0f;
        }

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
        currentPower = Mathf.Min(RequestedPower, currentPower + Mathf.Max(0f, suppliedPower));
    }

    public void CommitPowerStatus()
    {
        _workRequesters.RemoveWhere(IsDestroyedRequester);
        _icon?.CheckPowerStatus();
    }

    private static bool IsDestroyedRequester(Object requester)
    {
        return requester == null;
    }
}
