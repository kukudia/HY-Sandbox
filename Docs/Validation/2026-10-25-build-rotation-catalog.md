# 全部建造方块与全旋转轴验证

## 规则与实现

当前[尺寸奇偶网格规则](2026-10-25-build-rotation.md)以真实驾驶舱的整数占格边界为准：世界尺寸奇数轴中心为半整数、偶数轴中心为整数。2×1×1绕Z轴90°的世界尺寸1×2×1，中心为`(.5,整数,.5)`。此前奇偶映射相反，已替换；不能将此前自动断言数量解释为错误映射得到验证。

最新Unity隔离Play验证为78,769条断言通过，原460项回归同步当前规则并通过。新增真实驾驶舱所有连接点作为放置目标，检查所有姿态下方块AABB边界为整数，避免测试期望和生产代码的奇偶公式一同写反。测试范围仍为25种方块、24种朝向、六个旋转方向与64种合成尺寸。

旧用户存档与已摆放坐标不自动修改；再次旋转/重新放置时使用新规则。以下Inspector父对象回归的旧坐标只是说明局部/世界差值，不代表其符合更正后的占格规则。

本轮修复BuildManipulation.AlignAxisToNearestWorldDir：原上轴初始评分错误使用相机前向，且前/上方向独立选择可能重合；现在先选前轴，再从垂直方向中选择最接近相机上方的轴。TryBeginDrag捕获开始时的世界轴，后续帧不再读取手柄方向。

当前Resources/Blocks共27份Prefab，其中25份具有Block，对应Main建造栏全部25种条目；Bot和Connector是辅助对象。25种方块根缩放均为1且允许旋转；底座等原有禁止旋转规则保持。[BuildRotationCatalogProbe](../../Tools/Validation/BuildRotationCatalogProbe.cs)自动发现Block资产，未来新增方块自动进入覆盖矩阵。

## 已执行

### Inspector坐标回归补充

用户截图的2×1×1局部位置为(0.5,3,-10.5)、Z=90°。实际建造父预制体的位置为(-2.5,-0.5,10.5)，只读Type 04存档中的世界位置为(-2,2.5,0)，二者精确对应。此前原点父对象的测试没有覆盖这种制作偏移。

BlueprintLoader.ReplaceParent现在使用单位变换创建建造容器；方块继续按原世界姿态恢复，不修改用户文件或父预制体。探针通过真实ReplaceParent和BlueprintBlockFactory.TryRestore复现该坐标，确认Inspector局部位置也为(-2,2.5,0)，BlockData输入及恢复后保存坐标不变。所有25种方块改挂在该真实生产父对象下，姿态检查同时严格比较局部/世界位置；共72,577条断言通过，其中7条为父对象/截图恢复回归。

### 全量旋转矩阵

目标工程D:/git_projects/Unity/HY-Sandbox，Unity 6000.3.11f1。确认Main未脏化后进入隔离空场景Play，探针完成后退出并恢复Main；未写用户蓝图或保存场景。

- 25种真实Prefab × 24种正交初始姿态 × X/Y/Z × 正反方向。每个方向连续4次真实键盘输入，逐坐标严格比较独立期望解，验证四次回位、保存次数、Undo/Redo。
- 同一矩阵的手柄挂在真实方块下面，使用BoxCollider射线命中和TryBeginDrag，通过虚拟鼠标及真实相机/平面算法拖拽90°、180°、-90°、90°；释放只提交一次，验证Undo/Redo及内存JSON往返。
- 每种方块、24个姿态、六个面上可用连接点的放置解是精确半格坐标；无连接点的面仍由BuildPreview既有范围回退处理，不伪造连接点。
- 24种正交相机姿态及40种斜视/俯视/滚转组合，操作坐标架均为单位正交轴。
- 1–4格共64种合成尺寸 × 24种姿态 × 六个旋转方向，覆盖所有奇偶排列和长尺寸。
- 72,570条断言通过；此前460项中心/PhysX阻挡/JSON与Awake回归通过。临时VFX关闭、普通日志过滤，警告和错误保留启用；finally恢复输入设备、单例及日志设置。
- Unity导入/重新编译0错误/0警告；319份自有脚本可读性检查无差异/无错误；dotnet build HY-Sandbox.sln --no-restore为0错误/4个既有警告，git diff --check通过。函数图和导航已同步。

| 方块 | 逻辑尺寸 | 本方块断言数 |
| --- | --- | ---: |
| 1x1x1 | 1x1x1 | 3025 |
| 2x1x1 | 2x1x1 | 3025 |
| 2x2x1 | 2x2x1 | 3025 |
| 2x2x2 | 2x2x2 | 3025 |
| CargoHold | 2x2x2 | 3025 |
| Cockpit | 2x2x2 | 3025 |
| CoinHold | 1x1x1 | 3025 |
| CollectionBotContainer | 1x1x1 | 2545 |
| Door | 1x2x1 | 3025 |
| HoverFlightController | 1x1x1 | 2545 |
| HoverThruster | 1x1x1 | 2545 |
| HoverThrusterBig | 2x2x2 | 2545 |
| MainThruster | 1x1x1 | 3025 |
| MainThrusterBig | 2x2x2 | 2833 |
| PowerGeneratingUnit | 2x2x2 | 2545 |
| PowerTransmissionDevice | 1x1x1 | 2545 |
| ProjectileTurretDouble | 1x1x1 | 2545 |
| ProjectileTurretSingle | 1x1x1 | 2545 |
| Rack | 1x1x1 | 3025 |
| RepairBotContianer | 1x1x1 | 2545 |
| Stairs | 1x1x1 | 2833 |
| TechnologyHold | 1x1x1 | 3025 |
| Turret | 1x1x1 | 2545 |
| UniversalThruster | 1x1x1 | 2545 |
| UniversalThrusterBig | 2x2x2 | 2545 |

上表来自实际机器结果Temp/build-rotation-catalog.json，合成尺寸与相机断言不计入各行；临时JSON不提交。动态报告使用托管JSON序列化，避免Unity原生序列化器遗漏动态编译的嵌套逐Prefab结果。

```powershell
# 先在没有SaveManager的隔离空场景进入Play Mode
unity command run_script --file Tools/Validation/BuildRotationCatalogProbe.cs --entry BuildRotationCatalogProbe.Run --result-only
```

## 边界

覆盖真实操作方法和手柄命中，不等同完整UGUI点击/选择和人工手感验证；未运行独立Player。JSON覆盖BlockData字段的内存往返，不代表完整文件仓加载。原460项探针保留PhysX阻挡与JSON/Awake实例化回归，本探针聚焦全目录与全轴穷举，不代表所有玩法功能回归。当前资产均为单位根缩放；未来非单位根缩放会使探针显式失败，需要先确定其尺寸与逻辑网格契约。
