# Siege guidance feedback — 2026-11-16

## Change and contract
Hangar assist remains the sole force writer during guidance. Thruster guidance ownership suppresses ordinary input/hover force callbacks, publishes allocated powered output to baked nozzle VFX and releases it at completion/stop. Universal gimbals follow guided drive direction. No new runtime components or asset GUID changes.

## Evidence
- Before: real hangar 29 checks passed; launch 13.2801 s / return 21.1203 s. Live sample showed powered engines with thrust=0 and lastThrustValue=0 during guidance.
- After: real Main Play fixture 34 checks passed, zero errors; samples observe powered drive and lift output plus positive current nozzle intensity in launch and return; docking releases ownership/output. Launch/return routes and speed configuration unchanged in this commit.
- Isolated return/economy: 18 checks passed, including powered docking, power-loss grace, scrap completion and exactly-once rewards.
- Hover lifecycle: 16 checks passed, including no-cockpit, power loss, disable, transfer and shortest heading correction.
- Unity 6000.6.8f1 compilation completed without errors. Console ground truth zero errors.
- dotnet build HY-Sandbox.sln --no-restore: zero errors, 37 obsolete object-query API warnings in existing files.

## Limits and risk
Automated intensity sampling is not a human visual inspection. Full battle-return, damaged/full-size hull, multiple bays and regroup during guidance remain to be checked. No speed improvement is claimed here. Guidance assist still has its existing free-axis physics contract.

## Reproduction
Open saved Main only, run Tools/HY-Sandbox/Siege/Validate hangar fleet in Play. Report Temp/Hangars/play-checks.json, telemetry Temp/Hangars/flight-telemetry.json. In Main Play, SiegeReturnChecks.Run() and HoverControlPlayChecks.Run() (internal Editor fixture).

## 后续提速 P3a

同夹具完整导引elapsed：出库7.95999秒，返航7.31999秒；原基线13.2801/21.1203秒。采样间隔约1秒，不能把首尾普通采样差7.05秒当成完整导引时间。真实机库34项及隔离返航18项通过。补偿linearDamping仍受现有有电drive/lift上限；预设12m/s、12m/s²，不修改普通驾驶或刚体阻尼。已对准自身前门走廊取消绕顶往返，保留门外精确对齐、门打开、停泊位置/速度/角度门槛。非对准路径保持高空绕行。

SiegeFlightAuthoring将五场景原8/6预设迁移至12/12，备份原文件并保留非默认参数。满尺寸多舰、障碍侵入入口、完整交战返航及人工动态视觉仍待验证。
