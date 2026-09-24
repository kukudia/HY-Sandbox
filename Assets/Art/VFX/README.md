# 原生 VFX Graph 特效

Unity 6000.3.11f1 / URP 与 VFX Graph 17.3.0，1 Unity 单位 = 1 米。目标为支持 Compute 的桌面平台。保持 UNI 原生纹理、运动向量、网格和主要节点结构；推进尾焰使用删除烟雾系统的火焰变体。没有使用 Particle System 重制。

## 映射与编辑

| Prefab（Assets/Art/BlockVisuals/VFX） | Graph |
| --- | --- |
| Explosion / BreakBurst | UNI Aerial / Small Explosion |
| ImpactBurst / MuzzleFlash | UNI Impact Explosion，枪口缩小 |
| SmokeBurst / DetachedSmoke | UNI Small Smoke Impact / Device Fire |
| ThrusterJet / HoverJet / BotFlight | UNI Gas Fire Thruster（无烟） |
| RepairContact / RepairPulse / BuildBurst | EnergyContact / EnergyBurst |
| EnergyTrail / EnergyBeam | 世界空间运动历史拖尾 / 局部空间 GPU 网格光束 |

原生 Steam Leak 同时保存在素材库，可用于手动替换漏气效果。维修、建造、拾取与能量光束使用项目补充 Graph；这些效果没有匹配的 UNI 火烟素材。

- Prefab、Graph、贴图、运动向量、Shader Graph、网格全部位于可跟踪的 Assets/Art。原包保持不动，来源/许可证沿用用户本地 UNI VFX 包条款，Sources.json 记录源路径、源哈希、目标路径、GUID 与目标哈希。
- TGA 无损转为 PNG，保留原始分辨率和像素，并复制 Unity 纹理导入设置。ArtSource/VfxGraph/prepare_textures.py 可复现转换。
- VfxEffect 的 Graphs 数组、回收延时、自动播放开关均可在 Inspector 编辑。连续效果通过 Intensity 调透明度，避免低推力发射断续；Stop 保留最后透明度以自然消散，Clear 显式清空 GPU 模拟。
- UNI Gas Fire 原生喷射轴为 -X，Prefab 子节点旋转至挂点 +Z；推进器变体保留三套火焰系统，删除完整烟雾系统，模拟统一为 Local。ScaleWSP 随实例实际缩放。修改喷口位置请编辑 Block Prefab 的 Nozzle 挂点。
- 爆炸、命中、火焰、烟雾及蒸汽系统使用 Local 模拟空间，随宿主移动和转向；EnergyTrail 使用 World 模拟空间保留已发射粒子的轨迹，发射位置输入为 Local。不要把拖尾改为 Local，否则移动时轨迹会整体跟随物体。
- Block 内的效果已完全解包，模板调参不会自动覆盖这些副本；共享 Graph 节点修改会作用于全部引用。原有 Prefab 和控制器脚本 GUID 保留。
- 四种主/全向推进器的现有喷口使用 `HoverJet` 模板，普通/大型实例按对应悬浮推进器的世界尺寸保存；`ThrusterJet` 模板仍供其他素材使用。
- 单次效果最多 64 个并存，分离烟迹最多 24 个；停止后允许 12 秒尾烟回收。CullNone 保证离屏继续老化，防止返回镜头时重新播放。大型战斗 GPU/Overdraw 仍需测量。

## 工具

Tools/HY Sandbox/VFX Graph：

1. Prepare UNI Dependency Manifest：收集原生图与完整依赖。
2. 运行 `python ArtSource/VfxGraph/prepare_textures.py`，然后 Import Native Graphs。
3. Create Utility Graphs：补齐能量图；已有参数化图不覆盖。
4. Migrate Project Prefabs：只迁移仍含旧粒子的项目 Prefab，通过 Editor API 保存和绑定。
5. Capture Lifecycle Suite：在实际 Play Mode 和自动相机渲染中采样，结束恢复原场景。
6. Rebuild Position-Safe Effects：生成并检查无烟推进器变体，调整 Device Fire/Steam Leak 模拟空间，重绑项目 Prefab。
7. `NativeVfxPlaybackProbe.RunBlockTest(prefab, output)`：在实际 Play Mode 中隔离拍摄 Block 的连续喷焰，停机、清空后恢复原场景。

VfxGraphAuthoring 是 Unity 6000.3 内部编辑器 API 的隔离桥接；运行时没有反射，也不依赖此工具。重新导入会重映射图依赖；手动改图后应先提交再执行来源重建。

## 验证说明

Validation.json 检查项目 Prefab 残留组件、控制器、Graph、渲染器和依赖闭包。Preview 下按效果保存自动渲染 PNG 与实际帧/GPU 数量；Position_* 为世界坐标约 (1000, 0, 1000) 且物体横向移动的采样，Block_* 为主/悬浮大型喷口对比。0–3 为发射，3 之后停止，4 之后清空，5 之后重播，6 为重播结果。清空后的 GPU 计数可能为负，表示尚无读回数据，不能当成有负数粒子，也不能替代屏幕验证。

Assets/Art/BlockVisuals/PlayModeValidation.json 检查真实供电、炮塔伤害、推进、维修、回舱和回收。旧 Block/SpaceKit 缩略图仅作模型/挂点历史参考。正式构建和大规模战斗性能不在这次验证范围内。
