# 自有代码导航

由 Roslyn 实际语法节点生成；脚本介绍定位职责，方法链接定位实现。不是行为测试报告。完整入口链见 [RuntimeFlow](RuntimeFlow.md)。

## Assets/Editor/Architecture/ArchitecturePlayProbe.cs

// 职责：在一次临时 Play Mode 会话中回归蓝图加载、文件命名和实体恢复的架构契约。 // 入口：Tools/Architecture/Run Play Mode validation；结果写入 Temp/Architecture/Validation.json。 // 隔离：暂停场景业务，反射装配真实加载器；只写随机前缀临时目录，finally 恢复管理器状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ArchitecturePlayProbe | [`void Run()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L47) | public static |
| ArchitecturePlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L70) | private static |
| ArchitecturePlayProbe | [`void Tick()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L110) | private static |
| ArchitecturePlayProbe | [`void Capture(string message, string stack, LogType type)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L146) | private static |
| ArchitecturePlayProbe | [`void Finish(bool exitPlayMode)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L162) | private static |
| ArchitecturePlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L197) | private static |
| ArchitecturePlayProbe | [`void PrepareIsolation()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L344) | private static |
| ArchitecturePlayProbe | [`void PauseSceneUpdates(bool stopStartupCoroutines)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L411) | private static |
| ArchitecturePlayProbe | [`void VerifyFileStore()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L423) | private static |
| ArchitecturePlayProbe | [`BlockData[] CreateOrdinaryBlueprint()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L460) | private static |
| ArchitecturePlayProbe | [`BlockData MakeBlock(string resourcePath, int index)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L489) | private static |
| ArchitecturePlayProbe | [`void VerifyRestoredComponents(BlockData[] data, UnitFaction faction)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L524) | private static |
| ArchitecturePlayProbe | [`string WriteBlueprint(string suffix, BlockData[] blocks, bool enemy = false)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L546) | private static |
| ArchitecturePlayProbe | [`void Load(string name, bool enemy = false, Action initialBlock = null)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L554) | private static |
| ArchitecturePlayProbe | [`Type RuntimeType(string name)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L580) | private static |
| ArchitecturePlayProbe | [`object Invoke(object target, string method, params object[] arguments)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L585) | private static |
| ArchitecturePlayProbe | [`object Field(object target, string field)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L590) | private static |
| ArchitecturePlayProbe | [`void SetField(object target, string field, object value)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L595) | private static |
| ArchitecturePlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L600) | private static |
| ArchitecturePlayProbe | [`void RestoreIsolation()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L610) | private static |

## Assets/Editor/Architecture/HoverControlPlayChecks.cs

// 职责：以真实运行组件验证悬浮控制的有效归属、停供和禁用清理契约。 // 入口：Run()；由 ArchitecturePlayProbe 在暂停场景业务的 Play 会话中调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverControlPlayChecks | [`string[] Run()`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L11) | internal static |
| HoverControlPlayChecks | [`void Tick(HoverFlightController controller)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L122) | private static |
| HoverControlPlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L127) | private static |

## Assets/Editor/Architecture/PresentationPlayChecks.cs

// 职责：在真实 Play 会话中验证镜头/面板协程取消与临时 VFX 的销毁生命周期。 // 入口：Run(checks)；外层探针逐步 MoveNext，Current 是以秒为单位的等待时长。 // 生命周期：仅创建隔离对象；恢复单例与 Random 状态，不控制 Editor 或改动场景资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PresentationPlayChecks | [`IEnumerator Run(List<string> checks)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L16) | internal static |
| PresentationPlayChecks | [`GameObject Own(List<GameObject> owned, string name, Transform parent = null)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L279) | private static |
| PresentationPlayChecks | [`Type RuntimeType(string name)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L291) | private static |
| PresentationPlayChecks | [`object CreateRuntimeObject(string name, MonoBehaviour runner)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L296) | private static |
| PresentationPlayChecks | [`object GetField(object target, string name)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L301) | private static |
| PresentationPlayChecks | [`void SetField(object target, string name, object value)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L306) | private static |
| PresentationPlayChecks | [`void Invoke(object target, string name, params object[] arguments)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L311) | private static |
| PresentationPlayChecks | [`void InvokeStatic(Type type, string name, params object[] arguments)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L316) | private static |
| PresentationPlayChecks | [`bool Near(Vector3 actual, Vector3 expected)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L321) | private static |
| PresentationPlayChecks | [`void Check(List<string> checks, List<string> failures, string name, bool passed)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L326) | private static |

## Assets/Editor/Art/BlockArtDependencies.cs

// 职责：收集和处理方块美术集成所需的资源依赖。 // 入口：Resolve() / Unpack() / EnsureFolder()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtDependencies | [`bool IsVendor(string path)`](../Assets/Editor/Art/BlockArtDependencies.cs#L32) | private static |
| BlockArtDependencies | [`void Resolve()`](../Assets/Editor/Art/BlockArtDependencies.cs#L33) | public static |
| BlockArtDependencies | [`string Hash(string path)`](../Assets/Editor/Art/BlockArtDependencies.cs#L155) | private static |
| BlockArtDependencies | [`void Unpack(GameObject root)`](../Assets/Editor/Art/BlockArtDependencies.cs#L166) | public static |
| BlockArtDependencies | [`void EnsureFolder(string path)`](../Assets/Editor/Art/BlockArtDependencies.cs#L177) | public static |
| BlockArtDependencies | [`void Remap(Object target, Dictionary<Object, Object> map)`](../Assets/Editor/Art/BlockArtDependencies.cs#L189) | private static |

## Assets/Editor/Art/BlockArtIntegrator.cs

// 职责：把工业美术装配到可编辑方块 Prefab，并保持挂点/碰撞关系。 // 入口：Integrate() / MatchThrusterFlamesToHover()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtIntegrator | [`void Integrate()`](../Assets/Editor/Art/BlockArtIntegrator.cs#L17) | public static |
| BlockArtIntegrator | [`void MatchThrusterFlamesToHover()`](../Assets/Editor/Art/BlockArtIntegrator.cs#L40) | public static |
| BlockArtIntegrator | [`void IntegratePrefab(string path)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L109) | private static |
| BlockArtIntegrator | [`Transform Add(string relative, Transform parent, string fallback = null)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L281) | private static |
| BlockArtIntegrator | [`void Fit(Transform model, Vector3 size, Vector3 center, bool uniform = true)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L302) | private static |
| BlockArtIntegrator | [`Transform Socket(Transform parent, string name, Vector3 position, Vector3 forward)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L344) | private static |
| BlockArtIntegrator | [`VfxEffect Effect(Transform socket, string name, float scale)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L355) | private static |
| BlockArtIntegrator | [`void BindThruster(GameObject root, Transform movingModel)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L366) | private static |
| BlockArtIntegrator | [`void ConfigureMain(GameObject root, Transform model)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L378) | private static |
| BlockArtIntegrator | [`void ConfigureHover(GameObject root, Transform model)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L404) | private static |
| BlockArtIntegrator | [`void ConfigureUniversal(GameObject root, Transform model)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L415) | private static |
| BlockArtIntegrator | [`void ConfigureTurret(GameObject root)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L435) | private static |
| BlockArtIntegrator | [`void ConfigureBot(GameObject root)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L446) | private static |
| BlockArtIntegrator | [`void ConfigureBay(GameObject root, Transform model)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L499) | private static |
| BlockArtIntegrator | [`void StatusLights(GameObject root, Transform model, bool generator)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L521) | private static |

## Assets/Editor/Art/BlockArtPlayProbe.cs

