# 2026-10-26 阵地战来袭方向标记

夜色竞技场把整体亮度压下来之后，一个真实的可读性问题浮出来：敌舰在半径130米外进场，默认第三人称视角看不到，玩家只能从顶部数字推断“有东西来了”。本轮加的是最小闭环的威胁感知，不做雷达扫描或小地图。

## 改动

- 新增 `Assets/Scripts/Features/Siege/SiegeThreatMarkers.cs`，挂在Siege HUD上，由菜单 `HY-Sandbox/Siege/Apply Command HUD` 幂等添加并绑定 `session` 与主相机。
- 运行时建立固定 6 个标记（与单波同时在场上限一致），每个是橙色菱形 + 距离文字。**只在敌人不在视野内、或距离超过90米时显示**，避免在已经看得见目标时增加噪声。
- 位置换算经过 `RectTransformUtility.ScreenPointToLocalPointInRectangle`，因为覆盖层画布被 `CanvasScaler` 缩放（实测画布宽度2347而非1920），直接混用 `Screen.width` 与画布坐标会把标记顶出屏幕。目标在相机后方时方向取反，最后按画布半尺寸减边距夹取。
- 刷新间隔0.1秒，只写 `anchoredPosition` 与（整数变化时的）距离文字；没有每帧分配、没有新增渲染对象批次。

## 验证

| 验证 | 结果与边界 |
| --- | --- |
| 真实Play | 第1波开始后，两个敌舰在视野外，HUD右/下边缘出现 `88 m`、`123 m` 两个橙色菱形标记；截屏 `Temp/Agent/markers2.png`。 |
| 位置约束 | 运行时读取：`Threat Marker 1 pos=(1077.7,-266.6)`、`Marker 2 pos=(538.7,-444.0)`，画布半尺寸为(1173,540)、边距96，两个标记都在画布内且贴边。 |
| 关闭条件 | 无敌人时 `active=False`；敌人在视野内且近于90米时不显示（`onScreen` 分支直接跳过）。 |
| 静态检查 | CodeReadability 325脚本、0格式差异、0错误；`dotnet build` 0错误；`git diff --check` 通过。 |
| 参数化 | `poolSize/edgeMargin/alwaysMarkBeyond/markerColor/labelSize` 都是Inspector字段，可在场景里改。 |

## 未验证

- 没有在6舰同时在场的压力窗口下观察标记重叠；6个标记可能挤在相邻边缘位置，尚无分层或避让。
- 没有在超宽/竖屏比例下复核；夹取用的是画布实际尺寸，理论上自适应，但只有16:9实测。
- 标记没有区分舰型或威胁等级，也不显示“正在攻击基地”的状态。
- 未测量该组件的CPU占用；按设计是每0.1秒对不超过6个敌人的投影，未做帧时间对照。
