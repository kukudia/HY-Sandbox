# UNI 战斗特效适配

Unity 6000.3.11f1 / URP 17.3。使用本地 UNI VFX 的真实序列贴图合成原生 Particle System，无 VFX Graph、Compute Shader 或运行时生成粒子的依赖。最终 Prefab 在 `Assets/Art/BlockVisuals/VFX`，现有 GUID、根控制器和资源库引用保留。

| Prefab | 设计 | 总粒子容量 |
| --- | --- | --- |
| Explosion | 核心火球、5 个扩散火球、热碎片、点火闪光、冲击环、压力烟与翻滚余烟 | 45 |
| BreakBurst | 半尺寸爆燃，用于拆除/摧毁 | 29 |
| ImpactBurst | 五分之一尺寸爆燃，用于炮塔命中 | 29 |
| SmokeBurst | 压力烟与翻滚余烟，用于延迟余震 | 8 |
| DetachedSmoke | 世界空间火焰、翻滚烟迹和余烬；按速度/剩余寿命调节发射量 | 49 |

尺度假设：1 Unity 单位 = 1 米，桌面平台、现有战斗镜头。爆炸调用者仍可按模块尺寸缩放。瞬时效果 3.2 秒回收，残骸停止发射后粒子最多 1.5 秒，兼容现有 2.4 秒残留回收；沿用 64 个瞬时实例/24 个断裂烟迹全局上限。原有蓝色推进喷口、维修电弧、建造和炮口反馈保留其用途。

## 可编辑入口

- 打开对应 Prefab，直接编辑每个命名子层的尺寸、发射数、颜色、渐变、寿命、速度和 8×8 Texture Sheet Animation。
- `Materials` 使用 `Universal Render Pipeline/Particles/Unlit`，火球/烟使用 Alpha Blend，闪光/余烬/冲击环使用 Additive。不新增动态灯。
- 源 PNG 保留完整 4096×4096 像素；Unity 导入上限 2048，HQ 压缩、MipMap、Clamp。可根据性能预算在 Inspector 调整。
- `Tools/HY Sandbox/UNI VFX/Bake Combat Effects` **会重建上述五个 Prefab**，执行前提交手动调整；日常手调不需重新 Bake。不会修改模型、挂点或玩法数据。
- `Validate Dependencies` 检查资源闭包；`Render and Validate Playback` 生成 25 张 URP 分时预览与粒子生命周期报告；`Run Play Mode Probe` 在隔离场景验证工厂触发、分离残骸烟火、停止/重启/回收并返回原场景。
- 原 `Repair Particle Continuity` 会跳过 UNI 材质的效果，防止旧曲线覆盖新美术。

## 来源、源文件与复现

来源：用户本地 `Assets/UNI VFX/Realistic Explosions, Fire & Smoke` 与 `Common/Textures/uni_glow.png`。许可沿用原包条款；未独立核实商业授权，不将筛选素材作为独立素材包分发。

`Sources.json` 记录原路径、目标路径、SHA-256 和像素一致性。`ArtSource/UniVfx/prepare_textures.py` 用 Pillow 将 4 张 TGA 与 glow 无损存为 PNG（5 张逐像素核对），并生成可复现的柔边冲击环。项目根目录运行 `python ArtSource/UniVfx/prepare_textures.py`，再从 Editor Bake。原始 UNI 资源不修改。全部依赖在 Art 或 Unity/URP 包内，均不依赖 gitignore 目录。

## 验证范围

2026-09-24：五个 Prefab 保存重载、材质/贴图与忽略目录引用检查通过；25 个分时采样和实际 URP 渲染已检查；发射、结束、重播、禁用清理检查通过。Play Mode 七项工厂/残骸/回收检查通过，见 `PlayModeValidation.json`。并发 GPU 性能、复杂战斗镜头和正式构建尚未验证。
