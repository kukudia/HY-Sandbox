# Windows Player启动与体素区块优化

## 已确认问题与修复

用户目录原Player.log/Player-prev.log均在NVIDIA RTX3070、Unity6000.3.11f1的D3D12路径记录资源驱逐清单、Crash及UnityPlayer原生堆栈，没有C#异常。它们已备份到Temp/WorldPlayer/Player-original-*。日志缺少完整符号，不能确定具体驱动函数或资源。相同旧Build加`-force-d3d11`可响应且无崩溃；据此将WindowsStandaloneSupport显式配置为D3D11，新包无需额外参数。

这修复已复现的后端相关启动失败，不能推论所有D3D12设备均有问题，也不证明驱动本身是唯一原因。未禁用其他平台图形API，未更换用户驱动。

启动时用户AirKing为442模块，既有渐进加载间隔使恢复约12秒；保留其加载动画/间隔配置，新增组合进度遮罩，显示载具与世界实际完成比例。载具模块占95%，连接重建完成后100%；世界资源/地形/建筑/远景沿用已有分阶段进度。启动遮罩可取消，后台工作继续；进入游玩仍须就绪后才能启动。

建筑候选点概率0.45→0.35（相对减少22.2%），仍从persistentDataPath/BuildingBlueprints全部有效JSON选择，不改用户数据。实际栋数由种子、平台间距与预算共同决定。

## 参考研究与实现选择

2026-10-15实际读取以下README、GitHub仓库元数据，并核对许可证；额外读取Binary Greedy Meshing的mesher.h：

