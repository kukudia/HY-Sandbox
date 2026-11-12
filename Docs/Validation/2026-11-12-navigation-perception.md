# 远场路况预读与候选动作预测

场景：`Assets/Scenes/EnemyAvoidanceLab.unity`；Unity 6000.6.8f1。功能入口 `EnemyController.PredictMovement`，预测实现 `EnemyNavigationPrediction.Choose`；[源码关联函数图](../RuntimeFlow.md)。此轮延续少拐点A*，重点修复爬升分支没有统一运动预判、侧向驶入信息不足、仅临近船身才作动作判断的问题。

## 距离与信息

远场预读距离为 `max(80米, 8×舰体水平AABB最长边, max(实际航速,巡航速度)×10秒)`，上限160米。参数分别为 EnemyController 的 Far Look Ahead Distance / Body Lengths / Seconds，近场 Decision Horizon 默认3秒，二者独立。Finch 实测远场约147–148米，绝非只扫描一个身位。当前AABB会随航向变化，范围略有变化。

每次移动决策一次周向 OverlapBox，范围覆盖远场并额外加24米横向驶入余量；固定256个碰撞体，忽略自己、触发器和当前任务目标。保留几何 Bounds、线速度（GetPointVelocity）、角速度和碰撞体身份；普通自由刚体也参与预测。饱和则记录 SensorSaturated 并请求停止，不将未收集到的空间视为安全。

远场沿当前缓存路径最多四段预读；没有路线时查看任务方向。记录最近障碍距离、估计到达时间、威胁名称和 RouteBlocked / CrossingWatch。动态物体只投影最多3秒、平移最多24米、旋转余量最多8米；更远处只代表风险，不是确定的碰撞预报。前方同向且比本舰巡航更快的物体不触发错误让行。远场动态风险可触发 EarlySlow，按到达时间提前渐进调速，调速动作重新经过近场检查。静态 RouteBlocked 为前方路线信息，静态路线选择继续由已有A*/升降规划负责，未新增全局地图搜索。

近场由当前速度出发，模拟有限转向、速度变化和垂直升降，12段连续相对扫掠；默认继续动作安全时只评估一个候选，远场调速时两个。存在近处风险时最多比较继续、40%速度、等待、左右35°和70°七个动作，兼顾进度、净空与短期绕行方向保持。A*/升降已承诺的静态路线仅比较降速/等待，避免两个绕行器争抢方向；动态威胁仍可绕行。少量初始包络重叠允许单调脱离，薄墙通过连续段交点检测，不仅检查12个离散端点。

预测通过所有爬升、A*、直达和守卫返航分支后执行。明确选择预测让行时允许推进器反推消减动量；这是有意调整此前“遇障一律禁止反推”的规则，普通即时避障仍禁止反推。预测器不写刚体速度、不瞬移；推进能力和响应误差仍限制预测准确度。无安全候选记录 NoSafeCandidate，不能将其解释为安全保证。

## 可视化、记录与入口

面板同时显示远场预读米数、障碍距离/到达秒数/名称、近处碰撞时间、动作、观察数和耗时；Scene/Game Gizmos 青色水平圆表示远场范围，原盒线继续表示近处BoxCast。路线仍为橙色。

EnemyNavigationSnapshot 增加 predictionRevision、远场范围/障碍/到达时间、原动作及选定动作TTC、威胁运动速度、观察/运动物体数、候选数、分段检查数、预测耗时及制动标记。既有记录器自动落盘；按predictionRevision去重可统计实际被采样到的决策。字段增量兼容原记录，不改玩家存档。

```text
Assets/Scripts/Features/Combat/Enemies/
├── EnemyController.cs                 驾驶协调、Inspector参数、推进器输入
├── EnemyNavigationPrediction.cs       周向感知、远场路线预读、近场候选动作
├── EnemyNavigationSnapshot.cs         记录与面板契约
└── Testing/
    └── EnemyLabPanel.cs               同时显示远近场信息
Assets/Editor/Combat/Validation/
├── EnemyPredictionPlayChecks.cs       23项隔离物理预测验证
└── NavigationCombatPlayChecks.cs      75项驾驶/寻路/炮塔回归
```

