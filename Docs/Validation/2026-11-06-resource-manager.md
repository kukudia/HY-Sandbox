# 特效与音频资源管理验证

## 入口和设计

- Unity 菜单：Tools / HY Sandbox / 资源管理 / 特效与音频。
- `Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs`：窗口、原生 Inspector/预览、定位、打开、保存。
- `PresentationAssetIndex.cs`：共用资源发现、GUID 身份、路径搜索、直接引用查询；仅 Editor 使用，无运行时加载或常驻开销。
- `BlockVfxLibraryEditor.cs`：已有 ScriptableObject 的具名播放槽位，使用 SerializedObject 支持原生 Undo 和持久化；不改变数组顺序/枚举值/资产 GUID。
- `Assets/Editor/Presentation/Validation/PresentationResourceChecks.cs`：真实 AssetDatabase 检查入口 `Run()`，临时音频夹具使用唯一目录并在 finally 清理。

不移动已有美术资源，不替换运行时播放入口。全部新功能均在 Editor 目录。初始工作区干净；恢复分支 `codex/vfx-manager-baseline-20261106` 指向修改前 `61f3c3f08452d3d93c0d224bd8102ae971047c22`。

## 已验证

Unity 6000.6.8f1、URP/VFX Graph 17.6.0，连接本项目 Editor 导入并编译新脚本，由 Editor 生成新 meta 和 csproj 编译项。

`unity command eval 'return PresentationResourceChecks.Run();' 60000 --project-path D:/git_projects/Unity/HY-Sandbox --json`（第二个位置参数是 eval 的毫秒超时；CLI 的 `--timeout` 是不同参数）：

- 213 个特效相关资产，无重复 GUID；BlockVfxBaker.Root 下所有特效 Prefab 均在列表。
- 408 个含递归依赖资产；保留一级资源分类，不被依赖分类覆盖。
- 名称/路径忽略大小写搜索、类型筛选通过。
- Explosion.prefab 的直接引用包含实际 BlockVfxLibrary.asset。
- 创建临时 AudioClip 资产后音频索引可发现；特效索引不混入音频；移动后 GUID 仍解析到新路径。
- 正确选择 BlockVfxLibrary 自定义 Inspector，窗口可实际打开。
- 临时播放库副本修改支持 Undo；保存并重新导入后保留修改，生产播放库未改动。

两次检查请求触发 Pipeline 默认 5 秒超时；最终正确传入 eval 超时后的完整检查通过。该历史错误不属于脚本编译错误，不计为 Console 全程零错误。

本机仅安装 .NET SDK 9.0.320，项目 global.json 锁定 9.0.318 且禁止 roll-forward。原命令因缺少指定 SDK 不能运行；保持 global.json 不变，显式调用已安装 SDK 的 dotnet.dll 完成等价检查：

```powershell
dotnet 'C:/Program Files/dotnet/sdk/9.0.320/dotnet.dll' run --project Tools/CodeReadability -- --check .
dotnet 'C:/Program Files/dotnet/sdk/9.0.320/dotnet.dll' build HY-Sandbox.sln --no-restore -m:1
git diff --check
```

构建 0 错误、22 个程序集引用冲突警告。格式规范由相同工具的 `--write` 整理新文件；导航生成脚本的子进程同样显式使用 9.0.320，生成 48 张函数图及 355 份脚本导航。

## 边界

- 本轮是 Editor 资源管理功能，没有改动运行时效果和音频播放策略，未执行 Play Mode、Player 构建、全部效果的逐项画面验收或音频实际听觉验收。
- 原生预览能力取决于 Unity 对资产类型的 Inspector 支持；Prefab 缩略图不等于 GPU VFX 动态播放验证。
- 引用查询只代表 AssetDatabase 直接序列化依赖，不把零引用当作可安全删除的证据。未添加批量删除或自动重写引用功能。
- 范围为 Assets，不含 Packages；“包含依赖”展示递归依赖资产，不声称识别所有未被使用的通用贴图/Shader 属于特效。
- 本地原始输出在忽略的 `Logs/resource-manager-*`，不提交机器日志和编辑器布局。
