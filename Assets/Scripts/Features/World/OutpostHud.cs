// 职责：在游玩HUD显示最近据点剩余驻军及本次探索清剿数量。
// 入口：Update()；引用由OutpostHudAuthoring保存，不在运行时生成界面。
using UnityEngine;
using UnityEngine.UI;

public sealed class OutpostHud : MonoBehaviour
{
    [SerializeField]
    private Text _label;
    [SerializeField]
    private WorldSessionManager _session;
    private float nextUpdate;
    private void Update()
    {
        if (Time.unscaledTime < nextUpdate)
        {
            return;
        }

        nextUpdate = Time.unscaledTime + 0.2f;
        OutpostEncounterDirector director = _session != null ? _session.Outposts : null;
        _label.text = director == null ? "" : director.Status + (director.ClearedCount > 0 ? "\nOutposts cleared this expedition: " + director.ClearedCount : "");
    }
}