## 实船结果

内置 Finch、原有六路线顺序，3倍时间速度，固定物理步长0.02秒，原22米水平到达半径及2秒等待。每轮部署实体ID和物理差异会导致偏差，不是确定性统计实验。

| 路线 | 上轮交付秒 / 墙体接触 | 远场第一轮秒 / 墙体接触 | 最终轮秒 / 墙体接触 |
| --- | --- | --- | --- |
| 单墙 | 20.49 / 0 | 20.72 / 0 | 20.72 / 0 |
| 顶棚U型 | 20.97 / 0 | 20.99 / 0 | 21.11 / 0 |
| 高墙绕行 | 32.29 / 0 | 34.99 / 0 | 31.77 / 0 |
| 下穿 | 23.10 / 0 | 23.08 / 0 | 23.08 / 0 |
| 城区东西 | 69.12 / 5 | 58.34 / 0 | 62.97 / 0 |
| 城区北线 | 40.73 / 0 | 40.78 / 0 | 40.89 / 0 |

远场两轮12次均到达、墙体接触0；初始单墙仍可能有地面接触，不能称全程零接触或覆盖所有动态相位。最终轮城区东西真实记录 EarlySlow 17次，最远约49.66米就提前调速，按6m/s约8.28秒后到达风险区；近场预测窗口当时未判碰撞。高墙11次实际转向，与上一轮相近；不宣称所有路线转向都减少。

原始证据为 `Temp/EnemyAvoidanceLab/efficiency-perception-far.json`、`efficiency-perception-final.json` 及每项directory下的metadata/timeline/result；完整原始会话在 `Application.persistentDataPath/NavigationLabRecords/`，不提交生成日志。上轮为efficiency-anticipation-clean。实验版perception-v1/v2曾把任务目标舰当障碍而反复绕行，高墙耗时约57–67秒；最终排除当前任务目标，未将实验回退当作交付成果。

## 性能与验证边界

最终轮按10Hz快照中 predictionRevision 去重，预测器耗时包含一次物理采集、远场预读及近场候选，**不包含A*、其他避障和整个FixedUpdate**。单墙/顶棚/高墙/下穿平均约0.13–0.15ms；城区东西平均0.333ms、P95 0.821ms、最大1.108ms，城区北线平均0.286ms。城区最多观察39个碰撞体、单次最多3099段×物体检查；存在未被10Hz采样捕获的峰值可能，不作多舰帧率承诺。

- 固定预算：每次决策最多256物体、7动作×12段近场检查和4段远场检查；复用数组，无候选集合逐次分配。动作物理更新保持原调度，正常至少0.5秒一次决策。
- 23项 EnemyPredictionPlayChecks 实际Play通过，含侧向驶入、相对碰撞TTC、安全替代、远离物体、任务规则边界、动量转弯、薄墙、50多米远墙、10秒外到达预读、远场提前调速、8身位/160米上限和传感器饱和。
- 75项 NavigationCombatPlayChecks 实际Play通过，新增预测让行允许推进器制动、结束后恢复普通无反推策略。CLI首次请求曾超5秒，但通过最终生成报告及Pending结果核对，不把超时当通过。
- Unity导入/编译及实际Play成功，新增源码有Editor生成的meta；项目文件由Editor刷新并包含新脚本。函数图和真实方法导航同步。
- `dotnet run --project Tools/CodeReadability -- --check .`：437脚本、0格式差异、0错误；`dotnet build HY-Sandbox.sln --no-restore --nologo`：0错误、37条既有警告；本任务文件diff空白检查通过，已有场景/布局修改保留。
- 未验证独立Player、全部蓝图、全部12布局/起终点/动态相位、多舰压力、GC实测。恒速/有限转向预测无法保证所有突变、旋转复杂体和长期运动精确；远场距离不是整个范围内任意航路均已验证。
