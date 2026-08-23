# HY-Sandbox Unity C# 代码规范

本文件适用于 `Assets/Scripts/**/*.cs`。修改脚本时同时遵守仓库根目录 `AGENTS.md` 和 `DEVELOPMENT.md`。

## 文件与类型

- 一个运行时 MonoBehaviour/ScriptableObject 类型使用一个同名 `.cs` 文件；纯数据或接口类型也保持文件名与主类型一致。
- 使用项目现有的全局类型布局，不为一次性重构新增命名空间或移动脚本，避免破坏 Unity 序列化引用。
- `using` 按系统库、Unity 库、项目库分组；删除未使用的引用。Editor-only API 必须放在 `Assets/Editor` 或 `#if UNITY_EDITOR` 中。

## 命名与可见性

- 类型、方法、属性和事件使用 PascalCase；局部变量和参数使用 camelCase；私有字段使用 `_camelCase`，常量使用 PascalCase。
- Inspector 配置优先使用 `[SerializeField] private`，只有跨组件/UnityEvent 的稳定 API 才公开字段或属性。
- 所有方法显式声明访问修饰符。Unity 生命周期函数（`Awake`、`Start`、`Update`、`FixedUpdate`、`LateUpdate`、销毁/碰撞回调等）默认使用 `private`。
- 公共方法应表达稳定的业务动作；内部步骤保持 `private`/`protected`，不要通过扩大可见性解决调用顺序问题。

## Unity 生命周期与运行时

- `Awake` 只做引用缓存和本地初始化，`Start` 处理跨对象依赖；在 `Update`/`FixedUpdate` 入口先检查单例、组件和模式状态。
- 物理力、刚体速度和碰撞响应只在 `FixedUpdate` 或碰撞回调中写入；输入采样放在 `Update`，通过状态传给物理层。
- 运行时动态添加组件、对象销毁和重新分组必须考虑 Unity 的延迟销毁语义，并在调用链中保护已销毁对象。
- 不修改已存在的序列化字段名、类型或层级引用，除非任务明确包含迁移和验证。

## 注释与日志

- 在连接/分组、存档加载、输入模式切换、摧毁爆炸、推进器分配和视觉效果等关键路径前说明“不变量、顺序或性能原因”。
- 注释描述意图和约束，不逐行翻译语句；复杂算法补充输入、输出和边界条件。
- `Debug.Log` 只记录可行动的状态变化；可预期的用户输入错误使用 `LogWarning`，不要在每帧输出日志。

## 变更与验证

- 代码改动后运行 `dotnet build HY-Sandbox.sln --no-restore`、`git diff --check`，并检查脚本换行、编码和 `.meta` 未被无关改动。
- 涉及场景、Prefab、输入或运行时行为时，在 `DEVELOPMENT.md` 变更日志记录“文件确认 / Unity 编辑器或 Play Mode 验证 / 尚未验证”三种状态。
- 新增 `Assets/` 文件由 Unity 生成对应 `.meta`；仅编辑现有 `.cs` 不创建或重生成 `.meta`。
