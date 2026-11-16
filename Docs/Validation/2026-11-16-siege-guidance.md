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
