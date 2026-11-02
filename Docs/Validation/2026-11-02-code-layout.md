# 源码递归功能目录迁移（2026-11-02）

## 结果与边界

已按 [CodeLayout](../CodeLayout.md) 实际分类187份运行时脚本、88份Editor脚本，共275份；新增87个真实Unity文件夹及其meta。World根目录从46份脚本收敛为WorldSessionManager和WorldGenerationSettings，其他职责递归进入地形、建筑、据点、流式加载、持久化、表现、实验场和验证目录。其他较大功能目录同样分类，小而内聚的目录保持原状。

恢复点为 `codex/code-layout-baseline-20261102`，基线提交 `beeff9b`。完整旧/新路径、GUID、源码和meta的SHA256见 [迁移清单](2026-11-02-code-layout-moves.json)。Skill在本机唯一时间戳备份目录中保留原文及原路径/哈希清单，再更新为“实际物理目录递归分类，父目录保留主入口，文档树不能替代迁移”。

使用连接的Unity 6000.3.11f1 AssetDatabase.CreateFolder/MoveAsset迁移，未改变命名空间、类名、字段、游戏逻辑、存档或场景布局。唯一源码内容调整是SpaceKitCurator的VfxEffect依赖白名单路径；现有.meta原始字节保留。Unity自动刷新两个已跟踪csproj，只变更275项Compile文件路径，运行时229项及Editor111项总输入集合保持。DEVELOPMENT、当前说明、RuntimeFlow与HTML导航和CodeIndex均同步。

## 验证

- 磁盘完整性：275份脚本迁移前后SHA256一致（上述一处路径调整除外），275份原meta字节一致，345份已跟踪unity/prefab/asset哈希一致。
- Unity Editor：275个新路径均加载为MonoScript且GUID匹配；274个具有对应编译类型，另1个为无同名class的类型定义文件；54个生产Prefab及Main场景无Missing Script。最终Main为干净Edit Mode。
- Unity导入与脚本编译成功，compilationFailed=false。首次迁移调用因CLI主线程5秒上限返回超时，但落盘终态为275/275，之后独立通过Editor及磁盘核对；不把超时响应本身计为成功证据。初次Play探针被工具路径修改引发的重编译中断，编译稳定后重新执行。
- SiegeRulesChecks：19项通过。
- Main ArchitecturePlayProbe：68条检查中67条通过、1条失败，完整套件未通过。失败为旧测试要求瞬态光束/闪光到期销毁，但现有BeamFade/LightFade通过TransientEffectPool回收；失败测试及相关生产文件与基线内容一致。记录见 [Play原始结果](2026-11-02-code-layout-play.json)。未为目录任务修改生产特效或旧测试断言。
- CodeReadability：335脚本，0格式差异、0错误；Python工具9份语法/入口及模板检查通过；skill quick_validate通过。
- dotnet build HY-Sandbox.sln --no-restore：0错误、4警告（既有依赖冲突和Profiler过时API）。本机锁定9.0.318缺失，从项目外使用已安装9.0.320执行绝对工程路径，global.json未修改。
- git diff --check通过；源码树与43份函数图/HTML链接按最终真实路径重生。

本轮没有正式Player构建、自然游玩或视觉/性能验收。源码目录与契约验证已完成；旧特效生命周期测试失败单独记录，不宣称全功能回归通过。