| 来源 | 查阅版本/许可 | 本项目采用的思路 |
| --- | --- | --- |
| [Binary Greedy Meshing v2](https://github.com/cgerikj/binary-greedy-meshing) | af8c688ed3af63e98f9631c05774f30e1350a021，MIT | 含邻块边界的数据、可见面掩码、同材质面合并；注意大矩形和T接缝 |
| [Sector's Edge体素网格实现](https://github.com/vercidium/voxel-mesh-generation) | 34e1879fdff5d93a5618539281846a6daab18013，Apache-2.0，README注明已过时并链接新库 | 线性地址增量、减少内层重复计算、CPU生成与GPU提交分开 |

独立编写C#实现，没有复制第三方代码、添加插件或依赖；没有照搬OpenGL vertex pulling、单次多重间接绘制或其性能数字。Unity继续使用标准MeshRenderer/MeshCollider及现有URP材质。

## 源码与生命周期

- VoxelChunkSnapshot.Capture：主线程逐坐标采样一次，包含一格halo，冻结稀疏编辑/噪声结果；后台不接触世界、Dictionary或Unity对象。
- VoxelSurfaceMesher.Generate：按六方向构建材质掩码，线性索引步进，贪心合并矩形；保留三材质、表面覆盖、硬法线、平铺UV。现有三种URP/Lit材质不使用旧逐体素顶点颜色，因此不再分配该通道；自定义顶点色Shader不在本次兼容保证内。
- ProceduralVoxelWorld.LoadColumn：每个加载器最多一个Task；等待期间可渲染，完成后检查列请求和VoxelWorldData.Revision，编辑冲突重新采样，取消结果丢弃。结果无Unity资源，取消后后台短任务完成即可回收；不预生成整个世界。
- VoxelMeshData.Upload / VoxelWorldChunk.ApplyMesh：主线程创建Mesh，顶点少于65536时使用16位索引，替换拥有网格并提交PhysX。每次提交仍是不可中断的Unity操作。
- LateUpdate破坏重建同时受rebuildsPerFrame和rebuildFrameBudgetMilliseconds限制。显式FlushDirty默认同步完成，保留WorldLab测试/Bake调用契约。
- 静止满载每0.2秒维护休眠/恢复；中心列或列版本改变立即唤醒。建筑休眠按已有毫秒预算分批，地形每卸载一列让出帧；会话退出立即丢弃所有变化，保留原行为。

原始Prefab、网格GUID、玩家存档、BuildingBlueprints没有迁移或改写。Main场景布局未改，面板仍可在Inspector编辑。

## 对照与验证

[网格微基准](2026-10-15-world-chunks-benchmark.json)：相同seed14721、buildingChance0.45、padHalfSize4、81列×2立方块、每块16³、cell2m。同步单次Editor Mono测试，包含采样、Mesh上传和DestroyImmediate，不包含PhysX烘焙。

| 指标 | 修改前 | 修改后 |
| --- | ---: | ---: |
| 顶点 | 197,372 | 32,324 |
| 三角形 | 98,686 | 16,162 |
| 同步生成总时间 | 135.10ms | 83.74ms |

三角形减少83.6%。时间是单次微基准，约38%下降，仅用于发现回退，不是整局性能提升保证。早期未优化坐标循环的合并版本耗时上升，已通过线性地址步进修正；本表只报告同配置最终实现。

- [网格基准校验](2026-10-15-world-chunks-meshing.txt)：20,267断言，随机三材质数据与独立逐体素暴露面集合完全等价；覆盖负坐标、接缝、法线/绕序、UV、合并长面、快照隔离及工作线程计算。
- [Main回归](2026-10-15-world-chunks-main.json)：通过，包含半完成列取消和Unity拥有网格释放。异步等待帧会影响断言数量，以JSON为准。
- [边界回归](2026-10-15-world-chunks-edges.json)：23项通过，高速等待与真实物理、建筑休眠/预算。未执行故障注入。
- WorldLab数据12项、真实破坏/填补/炮塔命中16项通过；[刚体180物理帧落地](2026-10-15-world-chunks-physics.json)通过。
- Windows非Development包实际运行，覆盖自动默认D3D11、组合进度遮罩、81初始列、外部建筑库、真实出生碰撞、200m移动、活动列有界、退出清空、再次Ready。性能数字与图像见下方最终记录。
- Player验证前后对用户目录全部JSON做SHA256对照，无变化；临时载具退出使用正式CompletePlayEnd，跳过返航货仓保存入口。

## 最终Player记录

[画面与行为检查](2026-10-15-world-player-visual.json)及[独立计时检查](2026-10-15-world-player-timed.json)各16项通过、零错误。计时运行不截图，不同时操作Editor或构建；Editor保留打开。默认D3D11，Release/Mono，1280×720，上限120FPS。计时种子-603340831、25栋/1541模块，初始81列，200m移动；这是有帧率上限的窗口测试，不能据此推算无上限FPS。

| 阶段 | 帧数 | 中位/ms | P95/ms | 最大/ms |
| --- | ---: | ---: | ---: | ---: |
| 启动与442模块恢复 | 1300 | 8.48 | 12.83 | 300.69 |
| 等待世界/启用进入 | 63 | 8.34 | 9.20 | 36.68 |
| 静止 | 360 | 8.33 | 8.85 | 9.72 |
| 200m跨区块移动 | 420 | 8.33 | 9.42 | 64.16 |
| 退出 | 1 | 70.73 | 70.73 | 70.73 |
| 第二次世界准备 | 492 | 8.33 | 8.58 | 9.42 |

采样从脚本初始化后开始，不包含进程创建/图形设备初始化；启动阶段总计12.31秒，世界第二次准备4.10秒。计时快照Unity总已分配内存见JSON的memoryBytes，不是OS工作集或峰值，也没有长期泄漏结论。不同运行采用不同随机种子，不将不同栋数的Player结果当作严格前后帧率对照。首次资源初始化约301ms、移动约64ms和退出约71ms的峰值仍存在；没有宣称零卡顿。

验证后28份用户JSON的SHA256全部一致。最终BuildPipeline结果Succeeded，0 errors、1 warning（Pipeline未配置RuntimePipelineConfig，因此Editor联动工具在Player中禁用，不影响游戏）；dotnet静态编译0错误/4个既有警告，232脚本可读性/导航及git diff检查通过。

画面分别为[启动组合进度](2026-10-15-world-player-startup.png)、[进入遮罩](2026-10-15-world-player-loading.png)、[真实Player世界](2026-10-15-world-player-world.png)。截图运行的计时包含读回/PNG编码，性能表只采用另一次关闭截图的运行。

## 使用与限制

直接运行本地`Builds/WorldPerformanceValidation/HY-Sandbox.exe`；不覆盖旧Build目录。Editor工具WorldPlayerBuildChecks.Build可重新生成这个独立验证包，不递增版本、不自动压缩或上传二进制。显式传`-world-smoke-report <绝对JSON路径>`才启动自动测试；`-world-smoke-capture`额外保存截图。正常启动没有测试载具或自动退出。

Player测试设置1280×720、垂直同步关闭、帧率上限120、随机世界种子、临时无敌驾驶舱，关闭随机敌机刷新但保留真实建筑。不是长期完整战斗、4K、最大载具或其他GPU测试。建造存档渐进恢复约12秒；仍有首次资源/渲染初始化和整会话退出峰值，遮罩不是消除这些操作耗时的证明。跨区块单次模块启用、物理烘焙、GC及着色器首次使用仍可能产生峰值；没有宣称完全消除卡顿。
