# Main 大世界卡顿修正

## 结果与测量范围

Unity 6000.3.11f1、D3D11、Main、Game View 725×488，固定种子14721，33栋建筑、2271个模块。Editor实际帧间隔和CPU Profiler采样，不是正式Player帧率保证，没有开启Deep Profile。

| 指标 | GUI/搜索优化前 | 优化后 |
| --- | ---: | ---: |
| 静止帧中位数 | 30.43ms | 7.97ms |
| 静止帧P95 | 34.82ms | 9.52ms |
| 末尾100帧Main Thread均值 | 30.570ms | 8.536ms |
| GUI.Repaint均值 | 17.787ms | 未进入前35项 |
| Turret.FixedUpdate均值 | 3.354ms | 0.227ms |

这组对比的前后都已启用建筑网格合并与分帧加载，用于定位持续开销：每模块独立OnGUI的隐含GUI初始化，以及空目标每物理步重搜。最后一轮为避免测试载具在移动中死亡，加大了测试用耐久并检查游玩状态；世界生成配置不变。前一轮静止采240帧，后一轮采366帧、至少3秒；这是故障诊断对比，不是严格AB交替基准。

原始记录：[优化前GUI热点](2026-10-15-world-performance-before-gui.txt)、[最终Profiler](2026-10-15-world-performance-after.txt)。最终移动300帧中位数10.95ms、P95 15.86ms、最大54.06ms；静止阶段最大26.25ms，未出现50ms以上帧。

早期观测中EnterPlay同步调用约498ms，分帧后核心调用约3–4ms，正式UI/PlayManager同步入口约27ms。启用工作随后在遮罩下按2ms批次进行，暂停物理；最终入口阶段总2.44秒还包含启用后的120帧采样，不能全部视为加载时间。早期[原始基线](2026-10-15-world-performance-baseline.txt)期间存在CLI查询，帧最大值可能受Editor命令影响，不用来声称严格的加载加速比例。

最终准备阶段仍同时包含当前AirKing载具逐帧读取/连接重建，中位5.40ms、P95 27.06ms，偶有约75ms单帧；退出一次约75ms。单个Unity对象创建、资源集成、销毁和碰撞更新不能由协程内部强制中断，当前没有承诺所有帧低于16.7ms。正式Player、高分辨率、长时战斗和D3D12仍未验收。

## 改动

- 移除每个Durability的OnGUI/LateUpdate；DebugManager装配一个DurabilityDebugLabels集中绘制，关闭标签时不遍历模块。
- 炮塔空目标保留targetRefreshInterval；已有目标失效仍立即清除/重搜，按阵营先排除无关单位。
- 建筑资源异步预热、创建/启用按Inspector的Loading Frame Budget Milliseconds执行；分帧恢复不发布半完成建筑，取消时回滚。WorldLab同步事务接口保留。
- 全屏不透明遮罩覆盖进入世界，先呈现一帧再启用根；进度随批次更新，完成/退出/死亡/故障恢复原timeScale。途中缺地形时显示玩家预测路径的完成比例。
- 远景等初始列完成再生成，跨四列才重采样，列变化复用顶点/Mesh更新空洞；高度缓存有界FIFO替换。
- 同父刚性装饰网格按材质合并并共享；保留炮塔轴、后坐炮管、机械运动及发光Renderer。早期观测绘制约1.4万次，优化后正式入口约2700次；相机/运行路径不同，仅作为工作量观测。
- 大载具加载末尾的连接重建同样按2ms批次让出帧，避免先逐帧恢复再集中重建连接。

## 回归与画面

- Main真实Play：53项通过，含遮罩/进度、暂停恢复、共享网格、正负区块、损坏/货仓恢复、取消、激活中死亡与退出清空。[报告](2026-10-15-world-performance-main.json)
- 边界：23项通过，含81列预算、4000m/s真实物理步和动态建筑休眠恢复。[报告](2026-10-15-world-performance-edges.json)
- WorldLab建筑：43项通过；WorldLab数据：12项通过。[建筑报告](2026-10-15-world-performance-building-regression.json)
- 炮塔激光、实体炮弹与后坐回归见[报告](2026-10-15-world-performance-turrets.json)。
- Unity已导入新增脚本和meta；228份脚本格式/函数图检查、dotnet build（0错误、4个既有警告）、git diff --check通过。函数图是源码推导，不是Profiler调用轨迹。
- 实际查看[不透明加载遮罩](2026-10-15-world-performance-loading.png)和[合并后建筑近景](2026-10-15-world-performance-building.png)。

复现：在已保存的Main Edit Mode调用`WorldSessionPerformanceChecks.Run("production-profile")`，报告写入Temp/WorldSession。测试只使用运行时配置副本和临时载具，不保存用户蓝图；采样期间不要执行Editor eval、截图或切换窗口，以免污染帧时间。
