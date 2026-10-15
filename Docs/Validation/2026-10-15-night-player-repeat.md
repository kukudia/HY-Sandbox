# 固定种子与重复 Player 会话验证

## 本次实现

WorldPlayerSmokeChecks 仍仅在显式 `-world-smoke-report` 下创建。新增 `-world-smoke-seed <int>` 和 `-world-smoke-cycles <1..10>`；省略参数保持原有随机世界、单次测试行为。固定种子在场景 Start 之前写入本次 Player 进程内的配置，不写入资产或用户存档。

每轮复用正式游玩入口，临时单驾驶舱载具脚本移动 200m，然后通过 CompletePlayEnd 清空世界；等待世界 Ready 和原载具渐进恢复都完成，再重复下一轮。绕过的是返航货仓写盘入口，不绕过会话清理。报告新增请求/完成轮数、每轮种子、每轮 Unity 已分配内存，以及各阶段 P99 与超过 50ms 的帧数；多轮阶段名带 cycle 编号。

实现集中在一个显式验收组件：参数与报告、完整路线及正常退出属于同一测试生命周期；没有为测试拆出生产管理器或修改正常游戏世界行为。源码关联图见 RuntimeFlow 的“显式 Player 世界验证”。

## 构建与执行

- Unity 6000.3.11f1 非 Development Windows 构建，默认 Direct3D11。独立输出 `Builds/WorldPerformanceValidation/HY-Sandbox.exe`；不覆盖用户正在使用的 `Build/HY-Sandbox.exe`，不递增版本、不上传二进制。
- 原构建工具的延迟回调停在 scheduled，检查确认并未构建。撤销该回调后，通过 CLI detached job 直接调用同一个 BuildNow 入口；最终 Succeeded，0 errors、1 warning（Pipeline Player 联动配置缺失的既有提示），实际构建约 8.48 秒。没有把 scheduled 当成功。
- 1280×720、窗口模式、上限120FPS、vSync关闭、seed14721、cycles3、不截图；NightDesk遮罩持有焦点，用户原有Editor和旧Player保留。外部建筑库使用本机已有内容，本次未改写。
- 每轮 28 栋/1967 模块，载具从 (16,11,16) 移至 (216,11,16)。3/3轮完成，全部种子14721、16种检查条件通过、错误数组为空，进程正常退出码0。原始报告见 [三轮数据](2026-10-15-night-player-repeat.json)。
- 独立非法参数测试：cycles0 被拒绝，0轮完成、failed报告、退出码1，错误为 Invalid world smoke cycles argument。此项是预期失败路径，不能计为正常路线通过；见 [参数失败报告](2026-10-15-night-player-invalid.json)。
- 28份用户JSON逐项SHA256均不变；Main仍不脏且不在Play。232脚本可读性检查、源码导航、静态构建与差异检查通过；dotnet为0错误、4个既有警告。

## 本次数据与限制

| 轮次 | 移动P95/ms | 移动P99/ms | 移动最大/ms | 移动超过50ms | 退出单帧/ms | 移动后Unity已分配内存/bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 9.49 | 10.29 | 97.87 | 1 | 62.39 | 153733406 |
| 2 | 9.46 | 9.68 | 18.98 | 0 | 60.15 | 153927919 |
| 3 | 9.47 | 9.67 | 24.41 | 0 | 59.72 | 153421559 |

静态世界配置可重复，性能数字仍受系统调度、Editor和用户旧Player等影响；这不是严格前后优化对比。本次没有优化生产算法，不宣称性能提升。内存值是Unity总已分配内存的阶段快照，不是OS工作集、峰值、退出后常驻量或长期无泄漏证明。

脚本路线使用临时无敌单驾驶舱载具，随机敌机关闭，保留真实建筑；不是用户实际驾驶、最大载具、长时密集交火或完整回收返航验收。初次移动约98ms和每轮退出约60ms的尖峰仍存在。下一步应围绕这些阶段定位成本，而非重复无变化的整套回归。

## 复现

从项目根启动独立包，给报告指定一个已存在的输出目录：

```powershell
& './Builds/WorldPerformanceValidation/HY-Sandbox.exe' `
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 `
  -world-smoke-report '<现有绝对目录>/repeat.json' `
  -world-smoke-seed 14721 -world-smoke-cycles 3
```

正常游戏启动不传 `-world-smoke-report`，不会自动接管载具或退出进程。截图选项仍可用，但不与计时采样混用；多轮截图沿用原命名，会覆盖前一轮同类截图。
