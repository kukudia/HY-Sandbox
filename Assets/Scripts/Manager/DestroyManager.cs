using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyManager : MonoBehaviour
{
    private static DestroyManager _instance;
    public static DestroyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<DestroyManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DestroyManager");
                    _instance = go.AddComponent<DestroyManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private float _refreshDelay = 0.2f;
    private float _unitCleanupDelay = 5f;
    private bool _isRefreshScheduled;
    private int _destroyedCount;
    private HashSet<string> _scheduledUnitCleanups = new HashSet<string>();

    public void DestroyGameObject(GameObject obj)
    {
        Debug.Log($"{obj.name} durability reached 0, destroying object.");

        Cockpit cockpit = obj.GetComponent<Cockpit>();
        ControlUnit unit = obj.GetComponentInParent<ControlUnit>();
        if (unit != null)
        {
            unit.EnsureRuntimeUnitId();
            unit.AssignRuntimeOwnershipToBlocks(false);
        }

        RuntimeUnitMember member = obj.GetComponent<RuntimeUnitMember>();
        string ownerUnitId = member != null ? member.ownerUnitId : unit?.runtimeUnitId;
        UnitFaction ownerFaction = member != null ? member.ownerFaction : cockpit != null ? cockpit.faction : unit != null ? unit.faction : UnitFaction.Enemy;
        Destroy(obj);

        if (!PlayManager.instance.playMode) return;

        if (cockpit != null)
        {
            ScheduleUnitCleanup(ownerUnitId, ownerFaction);
            return;
        }

        if (unit != null)
        {
            PlayManager.instance.RefreshGroup(unit);
        }
    }

    private void ScheduleUnitCleanup(string ownerUnitId, UnitFaction ownerFaction)
    {
        if (string.IsNullOrEmpty(ownerUnitId) || _scheduledUnitCleanups.Contains(ownerUnitId))
        {
            return;
        }

        _scheduledUnitCleanups.Add(ownerUnitId);
        StartCoroutine(CleanupUnitAfterDelay(ownerUnitId, ownerFaction));
    }

    private IEnumerator CleanupUnitAfterDelay(string ownerUnitId, UnitFaction ownerFaction)
    {
        yield return new WaitForSeconds(_unitCleanupDelay);

        RuntimeUnitMember[] members = Object.FindObjectsByType<RuntimeUnitMember>(FindObjectsSortMode.None);
        foreach (RuntimeUnitMember member in members)
        {
            if (member == null || member.ownerUnitId != ownerUnitId) continue;

            PlayDisappearEffect(member.gameObject);
            Destroy(member.gameObject);
        }

        _scheduledUnitCleanups.Remove(ownerUnitId);

        if (ownerFaction == UnitFaction.Player && PlayManager.instance != null && PlayManager.instance.playMode)
        {
            MainUIPanels.instance.PlayEnd();
        }
    }

    private void PlayDisappearEffect(GameObject obj)
    {
        // Reserved hook for a future death/disappear VFX before the block is destroyed.
    }

    public void NotifyObjectDestroyed()
    {
        _destroyedCount++;
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        if (_isRefreshScheduled) return;

        _isRefreshScheduled = true;
        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(_refreshDelay);
        ExecuteRefresh();
        _isRefreshScheduled = false;
    }

    private void ExecuteRefresh()
    {
        Debug.Log($"Refreshing after {_destroyedCount} objects destroyed");
        _destroyedCount = 0;
    }
}
