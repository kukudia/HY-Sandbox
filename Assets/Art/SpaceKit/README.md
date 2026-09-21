# SpaceKit 科幻工业备用素材库

2026-09-21 从本机 `CUBE - Spaceships Pack 01` 与 `PolygonSciFiSpace` 筛选；Unity 6000.3.11f1 / URP 17.3.0。

105 个 Prefab，22 个用途类别，每类 2–7 个备选；另选 10 个配色材质。连同依赖共 349 项资产：103 个 FBX、105 个 Prefab、29 个材质、30 张贴图（含 10 个 PSD 源文件）、82 个碰撞网格，约 19.02 MiB（不含 meta、文档和预览）。

## 快速使用

- 从 `Prefabs` 按玩法用途挑选；从 `Materials` 挑选同模型 UV 对应的配色。不要跨资源包直接换图集。
- 完整名称、用途、尺寸、面数和粒子预算见 [CATALOG.md](CATALOG.md)；可检索选择表见 [Selection.json](Selection.json)。
- 依赖已复制至本目录的 `Models`、`Materials`、`Textures`、`Collision`，Unity API 已验证没有目录外的 `Assets` 依赖。仅使用项目已有 URP 包和 Unity 内置资源。
- 原包保留不动，新副本使用独立 GUID，允许后续手动编辑。`Catalog.json` 记录逐资产来源、原/新 GUID、原文件及 meta 的 SHA-256。
- 这是外观与效果储备库。接入建造模块时，将视觉层放入现有 Block 的 Model 节点，保留存档资源路径、连接点、质量、供电和控制逻辑。

## 分类与备选数量

| 目录 | 中文用途 | 数量 |
| --- | --- | ---: |
| `Modules/Cockpits` | 驾驶舱 | 5 |
| `Modules/Hulls` | 机身外壳 | 4 |
| `Modules/Wings` | 机翼装甲 | 5 |
| `Modules/Thrusters` | 推进器 | 6 |
| `Modules/LandingGear` | 起落架 | 4 |
| `Modules/Power` | 能源设备 | 5 |
| `Modules/Sensors` | 雷达与中继 | 4 |
| `Modules/Turrets` | 炮塔 | 6 |
| `Modules/Projectiles` | 弹体与发射架 | 4 |
| `Modules/RepairDrones` | 维修与服务机器人 | 3 |
| `Construction/Structures` | 支架管路楼梯 | 6 |
| `Construction/Panels` | 面板门框舱壁 | 7 |
| `Props/Cargo` | 货箱与储罐 | 6 |
| `Props/Controls` | 控制台与屏幕 | 6 |
| `Props/Lights` | 设备灯具 | 5 |
| `Environment/Asteroids` | 陨石 | 4 |
| `Environment/Debris` | 残骸 | 4 |
| `Vehicles/ShipReferences` | 整船参考 | 6 |
| `VFX/Propulsion` | 推进特效 | 4 |
| `VFX/Combat` | 战斗特效 | 4 |
| `VFX/DamageSmoke` | 火焰烟尘碎片 | 5 |
| `VFX/Energy` | 电能与信标 | 2 |
| `Materials/Palette` | 配色材质 | 10 |

## 实际渲染预览

下列为独立预览场景通过 URP Camera 渲染的联系表，每格独立取景，不能据此比较真实尺寸；灯光仅服务于浏览，没有主场景 Bloom。粒子采用固定随机种子、单帧采样，尾迹长短和爆炸阶段以 Unity Particle System 预览为准。

- [模块、能源、炮塔与机器人](Preview/01_Modules.jpg)
- [结构、舱壁与工业道具](Preview/02_Construction_Props.jpg)
- [陨石、残骸与整船](Preview/03_Environment_Ships.jpg)
- [粒子效果](Preview/04_VFX.jpg)

## 筛选规格与取舍

