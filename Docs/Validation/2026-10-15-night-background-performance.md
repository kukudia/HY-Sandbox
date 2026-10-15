# NightDesk 下世界性能采样基线

本轮修复的是 Editor 性能验证工具，不是生产世界算法；不据此宣称游戏性能提升。

## 问题与修复

原 WorldSessionPerformanceChecks 在后台运行关闭时，即使调用 QueuePlayerLoopUpdate，NightDesk 持有焦点后真实 Play 帧仍停在第 2 帧。旧超时检查位于“有新游戏帧”的判断后方，停帧时超时也无法结束。

探针现临时启用 Application.runInBackground，在完成、中止、超时后恢复此前后台运行、Profiler 开关、Editor 采样和 CPU 采样设置。检查真实时间早于帧号判断，确保没有新帧也能收尾。启动拒绝编译中、切换中、非 Main、脏场景和非法报告文件名。没有更改项目永久后台运行设置、生产脚本、世界资产或玩家数据。

## 实际执行

- 原探针后台停帧路线已中止，不算完整性能结果。
- 更新后首次路线推进 1,838 真实帧并完成，期间诊断 eval 干扰一次 travel 帧间隔至约 998ms；该组仅证明遮罩下真实运行，不作性能基线。
- 中途主动退出 Play：记录 `status=failed` 和明确中止原因，恢复后台/Profiler 设置并解除挂起状态。
- 人为将 deadline 设为过去，同时将记录帧号设为当前帧并关闭后台运行：无新帧分支也捕获 TimeoutException，记录 `status=failed` 并完成恢复。这是预期失败路径，不计为完整路线通过。
- 最后独立重跑完整固定种子路线；不在路线期间插入 Editor eval、截图或编译。完成 1,830 真实帧，最终 `status=completed`。结束后 Main 不脏，Play 已退出，后台运行/Profiler 均恢复原 false。
- 用户持久化 JSON 逐项 SHA256 对照；结果在本次提交前核对。

## 无诊断插入的 Editor 基线

Unity 6000.3.11f1，当前 Editor 为 Direct3D12，Game View 1450×975；固定世界种子 14721、28 栋建筑、1,967 模块；测试载具为隔离单驾驶舱构造。随机敌机关闭，真实建筑保留。NightDesk 持有焦点，开启 CPU Profiler，没有 Deep Profile。帧间隔包含 Editor 调度，不是 GPU 时间或正式 Player 性能。

| 阶段 | 帧数 | 中位/ms | P95/ms | 最大/ms | 超过50ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| 世界准备 | 1,020 | 18.24 | 28.70 | 72.37 | 12 |
| 进入与稳定 | 267 | 16.24 | 26.30 | 75.38 | 1 |
| 静止 | 240 | 17.00 | 27.97 | 33.31 | 0 |
| 跨区块移动 | 300 | 16.79 | 27.69 | 81.31 | 1 |
| 同步退出及下一帧 | 1 | 55.64 | 55.64 | 55.64 | 1 |

静止末尾 100 帧主线程包含式平均：Main Thread 16.693ms、EditorLoop 9.580ms、PlayerLoop 6.805ms、PowerTransmissionDevice.Update 0.939ms。这些是有重叠的包含式统计，不可相加，也不能据此认定每项都是独立瓶颈。

原始完整输出见 [性能采样](2026-10-15-night-background-performance.txt)。startup 只有 2 个早期 Editor 帧，未用于判断首次 Player 初始化。当前 D3D12 Editor 这次路线完成，不代表修复了先前 D3D12 原生崩溃；Windows Player 仍使用已验证的 D3D11 设置。

## 后续门槛

跨区块和退出仍有单次峰值。下一单元优先在正式 D3D11 Player 增加固定种子与可重复路线，做阶段定位和长时真实交火/回收；不能拿本次 D3D12 Editor 与此前不同种子、分辨率的 D3D11 Player 直接比较。当前未测 GPU、4K、最大载具、多 GPU 或整晚压力，不宣称零卡顿或无泄漏。
