# HY-Sandbox 项目规则

- Unity版本：6000.3.11f1；URP和Input System以Packages及ProjectSettings实际配置为准。
- 运行时自有源码：Assets/Scripts/Features；制作/验证工具：Assets/Editor按功能分目录；不整理第三方源码。
- 同时读取用户 game-code-readability、unity-editable-workflow skill和 Assets/Scripts/AGENTS.md。共用流程保存在skill，本文件只规定项目映射。
- DEVELOPMENT.md 是当前功能入口与验证状态；Docs/RuntimeFlow.md 是源码关联函数图；Docs/CodeIndex.md 是自动生成真实方法导航；Docs/DevelopmentHistory.md仅历史。
- 修改功能同步更新 DEVELOPMENT 和受影响函数图；整理格式后刷新导航行号。
- 检查：dotnet run --project Tools/CodeReadability -- --check .；dotnet build HY-Sandbox.sln --no-restore；git diff --check。新增或移动 Assets 后用连接Editor导入，不能以静态编译代替Play验证。
- 已有脚本GUID与序列化契约随迁移保留。只提交本任务文件；完成验证后检查远程差异再推main，不强推、不夹带无关提交。
