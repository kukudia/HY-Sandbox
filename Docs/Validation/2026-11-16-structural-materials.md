# 基础结构材质验证

Unity6000.6.8f1，Main，2026-11-16。恢复点：P1提交d74c0bd。

## 实施

新增可编辑BlockMaterialCatalog、BlockMaterial、MaterialBreakEffect和真实Main材质选择器。四种基础Prefab保持原GUID/表面，空materialId保留历史行为；新放置默认金属。造价、质量、耐久按实例体积。材质选项不修改已放置实体，不引入背包数量限制。

CSV仍拥有功能Prefab参数；材质只应用预挂支持组件的结构模块。存档工厂在创建前拒绝未知/不支持材质，恢复尺寸及材质后再初始化健康。禁用父级的CSV延后Awake原本覆盖尺寸，现由Block记录待恢复尺寸，在自身Awake先重申。建筑激活另按快照恢复受损耐久；材质刷新保留健康比例，不免费修复。建筑合并缓存加入材质ID避免串用表面，静态蓝图预览共享材质。

## 已验证

- BlockMaterialChecks.Run：28项。四种材质参数、实际Renderer、JSON工厂往返、健康比例，旧参数兼容，未知/功能模块拒绝，延后CSV与非默认尺寸，真实建筑受损恢复/捕获、Siege报价。
- 真实玻璃ParticleSystem播放，无Collider/Rigidbody；连续41次请求最多32活跃效果，夹具清理归还预算。
- SiegeBuildingDamageChecks：18项；BlockCsvChecks.RunPlayMode：4项。
- CSV Edit17项通过，重复材质Install目录原始字节不变；dotnet build0错误/37项既有obsolete API警告，格式0差异，diff检查通过。
- Unity真实导入/编译0错误。制作初期测试程序集访问与UI模板的双Graphic配置错误已修复；未将历史错误掩盖为始终无错误。
- 静态URP预览已查看四种表面；真实ScreenCapture（含Overlay UI）检查下拉文字、箭头、四项列表和选择glass。普通camera screenshot不包含Overlay UI，不作该UI证据。截图存于本机运行证据目录。

验证期间出现一次在Play退出尚未完成时重开场景，触发既有BuildManager/InputManager缺依赖异常；已停止该测试会话并重新从保存Main启动。此类错误记录保留，不把历史累计Console数写成当前编译失败。

## 剩余缺口

实际破坏动画的动态观感、完整建造/撤销交互、多舰与大规模透明排序/overdraw、Player构建未验收。玻璃无折射，纹理为保留生成器的程序化64x64细节；平衡数值暂定。静态构建与格式结果见提交Validation。

后续进展（同日）：包含四材质的新版本已成功构建独立Player并部署金属模块机库，见[Player验证](2026-11-16-siege-gameplay-player.md)。没有在Player逐材质验收动态毁坏、透明排序与完整编辑链，以上缺口仍适用。
