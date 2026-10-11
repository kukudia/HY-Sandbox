# 可读性与导航工具

需要 .NET SDK 9.0.318（global.json固定本地/CI一致的版本，项目工具直接引用该SDK自带Roslyn，不新增NuGet依赖）；导航生成需要Python 3。Unity工程仍使用自身的C#9编译规则。升级SDK时同步global.json并重新核对格式基线，避免Runner选中.NET10造成Roslyn运行时冲突。

从仓库根目录执行：

```powershell
dotnet run --project Tools/CodeReadability -- --write .
python Tools/CodeReadability/generate_navigation.py
dotnet run --project Tools/CodeReadability -- --check .
dotnet run --project Tools/CodeReadability -- --check-tools .
python Tools/CodeReadability/check_tool_sources.py
python Tools/CodeReadability/generate_flow_browser.py
```

`--write`整理Allman、4空格、控制流块、长参数列表和CRLF。扫描范围仅Assets/Scripts与Assets/Editor；保留实际UTF8/BOM/GB18030。写入前核对非括号token，拒绝改业务词法。`--check`还检查顶部职责说明、替换字符和RuntimeFlow符号；文件行数仅提示人工审查，不是质量验收。

`--inventory`输出真实脚本/方法/公开或序列化字段JSON，可用于迁移核查；不是完整ABI分析器。配置的预处理符号用于当前Unity6.3 Editor路径；新增平台条件应同步其符号并通过真正的Unity目标编译。

`--check-tools` / `--write-tools` 对 Blueprints 的9份 C# eval_file 使用 Roslyn Script 模式，不能把顶层脚本当作普通 Unity 源文件编译。Python 检查只在内存检查语法、显式 main 入口及生成器模板，不执行资产/存档写入。外围工具入口见 Docs/ToolIndex.md。Python 默认使用 Black 风格；不要求 C# 大括号风格。

`generate_flow_browser.py` 从 RuntimeFlow.md 同源生成 HTML，可切换图、缩放和点击源码。打开时需要访问固定 Mermaid 11.12.0 CDN；离线可阅读 Mermaid 原文和函数索引。

`generate_navigation.py`中的图规格表达实际主链，更新功能时同时修改它；生成Docs/RuntimeFlow.md的可点击源码节点和Docs/CodeIndex.md的全部方法签名/行号。生成器拒绝不存在的符号。静态符号有效不证明图语义或执行顺序，协程/通知使用虚线，人工审查调用者。

GitHub检查仅验证格式、说明和导航，不能替代Unity导入、Play Mode或Player构建。Unity探针菜单及报告详见DEVELOPMENT.md。
