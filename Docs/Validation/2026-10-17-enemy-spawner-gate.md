# EnemySpawner 会话门控验证

## 范围

本轮收束普通建造游玩与世界据点会话之间的敌机生成职责。普通场景保留 `EnemySpawner` 的配置间隔调度；`Main` 的 `WorldSessionManager` 负责世界探索和有限据点驻军，因此世界会话不调用全局计时刷怪。退出游玩时清理生成计时器和锚点，避免下一次出发复用旧状态。

## 已执行验证

- Unity Editor `6000.3.11f1` 通过 Pipeline 连接到 `D:/git_projects/Unity/HY-Sandbox`，打开 `Assets/Scenes/Main.unity`，场景初始和结束均为未保存脏状态。
- 通过 Editor 命令进入 Play Mode，等待约 20 秒后退出；随后读取 Unity 错误日志，结果为空。
- `dotnet build HY-Sandbox.sln --no-restore`：0 个错误，3 个既有依赖版本冲突警告。
- `git diff --check`：通过。
- `dotnet run --project Tools/CodeReadability -- --check .`：项目既有 `MainUIPanels.cs` 格式差异仍存在；本轮改动没有新增格式错误。其余为大脚本 review 提示，不是失败。

## 边界

本轮没有运行独立 Windows Player 的长时间自然战斗，也没有证明普通非世界场景的计时刷怪体验；上述内容需要在对应场景/Player 路线中补做。Main 世界据点的既有多轮 Player 证据仍见 `Docs/Validation/2026-10-16-outpost-endurance.md` 和相关报告。
