// 职责：按原始单位 ID 去重延迟清理；分别校验整单位、无驾驶舱组和远离组。
// 入口：ScheduleUnit() / ScheduleGroup() / ScheduleDistantGroup()。
// 生命周期：协程由 DestroyManager 承载；结束会话先停止协程再 Clear()。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class SalvageCleanupSession
{
    private readonly MonoBehaviour _coroutineHost;
    private readonly HashSet<string> _scheduledUnits = new HashSet<string>();
    internal SalvageCleanupSession(MonoBehaviour coroutineHost)
    {
        _coroutineHost = coroutineHost;
    }

    internal void ScheduleUnit(GameObject destroyedObject, string ownerUnitId, float fallbackDelay)
    {
        if (!TrySchedule(ownerUnitId))
        {
            return;
        }

        Debug.Log($"Scheduling cleanup for unit {destroyedObject} after {fallbackDelay} seconds.");
        _coroutineHost.StartCoroutine(CleanupUnitAfterDelay(ownerUnitId, fallbackDelay));
    }

    internal void ScheduleGroup(ControlUnit unit, float fallbackDelay)
    {
        if (unit == null || unit.HasAnyCockpit)
        {
            return;
        }

        unit.EnsureRuntimeUnitId();
        string ownerUnitId = unit.runtimeUnitId;
        if (!TrySchedule(ownerUnitId))
        {
            return;
        }

        _coroutineHost.StartCoroutine(CleanupGroupAfterDelay(unit, ownerUnitId, fallbackDelay));
    }

    internal void ScheduleDistantGroup(ControlUnit unit, Transform reference, float maxDistance, float fallbackDelay)
    {
        if (unit == null || reference == null || maxDistance <= 0f)
        {
            return;
        }

        unit.EnsureRuntimeUnitId();
        string ownerUnitId = unit.runtimeUnitId;
        if (!TrySchedule(ownerUnitId))
        {
            return;
        }

        _coroutineHost.StartCoroutine(CleanupDistantGroupAfterDelay(unit, ownerUnitId, reference, maxDistance * maxDistance, fallbackDelay));
    }

    internal void Clear()
    {
        _scheduledUnits.Clear();
    }

    private bool TrySchedule(string ownerUnitId)
    {
        return !string.IsNullOrEmpty(ownerUnitId) && _scheduledUnits.Add(ownerUnitId);
    }

    private static float GetCleanupDelay(float fallbackDelay)
    {
        return WreckSalvage.Settings != null ? WreckSalvage.Settings.cleanupDelay : fallbackDelay;
    }

    private IEnumerator CleanupDistantGroupAfterDelay(
        ControlUnit unit,
        string ownerUnitId,
        Transform reference,
        float maxDistanceSqr,
        float fallbackDelay)
    {
        yield return new WaitForSeconds(GetCleanupDelay(fallbackDelay));
        if (unit != null && reference != null && unit.runtimeUnitId == ownerUnitId && (unit.transform.position - reference.position).sqrMagnitude > maxDistanceSqr)
        {
            WreckSalvage.ConvertGroup(unit);
            Object.Destroy(unit.gameObject);
        }

        _scheduledUnits.Remove(ownerUnitId);
    }

    private IEnumerator CleanupGroupAfterDelay(ControlUnit unit, string ownerUnitId, float fallbackDelay)
    {
        yield return new WaitForSeconds(GetCleanupDelay(fallbackDelay));
        if (unit != null && unit.runtimeUnitId == ownerUnitId && !unit.HasAnyCockpit)
        {
            WreckSalvage.ConvertGroup(unit);
            Object.Destroy(unit.gameObject);
        }

        _scheduledUnits.Remove(ownerUnitId);
    }

    private IEnumerator CleanupUnitAfterDelay(string ownerUnitId, float fallbackDelay)
    {
        yield return new WaitForSeconds(GetCleanupDelay(fallbackDelay));
        RuntimeUnitMember[] members = Object.FindObjectsByType<RuntimeUnitMember>(FindObjectsSortMode.None);
        foreach (RuntimeUnitMember member in members)
        {
            if (member == null || member.ownerUnitId != ownerUnitId)
            {
                continue;
            }

            WreckSalvage.Convert(member.GetComponent<Block>());
            Object.Destroy(member.gameObject);
        }

        _scheduledUnits.Remove(ownerUnitId);
    }
}
