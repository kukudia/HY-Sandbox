// 职责：绘制试验场控制面板并采集限定场景内的键鼠操作，调用试验协调器。
// 入口：PollInput() / Draw()；普通对象，不动态补挂组件、不改玩家输入系统。
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

internal sealed class EnemyLabPanel
{
    private readonly EnemyAvoidanceLab lab;
    private Vector2 blueprintScroll;
    private Vector2 layoutScroll;
    private Vector2 panelScroll;
    private Vector2 locationScroll;
    private bool pickStart;
    private bool pickGoal;
    private bool showRoofs;
    internal EnemyLabPanel(EnemyAvoidanceLab lab)
    {
        this.lab = lab;
    }

    internal void BeginPick(bool start)
    {
        pickStart = start;
        pickGoal = !start;
        lab.RecordOperation("pick_mode", start ? "start" : "goal");
    }

    internal void PollInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            foreach (KeyControl key in keyboard.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    lab.RecordOperation("key_down", key.keyCode.ToString());
                }

                if (key.wasReleasedThisFrame)
                {
                    lab.RecordOperation("key_up", key.keyCode.ToString());
                }
            }

            bool flying = lab.ViewCamera != null && lab.ViewCamera.Looking;
            if (!flying && keyboard.rKey.wasPressedThisFrame)
            {
                lab.RecordOperation("key", "R reset");
                lab.ResetTrial();
            }

            if (!flying && keyboard.spaceKey.wasPressedThisFrame)
            {
                lab.RecordOperation("key", "Space pause");
                lab.SetPaused(!lab.Paused);
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                pickStart = pickGoal = false;
                lab.RecordOperation("key", "Escape cancel pick");
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            string pointer = mouse.position.ReadValue().ToString("F1");
            if (mouse.leftButton.wasPressedThisFrame)
            {
                lab.RecordOperation("mouse_down", "left " + pointer);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                lab.RecordOperation("mouse_up", "left " + pointer);
            }

            if (mouse.middleButton.wasPressedThisFrame)
            {
                lab.RecordOperation("mouse_down", "middle " + pointer);
            }

            if (mouse.middleButton.wasReleasedThisFrame)
            {
                lab.RecordOperation("mouse_up", "middle " + pointer);
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                lab.RecordOperation("mouse_down", "right " + pointer);
            }

            if (mouse.rightButton.wasReleasedThisFrame)
            {
                lab.RecordOperation("mouse_up", "right " + pointer);
            }

            if (mouse.scroll.ReadValue().sqrMagnitude > 0f)
            {
                lab.RecordOperation("mouse_scroll", mouse.scroll.ReadValue().ToString());
            }
        }

        if ((!pickStart && !pickGoal) || mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 cursor = mouse.position.ReadValue();
        if (cursor.x < 375f || Camera.main == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(cursor);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float enter))
        {
            return;
        }

        Vector3 point = ray.GetPoint(enter);
        if (Mathf.Abs(point.x) > 135f || Mathf.Abs(point.z) > 135f)
        {
            return;
        }

        if (pickStart)
        {
            lab.SetStart(new Vector3(point.x, lab.StartPosition.y, point.z));
        }
        else
        {
            lab.SetGoal(new Vector3(point.x, lab.GoalPosition.y, point.z));
        }

        pickStart = pickGoal = false;
    }

    private void ChangeStartHeight(float delta)
    {
        lab.SetStart(lab.StartPosition + Vector3.up * delta);
    }

    internal void Draw()
    {
        if (lab.LayoutCount == 0)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(12f, 12f, 350f, Mathf.Min(Screen.height - 24f, 850f)), GUI.skin.box);
        panelScroll = GUILayout.BeginScrollView(panelScroll);
        GUILayout.Label("导航试验场 / NAVIGATION LAB");
        GUILayout.Label(lab.Status);
        string mode = lab.Navigation.followingPath ? "A*跟随" : lab.Navigation.changingHeight ? "高度避障" : lab.Navigation.avoiding ? "身位避障" : "接近/战斗";
        GUILayout.Label($"{mode} | 上次A* {lab.Navigation.pathStatus} | 节点 {lab.Navigation.expandedNodes} | 航点 {lab.Navigation.waypoint} | 推进 {lab.Navigation.throttleRatio:F2}");
        GUILayout.Label($"预判 {lab.Navigation.predictionDecision} | 障碍 {lab.Navigation.observedObstacles} / 运动 {lab.Navigation.movingObstacles} | 候选 {lab.Navigation.predictionCandidates} | {lab.Navigation.predictionMilliseconds:F2}ms");
        GUILayout.Label($"远场预读 {lab.Navigation.lookAheadDistance:F0}m | {lab.Navigation.farDecision} | 最近 {lab.Navigation.farObstacleDistance:F1}m / 约{lab.Navigation.farArrivalTime:F1}s | {lab.Navigation.farThreat}");
        if (lab.Navigation.predictedCollisionTime >= 0f)
        {
            GUILayout.Label($"原意图预计 {lab.Navigation.predictedCollisionTime:F1}s 后接触 | 替代意图 {(lab.Navigation.selectedCollisionTime < 0f ? "预测窗口内净空" : lab.Navigation.selectedCollisionTime.ToString("F1") + "s后接触")}");
        }

        layoutScroll = GUILayout.BeginScrollView(layoutScroll, GUILayout.Height(145f));
        for (int i = 0; i < lab.LayoutCount; i++)
        {
            if (GUILayout.Button((i == lab.LayoutIndex ? "▶ " : "") + lab.LayoutName(i)))
            {
                lab.SelectLayout(i);
            }
        }

        GUILayout.EndScrollView();
        EnemyAvoidanceCourse course = lab.Course;
        if (course != null)
        {
            GUILayout.Label(course.Description);
        }

        GUILayout.Label("选择蓝图：" + lab.BlueprintName);
        blueprintScroll = GUILayout.BeginScrollView(blueprintScroll, GUILayout.Height(135f));
        for (int i = 0; i < lab.BlueprintCount; i++)
        {
            if (GUILayout.Button((i == lab.SelectedBlueprint ? "▶ " : "") + lab.Blueprints.Options[i].Label))
            {
                lab.SelectBlueprint(i);
            }
        }

        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新目录"))
        {
            lab.RefreshBlueprints();
        }

        if (GUILayout.Button("部署所选蓝图"))
        {
            lab.DeploySelected();
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重试 (R)"))
        {
            lab.ResetTrial();
        }

        if (GUILayout.Button("全部测试"))
        {
            lab.BeginSuite();
        }

        if (GUILayout.Button(lab.Paused ? "继续" : "暂停"))
        {
            lab.SetPaused(!lab.Paused);
        }

        GUILayout.EndHorizontal();
        GUILayout.Label("预设位置：左列设起点 / 右列设终点");
        locationScroll = GUILayout.BeginScrollView(locationScroll, GUILayout.Height(115f));
        for (int i = 0; i < lab.LocationCount; i++)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("起 " + lab.LocationName(i)))
            {
                lab.SelectLocation(i, true);
            }

            if (GUILayout.Button("终 " + lab.LocationName(i)))
            {
                lab.SelectLocation(i, false);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("点选起点"))
        {
            BeginPick(true);
        }

        if (GUILayout.Button("点选目标"))
        {
            BeginPick(false);
        }

        GUILayout.EndHorizontal();
        if (pickStart || pickGoal)
        {
            GUILayout.Label("点击场地设置位置，Esc取消");
        }

        if (GUILayout.Button("结束并保存记录"))
        {
            lab.StopTrial();
        }

        if (GUILayout.Button("打开记录目录"))
        {
            Application.OpenURL(new Uri(System.IO.Path.Combine(Application.persistentDataPath, "NavigationLabRecords")).AbsoluteUri);
        }

        bool roofs = GUILayout.Toggle(showRoofs, "显示顶棚表面（不影响碰撞）");
        if (roofs != showRoofs)
        {
            showRoofs = roofs;
            lab.SetRoofVisibility(roofs);
        }

        EnemyLabCamera view = lab.ViewCamera;
        if (view != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(view.FreeView ? "切换总览" : "自由视角"))
            {
                view.SetFreeView(!view.FreeView);
            }

            if (GUILayout.Button("相机复位"))
            {
                view.ResetOverview();
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Label("自由视角：按住右键 + WASD / Q E升降 / Shift加速");
        GUILayout.Label("R重试 / Space暂停 / 中键平移 / 滚轮缩放");
        GUILayout.Label($"轨迹深蓝慢 / 浅青快（0–{lab.TraceFastSpeed:F0}m/s） / 橙色：规划路线");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("起点高度 -"))
        {
            ChangeStartHeight(-1f);
        }

        if (GUILayout.Button("起点高度 +"))
        {
            ChangeStartHeight(1f);
        }

        if (GUILayout.Button("目标高度 -"))
        {
            lab.SetGoal(lab.GoalPosition + Vector3.down);
        }

        if (GUILayout.Button("目标高度 +"))
        {
            lab.SetGoal(lab.GoalPosition + Vector3.up);
        }

        GUILayout.EndHorizontal();
        GUILayout.Label(lab.Notice);
        GUILayout.Label(lab.RecordingStatus);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
