# Siege 玩法重构方案分析

## 现状问题诊断

### 1. **UI 混乱的根本原因**
- **核心问题**：UI 层混合了三个不同的系统
  - `SiegeHud`（整备/战斗专用界面）
  - `Main` 场景的完整 UI 系统（BuildPanel、PlayPanel 等）
  - `SiegeWorkshop`（蓝图编辑/整备切换）

- **症状**：
  - `SiegeHud` 的 SerializedFields 过多（约 30+ 个，包括 baysGroup、refitGroup、dockPanel 等）
  - 多个 UI 面板同时存在但状态管理散乱（deployGroup、combatGroup、shipStatus、refitGroup）
  - `Main` 的 MainUIPanels 仍在场景中，但大多被禁用，造成冗余
  - `FitLayout()` 函数需要手动调整约 10+ 个 UI 元素位置

### 2. **功能臃肿**
- **不必要的 Bays 抽象**：
  - SiegeHud 中的 `deploy` 和 `takeControl` 按钮数组固定为 4 个泊位
  - 对应的 UI 布局代码在 `ApplyDockLayout()` 中硬编码了"整备 vs 战斗"两种模式
  - 部署验证与修缮逻辑分散在 SiegeFleet / SiegeRefit / SiegeHud 中

- **冗余的修缮系统**：
  - `SiegeRefit` 同时处理部署验证、报价、修缮
  - `SiegeWorkshop` 复用 Main 的完整蓝图编辑器（包括材质、命名、AI 等无关选项）
  - 修缮报价刷新散乱（`RefreshRefitQuotes()` 在 OnDisable、UpdatePhase、RepairCore 等多处被调用）

- **Workshop 的"隐藏"设计**：
  - 在编辑器中隐藏战场舰队和建筑，在运行时恢复
  - `SiegeWorkshop` 持有 50+ 行的渲染器/碰撞体列表管理代码
  - 没有真正的"沙箱"，只是视觉上的隐藏

### 3. **玩法层的问题**
- **SiegeFleet 职责不清**：
  - 同时管理舰队生命周期、驾驶权、摄像头、停靠物理状态
  - `Spawn()` → `Deploy()` → `TakeControl()` → `SetCombatActive()` 的状态转移不明确
  - 与 `SiegeMatch`（纯经济模型）的边界模糊

- **SiegeMatch 过于简化**：
  - 只是数据容器（金币、波次、敌机名单）
  - 所有事件驱动逻辑都在 `SiegeSession` 中，导致 Session 失控

---

## 重构方案

### 方案 A：激进重构（完全拆分）
**目标**：彻底解耦 Siege 与 Main UI，为未来的自定义 UI 铺路

#### A1. UI 层重构
```
重构前：
├── SiegeHud（~650 行，处理所有 UI）
├── SiegeWorkshop（调用 Main 的编辑器）
└── MainUIPanels（多个被禁用的面板）

重构后：
├── SiegeUIController（~300 行）
│   ├── 只负责 Siege 特有的显示：经济、波次、横幅、击毁提示
│   └── 纯数据驱动（监听 SiegeMatch / SiegeFleet 事件）
│
├── SiegeDockPanel（~150 行）
│   ├── 显示泊位状态与切换按钮
│   ├── 部署按钮的启用/禁用逻辑
│   └── 修缮报价（绑定 SiegeFleet.Driven）
│
├── SiegeEditorView（~200 行）
│   ├── 隔离的蓝图编辑界面
│   ├── 只暴露"保存"和"返回"两个按钮
│   └── 不复用 Main 的任何 UI 组件
│
└── SiegeEventBar（~50 行）
    ├── 显示横幅、击毁、核心受击
    └── 纯展示，无交互
```

**收益**：
- SiegeHud 行数减少到 ~200 行，职责单一
- 新增UI 时无需修改 SiegeSession
- 方便后续切换到 Unity UI Toolkit

**工作量**：★★★☆☆（1-2 天）

