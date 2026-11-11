# 综合导航场地与自由视角

入口：`Assets/Scenes/EnemyAvoidanceLab.unity`，进入 Play 后默认显示综合城区。场景包含十二组可切换布局，原十一组专项场地保留。综合城区已保存高低楼群、中央隔墙、顶棚、桥梁、柱阵、道路及部署坪；障碍碰撞与视觉装饰分开，装饰不阻挡飞船。

## 操作

- 在场地内按住右键转向，WASD 前后左右移动，Q/E 降低/升高，Shift 加速。松开右键、Esc 或窗口失焦释放鼠标；暂停时仍能移动相机。
- 中键拖动平移，滚轮前后移动；面板可切换正交总览及重置视角，总览滚轮缩放。
- 综合城区面板的六行位置各有“起”和“终”按钮，可以独立组合；旧专项场地提供默认起点/终点。继续支持鼠标点地面及高度调整。
- 选择蓝图后部署，选择起点重置测试舰，运行中选择终点会直接更新目标并记录操作；R 重试、空格暂停。右键观察期间不处理这些试验快捷键。

位置为预期飞船根节点高度；目标物体按控制器悬浮高度偏移放置，避免重复加高。

| 位置 | 坐标（米） |
| --- | --- |
| West Dock | (-120, 8, 0) |
| East Dock | (120, 8, 0) |
| Northwest Pad | (-115, 8, 110) |
| Northeast Pad | (115, 8, 110) |
| Southwest Pad | (-115, 8, -110) |
| Southeast Pad | (115, 8, -110) |

## 制作与记录

`Tools/HY-Sandbox/Combat/Build Complex Navigation Field` 幂等补齐场地、位置标记及相机组件，并保存真实场景和材质资产。已有城区/位置保持手工调整；可以在 Hierarchy 和 Inspector 继续编辑障碍、位置 Transform、相机速度与灵敏度。新建试验场入口也会调用此扩展。

`EnemyLabCamera` 独立管理镜头，`EnemyAvoidanceCourse` 保存位置引用，`EnemyAvoidanceLab.SelectLocation` 统一设置起终点。镜头模式/位置变化和位置选择接入既有会话 `operations.jsonl`，导航输入及规划记录格式沿用[试验场说明](2026-11-11-navigation-test-field.md)。

## 实际验证（Unity 6000.6.8f1）

- Editor 导入编译成功。制作工具重复执行后仍为十二组布局；保存重载后自由相机为透视模式，场景无丢失脚本。
- `EnemyLabViewChecks`：28 项 Play 检查通过，包括六个位置起终点高度一致、无效位置拒绝、暂停时键鼠移动/转向、释放鼠标、总览切换及操作记录。
- `EnemyLabPlayChecks`：29 项通过，覆盖蓝图替换、点选、记录、暂停恢复及十二个布局启用。
- `NavigationCombatPlayChecks`：55 项通过，覆盖身位扫掠、速度预判、垂直优先、A*、连续航向及炮塔遮挡回归。
- 实船 Finch，Northwest Pad → Northeast Pad：44.36 秒进入到达范围，行程 232.79 米、5 次转向，墙体/目标/地面接触均为 0。记录：`NavigationLabRecords/20261111-111908-312-d4a22c/trial-0003`（persistentDataPath 下）。判定到达时距目标约 19.95 米，未测试精确停靠。
- 已查看透视和总览截图，检查楼群、道路、部署坪和六处标签。
- 可读性检查 0 错误；`dotnet build HY-Sandbox.sln --no-restore` 0 错误，37 条既有警告；静态构建与 Play 证据分别记录。

跨区冒烟只覆盖上述一条路线。中央东西向复杂路线曾出现 A* 节点预算耗尽；本次交付测试设施，不代表所有位置组合、所有尺寸蓝图均可通行，也未修改导航预算或算法。
