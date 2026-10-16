# 方块 CSV 数据

直接编辑 [Blocks.csv](../Assets/Resources/BlockData/Blocks.csv) 并保存。Unity 导入 CSV 后自动更新对应 Prefab；无需逐格操作 Inspector 或手动导入。菜单 `Tools/HY-Sandbox/CSV/打开方块 CSV` 可用系统默认应用打开；关闭 Prefab 编辑模式后可用“立即校验并同步 Prefab”重试同步。

当前 CSV 有 25 个 Block Prefab、79 个参数列及4个只读身份列。已经移除不参与质量计算的旧 `Block.density` 列；组件字段仍保留以兼容旧 Prefab。所有高级列直接显示；独立 Bot/Connector 资源不在表中，维修/收集机器人仍通过所属容器行配置。悬浮控制参数、万能推进模型对齐/响应、维修光束宽度及机器人显示开关由 Inspector 管理。

## 编辑规则

| 内容 | 规则 |
| --- | --- |
| 名称、路径、GUID、组件列表 | 只读；GUID 识别 Prefab，路径须与实际资产匹配 |
| 列名 | 保持原文，可调整列/行顺序 |
| 数字 | 使用英文小数点；参数范围由共享字段白名单校验 |
| 布尔值 | `true` / `false` 或 `1` / `0` |
| 枚举 | 使用导出的名称，如 `Coins`、`Technology`、`Enemy` |
| `Block.mass` | 留空自动使用 `x × y × z`；填写正数为手动质量，改变尺寸不会覆盖它 |
| `Block.density` | 已从 CSV 移除；旧 Prefab 字段保留兼容，质量由 `Block.mass` 或尺寸计算 |
| `requiresPower` | 映射货仓、收集机器人和维修机器人；需要供电时所属仓必须有 `Power`，维修仓可填 `false` 以使用无电维修 |
| 其他空单元格 | 保持该 Prefab 当前参数；没有对应组件的字段必须留空 |
| 说明文本 | 开启 `canEditInfo` 后填写；支持中文、逗号、双引号和多行 |

用 Excel 保存时选择“CSV UTF-8”，不要使用默认的本地 ANSI 编码。文件使用 UTF-8 BOM；数值与引用分离，不编辑当前电量/耐久、连接列表、模型、特效、存档内容等状态。

## 应用与保存

Editor 的 `BlockCsvPostprocessor` 只排队，`BlockCsvAuthoring.Update` 在导入/编译完成后执行完整校验和 Prefab 写入。写入前备份原始 Prefab、`.meta`、CSV 和哈希清单到 `Library/BlockDataTable/Backups/<唯一时间戳>`。校验失败不写入；中途失败恢复本批已写文件，不覆盖检测到的并发更改。重复同步无差异时不保存资产，不保存当前场景。

`BlockCsvBinding` 的执行顺序为 -10000，在其他组件的 Awake/OnEnable 初始化前应用配置。CSV 随 Resources 打包，运行时每次会话只解析一次；修改 Editor 内 CSV 会清除缓存。Play Mode 的已有对象不逐帧热更新，新建对象在 Awake 读取新配置，退出 Play Mode 后同步资产。独立构建使用打包时的 CSV，修改后需重新构建。

Block/绑定的 OnValidate 请求延迟应用；CSV 是已填写数值的权威来源。运行时应用只刷新质量和必要的尺寸派生数据，不创建组件、不覆盖资源路径，也不重建未变化尺寸的连接对象，因此不会清除连接状态、当前耐久或维修仓 Home/Outside 引用。Prefab 正在单独编辑时停止批量保存，关闭后用菜单重试，以免覆盖尚未保存的制作内容。仅改变尺寸会更新碰撞体/连接点，不自动缩放美术模型，需检查模型匹配。

蓝图存档的实例尺寸继续保留：恢复器先应用存档尺寸，再刷新质量与碰撞体；自动质量随实例尺寸更新，手动质量保持 CSV 值。

## 制作与验证入口

“从现有 Prefab 创建 CSV（不覆盖）”只在 CSV 不存在时生成初始值，已有 CSV 不被覆盖。新增参数需同时修改共享 `BlockDataTableSchema` 和 CSV 列名；删除字段后原组件字段保留 Inspector 值。

`BlockCsvChecks.RunEditMode()` 在当前 Editor 验证真实文件同步、质量模式、私有字段、子物体、共享 requiresPower、多行文本、幂等和错误拒绝；结束恢复测试前的 CSV/Prefab 字节。`RunPlayMode()` 在 Play Mode 验证真实 Awake 覆盖、耐久初始化顺序和 GUID 绑定。报告位于 `Temp/BlockCsvChecks`，本次交付记录见 [验证记录](Validation/2026-10-11-block-csv.json)。

维修仓原先缺少 Power，导致已选中损坏目标但无法离仓；现已补充 Power。BotContainer 的最低工作功率必须等于额定工作功率，供电只决定是否从仓内起飞；已经离仓的机器人会继续完成任务或返航。保持 requiresPower=true 时需接入发电机与中继供电网络；false 可免供电工作。Bake 会拒绝不满足功率门槛或缺少 Power 的货仓/机器人所属仓。

`BlockCsvRegressionChecks.Begin()` 在 Play Mode 用真实 Prefab 验证无电留仓、供电离仓维修、断电停止及免供电恢复，并检查重复 CSV 应用保留运行状态。后台验证时需临时启用 Application.runInBackground 并在结束后恢复；报告见 [回归记录](Validation/2026-10-11-block-csv-regression.json)。
