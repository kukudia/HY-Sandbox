# 独立Development Player据点制作验证

建筑制作快捷键现有策略仅允许UNITY_EDITOR或DEVELOPMENT_BUILD；本次保持该边界，新建独立D3D11 Development验证包，不打开普通发布包的制作权限。BuildAuthoring入口和发布Build共用经过检查的BuildPlayer实现，输出分别为OutpostAuthoringValidation与WorldPerformanceValidation，不发布二进制。

Unity6000.3.11f1 Development Windows Player，D3D11、请求1920×1080，seed14741，`-world-smoke-outpost-editor`。真实Input System Ctrl+Alt+F进入建筑模式，通过Main序列化按钮复制Supply Stronghold Outpost（完整建筑及三驻军）、修改X=-48/Yaw=90、Save pose原子保存并重新加载，再用快捷键退出。三个文件仓在测试作用域内被替换为独立缓存目录，finally恢复原引用和列表，随后只读重载原玩家载具。

30类检查通过、errors为空、进程正常退出，见[报告](2026-10-16-outpost-editor-player.json)。检查覆盖实际活动面板、资源模板存在、缓存路径边界、三个驻军范围网格/无碰撞、构建后的URP Unlit Shader存在且isSupported、pending文件消失、部署位置重载、退出预览清理和原载具恢复；之后普通世界跨列/退出再准备也通过。

Development构建0错误/1警告。该包带开发诊断，阶段时间不作为发布性能证据；隐藏Player截图不作为视觉证据。Editor实际画面沿用[部署预览报告](2026-10-16-outpost-preview.md)，本次补齐独立构建操作及Shader可用性，不声称独立Player的美术/图形帧率验收。