// 职责：在 Play Mode 检查方块美术、挂点和运行表现，输出验证结果。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtPlayProbe | [`void Run()`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L54) | public static |
| BlockArtPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L68) | private static |
| BlockArtPlayProbe | [`void Setup()`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L107) | private static |
| BlockArtPlayProbe | [`ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool kinematic)`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L184) | private static |
| BlockArtPlayProbe | [`GameObject Spawn(string name, Transform parent, Vector3 local)`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L203) | private static |
| BlockArtPlayProbe | [`void Supply(Transform parent)`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L210) | private static |
| BlockArtPlayProbe | [`void Tick()`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L217) | private static |
| BlockArtPlayProbe | [`void Check(string name, bool pass)`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L430) | private static |
| BlockArtPlayProbe | [`void Capture(string message, string stack, LogType type)`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L442) | private static |
| BlockArtPlayProbe | [`void Finish()`](../Assets/Editor/Art/BlockArtPlayProbe.cs#L450) | private static |

## Assets/Editor/Art/BlockArtPreview.cs

// 职责：生成方块美术预览场景或图像供检查。 // 入口：Render() / RenderBlocks()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtPreview | [`void Render( string path, string output, bool effects = false, bool front = false, Vector3? view = null, Action<GameObject> configure = null, bool transparent = false)`](../Assets/Editor/Art/BlockArtPreview.cs#L16) | public static |
| BlockArtPreview | [`void RenderBlocks(string folder, bool effects = false)`](../Assets/Editor/Art/BlockArtPreview.cs#L100) | public static |

## Assets/Editor/Art/BlockArtValidation.cs

// 职责：检查美术集成后的 Prefab 结构、引用与配置。 // 入口：Validate() / ValidateAssets()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtValidation | [`void Validate()`](../Assets/Editor/Art/BlockArtValidation.cs#L14) | public static |
| BlockArtValidation | [`void ValidateAssets(string[] roots)`](../Assets/Editor/Art/BlockArtValidation.cs#L16) | public static |

## Assets/Editor/Art/IndustrialArtGenerator.cs

// 职责：程序化生成工业方块模型、材质及组合资产。 // 入口：RebuildAll() / RebuildTurret()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IndustrialArtGenerator | [`void RebuildAll()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L48) | public static |
| IndustrialArtGenerator | [`void RebuildTurret()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L82) | public static |
| IndustrialArtGenerator | [`string[] GetBlockPrefabPaths()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L102) | private static |
| IndustrialArtGenerator | [`void EnsureFolders()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L107) | private static |
| IndustrialArtGenerator | [`void EnsureFolder(string parent, string child)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L116) | private static |
| IndustrialArtGenerator | [`void CreateSharedMeshes()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L125) | private static |
| IndustrialArtGenerator | [`Mesh SaveOrUpdateMesh(string path, Mesh generated)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L136) | private static |
| IndustrialArtGenerator | [`Mesh ClonePrimitiveMesh(PrimitiveType type)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L152) | private static |
| IndustrialArtGenerator | [`Mesh CreateRoundedCube(int segments, float radius)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L160) | private static |
| IndustrialArtGenerator | [`Vector3 FacePoint(int face, float u, float v)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L220) | private static |
| IndustrialArtGenerator | [`void AddFacingQuad( IReadOnlyList<Vector3> vertices, IReadOnlyList<Vector3> normals, ICollection<int> triangles, int a, int b, int c, int d)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L233) | private static |
| IndustrialArtGenerator | [`Mesh CreateTorus(int ringSegments, int tubeSegments, float radius, float tubeRadius)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L264) | private static |
| IndustrialArtGenerator | [`Mesh CreateCone(int segments)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L318) | private static |
| IndustrialArtGenerator | [`Mesh CreateWedge()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L353) | private static |
| IndustrialArtGenerator | [`void CreateSharedMaterials()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L416) | private static |
| IndustrialArtGenerator | [`Material CreateOrUpdateMaterial( string name, Color baseColor, float metallic, float smoothness, Color emission, bool transparent = false)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L456) | private static |
| IndustrialArtGenerator | [`void ConfigureRenderStyle()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L525) | private static |
| IndustrialArtGenerator | [`void ConfigureVolume(string path)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L537) | private static |
| IndustrialArtGenerator | [`T GetOrAdd(VolumeProfile profile)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L571) | private static |
| IndustrialArtGenerator | [`void Set(VolumeParameter<T> parameter, T value)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L577) | private static |
| IndustrialArtGenerator | [`void RebuildConnectorPrefab()`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L583) | private static |
| IndustrialArtGenerator | [`bool RebuildPrefab(string prefabPath)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L667) | private static |
| IndustrialArtGenerator | [`void RemovePlaceholderRenderers(Transform model)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L704) | private static |
| IndustrialArtGenerator | [`void BuildBlockVisual(string name, Block block, Transform model)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L723) | private static |
| IndustrialArtGenerator | [`bool ShouldUseLod(string name, Block block)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L772) | private static |
| IndustrialArtGenerator | [`void ApplyCategoryPalette(string name, Transform visualRoot)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L777) | private static |
| IndustrialArtGenerator | [`List<Renderer> BuildDetailedModel( string name, Block block, Transform parent, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L797) | private static |
| IndustrialArtGenerator | [`List<Renderer> BuildSimpleModel(string name, Block block, Transform parent)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L856) | private static |
| IndustrialArtGenerator | [`void BuildStructural( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L873) | private static |
| IndustrialArtGenerator | [`void BuildDoor( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L892) | private static |
| IndustrialArtGenerator | [`void BuildStairs(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L949) | private static |
| IndustrialArtGenerator | [`void BuildRack(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L992) | private static |
| IndustrialArtGenerator | [`void BuildCockpit( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1042) | private static |
| IndustrialArtGenerator | [`void BuildGenerator( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1145) | private static |
| IndustrialArtGenerator | [`void BuildRelay( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1251) | private static |
| IndustrialArtGenerator | [`void BuildFlightController( Transform parent, ICollection<Renderer> renderers, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1314) | private static |
| IndustrialArtGenerator | [`void BuildThruster( Transform parent, Block block, string name, ICollection<Renderer> renderers, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1362) | private static |
| IndustrialArtGenerator | [`void BuildTurret(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1456) | private static |
| IndustrialArtGenerator | [`void ConfigureTurretRig(Transform model, Transform detailedRoot)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1596) | private static |
| IndustrialArtGenerator | [`void BuildRepairBay(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1614) | private static |
| IndustrialArtGenerator | [`void RebuildBot(GameObject root)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1675) | private static |
| IndustrialArtGenerator | [`Transform CreateGroup(Transform parent, string name)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1754) | private static |
| IndustrialArtGenerator | [`MeshRenderer AddPart( Transform parent, string name, Mesh mesh, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material, bool castShadows = true)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1762) | private static |
| IndustrialArtGenerator | [`void SetLayerRecursively(GameObject root, int layer)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1788) | private static |
| IndustrialArtGenerator | [`void CreatePreviewScene(IReadOnlyList<string> prefabPaths)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1797) | private static |
| IndustrialArtGenerator | [`void AddPartToScene( string name, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation, Material material)`](../Assets/Editor/Art/IndustrialArtGenerator.cs#L1927) | private static |

## Assets/Editor/Art/SpaceKitCurator.cs

// 职责：筛选科幻素材并创建可编辑备用 Prefab、材质及分类目录。 // 入口：CreateLibrary() / RepairLegacyImportArtifacts() / ValidateLibrary()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SpaceKitCurator | [`bool IsSource(string path)`](../Assets/Editor/Art/SpaceKitCurator.cs#L77) | private static |
| SpaceKitCurator | [`void EnsureFolder(string path)`](../Assets/Editor/Art/SpaceKitCurator.cs#L78) | private static |
| SpaceKitCurator | [`string Destination(string path, Dictionary<string, string> selected)`](../Assets/Editor/Art/SpaceKitCurator.cs#L89) | private static |
| SpaceKitCurator | [`string Hash(string path)`](../Assets/Editor/Art/SpaceKitCurator.cs#L104) | private static |
| SpaceKitCurator | [`void CreateLibrary()`](../Assets/Editor/Art/SpaceKitCurator.cs#L115) | public static |
| SpaceKitCurator | [`void Remap(Object target, Dictionary<Object, Object> objects)`](../Assets/Editor/Art/SpaceKitCurator.cs#L254) | private static |
| SpaceKitCurator | [`void CleanLegacyArtifacts(GameObject instance)`](../Assets/Editor/Art/SpaceKitCurator.cs#L281) | private static |
| SpaceKitCurator | [`void RepairLegacyImportArtifacts()`](../Assets/Editor/Art/SpaceKitCurator.cs#L295) | public static |
| SpaceKitCurator | [`void ConvertMaterial(Material material)`](../Assets/Editor/Art/SpaceKitCurator.cs#L315) | private static |
| SpaceKitCurator | [`void ValidateLibrary()`](../Assets/Editor/Art/SpaceKitCurator.cs#L410) | public static |
| SpaceKitCurator | [`void RenderAllPreviews()`](../Assets/Editor/Art/SpaceKitCurator.cs#L546) | public static |
| SpaceKitCurator | [`void RenderPreviews(int start, int count)`](../Assets/Editor/Art/SpaceKitCurator.cs#L548) | public static |

## Assets/Editor/Bots/BotNavigationPlayProbe.cs

// 职责：隔离 Play 场景执行机器人墙面、U 形围挡、惯性制动和移动 Home 回归验证。 // 入口：Run()；结果保存在 Temp/BotNavigation/PlayValidation.json；不修改作者场景或资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationPlayProbe | [`void Run()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L30) | public static |
| BotNavigationPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L49) | private static |
| BotNavigationPlayProbe | [`void Capture(string condition, string stack, LogType type)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L97) | private static |
| BotNavigationPlayProbe | [`void Tick()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L105) | private static |
| BotNavigationPlayProbe | [`void TickCase()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L118) | private static |
| BotNavigationPlayProbe | [`void StartCase()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L169) | private static |
| BotNavigationPlayProbe | [`void Wall(Vector3 position, Vector3 scale)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L215) | private static |
| BotNavigationPlayProbe | [`void Finish()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L224) | private static |
| BotNavigationPlayProbe | [`void Restore()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L231) | private static |
| BotNavigationPlayProbe | [`void WriteReport()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L240) | private static |

## Assets/Editor/Build/AutoBuildTool.cs

// 职责：提供 Windows 玩家构建入口并设置输出参数。 // 入口：BuildWindows()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| AutoBuildTool | [`void BuildWindows()`](../Assets/Editor/Build/AutoBuildTool.cs#L12) | public static |
| AutoBuildTool | [`string IncrementVersion(string version)`](../Assets/Editor/Build/AutoBuildTool.cs#L56) | private static |
| AutoBuildTool | [`string[] GetEnabledScenes()`](../Assets/Editor/Build/AutoBuildTool.cs#L76) | private static |

## Assets/Editor/Building/BlockCsvAuthoring.cs

// 职责：初始化 CSV 与 Prefab 绑定，CSV 导入后自动备份/同步真实资产；校验失败不写入。 // 入口：CreateCsv() / Bake() / QueueBake()；只处理 Resources/Blocks，不保存当前场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvAuthoring | [`void OpenCsv()`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L34) | public static |
| BlockCsvAuthoring | [`void CreateCsv()`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L45) | public static |
| BlockCsvAuthoring | [`void Bake()`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L128) | public static |
| BlockCsvAuthoring | [`void QueueBake()`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L239) | public static |
| BlockCsvAuthoring | [`void ValidatePowerDependencies(GameObject root)`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L246) | public static |
| BlockCsvAuthoring | [`void RequestValidation(BlockCsvBinding binding)`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L279) | private static |
| BlockCsvAuthoring | [`void Update()`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L287) | private static |
| BlockCsvAuthoring | [`void OnPlayModeChanged(PlayModeStateChange state)`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L321) | private static |
| BlockCsvAuthoring | [`string ReadValue(SerializedProperty property)`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L329) | private static |
| BlockCsvAuthoring | [`string Snapshot(GameObject root)`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L348) | private static |
| BlockCsvAuthoring | [`string Hash(byte[] bytes)`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L355) | private static |
| BlockCsvAuthoring | [`void SaveBackup(string backup, Dictionary<string, byte[]> originals, Dictionary<string, string> hashes, string csv)`](../Assets/Editor/Building/BlockCsvAuthoring.cs#L361) | private static |

## Assets/Editor/Building/BlockCsvChecks.cs

// 职责：隔离验证实际 CSV→Prefab 同步、幂等/错误拒绝及真实 Awake/OnEnable 顺序。 // 入口：RunEditMode() / RunPlayMode()；Edit Mode 最终恢复 CSV 与 Prefab 原始字节，不保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvChecks | [`string RunEditMode()`](../Assets/Editor/Building/BlockCsvChecks.cs#L21) | public static |
| BlockCsvChecks | [`string RunPlayMode()`](../Assets/Editor/Building/BlockCsvChecks.cs#L207) | public static |
| BlockCsvChecks | [`GameObject Load(string name)`](../Assets/Editor/Building/BlockCsvChecks.cs#L257) | private static |
| BlockCsvChecks | [`void Set(List<List<string>> records, string name, string field, string value)`](../Assets/Editor/Building/BlockCsvChecks.cs#L262) | private static |
| BlockCsvChecks | [`void Write(List<List<string>> records)`](../Assets/Editor/Building/BlockCsvChecks.cs#L268) | private static |
| BlockCsvChecks | [`void Check(Report report, bool condition, string description)`](../Assets/Editor/Building/BlockCsvChecks.cs#L274) | private static |
| BlockCsvChecks | [`string SaveReport(Report report, string name)`](../Assets/Editor/Building/BlockCsvChecks.cs#L284) | private static |

## Assets/Editor/Building/BlockCsvPostprocessor.cs

// 职责：CSV 文件被 Unity 导入后请求延迟同步，避免在 AssetPostprocessor 内递归写资产。 // 入口：OnPostprocessAllAssets()；Prefab 自身重导入不会触发 CSV 同步循环。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvPostprocessor | [`void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)`](../Assets/Editor/Building/BlockCsvPostprocessor.cs#L8) | private static |

## Assets/Editor/Building/BlockCsvRegressionChecks.cs

// 职责：用真实维修仓跨物理帧验证 CSV、供电、离仓、维修和断电恢复，隔离创建并清理测试对象。 // 入口：Begin()（Play Mode）；结果写入 Temp/BlockCsvChecks/regression-play.json，不修改制作资产或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvRegressionChecks | [`string Begin()`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L31) | public static |
| BlockCsvRegressionChecks | [`void Tick()`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L76) | private static |
| BlockCsvRegressionChecks | [`void CheckRuntimeState()`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L141) | private static |
| BlockCsvRegressionChecks | [`void Check(bool condition, string description)`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L167) | private static |
| BlockCsvRegressionChecks | [`void Finish()`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L177) | private static |
| BlockCsvRegressionChecks | [`void WriteReport()`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L195) | private static |

## Assets/Editor/Building/BuildInteractionSetup.cs

// 职责：配置主场景建造交互、范围与连接点提示。 // 入口：Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildInteractionSetup | [`void Apply()`](../Assets/Editor/Building/BuildInteractionSetup.cs#L15) | public static |

## Assets/Editor/Building/BuildPaletteBaker.cs

// 职责：创建并保存建造分类目录、条目和图标引用。 // 入口：Bake() / RefreshHoveredBlockInfo() / ConfigureHintMaterial()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPaletteBaker | [`void Bake()`](../Assets/Editor/Building/BuildPaletteBaker.cs#L27) | public static |
| BuildPaletteBaker | [`void RefreshHoveredBlockInfo()`](../Assets/Editor/Building/BuildPaletteBaker.cs#L226) | public static |
| BuildPaletteBaker | [`void ConfigureThumbnail(GameObject root)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L274) | private static |
| BuildPaletteBaker | [`int Category(string name)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L291) | private static |
| BuildPaletteBaker | [`RectTransform Child(Transform parent, string name)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L316) | private static |
| BuildPaletteBaker | [`void Rect(RectTransform t, Vector2 min, Vector2 max, Vector2 low, Vector2 high)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L329) | private static |
| BuildPaletteBaker | [`Text Label(Transform parent, string name, Font font, int size)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L337) | private static |
| BuildPaletteBaker | [`void Set(Object target, string field, Object value)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L350) | private static |
| BuildPaletteBaker | [`void ImportSprite(string path)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L357) | private static |
| BuildPaletteBaker | [`void CreateBorder()`](../Assets/Editor/Building/BuildPaletteBaker.cs#L374) | private static |
| BuildPaletteBaker | [`void CreateHintAssets(out Mesh mesh, out Material material)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L392) | private static |
| BuildPaletteBaker | [`void ConfigureHintMaterial(Material material)`](../Assets/Editor/Building/BuildPaletteBaker.cs#L449) | public static |

## Assets/Editor/Building/BuildPalettePlayProbe.cs

// 职责：在 Play Mode 检查分类目录、选择、放置预览与连接点提示。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPalettePlayProbe | [`void Run()`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L35) | public static |
| BuildPalettePlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L58) | private static |
| BuildPalettePlayProbe | [`void Capture(string message, string stack, LogType type)`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L94) | private static |
| BuildPalettePlayProbe | [`void Tick()`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L102) | private static |
| BuildPalettePlayProbe | [`void Finish()`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L132) | private static |
| BuildPalettePlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L160) | private static |
| BuildPalettePlayProbe | [`void Invoke(object target, string name)`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L170) | private static |
| BuildPalettePlayProbe | [`void MovePointer(Vector2 position)`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L175) | private static |
| BuildPalettePlayProbe | [`Block RangeBlock(Vector3 position)`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L186) | private static |
| BuildPalettePlayProbe | [`float HintAlpha(ConnectorPlacementHints hints)`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L199) | private static |
| BuildPalettePlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Building/BuildPalettePlayProbe.cs#L211) | private static |

## Assets/Editor/Combat/CombatHudPlayProbe.cs

// 职责：在 Play Mode 检查敌方名牌与战斗 HUD 反馈。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatHudPlayProbe | [`void Run()`](../Assets/Editor/Combat/CombatHudPlayProbe.cs#L26) | public static |
| CombatHudPlayProbe | [`void OnPlayModeChanged(PlayModeStateChange state)`](../Assets/Editor/Combat/CombatHudPlayProbe.cs#L44) | private static |
| CombatHudPlayProbe | [`void Tick()`](../Assets/Editor/Combat/CombatHudPlayProbe.cs#L80) | private static |
| CombatHudPlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Combat/CombatHudPlayProbe.cs#L110) | private static |
| CombatHudPlayProbe | [`ControlUnit CreatePlayer(Vector3 position)`](../Assets/Editor/Combat/CombatHudPlayProbe.cs#L280) | private static |
| CombatHudPlayProbe | [`ControlUnit CreateEnemy(string name, Vector3 position)`](../Assets/Editor/Combat/CombatHudPlayProbe.cs#L320) | private static |
| CombatHudPlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Combat/CombatHudPlayProbe.cs#L362) | private static |
| CombatHudPlayProbe | [`void Finish()`](../Assets/Editor/Combat/CombatHudPlayProbe.cs#L372) | private static |

## Assets/Editor/Combat/CombatHudSetup.cs

// 职责：配置主场景战斗 HUD 与界面反馈组件。 // 入口：UpgradeDualHealthHud() / Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatHudSetup | [`void UpgradeDualHealthHud()`](../Assets/Editor/Combat/CombatHudSetup.cs#L18) | public static |
| CombatHudSetup | [`void Apply()`](../Assets/Editor/Combat/CombatHudSetup.cs#L154) | public static |
| CombatHudSetup | [`RectTransform Child(Transform parent, string name)`](../Assets/Editor/Combat/CombatHudSetup.cs#L290) | private static |
| CombatHudSetup | [`void Stretch(RectTransform rect)`](../Assets/Editor/Combat/CombatHudSetup.cs#L303) | private static |
| CombatHudSetup | [`void Position(RectTransform rect, float left, float bottom, float right, float top)`](../Assets/Editor/Combat/CombatHudSetup.cs#L311) | private static |
| CombatHudSetup | [`void BottomBox(RectTransform rect, float left, float bottom, float right, float top)`](../Assets/Editor/Combat/CombatHudSetup.cs#L318) | private static |
| CombatHudSetup | [`Image Image(RectTransform rect, Color color)`](../Assets/Editor/Combat/CombatHudSetup.cs#L327) | private static |
| CombatHudSetup | [`Text Text( Transform parent, string name, Font font, int size, Color color, TextAnchor alignment)`](../Assets/Editor/Combat/CombatHudSetup.cs#L340) | private static |
| CombatHudSetup | [`void Set(UnityEngine.Object target, string name, UnityEngine.Object value)`](../Assets/Editor/Combat/CombatHudSetup.cs#L364) | private static |

## Assets/Editor/Combat/HealthHudPreview.cs

// 职责：创建耐久/血条布局的可编辑预览。 // 入口：Render()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HealthHudPreview | [`string Render()`](../Assets/Editor/Combat/HealthHudPreview.cs#L17) | public static |
| HealthHudPreview | [`void RenderOne(Transform source, Vector2Int size)`](../Assets/Editor/Combat/HealthHudPreview.cs#L44) | private static |

## Assets/Editor/Combat/TurretAssetBaker.cs

// 职责：从指定 SpaceKit 源制作单管/双管炮塔、真实炮弹及独立建造目录资产。 // 入口：Bake()；只保存拥有的 Prefab/材质/图标，不保存当前场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretAssetBaker | [`void RefreshTurretIcons()`](../Assets/Editor/Combat/TurretAssetBaker.cs#L16) | public static |
| TurretAssetBaker | [`void ApplyExistingButtonStyle(GameObject entry)`](../Assets/Editor/Combat/TurretAssetBaker.cs#L48) | private static |
| TurretAssetBaker | [`void ConfigureRecoil()`](../Assets/Editor/Combat/TurretAssetBaker.cs#L62) | public static |
| TurretAssetBaker | [`void Bake()`](../Assets/Editor/Combat/TurretAssetBaker.cs#L105) | public static |
| TurretAssetBaker | [`TurretProjectile BakeProjectile()`](../Assets/Editor/Combat/TurretAssetBaker.cs#L126) | private static |
| TurretAssetBaker | [`void ConfigureLaser()`](../Assets/Editor/Combat/TurretAssetBaker.cs#L170) | private static |
| TurretAssetBaker | [`void BakeTurret(string name, string variant, float demand, TurretProjectile projectile)`](../Assets/Editor/Combat/TurretAssetBaker.cs#L193) | private static |
| TurretAssetBaker | [`Sprite RenderIcon(string name)`](../Assets/Editor/Combat/TurretAssetBaker.cs#L287) | private static |
| TurretAssetBaker | [`void BakeEntry(string name)`](../Assets/Editor/Combat/TurretAssetBaker.cs#L300) | private static |

## Assets/Editor/Combat/TurretFlightPlayProbe.cs

// 职责：验证真实引擎帧中的无目标空闲、自动申请供电、双管发射及有限飞行时间。 // 入口：Run()；在已进入的Play Mode隔离执行，输出JSON与飞行截图，不保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretFlightPlayProbe | [`void Run()`](../Assets/Editor/Combat/TurretFlightPlayProbe.cs#L38) | public static |
| TurretFlightPlayProbe | [`ControlUnit Unit(string name, Vector3 position, UnitFaction faction)`](../Assets/Editor/Combat/TurretFlightPlayProbe.cs#L89) | private static |
| TurretFlightPlayProbe | [`void Tick()`](../Assets/Editor/Combat/TurretFlightPlayProbe.cs#L109) | private static |
| TurretFlightPlayProbe | [`void Check(string label, bool passed)`](../Assets/Editor/Combat/TurretFlightPlayProbe.cs#L187) | private static |
| TurretFlightPlayProbe | [`void Capture()`](../Assets/Editor/Combat/TurretFlightPlayProbe.cs#L192) | private static |
| TurretFlightPlayProbe | [`void Finish()`](../Assets/Editor/Combat/TurretFlightPlayProbe.cs#L216) | private static |

## Assets/Editor/Combat/TurretRuntimeChecks.cs

// 职责：在当前 Play Mode 隔离检查真实炮塔资产、瞬时激光、炮管数量及炮弹物理扫掠。 // 入口：RunInCurrentPlayMode()；不保存场景、不切换模式，清理本次创建的检查对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretRuntimeChecks | [`string RunInCurrentPlayMode()`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L23) | public static |
| TurretRuntimeChecks | [`void PrepareWeapon(Turret turret, ControlUnit owner)`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L230) | private static |
| TurretRuntimeChecks | [`void CallFire(Turret turret, Vector3 origin, Vector3 direction)`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L238) | private static |
| TurretRuntimeChecks | [`ControlUnit Unit(Transform parent, string name, UnitFaction faction, Vector3 localPosition)`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L243) | private static |
| TurretRuntimeChecks | [`void Check(Report report, bool passed, string name)`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L260) | private static |

## Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs

// 职责：在真实 Unity 组件上交替测量冻结基线与当前供电实现，并验证行为等价。 // 入口：Run()；Play Mode 同步执行，生成原始样本，不将子系统耗时转换成游戏 FPS。 // 生命周期：使用两套相同布局防止连接列表污染缓存，finally 销毁所有临时对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerPerformanceChecks | [`void StartFrames()`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L41) | public static |
| PowerPerformanceChecks | [`void BeginWindow()`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L56) | private static |
| PowerPerformanceChecks | [`void Unregister(string field, Type type, List<T> participants)`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L69) | private static |
| PowerPerformanceChecks | [`void FrameTick()`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L78) | private static |
| PowerPerformanceChecks | [`void FinishFrames()`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L123) | private static |
| Rig | [`T Child(Vector3 position)`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L197) | private |
| Rig | [`void Rebuild(bool useBaseline)`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L206) | public |
| Rig | [`void Change(int step, string scenario)`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L218) | public |
| Rig | [`void Dispose()`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L234) | public |
| PowerPerformanceChecks | [`string Run()`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L241) | public static |
| PowerPerformanceChecks | [`Samples Measure(int count, int loadCount, string scenario)`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L282) | private static |
| PowerPerformanceChecks | [`double Percentile(double[] values, double percentile)`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L341) | private static |
| PowerPerformanceChecks | [`void CheckEquivalence(Report report)`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L348) | private static |
| PowerPerformanceChecks | [`void Compare(Rig before, Rig after, int step)`](../Assets/Editor/Diagnostics/Benchmarks/PowerPerformanceChecks.cs#L435) | private static |

## Assets/Editor/Diagnostics/Benchmarks/PresentationPerformanceChecks.cs

// 职责：隔离验证镜头墙体收回、自身过滤和图标空闲/淡出/恢复分支。 // 入口：Run()；Play Mode 调用真实方法，不替代视觉手感验收。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PresentationPerformanceChecks | [`string[] Run()`](../Assets/Editor/Diagnostics/Benchmarks/PresentationPerformanceChecks.cs#L13) | public static |
| PresentationPerformanceChecks | [`void Set(StatusIcon icon, string name, object value)`](../Assets/Editor/Diagnostics/Benchmarks/PresentationPerformanceChecks.cs#L82) | private static |
| PresentationPerformanceChecks | [`void Check(bool condition, string name, List<string> checks)`](../Assets/Editor/Diagnostics/Benchmarks/PresentationPerformanceChecks.cs#L87) | private static |

## Assets/Editor/Diagnostics/Benchmarks/ProjectilePoolChecks.cs

// 职责：验证炮弹复用重置、自身碰撞过滤恢复与模拟退出清理，并测量创建/复用周期。 // 入口：Run()；Play Mode 隔离对象，生命周期微基准不代表完整交火帧率。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProjectilePoolChecks | [`string Run()`](../Assets/Editor/Diagnostics/Benchmarks/ProjectilePoolChecks.cs#L28) | public static |
| ProjectilePoolChecks | [`void Launch(TurretProjectile shot, ControlUnit owner)`](../Assets/Editor/Diagnostics/Benchmarks/ProjectilePoolChecks.cs#L128) | private static |
| ProjectilePoolChecks | [`void Require(bool condition, string message)`](../Assets/Editor/Diagnostics/Benchmarks/ProjectilePoolChecks.cs#L141) | private static |
| ProjectilePoolChecks | [`T Median(T[] values)`](../Assets/Editor/Diagnostics/Benchmarks/ProjectilePoolChecks.cs#L149) | private static |

## Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs

// 职责：在 Play Mode 检查沙盒物理、控制和损伤相关场景。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PhysicsSandboxProbe | [`string Run()`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L13) | public static |
| PhysicsSandboxProbe | [`float Collide(Scene scene, float mass, float speed, List<string> checks, bool checkContacts)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L144) | private static |
| PhysicsSandboxProbe | [`Rigidbody Body(Scene scene, string name, float mass, Vector3 position)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L200) | private static |
| PhysicsSandboxProbe | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L213) | private static |
| PhysicsSandboxProbe | [`string Save(List<string> checks, string error)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L223) | private static |

## Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs

// 职责：解析 Profiler 捕获数据并输出性能分析结果。 // 入口：AnalyzeLatest() / Analyze() / Add()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProfilerCaptureAnalysis | [`void AnalyzeLatest()`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L18) | public static |
| ProfilerCaptureAnalysis | [`void Analyze(string capturePath, string reportPath)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L26) | public static |
| ProfilerCaptureAnalysis | [`string GetLatestCapturePath()`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L120) | private static |
| ProfilerCaptureAnalysis | [`void AddRawMarkers( RawFrameDataView raw, Dictionary<string, MarkerAggregate> aggregate, Dictionary<string, MarkerAggregate> gcMarkers, Dictionary<string, MarkerAggregate> scriptMarkers, FrameSummary summary)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L131) | private static |
| ProfilerCaptureAnalysis | [`MarkerAggregate GetOrCreate(Dictionary<string, MarkerAggregate> map, string name)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L163) | private static |
| ProfilerCaptureAnalysis | [`void WriteFrameStats(StringBuilder report, List<FrameSummary> frames)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L177) | private static |
| ProfilerCaptureAnalysis | [`void WriteCounters(StringBuilder report)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L195) | private static |
| ProfilerCaptureAnalysis | [`void WriteMarkers(StringBuilder report, string title, IEnumerable<MarkerAggregate> markers, int count)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L223) | private static |
| ProfilerCaptureAnalysis | [`void WriteWorstFrames(StringBuilder report, List<FrameSummary> frames, int count)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L233) | private static |
| ProfilerCaptureAnalysis | [`void WriteHierarchyDrilldowns(StringBuilder report, IEnumerable<int> frames)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L243) | private static |
| ProfilerCaptureAnalysis | [`void WriteHierarchyItem(StringBuilder report, HierarchyFrameDataView view, int id, int depth, int maxDepth)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L267) | private static |
| ProfilerCaptureAnalysis | [`float Percentile(float[] sorted, int pct)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L287) | private static |
| ProfilerCaptureAnalysis | [`void WriteReport(string reportPath, StringBuilder report)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L298) | private static |
| ProfilerCaptureAnalysis | [`void Finish(string message)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L309) | private static |
| ProfilerCaptureAnalysis | [`string F(float value)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L318) | private static |
| ProfilerCaptureAnalysis | [`string F(double value)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L323) | private static |
| ProfilerCaptureAnalysis | [`string Pad(string value, int width)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L328) | private static |
| MarkerAggregate | [`void Add(float ms, int frame)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L342) | public |

## Assets/Editor/Flight/ThrusterHudSetup.cs

// 职责：配置推进器参数 HUD 与交互引用。 // 入口：Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ThrusterHudSetup | [`void Apply()`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L19) | public static |
| ThrusterHudSetup | [`void SetupLamp()`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L225) | private static |
| ThrusterHudSetup | [`RectTransform Child(Transform parent, string name)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L290) | private static |
| ThrusterHudSetup | [`void Stretch(RectTransform rect)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L303) | private static |
| ThrusterHudSetup | [`void Box(RectTransform rect, Vector2 topLeft, Vector2 bottomRight)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L311) | private static |
| ThrusterHudSetup | [`Image Paint(RectTransform rect, Color color, bool raycast)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L319) | private static |
| ThrusterHudSetup | [`Text Label( Transform parent, string name, Font font, int size, Color color, TextAnchor alignment)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L332) | private static |
| ThrusterHudSetup | [`void Set(UnityEngine.Object target, string name, UnityEngine.Object value)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L356) | private static |
| ThrusterHudSetup | [`void SetArray(UnityEngine.Object target, string name, T[] values)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L363) | private static |

## Assets/Editor/Power/ActivePowerPlayChecks.cs

// 职责：在临时运行对象上验证按工作需求分配、停机释放、断电恢复和真实推进/入库启动。 // 入口：Run()；需要已连接 Editor 的 Play Mode，由武器回归 probe 调用并汇总结果。 // 生命周期：仅创建临时对象，退出销毁全部对象并恢复原模拟开关，不保存场景或资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ActivePowerPlayChecks | [`string[] Run()`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L12) | public static |
| ActivePowerPlayChecks | [`void CheckNetwork(Transform parent, List<string> checks)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L40) | private static |
| ActivePowerPlayChecks | [`void CheckCurrencyAttraction(Transform parent, List<string> checks)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L140) | private static |
| ActivePowerPlayChecks | [`void CheckThruster(Transform parent, List<string> checks)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L179) | private static |
| ActivePowerPlayChecks | [`void CheckCargo(Transform parent, List<string> checks)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L217) | private static |
| ActivePowerPlayChecks | [`ControlUnit CreateOwner(Transform parent, string name)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L253) | private static |
| ActivePowerPlayChecks | [`void CheckBots(Transform parent, List<string> checks)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L264) | private static |
| ActivePowerPlayChecks | [`void TickBot(Bot bot, string method)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L317) | private static |
| ActivePowerPlayChecks | [`T Child(Transform parent, string name)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L322) | private static |
| ActivePowerPlayChecks | [`void Tick(MainThruster thruster)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L330) | private static |
| ActivePowerPlayChecks | [`bool Near(float first, float second)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L335) | private static |
| ActivePowerPlayChecks | [`void Check(List<string> checks, string description, bool passed)`](../Assets/Editor/Power/ActivePowerPlayChecks.cs#L340) | private static |

## Assets/Editor/Presentation/BlockStatusSetup.cs

// 职责：配置方块状态灯/图标的可编辑资源。 // 入口：Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockStatusSetup | [`void Apply()`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L23) | public static |
| BlockStatusSetup | [`int ConfigureBlockPrefabs()`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L32) | private static |
| BlockStatusSetup | [`List<Status> BuildStatusList()`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L68) | private static |
| BlockStatusSetup | [`void ConfigureMainScene()`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L86) | private static |
| BlockStatusSetup | [`void ConfigureBlueprintPanel(BlueprintUIPanel panel)`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L121) | private static |
| BlockStatusSetup | [`Text GetOrCreateText(Transform parent, string objectName, Text template, string initialText)`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L132) | private static |
| BlockStatusSetup | [`void ConfigureDebugPanel(MainUIButtons mainButtons)`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L149) | private static |
| BlockStatusSetup | [`Button GetOrCreateButton(Transform panel, Transform rowTemplate, string rowName, string label)`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L189) | private static |
| BlockStatusSetup | [`T FindComponentInScene(Scene scene)`](../Assets/Editor/Presentation/BlockStatusSetup.cs#L213) | private static |

## Assets/Editor/Presentation/BlockVfxBaker.cs

// 职责：生成并保存方块效果所需的可编辑 VFX 资产。 // 入口：GraphName() / Continuous() / Load()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockVfxBaker | [`string GraphName(string name)`](../Assets/Editor/Presentation/BlockVfxBaker.cs#L31) | public static |
| BlockVfxBaker | [`bool Continuous(string name)`](../Assets/Editor/Presentation/BlockVfxBaker.cs#L62) | public static |
| BlockVfxBaker | [`GameObject Load(string name)`](../Assets/Editor/Presentation/BlockVfxBaker.cs#L63) | public static |
| BlockVfxBaker | [`VfxEffect Populate(GameObject root, string name, bool automatic = false)`](../Assets/Editor/Presentation/BlockVfxBaker.cs#L64) | public static |
| BlockVfxBaker | [`void BakeMissing()`](../Assets/Editor/Presentation/BlockVfxBaker.cs#L109) | public static |
| BlockVfxBaker | [`void SetObjects(Object target, string field, Object[] values)`](../Assets/Editor/Presentation/BlockVfxBaker.cs#L141) | public static |
| BlockVfxBaker | [`void SetObject(Object target, string field, Object value)`](../Assets/Editor/Presentation/BlockVfxBaker.cs#L156) | public static |

## Assets/Editor/Presentation/InterfaceBaker.cs

// 职责：生成并保存主界面视觉与物理遥测 HUD 配置。 // 入口：Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| InterfaceBaker | [`void Apply()`](../Assets/Editor/Presentation/InterfaceBaker.cs#L19) | public static |
| InterfaceBaker | [`void Style(GameObject root)`](../Assets/Editor/Presentation/InterfaceBaker.cs#L197) | private static |
| InterfaceBaker | [`void Primary(Button button, string label)`](../Assets/Editor/Presentation/InterfaceBaker.cs#L245) | private static |
| InterfaceBaker | [`void Label(Button button, string label)`](../Assets/Editor/Presentation/InterfaceBaker.cs#L268) | private static |
| InterfaceBaker | [`RectTransform Child(Transform parent, string name)`](../Assets/Editor/Presentation/InterfaceBaker.cs#L282) | private static |
| InterfaceBaker | [`void AddRule(Transform parent)`](../Assets/Editor/Presentation/InterfaceBaker.cs#L295) | private static |
| InterfaceBaker | [`void BakeTelemetry(MainUIPanels panels)`](../Assets/Editor/Presentation/InterfaceBaker.cs#L310) | private static |
| InterfaceBaker | [`Text TextAt( Transform parent, string name, string value, float x0, float y0, float x1, float y1, int size)`](../Assets/Editor/Presentation/InterfaceBaker.cs#L363) | private static |

## Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs

// 职责：建立原生 VFX Graph 资源库并配置效果参数。 // 入口：Destination() / GraphPath() / RebuildPositionSafeEffects()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NativeVfxLibraryBuilder | [`string Destination(string source)`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L30) | public static |
| NativeVfxLibraryBuilder | [`string GraphPath(string name)`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L36) | public static |
| NativeVfxLibraryBuilder | [`void RebuildPositionSafeEffects()`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L39) | public static |
| NativeVfxLibraryBuilder | [`void SetSimulationSpace(object graph, string space)`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L147) | private static |
| NativeVfxLibraryBuilder | [`void PrepareSources()`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L156) | public static |
| NativeVfxLibraryBuilder | [`void ImportSources()`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L176) | public static |
| NativeVfxLibraryBuilder | [`void CreateUtilityGraphs()`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L275) | public static |
| NativeVfxLibraryBuilder | [`void BuildUtility(string name, bool loop, bool world, bool beam)`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L285) | private static |
| NativeVfxLibraryBuilder | [`string Hash(string path)`](../Assets/Editor/Presentation/NativeVfxLibraryBuilder.cs#L381) | private static |

## Assets/Editor/Presentation/NativeVfxMigration.cs

// 职责：迁移旧特效引用到原生 VFX Graph 并检查依赖。 // 入口：Run() / Convert() / Validate()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NativeVfxMigration | [`void Run()`](../Assets/Editor/Presentation/NativeVfxMigration.cs#L16) | public static |
| NativeVfxMigration | [`void Convert(GameObject root)`](../Assets/Editor/Presentation/NativeVfxMigration.cs#L48) | public static |
| NativeVfxMigration | [`void RemoveParticles(GameObject root)`](../Assets/Editor/Presentation/NativeVfxMigration.cs#L123) | private static |
| NativeVfxMigration | [`string Validate()`](../Assets/Editor/Presentation/NativeVfxMigration.cs#L136) | public static |

## Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs

// 职责：在 Play Mode 检查原生 VFX 播放、参数和停止生命周期。 // 入口：RunSuite() / Run() / RunPositionTest()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NativeVfxPlaybackProbe | [`void NextQueued()`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L47) | private static |
| NativeVfxPlaybackProbe | [`void RunSuite()`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L65) | public static |
| NativeVfxPlaybackProbe | [`void Run(string prefab, string output)`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L74) | public static |
| NativeVfxPlaybackProbe | [`void RunPositionTest(string prefab, string output)`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L81) | public static |
| NativeVfxPlaybackProbe | [`void RunBlockTest(string prefab, string output)`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L88) | public static |
| NativeVfxPlaybackProbe | [`void StartCapture(string prefab, string output)`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L95) | private static |
| NativeVfxPlaybackProbe | [`void State(PlayModeStateChange state)`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L110) | private static |
| NativeVfxPlaybackProbe | [`void Tick()`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L225) | private static |
| NativeVfxPlaybackProbe | [`void Finish()`](../Assets/Editor/Presentation/NativeVfxPlaybackProbe.cs#L315) | private static |

## Assets/Editor/Presentation/SceneCameraPreviewSync.cs

// 职责：同步 Scene 视图与游戏镜头预览，供编辑器检查。 // 入口：Toggle() / SyncNow()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SceneCameraPreviewSync | [`void Toggle()`](../Assets/Editor/Presentation/SceneCameraPreviewSync.cs#L21) | public static |
| SceneCameraPreviewSync | [`bool ValidateToggle()`](../Assets/Editor/Presentation/SceneCameraPreviewSync.cs#L33) | private static |
| SceneCameraPreviewSync | [`void Update()`](../Assets/Editor/Presentation/SceneCameraPreviewSync.cs#L40) | private static |
| SceneCameraPreviewSync | [`bool SyncNow()`](../Assets/Editor/Presentation/SceneCameraPreviewSync.cs#L48) | public static |

## Assets/Editor/Presentation/VfxGraphAuthoring.cs

// 职责：构建与配置原生 VFX Graph 节点和参数。 // 入口：Type() / Get() / Set()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxGraphAuthoring | [`Type Type(string name)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L17) | public static |
| VfxGraphAuthoring | [`PropertyInfo Property(object target, string name)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L18) | private static |
| VfxGraphAuthoring | [`object Get(object target, string name)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L32) | public static |
| VfxGraphAuthoring | [`void Set(object target, string name, object value)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L33) | public static |
| VfxGraphAuthoring | [`object Call(object target, string method, params object[] args)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L54) | public static |
| VfxGraphAuthoring | [`object[] Items(object target, string property)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L62) | public static |
| VfxGraphAuthoring | [`object Resource(string path)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L63) | public static |
| VfxGraphAuthoring | [`object Graph(string path)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L64) | public static |
| VfxGraphAuthoring | [`Object[] Contents(string path)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L65) | public static |
| VfxGraphAuthoring | [`void Save(string path)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L66) | public static |
| VfxGraphAuthoring | [`object Create(string name)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L74) | public static |
| VfxGraphAuthoring | [`void Add(object parent, object child)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L75) | public static |
| VfxGraphAuthoring | [`void Setting(object target, string fieldName, object value)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L76) | public static |
| VfxGraphAuthoring | [`object Slot(object target, string name)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L97) | public static |
| VfxGraphAuthoring | [`void Value(object target, string name, object value)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L98) | public static |
| VfxGraphAuthoring | [`object Parameter(object graph, string name, object value)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L99) | public static |
| VfxGraphAuthoring | [`void Link(object parameter, object input)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L116) | public static |
| VfxGraphAuthoring | [`object Attribute(object context, string name, object value, bool multiply = false)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L117) | public static |
| VfxGraphAuthoring | [`void AddIntensity(string path)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L131) | public static |
| VfxGraphAuthoring | [`object Describe(string path)`](../Assets/Editor/Presentation/VfxGraphAuthoring.cs#L149) | public static |

## Assets/Editor/Salvage/SalvageAssetBaker.cs

// 职责：生成并保存货仓、回收、掉落物和相关配置资产。 // 入口：Bake() / Set() / Preview()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SalvageAssetBaker | [`void Bake()`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L15) | public static |
| SalvageAssetBaker | [`Material Material(string name, Color color, bool transparent, bool emissive)`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L68) | private static |
| SalvageAssetBaker | [`void Hold( string name, CargoKind kind, int size, int capacity, bool explosive, bool enemyDrops)`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L107) | private static |
| SalvageAssetBaker | [`void CollectionBay()`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L246) | private static |
| SalvageAssetBaker | [`void DropPrefab()`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L331) | private static |
| SalvageAssetBaker | [`GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L372) | private static |
| SalvageAssetBaker | [`void Set(Object target, string property, Object value)`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L385) | public static |
| SalvageAssetBaker | [`void Preview()`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L392) | public static |

## Assets/Editor/Salvage/SalvagePlayProbe.cs

// 职责：在 Play Mode 检查货仓、回收、机器人和持久化行为。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SalvagePlayProbe | [`void Run()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L32) | public static |
| SalvagePlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L49) | private static |
| SalvagePlayProbe | [`void Capture(string condition, string stack, LogType type)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L92) | private static |
| SalvagePlayProbe | [`void Tick()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L100) | private static |
| SalvagePlayProbe | [`void Finish()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L130) | private static |
| SalvagePlayProbe | [`void Check(string name, bool result)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L157) | private static |
| SalvagePlayProbe | [`ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool cockpit = true)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L169) | private static |
| SalvagePlayProbe | [`GameObject Spawn(string name, ControlUnit unit, Vector3 local)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L196) | private static |
| SalvagePlayProbe | [`CargoItem Coins(int count)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L211) | private static |
| SalvagePlayProbe | [`CargoItem Part()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L216) | private static |
| SalvagePlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L222) | private static |

## Assets/Scripts/Features/Bootstrap/GameManager.cs

// 职责：保存当前方块父对象，启动时初始化界面与默认存档加载。 // 入口：Init()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GameManager | [`void Awake()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L9) | private |
| GameManager | [`void Start()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L15) | private |
| GameManager | [`void Init()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L20) | public static |

## Assets/Scripts/Features/Bots/Bot.cs

// 职责：保存无人机 Inspector 配置并组织任务飞行、返航、离家和精确停靠状态。 // 入口：派生机器人的 FixedUpdate → TickNavigation() → NavigateToTarget()/NavigateHomeSmoothly()。 // 依赖：BotFlightMotion 处理飞行/制动；BotObstacleAvoidance 负责三维净空与脱困。 // 不变量：homeOffset 保持 Home 局部坐标；停靠切为运动学刚体，完成后恢复准确局部姿态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Bot | [`void Start()`](../Assets/Scripts/Features/Bots/Bot.cs#L130) | protected virtual |
| Bot | [`bool TickNavigation()`](../Assets/Scripts/Features/Bots/Bot.cs#L158) | protected |
| Bot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/Bot.cs#L171) | protected virtual |
| Bot | [`void OnDestroy()`](../Assets/Scripts/Features/Bots/Bot.cs#L190) | protected virtual |
| Bot | [`Power SetHomePowerRequested(bool requested)`](../Assets/Scripts/Features/Bots/Bot.cs#L196) | protected |
| Bot | [`void PrepareForHomeDestruction()`](../Assets/Scripts/Features/Bots/Bot.cs#L209) | public virtual |
| Bot | [`void InitializeTrail()`](../Assets/Scripts/Features/Bots/Bot.cs#L213) | private |
| Bot | [`void LateUpdate()`](../Assets/Scripts/Features/Bots/Bot.cs#L233) | private |
| Bot | [`void NavigateToTarget(Transform target)`](../Assets/Scripts/Features/Bots/Bot.cs#L246) | protected |
| Bot | [`void NavigateHomeSmoothly()`](../Assets/Scripts/Features/Bots/Bot.cs#L266) | protected |
| Bot | [`Vector3 GetRelativeHomeVelocity(Vector3 worldPosition)`](../Assets/Scripts/Features/Bots/Bot.cs#L303) | private |
| Bot | [`void ReturnHome()`](../Assets/Scripts/Features/Bots/Bot.cs#L309) | private |
| Bot | [`void LeaveHome()`](../Assets/Scripts/Features/Bots/Bot.cs#L345) | private |
| Bot | [`void SetNavigationState(NavigationState nextState)`](../Assets/Scripts/Features/Bots/Bot.cs#L357) | private |
| Bot | [`BotFlightMotion.Settings CreateFlightSettings()`](../Assets/Scripts/Features/Bots/Bot.cs#L377) | private |
| Bot | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Bots/Bot.cs#L404) | private |

## Assets/Scripts/Features/Bots/BotAvoidanceResult.cs

// 职责：表达一次导航采样的方向修正、强度、速度比例和障碍数量。 // Direction 在世界坐标中；没有障碍时 SpeedMultiplier 为 1。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Bots/BotAvoidanceSettings.cs

// 职责：携带单次障碍采样的距离、权重、过滤条件；值来自 Bot Inspector。 // 单位：Range/Radius/ProgressDistance 为米，Timeout/HoldTime 为秒，BrakingAcceleration 为米/秒²。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Bots/BotFlightEffects.cs

// 职责：按实际飞行速度更新已创作的飞行 VFX，并在停靠状态清理尾迹。 // 入口：Bot.LateUpdate() 调用 Update()；导航状态切换调用 SetDocked()。 // 不变量：只改变 VFX 的显示强度/启用状态，不修改刚体、导航或 Inspector 参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotFlightEffects | [`void Update( Rigidbody rb, bool flying, bool showTrail, float movementSpeed, VfxEffect trailRenderer, VfxEffect trailCoreRenderer, VfxEffect flightEffect)`](../Assets/Scripts/Features/Bots/BotFlightEffects.cs#L8) | public static |
| BotFlightEffects | [`void SetDocked(bool docked, VfxEffect trailRenderer, VfxEffect trailCoreRenderer, VfxEffect flightEffect)`](../Assets/Scripts/Features/Bots/BotFlightEffects.cs#L40) | public static |

## Assets/Scripts/Features/Bots/BotFlightMotion.cs

// 职责：缓存导航采样、平滑飞行方向，执行任务飞行与移动 Home 的相对速度制动。 // 入口：NavigateToTarget()/NavigateHome()；只能由机器人 FixedUpdate 导航路径调用。 // 不变量：Home 目标点每次重新计算；只缓存避障建议，不缓存移动目标的世界位置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotFlightMotion | [`void ResetDirection()`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L44) | public |
| BotFlightMotion | [`void ClearTarget()`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L49) | public |
| BotFlightMotion | [`void NavigateToTarget(Transform target, Settings settings)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L58) | public |
| BotFlightMotion | [`void NavigateHome( Transform home, Vector3 approachPosition, Rigidbody homeRigidbody, float rangeScale, Settings settings)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L69) | public |
| BotFlightMotion | [`void NavigateToPosition( Vector3 targetPosition, Transform targetReference, Rigidbody homeRigidbody, bool returningHome, float avoidanceRangeScale, Settings settings)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L85) | private |
| BotFlightMotion | [`void SampleGuidance( Transform targetReference, Vector3 targetDirection, float targetDistance, float avoidanceRangeScale)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L107) | private |
| BotFlightMotion | [`void UpdateDirectionAndSpeed(Vector3 targetDirection, float avoidanceRangeScale)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L129) | private |
| BotFlightMotion | [`void ApplyMovement(Vector3 targetPosition, Vector3 targetDirection, Rigidbody homeRigidbody, bool returningHome)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L154) | private |
| BotFlightMotion | [`void ApplyReturnHomeMovement(Vector3 targetPosition, Vector3 targetDirection, float effectiveSpeed, Rigidbody homeRigidbody)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L178) | private |

## Assets/Scripts/Features/Bots/BotNavigationGizmos.cs

// 职责：绘制最近一次避障采样、导航方向、速度和状态；仅显示，不修改机器人状态。 // 入口：Bot.OnDrawGizmos()；UnityEditor Handles 只在 Editor 编译分支使用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationGizmos | [`void Draw( Transform transform, Transform navigateTarget, Rigidbody rb, Bot.NavigationState currentState, BotFlightMotion motion, bool showAvoidanceRays, bool showDirectionVectors)`](../Assets/Scripts/Features/Bots/BotNavigationGizmos.cs#L7) | public static |

## Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs

// 职责：用机器人半径探测三维净空，保持绕障方向，并在没有目标进展时选择脱困路线。 // 入口：Evaluate() 采样导航，ConstrainVelocity() 在每个物理帧阻止继续朝近处障碍加速。 // 生命周期：每台机器人拥有独立缓冲和绕障记忆；切换目标时 Reset() 清理旧路线。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotObstacleAvoidance | [`void Reset()`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L46) | public |
| BotObstacleAvoidance | [`void ClearDebugHits()`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L57) | public |
| BotObstacleAvoidance | [`BotAvoidanceResult Evaluate( Transform target, Vector3 velocity, Vector3 targetDirection, float rangeScale, BotAvoidanceSettings settings, float targetDistance = float.PositiveInfinity)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L63) | public |
| BotObstacleAvoidance | [`bool CheckProgress(float targetDistance)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L147) | private |
| BotObstacleAvoidance | [`Vector3 ChooseClearDirection(Vector3 targetDirection, float range, bool stalled)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L158) | private |
| BotObstacleAvoidance | [`void ConsiderDirection( Vector3 candidate, Vector3 targetDirection, float range, bool stalled, ref float bestScore, ref Vector3 bestDirection)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L202) | private |
| BotObstacleAvoidance | [`Vector3 ConstrainVelocity(Vector3 desiredVelocity, float deltaTime, Vector3 currentVelocity, float brakingAcceleration)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L223) | public |
| BotObstacleAvoidance | [`Vector3 LimitApproachSpeed(Vector3 desiredVelocity, Vector3 corridor, float lookAhead, float margin, float acceleration)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L249) | private |
| BotObstacleAvoidance | [`float GetClearDistance(Vector3 direction, float range)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L266) | private |
| BotObstacleAvoidance | [`bool Cast(Vector3 direction, float range, out RaycastHit nearest)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L271) | private |
| BotObstacleAvoidance | [`bool TryGetOverlapEscape(out Vector3 escape)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L304) | private |
| BotObstacleAvoidance | [`bool IsIgnored(Collider collider)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L359) | private |

## Assets/Scripts/Features/Bots/CollectionBot.cs

// 职责：执行掉落物寻找、拾取、运输和入库，复用 Bot 导航。 // 入口：FixedUpdate() → 核对任务 → 寻找 / 拾取 / 返航交付。 // 生命周期：停用、销毁、Home 摧毁或任务失效时释放 LootDrop 认领；停靠后才入库。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CollectionBot | [`void Start()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L22) | protected override |
| CollectionBot | [`void FixedUpdate()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L31) | private |
| CollectionBot | [`bool IsMissionInvalid(ControlUnit owner, bool canWork)`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L75) | private |
| CollectionBot | [`void CollectTarget()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L80) | private |
| CollectionBot | [`void ReturnCargo()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L89) | private |
| CollectionBot | [`void FindTarget(ControlUnit owner)`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L104) | private |
| CollectionBot | [`void Abandon()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L141) | private |
| CollectionBot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L152) | protected override |
| CollectionBot | [`void OnDestroy()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L158) | protected override |
| CollectionBot | [`void PrepareForHomeDestruction()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L164) | public override |

## Assets/Scripts/Features/Bots/RepairBeamView.cs

// 职责：初始化与显示维修光束、命中 VFX，并计算可见目标表面的光束落点。 // 入口：Initialize()/Update()/Hide()；只修改表现，维修计时和耐久规则由 RepairBot 持有。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RepairBeamView | [`void Initialize(GameObject gameObject, VfxEffect impact, float beamWidth)`](../Assets/Scripts/Features/Bots/RepairBeamView.cs#L15) | public |
| RepairBeamView | [`void Hide()`](../Assets/Scripts/Features/Bots/RepairBeamView.cs#L36) | public |
| RepairBeamView | [`void Update(bool active, Durability target, Vector3 origin, Gradient gradient)`](../Assets/Scripts/Features/Bots/RepairBeamView.cs#L49) | public |
| RepairBeamView | [`Vector3 GetRepairTargetPoint(Durability target, Vector3 origin)`](../Assets/Scripts/Features/Bots/RepairBeamView.cs#L77) | public |
| RepairBeamView | [`void UpdateRepairImpact(Vector3 targetPoint, Vector3 origin, float pulse)`](../Assets/Scripts/Features/Bots/RepairBeamView.cs#L98) | private |
| RepairBeamView | [`void SetRepairImpactActive(bool active)`](../Assets/Scripts/Features/Bots/RepairBeamView.cs#L115) | private |

## Assets/Scripts/Features/Bots/RepairBot.cs

// 职责：寻找受损友方设备并维修，复用 Bot 的导航和返航能力。 // 入口：FixedUpdate() → 搜索受损目标 / 导航并维修；ClearTarget() 取消当前维修。 // 依赖：RepairTargetSearch 扫描归属与范围；RepairBeamView 显示光束与命中效果。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RepairBot | [`void Start()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L26) | protected override |
| RepairBot | [`void FixedUpdate()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L36) | private |
| RepairBot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L89) | protected override |
| RepairBot | [`void FindDamagedBlock()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L95) | private |
| RepairBot | [`void CheckAndRepair()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L108) | private |
| RepairBot | [`void UpdateRepairBeam(bool active)`](../Assets/Scripts/Features/Bots/RepairBot.cs#L149) | private |
| RepairBot | [`void EnsureRepairBeamGradient()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L154) | private |
| RepairBot | [`void ClearTarget()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L167) | public |

## Assets/Scripts/Features/Bots/RepairTargetSearch.cs

// 职责：扫描 Home 范围内的友方耐久对象，并选择距离机器人最近的受损目标。 // 入口：Scan()/FindClosestDamaged()；IsValidTarget() 用于执行维修前重新核对归属与范围。 // 生命周期：非分配碰撞缓冲按需扩展至 1024；多个 Collider 归属的耐久对象只记录一次。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RepairTargetSearch | [`void Scan(Transform home, int targetRange)`](../Assets/Scripts/Features/Bots/RepairTargetSearch.cs#L15) | public |
| RepairTargetSearch | [`Durability FindClosestDamaged(Vector3 position)`](../Assets/Scripts/Features/Bots/RepairTargetSearch.cs#L58) | public |
| RepairTargetSearch | [`bool IsValidTarget(Durability target, Transform home, int targetRange)`](../Assets/Scripts/Features/Bots/RepairTargetSearch.cs#L80) | public static |

## Assets/Scripts/Features/Building/ActionCounterUI.cs

// 职责：展示建造动作历史计数及 Undo/Redo 状态。 // 入口：UpdateUndoText() / UpdateRedoText()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ActionCounterUI | [`void Awake()`](../Assets/Scripts/Features/Building/ActionCounterUI.cs#L11) | private |
| ActionCounterUI | [`void FixedUpdate()`](../Assets/Scripts/Features/Building/ActionCounterUI.cs#L16) | private |
| ActionCounterUI | [`void UpdateUndoText(int count)`](../Assets/Scripts/Features/Building/ActionCounterUI.cs#L22) | public |
| ActionCounterUI | [`void UpdateRedoText(int count)`](../Assets/Scripts/Features/Building/ActionCounterUI.cs#L37) | public |

## Assets/Scripts/Features/Building/ActionManager.cs

// 职责：管理建造动作的 Undo/Redo 栈与操作计数。 // 入口：Push() / Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ActionManager | [`void Awake()`](../Assets/Scripts/Features/Building/ActionManager.cs#L18) | private |
| ActionManager | [`void Push(IBlockAction action)`](../Assets/Scripts/Features/Building/ActionManager.cs#L26) | public |
| ActionManager | [`void Undo()`](../Assets/Scripts/Features/Building/ActionManager.cs#L37) | public |
| ActionManager | [`void Redo()`](../Assets/Scripts/Features/Building/ActionManager.cs#L50) | public |
| ActionManager | [`void Clear()`](../Assets/Scripts/Features/Building/ActionManager.cs#L60) | public |
| ActionManager | [`void CountAction(IBlockAction action)`](../Assets/Scripts/Features/Building/ActionManager.cs#L69) | private |
| ActionManager | [`int GetTotalActionCount()`](../Assets/Scripts/Features/Building/ActionManager.cs#L84) | public |
| ActionManager | [`int GetActionCount(string actionName)`](../Assets/Scripts/Features/Building/ActionManager.cs#L88) | public |
| ActionManager | [`void ShowDebug()`](../Assets/Scripts/Features/Building/ActionManager.cs#L93) | public |
| ActionManager | [`int GetUndoCount()`](../Assets/Scripts/Features/Building/ActionManager.cs#L99) | public |
| ActionManager | [`int GetRedoCount()`](../Assets/Scripts/Features/Building/ActionManager.cs#L101) | public |

## Assets/Scripts/Features/Building/AddBlockAction.cs

// 职责：保存新建方块快照，实施创建动作的撤销与重做。 // 入口：Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CreateBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/AddBlockAction.cs#L26) | public |
| CreateBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/AddBlockAction.cs#L39) | public |

## Assets/Scripts/Features/Building/BlockData.cs

// 职责：定义方块 JSON 存档字段及方块与数据的转换，包括货仓内容。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs

// 职责：在其他组件 Awake 前应用 CSV 配置；OnValidate 仅请求 Editor 延迟校验。 // 入口：Awake() / OnValidate()；相同 CSV 共用已校验快照，错误保留 Prefab 原参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvBinding | [`void SetPrefabGuid(string guid)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L18) | public |
| BlockCsvBinding | [`void Awake()`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L23) | private |
| BlockCsvBinding | [`void OnValidate()`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L28) | private |
| BlockCsvBinding | [`void RequestValidation()`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L33) | public |
| BlockCsvBinding | [`bool Apply()`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L41) | public |
| BlockCsvBinding | [`void ResetCache()`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L74) | public static |

## Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs

// 职责：严格解析 CSV、校验完整配置，并按白名单字段构造组件 JSON；不修改资源文件。 // 入口：Parse() / Apply()；支持逗号、引号、多行说明与 UTF-8 BOM。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvDocument | [`BlockCsvDocument Parse(string text)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L33) | public static |
| BlockCsvDocument | [`bool Apply(GameObject root, string guid)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L73) | public |
| BlockCsvDocument | [`string PathFor(string guid)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L144) | public |
| BlockCsvDocument | [`void ValidateRow(List<string> values)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L151) | private |
| BlockCsvDocument | [`string Normalize(BlockDataTableSchema.Column column, string value, string name)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L171) | private static |
| BlockCsvDocument | [`string Quote(string text)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L216) | private static |
| BlockCsvDocument | [`string WriteRecords(IEnumerable<IEnumerable<string>> records)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L222) | public static |
| BlockCsvDocument | [`List<List<string>> ParseRecords(string text)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L234) | public static |

## Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs

// 职责：定义 CSV 支持的配置字段、类型、枚举和有效范围，Editor 与运行时共享同一契约。 // 入口：CreateColumns()；不包含运行时状态或资源引用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Column | [`Component[] FindTargets(GameObject root)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L31) | public |
| BlockDataTableSchema | [`List<Column> CreateColumns()`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L69) | public static |
| BlockDataTableSchema | [`void AddFloat( List<Column> columns, string group, string property, string label, double minimum = 0, double maximum = double.PositiveInfinity)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L221) | private static |
| BlockDataTableSchema | [`void AddInteger( List<Column> columns, string group, string property, string label, double minimum = 0, double maximum = double.PositiveInfinity)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L240) | private static |
| BlockDataTableSchema | [`void AddBoolean(List<Column> columns, string group, string property, string label)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L259) | private static |
| BlockDataTableSchema | [`void AddString(List<Column> columns, string group, string property, string label)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L272) | private static |
| BlockDataTableSchema | [`void AddEnum(List<Column> columns, string group, string property, string label, string[] names)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L285) | private static |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs

// 职责：从存档恢复一个方块的身份、尺寸、阵营、货仓和耐久。 // 入口：TryRestore()；只创建实体，不负责加载会话、统计、UI 或 JSON 写入。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintBlockFactory | [`bool TryRestore(BlockData data, Transform parent, UnitFaction faction, out Block block)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7) | public static |
| BlueprintBlockFactory | [`void ApplyFaction(Block block, UnitFaction faction)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L55) | public static |
| BlueprintBlockFactory | [`string ToResourcePath(string fullPath)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L63) | public static |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintFileStore.cs

// 职责：统一蓝图文件命名、列表、重命名及不覆盖源文件的复制规则。 // 入口：GetNames() / TryRename() / TryDuplicate()；不依赖 Unity、管理器或 UI。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintFileStore | [`string GetPath(string name)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintFileStore.cs#L15) | public |
| BlueprintFileStore | [`IEnumerable<string> GetNames()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintFileStore.cs#L21) | public |
| BlueprintFileStore | [`bool TryRename(string oldName, string newName, out string reason)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintFileStore.cs#L30) | public |
| BlueprintFileStore | [`bool TryDuplicate(string name, out string duplicateName, out string reason)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintFileStore.cs#L68) | public |
| BlueprintFileStore | [`bool CanUseFileName(string name, out string reason)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintFileStore.cs#L103) | private static |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs

// 职责：根据完整蓝图的逻辑包围盒控制加载环绕镜头，避免随逐帧生成不断后退。 // 入口：Begin() / Stop()；由 BlueprintLoader 管理生命周期。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintLoadCamera | [`void Begin(GameObject parent, List<BlockData> blocks, BlueprintLoadSettings settings, Camera camera)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L15) | public |
| BlueprintLoadCamera | [`void Stop()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L26) | public |
| BlueprintLoadCamera | [`void StartLoadingCameraOrbit(GameObject frameObject, int blockCount)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L37) | private |
| BlueprintLoadCamera | [`float CalculateLoadingCameraOrbitDegreesPerSecond(int blockCount)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L84) | private |
| BlueprintLoadCamera | [`CameraController GetMainCameraController()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L90) | private |
| BlueprintLoadCamera | [`float GetCameraOrbitAngle(Vector3 orbitCenter)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L96) | private |
| BlueprintLoadCamera | [`bool TryCalculateLoadingCameraOrbitBounds(List<BlockData> blocksToLoad, Vector3 fallbackCenter, out Bounds bounds)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L113) | private |
| BlueprintLoadCamera | [`Bounds CalculateBlockDataBounds(BlockData data)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L144) | private |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadSettings.cs

// 职责：保存一次蓝图加载所使用的延时与镜头参数快照。 // 来源：BuildManager 的现有 Inspector 字段；不改变场景或 Prefab 的字段身份。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintLoadSettings | [`float GetInterval(double elapsedSeconds)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadSettings.cs#L19) | public |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs

// 职责：管理逐帧蓝图恢复、请求所有权、取消、统计及连接重建。 // 入口：Load() / Cancel()；依赖由 BuildManager 装配，UI 通过 StatisticsChanged 消费结果。 // 生命周期：新请求使旧版本失效；结束、取消及异常均释放镜头与会话状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintLoader | [`void Load( BuildTargetContext target, GameObject parentPrefab, BlueprintLoadSettings settings, Camera camera, Action createInitialBlock)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L39) | public |
| BlueprintLoader | [`bool IsLoadingTarget(BuildTargetContext target)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L81) | public |
| BlueprintLoader | [`void Cancel()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L87) | public |
| BlueprintLoader | [`IEnumerator LoadRoutine(int version, List<BlockData> blocks, BlueprintLoadSettings settings, Action createInitialBlock)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L100) | private |
| BlueprintLoader | [`bool TryRead(string path, out BlockDataList data)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L167) | private |
| BlueprintLoader | [`void ReplaceParent(GameObject prefab, string name)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L206) | private |
| BlueprintLoader | [`bool OwnsParent(int version)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L222) | private |
| BlueprintLoader | [`void RemoveMissingData(HashSet<string> ids)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L227) | private |
| BlueprintLoader | [`void RebuildConnections()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L238) | private |
| BlueprintLoader | [`void ReleaseInvalidParent()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L250) | private |
| BlueprintLoader | [`void FinishSession()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L270) | private |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs

// 职责：统一蓝图完整刷新与增量恢复的质量、耗电和发电统计。 // 入口：Accumulate() / Calculate()；不读取管理器单例，不更新 UI。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintStatistics | [`void Accumulate(Block block)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs#L26) | public |
| BlueprintStatistics | [`BlueprintStatistics Calculate(List<BlockData> savedBlocks, Transform parent)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs#L47) | public static |

## Assets/Scripts/Features/Building/BlueprintUIPanel.cs

// 职责：展示蓝图名称、数量、质量和功率；业务统计由 BlueprintStatistics 提供。 // 入口：Refresh() / DisplayStatistics() / UpdateCurrentSaveName()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintUIPanel | [`void Awake()`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L14) | private |
| BlueprintUIPanel | [`void Start()`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L19) | private |
| BlueprintUIPanel | [`void OnDestroy()`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L24) | private |
| BlueprintUIPanel | [`void Refresh()`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L33) | public |
| BlueprintUIPanel | [`void DisplayStatistics(BlueprintStatistics statistics)`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L44) | public |
| BlueprintUIPanel | [`void UpdateCurrentSaveName(string newName)`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L49) | public |
| BlueprintUIPanel | [`void UpdateStatistics(int blockCount, float mass, float requiredPower, float generatorOutput)`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L57) | public |
| BlueprintUIPanel | [`void UpdateTotalNumber(int newNumber)`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L65) | public |
| BlueprintUIPanel | [`void UpdateTotalMass(float newMass)`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L73) | public |
| BlueprintUIPanel | [`void UpdateTotalRequiredPower(float newRequiredPower)`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L82) | public |
| BlueprintUIPanel | [`void UpdateTotalGeneratorOutput(float newGeneratorOutput)`](../Assets/Scripts/Features/Building/BlueprintUIPanel.cs#L91) | public |

## Assets/Scripts/Features/Building/BuildManager.cs

// 职责：协调建造输入、选择与放置；蓝图加载委托 Building 功能，保留现有 Inspector 契约。 // 入口：SetBuildMode() / ToggleEnemyBlueprintBuildMode() / EnterEnemyBlueprintBuildMode()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildManager | [`void Awake()`](../Assets/Scripts/Features/Building/BuildManager.cs#L63) | private |
| BuildManager | [`void OnDisable()`](../Assets/Scripts/Features/Building/BuildManager.cs#L68) | private |
| BuildManager | [`void OnDestroy()`](../Assets/Scripts/Features/Building/BuildManager.cs#L73) | private |
| BuildManager | [`void Start()`](../Assets/Scripts/Features/Building/BuildManager.cs#L90) | private |
| BuildManager | [`void Update()`](../Assets/Scripts/Features/Building/BuildManager.cs#L96) | private |
| BuildManager | [`void SetBuildMode(bool lockView)`](../Assets/Scripts/Features/Building/BuildManager.cs#L154) | public |
| BuildManager | [`void ToggleEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Building/BuildManager.cs#L166) | public |
| BuildManager | [`void EnterEnemyBlueprintBuildMode(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L178) | public |
| BuildManager | [`void ExitEnemyBlueprintBuildMode(bool reloadPlayerSave)`](../Assets/Scripts/Features/Building/BuildManager.cs#L208) | public |
| BuildManager | [`void SetCurrentBlockResource(string resourcePath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L227) | public |
| BuildManager | [`void SetCurrentSaveName(string saveName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L242) | public |
| BuildManager | [`void SetCurrentEnemyBlueprintName(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L247) | public |
| BuildManager | [`void SetBuildType(BuildType buildType)`](../Assets/Scripts/Features/Building/BuildManager.cs#L252) | public |
| BuildManager | [`void EnsureBuildInteraction()`](../Assets/Scripts/Features/Building/BuildManager.cs#L279) | private |
| BuildManager | [`void HandleBuildingPreview()`](../Assets/Scripts/Features/Building/BuildManager.cs#L306) | private |
| BuildManager | [`void ClearCurrentGhost(bool hideHints = true)`](../Assets/Scripts/Features/Building/BuildManager.cs#L314) | private |
| BuildManager | [`void DeselectBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L322) | public |
| BuildManager | [`Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)`](../Assets/Scripts/Features/Building/BuildManager.cs#L329) | public |
| BuildManager | [`bool IsWithinBuildRange(Vector3 position)`](../Assets/Scripts/Features/Building/BuildManager.cs#L335) | public |
| BuildManager | [`void CreateBlock(GameObject prefab, string resourcePath, Vector3 pos, Quaternion rot)`](../Assets/Scripts/Features/Building/BuildManager.cs#L340) | public |
| BuildManager | [`void DeleteBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L367) | public |
| BuildManager | [`void EnsureBlueprintEditor()`](../Assets/Scripts/Features/Building/BuildManager.cs#L388) | private |
| BuildManager | [`Block CreateDuplicateBlock(GameObject prefab, string path, Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/Building/BuildManager.cs#L397) | private |
| BuildManager | [`void SaveBlock(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L404) | public |
| BuildManager | [`void RemoveBlock(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L417) | public |
| BuildManager | [`void LoadAllBlocks()`](../Assets/Scripts/Features/Building/BuildManager.cs#L425) | public |
| BuildManager | [`BlueprintStatistics GetBlueprintStatistics()`](../Assets/Scripts/Features/Building/BuildManager.cs#L443) | public |
| BuildManager | [`void EnsureBlueprintLoader()`](../Assets/Scripts/Features/Building/BuildManager.cs#L450) | private |
| BuildManager | [`void DisplayBlueprintStatistics(BlueprintStatistics statistics)`](../Assets/Scripts/Features/Building/BuildManager.cs#L459) | private |
| BuildManager | [`BlueprintLoadSettings CreateLoadSettings()`](../Assets/Scripts/Features/Building/BuildManager.cs#L467) | private |
| BuildManager | [`void ClearUnloadableData(string id)`](../Assets/Scripts/Features/Building/BuildManager.cs#L485) | public |
| BuildManager | [`bool IsLoadingBuildTarget(BuildTargetKind kind, string saveName, string enemyBlueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L490) | public |
| BuildManager | [`bool IsLoadingBuildTarget(BuildTargetContext context)`](../Assets/Scripts/Features/Building/BuildManager.cs#L496) | private |
| BuildManager | [`void ClearUnloadableData(string id, string targetSavePath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L501) | private |
| BuildManager | [`void InitialBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L510) | private |
| BuildManager | [`bool CanCreateBlock(GameObject prefab, out string reason)`](../Assets/Scripts/Features/Building/BuildManager.cs#L518) | private |
| BuildManager | [`void ApplyBlockBuildDefaults(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L542) | public |
| BuildManager | [`int CountCockpitsInCurrentConstruct()`](../Assets/Scripts/Features/Building/BuildManager.cs#L547) | private |
| BuildManager | [`void ResetBuildState()`](../Assets/Scripts/Features/Building/BuildManager.cs#L557) | private |
| BuildManager | [`void SetBuildTarget(BuildTargetContext context)`](../Assets/Scripts/Features/Building/BuildManager.cs#L567) | private |
| BuildManager | [`void RefreshBlueprintUI()`](../Assets/Scripts/Features/Building/BuildManager.cs#L579) | private |
| BuildManager | [`void StopActiveBlockLoad()`](../Assets/Scripts/Features/Building/BuildManager.cs#L587) | private |
| BuildManager | [`string ConvertToResourcesPath(string fullPath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L592) | public static |

## Assets/Scripts/Features/Building/BuildPalette.cs

// 职责：管理建造分类、选择与目录滚动显示。 // 入口：SelectCategory() / ShowTooltip() / HideTooltip()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPalette | [`void Awake()`](../Assets/Scripts/Features/Building/BuildPalette.cs#L27) | private |
| BuildPalette | [`void OnValidate()`](../Assets/Scripts/Features/Building/BuildPalette.cs#L53) | private |
| BuildPalette | [`void OnEnable()`](../Assets/Scripts/Features/Building/BuildPalette.cs#L67) | private |
| BuildPalette | [`void OnDisable()`](../Assets/Scripts/Features/Building/BuildPalette.cs#L72) | private |
| BuildPalette | [`void LateUpdate()`](../Assets/Scripts/Features/Building/BuildPalette.cs#L77) | private |
| BuildPalette | [`void SelectCategory(int category)`](../Assets/Scripts/Features/Building/BuildPalette.cs#L98) | public |
| BuildPalette | [`void ShowTooltip(BuildPaletteItem item)`](../Assets/Scripts/Features/Building/BuildPalette.cs#L124) | public |
| BuildPalette | [`void HideTooltip(BuildPaletteItem item)`](../Assets/Scripts/Features/Building/BuildPalette.cs#L133) | public |
| BuildPalette | [`BuildPaletteItem GetSelectedItem()`](../Assets/Scripts/Features/Building/BuildPalette.cs#L145) | public |

## Assets/Scripts/Features/Building/BuildPaletteItem.cs

// 职责：展示单个建造条目的图标、选择反馈与悬停参数。 // 入口：SetSelected() / OnPointerEnter() / OnPointerExit()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPaletteItem | [`void Awake()`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L26) | private |
| BuildPaletteItem | [`void BindPalette(BuildPalette palette)`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L35) | public |
| BuildPaletteItem | [`void ApplyButtonStyle(BuildPaletteItem template)`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L45) | public |
| BuildPaletteItem | [`void SetSelected(bool selected, Color color)`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L63) | public |
| BuildPaletteItem | [`void OnPointerEnter(PointerEventData eventData)`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L68) | public |
| BuildPaletteItem | [`void OnPointerExit(PointerEventData eventData)`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L73) | public |
| BuildPaletteItem | [`void OnSelect(BaseEventData eventData)`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L86) | public |
| BuildPaletteItem | [`void OnDeselect(BaseEventData eventData)`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L91) | public |
| BuildPaletteItem | [`void OnDisable()`](../Assets/Scripts/Features/Building/BuildPaletteItem.cs#L96) | private |

## Assets/Scripts/Features/Building/BuildTargetContext.cs

// 职责：定义玩家/敌方蓝图目标、名称、阵营和对应存档路径。 // 入口：GetSavePath() / PlayerSave() / EnemyBlueprint()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildTargetContext | [`string GetSavePath(SaveManager saveManager)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L25) | public |
| BuildTargetContext | [`BuildTargetContext PlayerSave(string saveName, string enemyBlueprintName)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L30) | public static |
| BuildTargetContext | [`BuildTargetContext EnemyBlueprint(string saveName, string enemyBlueprintName)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L35) | public static |
| BuildTargetContext | [`string NormalizeName(string name, string fallback)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L40) | private static |

## Assets/Scripts/Features/Building/ConnectorPlacementHints.cs

// 职责：显示可用连接点提示，按镜头、建造范围及占用状态筛选。 // 入口：Show() / Hide()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ConnectorPlacementHints | [`void Show(Camera camera, float radius, LayerMask layerMask, GameObject excludedGhost = null)`](../Assets/Scripts/Features/Building/ConnectorPlacementHints.cs#L52) | public |
| ConnectorPlacementHints | [`void Hide()`](../Assets/Scripts/Features/Building/ConnectorPlacementHints.cs#L72) | public |
| ConnectorPlacementHints | [`void OnDisable()`](../Assets/Scripts/Features/Building/ConnectorPlacementHints.cs#L78) | private |
| ConnectorPlacementHints | [`void ScanNearbyBlocks()`](../Assets/Scripts/Features/Building/ConnectorPlacementHints.cs#L86) | private |
| ConnectorPlacementHints | [`bool CanDisplay(Block block, Connector connector)`](../Assets/Scripts/Features/Building/ConnectorPlacementHints.cs#L115) | private |
| ConnectorPlacementHints | [`void LateUpdate()`](../Assets/Scripts/Features/Building/ConnectorPlacementHints.cs#L120) | private |

## Assets/Scripts/Features/Building/DeleteBlockAction.cs

// 职责：保存删除方块和货仓快照，实施删除动作的撤销与重做。 // 入口：Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DeleteBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/DeleteBlockAction.cs#L27) | public |
| DeleteBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/DeleteBlockAction.cs#L57) | public |

## Assets/Scripts/Features/Building/GroupAction.cs

// 职责：将多项建造动作组成一次可撤销/重做的事务。 // 入口：Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GroupAction | [`void Undo()`](../Assets/Scripts/Features/Building/GroupAction.cs#L15) | public |
| GroupAction | [`void Redo()`](../Assets/Scripts/Features/Building/GroupAction.cs#L24) | public |

## Assets/Scripts/Features/Building/IBlockAction.cs

// 职责：约定建造动作的撤销、重做和计数名称。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/IBlockAction.cs#L4) |  |
| IBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/IBlockAction.cs#L5) |  |

## Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs

// 职责：持久化建造编辑并同步受影响连接；不负责输入、Ghost 或加载会话。 // 入口：Save() / Remove() / RemoveData()；SaveManager 和几何查询由组合入口注入。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildBlueprintEditor | [`void Save(Block block, string path, UnitFaction faction)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L17) | public |
| BuildBlueprintEditor | [`void Remove(Block block, string path)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L35) | public |
| BuildBlueprintEditor | [`bool RemoveData(string id, string path)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L50) | public |
| BuildBlueprintEditor | [`void Write(string path)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L63) | private |
| BuildBlueprintEditor | [`void RefreshConnectionsAround(Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L68) | private |

## Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs

// 职责：处理所选方块的键盘移动/旋转、轴拖拽及复制，提交存档和 Undo 动作。 // 入口：Poll() / TryBeginDrag() / CancelDrag()；几何判定复用 BuildPlacementGeometry。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildManipulation | [`void Configure(Camera camera, GameObject move, GameObject rotate)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L52) | public |
| BuildManipulation | [`void Poll(Block block, BuildType type)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L59) | public |
| BuildManipulation | [`bool TryBeginDrag(RaycastHit hit, Block block, BuildType type)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L80) | public |
| BuildManipulation | [`void CancelDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L118) | public |
| BuildManipulation | [`void AlignAxisToNearestWorldDir()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L124) | private |
| BuildManipulation | [`void HandleMovement()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L165) | private |
| BuildManipulation | [`void HandleRotation()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L235) | private |
| BuildManipulation | [`void HandleMoveAxisDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L296) | private |
| BuildManipulation | [`void HandleRotateAxisDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L357) | private |
| BuildManipulation | [`void HandleDuplicate(Vector3 newPos, Quaternion newRot)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L408) | private |

## Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs

// 职责：集中建造尺寸、连接点对齐、半网格吸附、阻挡与步长几何。 // 入口：SnapCenterByMinCorner() / TryCalculateConnectorAlignedPosition() / IsBlocked()。 // 依赖：显式网格尺寸和方块层；无管理器单例、UI 或存档依赖。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPlacementGeometry | [`Vector3 GetBlockHalfExtents(Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L10) | public |
| BuildPlacementGeometry | [`float GetHalfExtentAlongDirection(Block block, Quaternion rotation, Vector3 worldDirection)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L21) | public |
| BuildPlacementGeometry | [`bool TryCalculateConnectorAlignedPosition( Block prefabBlock, Quaternion previewRotation, Vector3 targetConnectorPosition, Vector3 targetConnectorNormal, out Vector3 position)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L31) | public |
| BuildPlacementGeometry | [`Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L76) | public |
| BuildPlacementGeometry | [`bool IsBlocked(Vector3 targetCenter, Quaternion targetRotation, Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L121) | public |
| BuildPlacementGeometry | [`float GetMoveStep(Block block, Vector3 moveDir)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L148) | public |

## Assets/Scripts/Features/Building/Interaction/BuildPreview.cs

// 职责：管理建造 Ghost、连接点提示、有效位置判定和确认放置。 // 入口：Configure() / Poll() / Clear()；不负责选择、加载、JSON 或 Undo。 // 生命周期：退出建造或销毁时 Clear() 释放 Ghost 和提示。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPreview | [`void Configure(Camera camera, float range, Material highlight, ConnectorPlacementHints hints)`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L28) | public |
| BuildPreview | [`bool IsWithinBuildRange(Vector3 position)`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L36) | public |
| BuildPreview | [`void Poll()`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L41) | public |
| BuildPreview | [`void Clear(bool hideHints = true)`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L195) | public |

## Assets/Scripts/Features/Building/Interaction/BuildSelection.cs

// 职责：选择方块、恢复高亮材质和管理移动/旋转 Gizmo 的可见性。 // 入口：Poll() / SelectBlock() / DeselectBlock()；轴拖拽由 BuildManipulation 管理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildSelection | [`void Configure( Camera camera, LayerMask axes, LayerMask blocks, Material highlight, GameObject move, GameObject rotate)`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L26) | public |
| BuildSelection | [`void Poll()`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L42) | public |
| BuildSelection | [`void SelectBlock(Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L80) | public |
| BuildSelection | [`void DeselectBlock()`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L123) | public |
| BuildSelection | [`void SetGizmosEnabled(bool enabled)`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L140) | public |
| BuildSelection | [`void RefreshBuildGizmos()`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L146) | public |
| BuildSelection | [`void HideBuildGizmos()`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L166) | private |

## Assets/Scripts/Features/Building/MoveAxisHandle.cs

// 职责：标记并显示建造移动轴，供 BuildManager 拖拽操作使用。 // 入口：Awake()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MoveAxisHandle | [`void Awake()`](../Assets/Scripts/Features/Building/MoveAxisHandle.cs#L10) | private |
| MoveAxisHandle | [`Vector3 GetLocalAxis()`](../Assets/Scripts/Features/Building/MoveAxisHandle.cs#L18) | private |

## Assets/Scripts/Features/Building/MoveBlockAction.cs

// 职责：保存移动前后位置，撤销/重做后同步方块存档。 // 入口：Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MoveBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/MoveBlockAction.cs#L19) | public |
| MoveBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/MoveBlockAction.cs#L29) | public |

## Assets/Scripts/Features/Building/RotateAxisHandle.cs

// 职责：标记并显示建造旋转轴，供 BuildManager 拖拽操作使用。 // 入口：Awake()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RotateAxisHandle | [`void Awake()`](../Assets/Scripts/Features/Building/RotateAxisHandle.cs#L10) | private |
| RotateAxisHandle | [`Vector3 GetLocalAxis()`](../Assets/Scripts/Features/Building/RotateAxisHandle.cs#L18) | private |

## Assets/Scripts/Features/Building/RotateBlockAction.cs

// 职责：保存旋转前后姿态及位置，撤销/重做后同步存档。 // 入口：Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RotateBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/RotateBlockAction.cs#L23) | public |
| RotateBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/RotateBlockAction.cs#L34) | public |

## Assets/Scripts/Features/Building/SaveManager.cs

// 职责：管理玩家/敌方蓝图目录、名称和缓存，提供加载/复制/重命名入口。 // 入口：EnsureSaveDirectories() / GetAllSaveNames() / GetAllEnemyBlueprintNames()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SaveManager | [`void Awake()`](../Assets/Scripts/Features/Building/SaveManager.cs#L25) | private |
| SaveManager | [`void OnDestroy()`](../Assets/Scripts/Features/Building/SaveManager.cs#L39) | private |
| SaveManager | [`void EnsureSaveDirectories()`](../Assets/Scripts/Features/Building/SaveManager.cs#L47) | public |
| SaveManager | [`void GetAllSaveNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L60) | public |
| SaveManager | [`void GetAllEnemyBlueprintNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L70) | public |
| SaveManager | [`void CreateNewSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L80) | public |
| SaveManager | [`void CreateNewEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L98) | public |
| SaveManager | [`void LoadSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L111) | public |
| SaveManager | [`void LoadEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L135) | public |
| SaveManager | [`void DeleteSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L157) | public |
| SaveManager | [`void DeleteEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L169) | public |
| SaveManager | [`bool RenameSave(string oldSaveName, string newSaveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L181) | public |
| SaveManager | [`bool RenameEnemyBlueprint(string oldBlueprintName, string newBlueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L186) | public |
| SaveManager | [`bool RenameBlueprint(string oldName, string newName, bool enemy)`](../Assets/Scripts/Features/Building/SaveManager.cs#L191) | private |
| SaveManager | [`void DuplicateSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L238) | public |
| SaveManager | [`string GetSavePath(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L252) | public |
| SaveManager | [`string GetEnemyBlueprintPath(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L257) | public |
| SaveManager | [`string GetSaveFileSize(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L262) | public |

## Assets/Scripts/Features/Building/SaveUIPanel.cs

// 职责：展示玩家/敌方蓝图存档列表并绑定选择动作。 // 入口：RefreshList()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SaveUIPanel | [`void Awake()`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L12) | private |
| SaveUIPanel | [`void RefreshList()`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L17) | public |
| SaveUIPanel | [`void ConfigureSaveItem(GameObject obj, string saveName, UnityEngine.Events.UnityAction onOpen)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L53) | private |
| SaveUIPanel | [`Button CreateRenameButton(Button deleteButton, Transform parent)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L71) | private |
| SaveUIPanel | [`void OnSaveClicked(string saveName)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L88) | private |
| SaveUIPanel | [`void OnEnemyBlueprintClicked(string blueprintName)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L93) | private |
| SaveUIPanel | [`void OnDuplicateClicked(string saveName)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L102) | private |

## Assets/Scripts/Features/Combat/CollisionDamageResolver.cs

// 职责：将单次碰撞的法向求解器能量分摊给本控制单元实际接触的模块。 // 入口：ApplyContactDamage()；仅由 ControlUnit.OnCollisionEnter() 调用。 // 约束：爆炸保护检查双方；伤害前记录玩家冲击，避免重组丢失原事件信息。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CollisionDamageResolver | [`void ApplyContactDamage(ControlUnit unit, Collision collision)`](../Assets/Scripts/Features/Combat/CollisionDamageResolver.cs#L10) | internal |

## Assets/Scripts/Features/Combat/CombatHud.cs

// 职责：展示敌方耐久名牌、玩家相关战斗信息与击杀提示。 // 入口：ResetHud() / ShowPlayerKill() / IsWorldPointOccluded()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatHud | [`void OnEnable()`](../Assets/Scripts/Features/Combat/CombatHud.cs#L51) | private |
| CombatHud | [`void OnDisable()`](../Assets/Scripts/Features/Combat/CombatHud.cs#L56) | private |
| CombatHud | [`void ResetHud()`](../Assets/Scripts/Features/Combat/CombatHud.cs#L70) | public |
| CombatHud | [`void ShowPlayerKill(string enemyName)`](../Assets/Scripts/Features/Combat/CombatHud.cs#L81) | public |
| CombatHud | [`void LateUpdate()`](../Assets/Scripts/Features/Combat/CombatHud.cs#L96) | private |
| CombatHud | [`bool IsOccluded(Vector3 target, ControlUnit unit)`](../Assets/Scripts/Features/Combat/CombatHud.cs#L149) | private |
| CombatHud | [`bool IsWorldPointOccluded(Camera camera, Vector3 target, Transform owner, RaycastHit[] hits)`](../Assets/Scripts/Features/Combat/CombatHud.cs#L154) | public static |
| CombatHud | [`void RefreshEnemies()`](../Assets/Scripts/Features/Combat/CombatHud.cs#L183) | private |
| CombatHud | [`EnemyView CreateView()`](../Assets/Scripts/Features/Combat/CombatHud.cs#L237) | private |
| CombatHud | [`void Recycle(EnemyView view)`](../Assets/Scripts/Features/Combat/CombatHud.cs#L254) | private |

## Assets/Scripts/Features/Combat/DestroyManager.cs

// 职责：协调销毁主链、资源释放与驾驶舱死亡；配置并调用爆炸规则及残骸清理会话。 // 入口：DestroyGameObject() / ExplodeBlock() / ScheduleUnitCleanup()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DestroyManager | [`void Awake()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L25) | private |
| DestroyManager | [`void DestroyGameObject(GameObject obj)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L30) | public |
| DestroyManager | [`void ExplodeBlock(Block block)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L131) | public |
| DestroyManager | [`void ScheduleUnitCleanup(ControlUnit unit)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L168) | public |
| DestroyManager | [`void ScheduleDistantGroupCleanup(ControlUnit unit, Transform reference, float maxDistance)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L173) | public |
| DestroyManager | [`void EndSalvageSession()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L178) | public |
| DestroyManager | [`void NotifyObjectDestroyed()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L186) | public |
| DestroyManager | [`void ScheduleRefresh()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L192) | private |
| DestroyManager | [`IEnumerator DelayedRefresh()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L203) | private |
| DestroyManager | [`void ExecuteRefresh()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L210) | private |

## Assets/Scripts/Features/Combat/Durability.cs

// 职责：管理当前/最大耐久、损伤反馈及销毁通知。 // 入口：ApplyDamage() / ApplyCollisionEnergy() / ProtectFromExplosionCollisions()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Durability | [`void Awake()`](../Assets/Scripts/Features/Combat/Durability.cs#L44) | private |
| Durability | [`void OnEnable()`](../Assets/Scripts/Features/Combat/Durability.cs#L54) | private |
| Durability | [`void ApplyDamage(float amount, ControlUnit attacker)`](../Assets/Scripts/Features/Combat/Durability.cs#L63) | public |
| Durability | [`void ApplyCollisionEnergy(float reducedMass, float normalSpeed, float share)`](../Assets/Scripts/Features/Combat/Durability.cs#L74) | public |
| Durability | [`void ProtectFromExplosionCollisions(float duration)`](../Assets/Scripts/Features/Combat/Durability.cs#L100) | public |
| Durability | [`void UpdateDurablility(float value)`](../Assets/Scripts/Features/Combat/Durability.cs#L113) | public |
| Durability | [`void ChangeDurability(float value)`](../Assets/Scripts/Features/Combat/Durability.cs#L123) | private |
| Durability | [`void LateUpdate()`](../Assets/Scripts/Features/Combat/Durability.cs#L153) | private |
| Durability | [`void OnGUI()`](../Assets/Scripts/Features/Combat/Durability.cs#L172) | private |
| Durability | [`bool ShouldShowDebugLabel()`](../Assets/Scripts/Features/Combat/Durability.cs#L190) | private |

## Assets/Scripts/Features/Combat/EnemyController.cs

// 职责：为敌方控制单元选择目标并生成驾驶/攻击输入。 // 入口：FixedUpdate()；按固定周期更新目标和避障意图，逐物理步平滑输入。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyController | [`void Awake()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L32) | private |
| EnemyController | [`void FixedUpdate()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L37) | private |
| EnemyController | [`Vector3 CalculateDesiredMovement(Vector3 flatDirection)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L83) | private |
| EnemyController | [`ControlUnit FindNearestPlayer()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L108) | private |
| EnemyController | [`void ConfigureHoverThrusters()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L141) | private |
| EnemyController | [`Vector3 GetStrafeDirection(Vector3 flatDirection)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L180) | private |
| EnemyController | [`Vector3 ApplyObstacleAvoidance(Vector3 desiredMove, Vector3 targetDirection)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L191) | private |
| EnemyController | [`Vector3 ProbeObstacle(Vector3 direction, float weight)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L211) | private |
| RaycastHitDistanceComparer | [`int Compare(RaycastHit a, RaycastHit b)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L259) | public |

## Assets/Scripts/Features/Combat/EnemyIdentity.cs

// 职责：保存敌方蓝图身份和生成信息，供 HUD 与击杀反馈使用。 // 入口：SetDisplayName() / SetSpawnMaxHealth()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyIdentity | [`void SetDisplayName(string displayName)`](../Assets/Scripts/Features/Combat/EnemyIdentity.cs#L13) | public |
| EnemyIdentity | [`void SetSpawnMaxHealth(float maximum)`](../Assets/Scripts/Features/Combat/EnemyIdentity.cs#L18) | public |

## Assets/Scripts/Features/Combat/EnemySpawner.cs

// 职责：选择敌方蓝图并恢复敌方控制单元及出生状态。 // 入口：BeginPlayMode() / SpawnRandomEnemy() / SpawnEnemy()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemySpawner | [`void Start()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L21) | private |
| EnemySpawner | [`void Update()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L26) | private |
| EnemySpawner | [`void BeginPlayMode(Transform anchor)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L43) | public |
| EnemySpawner | [`void InitializeEnemyBlueprintPool()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L54) | private |
| EnemySpawner | [`void SpawnRandomEnemy()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L89) | public |
| EnemySpawner | [`void SpawnEnemy()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L112) | public |
| EnemySpawner | [`bool IsValidBlueprint(BlockDataList dataList, string enemyBlueprint)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L123) | private |
| EnemySpawner | [`ControlUnit SpawnBlockData(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L134) | private |
| EnemySpawner | [`void ApplyBlueprintLocalTransform(Transform blockTransform, Vector3 localPosition, Quaternion localRotation)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L232) | private |
| EnemySpawner | [`Vector3 CleanIntegerPosition(Vector3 position, string blockName)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L238) | private |
| EnemySpawner | [`Quaternion CleanRightAngleRotation(Quaternion rotation, string blockName)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L249) | private |
| EnemySpawner | [`float SnapNearInteger(float value)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L261) | private |
| EnemySpawner | [`float SnapNearRightAngle(float angle)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L267) | private |
| EnemySpawner | [`bool IsIntegerVector(Vector3 value)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L273) | private |
| EnemySpawner | [`bool IsRightAngleVector(Vector3 euler)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L278) | private |
| EnemySpawner | [`bool IsNearlyInteger(float value)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L283) | private |
| EnemySpawner | [`bool IsNearlyRightAngle(float angle)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L288) | private |
| EnemySpawner | [`Vector3 GetSpawnPosition()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L294) | private |
| EnemySpawner | [`void PrepareRuntimeEnemy(ControlUnit enemy)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L312) | private |

## Assets/Scripts/Features/Combat/ExplosionPropagation.cs

// 职责：在源单位内概率断链，并对受影响刚体施加有限冲量及双向碰撞保护。 // 入口：DisconnectBlocksInExplosionRange() / ApplyExplosionForce()；由 DestroyManager 分别在重组前后调用。 // 约束：爆炸不直接造成伤害；原始 ownerUnitId 用于查回重组后的同源碎片。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ExplosionPropagation | [`void DisconnectBlocksInExplosionRange(ControlUnit unit, Block block, Vector3 explosionPosition)`](../Assets/Scripts/Features/Combat/ExplosionPropagation.cs#L9) | internal static |
| ExplosionPropagation | [`void ApplyExplosionForce( Block block, string ownerUnitId, UnitFaction ownerFaction, Vector3 explosionPosition, float upwardsModifier, float impulseScale, float maximumSpeedChange, float collisionProtection)`](../Assets/Scripts/Features/Combat/ExplosionPropagation.cs#L53) | internal static |

## Assets/Scripts/Features/Combat/LaserTurret.cs

// 职责：瞬时激光命中与短时光束表现；不对目标施加冲量。 // 入口：Turret 固定步调用 Fire()；公用配置通过基类保持原序列化字段。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| LaserTurret | [`void Awake()`](../Assets/Scripts/Features/Combat/LaserTurret.cs#L13) | protected override |
| LaserTurret | [`void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)`](../Assets/Scripts/Features/Combat/LaserTurret.cs#L33) | protected override |
| LaserTurret | [`void HideShotFeedback()`](../Assets/Scripts/Features/Combat/LaserTurret.cs#L50) | protected override |
| LaserTurret | [`void UpdateShotFeedback()`](../Assets/Scripts/Features/Combat/LaserTurret.cs#L56) | protected override |

## Assets/Scripts/Features/Combat/Meteor.cs

// 职责：维护陨石刚体、飞行和碰撞相关行为。 // 入口：Start() / OnCollisionEnter()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Meteor | [`void Start()`](../Assets/Scripts/Features/Combat/Meteor.cs#L15) | private |
| Meteor | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Combat/Meteor.cs#L26) | private |

## Assets/Scripts/Features/Combat/MeteorShower.cs

// 职责：根据配置周期生成陨石并设置位置与运动参数。 // 入口：MeteorDestroyed()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MeteorShower | [`void Start()`](../Assets/Scripts/Features/Combat/MeteorShower.cs#L26) | private |
| MeteorShower | [`IEnumerator SpawnMeteors()`](../Assets/Scripts/Features/Combat/MeteorShower.cs#L31) | private |
| MeteorShower | [`void SpawnMeteor()`](../Assets/Scripts/Features/Combat/MeteorShower.cs#L45) | private |
| MeteorShower | [`void MeteorDestroyed()`](../Assets/Scripts/Features/Combat/MeteorShower.cs#L94) | public |

## Assets/Scripts/Features/Combat/ProjectileTurret.cs

// 职责：每炮管发射真实炮弹并播放后退/复位动画；炮弹负责飞行和命中。 // 入口：Turret.Fire()；炮管、速度、寿命和冲量可在 Prefab Inspector 配置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProjectileTurret | [`void Awake()`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L34) | protected override |
| ProjectileTurret | [`void LateUpdate()`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L57) | private |
| ProjectileTurret | [`void UpdateRecoil(float deltaTime)`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L66) | private |
| ProjectileTurret | [`void ApplyRecoilPose()`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L84) | private |
| ProjectileTurret | [`void OnDisable()`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L105) | protected override |
| ProjectileTurret | [`void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L113) | protected override |

## Assets/Scripts/Features/Combat/SalvageCleanupSession.cs

// 职责：按原始单位 ID 去重延迟清理；分别校验整单位、无驾驶舱组和远离组。 // 入口：ScheduleUnit() / ScheduleGroup() / ScheduleDistantGroup()。 // 生命周期：协程由 DestroyManager 承载；结束会话先停止协程再 Clear()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SalvageCleanupSession | [`void ScheduleUnit(GameObject destroyedObject, string ownerUnitId, float fallbackDelay)`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L17) | internal |
| SalvageCleanupSession | [`void ScheduleGroup(ControlUnit unit, float fallbackDelay)`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L28) | internal |
| SalvageCleanupSession | [`void ScheduleDistantGroup(ControlUnit unit, Transform reference, float maxDistance, float fallbackDelay)`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L45) | internal |
| SalvageCleanupSession | [`void Clear()`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L62) | internal |
| SalvageCleanupSession | [`bool TrySchedule(string ownerUnitId)`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L67) | private |
| SalvageCleanupSession | [`float GetCleanupDelay(float fallbackDelay)`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L72) | private static |
| SalvageCleanupSession | [`IEnumerator CleanupDistantGroupAfterDelay( ControlUnit unit, string ownerUnitId, Transform reference, float maxDistanceSqr, float fallbackDelay)`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L77) | private |
| SalvageCleanupSession | [`IEnumerator CleanupGroupAfterDelay(ControlUnit unit, string ownerUnitId, float fallbackDelay)`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L94) | private |
| SalvageCleanupSession | [`IEnumerator CleanupUnitAfterDelay(string ownerUnitId, float fallbackDelay)`](../Assets/Scripts/Features/Combat/SalvageCleanupSession.cs#L106) | private |

## Assets/Scripts/Features/Combat/Turret.cs

// 职责：共享炮塔启动、独立索敌、功率请求和双轴瞄准；武器种类只实现发射与表现。 // 入口：SetStarted() / FixedUpdate()；禁用和退出运行时撤销功率需求。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Turret | [`void Awake()`](../Assets/Scripts/Features/Combat/Turret.cs#L44) | protected virtual |
| Turret | [`void OnValidate()`](../Assets/Scripts/Features/Combat/Turret.cs#L51) | protected virtual |
| Turret | [`void SetStarted(bool started)`](../Assets/Scripts/Features/Combat/Turret.cs#L62) | public |
| Turret | [`void FixedUpdate()`](../Assets/Scripts/Features/Combat/Turret.cs#L71) | protected virtual |
| Turret | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Turret.cs#L93) | protected virtual |
| Turret | [`void StopWorking()`](../Assets/Scripts/Features/Combat/Turret.cs#L98) | private |
| Turret | [`void RefreshTarget()`](../Assets/Scripts/Features/Combat/Turret.cs#L107) | private |
| Turret | [`void AimAndFire(Durability aimTarget)`](../Assets/Scripts/Features/Combat/Turret.cs#L132) | private |
| Turret | [`void AimAt(Vector3 worldDirection)`](../Assets/Scripts/Features/Combat/Turret.cs#L151) | private |
| Turret | [`void ResolveAimingRig()`](../Assets/Scripts/Features/Combat/Turret.cs#L181) | private |
| Turret | [`Vector3 GetMuzzlePosition()`](../Assets/Scripts/Features/Combat/Turret.cs#L215) | protected |
| Turret | [`Vector3 GetAimForward()`](../Assets/Scripts/Features/Combat/Turret.cs#L226) | protected |
| Turret | [`UnitFaction GetEffectiveTargetFaction()`](../Assets/Scripts/Features/Combat/Turret.cs#L232) | protected |
| Turret | [`void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)`](../Assets/Scripts/Features/Combat/Turret.cs#L237) | protected abstract |
| Turret | [`void HideShotFeedback()`](../Assets/Scripts/Features/Combat/Turret.cs#L238) | protected virtual |
| Turret | [`void UpdateShotFeedback()`](../Assets/Scripts/Features/Combat/Turret.cs#L242) | protected virtual |

## Assets/Scripts/Features/Combat/TurretHitResolver.cs

// 职责：共享激光与实体炮弹的命中反馈、敌对伤害和可选炮弹冲量。 // 入口：Apply()；先施加炮弹冲量，确保拆组后的刚体继承命中速度。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretHitResolver | [`void Apply( Collider collider, Vector3 point, Vector3 normal, Vector3 direction, ControlUnit owner, UnitFaction faction, float damage, float impulse)`](../Assets/Scripts/Features/Combat/TurretHitResolver.cs#L7) | internal static |

## Assets/Scripts/Features/Combat/TurretHitscan.cs

// 职责：激光沿射线瞬时命中最近的非自身障碍，结算伤害但不施加冲量。 // 入口：Fire()；由 LaserTurret 调用并返回光束终点。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretHitscan | [`Vector3 Fire( Vector3 origin, Vector3 direction, ControlUnit owner, UnitFaction targetFaction, float range, LayerMask hitLayers, float damage)`](../Assets/Scripts/Features/Combat/TurretHitscan.cs#L10) | internal |
| RaycastHitDistanceComparer | [`int Compare(RaycastHit a, RaycastHit b)`](../Assets/Scripts/Features/Combat/TurretHitscan.cs#L59) | public |

## Assets/Scripts/Features/Combat/TurretProjectile.cs

// 职责：真实刚体炮弹飞行、每步扫掠防穿透、敌对伤害和寿命清理。 // 入口：Launch()；触发器避免物理引擎额外施加冲量，自身构造始终过滤。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretProjectile | [`void SetPool(TurretProjectilePool pool, TurretProjectile prefab)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L25) | internal |
| TurretProjectile | [`void OnDisable()`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L31) | private |
| TurretProjectile | [`void ClearIgnoredCollisions()`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L38) | private |
| TurretProjectile | [`void Awake()`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L51) | private |
| TurretProjectile | [`void Launch( ControlUnit owner, UnitFaction faction, float damage, float impulse, Vector3 velocity, float lifetime, float maxDistance, LayerMask hitLayers)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L63) | public |
| TurretProjectile | [`void FixedUpdate()`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L96) | private |
| TurretProjectile | [`bool ResolveOverlaps(Vector3 origin, float radius)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L165) | private |
| TurretProjectile | [`void OnTriggerEnter(Collider other)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L187) | private |
| TurretProjectile | [`bool CanHit(Collider collider)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L196) | private |
| TurretProjectile | [`void ResolveHit(Collider collider, Vector3 point, Vector3 normal)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L201) | private |
| TurretProjectile | [`void Retire()`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L222) | private |

## Assets/Scripts/Features/Combat/TurretProjectilePool.cs

// 职责：按炮弹 Prefab 复用运行实例，限制闲置数量并在模拟结束时清理。 // 入口：Rent() / Return()；炮弹仍独立执行原有物理与命中流程。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretProjectilePool | [`TurretProjectile Rent(TurretProjectile prefab, Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/Combat/TurretProjectilePool.cs#L11) | public static |
| TurretProjectilePool | [`void Return(TurretProjectile shot, TurretProjectile prefab)`](../Assets/Scripts/Features/Combat/TurretProjectilePool.cs#L44) | internal |
| TurretProjectilePool | [`void Update()`](../Assets/Scripts/Features/Combat/TurretProjectilePool.cs#L57) | private |
| TurretProjectilePool | [`void OnDestroy()`](../Assets/Scripts/Features/Combat/TurretProjectilePool.cs#L65) | private |

## Assets/Scripts/Features/Combat/TurretTargetSelector.cs

// 职责：从有效敌对组中选择范围内最近的伤害目标，支持玩家优先攻击敌方驾驶舱。 // 入口：FindNearestTarget()；由 Turret 按索敌周期调用，不依赖当前供电。 // 生命周期：每炮塔独立缓存模块引用；销毁引用在距离计算时跳过。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretTargetSelector | [`ControlUnit FindNearestTarget( IReadOnlyList<ControlUnit> units, ControlUnit owner, UnitFaction faction, float range, bool prioritizeCockpit, out Durability nearestDurability)`](../Assets/Scripts/Features/Combat/TurretTargetSelector.cs#L16) | internal |
| TurretTargetSelector | [`Durability FindNearestDurability(ControlUnit unit)`](../Assets/Scripts/Features/Combat/TurretTargetSelector.cs#L97) | private |
| TurretTargetSelector | [`Durability[] GetDurabilities(ControlUnit unit)`](../Assets/Scripts/Features/Combat/TurretTargetSelector.cs#L120) | private |
| TurretTargetSelector | [`Durability FindCockpitDurability(ControlUnit unit)`](../Assets/Scripts/Features/Combat/TurretTargetSelector.cs#L136) | private |

## Assets/Scripts/Features/Diagnostics/AbnormalImpulseBodyProbe.cs

// 职责：采集单个刚体的异常速度/冲量线索，供调试记录器分析。 // 入口：Awake() / OnCollisionEnter()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| AbnormalImpulseBodyProbe | [`void Awake()`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseBodyProbe.cs#L10) | private |
| AbnormalImpulseBodyProbe | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseBodyProbe.cs#L15) | private |

## Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs

// 职责：在编辑器调试时记录异常刚体冲量并输出诊断数据。 // 入口：RecordCollision()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| AbnormalImpulseRecorder | [`void StartSession()`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L29) | internal static |
| AbnormalImpulseRecorder | [`void RecordCollision(Rigidbody body, Collision collision)`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L41) | public static |
| AbnormalImpulseRecorder | [`void Awake()`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L59) | private |
| AbnormalImpulseRecorder | [`void FixedUpdate()`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L77) | private |
| AbnormalImpulseRecorder | [`void Record( Rigidbody body, string source, float impulse, float deltaVelocity, Vector3 point, string other)`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L135) | private |
| AbnormalImpulseRecorder | [`string Csv(string value)`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L190) | private static |
| AbnormalImpulseRecorder | [`string HierarchyPath(Transform node)`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L191) | private static |
| AbnormalImpulseRecorder | [`void OnDestroy()`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L203) | private |
| AbnormalImpulseRecorderBootstrap | [`void OnPlayModeChanged(PlayModeStateChange state)`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L221) | private static |
| AbnormalImpulseRecorderBootstrap | [`void EnsureSession()`](../Assets/Scripts/Features/Diagnostics/AbnormalImpulseRecorder.cs#L229) | private static |

## Assets/Scripts/Features/Diagnostics/BotNavigationProbeDriver.cs

// 职责：在探针物理帧调用生产导航并记录到达、碰撞和爬升，不影响真实机器人任务。 // 入口：BotNavigationPlayProbe.Initialize() 创建一次性驱动；只在 Editor Play 验证使用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationProbeDriver | [`void Initialize(string caseName, Transform target, Rigidbody body, Rigidbody home, Bot authored)`](../Assets/Scripts/Features/Diagnostics/BotNavigationProbeDriver.cs#L36) | public |
| BotNavigationProbeDriver | [`void FixedUpdate()`](../Assets/Scripts/Features/Diagnostics/BotNavigationProbeDriver.cs#L70) | private |
| BotNavigationProbeDriver | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Diagnostics/BotNavigationProbeDriver.cs#L97) | private |

## Assets/Scripts/Features/Diagnostics/DebugManager.cs

// 职责：保存调试显示配置和状态图标 Canvas；供电范围网格由 PowerRangeUnionView 拥有。 // 入口：GetPowerRangeColor() / TogglePowerRange() / TogglePowerConnections()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DebugManager | [`void Awake()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L43) | private |
| DebugManager | [`void OnDestroy()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L50) | private |
| DebugManager | [`Color GetPowerRangeColor(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L59) | public |
| DebugManager | [`void TogglePowerRange()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L74) | public |
| DebugManager | [`void TogglePowerConnections()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L87) | public |
| DebugManager | [`void ToggleConnectionStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L92) | public |
| DebugManager | [`void ToggleDurabilityStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L98) | public |
| DebugManager | [`void TogglePowerStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L104) | public |
| DebugManager | [`void EnsureStatusIconCanvas()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L110) | private |
| DebugManager | [`void RefreshPowerRangeMesh(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L132) | internal |
| DebugManager | [`void ClearPowerRangeMesh()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L137) | internal |

## Assets/Scripts/Features/Diagnostics/PerformanceMonitor.cs

// 职责：采集运行性能指标供调试展示。 // 入口：Start() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PerformanceMonitor | [`void Start()`](../Assets/Scripts/Features/Diagnostics/PerformanceMonitor.cs#L35) | private |
| PerformanceMonitor | [`void Update()`](../Assets/Scripts/Features/Diagnostics/PerformanceMonitor.cs#L59) | private |
| PerformanceMonitor | [`void UpdateCpuUsage()`](../Assets/Scripts/Features/Diagnostics/PerformanceMonitor.cs#L80) | private |
| PerformanceMonitor | [`void UpdateGpuFrameTime()`](../Assets/Scripts/Features/Diagnostics/PerformanceMonitor.cs#L89) | private |
| PerformanceMonitor | [`void OnGUI()`](../Assets/Scripts/Features/Diagnostics/PerformanceMonitor.cs#L103) | private |

## Assets/Scripts/Features/Diagnostics/PhysicsTelemetryHud.cs

// 职责：展示玩家单元的物理遥测与运行状态。 // 入口：OnEnable() / OnDisable() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PhysicsTelemetryHud | [`void OnEnable()`](../Assets/Scripts/Features/Diagnostics/PhysicsTelemetryHud.cs#L19) | private |
| PhysicsTelemetryHud | [`void OnDisable()`](../Assets/Scripts/Features/Diagnostics/PhysicsTelemetryHud.cs#L26) | private |
| PhysicsTelemetryHud | [`void OnImpact(float energy, float impulse)`](../Assets/Scripts/Features/Diagnostics/PhysicsTelemetryHud.cs#L31) | private |
| PhysicsTelemetryHud | [`void Update()`](../Assets/Scripts/Features/Diagnostics/PhysicsTelemetryHud.cs#L39) | private |

## Assets/Scripts/Features/Flight/HoverControlSolver.cs

// 职责：保存高度 PID 历史，计算高度需求、重力补偿、倾斜及旋转修正建议。 // 入口：StepHeight() / CalculateTilt() / TryCalculateAngularVelocity()；不读取组件或写刚体。 // 生命周期：停止输出及切换运行归属时 Reset()，不将旧积分带入新物理会话。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverControlSolver | [`void Reset()`](../Assets/Scripts/Features/Flight/HoverControlSolver.cs#L23) | internal |
| HoverControlSolver | [`float StepHeight( float heightError, float proportionalGain, float integralGain, float derivativeGain, float deltaTime)`](../Assets/Scripts/Features/Flight/HoverControlSolver.cs#L29) | internal |
| HoverControlSolver | [`float CalculateGravityCompensation( float mass, float gravityMagnitude, float compensationFactor, float absoluteHeightError, float heightTolerance)`](../Assets/Scripts/Features/Flight/HoverControlSolver.cs#L43) | internal static |
| HoverControlSolver | [`TiltCorrection CalculateTilt( Vector3 currentUp, Vector3 angularVelocity, float maximumTiltCos, float proportionalGain, float derivativeGain)`](../Assets/Scripts/Features/Flight/HoverControlSolver.cs#L55) | internal static |
| HoverControlSolver | [`bool TryCalculateAngularVelocity( Quaternion rotation, Vector3 currentUp, float rotationSmoothing, float deltaTime, float outputEfficiency, out Vector3 velocity)`](../Assets/Scripts/Features/Flight/HoverControlSolver.cs#L81) | internal static |

## Assets/Scripts/Features/Flight/HoverFlightController.cs

// 职责：装配悬浮参数和推进器缓存，在有效游玩归属下应用 HoverControlSolver 的控制输出。 // 入口：Init()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverFlightController | [`void Init()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L64) | public |
| HoverFlightController | [`void OnEnable()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L85) | private |
| HoverFlightController | [`void OnDisable()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L91) | private |
| HoverFlightController | [`void OnValidate()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L97) | private |
| HoverFlightController | [`void FixedUpdate()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L103) | private |
| HoverFlightController | [`bool HasActiveHoverThruster()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L157) | private |
| HoverFlightController | [`void StopControlOutput()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L170) | private |
| HoverFlightController | [`void ClearHoverThrust()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L177) | private |
| HoverFlightController | [`bool EnsureControllerReady()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L202) | private |
| HoverFlightController | [`void RefreshCachedPhysicsValues()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L243) | private |
| HoverFlightController | [`void RefreshTiltLimitCache()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L270) | private |
| HoverFlightController | [`void RebuildThrusterCache()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L276) | private |
| HoverFlightController | [`void DistributeThrust(float heightAdjustment, Vector3 currentUp)`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L348) | private |
| HoverFlightController | [`void ApplyRotationCorrection(Vector3 currentUp, float outputEfficiency)`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L389) | private |
| HoverFlightController | [`void OnDrawGizmosSelected()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L404) | private |

## Assets/Scripts/Features/Flight/HoverThruster.cs

// 职责：计算悬浮推进输出并应用高度/重力/姿态相关反馈。 // 入口：ApplyThrust() / ShouldActivate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverThruster | [`void SetControlledThrust(float desiredThrust)`](../Assets/Scripts/Features/Flight/HoverThruster.cs#L11) | public |
| HoverThruster | [`void FixedUpdate()`](../Assets/Scripts/Features/Flight/HoverThruster.cs#L16) | private |
| HoverThruster | [`void ApplyThrust()`](../Assets/Scripts/Features/Flight/HoverThruster.cs#L46) | public virtual |
| HoverThruster | [`bool ShouldActivate()`](../Assets/Scripts/Features/Flight/HoverThruster.cs#L57) | public override |

## Assets/Scripts/Features/Flight/MainThruster.cs

// 职责：根据主推进需求判断激活并提供推进方向。 // 入口：ApplyThrust() / ShouldActivate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainThruster | [`void FixedUpdate()`](../Assets/Scripts/Features/Flight/MainThruster.cs#L10) | private |
| MainThruster | [`void ApplyThrust()`](../Assets/Scripts/Features/Flight/MainThruster.cs#L32) | public |
| MainThruster | [`bool ShouldActivate()`](../Assets/Scripts/Features/Flight/MainThruster.cs#L43) | public override |

## Assets/Scripts/Features/Flight/Thruster.cs

// 职责：提供推进器共用的供电门控、运行归属和力学输出生命周期。 // 入口：ShouldActivate() / SetRuntimeReferences() / ApplyThrustChangeRateLimit()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Thruster | [`bool ShouldActivate()`](../Assets/Scripts/Features/Flight/Thruster.cs#L30) | public abstract |
| Thruster | [`void Awake()`](../Assets/Scripts/Features/Flight/Thruster.cs#L31) | protected virtual |
| Thruster | [`void Start()`](../Assets/Scripts/Features/Flight/Thruster.cs#L38) | protected virtual |
| Thruster | [`void OnTransformChildrenChanged()`](../Assets/Scripts/Features/Flight/Thruster.cs#L45) | private |
| Thruster | [`void SetRuntimeReferences(ControlUnit owner, Rigidbody ownerRigidbody)`](../Assets/Scripts/Features/Flight/Thruster.cs#L50) | public |
| Thruster | [`bool RefreshRuntimeReferences()`](../Assets/Scripts/Features/Flight/Thruster.cs#L57) | protected |
| Thruster | [`bool TryEnsureRigidbody()`](../Assets/Scripts/Features/Flight/Thruster.cs#L69) | protected |
| Thruster | [`bool IsPlayModeActive()`](../Assets/Scripts/Features/Flight/Thruster.cs#L79) | protected |
| Thruster | [`bool HasValidRuntimeOwner()`](../Assets/Scripts/Features/Flight/Thruster.cs#L84) | protected |
| Thruster | [`void CacheLocalReferences()`](../Assets/Scripts/Features/Flight/Thruster.cs#L117) | protected |
| Thruster | [`void ApplyThrustChangeRateLimit()`](../Assets/Scripts/Features/Flight/Thruster.cs#L126) | public virtual |
| Thruster | [`void SetPowerRequested(bool requested)`](../Assets/Scripts/Features/Flight/Thruster.cs#L136) | protected |
| Thruster | [`void OnDisable()`](../Assets/Scripts/Features/Flight/Thruster.cs#L146) | protected virtual |
| Thruster | [`bool CanApplyThrust()`](../Assets/Scripts/Features/Flight/Thruster.cs#L153) | protected |
| Thruster | [`float GetPowerEfficiency()`](../Assets/Scripts/Features/Flight/Thruster.cs#L159) | private |
| Thruster | [`void VisualizeThrust()`](../Assets/Scripts/Features/Flight/Thruster.cs#L169) | public virtual |
| Thruster | [`void VisualizeThrust(bool forceUpdate)`](../Assets/Scripts/Features/Flight/Thruster.cs#L174) | public virtual |
| Thruster | [`void DisableLegacyLineRenderer()`](../Assets/Scripts/Features/Flight/Thruster.cs#L212) | private |
| Thruster | [`void EnsureVisualEffect()`](../Assets/Scripts/Features/Flight/Thruster.cs#L221) | private |
| Thruster | [`Vector3 GetNormalizedThrustDirection()`](../Assets/Scripts/Features/Flight/Thruster.cs#L236) | private |

## Assets/Scripts/Features/Flight/ThrusterAllocator.cs

// 职责：把期望的力/力矩分配到单元推进器。 // 入口：Solve()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ThrusterAllocator | [`float[] Solve( Vector3[] forceDirs, // 每个推进器的力方向（单位向量，一般 t.transform.up） Vector3[] torqueDirs, // 每个推进器对单位推力产生的力矩方向 = r × dir float[] fMax, // 每个推进器的最大推力 Vector3 desiredForce, // 期望总力（世界坐标） Vector3 desiredTorque, // 期望总力矩（世界坐标） float forceWeight = 1f, // 力的权重（放大力误差在目标函数中的比重） float torqueWeight = 1f, // 力矩的权重 float damping = 1e-3f, // 阻尼λ（数值稳定） int maxIters = 8 // 主动集最大迭代次数 )`](../Assets/Scripts/Features/Flight/ThrusterAllocator.cs#L15) | public static |
| ThrusterAllocator | [`float[,] MultiplyAT_A(float[,] A, int rows, int cols)`](../Assets/Scripts/Features/Flight/ThrusterAllocator.cs#L192) | static |
| ThrusterAllocator | [`float[] MultiplyAT_b(float[,] A, int rows, int cols, float[] b)`](../Assets/Scripts/Features/Flight/ThrusterAllocator.cs#L213) | static |
| ThrusterAllocator | [`void AddDamping(float[,] H, int n, float lambda)`](../Assets/Scripts/Features/Flight/ThrusterAllocator.cs#L230) | private static |
| ThrusterAllocator | [`bool CholeskySolveInPlace(float[,] H, float[] rhs, int n)`](../Assets/Scripts/Features/Flight/ThrusterAllocator.cs#L242) | private static |

## Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs

// 职责：展示选中推进器的可编辑信息与参数。 // 入口：Toggle() / Close() / SelectCategory()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ThrusterInfoPanel | [`void Awake()`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L40) | private |
| ThrusterInfoPanel | [`void OnDisable()`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L54) | private |
| ThrusterInfoPanel | [`void Toggle()`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L62) | public |
| ThrusterInfoPanel | [`void Close()`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L77) | public |
| ThrusterInfoPanel | [`void SelectCategory(int category)`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L85) | public |
| ThrusterInfoPanel | [`void Update()`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L106) | private |
| ThrusterInfoPanel | [`bool MatchesCategory(Thruster thruster)`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L157) | private |
| ThrusterInfoPanel | [`Row CreateRow()`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L162) | private |
| ThrusterInfoPanel | [`void UpdateRow(Row row, Thruster thruster, int index)`](../Assets/Scripts/Features/Flight/ThrusterInfoPanel.cs#L176) | private |

## Assets/Scripts/Features/Flight/ThrusterVisualEffect.cs

// 职责：依据推进器激活和输出强度更新喷焰表现。 // 入口：Initialize() / SetThrust()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ThrusterVisualEffect | [`void Initialize(Thruster owner)`](../Assets/Scripts/Features/Flight/ThrusterVisualEffect.cs#L16) | public |
| ThrusterVisualEffect | [`void SetThrust(float thrustRatio, Vector3 thrustDirection)`](../Assets/Scripts/Features/Flight/ThrusterVisualEffect.cs#L21) | public |
| ThrusterVisualEffect | [`void LateUpdate()`](../Assets/Scripts/Features/Flight/ThrusterVisualEffect.cs#L27) | private |
| ThrusterVisualEffect | [`void OnDisable()`](../Assets/Scripts/Features/Flight/ThrusterVisualEffect.cs#L44) | private |

## Assets/Scripts/Features/Flight/UniversalThruster.cs

// 职责：根据控制输入确定通用推进器的激活与方向。 // 入口：ApplyThrust() / ShouldActivate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| UniversalThruster | [`void FixedUpdate()`](../Assets/Scripts/Features/Flight/UniversalThruster.cs#L10) | private |
| UniversalThruster | [`void RotateThruster(Vector3 worldDir, bool active)`](../Assets/Scripts/Features/Flight/UniversalThruster.cs#L32) | private |
| UniversalThruster | [`void ApplyThrust()`](../Assets/Scripts/Features/Flight/UniversalThruster.cs#L43) | public |
| UniversalThruster | [`bool ShouldActivate()`](../Assets/Scripts/Features/Flight/UniversalThruster.cs#L54) | public override |

## Assets/Scripts/Features/Player/InputManager.cs

// 职责：采样输入并协调建造/游玩模式、快捷键和光标状态。 // 入口：EnterBuildMode()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| InputManager | [`void Awake()`](../Assets/Scripts/Features/Player/InputManager.cs#L24) | private |
| InputManager | [`void Start()`](../Assets/Scripts/Features/Player/InputManager.cs#L29) | private |
| InputManager | [`void Update()`](../Assets/Scripts/Features/Player/InputManager.cs#L40) | private |
| InputManager | [`void EnterBuildMode()`](../Assets/Scripts/Features/Player/InputManager.cs#L135) | public |
| InputManager | [`void ApplyCursorState(bool showCursor)`](../Assets/Scripts/Features/Player/InputManager.cs#L151) | private |

## Assets/Scripts/Features/Player/Player.cs

// 职责：保存玩家运行身份与控制对象关系。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Player/PlayerController.cs

// 职责：处理玩家移动/视角相关输入并更新玩家控制状态。 // 入口：Start() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PlayerController | [`void Start()`](../Assets/Scripts/Features/Player/PlayerController.cs#L8) | private |
| PlayerController | [`void Update()`](../Assets/Scripts/Features/Player/PlayerController.cs#L13) | private |

## Assets/Scripts/Features/Player/PlayerHealth.cs

// 职责：管理玩家生命、死亡与重生相关状态。 // 入口：Start() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PlayerHealth | [`void Start()`](../Assets/Scripts/Features/Player/PlayerHealth.cs#L8) | private |
| PlayerHealth | [`void Update()`](../Assets/Scripts/Features/Player/PlayerHealth.cs#L13) | private |

## Assets/Scripts/Features/Power/Power.cs

// 职责：记录各使用者的工作意图及当前供电，空闲负载不占功率，供电短缺提供效率。 // 入口：使用者先 SetWorkRequested()；PowerNetwork.Rebuild() 顺序调用 ResetPower() / ReceivePower() / CommitPowerStatus()。 // 生命周期：启用时登记负载；最后请求撤销或退出立即清零；独立电网累加供电但不超出需求。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Power | [`void OnEnable()`](../Assets/Scripts/Features/Power/Power.cs#L48) | private |
| Power | [`void OnDisable()`](../Assets/Scripts/Features/Power/Power.cs#L55) | private |
| Power | [`void LateUpdate()`](../Assets/Scripts/Features/Power/Power.cs#L64) | private |
| Power | [`void SetWorkRequested(Object requester, bool requested)`](../Assets/Scripts/Features/Power/Power.cs#L74) | public |
| Power | [`void ResetPower()`](../Assets/Scripts/Features/Power/Power.cs#L100) | public |
| Power | [`void ReceivePower(float suppliedPower)`](../Assets/Scripts/Features/Power/Power.cs#L106) | public |
| Power | [`void CommitPowerStatus()`](../Assets/Scripts/Features/Power/Power.cs#L111) | public |
| Power | [`bool IsDestroyedRequester(Object requester)`](../Assets/Scripts/Features/Power/Power.cs#L119) | private static |

## Assets/Scripts/Features/Power/PowerConnectionVisuals.cs

// 职责：复用 LineRenderer 绘制中继与发电机之间的虚线连接。 // 入口：Refresh() / Hide()；由所属 PowerTransmissionDevice 的生命周期调用。 // 边界：只读取电网结果，不计算供电；线对象由所属 GameObject 持有并随其销毁。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerConnectionVisuals | [`void Refresh(DebugManager settings)`](../Assets/Scripts/Features/Power/PowerConnectionVisuals.cs#L19) | public |
| PowerConnectionVisuals | [`void Hide()`](../Assets/Scripts/Features/Power/PowerConnectionVisuals.cs#L48) | public |
| PowerConnectionVisuals | [`void HideUnusedLines(int firstUnusedIndex)`](../Assets/Scripts/Features/Power/PowerConnectionVisuals.cs#L53) | private |
| PowerConnectionVisuals | [`void DrawConnection(int index, Vector3 end, Color color, DebugManager settings)`](../Assets/Scripts/Features/Power/PowerConnectionVisuals.cs#L64) | private |
| PowerConnectionVisuals | [`LineRenderer GetOrCreateLine(int index)`](../Assets/Scripts/Features/Power/PowerConnectionVisuals.cs#L92) | private |
| PowerConnectionVisuals | [`void ConfigureLine(LineRenderer line)`](../Assets/Scripts/Features/Power/PowerConnectionVisuals.cs#L115) | private static |
| PowerConnectionVisuals | [`Material GetDashedLineMaterial()`](../Assets/Scripts/Features/Power/PowerConnectionVisuals.cs#L127) | private static |
| PowerConnectionVisuals | [`Texture2D CreateDashTexture()`](../Assets/Scripts/Features/Power/PowerConnectionVisuals.cs#L149) | private static |

## Assets/Scripts/Features/Power/PowerGeneratingUnit.cs

// 职责：保存发电量并登记已启用的发电机，不承担电网遍历或负载分配。 // 入口：OnEnable() / OnDisable()；PowerNetwork.Rebuild() 读取 ActiveUnits 和 outputPower。 // 生命周期：退出时清除中继的反向引用，下一次电网重建排除该发电机。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerGeneratingUnit | [`void OnEnable()`](../Assets/Scripts/Features/Power/PowerGeneratingUnit.cs#L16) | private |
| PowerGeneratingUnit | [`void OnDisable()`](../Assets/Scripts/Features/Power/PowerGeneratingUnit.cs#L21) | private |

## Assets/Scripts/Features/Power/PowerNetwork.cs

// 职责：缓存无线供电拓扑，以空间索引收集负载并逐帧分配所需功率。 // 入口：Rebuild()；由 PowerTransmissionDevice 每帧装配参与者，精确位置/身份变化才重建拓扑。 // 边界：不调度 Unity 帧或绘制调试图；复用实例缓冲，在一次分配结束后提交负载状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerNetwork | [`void Rebuild( IEnumerable<PowerTransmissionDevice> devices, IEnumerable<PowerGeneratingUnit> generators, IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L50) | public |
| PowerNetwork | [`void Clear(IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L71) | public |
| PowerNetwork | [`void CollectActiveParticipants( IEnumerable<PowerTransmissionDevice> devices, IEnumerable<PowerGeneratingUnit> generators, IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L101) | private |
| PowerNetwork | [`void ResetConnections()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L134) | private |
| PowerNetwork | [`bool CaptureTopology()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L149) | private |
| PowerNetwork | [`void BuildConnections()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L206) | private |
| PowerNetwork | [`void ConnectGenerators(PowerTransmissionDevice device, Vector3 position)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L246) | private |
| PowerNetwork | [`void CacheConnectedComponents()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L261) | private |
| PowerNetwork | [`void PrepareLoads()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L291) | private |
| PowerNetwork | [`void DistributeNetworkPower()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L308) | private |
| PowerNetwork | [`void CollectConnectedComponent(PowerTransmissionDevice rootDevice)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L331) | private |
| PowerNetwork | [`void VisitDevices(List<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L355) | private |
| PowerNetwork | [`void CollectDeviceLoads(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L366) | private |
| PowerNetwork | [`void SupplyConnectedComponent()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L382) | private |
| PowerNetwork | [`void CommitPowerStatuses()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L419) | private |
| PowerNetwork | [`bool IsWithinBoxRange(Vector3 firstPosition, Vector3 secondPosition, float range)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L427) | private static |

## Assets/Scripts/Features/Power/PowerNetworkBaseline.cs



| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerNetworkBaseline | [`void Rebuild( IEnumerable<PowerTransmissionDevice> devices, IEnumerable<PowerGeneratingUnit> generators, IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L23) | public |
| PowerNetworkBaseline | [`void Clear(IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L36) | public |
| PowerNetworkBaseline | [`void CollectActiveParticipants( IEnumerable<PowerTransmissionDevice> devices, IEnumerable<PowerGeneratingUnit> generators, IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L57) | private |
| PowerNetworkBaseline | [`void ResetConnectionsAndPower()`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L90) | private |
| PowerNetworkBaseline | [`void BuildConnections()`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L110) | private |
| PowerNetworkBaseline | [`void ConnectGenerators(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L130) | private |
| PowerNetworkBaseline | [`void DistributeNetworkPower()`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L142) | private |
| PowerNetworkBaseline | [`void CollectConnectedComponent(PowerTransmissionDevice rootDevice)`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L155) | private |
| PowerNetworkBaseline | [`void VisitDevices(List<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L180) | private |
| PowerNetworkBaseline | [`void CollectDeviceLoads(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L191) | private |
| PowerNetworkBaseline | [`void SupplyConnectedComponent()`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L202) | private |
| PowerNetworkBaseline | [`void CommitPowerStatuses()`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L239) | private |
| PowerNetworkBaseline | [`bool IsWithinBoxRange(Vector3 firstPosition, Vector3 secondPosition, float range)`](../Assets/Scripts/Features/Power/PowerNetworkBaseline.cs#L247) | private static |

## Assets/Scripts/Features/Power/PowerPositionIndex.cs

// 职责：按世界坐标网格索引供电参与者，复用桶与候选列表以减少全体范围扫描。 // 入口：Clear() / Add() / Query()；候选仍须由调用者执行精确盒边界检查。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerPositionIndex | [`void Clear()`](../Assets/Scripts/Features/Power/PowerPositionIndex.cs#L12) | internal |
| PowerPositionIndex | [`void Add(Vector3 position, int index)`](../Assets/Scripts/Features/Power/PowerPositionIndex.cs#L24) | internal |
| PowerPositionIndex | [`void Query(Vector3 position, float range, List<int> result)`](../Assets/Scripts/Features/Power/PowerPositionIndex.cs#L37) | internal |
| PowerPositionIndex | [`Vector3Int GetCell(Vector3 position)`](../Assets/Scripts/Features/Power/PowerPositionIndex.cs#L70) | private static |

## Assets/Scripts/Features/Power/PowerRangeUnionView.cs

// 职责：构建供电范围盒的并集表面，缓存网格签名并拥有临时网格/材质资源。 // 入口：Refresh() / Clear() / Hide() / Dispose()；由 DebugManager 管理生命周期。 // 边界：只读取供电结果；不创建状态图标 Canvas，不处理玩法或全局调试开关。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerRangeUnionView | [`void Refresh( IList<PowerTransmissionDevice> devices, bool show, Color isolatedColor, Color connectedColor, Color poweredColor)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L36) | internal |
| PowerRangeUnionView | [`void Invalidate()`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L61) | internal |
| PowerRangeUnionView | [`void Hide()`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L66) | internal |
| PowerRangeUnionView | [`void Clear()`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L71) | internal |
| PowerRangeUnionView | [`void Dispose()`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L79) | internal |
| PowerRangeUnionView | [`void EnsurePowerRangeObject()`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L100) | private |
| PowerRangeUnionView | [`void BuildPowerRangeMesh(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L124) | private |
| PowerRangeUnionView | [`void CollectCoordinates(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L226) | private |
| PowerRangeUnionView | [`void AddXFace(int x, int y, int z, bool positive, int materialIndex)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L254) | private |
| PowerRangeUnionView | [`void AddYFace(int x, int y, int z, bool positive, int materialIndex)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L266) | private |
| PowerRangeUnionView | [`void AddZFace(int x, int y, int z, bool positive, int materialIndex)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L278) | private |
| PowerRangeUnionView | [`void AddQuad( Vector3 a, Vector3 b, Vector3 c, Vector3 d, int materialIndex, bool reverse)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L290) | private |
| PowerRangeUnionView | [`void EnsurePowerRangeMaterials( IList<PowerTransmissionDevice> devices, Color isolatedRangeColor, Color connectedRangeColor, Color poweredRangeColor)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L325) | private |
| PowerRangeUnionView | [`int CalculatePowerRangeSignature(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L381) | private static |
| PowerRangeUnionView | [`int GetPowerState(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L405) | private static |
| PowerRangeUnionView | [`int FindCoordinateIndex(List<float> values, float target)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L415) | private static |
| PowerRangeUnionView | [`void SortAndUnique(List<float> values)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L432) | private static |
| PowerRangeUnionView | [`void SetPowerRangeMeshVisible(bool visible)`](../Assets/Scripts/Features/Power/PowerRangeUnionView.cs#L444) | private |

## Assets/Scripts/Features/Power/PowerTransmissionDevice.cs

// 职责：保存中继参数，登记生命周期并装配每帧一次的无线电网重建。 // 入口：Update() → PowerNetwork.Rebuild()；调试连接由 PowerConnectionVisuals 独立绘制。 // 生命周期：退出时移除双向连接；最后一个中继退出立即清除全体负载供电。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerTransmissionDevice | [`void Awake()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L40) | private |
| PowerTransmissionDevice | [`void OnValidate()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L45) | private |
| PowerTransmissionDevice | [`void OnEnable()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L50) | private |
| PowerTransmissionDevice | [`void Update()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L61) | private |
| PowerTransmissionDevice | [`void OnDisable()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L74) | private |
| PowerTransmissionDevice | [`void Disconnect()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L92) | private |
| PowerTransmissionDevice | [`void CacheDebugReferences()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L115) | private |
| PowerTransmissionDevice | [`void UpdateDebugVisuals()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L129) | private |
| PowerTransmissionDevice | [`void OnDrawGizmosSelected()`](../Assets/Scripts/Features/Power/PowerTransmissionDevice.cs#L142) | private |

## Assets/Scripts/Features/Presentation/BeamFade.cs

// 职责：按寿命衰减能量线并销毁临时对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BeamFade | [`void Initialize(StylizedBeamEffect effect, Color lineColor, float lifetime)`](../Assets/Scripts/Features/Presentation/BeamFade.cs#L10) | public |
| BeamFade | [`void Update()`](../Assets/Scripts/Features/Presentation/BeamFade.cs#L17) | private |

## Assets/Scripts/Features/Presentation/BlockStatusLight.cs

// 职责：将设备供电、机器人任务与方块状态映射为状态灯表现。 // 入口：Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockStatusLight | [`void Update()`](../Assets/Scripts/Features/Presentation/BlockStatusLight.cs#L20) | private |

## Assets/Scripts/Features/Presentation/BlockVfxLibrary.cs

// 职责：管理可编辑 VFX 资源和效果生成/回收预算，供玩法调用。 // 入口：Play()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockVfxLibrary | [`SpawnResult Play(Effect kind, Vector3 position, Quaternion rotation, float scale = 1f)`](../Assets/Scripts/Features/Presentation/BlockVfxLibrary.cs#L54) | public static |

## Assets/Scripts/Features/Presentation/CameraController.cs

// 职责：处理自由镜头、目标聚焦、平滑移动及加载环绕运动。 // 入口：FocusCameraOnBlock() / SmoothFocusCameraOnBlock() / SmoothFocusCameraOnBlockFramedBy()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CameraController | [`void Awake()`](../Assets/Scripts/Features/Presentation/CameraController.cs#L44) | private |
| CameraController | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/CameraController.cs#L49) | private |
| CameraController | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/CameraController.cs#L54) | private |
| CameraController | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/CameraController.cs#L63) | private |
| CameraController | [`void HandleLook()`](../Assets/Scripts/Features/Presentation/CameraController.cs#L72) | private |
| CameraController | [`Vector3 ResolveThirdPersonCollision(Vector3 lookTarget, Vector3 desiredPosition)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L126) | private |
| CameraController | [`bool IsPlayerCollider(Collider collider)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L177) | private |
| CameraController | [`void HandleMovement()`](../Assets/Scripts/Features/Presentation/CameraController.cs#L182) | private |
| CameraController | [`void FocusCameraOnBlock(GameObject obj)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L202) | public |
| CameraController | [`void SmoothFocusCameraOnBlock(GameObject obj, float duration)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L225) | public |
| CameraController | [`void SmoothFocusCameraOnBlockFramedBy(GameObject lookObj, GameObject frameObj, float duration)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L245) | public |
| CameraController | [`void SmoothOrbitCameraAroundBlock(GameObject frameObj, float yawDegrees, float pitchDegrees, float duration)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L275) | public |
| CameraController | [`void SmoothOrbitCameraAroundBlock(GameObject frameObj, float yawDegrees, float pitchDegrees, float radiusMultiplier, float duration)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L280) | public |
| CameraController | [`void SmoothOrbitCameraAroundBlock( GameObject frameObj, Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radiusMultiplier, float duration)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L301) | public |
| CameraController | [`void StartContinuousOrbitCameraAroundBlock( GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L329) | public |
| CameraController | [`void StartContinuousOrbitCameraAroundBounds( Bounds frameBounds, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L360) | public |
| CameraController | [`void StopCameraMotion()`](../Assets/Scripts/Features/Presentation/CameraController.cs#L391) | public |
| CameraController | [`IEnumerator SmoothFocusRoutine(Vector3 targetPosition, Quaternion targetRotation, float duration)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L402) | private |
| CameraController | [`IEnumerator ContinuousOrbitRoutine( GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L430) | private |
| CameraController | [`IEnumerator ContinuousOrbitRoutine( Bounds frameBounds, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L471) | private |
| CameraController | [`float CalculateOrbitRadiusMultiplier(float yawDegrees, float radiusVariation, float radiusWaveDegrees)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L506) | private |
| CameraController | [`IEnumerator SmoothOrbitRoutine(Vector3 orbitCenter, float targetYaw, float targetPitch, float targetRadius, float duration)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L518) | private |
| CameraController | [`void SetOrbitPose(Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radius)`](../Assets/Scripts/Features/Presentation/CameraController.cs#L559) | private |

## Assets/Scripts/Features/Presentation/CameraFraming.cs

// 职责：计算目标包围盒、视锥取景距离及环绕姿态，不处理输入或协程。 // 入口：TryGetFocusPose() / CalculateFramingDistance()；镜头和网格尺寸由调用者提供。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CameraFraming | [`Vector3 GetOrbitCameraDirection(float yawDegrees, float pitchDegrees)`](../Assets/Scripts/Features/Presentation/CameraFraming.cs#L18) | public |
| CameraFraming | [`bool TryGetFocusPose(GameObject obj, out Vector3 targetPosition, out Quaternion targetRotation)`](../Assets/Scripts/Features/Presentation/CameraFraming.cs#L26) | public |
| CameraFraming | [`bool TryGetFocusPose(Bounds frameBounds, Vector3 lookPoint, out Vector3 targetPosition, out Quaternion targetRotation)`](../Assets/Scripts/Features/Presentation/CameraFraming.cs#L38) | public |
| CameraFraming | [`bool TryGetOrbitPose( Bounds frameBounds, float yawDegrees, float pitchDegrees, float radiusMultiplier, out Vector3 targetPosition, out Quaternion targetRotation)`](../Assets/Scripts/Features/Presentation/CameraFraming.cs#L61) | public |
| CameraFraming | [`float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint)`](../Assets/Scripts/Features/Presentation/CameraFraming.cs#L89) | public |
| CameraFraming | [`float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint, Vector3 cameraDirection)`](../Assets/Scripts/Features/Presentation/CameraFraming.cs#L94) | public |
| CameraFraming | [`bool TryCalculateBlockBounds(GameObject obj, out Bounds bounds)`](../Assets/Scripts/Features/Presentation/CameraFraming.cs#L139) | public |

## Assets/Scripts/Features/Presentation/CameraShake.cs

// 职责：管理叠加镜头偏移及协程，取消或销毁宿主时移除残留偏移。 // 入口：Play() / Cancel()；显式注入宿主与目标 Camera。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CameraShake | [`void Cancel()`](../Assets/Scripts/Features/Presentation/CameraShake.cs#L17) | public |
| CameraShake | [`void Play(Camera camera, float amplitude, float duration)`](../Assets/Scripts/Features/Presentation/CameraShake.cs#L29) | public |
| CameraShake | [`IEnumerator CameraShakeRoutine(float amplitude, float duration)`](../Assets/Scripts/Features/Presentation/CameraShake.cs#L57) | private |
| CameraShake | [`void ClearCameraOffset()`](../Assets/Scripts/Features/Presentation/CameraShake.cs#L76) | private |

## Assets/Scripts/Features/Presentation/CargoHoldView.cs

// 职责：根据货仓内容刷新可见货物，不参与库存存档或结算。 // 入口：Awake()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoHoldView | [`void Awake()`](../Assets/Scripts/Features/Presentation/CargoHoldView.cs#L27) | private |
| CargoHoldView | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/CargoHoldView.cs#L33) | private |

## Assets/Scripts/Features/Presentation/CargoVisual.cs

// 职责：为货物或掉落对象建立对应的视觉表示。 // 入口：Create()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoVisual | [`GameObject Create(string resourcePath, Transform parent, float size)`](../Assets/Scripts/Features/Presentation/CargoVisual.cs#L8) | public static |

## Assets/Scripts/Features/Presentation/DetachedPartSmokeTrail.cs

// 职责：管理断裂零件烟迹和停止后的尾烟生命周期。 // 入口：Attach()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DetachedPartSmokeTrail | [`void Attach(Rigidbody body, Vector3 worldAnchor, float effectIntensity)`](../Assets/Scripts/Features/Presentation/DetachedPartSmokeTrail.cs#L30) | public static |
| DetachedPartSmokeTrail | [`void Initialize(Rigidbody body, Vector3 worldAnchor, float effectIntensity)`](../Assets/Scripts/Features/Presentation/DetachedPartSmokeTrail.cs#L55) | private |
| DetachedPartSmokeTrail | [`void Refresh(Vector3 worldAnchor, float effectIntensity)`](../Assets/Scripts/Features/Presentation/DetachedPartSmokeTrail.cs#L74) | private |
| DetachedPartSmokeTrail | [`void Update()`](../Assets/Scripts/Features/Presentation/DetachedPartSmokeTrail.cs#L85) | private |
| DetachedPartSmokeTrail | [`void StopAndRelease()`](../Assets/Scripts/Features/Presentation/DetachedPartSmokeTrail.cs#L119) | private |
| DetachedPartSmokeTrail | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/DetachedPartSmokeTrail.cs#L141) | private |

## Assets/Scripts/Features/Presentation/GlobalTextStyler.cs

// 职责：统一自有界面的文字样式和辅助效果。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GlobalTextStyler | [`void CreateInstance()`](../Assets/Scripts/Features/Presentation/GlobalTextStyler.cs#L14) | private static |
| GlobalTextStyler | [`IEnumerator Start()`](../Assets/Scripts/Features/Presentation/GlobalTextStyler.cs#L22) | private |
| GlobalTextStyler | [`void ApplyToSceneTexts()`](../Assets/Scripts/Features/Presentation/GlobalTextStyler.cs#L31) | private static |

## Assets/Scripts/Features/Presentation/HoverControllerStatusLight.cs

// 职责：将悬浮控制状态映射为控制器灯光反馈。 // 入口：Awake() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverControllerStatusLight | [`void Awake()`](../Assets/Scripts/Features/Presentation/HoverControllerStatusLight.cs#L20) | private |
| HoverControllerStatusLight | [`void Update()`](../Assets/Scripts/Features/Presentation/HoverControllerStatusLight.cs#L26) | private |

## Assets/Scripts/Features/Presentation/IconManager.cs

// 职责：管理方块/资源图标查询，供建造与存档界面使用。 // 入口：Register() / Unregister()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IconManager | [`void Awake()`](../Assets/Scripts/Features/Presentation/IconManager.cs#L18) | private |
| IconManager | [`void Update()`](../Assets/Scripts/Features/Presentation/IconManager.cs#L23) | private |
| IconManager | [`void Register(CanvasGroup icon)`](../Assets/Scripts/Features/Presentation/IconManager.cs#L39) | public |
| IconManager | [`void Unregister(CanvasGroup icon)`](../Assets/Scripts/Features/Presentation/IconManager.cs#L47) | public |

## Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs

// 职责：为工业零件提供配置驱动的机械运动表现。 // 入口：Configure()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IndustrialPartMotion | [`void Awake()`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L30) | private |
| IndustrialPartMotion | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L35) | private |
| IndustrialPartMotion | [`void Update()`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L40) | private |
| IndustrialPartMotion | [`void Configure( Transform[] newSpinTargets, Vector3 newSpinAxis, float newDegreesPerSecond, Transform newBobTarget, float newBobAmplitude, float newBobFrequency, Renderer[] newGlowRenderers, Color newBaseEmission, float newEmissionPulse)`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L79) | public |
| IndustrialPartMotion | [`void CacheRuntimeState()`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L102) | private |

## Assets/Scripts/Features/Presentation/LightFade.cs

// 职责：按寿命衰减闪光并销毁临时对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| LightFade | [`void Initialize(Light light, float lifetime, float intensity)`](../Assets/Scripts/Features/Presentation/LightFade.cs#L10) | public |
| LightFade | [`void Update()`](../Assets/Scripts/Features/Presentation/LightFade.cs#L17) | private |

## Assets/Scripts/Features/Presentation/MainUIButtons.cs

// 职责：绑定主界面按钮到建造、游玩和存档动作。 // 入口：SetDefault() / SetMove() / SetRotate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainUIButtons | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L45) | private |
| MainUIButtons | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L50) | private |
| MainUIButtons | [`void Awake()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L58) | private |
| MainUIButtons | [`void OnValidate()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L63) | private |
| MainUIButtons | [`void Start()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L77) | private |
| MainUIButtons | [`void SetDefault()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L109) | public |
| MainUIButtons | [`void SetMove()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L115) | public |
| MainUIButtons | [`void SetRotate()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L121) | public |
| MainUIButtons | [`void RefreshBuildModeButtons(BuildType buildType)`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L127) | public |
| MainUIButtons | [`void SetBuildButtonColor(Button button, bool selected)`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L136) | private |
| MainUIButtons | [`void SetCurrentBlock(string fileName)`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L162) | public |
| MainUIButtons | [`void ShowPowerRange()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L169) | private |
| MainUIButtons | [`void ShowPowerConnections()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L183) | private |
| MainUIButtons | [`void ShowConnectionStatus()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L197) | private |
| MainUIButtons | [`void ShowDurabilityStatus()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L208) | private |
| MainUIButtons | [`void ShowPowerStatus()`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L219) | private |
| MainUIButtons | [`void SetDebugButtonState(Button button, bool enabled)`](../Assets/Scripts/Features/Presentation/MainUIButtons.cs#L230) | private static |

## Assets/Scripts/Features/Presentation/MainUIPanels.cs

// 职责：管理主面板、弹窗和模式切换时的过渡与交互状态。 // 入口：ShowCreatePanel() / ShowRenamePanel() / HideCreatePanel()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainUIPanels | [`void Awake()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L42) | private |
| MainUIPanels | [`void Update()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L48) | private |
| MainUIPanels | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L71) | private |
| MainUIPanels | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L76) | private |
| MainUIPanels | [`void ShowCreatePanel()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L85) | public |
| MainUIPanels | [`void ShowRenamePanel(string save)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L100) | public |
| MainUIPanels | [`void HideCreatePanel()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L115) | public |
| MainUIPanels | [`void SetInputPlaceholder(string text)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L125) | private |
| MainUIPanels | [`void ShowDeletePanel(string save)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L134) | public |
| MainUIPanels | [`void HideDeletePanel()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L150) | public |
| MainUIPanels | [`void OnConfirmCreate()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L162) | public |
| MainUIPanels | [`void OnConfirmDelete(string save)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L197) | private |
| MainUIPanels | [`void PlayStart()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L216) | public |
| MainUIPanels | [`void PlayEnd()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L231) | public |
| MainUIPanels | [`void PlayerDeath()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L245) | public |
| MainUIPanels | [`void EnterEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L273) | public |
| MainUIPanels | [`void ExitEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L279) | public |
| MainUIPanels | [`void UpdateHealthBar(GameObject obj, float currentHealth, float maxHealth)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L285) | public |
| MainUIPanels | [`void SetHealthBar(float currentHealth, float maxHealth)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L300) | private |
| MainUIPanels | [`void SetCockpitHealth(Cockpit cockpit)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L316) | private |

## Assets/Scripts/Features/Presentation/MeteorLightToken.cs

// 职责：统计仍存活的陨石灯，释放时归还灯光预算。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MeteorLightToken | [`void Awake()`](../Assets/Scripts/Features/Presentation/MeteorLightToken.cs#L12) | private |
| MeteorLightToken | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/MeteorLightToken.cs#L17) | private |

## Assets/Scripts/Features/Presentation/MeteorTrailFollower.cs

// 职责：让独立尾迹跟随陨石，陨石销毁后停止并释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MeteorTrailFollower | [`void Initialize(Transform meteor, VfxEffect effect)`](../Assets/Scripts/Features/Presentation/MeteorTrailFollower.cs#L9) | public |
| MeteorTrailFollower | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/MeteorTrailFollower.cs#L17) | private |
| MeteorTrailFollower | [`void Follow()`](../Assets/Scripts/Features/Presentation/MeteorTrailFollower.cs#L35) | private |

## Assets/Scripts/Features/Presentation/PanelTransitions.cs

// 职责：控制面板淡入淡出、交互状态和取消；不参与游戏模式及存档决策。 // 入口：Show() / CancelAll()；由 MainUIPanels 注入协程宿主，禁用时取消。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PanelTransitions | [`void CancelAll()`](../Assets/Scripts/Features/Presentation/PanelTransitions.cs#L16) | public |
| PanelTransitions | [`void Show(GameObject panel, bool show, float duration)`](../Assets/Scripts/Features/Presentation/PanelTransitions.cs#L29) | public |
| PanelTransitions | [`IEnumerator Fade(GameObject panel, bool show, float duration)`](../Assets/Scripts/Features/Presentation/PanelTransitions.cs#L53) | private |
| PanelTransitions | [`void SetInteraction(GameObject panel, bool interactable)`](../Assets/Scripts/Features/Presentation/PanelTransitions.cs#L93) | public static |

## Assets/Scripts/Features/Presentation/StatusIcon.cs

// 职责：显示方块状态图标与参数提示，管理提示对象的可见性。 // 入口：CheckConnectionStatus() / CheckDurabilityStatus() / CheckPowerStatus()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| StatusIcon | [`void Awake()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L39) | private |
| StatusIcon | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L47) | private |
| StatusIcon | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L56) | private |
| StatusIcon | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L74) | private |
| StatusIcon | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L80) | private |
| StatusIcon | [`void CheckConnectionStatus()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L88) | public |
| StatusIcon | [`void CheckDurabilityStatus()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L111) | public |
| StatusIcon | [`void CheckPowerStatus()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L138) | public |
| StatusIcon | [`void SetConnectionStatus(ConnectionStatus nextStatus)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L165) | private |
| StatusIcon | [`void RefreshStatusIcons()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L176) | public |
| StatusIcon | [`void RefreshAllStatusIcons()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L186) | public static |
| StatusIcon | [`void RefreshIcons(DebugManager manager)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L197) | private |
| StatusIcon | [`void EnsureIconVisuals(DebugManager manager)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L223) | private |
| StatusIcon | [`Image CreateIcon(string iconName)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L252) | private |
| StatusIcon | [`bool SetIcon(Image image, string statusName)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L265) | private |
| StatusIcon | [`Sprite FindStatusIcon(string statusName)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L273) | private |
| StatusIcon | [`void PositionVisibleIcons(int visibleCount)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L292) | private |
| StatusIcon | [`void PositionIcon(Image image, ref float x)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L302) | private static |
| StatusIcon | [`void UpdateIconScreenPosition(DebugManager manager)`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L313) | private |
| StatusIcon | [`void HideIcons()`](../Assets/Scripts/Features/Presentation/StatusIcon.cs#L362) | private |

## Assets/Scripts/Features/Presentation/StylizedBeamEffect.cs

// 职责：更新能量/维修光束的起终点、材质与可见性。 // 入口：Configure() / SetEndpoints() / SetColor()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| StylizedBeamEffect | [`void Configure(float width)`](../Assets/Scripts/Features/Presentation/StylizedBeamEffect.cs#L16) | public |
| StylizedBeamEffect | [`void SetEndpoints(Vector3 start, Vector3 end)`](../Assets/Scripts/Features/Presentation/StylizedBeamEffect.cs#L21) | public |
| StylizedBeamEffect | [`void SetColor(Color color)`](../Assets/Scripts/Features/Presentation/StylizedBeamEffect.cs#L28) | public |
| StylizedBeamEffect | [`void SetIntensity(float value)`](../Assets/Scripts/Features/Presentation/StylizedBeamEffect.cs#L37) | public |
| StylizedBeamEffect | [`void SetVisible(bool value)`](../Assets/Scripts/Features/Presentation/StylizedBeamEffect.cs#L46) | public |
| StylizedBeamEffect | [`void UpdateBeam()`](../Assets/Scripts/Features/Presentation/StylizedBeamEffect.cs#L72) | private |
| StylizedBeamEffect | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/StylizedBeamEffect.cs#L94) | private |

## Assets/Scripts/Features/Presentation/TransientEffects.cs

// 职责：创建能量线、径向碎光与灯光闪烁，统一颜色及可见网格包围盒规则。 // 入口：CreateLineStreak() / CreateRadialStreakBurst() / CreateLightFlash()；无模式或场景单例依赖。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TransientEffects | [`void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L7) | public static |
| TransientEffects | [`void CreateRadialStreakBurst( Vector3 center, Vector3 normal, Color color, int count, float length, float duration, float width)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L18) | public static |
| TransientEffects | [`void CreateLightFlash(Vector3 position, Color color, float intensity, float range, float duration)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L47) | public static |
| TransientEffects | [`Bounds GetBounds(GameObject target, Vector3 fallbackCenter, Vector3 fallbackSize)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L60) | public static |
| TransientEffects | [`Vector3 GetBlockSize(Block block)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L86) | public static |
| TransientEffects | [`Color WithAlpha(Color color, float alpha)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L91) | public static |
| TransientEffects | [`Color Brighten(Color color, float intensity, float whiteBlend = 0f)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L97) | public static |

## Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs

// 职责：为按钮悬停、按下和焦点提供缩放反馈。 // 入口：OnPointerEnter() / OnPointerExit() / OnPointerDown()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| UIInteractionFeedback | [`void Awake()`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L20) | private |
| UIInteractionFeedback | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L25) | private |
| UIInteractionFeedback | [`void OnPointerEnter(PointerEventData eventData)`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L42) | public |
| UIInteractionFeedback | [`void OnPointerExit(PointerEventData eventData)`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L48) | public |
| UIInteractionFeedback | [`void OnPointerDown(PointerEventData eventData)`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L55) | public |
| UIInteractionFeedback | [`void OnPointerUp(PointerEventData eventData)`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L61) | public |
| UIInteractionFeedback | [`void OnSelect(BaseEventData eventData)`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L67) | public |
| UIInteractionFeedback | [`void OnDeselect(BaseEventData eventData)`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L73) | public |
| UIInteractionFeedback | [`void Animate()`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L79) | private |
| UIInteractionFeedback | [`IEnumerator AnimateScale()`](../Assets/Scripts/Features/Presentation/UIInteractionFeedback.cs#L89) | private |

## Assets/Scripts/Features/Presentation/VfxEffect.cs

// 职责：封装原生 VFX Graph 的播放、停止、参数与尾烟处理。 // 入口：SetIntensity() / SetColor() / SetSpawnCount()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxEffect | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L26) | private |
| VfxEffect | [`void SetIntensity(float value)`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L34) | public |
| VfxEffect | [`void SetColor(Color color)`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L70) | public |
| VfxEffect | [`void SetSpawnCount(float count)`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L81) | public |
| VfxEffect | [`void Clear()`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L92) | public |
| VfxEffect | [`void PlayOnce()`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L107) | public |
| VfxEffect | [`bool MakeRoomForBurst(bool priority)`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L113) | public static |
| VfxEffect | [`void ReleaseAfterPlayback(bool priority = false)`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L151) | public |
| VfxEffect | [`void StopAndRelease()`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L164) | public |
| VfxEffect | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L171) | private |
| VfxEffect | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/VfxEffect.cs#L172) | private |

## Assets/Scripts/Features/Presentation/VisualEffectsManager.cs

// 职责：为建造、维修、损伤、爆炸和能量反馈提供统一效果入口。 // 入口：EnsureInstance() / GetSharedLineMaterial() / TryPlayBlockPlaced()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VisualEffectsManager | [`VisualEffectsManager EnsureInstance()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L23) | public static |
| VisualEffectsManager | [`Material GetSharedLineMaterial()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L41) | public static |
| VisualEffectsManager | [`void TryPlayBlockPlaced(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L46) | public static |
| VisualEffectsManager | [`void TryPlayBlockRemoved(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L56) | public static |
| VisualEffectsManager | [`void TryPlayBlockExplosion(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L66) | public static |
| VisualEffectsManager | [`void TryPlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L76) | public static |
| VisualEffectsManager | [`void TryAttachDetachedPartSmoke(Rigidbody body, Vector3 worldAnchor, float intensity)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L81) | public static |
| VisualEffectsManager | [`void TryPlayObjectDestroyed(GameObject target)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L97) | public static |
| VisualEffectsManager | [`void TryPlayBlockMoved(Block block, Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L107) | public static |
| VisualEffectsManager | [`void TryPlayBlockRotated(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L117) | public static |
| VisualEffectsManager | [`void TryShowBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L127) | public static |
| VisualEffectsManager | [`void TryClearBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L137) | public static |
| VisualEffectsManager | [`void TryUpdateGhostPreview(GameObject ghost, bool isBlocked)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L147) | public static |
| VisualEffectsManager | [`void TryClearGhostPreview(GameObject ghost)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L157) | public static |
| VisualEffectsManager | [`void TryDecorateMeteor(Meteor meteor)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L167) | public static |
| VisualEffectsManager | [`void TryPlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L177) | public static |
| VisualEffectsManager | [`void Awake()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L182) | private |
| VisualEffectsManager | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L194) | private |
| VisualEffectsManager | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L199) | private |
| VisualEffectsManager | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L208) | private |
| VisualEffectsManager | [`void ApplySceneLook()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L213) | private |
| VisualEffectsManager | [`void PlayBlockPlaced(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L227) | private |
| VisualEffectsManager | [`void PlayBlockRemoved(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L251) | private |
| VisualEffectsManager | [`void PlayBlockExplosion(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L274) | private |
| VisualEffectsManager | [`void PlayObjectDestroyed(GameObject target)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L308) | private |
| VisualEffectsManager | [`void PlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L340) | private |
| VisualEffectsManager | [`void PlayBlockMoved(Block block, Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L367) | private |
| VisualEffectsManager | [`void PlayBlockRotated(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L380) | private |
| VisualEffectsManager | [`void ShowBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L401) | private |
| VisualEffectsManager | [`void ClearBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L415) | private |
| VisualEffectsManager | [`void UpdateGhostPreview(Transform ghost, bool isBlocked)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L420) | private |
| VisualEffectsManager | [`void ClearGhostPreview()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L425) | private |
| VisualEffectsManager | [`void DecorateMeteor(Meteor meteor)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L429) | private |
| VisualEffectsManager | [`void PlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L470) | private |

## Assets/Scripts/Features/Salvage/CargoHold.cs

// 职责：管理货仓容量、内容增减、恢复和快照，不负责无人机导航。 // 入口：CanReceive() / Reserve() / ReleaseReservation()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoHold | [`void Awake()`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L56) | private |
| CargoHold | [`void OnValidate()`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L62) | private |
| CargoHold | [`void OnEnable()`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L68) | private |
| CargoHold | [`void OnDisable()`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L73) | private |
| CargoHold | [`void Update()`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L80) | private |
| CargoHold | [`void RefreshPowerRequest()`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L85) | private |
| CargoHold | [`bool CanReceive(CargoItem item, LootDrop reservation = null)`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L93) | public |
| CargoHold | [`bool Reserve(LootDrop drop)`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L109) | public |
| CargoHold | [`void ReleaseReservation(LootDrop drop)`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L122) | public |
| CargoHold | [`int Store(CargoItem item, LootDrop reservation = null)`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L128) | public |
| CargoHold | [`List<CargoItem> CaptureContents()`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L156) | public |
| CargoHold | [`void RestoreContents(List<CargoItem> items)`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L170) | public |
| CargoHold | [`void ReleaseContents(UnitFaction faction)`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L206) | public |
| CargoHold | [`CargoHold FindReceiver(CargoItem item, Vector3 position, ControlUnit owner = null, LootDrop reservation = null)`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L229) | public static |

## Assets/Scripts/Features/Salvage/CargoItem.cs

// 职责：定义回收物品类型、存储条目及货仓分配规则。 // 入口：Copy()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoItem | [`CargoItem Copy(int count = -1)`](../Assets/Scripts/Features/Salvage/CargoItem.cs#L20) | public |

## Assets/Scripts/Features/Salvage/CargoPersistence.cs

// 职责：在返回/死亡时持久化或清空玩家货仓，保持蓝图几何数据。 // 入口：SaveReturnCargo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoPersistence | [`bool SaveReturnCargo(bool survived)`](../Assets/Scripts/Features/Salvage/CargoPersistence.cs#L11) | public static |

## Assets/Scripts/Features/Salvage/CoinManager.cs

// 职责：保存和展示玩家货币状态，提供增减入口。 // 入口：Start() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CoinManager | [`void Start()`](../Assets/Scripts/Features/Salvage/CoinManager.cs#L8) | private |
| CoinManager | [`void Update()`](../Assets/Scripts/Features/Salvage/CoinManager.cs#L13) | private |

## Assets/Scripts/Features/Salvage/LootDrop.cs

// 职责：维护掉落物品身份、携带/认领、吸附入库，以及散落悬浮和文字遮挡表现。 // 入口：Spawn() / Initialize() / Claim()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| LootDrop | [`void OnEnable()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L64) | private |
| LootDrop | [`void OnDisable()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L69) | private |
| LootDrop | [`void Start()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L75) | private |
| LootDrop | [`LootDrop Spawn(CargoItem item, Vector3 position)`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L83) | public static |
| LootDrop | [`void Initialize(CargoItem item)`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L132) | public |
| LootDrop | [`void RefreshLabel()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L177) | private |
| LootDrop | [`bool Claim(CollectionBot bot, CargoHold destination)`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L188) | public |
| LootDrop | [`void Carry(Transform socket)`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L200) | public |
| LootDrop | [`void Release()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L214) | public |
| LootDrop | [`bool Deliver(CargoHold hold)`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L231) | public |
| LootDrop | [`void Update()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L261) | private |
| LootDrop | [`void LateUpdate()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L320) | private |
| LootDrop | [`void ResetDisplayPose()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L364) | private |
| LootDrop | [`void ClearSession()`](../Assets/Scripts/Features/Salvage/LootDrop.cs#L379) | public static |

## Assets/Scripts/Features/Salvage/SalvageSettings.cs

// 职责：提供回收物品、无人机、残骸和掉落的可编辑配置。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Salvage/WreckSalvage.cs

// 职责：处理残骸所有权和可回收内容，生成对应掉落。 // 入口：Convert() / ConvertGroup()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WreckSalvage | [`void Convert(Block block)`](../Assets/Scripts/Features/Salvage/WreckSalvage.cs#L10) | public static |
| WreckSalvage | [`void ConvertGroup(ControlUnit unit)`](../Assets/Scripts/Features/Salvage/WreckSalvage.cs#L62) | public static |

## Assets/Scripts/Features/Simulation/Block.cs

// 职责：保存方块尺寸、质量、连接面和编辑器配置；由 BlockConnections 维护运行时连接。 // 入口：GetDisplayInfo() / GetConnectorWorldPosition() / GetConnectorWorldNormal()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Block | [`void Awake()`](../Assets/Scripts/Features/Simulation/Block.cs#L50) | private |
| Block | [`void OnValidate()`](../Assets/Scripts/Features/Simulation/Block.cs#L71) | private |
| Block | [`void RefreshAuthoringData()`](../Assets/Scripts/Features/Simulation/Block.cs#L88) | public |
| Block | [`void RefreshRuntimeData(bool rebuildConnectors = false)`](../Assets/Scripts/Features/Simulation/Block.cs#L117) | public |
| Block | [`void RefreshMass()`](../Assets/Scripts/Features/Simulation/Block.cs#L145) | public |
| Block | [`void SetMassSettings(bool useCustomMass, float value)`](../Assets/Scripts/Features/Simulation/Block.cs#L158) | public |
| Block | [`void Reset()`](../Assets/Scripts/Features/Simulation/Block.cs#L166) | private |
| Block | [`void GenerateConnectionPoints()`](../Assets/Scripts/Features/Simulation/Block.cs#L171) | private |
| Block | [`string GetDisplayInfo()`](../Assets/Scripts/Features/Simulation/Block.cs#L241) | public |
| Block | [`string GenerateInfo()`](../Assets/Scripts/Features/Simulation/Block.cs#L246) | private |
| Block | [`void CreateConnectionPoint(ConnectType connectType, Vector3 localPos, Vector3 normal, int order)`](../Assets/Scripts/Features/Simulation/Block.cs#L292) | private |
| Block | [`Vector3 GetConnectorWorldPosition(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L306) | public |
| Block | [`Vector3 GetConnectorWorldNormal(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L311) | public |
| Block | [`bool IsConnectorAvailableForPlacement(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L316) | public |
| Block | [`void CheckConnection()`](../Assets/Scripts/Features/Simulation/Block.cs#L321) | public |
| Block | [`List<Block> Neighbors()`](../Assets/Scripts/Features/Simulation/Block.cs#L326) | public |
| Block | [`void CollectNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/Block.cs#L334) | internal |
| Block | [`void DisConnectAllConnectors(bool refreshNeighbors = true)`](../Assets/Scripts/Features/Simulation/Block.cs#L339) | public |
| Block | [`void RefreshConnectionStatus()`](../Assets/Scripts/Features/Simulation/Block.cs#L344) | internal |
| Block | [`bool IsBlockedGhost()`](../Assets/Scripts/Features/Simulation/Block.cs#L352) | public |
| Block | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Simulation/Block.cs#L376) | private |

## Assets/Scripts/Features/Simulation/BlockConnections.cs

// 职责：探测匹配连接面、维护双侧连接引用和邻接查询；与方块序列化配置分离。 // 入口：CheckConnection() / CollectNeighbors() / DisConnectAllConnectors()。 // 约束：批量图遍历自行统一同步物理；探测缓冲满时扩容，连接视觉不决定拓扑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockConnections | [`bool IsConnectorAvailableForPlacement(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L20) | internal |
| BlockConnections | [`void CheckConnection()`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L34) | internal |
| BlockConnections | [`void ReleaseExistingConnections()`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L83) | private |
| BlockConnections | [`void ReleaseConnectionPairWithoutVisual(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L99) | private |
| BlockConnections | [`void ReleaseConnectionReferences(GameObject connectorObject)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L113) | private |
| BlockConnections | [`Block FindBlockAcrossConnector(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L143) | private |
| BlockConnections | [`Connector FindMatchingConnector(Block otherBlock, Connector connector, bool requireAvailable = true)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L170) | private |
| BlockConnections | [`void ConnectTo(Connector connector, Block otherBlock, Connector otherConnector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L192) | private |
| BlockConnections | [`void ClearConnector(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L213) | private |
| BlockConnections | [`void CollectNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L232) | internal |
| BlockConnections | [`void DisConnectAllConnectors(bool refreshNeighbors = true)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L260) | internal |

## Assets/Scripts/Features/Simulation/BlockGroupManager.cs

// 职责：按方块连接关系分组并计算组中心，供游玩物理单元建立使用。 // 入口：GroupBlocks() / CalculateGroupCenter()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockGroupManager | [`List<List<Block>> GroupBlocks(List<Block> allBlocks)`](../Assets/Scripts/Features/Simulation/BlockGroupManager.cs#L9) | public static |
| BlockGroupManager | [`Vector3 CalculateGroupCenter(List<Block> group)`](../Assets/Scripts/Features/Simulation/BlockGroupManager.cs#L65) | public static |

## Assets/Scripts/Features/Simulation/Cockpit.cs

// 职责：保存驾驶舱所属阵营及继承的方块行为。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Simulation/ControlUnit.cs

// 职责：组织运行单元的方块、刚体、输入、总耐久与资源归属。 // 入口：ProtectExplosionContacts() / TryGetTotalDurability() / RefreshChildren()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ControlUnit | [`void ProtectExplosionContacts(float duration)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L24) | public |
| ControlUnit | [`bool TryGetTotalDurability(out float current, out float maximum)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L35) | public |
| ControlUnit | [`void Start()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L58) | private |
| ControlUnit | [`void Update()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L70) | private |
| ControlUnit | [`void RefreshChildren()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L117) | public |
| ControlUnit | [`void AssignRuntimeOwnershipToBlocks(bool overwriteExisting)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L167) | public |
| ControlUnit | [`void EnsureRuntimeUnitId()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L188) | public |
| ControlUnit | [`void PlayEnd()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L196) | public |
| ControlUnit | [`void SetMovementInput(Vector3 worldDirection)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L206) | public |
| ControlUnit | [`void SetTarget(Transform newTarget)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L211) | public |
| ControlUnit | [`Vector3 GetPlayerMovementInput()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L216) | private |
| ControlUnit | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L254) | private |
| ControlUnit | [`void OnDestroy()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L264) | private |
| RuntimeUnitMember | [`RuntimeUnitMember Ensure(GameObject obj, string unitId, UnitFaction faction)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L277) | public static |

## Assets/Scripts/Features/Simulation/GroupPhysicsSettings.cs

// 职责：传递重组刚体的阻尼、解穿透及继承速度限制，不拥有 Inspector 数据。 // 入口：由 PlayManager 从现有序列化字段构造，供 RuntimeGroupBuilder 使用。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Simulation/ImpactPhysics.cs

// 职责：集中计算碰撞/爆炸的力学与伤害反馈规则。 // 入口：ReducedMass() / DamageEnergy() / ClosestPoint()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ImpactPhysics | [`void ResetEvents()`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L10) | private static |
| ImpactPhysics | [`float ReducedMass(float mass, float otherMass, bool otherIsStatic)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L16) | public static |
| ImpactPhysics | [`float DamageEnergy(float reducedMass, float normalSpeed, float safeSpeed)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L23) | public static |
| ImpactPhysics | [`Vector3 ClosestPoint(Collider collider, Vector3 position)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L28) | public static |
| ImpactPhysics | [`void ReportPlayerImpact(float energy, float impulse)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L39) | public static |

## Assets/Scripts/Features/Simulation/ModularUnitValidator.cs

// 职责：检查模块单元组合与必要部件的有效性。 // 入口：TryGetSingleCockpit() / CountCockpits() / CountLoadedCockpits()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ModularUnitValidator | [`bool TryGetSingleCockpit(Component root, out Cockpit cockpit, out string reason)`](../Assets/Scripts/Features/Simulation/ModularUnitValidator.cs#L8) | public static |
| ModularUnitValidator | [`int CountCockpits(IEnumerable<Block> blocks)`](../Assets/Scripts/Features/Simulation/ModularUnitValidator.cs#L29) | public static |
| ModularUnitValidator | [`int CountLoadedCockpits()`](../Assets/Scripts/Features/Simulation/ModularUnitValidator.cs#L48) | public static |

## Assets/Scripts/Features/Simulation/PlayBlockSelection.cs

// 职责：保存并恢复选中方块的材质，维护选中视觉效果；不参与连接或组重建。 // 入口：Highlight() / Clear()；由 PlayManager 负责选中状态及 Rack 的玩法副作用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PlayBlockSelection | [`void Highlight(Block block, Material highlightMaterial)`](../Assets/Scripts/Features/Simulation/PlayBlockSelection.cs#L9) | internal |
| PlayBlockSelection | [`void Clear(Block block)`](../Assets/Scripts/Features/Simulation/PlayBlockSelection.cs#L37) | internal |

## Assets/Scripts/Features/Simulation/PlayManager.cs

// 职责：协调建造/游玩会话、组注册、选中表现及遥测；重组和清理规则由 Simulation 功能实现。 // 入口：PlayStart() / CanStartPlay() / RefreshGroup()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PlayManager | [`void Awake()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L51) | private |
| PlayManager | [`void FixedUpdate()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L56) | private |
| PlayManager | [`void PlayStart()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L84) | public |
| PlayManager | [`bool CanStartPlay(out string reason)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L113) | public |
| PlayManager | [`void RefreshGroup(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L148) | public |
| PlayManager | [`void SetPlayMode(bool lockView)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L175) | public |
| PlayManager | [`void CalculateVelocity()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L183) | private |
| PlayManager | [`void HandleSelection()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L192) | private |
| PlayManager | [`void SelectBlock(Block block)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L221) | private |
| PlayManager | [`void DeselectBlock()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L240) | public |
| PlayManager | [`void PlayEnd()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L246) | public |
| PlayManager | [`void AssignBlocksToParentGroups(List<Block> blocks)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L270) | public |
| PlayManager | [`void RegisterControlUnit(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L286) | public |
| PlayManager | [`void UnregisterControlUnit(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L300) | public |
| PlayManager | [`IReadOnlyList<ControlUnit> GetControlUnits()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L308) | public |
| PlayManager | [`EnemySpawner EnsureEnemySpawner()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L321) | private |

## Assets/Scripts/Features/Simulation/Rack.cs

// 职责：管理机器人停泊容器及所属单元关系。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs

// 职责：按连接分量建立运行组，迁移方块并恢复受限的源刚体运动。 // 入口：Rebuild()；由 PlayManager.AssignBlocksToParentGroups() 调用。 // 约束：先捕获源刚体；重组保留 RuntimeUnitMember 的原始单位归属。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RuntimeGroupBuilder | [`void Rebuild(List<Block> blocks, GroupPhysicsSettings settings)`](../Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs#L15) | internal |
| RuntimeGroupBuilder | [`Dictionary<Block, Rigidbody> CaptureSourceBodies(List<Block> blocks)`](../Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs#L64) | private static |
| RuntimeGroupBuilder | [`void RestoreGroupPhysics( Rigidbody targetBody, List<Block> group, Dictionary<Block, Rigidbody> sourceBodies, GroupPhysicsSettings settings)`](../Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs#L84) | private static |

## Assets/Scripts/Features/Simulation/RuntimeGroupCleanup.cs

// 职责：按无驾驶舱组上限和玩家距离安排残骸清理，不立即销毁或更改归属。 // 入口：EnforceLimit() / EnforceDistance()；由 PlayManager 在原有时序调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RuntimeGroupCleanup | [`void EnforceLimit(List<ControlUnit> units, int maximumUselessGroups, DestroyManager destruction)`](../Assets/Scripts/Features/Simulation/RuntimeGroupCleanup.cs#L8) | internal static |
| RuntimeGroupCleanup | [`void EnforceDistance( List<ControlUnit> units, bool playMode, Transform playerRoot, float cleanupDistance, DestroyManager destruction)`](../Assets/Scripts/Features/Simulation/RuntimeGroupCleanup.cs#L51) | internal static |

## Assets/Scripts/Features/Simulation/UnitFaction.cs

// 职责：定义阵营和运行单元归属，支持友敌判断与残骸追踪。

数据/配置/接口定义，无方法体。
