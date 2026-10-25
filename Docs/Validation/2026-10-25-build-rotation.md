# 建造旋转中心锁定

## 问题与规则

`BuildManager` 的操作由 `BuildManipulation` 实现。旧键盘/轴拖拽路径旋转后调用 `SnapCenterByMinCorner`；旋转改变包围盒尺寸，最小角重新吸附会移动中心，且拖拽下一帧又以移动后的中心求角度。偶数尺寸放置产生的半格中心使这种偏移更难预测。操作轴本为世界轴，旧代码在姿态右侧相乘，还会导致已有旋转时绕错方向。

放置仍采用真实连接点：`position = targetConnectorPosition - rotation * connectorLocalPosition`。偶数长度面上的连接点可能距中心半格，不能把这种合法偏移强制抹成整数。旋转则保留当前中心；拖拽始终使用开始拖拽的中心和平面，世界增量左乘原姿态。阻挡检查仍按候选中心和姿态进行；阻挡时不更改姿态、存档或历史。

固定中心不保证旋转后原连接点仍接合，例如 2×1×1 的侧面半格连接点绕中心转动后会离开原接点；需要重新连接时可移动方块。围绕连接点旋转会改变中心，是另一种操作语义，本次没有引入自动切换。

## 已执行

- Unity Editor 6000.3.11f1，目标工程 `D:\git_projects\Unity\HY-Sandbox`，脚本重新编译：0 错误/0 警告。
- Main 原场景确认未脏化后切换至隔离空场景进入 Play Mode，运行 [BuildRotationProbe](../../Tools/Validation/BuildRotationProbe.cs)，85 项通过；退出 Play 后重新打开 Main，未保存场景或修改用户蓝图。
- 实际加载 1×1×1、2×1×1、2×2×1、2×2×2 Prefab，经上表面连接点求放置中心并实例化；校验 Awake 不改变求解后的中心。
- 虚拟键盘 W 以世界 X 轴从 Y=90°初始姿态连续旋转四次；中心不变，姿态与世界轴期望一致，四次回到原姿态，Undo/Redo 正确。
- 虚拟鼠标射线通过真实相机和平面计算拖拽，覆盖 15°、45°、90°、135°、回到 0°、-45°和最终 90°；中心始终不变，释放只保存和记录一次，Undo/Redo 正确。
- 真实 PhysX 阻挡使旋转拒绝，不改变姿态、历史或保存次数。
- 同一几何模块复现旧 AABB 吸附反例：2×1×1，中心 (0.5, 1, 0)，Y=15°，旧路径偏移约 (0.095335, 0, 0.241782)m。
- 静态 `dotnet build HY-Sandbox.sln --no-restore`：0 错误/4 个既有警告（3 个依赖版本冲突与 1 个过时诊断 API）；319 份自有脚本可读性检查无格式差异或错误，`git diff --check` 通过。

运行方式（先在没有 SaveManager 的隔离空场景进入 Play Mode）：

```powershell
unity command run_script --file Tools/Validation/BuildRotationProbe.cs --entry BuildRotationProbe.Run --result-only
```

完整机器结果位于本地临时文件 `Temp/build-rotation-probe.json`，不提交缓存。探针使用临时输入设备与对象，保存回调只采样内存，不写用户 JSON；Undo/Redo 的真实存档入口因空场景没有 SaveManager 而直接返回。

## 边界

- 未进行真实鼠标手感、完整存档重载或独立 Player 验证。虚拟输入调用真实操作方法，但不等同于完整 UI 点击和选择流程。
- 既有 `BlockData`/`Block.Awake` 将姿态量化到 90°，本次保留该存档契约；15°用例证明交互时中心稳定，不表示任意角度可精确保存在蓝图中。
- 不修改 Prefab、场景、GUID、序列化字段、放置连接点选择或移动吸附规则。
