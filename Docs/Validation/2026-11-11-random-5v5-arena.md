# 自动随机5v5观战场景

入口：`Assets/Scenes/Random5v5Arena.unity`，打开场景进入Play自动开始。场景由双层 EnemyAvoidanceLab 综合城区复制而来，保留高架层、桥梁、地面街区和四组动态障碍；原避障试验场不变。

## 对战循环

- `Automatic 5v5 Match` 保存十个可编辑出生槽，前五个属于蓝队，后五个属于红队；所有舰船由AI驾驶。
- 蓝队只从 `persistentDataPath/Saves` 随机抽取玩家蓝图，红队从 `persistentDataPath/EnemyBlueprints` 和有效内置敌舰蓝图池抽取。各槽独立抽取并允许重复；任一池为空会显示配置错误，不会跨池借用蓝图。
- 驾驶舱摧毁后该舰淘汰。任一队驾驶舱全灭即判另一队获胜；双方全灭或双方存活舰队都失去作战能力时判平局。平局能力判定连续确认3秒，避免电网初始化期间误报。
- `ControlUnit.HasCombatCapability` 为只读 `bool`：舰船仍有至少一件启动、完好且其所在电网具备足够供电的武器时为 `true`。无目标、暂时失去目标、瞄准、冷却及当帧供电分配不影响该判断。
- 结算时停止AI和武器，等待5个模拟秒，再清除舰体、碎片、脱离模块、掉落、池内弹丸及活动特效。销毁完成后随机补齐十槽，动态障碍复位并自动开始下一轮。

## 地图与观战

建筑数量和各建筑尺寸保持原样，城区平面间距扩大，地面范围从300扩大到480；高架层、支撑、桥梁及动态障碍随布局一起扩展。相机保留自由透视控制，不锁定观战舰；镜头起点随战场重取景。舰队保持完整模块显示；蓝红染色按源材质共享阵营材质，不给逐Renderer添加PropertyBlock，以保留项目启用的GPU Resident Drawer绘制路径。耐久汇总复用ControlUnit当前物理步组件缓存；名牌遮挡射线改为每0.12秒一次。

`Block.showLabel` 和 `PlayManager.showLabel` 默认关闭，连接器本身仍可显示。双方舰体、血条、名牌和阵容文本使用蓝/红阵营色。击杀浮窗复用Exploration的 `CombatHud`，同时显示轮次、存活数、阵容和近期击杀。

自由相机控制：右键配合WASD移动，Q/E升降，Shift加速，中键平移，滚轮前后移动。松开右键或窗口失焦会释放鼠标。

制作入口：`Tools/HY-Sandbox/Combat/Create Random 5v5 Arena`。已有场景的升级入口为 `Tools/HY-Sandbox/Combat/Upgrade Arena To 5v5`；该迁移保留原场景GUID。源码运行图见 [RuntimeFlow](../RuntimeFlow.md)，方法导航见 [CodeIndex](../CodeIndex.md)。

## 验证

- Unity 6000.6.8f1导入及Edit验证：18项通过，覆盖十个槽位、5秒结算、依赖引用、四个移动障碍、自由相机、无预存舰船、Connector文字关闭、建筑扩散与阵营色。
- Unity Play验证：42项通过。十舰自动填充；蓝队名称来自玩家池，红队名称来自敌人池；十艘均为AI；击杀归因和重复通知幂等；分别验证蓝队与红队全灭结算；5秒前不清场，5秒后清除旧舰、碎片、掉落和弹丸并重置；第三轮补充通过。
- 能力验证：启动且有可用供电的无目标武器仍报告有能力；关闭发电机供能后报告失能；只关闭一队武器不会误判平局；双方失能后报告平局并完成5秒后的新一轮补充。
- Play画面捕获并检查过地图、蓝红舰队和HUD。静态构建、格式、导航及Git差异检查结果随交付记录；自由视角的真实键鼠输入仍需人工体验确认。
- 退出清理专项检查：24个本轮共享阵营材质在退出Play后降至0；修复Editor退出时延迟销毁队列不再执行造成的残留。静态构建0错误、37个现有警告；未执行Player构建或目标设备性能测试。

## 性能分析与优化

- Unity Profiler实测的代表性Editor Play帧约800ms，其中Lab Camera的URP RenderGraph约580ms，不透明与透明绘制约548ms；主要瓶颈是每艘远舰逐模块Renderer绘制。
- 阵营染色最初用每Renderer的MaterialPropertyBlock实现；Profiler显示大量模块进入普通逐Renderer绘制，抵消URP GPU Resident Drawer。现按阵营和源材质缓存共享染色材质，保持舰船所有模块并行绘制。
- `CombatHud`将名牌遮挡射线降至每舰约8Hz；`ControlUnit.TryGetTotalDurability`复用既有缓存，移除重复组件数组分配。
- 受控同舰队对照：同一批15698个MeshRenderer，固定相机、暂停物理和工业动画，每组预热5帧后采样15帧。属性块染色：帧中位数17.972ms、相机渲染均耗7.131ms；共享材质：14.648ms及4.539ms，渲染均耗降低36.3%。已保存可重复入口 `SkirmishPerformanceChecks.Begin()`，原始结果见下方记录。早期800ms坏帧与此对照不是同一阵容，不据此宣称30倍帧率提升。
- [同舰队性能原始结果](2026-11-11-5v5-same-fleet-performance.txt)。Editor包含Game View与驱动开销，不代表目标设备帧率。
