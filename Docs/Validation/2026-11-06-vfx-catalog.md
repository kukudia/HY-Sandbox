# 特效引用集中迁移

## 使用

Tools → HY Sandbox → 资源管理 → 特效与音频 → 运行时播放库。

一次性效果和挂载效果都在同一 BlockVfxLibrary 资产选择 Prefab。HoverJet 管理现有悬浮/主推进/通用推进喷口，BotFlight 管理机器人双喷口，EnergyTrail 管理机器人/掉落物尾迹，RepairContact 管理维修接触，MuzzleFlash 管理炮口，LootBurst 单独管理拾取爆发。特殊蒸汽/烟雾保留成两份明确命名的模板，避免外观被标准模板覆盖。

新 Prefab 必须挂载 VfxEffect 并配置 Graphs；模板的 Effect Id 和 Members 必须为空，避免递归。Prefab 内部 Graph、Renderer 参数及层级由模板管理；玩法 Prefab 只保留位置/朝向/缩放、效果编号和自动播放行为。模板替换影响新实例，已有实例不热更新，重新进入 Play 即全部重新加载。

## 改动范围

- `BlockVfxLibrary.Resolve` 统一查询已有一次性槽位和新增挂载槽位，无第二套加载库。
- `VfxEffect.EnsureContent` 每个挂载点只生成一次；机器人组合保存成员引用，避免引用已删除的内部 Graph。
- `VfxCatalogMigration.Run` 使用 Unity AssetDatabase/PrefabUtility/SerializedObject 迁移 29 个 Art/Resources Prefab；54 个挂载点、3 个组合组件。不改第三方原包。
- 对迁移前 Graph/Renderer 序列化数据、局部变换和释放时间计算签名；差异另存模板。迁移不修改挂载组件身份和位置，外部玩法字段保持有效。
- 制作链 `BlockArtIntegrator` 和 `BlockVfxBaker.Populate` 改为生成编号挂载点；模板生产明确用 `PopulateTemplate`。BakeMissing 只补空配置，不覆盖用户替换。
- 旧 NativeVfxMigration 验证同步识别目录挂载点和组；音频资源索引保持共用，未引入音频播放系统。
- 恢复分支 `codex/vfx-central-baseline-20261106` 指向迁移前提交 b451c88。

## 实际验证

Unity 6000.6.8f1 连接本项目导入编译，生成新 meta 和 csproj 项。

1. `VfxCatalogChecks.CheckAssets()`：54 个挂载点全部可解析模板，3 个组合控制器；迁移控制器无分散 Graph 引用。
2. 从 Git HEAD 导出全部 29 个原 Prefab 到独立临时 Assets 目录，用 `CheckPreservation` 实际导入对照。57 个组件 fileID、局部变换保持一致，所有叶效果的 Graph/Renderer 参数与集中模板签名完全一致。验证后由 AssetDatabase 删除临时目录。
3. 实际 Play Mode 空场景隔离夹具：从真实推进器/机器人/炮塔/掉落物 Prefab 克隆挂载点，检查加载、自动播放、双喷口、停止、禁用/再启用无重复内容。
4. Play 中临时将 HoverJet 库项替换为 RepairContact，真实推进器新挂载点使用新的 Graph，旧实例保持既有 Graph。finally 还原库配置并清理夹具；退出 Play 后恢复原 Siege 场景。
5. 重跑迁移前后逐文件比较全部项目 Prefab 与配置库字节一致。资源与场景 GUID 不重建；最终索引为 215 个特效资产、410 个含依赖资产。Art/Resources 中除库模板外，不再存在直接 VisualEffect 组件。
6. 显式 .NET SDK 9.0.320 构建（本机缺少锁定的 9.0.318）：0 错误、22 个程序集引用警告。CodeReadability、生成导航和 git diff 检查执行于最终版本。

日志位于忽略的 Logs/vfx-central-*。Console 中保留此前 Pipeline 超时和一次 CSV 导入共享冲突记录，最终检查未出现新的 VFX 错误；不声称全程 Console 零错误。

## 未覆盖

本轮验证配置迁移及真实 Play 控制/实例生命周期，不等于全部 GPU 特效逐帧视觉验收。未执行 Player 构建、长期战斗压力或音频实际听觉验收。主场景未保存或重建。
