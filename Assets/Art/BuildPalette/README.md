# 建造目录与连接点提示

主场景的 `MainCanvas/BuildPanel/ButtonContent` 保存完整目录：分类导航、滚动视口、23 个图标按钮、名称提示及滚动条。分类为 All / Structure / Flight / Power / Combat / Salvage，沿用当前界面的英文字体。Bot 和 Connector 不是可选建造块。

- `BuildPaletteItem`：Inspector 中编辑分类、名称及边框引用；名称在鼠标悬停或键盘焦点时出现。
- `BuildPalette`：管理筛选、滚动归位和选中颜色；不在运行时加载全部 Prefab 或生成缩略图。
- `ConnectorPlacementHints`：Main 的 BuildManager 上配置；默认宽高 0.9、向连接面外偏移 0.015，白色无光照材质。只显示指向方块的 `canConnect && !isConnected` 连接点，正常深度遮挡，不额外生成 Collider。
- 新增模块或修改模型后，保存 Main，再使用 `Tools > Build Palette > Bake thumbnails and refresh Main`。该工具重建目录的生成布局、默认分类和预览，保留目录之外的工具按钮与面板；会在 Temp/BuildPalette 保存重建前场景副本。手调目录后勿无意重复 Bake。
- 验证入口：打开 Main，执行 `Tools > Build Palette > Run Play Mode validation`。测试只在 Play Mode 副本中改 UI 和场景，测试结果与截图保存在 Temp/BuildPalette。

## 资产规格与来源

图标从现有 Resources/Blocks Prefab 在独立 URP PreviewScene 渲染，源 PNG 为 640×520 RGBA，导入限制到 256 像素，透明底、固定镜头/灯光、无粒子。按钮为 84×84，四列滚动排布。金币/科技仓预览克隆填充 65%，只用于区分图标，不修改 Prefab 初始库存。圆角边框由 Bake 工具生成，128×128 RGBA、九宫格拉伸；世界线框使用 72 顶点共享 Mesh，无逐帧对象或材质创建。

制作源为 `Assets/Editor/BuildPaletteBaker.cs` 和 `BlockArtPreview.cs`，不依赖外部生成服务。模型和材质继承现有项目资产来源与许可，未引入新的第三方素材。