| 项目 | 当前约定 |
| --- | --- |
| 风格 | 卡通低多边形工业设备，硬边轮廓、灰蓝/工程黄/青色发光，与现有 Industrial 功能色阶相容 |
| 用途 | 优先驾驶舱、推进、电力、武器、维修、结构件和陨石；整船用于造型/敌人外观参考 |
| 尺寸与 Pivot | 保留包内导入缩放、朝向、Pivot；CATALOG 的 AABB 为 Unity 单位，不视为已对齐建造网格 |
| 网格预算 | 面数与材质槽来自 Unity 实测。高频重复小模块优先约 3000 三角面以内（暂定预算）；大控制台/楼梯/储罐另做降面或限量使用；16,602 面运输船仅作整船参考 |
| 碰撞 | 保留配套源碰撞网格；动态 Rigidbody 接入时选择 Box/Capsule 或审核后的 Convex MeshCollider，不直接使用非凸碰撞 |
| VFX | 保留可编辑 Particle System 参数；爆炸/导弹总 maxParticles=3100，激光 1008/1108，均为容量而非实际同时存活数，需池化、并发上限和实机性能验证 |
| 材质 | 全部使用 URP Lit 或 URP Particles/Unlit；保留调色板 UV、基础贴图、发光与透明/加法语义 |
| 纹理与源文件 | PNG/PSD、FBX 和碰撞 mesh 字节与源文件一致，保留导入配置；仅对复制的 FBX 外部材质映射做重定向 |
| 动画 | 本次没有选独立角色/骨骼动画；维修与门的动作仍由玩法系统驱动 |
| 排除 | Demo 场景、角色、家具生活用品、大型空间站、行星云层、HUD 圆环、后处理配置及旧自定义 Shader |

有意调整：旧 Standard 材质转换为 URP；飞船 Rim Shader 转换为调色板+发光 Lit，不复制 Rim/Detail 叠加算法。陨石旧 PlanetsLines 材质转为源岩石基色的低光泽 Lit，不保留行星描边/云层。清理两个静态道具的空 Animator，以及 Billboard/Stretch 粒子上的无效 mesh 字段。禁用 trails 时空 trailMaterial 属于合法可选槽，保留原设置。

## 编辑、复核与恢复

- `Tools > HY Sandbox > Space Kit > Validate Library`：重新加载全部 Prefab，检查依赖、缺失引用、Shader、模型/材质，采样粒子，并更新 `Validation.json`（无需原包即可验证）。
- `Render Preview Images`：在独立临时场景逐个渲染，PNG 输出到 `.utmp/art-curation/previews`；不会保存或切换 Main 场景。
- `Create Missing Library`：依据 Selection 和两个本地源包创建库；发现 Catalog 或同名目的资产会拒绝覆盖，以保护人工编辑。该操作仅用于初次建立，不是更新/覆盖按钮。
- 恢复已交付版本优先使用 Git；源包保持原位置，来源表可用于重新核对原文件。没有新增生成模型、假造动画或新来源资产。

## 来源与许可

- CUBE - Spaceships Pack 01：原包说明标注 Mesh Tint Team，网站 https://www.MeshTint.com ，Unity Asset Store publisher/3867。
- PolygonSciFiSpace：包名与 Shader 标识为 Synty Studios POLYGON Sci-Fi Space。
- 本次仅整理用户已有的本地资源包；目录内未找到可确认具体授权范围的许可证/购买凭证，未独立核验授权。来源许可沿用原购买条款，不能将此整理结果当作可独立再分发的素材包。

## 验证范围

- 已实际验证：Unity 6000.3.11f1 导入/脚本编译、349 项资产加载与依赖闭包、105 个 Prefab 内容加载、无缺失 mesh/material/script、全部材质 URP 可用；粒子在编辑器内模拟采样；105 个 Prefab 实际 URP 渲染并检查四张联系表。
- 已通过文件检查：源文件/源 meta 哈希不变、独立 GUID、无原包 GUID 残留、所有 meta 完整；`dotnet build HY-Sandbox.sln --no-restore --nologo` 为 0 错误、4 个既有警告；`git diff --check`。
- 当前 Main 场景保持原路径且未被弄脏。最终验证无新增 Console error，仍有既有 ProfilerCaptureAnalysis 弃用警告。
- 尚未验证：接入 Block 后的网格/Pivot 适配、动态碰撞、存档/战斗 Play Mode、大量透明粒子并发和目标机器性能。本任务没有替换现有玩法素材。
