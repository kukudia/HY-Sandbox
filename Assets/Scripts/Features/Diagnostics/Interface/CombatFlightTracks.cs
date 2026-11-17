// 职责：用场景保存的有界追踪槽展示当前战场飞行历史与规划路线，共用5v5的NavigationTrack。
// 入口：Update()每0.1秒按阵营绑定/采样；关闭诊断后隐藏并释放绑定，不创建运行时组件。
using System.Collections.Generic;
using UnityEngine;

public sealed class CombatFlightTracks : MonoBehaviour
{
    [SerializeField]
    private NavigationTrack[] tracks;
    private readonly HashSet<ControlUnit> bound = new HashSet<ControlUnit>();
    private float nextSample;
    private void Update()
    {
        if (Time.unscaledTime < nextSample || PauseMenu.IsPaused)
        {
            return;
        }

        nextSample = Time.unscaledTime + .1f;
        DebugManager manager = DebugManager.instance;
        bool show = manager != null && manager.showFlightTrajectories && PlayManager.instance != null && PlayManager.instance.playMode;
        bound.Clear();
        foreach (NavigationTrack track in tracks)
        {
            if (track.Owner != null && (!show || !track.Alive || !manager.Includes(track.Owner)))
            {
                track.Bind(null, string.Empty, true);
            }

            if (track.Owner != null)
            {
                bound.Add(track.Owner);
            }

            track.SetVisible(show && track.Owner != null, show && track.Owner != null);
        }

        if (!show)
        {
            return;
        }

        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (unit == null || !unit.HasValidCockpit || unit.Building != null || !unit.IsSimulationActive || !manager.Includes(unit) || bound.Contains(unit))
            {
                continue;
            }

            foreach (NavigationTrack track in tracks)
            {
                if (track.Owner == null)
                {
                    track.Bind(unit, unit.name, true);
                    bound.Add(unit);
                    track.SetVisible(true, true);
                    break;
                }
            }
        }

        foreach (NavigationTrack track in tracks)
        {
            if (track.Owner != null)
            {
                track.Sample();
            }
        }
    }
}
