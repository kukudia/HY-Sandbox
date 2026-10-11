# HY-Sandbox C# 项目例外

- 自有功能路径为 Features/<Feature>，运行时类型沿用全局命名空间；不要为格式整理变更类型身份。
- 共用架构、Allman、4空格、介绍/注释与函数图规则见用户 game-code-readability skill。项目.editorconfig、Tools/CodeReadability检查入口优先。
- 保留既有 public Inspector字段、类型和初值作为场景/外部契约；新增参数用 SerializeField private。可执行资产迁移时再主动修改契约，不机械改可见性。
- RuntimeUnitMember目前与ControlUnit同文件为既有身份例外；其他新增MonoBehaviour同名独立脚本。纯配置小struct可与其直接使用者同文件。
- 原GroupAction.cs、Meteor.cs、ThrusterAllocator.cs为GB18030，保留编码；其他脚本按真实BOM保持UTF8。C#统一CRLF。
- 修改已有脚本不改meta；移动同步meta保留GUID；新增资源正确生成meta。物理写入保持FixedUpdate/碰撞回调，输入保持原Input System时序。
- 运行图位置 Docs/RuntimeFlow.md；导航生成 python Tools/CodeReadability/generate_navigation.py；版本与验证命令见根AGENTS。
