# HY-Sandbox 项目规则

- Unity版本：6000.6.8f1；URP和Input System以Packages及ProjectSettings实际配置为准。
- 运行时自有源码：Assets/Scripts/Features；制作/验证工具：Assets/Editor按功能分目录；不整理第三方源码。
- 同时读取用户 engineering-quality、unity-editable-workflow skill和 Assets/Scripts/AGENTS.md。旧 code-readability/game-code-readability 仅为兼容入口，共用流程保存在skill，本文件只规定项目映射。
- DEVELOPMENT.md 是当前功能入口与验证状态；Docs/RuntimeFlow.md 是源码关联函数图；Docs/CodeIndex.md 是自动生成真实方法导航；Docs/DevelopmentHistory.md仅历史。
- Docs/ProjectRiskRegister.md 保存当前风险、行为契约及待验证项；提交/推送前按实际影响更新风险、证据与缺口，不重复创建全项目报告。
- 尽量避免逐个遍历基础部件（Block、Durability、Collider、Renderer、推进器等），包括层级查询、缓存列表循环和 LINQ。优先按单位处理、在结构变化时更新名单或复用同一步结果。新增或修改此类遍历必须同步登记到 Docs/ProjectRiskRegister.md：注明源码入口、触发频率、遍历范围、保留原因、失效条件及验证证据；使用 NonAlloc 或缓存列表不能免除登记。
- 修改功能同步更新 DEVELOPMENT 和受影响函数图；整理格式后刷新导航行号。
- 检查：dotnet run --project Tools/CodeReadability -- --check .；dotnet build HY-Sandbox.sln --no-restore；git diff --check。新增或移动 Assets 后用连接Editor导入，不能以静态编译代替Play验证。
- 已有脚本GUID与序列化契约随迁移保留。只提交本任务文件；完成验证后检查远程差异再推main，不强推、不夹带无关提交。
