# 2026-10-26 阵地战指挥栏与战斗阶段HUD

本轮目标是用户提出的“简化多余UI、兼顾性能，同时更好玩更壮观”的第一步：先让阵地在部署和战斗两个阶段都能看清战场，再叠加玩法与表现。改动集中在 `Assets/Scenes/Siege.unity` 的UI层级与 `SiegeHud` 的显示逻辑，规则、经济、物理、波次算法未改。

## 旧界面的实际问题（实测）

在普通 `Assets/Scenes/Siege.unity` 进入Play并部署一艘舰后，1920×1080合成截屏与UI层级普查记录到：

- Main遗留的 `World Preparation`（独立Canvas与MainCanvas各一份）仍在显示“Preparing terrain and buildings...”，覆盖屏幕中央。
- `PlayPanel/ThrusterInfoPanel` 窗口（480×540，含 MAIN/UNIVERSAL/HOVER 三个页签）默认展开，右侧约三分之一画面被浅色面板覆盖。
- `PlayPanel/PhysicsTelemetry` 常驻显示 `SPEED/MASS/KINETIC/LAST IMPACT` 调试遥测。
- 舰队坞内“部署控件”与“修缮/泊位”混排；核心耐久在顶部栏与独立 `Core Health` 面板重复出现；按键说明整段常驻。
- 战斗阶段没有任何阶段化界面：部署按钮、修缮按钮与掉落说明仍然占位，波次进度只能从顶部一行数字推断。

## 改动

1. 新增制作工具 `Assets/Editor/Siege/SiegeCommandHudAuthoring.cs`：菜单 `HY-Sandbox/Siege/Apply Command HUD`，幂等重排HUD并保存场景。它不重建舰船/基地，不改Main场景；停用对象只 `SetActive(false)`，序列化引用保持可逆。
2. 顶栏改为单条指挥栏：`BASTION · 阶段`、`CREDITS / WAVE / RAIDERS / NEXT`、状态行、右侧核心耐久文字与比例条（低耐久转黄/红）。
3. 左栏收敛为舰队坞：蓝图下拉、部署按钮、刷新、6个泊位、两个修缮按钮分成 `Dock Deploy Group` / `Dock Bays Group` / `Dock Refit Group` 三组；战斗阶段收起部署组与修缮组，泊位组上移，面板高度从726压到344。
4. 底部动作行只保留 `BEGIN NEXT WAVE [P]`、`BUILD / BLUEPRINTS [B]`，`RESTART SIEGE` 降为右下小按钮；战斗阶段整行换成 `WAVE n · RAIDERS k / total` 进度条。
5. 新增战斗反馈：进入波次/清空波次/失败各弹一次居中横幅（2.6秒淡出），击毁敌舰显示 `RAIDER DESTROYED · +奖励` 短提示；两者只改 `CanvasGroup.alpha`，不做每帧文字重建。
6. 新增 `Ship Status` 面板（PILOT名、整机耐久条、驾驶舱条），替换Main的 `HealthBar` 与 `FlightTelemetry` 调试块；Main遗留面板只在需要时保留但不显示。
7. `F2` 切换按键说明面板，默认隐藏，右下留 `F2 CONTROLS` 提示。
8. 刷新成本：文字仍按0.25秒节流；按钮与标签的 `GetComponentInChildren<Text>` 改为 `OnEnable` 一次性缓存，战斗阶段每帧只更新两个 `anchorMax` 与一个透明度。没有新增每帧查找、分配或物理查询。

## 验证

环境：Windows，Unity 6000.3.11f1，CLI 1.0.0-beta.8，普通 `Assets/Scenes/Siege.unity`，编辑模式重排后重新载入场景并确认序列化引用全部保留。

