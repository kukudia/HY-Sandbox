using System.Collections;
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
    private bool _isRefreshScheduled;
    private int _destroyedCount;

    public void DestroyGameObject(GameObject obj)
    {
        Debug.Log($"{obj.name} durability reached 0, destroying object.");

        Cockpit cockpit = obj.GetComponent<Cockpit>();
        ControlUnit unit = obj.GetComponentInParent<ControlUnit>();
        Destroy(obj);

        if (!PlayManager.instance.playMode) return;

        if (cockpit != null && cockpit.faction == UnitFaction.Player)
        {
            MainUIPanels.instance.PlayEnd();
            return;
        }

        if (unit != null)
        {
            PlayManager.instance.RefreshGroup(unit);
        }
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
