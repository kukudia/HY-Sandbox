# 多舰表现性能优化（2026-11-05）

本轮在既有连接刷新和炮塔索敌优化之上，优先处理多舰时的渲染提交、工业零件表现更新和编辑器连接点调试。没有修改物理步长、伤害频率、舰船数量预算或模型质量。

## 实现

- `Assets/Settings/PC_RPAsset.asset` 启用 GPU Resident Drawer 的 Instanced Drawing；Graphics Settings 的 BRG Variants 设置为 Keep All，防止 Player 构建剥离所需变体。Unity 6000.6 保存时补齐了新版序列化默认字段。Mobile 管线保持原配置。
- `IndustrialPartMotion.OnEnable/OnDisable` 维护活动列表，单个 `IndustrialMotionDriver.Update` 调度；旋转仍读取 scaled delta，浮动和发光仍读取 unscaled time，Inspector 参数与已有GUID不变。
- GPU 绘制路径中，`IndustrialEmissionBinding` 按源材质、发光颜色、频率和幅度共享临时材质，每组每帧仅更新一次。固定夹具有27,878个发光Renderer，只使用1个脉冲材质配置。普通SRP路径继续使用PropertyBlock，以保留原有自动实例化；已有或稍后添加的独立属性继续读改写，不清除其余值。
- 建造和游玩选择系统保存源材质，取消选择时恢复当前有效脉冲绑定。禁用、重配及销毁释放引用；最后一份引用释放时销毁临时材质，不修改源资产。
- `Block.OnDrawGizmos` 在连接点和标签均关闭时立即返回；显示标签时复用GUIStyle。这个分支只影响Editor，仍有Unity自身逐对象回调成本。

入口和调用图见 [RuntimeFlow](../RuntimeFlow.md#工业零件统一调度与共享发光)，完整原始样本和检查项见 [JSON](2026-11-05-fleet-presentation.json)。

## 最终对照

Unity 6000.6.8f1，RTX 3070，Siege部署阶段，固定8艘CODEX_01_Bastion加基地；9个ControlUnit、13,939个工业动画组件。两个会话使用相同固定布局和镜头，各跳过40个观察帧，再记录300个Profiler CPU帧样本。对照为同一份最终代码的传统绘制与GPU绘制；不是改动前后所有优化的总收益。

| CPU帧时间 | 传统绘制 / PropertyBlock | GPU绘制 / 共享发光 |
| --- | ---: | ---: |
| 平均 | 61.36ms | 39.20ms |
| 中位 | 60.42ms | 38.02ms |
| P95 | 68.97ms | 46.27ms |

中位减少约37.1%，P95减少约32.9%。GPU路径记录初始化成功；[固定舰队截图](fleet-final-gpu.png)确认主要舰体和发光可见。两个独立Play会话按先GPU后传统执行，没有ABBA随机顺序，不代表自然战斗、Player FPS或GPU执行耗时；观察仍受Editor调度和Profiler影响。

前期探索中，原实现全部工业动画的手动更新批次中位约30.96ms，统一调度/共享发光约9.38ms；只作局部优化线索，不与最终整帧对照混算。初次直接去除PropertyBlock而未启用GPU绘制时，脚本时间下降但整帧未改善，因此最终仅在GPU路径使用共享材质，传统路径保留原方式。

复现：新进入Siege Play，调用 `FleetPresentationBenchmark.CreateFixedFleet()`；确认9个单位、13,939个动画和相同镜头后调用 `StartCapture("label")`，结果保存于 `Temp/FleetPresentation/label.json`。退出Play清除测试舰队。探针仅读取当前渲染配置；配置切换在退出Play后通过Inspector进行，不在运行中来回切换。

## 验证

- 已在真实Editor完成脚本导入、编译和新增资源meta生成，已有脚本GUID不变。
- 14项工业表现回归：同配置共享、源材质不变、暂停脉冲、独立属性、运行中追加属性、高亮、重配、禁用、再启用与销毁。
- 4项传统路径兼容检查通过。
- AI 21项、连接生命周期39项、阵地战28项和真实炮塔32项通过；炮塔检查在Battle阶段执行，错误列表为空。
- GPU配置下实际启动波次并采集[战斗画面](fleet-gpu-combat.png)。这只证明该次短时运行，不能替代长时战斗压力测试。
- `dotnet run --project Tools/CodeReadability -- --check .`：349脚本、46函数图，格式差异0、错误0；`dotnet build HY-Sandbox.sln --no-restore`：0错误、109条已有警告；`git diff --check`通过。静态检查不代替上述Play验证。

## 限制与剩余优先级

尝试运行中反复切换GPU Drawer的ABBA实验时，Editor发生一次原生崩溃，堆栈位于渲染队列的Mimalloc分配和CullResults/CreateSharedRendererScene，根因未确认。没有生成有效ABBA报告；恢复Editor后，移除运行中切换工具，使用固定配置的独立Play会话完成上述采样。不能据此宣称该引擎路径在所有规模下稳定。

尚未验证Standalone Player构建、变体编译时长/包体增量、长时战斗、其他显卡和移动端实际运行。Keep All会增加保留的BRG变体，需在Player构建阶段评估包体和编译影响；当前不包含GPU遮挡剔除或ComputeShader物理/AI迁移。

后续按真实战斗Profiler重新排序：剩余的渲染对象/Transform更新、零件旋转与浮动、交火时的索敌/伤害/物理。Gizmos属于Editor成本，Player中不以其作为主要优化目标。进一步优化应先收集独立Player的主线程、渲染线程和GPU时间，避免把绘制提交的改善等同于物理或AI加速。
