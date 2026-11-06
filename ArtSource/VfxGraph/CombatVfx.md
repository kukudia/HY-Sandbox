# 战斗特效规格与源制作

2026-11-06；Unity 6000.6.8f1 / URP 与 VFX Graph 17.6.0；桌面 Compute 平台。目标是在中远景辨认残骸方向、激光射线与炮弹运动。以正交可视高度 12m 和 24m 作为初始视觉验证基线，不代表全部实战镜头。

| 资产 | 形态与参数 | 预算与编辑入口 |
| --- | --- | --- |
| DebrisSmoke | 世界空间高不透明度灰烟，出生前 0.50s 带橙色余火；8×8 图集逐帧扩散，粒子寿命 2.8s、尺寸曲线 0.35→2.2m；向上漂移并横向散开 | 38 粒子/s、容量 192、2 个共享粒子 Output，保留最多 24 条烟迹门槛；Prefab 停止后 4s 回收 |
| LaserBeam | 沿局部 +Z 的收尖亮芯与两层橙红外晕；激光宽度参数 0.16m，可见时间 0.2s；外晕比亮芯更宽 | 单粒子、3 个 Mesh Output；无附加动态灯；LaserTurret Inspector 调宽度/时间/HDR 色 |
| ProjectileTracer | 与激光共用 CombatBeam 图，独立 Prefab 外晕强度 0.7；真实世界位置历史形成 0.09s 短迹，宽度 0.1m、长度最多 7m | 每炮弹 32 槽固定环形缓冲；池化重用时清空；TurretShell 上 ProjectileTracer 调参 |

源制作入口：`Assets/Editor/Presentation/Effects/CombatVfxBaker.cs`。通过 Editor Graph API 创建真实 `.vfx`、Prefab 和 Mesh 资产；菜单 **Tools / HY Sandbox / 资源管理 / 制作战斗特效**。只补缺失资产，不覆盖已存在图、模板或非空库选择；首次将旧 DetachedSmoke 默认槽迁入 DebrisSmoke。若需要全新再生，在受版本控制的隔离分支或备份后移走目标资产再执行，不能覆盖用户手改图。

输出：`Assets/Art/VFX/Graphs/{DebrisSmoke,CombatBeam}.vfx`，`Assets/Art/BlockVisuals/VFX/{DebrisSmoke,LaserBeam,ProjectileTracer}.prefab`，以及 `CombatBeamMesh.asset`。Mesh 为 48 顶点、72 三角形、局部 Y 轴长度 2，经 Graph 旋转缩放映射到局部 +Z 长度 1；3 个输出共 216 三角形/束。使用纯色，UV 不适用；无碰撞、骨骼、LOD。

烟雾使用现有 UNI `uni_smoke_cloud.png` 图集及其原导入设置，不重新生成或复制纹理；来源与许可继承项目已有 UNI VFX 包，见 `Assets/Art/VFX/Sources.json`。光束 Mesh、曲线与新图由本项目制作脚本生成。没有外部生成服务或付费资源调用。

集中库入口：残骸烟雾槽、LaserBeam、ProjectileTracer。普通 EnergyBeam 继续服务维修等效果，不扩大它们的宽度/亮度。伤害、物理弹速、弹道扫掠和炮塔射击频率不改。残骸销毁后停止发射，但已有烟粒继续自然老化。当前陨石同样通过共享残骸烟雾槽取得世界空间烟迹。

验证源：`CombatVfxCapture.StartCapture` 拍摄近/远两档视野、销毁后尾烟；`CombatVfxChecks.RunPlay` 检查真实炮弹池复用和历史重置。最新验证记录见 `Docs/Validation/2026-11-06-combat-vfx.md`。

尾烟可见性调参（2026-11-06）：烟雾 alpha 曲线由峰值 0.48 / 中段 0.28 提高为 0.92 / 0.68，灰烟色乘数改为 (2.1, 2.15, 2.2)。DetachedPartSmokeTrail 的 minimumOpacity 默认 0.85，避免中低速再次把整条尾烟压成近乎透明；发射门槛和停发规则不变。第二个 Additive Output 复用相同世界空间粒子，橙色 (12, 2.4, 0.18)，尺寸 0.28→0.52→0.2m，alpha 在归一化寿命 0.18 处归零（约 0.50s），不增加动态灯。上述参数均在真实 Graph 或组件 Inspector 中编辑；制作入口仍只补缺失资源。

包裹火焰（2026-11-06）：集中库 DebrisSmoke.prefab 新增局部空间 DebrisEnvelope.vfx。模型 MeshRenderer/SkinnedMeshRenderer 的局部包围盒合并为燃烧体积（只统计同一刚体），扩张 1.65 倍；单粒尺寸为模型最短边的 3 倍、最低 0.35m。燃烧层跟随模型中心与旋转，世界空间尾烟保持独立。使用项目已有 UNI uni_fire.png（16×8，帧 0→25），来源和许可继承 Sources.json；90 粒子/s、0.45s 寿命、容量64，一个 Additive Output，无新增灯光。制作入口 DebrisFireBaker.BakeMissing，由 CombatVfxBaker 调用；组件 Inspector 调 flameEnvelopeScale/flameCoverage，Graph 调颜色、帧序与密度。适合紧凑残骸的体积包裹，不是精确网格表面采样；极细长和中空结构需后续按具体模型验收。

当前烟火平衡修订：包裹火焰发射量 90→60/s，色乘数 (2,1.3,0.7)→(1.1,0.8,0.45)，alpha 峰值 0.8→0.65、中段 0.65→0.4，Coverage 3→2.5。保留体积扩张 1.65 以覆盖模型前侧，粒子随机滚转 ±180°。同组粒子增加灰黑烟输出（alpha 峰值0.32），使局部烟火交错；新增绘制输出，不增加模拟粒子。世界尾烟尺寸曲线改为0.65→1.35→2.4m、颜色乘数(1.25,1.28,1.3)，余火乘数(5,1.2,0.12)。旧段落保留此前参数演进，本段为当前值。
