using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyManager : MonoBehaviour
{
    // 单例模式确保全局访问
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
                    DontDestroyOnLoad(go); // 跨场景不销毁
                }
            }
            return _instance;
        }
    }

    private float _refreshDelay = 0.2f; // 合并调用的延迟时间（秒）
    private bool _isRefreshScheduled = false;

    // 被销毁物体的计数器（按需替换为其他数据结构）
    private int _destroyedCount = 0;

    public void DestroyGameObject(GameObject obj)
    {
        Debug.Log($"{name} 耐久值为0，物体被销毁");

        ControlUnit unit = obj.GetComponentInParent<ControlUnit>();
        //obj.GetComponent<Block>().DisConnectAllConnectors();
        Destroy(obj);

        if (PlayManager.instance.playMode)
        {
            if (GetComponent<Cockpit>() != null)
            {
                MainUIPanels.instance.PlayEnd();
                return;
            }

            //DestroyManager.Instance.NotifyObjectDestroyed();
            PlayManager.instance.RefreshGroup(unit);
        }
    }

    // 物体销毁时调用此方法
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
        //PlayManager.instance.RefreshGroups();
        _destroyedCount = 0;
    }
}