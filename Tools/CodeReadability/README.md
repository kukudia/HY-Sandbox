# 可读性与导航工具

需要 .NET SDK 9.0.318（global.json固定本地/CI一致的版本，项目工具直接引用该SDK自带Roslyn，不新增NuGet依赖）；导航生成需要Python 3。Unity工程仍使用自身的C#9编译规则。升级SDK时同步global.json并重新核对格式基线，避免Runner选中.NET10造成Roslyn运行时冲突。

从仓库根目录执行：

```powershell
dotnet run --project Tools/CodeReadability -- --write .
python Tools/CodeReadability/generate_navigation.py
dotnet run --project Tools/CodeReadability -- --check .
dotnet run --project Tools/CodeReadability -- --duplicates .
dotnet run --project Tools/CodeReadability -- --check-tools .
python Tools/CodeReadability/check_tool_sources.py
python Tools/CodeReadability/generate_flow_browser.py
```

`--write`整理Allman、4空格、控制流块、长参数列表和CRLF。扫描范围仅Assets/Scripts与Assets/Editor；保留实际UTF8/BOM/GB18030。写入前核对非括号token，拒绝改业务词法。`--check`还检查顶部职责说明、替换字符和RuntimeFlow符号；文件行数仅提示人工审查，不是质量验收。

`--inventory`输出真实脚本/方法/公开或序列化字段JSON，可用于迁移核查；不是完整ABI分析器。解析符号分别读取目标Editor实际生成的Assembly-CSharp.csproj和Assembly-CSharp-Editor.csproj（当前Unity6000.6 Windows Editor，含版本链、包开关及每个程序集不同的.NET配置），并拒绝缺少UNITY_6000_6_OR_NEWER的过期工程文件。Windows Player和其他平台的排除分支仍须通过真正的目标构建验证，不能将单个Editor语法树称为全平台检查。

`--duplicates`只读扫描生产方法，报告不少于50个token且方法体token完全相同的候选，忽略注释/空白；排除Editor、Validation和整文件`#if UNITY_EDITOR`参考基准。它不归一化变量名、不推断语义、不自动删除代码；候选应核对调用者和领域差异，零候选也不能证明没有语义重复。新增消费者应先复用已有机制，再显式表达策略差异。

`--check-tools` / `--write-tools` 对 Blueprints 的9份 C# eval_file 使用 Roslyn Script 模式，不能把顶层脚本当作普通 Unity 源文件编译。Python 检查只在内存检查语法、显式 main 入口及生成器模板，不执行资产/存档写入。外围工具入口见 Docs/ToolIndex.md。Python 默认使用 Black 风格；不要求 C# 大括号风格。

`generate_flow_browser.py` 从 RuntimeFlow.md 同源生成 HTML，可切换图、缩放和点击源码。打开时需要访问固定 Mermaid 11.12.0 CDN；离线可阅读 Mermaid 原文和函数索引。

`generate_navigation.py`中的图规格表达实际主链，更新功能时同时修改它；生成Docs/RuntimeFlow.md的可点击源码节点和Docs/CodeIndex.md的全部方法签名/行号。同时从 Roslyn 清单和 ToolIndex 生成源码文件及文件夹树，呈现每层主要代码与子功能，按真实层级稳定排序，排除第三方与缓存；新增、移动或删除代码目录后重新生成。生成器拒绝不存在的符号或 ToolIndex 源文件。静态符号有效不证明图语义或执行顺序，协程/通知使用虚线，人工审查调用者。

GitHub检查仅验证格式、说明和导航，不能替代Unity导入、Play Mode或Player构建。Unity探针菜单及报告详见DEVELOPMENT.md。

实际物理分类边界见 [CodeLayout](../../Docs/CodeLayout.md)。目录树记录已落地的源文件布局，不能替代真实迁移。
