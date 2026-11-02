# 建筑生成唯一来源验证

日期：2026-11-02。Unity：6000.3.11f1。场景：Main。

## 最终行为

WorldBuildingLibrary只读SaveManager共用BuildingFiles，不再合并Resources模板。WorldLab选择面板也不读取Catalog.blueprints。初始布局保留创作的坐标、朝向和阵营，只按资源名称解析BuildingBlueprints中的有效文件；缺失或无效就跳过。Catalog资源与Resources样例继续用于制作参考和建筑编辑器显式复制，不自动导入用户目录。

会话启动重新读取目录。当前会话已生成的建筑保留快照；删除文件后需重新开始会话，才会清空上一会话的活动/休眠实例。WorldLab已有实例存档恢复不属于生成目录，保留其存档内容。

## 实际验证

- Unity导入/编译成功，最终Console无错误。
- [Editor原始报告](2026-11-02-building-source-editor.json)：11项隔离检查通过，包括空目录无内置回退、仅显式存在的文件进入库、坏/无效/锁定/超大文件隔离、世界平台约束拒绝后无同名内置回退、WorldLab忽略Catalog内容、初始布局跳过缺失名称、删除后刷新不复活、独立WorldLab解析相同目录。
- [Play原始报告](2026-11-02-building-source-play.json)：4项真实Main会话检查通过。先核对真实用户目录与实际世界模板，再注入隔离文件仓，验证唯一测试文件对应真实生成建筑；删除该文件并重建会话后，模板、活动建筑和休眠建筑均为空。使用真实分帧准备、资源预热、地形生成和建筑恢复流程。
- 34个真实用户JSON的SHA256与测试前一致；未修改用户建筑、玩家或敌方文件。现有Assets GUID、.meta、场景和Catalog资产未改。
- 测试结束恢复文件仓，退出Play，不保存测试对象，重新打开Main，场景干净。

## 静态验证

- 可读性：335个脚本，格式差异0，错误0。
- dotnet build：0错误，4个已有警告（Pipeline引用版本、过时Profiler API）。固定SDK 9.0.318本机缺失，从项目外使用已安装9.0.320，global.json未改。
- 43张函数图与335个脚本导航已刷新；9项工具源码检查通过。
- git diff --check通过。

未进行独立Player构建或人工视觉验收。复现入口见[检查脚本说明](../../Tools/Validation/BuildingSource/README.md)。
