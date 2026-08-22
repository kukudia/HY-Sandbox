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
    private float _unitCleanupDelay = 10f;
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
        Block block = obj.GetComponent<Block>();
        if (block != null)
        {
            VisualEffectsManager.TryPlayBlockRemoved(block);
        }
        else
        {
            VisualEffectsManager.TryPlayObjectDestroyed(obj);
        }
        Destroy(obj);

        if (!PlayManager.instance.playMode) return;

        if (cockpit != null)
        {
            if (ownerFaction == UnitFaction.Player && PlayManager.instance.playMode)
            {
                //MainUIPanels.instance.PlayEnd();
                MainUIPanels.instance.PlayerDeath();
                return;
            }

            ScheduleUnitCleanup(obj, ownerUnitId, ownerFaction);

            //Block explosion fuction

            return;
        }

        if (unit != null)
        {
            PlayManager.instance.RefreshGroup(unit);
        }
    }

    private void ScheduleUnitCleanup(GameObject obj, string ownerUnitId, UnitFaction ownerFaction)
    {
        if (string.IsNullOrEmpty(ownerUnitId) || _scheduledUnitCleanups.Contains(ownerUnitId))
        {
            return;
        }

        _scheduledUnitCleanups.Add(ownerUnitId);

        Debug.Log($"Scheduling cleanup for unit {obj} after {_unitCleanupDelay} seconds.");

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
    }

    private void PlayDisappearEffect(GameObject obj)
    {
        VisualEffectsManager.TryPlayObjectDestroyed(obj);
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
