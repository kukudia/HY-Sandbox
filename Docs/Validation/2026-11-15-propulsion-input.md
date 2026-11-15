# 模拟输入与实际推进喷口回归

## 根因与实现

用户反馈Type 03直航仍不断调整。旧 `EnemyController.GetVelocityInput` 把参考速度减实际速度的误差向量直接作为推进输入；`UniversalThruster` 对任意非零输入输出满推力。导航锁定方向并不能阻止小速度误差驱动喷口反复侧向甚至反向转动。此前直航锁定记录只验证了导航意图，未覆盖这个执行层问题。

- `Flight/FlightPropulsionControl` 统一转换参考速度：独立朝向、比例油门和显式制动；正常巡航只沿锁定航向调速，不为小横向误差微调。显著转弯才抵消旧横向动量，停车、安全减速和明显超速允许反推，带迟滞避免零误差附近翻转。
- 根据刚体质量/阻尼、主推进器实际方向和通用推进器已确认供电比例估算油门。Type 03有一个喷口缺电，不能把它计入可用推力；完全缺电仍保留启动请求，实际供电和施力由原系统执行。
- `ControlUnit.SetPropulsionInput` 在零油门时仍可保留推进朝向；旧 `SetMovementInput` 和公开输入字段保留。停用、无驾驶舱、PlayEnd、玩家输入清空走同一入口。已有RefreshChildren结果复用为推进器缓存，不逐物理步扫描层级。
- `UniversalThruster` 按输入幅值输出；喷口未对准时既不申请新推力，也不让变化率历史的残余推力扫向侧面。仍保留转速、最大推力、推力变化率和电网门控，未直接写刚体速度。
- Debug快照/竞技场面板分别显示导航限速比例、推进方向、油门与反推制动，避免把导航限速误认为真实油门。功能入口保留在Flight，诊断工具归入Editor/Combat/Diagnostics。

恢复点：`codex/propulsion-input-base`（6d3726b）。保留用户场景、悬浮Prefab和蓝图源数据，未写入玩家存档。

## Type 03实际物理对比

Unity 6000.6.8f1，EnemyAvoidanceLab/Open flight，正常时间倍率，真实生产AI、供电、质量、推进器与碰撞。只读加载Saves / Type 03。两轮均以正式arrived结果结束，距离判定包含接敌范围，不表示贴到目标中心。

| 指标 | 修改前 | 最终实现 |
| --- | --- | --- |
| 到达时间 | 30.90秒 | 21.08秒 |
| 累计航程 | 184.57米 | 119.16米 |
| 接触 | 2次地面接触 | 0 |
| 直航样本数 | 175 | 94 |
| 实际喷口与锁定航向最大水平夹角 | 179.95度 | 小于0.001度 |
| 实际喷口与锁定航向平均水平夹角 | 71.84度 | 小于0.001度 |

直航窗口：起飞4秒后且水平距目标超过50米；实际采样第一台通用推进器的model.forward，并与导航方向比较，未只统计期望航向。单次对照用于确认根因与实际改善，不代表统计性能保证；转弯/接敌阶段允许改变方向。

证据：`Temp/PropulsionInput/type03-before.json`、`type03-powered.json`。原始完整试验目录分别为 `NavigationLabRecords/20261115-145223-821-c1d81b/trial-0003` 和 `20261115-151607-386-24dfaf/trial-0003`，位于Application.persistentDataPath下。

## 回归验证

- `PropulsionInputChecks.Validate`：比例油门、零油门保留朝向、未对准不申请推力、20次交替微小速度/横向误差保持直航、真实转弯抵消旧动量、停车/安全减速反推及旧接口清空，全部通过。
- `FlightCourseChecks.Validate`：原直航锁定、导航驾驶权不触发微调、明显转弯与承诺时间、横偏恢复、紧急接管和参考航向限转速，全部通过。
- `ActivePowerPlayChecks.Run`：32项既有真实组件检查通过，覆盖负载请求、断电恢复、主推进器、货物、维修/回收Bot。并非全部舰型断电飞行验收。
- Finch/Open flight：24.00秒、115.19米；Manta/Open flight：27.61秒、116.39米；均正式到达且零接触。`Temp/FlightCourse/propulsion-powered.txt`，原始trial-0007、trial-0009。
- Manta/Single wall：23.27秒、91.97米、零接触，正式到达；`Temp/PropulsionInput/wall-result.json`，原始trial-0005。

- Random5v5Arena：18项实战追踪/切换/清理/正式换轮检查通过，现场报告包含独立Drive、Throttle、Reverse brake和Navigation limit。证据 `Temp/PropulsionInput/arena-result.json`、`Temp/ArenaNavigation/live-report.txt`。该短轮回归不代表长期对战到达率验收。
- CodeReadability：453脚本、0差异/错误；`dotnet build HY-Sandbox.sln --no-restore --nologo -p:UseSharedCompilation=false`：0错误、37条既有警告。Unity导入/编译成功，最后Console ground truth为0错误（历史捕获缓冲仍含先前编译工具错误）。
- 本次暂存范围 `git diff --cached --check` 通过；全工作区检查仍包含用户既有布局文件的尾随空格，未清理或提交。

## 工具与边界

Unity曾出现Roslyn编译服务堆栈溢出及Bee后端错误；关闭编译服务、重新编译后已恢复，确认Editor载入包含AvailableAcceleration的新程序集且compilationFailed=false。失败尝试不计入运行通过。

完整蓝图池、长时间密集多舰压力、复杂连续窄弯、不同供电变化全过程和Player构建未全量验证。记录的是当前场景实测，不能用静态编译替代实际效果。