---

#### A2. 修缮流程清晰化
```
重构前：
SiegeHud.RepairCore() 
  → SiegeSession.TryRepair(isBase)
  → SiegeRefit.Repair() / SiegeFleet.Quote()
  → SiegeMatch.TrySpend()
  → RefreshRefitQuotes() （多处重复调用）

重构后：
SiegeDockPanel.OnRepairClicked()
  → SiegeFleet.TryRepair(ship, quote)  // 新增，处理修缮逻辑
  ├── SiegeRefit.Repair()            // 物理/几何验证
  └── SiegeMatch.TrySpend()          // 经济验证
  → SiegeDockPanel.RefreshQuotes()   // 单一刷新入口

修缮报价 = SiegeFleet.GetRepairCost(ship)  // 查询 API
```

**收益**：
- 修缮报价只在"部署后"、"修缮成功后"、"打开修缮面板时"刷新
- 消除高频的 `Quote()` 遍历

**工作量**：★★☆☆☆（4-8 小时）

---

#### A3. 舰队模型清晰化
```
SiegeFleet 职责拆分：

SiegeFleet（舰队编队管理，~300 行）
├── Ships[]
├── Driven（当前驾驶权）
├── Base
├── Deploy(option) → SiegeFleetEntry  // 生成+登记
├── TakeControl(slot)                 // 权限转移
├── NotifyLost(cockpit)               // 失败处理
└── 纯数据，不触发摄像头/物理

SiegeFleetPhysicsController（~150 行）
├── SetCombatActive(bool)             // 控制 Rigidbody.isKinematic
├── SetParked(unit)                   // 波次间的停靠
└── RefreshBindings()                 // AI 启用/禁用

SiegeCameraController（~80 行）
├── FocusShip(ship)                   // 摄像头跟随
├── FocusBase()
└── RestoreView()
```

**收益**：
- SiegeFleet 从"舰队+物理+摄像头+修缮" 降低到 "纯编队管理"
- 便于单元测试（不涉及 Physics/Camera/UI）
- 摄像头逻辑不再污染 Fleet

**工作量**：★★★☆☆（1 天）

---

### 方案 B：渐进式改进（短期见效）
**目标**：在保持现有架构的基础上，快速消除混乱感

#### B1. UI 面板合并
```
拆除 5 个面板，统一到一个 SiegeUIRoot：

现状：
├── deployGroup
├── combatGroup
├── refitGroup
├── baysGroup
└── shipStatus

改为：
├── SiegeControlCenter（整个 UI 根）
│   ├── StrategyBar（顶部：经济、波次、核心）
│   ├── FleetDock（左侧：泊位 + 修缮）
│   ├── CombatStatus（右侧：波次进度 + 座舰状态）
│   ├── Banners（居中：横幅、击毁、告警）
│   └── WorkshopRoot（蓝图编辑器，平时隐藏）

SiegeHud 只需：
├── OnEnable：注册按钮
├── Update：刷新数值（250ms 节流）
├── OnPhaseChanged：切换显示/隐藏
└── (删除所有布局代码)
```

**收益**：
- SiegeHud 从 ~650 行降到 ~250 行
- UI 更新逻辑从数十个 Update 条件简化为阶段机器

**工作量**：★★☆☆☆（6-8 小时）

#### B2. 修缮报价缓存
```cpp
// 在 SiegeFleet 中添加
private Dictionary<SiegeFleetEntry, long> repairCostCache = new();

public long GetRepairCostQuick(SiegeFleetEntry ship)
{
    if (session.Match.Phase == SiegePhase.Battle)
        return 0;  // 战斗中不显示报价
    
    if (!repairCostCache.TryGetValue(ship, out long cost))
    {
        cost = SiegeRefit.Quote(ship, session.Settings);
        repairCostCache[ship] = cost;
    }
    return cost;
}

// 只在这两处清空缓存：
// 1. SiegeFleet.Deploy() 后
// 2. SiegeFleet.RepairBetweenWaves() 后
// 3. SiegeSession.BeginNextWave() 时
```

