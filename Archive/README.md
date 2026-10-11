# 历史快照归档

当前 Unity 工程位于仓库根目录，开发与重构范围为根目录的 `Assets/`、`Packages/`、`ProjectSettings/`。本目录保存历史材料，不作为第二套活跃实现，不进入当前工程的脚本编译、可读性格式化或运行时函数图。

## LegacyPrototype/HY-Sandbox

2026-10-11 将原仓库内的 `HY-Sandbox/` 整体迁移到 `Archive/LegacyPrototype/HY-Sandbox/`。

- 原快照只有 `Assets/`、`Library/` 和 `.vsconfig`，缺少 `Packages/`、`ProjectSettings/`，不是可直接打开并可靠运行的完整 Unity 工程。
- 保留全部 811 个原文件、原始编码与换行、所有 `.meta` GUID 和旧 `Library/` 内容；未重新格式化旧源码，未删除缓存或其他历史数据。
- 旧 `Library/` 只在本地归档保留并受 `.gitignore` 排除；版本库保存历史资产、源码和迁移清单，不重新提交缓存。旧缓存也可从迁移前 Git 历史恢复。
- 其中 `Assets/` 为 150 个文件，含 11 个历史 C# 和 84 个 `.meta`；`Library/` 为 660 个文件；另有 `.vsconfig`，总大小 129,888,860 字节。
- 当前工程源码、资源配置、构建工具及文档未发现依赖原嵌套目录的相对或绝对路径。旧资产保留内部 GUID 关系，但旧 `Library/` 是历史缓存，不能视为新路径下仍有效的 Unity 导入结果。
- 迁移前后逐文件 SHA-256 和字节数全部一致。路径映射与哈希见 [MigrationManifest.json](LegacyPrototype/MigrationManifest.json)。

需要研究旧实现时只读打开归档文件。需要运行旧版本时复制到独立工作区，再根据 Git 历史恢复匹配的 `Packages/`、`ProjectSettings/` 和 Unity 版本并重新导入；不要直接将旧脚本或 `Library/` 合并到当前 `Assets/`。

如需撤销本次归档，在确认原位置 `HY-Sandbox/` 不存在且 Unity 未使用目标目录后，将 `Archive/LegacyPrototype/HY-Sandbox/` 整体移回仓库根目录。完整内容与 GUID 未更改，迁移清单可用于核对。
