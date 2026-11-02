# 建筑蓝图共用文件仓（2026-11-02）

## 修改

建筑沿用EnemySpawner的文件仓模式：通过SaveManager取得用户蓝图来源，再由消费者执行生成规则。`SaveManager.GetBuildingBlueprintFiles()`在Main返回现有BuildingFiles实例，保留测试或其他调用者注入的文件仓；独立WorldLab无SaveManager时，使用SaveManager同一buildingBlueprintDirectory规则创建BlueprintFileStore。

WorldSessionManager不再向WorldBuildingLibrary传入自己拼接的目录。WorldBuildingPalette删除重复的目录扫描与JSON解析，改为调用同一个WorldBuildingLibrary.Load。共用加载过程负责有序枚举、4MB文件上限、解析、逐文件错误隔离、用户同名覆盖和有效内置模板回退。World保留Resources内置建筑和异步资源预热、逐文件让帧、非空/平台约束；WorldLab保留场景Catalog内置列表和编辑放置范围。既有场景、资源GUID、存档格式和Enemy逻辑未改动。

SaveManager在开始任务前已有字段顺序的用户修改；工作区保留，提交只纳入新增访问入口和路径共用改动，不包含用户字段重排。恢复分支为`codex/building-catalog-baseline-20261102`；原用户差异和SaveManager原文件保存在本机Temp/BuildingCatalog。

## 已执行验证

- Unity6000.3.11f1导入/脚本编译通过，compilationFailed=false；Main最终恢复干净Edit Mode，临时开启的runInBackground已恢复false。
- [9项隔离检查](2026-11-02-building-catalog-isolated.json)全部通过：Main注入文件仓身份、SaveManager名称列表、内置/用户合并去重、损坏/非法/超大/独占锁文件隔离、非法用户覆盖回退、世界平台约束失败回退、WorldLab共享来源、读取不改源文件、无管理器的独立WorldLab文件仓。可复现脚本见 [checks.cs](2026-11-02-building-catalog-checks.cs)，在Main干净Edit Mode用unity command eval_file执行，夹具只写独立临时目录并恢复单例。
- 真实Main世界Play回归完成50项检查，包括建筑预热、蓝图库合并、真实建筑生成、地形列流式加载、货仓与损伤跨卸载恢复、退出清空和新会话。随后在生产死亡路径“恢复Preparing且Time.timeScale==1”的组合断言失败，完整套件未通过，未验证失败后的剩余路径。见 [原始结果](2026-11-02-building-catalog-world.json)。没有据此宣称死亡流程无问题，也未在本任务改动死亡逻辑。
- 首次世界回归被测试中“所有用户JSON都必须进入生成池”的旧假设阻断：当前目录含空default_building，世界本来就跳过空建筑。测试改为只要求有效非空用户建筑进入生成池，再运行得到上述50项结果；未修改或删除用户文件。
- 35份用户Saves/EnemyBlueprints/BuildingBlueprints JSON在世界回归前后SHA256一致。
- CodeReadability335脚本，0格式差异/0错误；Python工具9份语法/入口及模板检查通过；43张函数图和HTML导航已同步。
- dotnet build HY-Sandbox.sln --no-restore：0错误、4项既有警告。锁定SDK9.0.318本机缺失，从项目外使用已安装9.0.320执行绝对路径工程，没有改global.json。
- git diff --check通过。

未运行独立Player构建、完整自然游玩或性能对照；不以静态检查替代这些范围。