| 验证 | 结果与边界 |
| --- | --- |
| 场景保存/重载 | `Apply Command HUD` 保存后重新打开场景，`helpPanel/coreFill/deployGroup/combatGroup/shipStatus/bannerPanel/killFeed/dockPanel/baysGroup/refitGroup/dockTitle` 全部解析成功，未出现空引用。 |
| 部署流程 | `SiegeUsabilityChecks.RunDeployment()` 9项通过（泊位回退、精确扣费、全受阻拒绝、清障再购买、非法选项不扣费等），证据 `Temp/Siege/usability-deployment.json`。 |
| 蓝图选择/工作台 | `SiegeUsabilityChecks.RunSelection()` 10项通过，含“通过生产入口打开/关闭工作台”，说明新按钮布局仍走真实入口，证据 `Temp/Siege/usability-selection.json`。 |
| 真实Play流程 | 全新Siege Play部署AirKing（2,225金币，余额775）→ 开第1波 → 12秒后 `phase=Battle wave=1 active=2 pending=0 core=500`，横幅与波次进度按阶段出现。 |
| 视觉 | 1920×1080合成截屏四张：部署、按键说明、波次横幅、战斗（含淡出后），保存于 `Temp/Siege/2026-10-26-hud/`。核心耐久文字与比例条、战斗进度条、坐舰耐久均实际可见。 |
| 静态检查 | `dotnet run --project Tools/CodeReadability -- --check .`：322脚本，0格式差异、0错误；`dotnet build HY-Sandbox.sln --no-restore` 0错误/4既有警告；`git diff --check` 通过。 |
| 导航 | `generate_navigation.py` 重新生成 42图/322脚本；新增“阵地战HUD指挥栏与阶段切换”函数图。 |

## 发现但未在本轮修复

- `SiegePlayChecks.Run()` 在当前HEAD不再整体通过，且两次运行停在不同断点：带本轮改动时停在“no surviving ships falls back to base observation”，把 `SiegeHud`/`SiegeMatch` 还原到HEAD后停在更早的“repair restores actual detached collider participation and single connected hull”。两次都使用同一场景与同一Editor，说明该套件在现有HEAD上对物理/时机敏感或已回归，不能作为本轮HUD改动的回归证据，也不能再宣称“旧闭环28项通过”。后续应拆成可容错的逐项报告并单独核对维修/接管链。
- `CargoHoldView.LateUpdate` 在底座方块上出现 `MaterialPropertyBlock` 空引用（`_properties` 未初始化），本次用只读普查确认48个实例的 `_properties` 为null、`_hold` 有效；只在 `_statusLamp` 非空的实例触发。未修复，属既有缺陷，计划单独小改并附证据。
- 编辑模式打开 `Siege` 场景时 `CargoHoldView` 曾累计50条空引用日志，属于同一问题。
- 本轮没有重建Windows Player，也没有做长时自然多波性能采样；帧时间结论仍以既有Player证据为准。

## 收尾复核（2026-10-27 01:20）

最终代码上重跑全部套件，均为通过：

| 套件 | 结果 |
| --- | --- |
| `SiegePlayChecks.Run()` | **28/28 PASS，0 FAIL**（含此前失败的维修/自动修缮/接管三项） |
| `SiegeUsabilityChecks.RunDeployment()` | 9/9 |
| `SiegeUsabilityChecks.RunSelection()` | 10/10 |
| `SiegeCombatFeedbackChecks.Run()` | 17/17（精英、连击、无伤奖励、战果记录） |
| 独立Windows D3D11 Player `-siege-checks-report` | **89项通过/0错误**，`outcome=scripted-loop-passed` |

期间的收尾打磨：部署下拉框不再显示无贴图的实心方块箭头；顶栏的 `ELITE IN n` 只在距精英波≤2波时出现，避免压到核心耐久条（此前在部署态显示 `ELITE IN 5` 与耐久条重叠）。

## 下一步

1. 玩法：精英波次与击毁连击奖励，让“打得好”有可见回报。（已在 `cf6d87c` 完成）
2. 表现：核心受击的屏幕边缘告警、波次入口的敌舰预警轨迹、环境氛围（雾/地平线/灯光）。（已在 `cf6d87c` 与夜色外观提交完成）
3. 性能：补充HUD刷新与横幅的成本测量，确认新增UI不影响既有帧时间分布。（部分完成：有自然战斗Player帧记录，仍无A/B）
4. 测试基建：把 `SiegePlayChecks` 改为收集全部结果再判定，恢复可复现的回归基线。（已在 `4b92a69` 完成）