**收益**：
- 消除每帧 Quote() 的性能问题
- RefreshRefitQuotes() 改为一行调用

**工作量**：★☆☆☆☆（1-2 小时）

#### B3. 硬编码泊位变为配置
```csharp
[SerializeField]
private SiegeBayConfig[] bays = new SiegeBayConfig[4];

[System.Serializable]
public class SiegeBayConfig
{
    public Button deployButton;
    public Button takeControlButton;
    public Text statusLabel;
}

// SiegeHud 中：
private void UpdateBays()
{
    for (int i = 0; i < bays.Length; i++)
    {
        SiegeFleetEntry ship = session.Fleet.Ships[i];
        bays[i].UpdateDisplay(ship, session.Match.Phase);
    }
}
```

**收益**：
- 支持 2-6 个泊位的灵活配置
- 删除 UI 布局硬编码（100+ 行数字）

**工作量**：★★☆☆☆（4-6 小时）

---

## 对比分析

| 维度 | 方案 A（激进） | 方案 B（渐进） | 现状 |
|------|--------------|-------------|------|
| UI 代码行数 | ~300 | ~400 | ~650 |
| 修缮报价性能 | 极好 | 中等 | 差 |
| 模块独立性 | 高 | 中等 | 低 |
| 开发周期 | 2-3 天 | 1-2 天 | - |
| 风险 | 中等（广泛改动） | 低（局部改进） | - |
| 后续扩展性 | 优秀 | 中等 | 差 |
| 技术债清理 | 彻底 | 部分 | - |

---

## 推荐方案：A + B 混合

### Phase 1（8 小时）- B2 + B3
- 修缮报价缓存（快速胜利）
- 泊位配置化（支持不同规模）

**里程碑**：UI 混乱感显著降低，代码行数 ~550

### Phase 2（1 天）- B1
- UI 面板合并、阶段切换简化

**里程碑**：SiegeHud 可读性显著提升，维护成本 -50%

### Phase 3（1-2 天，可选）- A1 + A2 + A3
- 完整的模块分离
- 为新 UI 系统预留接口

**里程碑**：架构清晰，可独立迭代任何模块

---

## 具体改进清单（优先级）

### 高优先级（影响用户体验）
- [ ] 修缮报价缓存（性能）
- [ ] UI 面板合并（视觉混乱）
- [ ] 部署验证错误信息明确化（用户困惑）

### 中优先级（代码质量）
- [ ] SiegeFleet 拆分物理控制器
- [ ] 泊位数量配置化
- [ ] SiegeSession 事件系统化

### 低优先级（长期维护）
- [ ] 蓝图编辑器隔离（当前设计能用）
- [ ] Unity UI Toolkit 迁移（资源投入大）

---

## 风险评估

### 方案 A 的风险
1. **广泛改动**：涉及 8+ 个文件，合并冲突可能性高
   - 缓解：分 3 个阶段，每阶段后留出测试窗口

2. **Workshop 重写**：蓝图编辑器集成点多
   - 缓解：新的 SiegeEditorView 完全独立，可逐步迁移

3. **摄像头逻辑移动**：涉及 PlayManager / CameraController
   - 缓解：SiegeCameraController 只是委托器，实际逻辑不变

### 方案 B 的风险
- 低风险，局部改进，易于回滚

---

## 验证标准

### 代码质量
- [ ] 单个类 < 400 行（除了 SiegeSession）
- [ ] 无类持有 15+ 个 SerializedFields
- [ ] 修缮流程<3 个方法调用深度

### 性能
- [ ] 修缮报价查询 < 0.1ms（当前可能 > 1ms）
- [ ] UI 更新 < 0.5ms（已可达到）

### 可读性
- [ ] 新人能在 30 分钟内理解舰队/整备流程
- [ ] 类名能直观反映职责
