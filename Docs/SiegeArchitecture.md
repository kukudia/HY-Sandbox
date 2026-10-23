# 阵地战架构选择与规模边界

当前采用 **GameObject组件 + 按连通分量的PhysX复合刚体 + 缓存连接图/批处理毁伤**。基地来自大型建筑蓝图，舰船具有独立驾驶舱、驾驶来源和AI。没有给每块方块添加Rigidbody/FixedJoint，也没有将自有模拟迁移至Entities。

## 已落地的职责

| 领域 | 入口 | 当前选择 |
| --- | --- | --- |
| 波次、金币、失败 | SiegeMatch / SiegeSession | 普通C#规则和场景生命周期分开 |
| 飞船驾驶 | Cockpit.ControlMode / ControlUnit | 唯一手动来源，其余友舰复用自主控制器 |
| 舰船断裂 | BlockGroupManager / RuntimeGroupBuilder | 双侧连接、主体刚体保留、局部刷新 |
| 建筑支撑 | BuildingRuntime / BuildingConnectionGraph | 虚拟底座、几何邻接缓存、底座接触缓存、visited-first BFS |
| 集中毁伤 | WorldBuildingDamage | 最外层批次完成后统一刷新，连锁顺序结算 |
| 短时表现 | TransientEffectPool / BlockVfxLibrary | 光束/点光源有界闲置复用，GPU突发沿原预算释放 |
| 真实验证 | SiegePlayerChecks / SiegePlayerSurvey | 独立发布包、可选嵌套计时、刚体分类、受控驾驶和自然交火分开 |

物理模拟单元是连通结构，而不是蓝图模块。模块保存耐久、连接、功能和可编辑身份；一个完整基地可以有2048个模块/碰撞体，却只有一个锚定主体刚体。断裂时再按分量建立动态残骸，并服从既有残骸预算。因此“万模块”“万碰撞体”“万独立动态刚体”是不同负载。

## Jobs、Burst、ECS如何选择

Jobs适合拆出的纯数据批量计算，Burst可以编译支持的计算代码；ECS是更完整的数据与调度架构。它们不是必须三选一的同类方案。当前锁定依赖中已有Burst1.8.28和Collections2.6.2，但本轮自有连接/支撑路径尚未使用Jobs/Burst，未安装或接入Entities物理替换。

先减少重复工作，再决定搬哪些计算。当前证据说明：1024块集中毁伤的按块支撑刷新曾是主要问题，合批后同步夹具879.25→40.49ms；2048块BFS访问状态优化将热遍历4.38→1.48ms；底座接触缓存将热底座2.638→0.143ms、完整热刷新16.995→13.851ms。它们来自不同明确夹具，不能相加，也不等于自然FPS提升。

下一次并行化应先把模块索引、边、支撑位和脏集合整理成不含Unity对象访问的快照，并单独测量快照建立、计算和主线程应用成本。若纯图计算只占很小一部分，单独将BFS放进Job不会解决Instantiate、物理形状、重组、渲染或GC的长帧。冷邻接仍包含真实几何/物理查询，不能把Unity对象调用直接塞进普通Job。

每块Rigidbody再用FixedJoint连接，会增加独立刚体、约束和接触求解的工作；它更适合确实需要连接弹性/断裂约束的结构。项目没有做同布局FixedJoint对照，不能给出已验证的倍数或将其称为性能优化。

## GPU与飞行/机器人计算的分工

GPU可以承担大量并行计算，但内置GameObject Rigidbody/PhysX并没有可直接打开的GPU刚体求解开关。现有Transform、组件、供电请求、AddForceAtPosition和Physics查询不能直接在Compute Shader中调用。Jobs/Burst使用CPU，采用它们也不代表物理计算已搬至GPU。大批量相似计算、数据长期留在显存且不要求同帧回读时，GPU才更有机会获益；逐舰上传、同步回读推力再提交PhysX可能增加延迟和等待。当前渲染本身的开销仍缺完整归因，不能假设GPU有空闲。

| 工作 | 优先路径 |
| --- | --- |
| Hover高度PID/姿态/推力分配 | 保持物理频率，缓存结构数据；足够多控制器时评估集中调度及Jobs/Burst，单独测量快照和应用成本 |
| Bot避障和制动 | 复用查询结果与过滤判断；保持近障制动及时，规模扩大后评估CPU批量物理查询及分阶段调度 |
| Bot任务搜索/AI决策 | 低频、错峰、空间索引，保持飞行执行与移动目标跟踪在物理帧 |
| BFS/拓扑重组 | 先测图计算占比，再评估纯数据快照与CPU Jobs/Burst；主线程应用、对象创建及PhysX成本仍需控制 |
| 非玩法碰撞的粒子/装饰碎片 | 优先GPU VFX或实例化绘制；外观碎片与参与伤害、维修的实体分开 |

HoverControlSolver已经分离纯计算，HoverFlightController也已有推进器、比例和物理参数缓存；其数量取决于实际控制器，不能把万模块等同于万次PID。Bot不是舰船EnemyController：当前Bot派生类是RepairBot和CollectionBot。实际热点仍需按两类AI分别采样。

2026-10-24先落地保持行为的局部轻量化：Bot单次Evaluate/ConstrainVelocity内复用碰撞体过滤，保持路线及候选方向评分的净空结果不再重查；同物理帧共享目标距离。Hover跳过未激活推进器的姿态计算，推进器可视化方向计算遵守已有节流。没有改变默认候选数（12×3+2）、采样间隔、近障检测频率、推力模型或物理后端。29项导航与13项悬浮Play检查通过，尚无同场景性能AB；详见 [验证](Validation/2026-10-24-flight-bot-lightening.md)。

## 规模证据与未完成验证

正常玩法使用388模块建筑基地；单栋压力布局达到现有2048模块上限。混合压力场保持建筑总8192预算，叠加六舰和最多十二敌舰，实际总模块和动态刚体规模以Player普查为准。场景布局、敌舰预算和镜头不同的窗口不能作为加速AB对照。

发布Player的托管错误数组不包含所有原生诊断；目前D3D11 PresentFrame时序提示仍存在。物理/Main Thread记录器句柄有效但没有实际样本，未用空值伪造物理耗时。完整人工驾驶、长期内存稳定性及万独立动态刚体都尚未验收。

入口和证据见 [阵地战玩法](SiegeWarfare.md)、[函数图](RuntimeFlow.md)、[底座缓存](Validation/2026-10-23-siege-foundation.md)、[真实驾驶](Validation/2026-10-23-siege-driving.md)、[万模块混合压力](Validation/2026-10-23-siege-mixed.md)。混合窗口实测峰10224模块、275刚体；十分钟P95约167ms，尚未达到流畅目标，也未获得足以将长帧归因于BFS的引擎样本。
