# 自动随机3v3观战场景

入口：`Assets/Scenes/Random3v3Arena.unity`，打开场景进入Play自动开始，无需部署按钮。它是双层 EnemyAvoidanceLab 综合城区的独立副本；原场景不改动。保留高架层、桥梁、地面街区和四组动态障碍，移除了避障试验面板、旧测试舰与其他专项布局。

## 对战循环

- 六个真实槽位位于 `Automatic 3v3 Match` 下，前3个蓝队（Player阵营）、后3个红队（Enemy阵营），全部使用Autonomous驾驶。
- 开局从相同池为六槽独立随机抽取敌舰，允许重复：Inspector中的三份内置蓝图，加上只读加载的 `persistentDataPath/EnemyBlueprints` 有效敌舰；不读取玩家Saves、不写蓝图。`Include Saved Enemies` 可关闭外部敌舰池。
- 保留正式供电、推进器、武器和碰撞伤害。驾驶舱摧毁为淘汰，机体根节点重组不会误算死亡。一队三个驾驶舱全部淘汰后结算，双方同时全灭显示DRAW。
- 结算时停止AI/武器，保留战场5个模拟秒；随后清除本轮舰船、重组碎片、脱离模块、掉落、所有池内弹丸与活动特效。等待Destroy完成后重新随机填满六个槽位，动态障碍恢复初始相位，自动进入下一轮。相机姿态和地图编辑资产保留。
- `Restart Delay` 默认5秒，可在Inspector调整；暂停模拟会暂停倒计时。没有加超时强制判负：失去推进器但驾驶舱仍存活的舰船仍需被摧毁，因此部分蓝图组合可能长时间无法结束。

## 观战与击杀

自由透视相机：右键+WASD移动、Q/E升降、Shift加速、中键平移、滚轮前后移动。独立场景不再预留避障面板的375像素禁用区；失焦或松开右键释放鼠标。

左上显示轮次、双方存活数和六槽阵容；右上保留最近六条击杀信息。击杀浮窗复用Exploration的 `CombatHud` 视觉和淡入淡出主链，显示攻击者→被击杀者及双方存活数；无已知攻击者时只显示被摧毁，不虚构击杀者。副本加宽提示布局以容纳两侧蓝图名。正式 `DestroyManager` 在模块脱离前通知本局，蓝队AI死亡不会调用探索玩家死亡界面。Exploration的 `ShowPlayerKill` 继续保留原累计逻辑。

制作入口：`Tools/HY-Sandbox/Combat/Create Random 3v3 Arena`；目标场景存在时拒绝覆盖。运行入口：`SkirmishMatch.Start`；流程图见 [RuntimeFlow](../RuntimeFlow.md)，方法导航见 [CodeIndex](../CodeIndex.md)。本场景供直接Editor运行，未接入主菜单或制作独立Player包。

## 实际验证

- Unity 6000.6.8f1实际导入编译、保存重载；`SkirmishArenaChecks.ValidateSaved()` 15项通过，覆盖六槽、蓝图、5秒参数、显式引用、移除旧协调器、四个动态障碍、自由视角及没有预存战斗舰。
- `SkirmishArenaChecks.Begin()` 21项Play检查通过。受控真实驾驶舱伤害分别触发红队与蓝队战败，验证击杀归因、重复通知不重复计数、5秒前不清理、5秒后六艘新舰、旧舰/脱离物/掉落/池内弹丸清除、轮次击杀重置，连续运行到第三轮。受控清场用的伤害刺激不等于自然交战胜负证明。
- 自然交战观察中六艘AI自动移动、发生实际耐久损失，并出现RED-3 Type12→BLUE-3 default_enemy、RED-2 Type12→BLUE-2 CODEX_E03_Manta等非刺激击杀播报；HUD阵容和名牌可见；没有将这段未结束的自然战斗记为完整自然轮次验收。
- 已检查实际Play截图，包括地图、副本HUD和真实驾驶舱伤害触发的击杀列表；另以运行时临时延长浮窗时长检查加宽布局，未写回默认时长。截图在 `Temp/Random3v3Arena`。
- 在复用的CombatHud实例上执行原探索 `ShowPlayerKill` 两次，实际文本为 `KILLS 02` 且保留第二个目标名；未在本轮重跑整个Exploration场景。
- 原 EnemyAvoidanceLab 场景无Git改动。Play检查阶段Console新增Error为0；最终静态构建0错误、36条警告。可读性/导航及 `git diff --check` 结果见提交验证信息。

不保证所有外部敌舰都武装完备或适合当前净空，也不保证所有随机组合都迅速分胜负；本次不改造已有AI导航、外部蓝图或其战斗能力。

自由相机组件、透视模式与全屏操控区已保存验证；本轮未完成真实键鼠自由飞行输入回归，Editor曾报告Application.isFocused为false。
