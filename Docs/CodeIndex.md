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
| ArchitecturePlayProbe | [`void PrepareIsolation()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L345) | private static |
| ArchitecturePlayProbe | [`void PauseSceneUpdates(bool stopStartupCoroutines)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L412) | private static |
| ArchitecturePlayProbe | [`void VerifyFileStore()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L424) | private static |
| ArchitecturePlayProbe | [`BlockData[] CreateOrdinaryBlueprint()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L461) | private static |
| ArchitecturePlayProbe | [`BlockData MakeBlock(string resourcePath, int index)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L490) | private static |
| ArchitecturePlayProbe | [`void VerifyRestoredComponents(BlockData[] data, UnitFaction faction)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L525) | private static |
| ArchitecturePlayProbe | [`string WriteBlueprint(string suffix, BlockData[] blocks, bool enemy = false)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L547) | private static |
| ArchitecturePlayProbe | [`void Load(string name, bool enemy = false, Action initialBlock = null)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L555) | private static |
| ArchitecturePlayProbe | [`Type RuntimeType(string name)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L582) | private static |
| ArchitecturePlayProbe | [`object Invoke(object target, string method, params object[] arguments)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L587) | private static |
| ArchitecturePlayProbe | [`object Field(object target, string field)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L592) | private static |
| ArchitecturePlayProbe | [`void SetField(object target, string field, object value)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L597) | private static |
| ArchitecturePlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L602) | private static |
| ArchitecturePlayProbe | [`void RestoreIsolation()`](../Assets/Editor/Architecture/ArchitecturePlayProbe.cs#L612) | private static |

## Assets/Editor/Architecture/HoverControlPlayChecks.cs

// 职责：以真实运行组件验证悬浮控制的有效归属、停供和禁用清理契约。 // 入口：Run()；由 ArchitecturePlayProbe 在暂停场景业务的 Play 会话中调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverControlPlayChecks | [`string[] Run()`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L11) | internal static |
| HoverControlPlayChecks | [`void Tick(HoverFlightController controller)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L142) | private static |
| HoverControlPlayChecks | [`void TickThruster(HoverThruster thruster)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L147) | private static |
| HoverControlPlayChecks | [`void CheckRampDown(List<string> checks, HoverThruster thruster, string reason)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L152) | private static |
| HoverControlPlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L175) | private static |

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

// 职责：隔离 Play 场景验证 Bot 过滤、墙面、U 形围挡、惯性制动和移动 Home 导航。 // 入口：Run()；结果保存在 Temp/BotNavigation/PlayValidation.json；不修改作者场景或资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationPlayProbe | [`void Run()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L31) | public static |
| BotNavigationPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L50) | private static |
| BotNavigationPlayProbe | [`void Capture(string condition, string stack, LogType type)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L101) | private static |
| BotNavigationPlayProbe | [`void Tick()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L109) | private static |
| BotNavigationPlayProbe | [`void TickCase()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L122) | private static |
| BotNavigationPlayProbe | [`void ValidateBotFiltering()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L198) | private static |
| BotNavigationPlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L324) | private static |
| BotNavigationPlayProbe | [`void ValidateFrozenNavigation()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L336) | private static |
| BotNavigationPlayProbe | [`void StartCase()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L362) | private static |
| BotNavigationPlayProbe | [`void Wall(Vector3 position, Vector3 scale)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L426) | private static |
| BotNavigationPlayProbe | [`void Finish()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L435) | private static |
| BotNavigationPlayProbe | [`void Restore()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L442) | private static |
| BotNavigationPlayProbe | [`void WriteReport()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L451) | private static |

## Assets/Editor/Bots/BotNavigationPolicyMigration.cs

// 职责：通过 Editor 重存 Bot Prefab，清理已固化的旧导航字段并核对剩余创作数据。 // 入口：Run() / Tools/Bots/Migrate fixed navigation policy；不保存场景、不重建 GUID。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationPolicyMigration | [`string Run()`](../Assets/Editor/Bots/BotNavigationPolicyMigration.cs#L12) | public static |
| BotNavigationPolicyMigration | [`string Snapshot(string path)`](../Assets/Editor/Bots/BotNavigationPolicyMigration.cs#L62) | private static |

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
| BlockCsvChecks | [`string RunPlayMode()`](../Assets/Editor/Building/BlockCsvChecks.cs#L200) | public static |
| BlockCsvChecks | [`GameObject Load(string name)`](../Assets/Editor/Building/BlockCsvChecks.cs#L250) | private static |
| BlockCsvChecks | [`void Set(List<List<string>> records, string name, string field, string value)`](../Assets/Editor/Building/BlockCsvChecks.cs#L255) | private static |
| BlockCsvChecks | [`void Write(List<List<string>> records)`](../Assets/Editor/Building/BlockCsvChecks.cs#L261) | private static |
| BlockCsvChecks | [`void Check(Report report, bool condition, string description)`](../Assets/Editor/Building/BlockCsvChecks.cs#L267) | private static |
| BlockCsvChecks | [`string SaveReport(Report report, string name)`](../Assets/Editor/Building/BlockCsvChecks.cs#L277) | private static |

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
| BlockCsvRegressionChecks | [`void CheckRuntimeState()`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L143) | private static |
| BlockCsvRegressionChecks | [`void Check(bool condition, string description)`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L169) | private static |
| BlockCsvRegressionChecks | [`void Finish()`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L179) | private static |
| BlockCsvRegressionChecks | [`void WriteReport()`](../Assets/Editor/Building/BlockCsvRegressionChecks.cs#L197) | private static |

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

## Assets/Editor/Building/BuildingEditorAssets.cs

// 职责：保存建筑底座、网格材质及尺寸编辑 uGUI 为真实可编辑资产，不写入 Main 场景。 // 入口：Bake() 菜单；已存在的 Prefab/材质优先保留，删除指定资产后可重新生成。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingEditorAssets | [`void Bake()`](../Assets/Editor/Building/BuildingEditorAssets.cs#L14) | public static |
| BuildingEditorAssets | [`Material CreateGridMaterial()`](../Assets/Editor/Building/BuildingEditorAssets.cs#L38) | private static |
| BuildingEditorAssets | [`void CreateFoundation(Material material)`](../Assets/Editor/Building/BuildingEditorAssets.cs#L79) | private static |
| BuildingEditorAssets | [`void CreatePanel()`](../Assets/Editor/Building/BuildingEditorAssets.cs#L110) | private static |
| BuildingEditorAssets | [`GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position)`](../Assets/Editor/Building/BuildingEditorAssets.cs#L176) | private static |
| BuildingEditorAssets | [`Text Label( string name, Transform parent, string value, Vector2 size, Vector2 position, int fontSize)`](../Assets/Editor/Building/BuildingEditorAssets.cs#L188) | private static |
| BuildingEditorAssets | [`InputField Input(string name, Transform parent, Vector2 position)`](../Assets/Editor/Building/BuildingEditorAssets.cs#L206) | private static |

## Assets/Editor/Building/BuildingEditorPlayProbe.cs

// 职责：在隔离存档的真实 Play Mode 检查建筑快捷键、底座连接、尺寸 UI 和跨模式持久化。 // 入口：Run()；只写 Temp/BuildingEditor，不改动用户存档或保存 Main 场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingEditorPlayProbe | [`void Run()`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L32) | public static |
| BuildingEditorPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L45) | private static |
| BuildingEditorPlayProbe | [`void Capture(string message, string stack, LogType type)`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L84) | private static |
| BuildingEditorPlayProbe | [`void Tick()`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L92) | private static |
| BuildingEditorPlayProbe | [`void Finish()`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L122) | private static |
| BuildingEditorPlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L149) | private static |
| BuildingEditorPlayProbe | [`void Field(object target, string name, object value)`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L159) | private static |
| BuildingEditorPlayProbe | [`object Field(object target, string name)`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L164) | private static |
| BuildingEditorPlayProbe | [`object CreateFileStore(string directory)`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L169) | private static |
| BuildingEditorPlayProbe | [`void Shortcut(params Key[] keys)`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L175) | private static |
| BuildingEditorPlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Building/BuildingEditorPlayProbe.cs#L186) | private static |

## Assets/Editor/Building/OutpostEditorAuthoring.cs

// 职责：为现有建筑编辑Prefab添加可编辑据点表单，保留底座尺寸控件与既有GUID。 // 入口：Upgrade()；通过PrefabUtility保存真实资产，重复执行不重复添加。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostEditorAuthoring | [`void Upgrade()`](../Assets/Editor/Building/OutpostEditorAuthoring.cs#L12) | public static |
| OutpostEditorAuthoring | [`void AddTemplateCopy(GameObject root, BuildingOutpostEditor editor)`](../Assets/Editor/Building/OutpostEditorAuthoring.cs#L106) | private static |
| OutpostEditorAuthoring | [`void AddMatchingBase(GameObject root, BuildingOutpostEditor editor)`](../Assets/Editor/Building/OutpostEditorAuthoring.cs#L139) | private static |
| OutpostEditorAuthoring | [`Dropdown Dropdown(Transform parent, string name, string caption, float y)`](../Assets/Editor/Building/OutpostEditorAuthoring.cs#L156) | private static |
| OutpostEditorAuthoring | [`Button Button(Transform parent, string name, string caption, float x)`](../Assets/Editor/Building/OutpostEditorAuthoring.cs#L164) | private static |
| OutpostEditorAuthoring | [`Text Label(Transform parent, string name, string caption, Vector2 size, Vector2 position)`](../Assets/Editor/Building/OutpostEditorAuthoring.cs#L172) | private static |
| OutpostEditorAuthoring | [`void Place(GameObject obj, Transform parent, string name, Vector2 size, Vector2 position)`](../Assets/Editor/Building/OutpostEditorAuthoring.cs#L185) | private static |

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

## Assets/Editor/Combat/EnemyBlueprintPoolChecks.cs

// 职责：隔离临时蓝图库，验证真实敌机池初始化遇损坏文件仍保留有效蓝图。 // 入口：Run()；同步检查后恢复SaveManager私有文件存储和名称列表，不写用户目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyBlueprintPoolChecks | [`string Run()`](../Assets/Editor/Combat/EnemyBlueprintPoolChecks.cs#L23) | public static |
| EnemyBlueprintPoolChecks | [`string ValidBlueprint()`](../Assets/Editor/Combat/EnemyBlueprintPoolChecks.cs#L131) | private static |
| EnemyBlueprintPoolChecks | [`void Check(Report report, string label, bool passed)`](../Assets/Editor/Combat/EnemyBlueprintPoolChecks.cs#L139) | private static |

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
| TurretRuntimeChecks | [`void PrepareWeapon(Turret turret, ControlUnit owner)`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L263) | private static |
| TurretRuntimeChecks | [`void CallFire(Turret turret, Vector3 origin, Vector3 direction)`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L271) | private static |
| TurretRuntimeChecks | [`ControlUnit Unit(Transform parent, string name, UnitFaction faction, Vector3 localPosition)`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L276) | private static |
| TurretRuntimeChecks | [`void Check(Report report, bool passed, string name)`](../Assets/Editor/Combat/TurretRuntimeChecks.cs#L293) | private static |

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

// 职责：隔离验证默认关闭镜头碰撞后的墙体行为和图标空闲/淡出/恢复分支。 // 入口：Run()；Play Mode 调用真实方法，不替代视觉手感验收。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PresentationPerformanceChecks | [`string[] Run()`](../Assets/Editor/Diagnostics/Benchmarks/PresentationPerformanceChecks.cs#L13) | public static |
| PresentationPerformanceChecks | [`void Set(StatusIcon icon, string name, object value)`](../Assets/Editor/Diagnostics/Benchmarks/PresentationPerformanceChecks.cs#L79) | private static |
| PresentationPerformanceChecks | [`void Check(bool condition, string name, List<string> checks)`](../Assets/Editor/Diagnostics/Benchmarks/PresentationPerformanceChecks.cs#L84) | private static |

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
| PhysicsSandboxProbe | [`float Collide( Scene scene, float mass, float speed, List<string> checks, bool checkContacts, float restitution = 0f, float tangentSpeed = 0f)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L188) | private static |
| PhysicsSandboxProbe | [`void CollideDynamicPair(Scene scene, List<string> checks)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L262) | private static |
| PhysicsSandboxProbe | [`Rigidbody Body(Scene scene, string name, float mass, Vector3 position)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L304) | private static |
| PhysicsSandboxProbe | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L317) | private static |
| PhysicsSandboxProbe | [`string Save(List<string> checks, string error)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L327) | private static |

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

## Assets/Editor/Presentation/ReturnStatusSetup.cs

// 职责：只在Main创建/绑定可编辑返航失败状态栏，不重排既有面板。 // 入口：Apply()；保存真实场景，保留已有状态栏的布局与样式。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ReturnStatusSetup | [`void Apply()`](../Assets/Editor/Presentation/ReturnStatusSetup.cs#L11) | public static |

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

## Assets/Editor/Salvage/CargoReturnFileChecks.cs

// 职责：在真实Main游玩中隔离蓝图文件仓，验证返航写盘失败保留会话、成功保留几何和备份。 // 入口：Run()；同步操作仅限唯一Temp目录，finally恢复文件仓/名称/缓存，不覆盖用户存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoReturnFileChecks | [`string Run()`](../Assets/Editor/Salvage/CargoReturnFileChecks.cs#L19) | public static |
| CargoReturnFileChecks | [`void ExpectRetained(Report report, string label, Transform player, WorldSessionManager session)`](../Assets/Editor/Salvage/CargoReturnFileChecks.cs#L101) | private static |
| CargoReturnFileChecks | [`bool SameGeometry(BlockDataList before, BlockDataList after)`](../Assets/Editor/Salvage/CargoReturnFileChecks.cs#L131) | private static |
| CargoReturnFileChecks | [`void Check(Report report, string label, bool passed)`](../Assets/Editor/Salvage/CargoReturnFileChecks.cs#L146) | private static |

## Assets/Editor/Salvage/SalvageAssetBaker.cs

// 职责：生成并保存货仓、回收、掉落物和相关配置资产。 // 入口：Bake() / Set() / Preview()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SalvageAssetBaker | [`void Bake()`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L15) | public static |
| SalvageAssetBaker | [`Material Material(string name, Color color, bool transparent, bool emissive)`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L68) | private static |
| SalvageAssetBaker | [`void Hold( string name, CargoKind kind, int size, int capacity, bool explosive, bool enemyDrops)`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L107) | private static |
| SalvageAssetBaker | [`void CollectionBay()`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L246) | private static |
| SalvageAssetBaker | [`void DropPrefab()`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L329) | private static |
| SalvageAssetBaker | [`GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L370) | private static |
| SalvageAssetBaker | [`void Set(Object target, string property, Object value)`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L383) | public static |
| SalvageAssetBaker | [`void Preview()`](../Assets/Editor/Salvage/SalvageAssetBaker.cs#L390) | public static |

## Assets/Editor/Salvage/SalvagePlayProbe.cs

// 职责：在 Play Mode 检查货仓、回收、机器人和持久化行为。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SalvagePlayProbe | [`void Run()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L32) | public static |
| SalvagePlayProbe | [`void RunBuildings()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L38) | public static |
| SalvagePlayProbe | [`void Start(bool buildingsOnly)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L44) | private static |
| SalvagePlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L61) | private static |
| SalvagePlayProbe | [`void Capture(string condition, string stack, LogType type)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L104) | private static |
| SalvagePlayProbe | [`void Tick()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L112) | private static |
| SalvagePlayProbe | [`void Finish()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L142) | private static |
| SalvagePlayProbe | [`void Check(string name, bool result)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L169) | private static |
| SalvagePlayProbe | [`ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool cockpit = true)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L181) | private static |
| SalvagePlayProbe | [`GameObject Spawn(string name, ControlUnit unit, Vector3 local)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L208) | private static |
| SalvagePlayProbe | [`CargoItem Coins(int count)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L223) | private static |
| SalvagePlayProbe | [`CargoItem Part()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L228) | private static |
| SalvagePlayProbe | [`BuildingRuntime SalvageBuilding(WorldBuildingManager manager, UnitFaction faction, bool withBot = false)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L234) | private static |
| SalvagePlayProbe | [`IEnumerator BuildingSalvage()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L278) | private static |
| SalvagePlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L428) | private static |

## Assets/Editor/Siege/SiegeArenaLookAuthoring.cs

// 职责：为阵地战场景建立夜色竞技场外观：程序化天空盒、雾色、环境光与主光，并保持可编辑资产。 // 入口：ApplyLook()；只改Siege场景与其天空盒材质，复用Main既有色调默认值，不改Main场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeArenaLookAuthoring | [`void ApplyLook()`](../Assets/Editor/Siege/SiegeArenaLookAuthoring.cs#L18) | public static |
| SiegeArenaLookAuthoring | [`void Pylons()`](../Assets/Editor/Siege/SiegeArenaLookAuthoring.cs#L111) | private static |
| SiegeArenaLookAuthoring | [`Material Material(string name, Color color, Color? emission)`](../Assets/Editor/Siege/SiegeArenaLookAuthoring.cs#L157) | private static |

## Assets/Editor/Siege/SiegeBaseBlueprintAuthoring.cs

// 职责：制作32×32m建筑蓝图基地，并通过Editor迁移已有Siege场景的建筑管理器/核心配置。 // 入口：UpgradeScene() / Generate()；写入真实资产，保留已有GUID，不修改Main或用户建筑目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBaseBlueprintAuthoring | [`void UpgradeScene()`](../Assets/Editor/Siege/SiegeBaseBlueprintAuthoring.cs#L14) | public static |
| SiegeBaseBlueprintAuthoring | [`void ConfigureManager(WorldBuildingManager buildings)`](../Assets/Editor/Siege/SiegeBaseBlueprintAuthoring.cs#L55) | internal static |
| SiegeBaseBlueprintAuthoring | [`TextAsset Generate(bool replace = false)`](../Assets/Editor/Siege/SiegeBaseBlueprintAuthoring.cs#L62) | public static |
| SiegeBaseBlueprintAuthoring | [`void Add(BlockDataList blueprint, string resource, Vector3 position, string id = null)`](../Assets/Editor/Siege/SiegeBaseBlueprintAuthoring.cs#L153) | private static |

## Assets/Editor/Siege/SiegeBlueprintCatalogChecks.cs

// 职责：验证攻守蓝图目录隔离、坏文件容错、身份复制与源文件只读边界。 // 入口：Run()；使用Temp内独立目录，不读写用户存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBlueprintCatalogChecks | [`string Run()`](../Assets/Editor/Siege/SiegeBlueprintCatalogChecks.cs#L10) | public static |
| SiegeBlueprintCatalogChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeBlueprintCatalogChecks.cs#L51) | private static |

## Assets/Editor/Siege/SiegeBlueprintSelectionAuthoring.cs

// 职责：通过Editor API为正常Siege安装真实可编辑的双方蓝图选择器，保留压力场夹具。 // 入口：Install()；重复执行复用现有控件，不覆盖Main或用户蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBlueprintSelectionAuthoring | [`void Install()`](../Assets/Editor/Siege/SiegeBlueprintSelectionAuthoring.cs#L12) | public static |
| SiegeBlueprintSelectionAuthoring | [`Dropdown CreateDropdown(Transform parent, string name, float y)`](../Assets/Editor/Siege/SiegeBlueprintSelectionAuthoring.cs#L56) | private static |
| SiegeBlueprintSelectionAuthoring | [`void Place(RectTransform rect, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeBlueprintSelectionAuthoring.cs#L83) | private static |

## Assets/Editor/Siege/SiegeBuildingBenchmark.cs

// 职责：用真实两层建筑夹具测量完整支撑与单次范围损毁，保留生产同步开销与正确性计数。 // 入口：Start() / Status；远离战场、无物理步，不代表大规模交火帧率。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingBenchmark | [`string Start(string label = "geometry-baseline", bool large = false)`](../Assets/Editor/Siege/SiegeBuildingBenchmark.cs#L17) | public static |
| SiegeBuildingBenchmark | [`void Execute(string label, bool large)`](../Assets/Editor/Siege/SiegeBuildingBenchmark.cs#L42) | private static |
| SiegeBuildingBenchmark | [`Sample Measure(int count)`](../Assets/Editor/Siege/SiegeBuildingBenchmark.cs#L98) | private static |
| SiegeBuildingBenchmark | [`Stages ReadStages()`](../Assets/Editor/Siege/SiegeBuildingBenchmark.cs#L209) | private static |

## Assets/Editor/Siege/SiegeBuildingChecks.cs

// 职责：验证真实大型建筑蓝图基地的支撑/净空、原位修缮、独立核心以及舰船生命周期隔离。 // 入口：Run()；全新Siege Play，显式损坏夹具不代表自然波次或性能结论。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingChecks | [`string Run()`](../Assets/Editor/Siege/SiegeBuildingChecks.cs#L13) | public static |
| SiegeBuildingChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeBuildingChecks.cs#L151) | private static |

## Assets/Editor/Siege/SiegeBuildingDamageChecks.cs

// 职责：验证建筑批量损毁的命中顺序独立、一次支撑刷新、链式爆炸及单块即时断裂契约。 // 入口：Run()；仅使用远离场景的自有夹具，恢复阵地战伤害门控，不改资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingDamageChecks | [`string Run()`](../Assets/Editor/Siege/SiegeBuildingDamageChecks.cs#L13) | public static |
| SiegeBuildingDamageChecks | [`void CheckPartialBlast(List<string> checks, bool reverse)`](../Assets/Editor/Siege/SiegeBuildingDamageChecks.cs#L52) | private static |
| SiegeBuildingDamageChecks | [`void CheckChain(List<string> checks, bool projectile)`](../Assets/Editor/Siege/SiegeBuildingDamageChecks.cs#L87) | private static |
| SiegeBuildingDamageChecks | [`void CheckImmediate(List<string> checks)`](../Assets/Editor/Siege/SiegeBuildingDamageChecks.cs#L138) | private static |
| SiegeBuildingDamageChecks | [`GameObject CreateTower(bool reverse, out WorldBuildingManager manager, out BuildingRuntime tower)`](../Assets/Editor/Siege/SiegeBuildingDamageChecks.cs#L163) | private static |
| SiegeBuildingDamageChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeBuildingDamageChecks.cs#L206) | private static |

## Assets/Editor/Siege/SiegeBuildingGraphChecks.cs

// 职责：对照实时几何验证建筑邻接缓存的热路径、根运动、局部修改、断链与删除失效。 // 入口：Run()；隔离真实方块夹具，不运行物理步，不修改场景资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingGraphChecks | [`string Run()`](../Assets/Editor/Siege/SiegeBuildingGraphChecks.cs#L14) | public static |

## Assets/Editor/Siege/SiegeBuildingPowerProbe.cs

// 职责：用基地中央的高耐久敌方固定靶验证四座建筑炮塔真实索敌、供电和弹道命中。 // 入口：Start() / Status；真实推进物理帧，固定靶是显式夹具，不代表自然波次完成。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingPowerProbe | [`string Start()`](../Assets/Editor/Siege/SiegeBuildingPowerProbe.cs#L18) | public static |
| SiegeBuildingPowerProbe | [`void Tick()`](../Assets/Editor/Siege/SiegeBuildingPowerProbe.cs#L58) | private static |
| SiegeBuildingPowerProbe | [`void Finish(string result)`](../Assets/Editor/Siege/SiegeBuildingPowerProbe.cs#L87) | private static |

## Assets/Editor/Siege/SiegeCombatFeedbackChecks.cs

// 职责：验证精英波次奖励倍数与战斗中连击加成的纯规则行为，不依赖场景、物理或敌舰生成。 // 入口：Run()；只构造SiegeMatch并登记/结算虚构敌机，不写资产、不改设置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCombatFeedbackChecks | [`string Run()`](../Assets/Editor/Siege/SiegeCombatFeedbackChecks.cs#L16) | public static |
| SiegeCombatFeedbackChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeCombatFeedbackChecks.cs#L230) | private static |

## Assets/Editor/Siege/SiegeCommandHudAuthoring.cs

// 职责：把阵地战HUD重排为单条指挥栏、左侧舰队坞与底部主操作，并停用Main遗留的准备/调试遮罩。 // 入口：Apply()；幂等重排，克隆HUD已有文本以保证字体/阴影材质可渲染，不改Main场景与用户蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCommandHudAuthoring | [`void Apply()`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L21) | public static |
| SiegeCommandHudAuthoring | [`void RescueLegacyHelp(SiegeHud hud)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L65) | private static |
| SiegeCommandHudAuthoring | [`Text FindLegacyHelp(SiegeHud hud)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L74) | private static |
| SiegeCommandHudAuthoring | [`void DestroyGenerated(SiegeHud hud, params string[] names)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L87) | private static |
| SiegeCommandHudAuthoring | [`void DisableLegacyOverlays()`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L100) | private static |
| SiegeCommandHudAuthoring | [`void CommandBar(SiegeHud hud, Text template)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L129) | private static |
| SiegeCommandHudAuthoring | [`void FleetDock(SiegeHud hud)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L210) | private static |
| SiegeCommandHudAuthoring | [`void ActionBar(SiegeHud hud)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L411) | private static |
| SiegeCommandHudAuthoring | [`void CombatHud(SiegeHud hud, Text template)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L441) | private static |
| SiegeCommandHudAuthoring | [`void Edge( Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L728) | private static |
| SiegeCommandHudAuthoring | [`RectTransform Group( Transform parent, string name, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L757) | private static |
| SiegeCommandHudAuthoring | [`void ThreatMarkers(SiegeHud hud)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L779) | private static |
| SiegeCommandHudAuthoring | [`void ControlsHelp(SiegeHud hud, Text template)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L793) | private static |
| SiegeCommandHudAuthoring | [`Text CloneLabel( Transform parent, Text template, string name, string text, float x, float y, float width, float height, int size)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L880) | private static |
| SiegeCommandHudAuthoring | [`void StyleButton(Button button, int fontSize, bool primary)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L907) | private static |
| SiegeCommandHudAuthoring | [`void PlaceOptional( Transform parent, string name, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L935) | private static |
| SiegeCommandHudAuthoring | [`Text RequireText(Transform parent, string name)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L950) | private static |
| SiegeCommandHudAuthoring | [`RectTransform Require(Transform parent, string path)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L962) | private static |
| SiegeCommandHudAuthoring | [`Text FindText(Transform parent, string name)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L973) | private static |
| SiegeCommandHudAuthoring | [`Transform FindChild(Transform parent, string name)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L980) | private static |
| SiegeCommandHudAuthoring | [`Transform FindDeep(Transform parent, string name)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L994) | private static |
| SiegeCommandHudAuthoring | [`Transform FindUnder(Transform root, string path)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L1007) | private static |
| SiegeCommandHudAuthoring | [`Transform FindByPath(string path)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L1022) | private static |
| SiegeCommandHudAuthoring | [`RectTransform Panel( Transform parent, string name, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L1035) | private static |
| SiegeCommandHudAuthoring | [`void EnsureLine( Transform parent, string name, float x, float y, float width, float height, Color color)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L1050) | private static |
| SiegeCommandHudAuthoring | [`void Place(RectTransform rect, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeCommandHudAuthoring.cs#L1072) | private static |

## Assets/Editor/Siege/SiegeDestructionLogChecks.cs

// 职责：隔离检查销毁/VFX诊断默认值、可恢复开关及ABBA日志路径开销，不刺激自然战斗。 // 入口：Run()；禁用效果的同方法对照只测诊断路径，不代表交火帧率或真实爆炸成本。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeDestructionLogChecks | [`string Run()`](../Assets/Editor/Siege/SiegeDestructionLogChecks.cs#L15) | public static |
| SiegeDestructionLogChecks | [`void Check(Report report, string name, bool passed)`](../Assets/Editor/Siege/SiegeDestructionLogChecks.cs#L118) | private static |

## Assets/Editor/Siege/SiegeFleetChecks.cs

// 职责：在真实Play环境验证共用蓝图恢复、阵营与驾驶权隔离、AI接管及旧敌机入口。 // 入口：Run()；创建隔离舰艇，结束后停用并销毁，不保存玩家文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFleetChecks | [`string Run()`](../Assets/Editor/Siege/SiegeFleetChecks.cs#L12) | public static |
| SiegeFleetChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeFleetChecks.cs#L108) | private static |

## Assets/Editor/Siege/SiegeFoundationCacheChecks.cs

// 职责：在真实基地上对照缓存与实时底座判定，验证根运动和局部连接几何失效。 // 入口：Run()；同步隔离变化后恢复，不推进物理步或保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFoundationCacheChecks | [`string Run()`](../Assets/Editor/Siege/SiegeFoundationCacheChecks.cs#L14) | public static |

## Assets/Editor/Siege/SiegeMainInterfaceAuthoring.cs

// 职责：将 Main 的真实建造UI、管理器与轴工具迁移至Siege，保留Main源场景与战场布局。 // 入口：Install()；显式菜单保存资产，可在Inspector继续编辑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeMainInterfaceAuthoring | [`void Install()`](../Assets/Editor/Siege/SiegeMainInterfaceAuthoring.cs#L14) | public static |
| SiegeMainInterfaceAuthoring | [`void FitLayout()`](../Assets/Editor/Siege/SiegeMainInterfaceAuthoring.cs#L134) | public static |
| SiegeMainInterfaceAuthoring | [`void Place(RectTransform rect, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeMainInterfaceAuthoring.cs#L190) | private static |

## Assets/Editor/Siege/SiegeMixedStressAuthoring.cs

// 职责：制作四栋建筑/六舰/十二敌舰的独立混合压力Scene和配置，保留正常阵地战资产。 // 入口：Create()；首次创建后可编辑保存，已有目标不覆盖，恢复原活动场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeMixedStressAuthoring | [`void Create()`](../Assets/Editor/Siege/SiegeMixedStressAuthoring.cs#L15) | public static |

## Assets/Editor/Siege/SiegeMixedStressChecks.cs

// 职责：验证独立混合压力场的实际建筑规模、预算、模块身份和卫星损毁/清理边界。 // 入口：Start() / Status；真实Play加载完成后执行，仅作用于该测试会话。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeMixedStressChecks | [`void Start()`](../Assets/Editor/Siege/SiegeMixedStressChecks.cs#L16) | public static |
| SiegeMixedStressChecks | [`void Tick()`](../Assets/Editor/Siege/SiegeMixedStressChecks.cs#L31) | private static |
| SiegeMixedStressChecks | [`void Check(Report report, string label, bool passed)`](../Assets/Editor/Siege/SiegeMixedStressChecks.cs#L117) | private static |

## Assets/Editor/Siege/SiegePerformanceCapture.cs

// 职责：采集阵地战真实Play帧观察间隔、分阶段CPU计时和低频场景规模，保存可审查原始样本。 // 入口：StartCapture() / Finish() / Status；Editor观察间隔含编辑器调度，不代表Player或GPU耗时。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePerformanceCapture | [`string StartCapture(string label = "natural-baseline", float seconds = 90f)`](../Assets/Editor/Siege/SiegePerformanceCapture.cs#L31) | public static |
| SiegePerformanceCapture | [`void Tick()`](../Assets/Editor/Siege/SiegePerformanceCapture.cs#L72) | private static |
| SiegePerformanceCapture | [`void TakeCensus(double now)`](../Assets/Editor/Siege/SiegePerformanceCapture.cs#L101) | private static |
| SiegePerformanceCapture | [`string Finish(string reason = "Manual stop")`](../Assets/Editor/Siege/SiegePerformanceCapture.cs#L133) | public static |
| SiegePerformanceCapture | [`double Percentile(List<double> values, double fraction)`](../Assets/Editor/Siege/SiegePerformanceCapture.cs#L179) | private static |
| SiegePerformanceCapture | [`void BeforeReload()`](../Assets/Editor/Siege/SiegePerformanceCapture.cs#L184) | private static |

## Assets/Editor/Siege/SiegePlayChecks.cs

// 职责：在独立Siege真实Play中验证部署/接管、修缮身份与金币、驾驶舰死亡和基地失败。 // 入口：Run()；损伤/缺件/断裂为显式夹具，死亡走生产伤害链；不代表自然交火或性能测试。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayChecks | [`string Run()`](../Assets/Editor/Siege/SiegePlayChecks.cs#L14) | public static |
| SiegePlayChecks | [`int SetComboBonus(SiegeSession session, int value)`](../Assets/Editor/Siege/SiegePlayChecks.cs#L230) | private static |
| SiegePlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegePlayChecks.cs#L241) | private static |

## Assets/Editor/Siege/SiegePlayerBuild.cs

// 职责：构建以Siege为入口的独立Windows D3D11验证包，不改全局构建场景或用户发布包。 // 入口：Build() / BuildStress() / BuildLargeStress() / BuildMixedStress() / BuildLargeProfiling()；独立Development包用于引擎归因。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayerBuild | [`void Build()`](../Assets/Editor/Siege/SiegePlayerBuild.cs#L16) | public static |
| SiegePlayerBuild | [`void BuildStress()`](../Assets/Editor/Siege/SiegePlayerBuild.cs#L22) | public static |
| SiegePlayerBuild | [`void BuildLargeStress()`](../Assets/Editor/Siege/SiegePlayerBuild.cs#L32) | public static |
| SiegePlayerBuild | [`void BuildLargeProfiling()`](../Assets/Editor/Siege/SiegePlayerBuild.cs#L42) | public static |
| SiegePlayerBuild | [`void BuildMixedStress()`](../Assets/Editor/Siege/SiegePlayerBuild.cs#L53) | public static |
| SiegePlayerBuild | [`void Schedule(string scene, string output, string statusPath, BuildOptions options = BuildOptions.None)`](../Assets/Editor/Siege/SiegePlayerBuild.cs#L63) | private static |
| SiegePlayerBuild | [`void BuildNow(string scene, string output, string statusPath, BuildOptions options)`](../Assets/Editor/Siege/SiegePlayerBuild.cs#L83) | private static |

## Assets/Editor/Siege/SiegePlayerSurveyChecks.cs

// 职责：在真实Siege Play中校验刚体分类、闲置池过滤与显式采样的计时开关恢复，避免诊断污染玩法。 // 入口：Run()；分类用真实基地/舰船，慢帧边界用合成delta，不是性能或物理步证据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayerSurveyChecks | [`string Run()`](../Assets/Editor/Siege/SiegePlayerSurveyChecks.cs#L12) | public static |
| SiegePlayerSurveyChecks | [`Rigidbody CreateBody(List<GameObject> roots, string name)`](../Assets/Editor/Siege/SiegePlayerSurveyChecks.cs#L128) | private static |
| SiegePlayerSurveyChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegePlayerSurveyChecks.cs#L138) | private static |

## Assets/Editor/Siege/SiegePriorityTargetChecks.cs

// 职责：用真实基地与敌舰验证任务目标远程追击、普通索敌边界及阵营/启用/守卫约束。 // 入口：Run()；全新Siege Play的同步隔离夹具，不推进物理步，不把该检查当作AI实飞验证。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePriorityTargetChecks | [`string Run()`](../Assets/Editor/Siege/SiegePriorityTargetChecks.cs#L14) | public static |
| SiegePriorityTargetChecks | [`void Check(Report report, string name, bool passed)`](../Assets/Editor/Siege/SiegePriorityTargetChecks.cs#L128) | private static |

## Assets/Editor/Siege/SiegePriorityTargetFlight.cs

// 职责：真实Play跨帧验证范围外任务目标的接近输入与刚体位移，保留生产飞行和供电参数。 // 入口：Start() / Status；仅创建并清理自己的敌舰，不代表自然整波性能或全部AI导航。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePriorityTargetFlight | [`string Start()`](../Assets/Editor/Siege/SiegePriorityTargetFlight.cs#L18) | public static |
| SiegePriorityTargetFlight | [`void Tick()`](../Assets/Editor/Siege/SiegePriorityTargetFlight.cs#L57) | private static |
| SiegePriorityTargetFlight | [`void BeforeReload()`](../Assets/Editor/Siege/SiegePriorityTargetFlight.cs#L83) | private static |
| SiegePriorityTargetFlight | [`void Finish(string reason)`](../Assets/Editor/Siege/SiegePriorityTargetFlight.cs#L88) | private static |

## Assets/Editor/Siege/SiegeProgressAuthoring.cs

// 职责：提供清除本机阵地战战果记录的显式菜单，便于测试或重新开始统计。 // 入口：ClearRecords()；只写本地PlayerPrefs，不触碰存档蓝图或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeProgressAuthoring | [`void ClearRecords()`](../Assets/Editor/Siege/SiegeProgressAuthoring.cs#L8) | public static |

## Assets/Editor/Siege/SiegeRegroupBaseline.cs

// 职责：冻结3b1e13d的整组迁移实现，仅供Editor同布局性能对照，不进入运行时。 // 入口：Rebuild()；连接查询仍调用当前同一几何实现，隔离测量刚体迁移策略差异。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRegroupBaseline | [`void Rebuild(List<Block> blocks, GroupPhysicsSettings settings)`](../Assets/Editor/Siege/SiegeRegroupBaseline.cs#L14) | public |
| SiegeRegroupBaseline | [`Dictionary<Block, Rigidbody> CaptureSourceBodies(List<Block> blocks)`](../Assets/Editor/Siege/SiegeRegroupBaseline.cs#L74) | private static |
| SiegeRegroupBaseline | [`void RestoreGroupPhysics( Rigidbody targetBody, List<Block> group, Dictionary<Block, Rigidbody> sourceBodies, GroupPhysicsSettings settings)`](../Assets/Editor/Siege/SiegeRegroupBaseline.cs#L94) | private static |

## Assets/Editor/Siege/SiegeRegroupBenchmark.cs

// 职责：在相同真实方块链上交替比较冻结整组迁移与主体保留，分开记录创建/验证和同步刷新耗时。 // 入口：Run()；临时夹具每次立即清理，不将同步CPU对照称为物理帧率。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRegroupBenchmark | [`string Start()`](../Assets/Editor/Siege/SiegeRegroupBenchmark.cs#L18) | public static |
| SiegeRegroupBenchmark | [`void ExecuteScheduled()`](../Assets/Editor/Siege/SiegeRegroupBenchmark.cs#L32) | private static |
| SiegeRegroupBenchmark | [`string Run()`](../Assets/Editor/Siege/SiegeRegroupBenchmark.cs#L50) | public static |
| SiegeRegroupBenchmark | [`double Measure(int count, bool split, bool baseline, SiegeRegroupBaseline previous, GroupPhysicsSettings settings)`](../Assets/Editor/Siege/SiegeRegroupBenchmark.cs#L128) | private static |
| SiegeRegroupBenchmark | [`double Median(List<double> values)`](../Assets/Editor/Siege/SiegeRegroupBenchmark.cs#L212) | private static |

## Assets/Editor/Siege/SiegeRegroupChecks.cs

// 职责：验证增量重组保留驾驶主体、世界姿态、源点速度、阵营/货物归属及局部刷新边界。 // 入口：Run()；要求全新Siege Play，受控断链夹具不代表自然战斗性能。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRegroupChecks | [`string Run()`](../Assets/Editor/Siege/SiegeRegroupChecks.cs#L13) | public static |
| SiegeRegroupChecks | [`void CheckSingleBlock(List<string> checks)`](../Assets/Editor/Siege/SiegeRegroupChecks.cs#L118) | private static |
| SiegeRegroupChecks | [`void CheckEmptyRoot(List<string> checks)`](../Assets/Editor/Siege/SiegeRegroupChecks.cs#L147) | private static |
| SiegeRegroupChecks | [`void CheckCockpitlessSplit(List<string> checks)`](../Assets/Editor/Siege/SiegeRegroupChecks.cs#L178) | private static |
| SiegeRegroupChecks | [`bool PosesUnchanged(Dictionary<Block, Pose> poses)`](../Assets/Editor/Siege/SiegeRegroupChecks.cs#L234) | private static |
| SiegeRegroupChecks | [`bool Near(Vector3 left, Vector3 right)`](../Assets/Editor/Siege/SiegeRegroupChecks.cs#L239) | private static |
| SiegeRegroupChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeRegroupChecks.cs#L244) | private static |

## Assets/Editor/Siege/SiegeRulesChecks.cs

// 职责：验证阵地战阶段、波次预算和经济的边界；无需进入Play或读写玩家存档。 // 入口：Run() / 菜单 HY-Sandbox/Validation/Siege Rules。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRulesChecks | [`void RunFromMenu()`](../Assets/Editor/Siege/SiegeRulesChecks.cs#L11) | public static |
| SiegeRulesChecks | [`string Run()`](../Assets/Editor/Siege/SiegeRulesChecks.cs#L17) | public static |
| SiegeRulesChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeRulesChecks.cs#L117) | private static |

## Assets/Editor/Siege/SiegeRuntimeConnectionChecks.cs

// 职责：验证缓存连接的无查询热路径、断链、制作变更失效和无视觉/双侧视觉生命周期。 // 入口：Run()；调用真实内部运行分组入口，物理探测/旧视觉扫描计数由生产代码按需记录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRuntimeConnectionChecks | [`string Run()`](../Assets/Editor/Siege/SiegeRuntimeConnectionChecks.cs#L15) | public static |
| SiegeRuntimeConnectionChecks | [`List<List<Block>> Groups(List<Block> blocks)`](../Assets/Editor/Siege/SiegeRuntimeConnectionChecks.cs#L164) | private static |
| SiegeRuntimeConnectionChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeRuntimeConnectionChecks.cs#L169) | private static |

## Assets/Editor/Siege/SiegeSceneAuthoring.cs

// 职责：通过Unity API创建独立、可编辑的阵地战场景和默认配置，不重写Main场景或用户存档。 // 入口：Create() / Open()；已有场景不覆盖，泊位、UI、材质和波次配置均保存为真实资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeSceneAuthoring | [`void Create()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L20) | public static |
| SiegeSceneAuthoring | [`void Open()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L83) | public static |
| SiegeSceneAuthoring | [`void FitHudToViewport()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L94) | public static |
| SiegeSceneAuthoring | [`void RegenerateDefaultBase()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L120) | public static |
| SiegeSceneAuthoring | [`TextAsset CreateBaseBlueprint(bool replace = false)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L138) | private static |
| SiegeSceneAuthoring | [`SiegeSettings CreateSettings(TextAsset baseBlueprint)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L143) | private static |
| SiegeSceneAuthoring | [`void CreateManagers()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L188) | private static |
| SiegeSceneAuthoring | [`void CreateEnvironment()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L204) | private static |
| SiegeSceneAuthoring | [`Transform[] CreateDocks(Transform parent)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L227) | private static |
| SiegeSceneAuthoring | [`Material MaterialAsset(string name, Color color)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L248) | private static |
| SiegeSceneAuthoring | [`void CreateCamera()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L265) | private static |
| SiegeSceneAuthoring | [`void CreateHud(SiegeSession session, SiegeSettings settings, int bays)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L282) | private static |
| SiegeSceneAuthoring | [`RectTransform Panel( Transform parent, string name, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L447) | private static |
| SiegeSceneAuthoring | [`Text Label( Transform parent, string text, float x, float y, float width, float height, int size)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L462) | private static |
| SiegeSceneAuthoring | [`Button ActionButton( Transform parent, string name, string text, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L483) | private static |
| SiegeSceneAuthoring | [`void Place(RectTransform rect, float x, float y, float width, float height)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L513) | private static |
| SiegeSceneAuthoring | [`void SetArray(SerializedProperty property, Object[] values)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L521) | private static |

## Assets/Editor/Siege/SiegeStressAuthoring.cs

// 职责：从阵地战场景制作六舰/2048模块建筑压力资产，隔离测试经济与正常游戏参数。 // 入口：Create() / CreateLarge()；首次创建不覆盖既有资产，保存后恢复原活动场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeStressAuthoring | [`void Create()`](../Assets/Editor/Siege/SiegeStressAuthoring.cs#L18) | public static |
| SiegeStressAuthoring | [`void CreateLarge()`](../Assets/Editor/Siege/SiegeStressAuthoring.cs#L24) | public static |
| SiegeStressAuthoring | [`void CreateProfile(string scenePath, string settingsPath, string sourcePath, bool large)`](../Assets/Editor/Siege/SiegeStressAuthoring.cs#L31) | private static |
| SiegeStressAuthoring | [`TextAsset GenerateLargeBlueprint(TextAsset source)`](../Assets/Editor/Siege/SiegeStressAuthoring.cs#L124) | private static |
| SiegeStressAuthoring | [`SiegeSession FindSession(Scene scene)`](../Assets/Editor/Siege/SiegeStressAuthoring.cs#L166) | private static |

## Assets/Editor/Siege/SiegeStressCancellationChecks.cs

// 职责：在首次分帧恢复中取消压力组件，验证HUD恢复与未发布建筑的回滚。 // 入口：Start() / Status；全新普通Siege Play，两个实际帧后检查无残留根对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeStressCancellationChecks | [`string Start()`](../Assets/Editor/Siege/SiegeStressCancellationChecks.cs#L19) | public static |
| SiegeStressCancellationChecks | [`void Complete()`](../Assets/Editor/Siege/SiegeStressCancellationChecks.cs#L64) | private static |
| SiegeStressCancellationChecks | [`void Check(string label, bool passed)`](../Assets/Editor/Siege/SiegeStressCancellationChecks.cs#L94) | private static |

## Assets/Editor/Siege/SiegeTopologyBaseline.cs

// 职责：在远离战场的真实方块夹具上测量分组，并复现断链后输入顺序影响，保留优化前证据。 // 入口：Run()；不运行物理步，不将同步图查询耗时称为大规模战斗帧率。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeTopologyBaseline | [`string Run()`](../Assets/Editor/Siege/SiegeTopologyBaseline.cs#L13) | public static |
| SiegeTopologyBaseline | [`string Partition(List<List<Block>> groups)`](../Assets/Editor/Siege/SiegeTopologyBaseline.cs#L103) | private static |

## Assets/Editor/Siege/SiegeTopologyChecks.cs

// 职责：用真实方块验证断链对称性、碰撞层恢复以及移动/禁用连接面等分组契约。 // 入口：Run()；临时夹具与现有单位隔离，失败抛出并在finally清理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeTopologyChecks | [`string Run()`](../Assets/Editor/Siege/SiegeTopologyChecks.cs#L12) | public static |
| SiegeTopologyChecks | [`bool IsAllSingleton(List<List<Block>> groups)`](../Assets/Editor/Siege/SiegeTopologyChecks.cs#L136) | private static |
| SiegeTopologyChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeTopologyChecks.cs#L141) | private static |

## Assets/Editor/Siege/SiegeTransientEffectBenchmark.cs

// 职责：测量实际短时光束/点光源的创建路径；远处隔离夹具不代表自然战斗FPS。 // 入口：Run(label)；每批同步到期，清理在计时外，不推进物理或保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeTransientEffectBenchmark | [`string Run(string label)`](../Assets/Editor/Siege/SiegeTransientEffectBenchmark.cs#L15) | public static |
| SiegeTransientEffectBenchmark | [`bool IsOwned(MonoBehaviour item, Vector3 origin)`](../Assets/Editor/Siege/SiegeTransientEffectBenchmark.cs#L81) | private static |

## Assets/Editor/Siege/SiegeTransientEffectChecks.cs

// 职责：检查短时效果复用、重置、闲置上限及场景卸载；不替代自然画面验证。 // 入口：Run() / Status；卸载异步完成后写出最终报告。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeTransientEffectChecks | [`string Run()`](../Assets/Editor/Siege/SiegeTransientEffectChecks.cs#L23) | public static |
| SiegeTransientEffectChecks | [`void CompleteUnload()`](../Assets/Editor/Siege/SiegeTransientEffectChecks.cs#L145) | private static |
| SiegeTransientEffectChecks | [`T Read(object owner, string field)`](../Assets/Editor/Siege/SiegeTransientEffectChecks.cs#L174) | private static |
| SiegeTransientEffectChecks | [`void Expire(MonoBehaviour fade)`](../Assets/Editor/Siege/SiegeTransientEffectChecks.cs#L179) | private static |
| SiegeTransientEffectChecks | [`void Check(string label, bool passed)`](../Assets/Editor/Siege/SiegeTransientEffectChecks.cs#L186) | private static |

## Assets/Editor/Siege/SiegeUsabilityChecks.cs

// 职责：以真实部署净空和工作台往返检查阵地战泊位选择与蓝图选择保持，保护金币和用户存档。 // 入口：RunDeployment() / RunSelection()；分别在全新 Siege Play 中执行，不修改源蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeUsabilityChecks | [`string RunDeployment()`](../Assets/Editor/Siege/SiegeUsabilityChecks.cs#L15) | public static |
| SiegeUsabilityChecks | [`string RunSelection()`](../Assets/Editor/Siege/SiegeUsabilityChecks.cs#L96) | public static |
| SiegeUsabilityChecks | [`SiegeSession FreshSession()`](../Assets/Editor/Siege/SiegeUsabilityChecks.cs#L174) | private static |
| SiegeUsabilityChecks | [`void RefreshHud(SiegeHud hud)`](../Assets/Editor/Siege/SiegeUsabilityChecks.cs#L185) | private static |
| SiegeUsabilityChecks | [`void Check(List<string> checks, string label, bool passed)`](../Assets/Editor/Siege/SiegeUsabilityChecks.cs#L191) | private static |
| SiegeUsabilityChecks | [`string Write(string name, List<string> checks, Exception error = null)`](../Assets/Editor/Siege/SiegeUsabilityChecks.cs#L201) | private static |

## Assets/Editor/Siege/SiegeWorkshopChecks.cs

// 职责：在真实Siege Play帧验证Main工作台、异步取消、存档写入和战场隔离。 // 入口：Run()；只创建唯一临时Saves蓝图，finally清理，绝不写用户已有蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWorkshopChecks | [`void Run()`](../Assets/Editor/Siege/SiegeWorkshopChecks.cs#L18) | public static |
| SiegeWorkshopChecks | [`IEnumerator CheckRoutine(SiegeSession session)`](../Assets/Editor/Siege/SiegeWorkshopChecks.cs#L33) | private static |
| SiegeWorkshopChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegeWorkshopChecks.cs#L236) | private static |

## Assets/Editor/Siege/SiegeWreckBudgetChecks.cs

// 职责：验证敌方碎片预算、寿命、节流及阵营/驾驶舱/场景保护，不制造真实击杀或修改蓝图。 // 入口：Run()；普通Siege全新Play，夹具同步检查后清除。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWreckBudgetChecks | [`string Run()`](../Assets/Editor/Siege/SiegeWreckBudgetChecks.cs#L15) | public static |
| SiegeWreckBudgetChecks | [`ControlUnit Create(string label, UnitFaction faction, int count)`](../Assets/Editor/Siege/SiegeWreckBudgetChecks.cs#L125) | private static |
| SiegeWreckBudgetChecks | [`void Check(string label, bool passed)`](../Assets/Editor/Siege/SiegeWreckBudgetChecks.cs#L144) | private static |

## Assets/Editor/World/OutpostContentAuthoring.cs

// 职责：制作三套普通建筑与三套敌机轮廓底座JSON，保留可重建的项目源。 // 入口：Bake()；只写项目Resources，不修改用户蓝图或存档；已有JSON保留，编辑后可单独删除重建。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostContentAuthoring | [`void Bake()`](../Assets/Editor/World/OutpostContentAuthoring.cs#L12) | public static |
| OutpostContentAuthoring | [`void Rebuild()`](../Assets/Editor/World/OutpostContentAuthoring.cs#L18) | public static |
| OutpostContentAuthoring | [`void Bake(bool overwrite)`](../Assets/Editor/World/OutpostContentAuthoring.cs#L24) | private static |
| OutpostContentAuthoring | [`void ExportMatchingBases()`](../Assets/Editor/World/OutpostContentAuthoring.cs#L60) | public static |
| OutpostContentAuthoring | [`BlockDataList Base(int width, int length)`](../Assets/Editor/World/OutpostContentAuthoring.cs#L89) | private static |
| OutpostContentAuthoring | [`BlockDataList Workshop()`](../Assets/Editor/World/OutpostContentAuthoring.cs#L109) | private static |
| OutpostContentAuthoring | [`BlockDataList Storehouse()`](../Assets/Editor/World/OutpostContentAuthoring.cs#L146) | private static |
| OutpostContentAuthoring | [`BlockDataList Ruins()`](../Assets/Editor/World/OutpostContentAuthoring.cs#L173) | private static |
| OutpostContentAuthoring | [`void Loot(BlockDataList data, string resource, Vector3 position, CargoKind kind, int amount)`](../Assets/Editor/World/OutpostContentAuthoring.cs#L202) | private static |
| OutpostContentAuthoring | [`void Add( BlockDataList data, string resource, Vector3 position, int x = 0, int y = 0, int z = 0)`](../Assets/Editor/World/OutpostContentAuthoring.cs#L217) | private static |
| OutpostContentAuthoring | [`Vector3 SnapPosition(Vector3 position, int x, int y, int z)`](../Assets/Editor/World/OutpostContentAuthoring.cs#L234) | private static |
| OutpostContentAuthoring | [`void UpgradeGeometry()`](../Assets/Editor/World/OutpostContentAuthoring.cs#L243) | public static |
| OutpostContentAuthoring | [`string CreateStructuralPart(int x, int y, int z)`](../Assets/Editor/World/OutpostContentAuthoring.cs#L289) | private static |
| OutpostContentAuthoring | [`void Write(string name, BlockDataList data, bool overwrite)`](../Assets/Editor/World/OutpostContentAuthoring.cs#L329) | private static |

## Assets/Editor/World/OutpostContentChecks.cs

// 职责：在Main Play隔离物理场景逐套验证建筑模型/连接/供电/驻军数据并渲染预览。 // 入口：Run()；仅写Temp，不保存场景或用户数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostContentChecks | [`string Run()`](../Assets/Editor/World/OutpostContentChecks.cs#L14) | public static |
| OutpostContentChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/World/OutpostContentChecks.cs#L157) | private static |
| OutpostContentChecks | [`void Render(BuildingRuntime building, string path)`](../Assets/Editor/World/OutpostContentChecks.cs#L167) | private static |

## Assets/Editor/World/OutpostHudAuthoring.cs

// 职责：为Main保存据点状态HUD；世界会话运行时由EnemySpawner自身门控全局刷怪。 // 入口：Apply()；只新增所属HUD，保留既有界面布局。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostHudAuthoring | [`void Apply()`](../Assets/Editor/World/OutpostHudAuthoring.cs#L13) | public static |

## Assets/Editor/World/OutpostResidentChecks.cs

// 职责：在隔离真实流式地形验证停机显示、原位激活和击败后不复活的据点生命周期。 // 入口：Run()由建筑编辑Play探针逐帧驱动；finally清理实例，不写用户数据或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostResidentChecks | [`IEnumerator Run(Action<string, bool> check)`](../Assets/Editor/World/OutpostResidentChecks.cs#L12) | public static |

## Assets/Editor/World/ProceduralVoxelWorldEditor.cs

// 职责：为选中的程序化世界提供Undo预览、重生成与真实网格Bake入口。 // 入口：ProceduralVoxelWorld Inspector；只写所选世界及WorldLab网格目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProceduralVoxelWorldEditor | [`void OnInspectorGUI()`](../Assets/Editor/World/ProceduralVoxelWorldEditor.cs#L9) | public override |

## Assets/Editor/World/VoxelMeshingChecks.cs

// 职责：以逐体素暴露面为独立基准，验证贪心网格覆盖、材质、法线、接缝与快照隔离。 // 入口：Run()；只创建临时数据/Mesh，报告保存到Temp，不修改场景和玩家数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelMeshingChecks | [`string Run()`](../Assets/Editor/World/VoxelMeshingChecks.cs#L12) | public static |
| VoxelMeshingChecks | [`void Verify(VoxelWorldData data, Vector3Int start, int size)`](../Assets/Editor/World/VoxelMeshingChecks.cs#L61) | private static |
| VoxelMeshingChecks | [`string Key(Vector3Int cell, Vector3Int normal, int material)`](../Assets/Editor/World/VoxelMeshingChecks.cs#L136) | private static |
| VoxelMeshingChecks | [`void Check(bool valid, string message)`](../Assets/Editor/World/VoxelMeshingChecks.cs#L141) | private static |

## Assets/Editor/World/WorldBuildingAuthoring.cs

// 职责：为独立 WorldLab 安装建筑库、示例模板和场景组合；已有模板与布局不覆盖。 // 入口：Tools/HY-Sandbox/World/Install building integration；只保存 WorldLab 与 Assets/WorldLab/Buildings。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingAuthoring | [`void Install()`](../Assets/Editor/World/WorldBuildingAuthoring.cs#L12) | public static |
| WorldBuildingAuthoring | [`BlockDataList CreateSample()`](../Assets/Editor/World/WorldBuildingAuthoring.cs#L93) | public static |
| WorldBuildingAuthoring | [`void Add(BlockDataList blueprint, string resource, Vector3 position)`](../Assets/Editor/World/WorldBuildingAuthoring.cs#L107) | private static |
| WorldBuildingAuthoring | [`Vector3 FindFlatPosition(ProceduralVoxelWorld world, BlockDataList blueprint)`](../Assets/Editor/World/WorldBuildingAuthoring.cs#L113) | private static |

## Assets/Editor/World/WorldBuildingPlayProbe.cs

// 职责：在 WorldLab 真实物理帧验证建筑放置、战斗、事务恢复、支撑坍塌和输入预览。 // 入口：Run()；存档只写 Temp/WorldBuildings，结束恢复种子和初始建筑，不保存运行场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingPlayProbe | [`void Run()`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L39) | public static |
| WorldBuildingPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L51) | private static |
| WorldBuildingPlayProbe | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L69) | private static |
| WorldBuildingPlayProbe | [`void Tick()`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L77) | private static |
| WorldBuildingPlayProbe | [`void Check(bool result, string description)`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L106) | private static |
| WorldBuildingPlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L116) | private static |
| WorldBuildingPlayProbe | [`void InvokeUpdate(MonoBehaviour behaviour)`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L417) | private static |
| WorldBuildingPlayProbe | [`void ExpectRejected(WorldBuildingManager manager, WorldLabSnapshot snapshot, string name)`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L422) | private static |
| WorldBuildingPlayProbe | [`void Finish()`](../Assets/Editor/World/WorldBuildingPlayProbe.cs#L437) | private static |

## Assets/Editor/World/WorldLabAuthoring.cs

// 职责：创建独立WorldLab场景并将生成网格保存为真实资产；Inspector支持种子与分块参数。 // 入口：Tools/HY-Sandbox/World/Create or open World Lab；不保存或改写Main。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabAuthoring | [`void CreateOrOpen()`](../Assets/Editor/World/WorldLabAuthoring.cs#L13) | public static |
| WorldLabAuthoring | [`Material MaterialAt(string name, Color color, float metallic)`](../Assets/Editor/World/WorldLabAuthoring.cs#L79) | private static |
| WorldLabAuthoring | [`void BakeMeshes(ProceduralVoxelWorld world)`](../Assets/Editor/World/WorldLabAuthoring.cs#L99) | public static |

## Assets/Editor/World/WorldLabChecks.cs

// 职责：检查种子、网格边界与真实PhysX破坏/填补/炮弹命中，并将结果写入验证文件。 // 入口：RunDataChecks() / RunPlayChecks()；只在独立WorldLab执行运行检查，不操作玩家存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabChecks | [`void Check(bool result, string name)`](../Assets/Editor/World/WorldLabChecks.cs#L12) | private static |
| WorldLabChecks | [`string RunDataChecks()`](../Assets/Editor/World/WorldLabChecks.cs#L22) | public static |
| WorldLabChecks | [`void Clear(VoxelWorldData data)`](../Assets/Editor/World/WorldLabChecks.cs#L66) | private static |
| WorldLabChecks | [`string RunPlayChecks()`](../Assets/Editor/World/WorldLabChecks.cs#L81) | public static |
| WorldLabChecks | [`string Save(string phase)`](../Assets/Editor/World/WorldLabChecks.cs#L176) | private static |

## Assets/Editor/World/WorldLabCollapseChecks.cs

// 职责：验证六邻接支撑、尺寸/数量上限及真实世界片段转刚体、再生清理。 // 入口：RunData() / RunPlay()；动态检查仅在WorldLab，真实下落由配套跨帧探针记录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabCollapseChecks | [`void Check(bool value, string name)`](../Assets/Editor/World/WorldLabCollapseChecks.cs#L13) | private static |
| WorldLabCollapseChecks | [`string RunData()`](../Assets/Editor/World/WorldLabCollapseChecks.cs#L23) | public static |
| WorldLabCollapseChecks | [`string RunPlay()`](../Assets/Editor/World/WorldLabCollapseChecks.cs#L54) | public static |
| WorldLabCollapseChecks | [`string Save(string phase)`](../Assets/Editor/World/WorldLabCollapseChecks.cs#L102) | private static |

## Assets/Editor/World/WorldLabPerformanceChecks.cs

// 职责：在WorldLab测量真实生成、球形破坏和脏块网格/碰撞重建的同步CPU成本。 // 入口：Run()；固定种子/爆点和样本记录，不把微基准当作帧率或GPU性能。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabPerformanceChecks | [`string Run()`](../Assets/Editor/World/WorldLabPerformanceChecks.cs#L13) | public static |
| WorldLabPerformanceChecks | [`double Median(double[] values)`](../Assets/Editor/World/WorldLabPerformanceChecks.cs#L80) | private static |

## Assets/Editor/World/WorldLabPersistenceChecks.cs

// 职责：验证差量保存/读取、原子替换备份与损坏文件拒绝，使用独立临时路径。 // 入口：Run()；不读写玩家或WorldLab真实存档；结束删除本次测试文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabPersistenceChecks | [`string Run()`](../Assets/Editor/World/WorldLabPersistenceChecks.cs#L9) | public static |

## Assets/Editor/World/WorldLabSoakChecks.cs

// 职责：在真实帧后台持续爆破/重生成，周期落盘并检查分块/网格有界及输入遮罩下帧推进。 // 入口：Begin(minutes)；最多15分钟，Stop()或离开Play安全结束，不接触真实玩家存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabSoakChecks | [`string Begin(int minutes = 15)`](../Assets/Editor/World/WorldLabSoakChecks.cs#L28) | public static |
| WorldLabSoakChecks | [`void Tick()`](../Assets/Editor/World/WorldLabSoakChecks.cs#L55) | private static |
| WorldLabSoakChecks | [`void Stop()`](../Assets/Editor/World/WorldLabSoakChecks.cs#L138) | public static |
| WorldLabSoakChecks | [`void Finish(bool complete)`](../Assets/Editor/World/WorldLabSoakChecks.cs#L147) | private static |
| WorldLabSoakChecks | [`void WriteCheckpoint()`](../Assets/Editor/World/WorldLabSoakChecks.cs#L162) | private static |

## Assets/Editor/World/WorldPlayerBuildChecks.cs

// 职责：构建D3D11发布验证包或独立Development制作工具验证包，结果写Temp，不覆盖用户发布包。 // 入口：Build()/BuildAuthoring()从已保存Main的Edit Mode调用，分别写Builds下两个验证目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldPlayerBuildChecks | [`void Build()`](../Assets/Editor/World/WorldPlayerBuildChecks.cs#L13) | public static |
| WorldPlayerBuildChecks | [`void BuildNow()`](../Assets/Editor/World/WorldPlayerBuildChecks.cs#L25) | private static |
| WorldPlayerBuildChecks | [`void BuildAuthoring()`](../Assets/Editor/World/WorldPlayerBuildChecks.cs#L33) | public static |
| WorldPlayerBuildChecks | [`void BuildAuthoringNow()`](../Assets/Editor/World/WorldPlayerBuildChecks.cs#L44) | private static |
| WorldPlayerBuildChecks | [`void BuildPlayer(string output, string reportPath, BuildOptions options)`](../Assets/Editor/World/WorldPlayerBuildChecks.cs#L52) | private static |

## Assets/Editor/World/WorldSessionAuthoring.cs

// 职责：创建可编辑世界配置与会话 Prefab，并将后台预热及加载界面接入 Main。 // 入口：Install()；重复执行保留配置、Prefab 参数及已存在的界面布局。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionAuthoring | [`void Install()`](../Assets/Editor/World/WorldSessionAuthoring.cs#L17) | public static |
| WorldSessionAuthoring | [`bool IsInEnvironment(Transform item)`](../Assets/Editor/World/WorldSessionAuthoring.cs#L104) | private static |
| WorldSessionAuthoring | [`GameObject CreatePrefab(WorldGenerationSettings configuration)`](../Assets/Editor/World/WorldSessionAuthoring.cs#L117) | private static |
| WorldSessionAuthoring | [`WorldLoadingPanel CreateLoadingPanel(Scene scene)`](../Assets/Editor/World/WorldSessionAuthoring.cs#L174) | private static |
| WorldSessionAuthoring | [`RectTransform Rect( string name, Transform parent, Vector2 minimum, Vector2 maximum, Vector2 position, Vector2 size)`](../Assets/Editor/World/WorldSessionAuthoring.cs#L282) | private static |
| WorldSessionAuthoring | [`Text Label( string name, Transform parent, string value, Font font, Vector2 position, Vector2 size, float fontSize, Color color)`](../Assets/Editor/World/WorldSessionAuthoring.cs#L299) | private static |
| WorldSessionAuthoring | [`T FindInScene(Scene scene)`](../Assets/Editor/World/WorldSessionAuthoring.cs#L326) | private static |
| WorldSessionAuthoring | [`void SelectConfiguration()`](../Assets/Editor/World/WorldSessionAuthoring.cs#L332) | public static |
| WorldSessionAuthoring | [`void RegeneratePreview()`](../Assets/Editor/World/WorldSessionAuthoring.cs#L338) | public static |

## Assets/Editor/World/WorldSessionEdgeChecks.cs

// 职责：用运行时配置副本验证建筑预算延期、高速区块等待及脱锚建筑休眠恢复。 // 入口：Run()；只写Temp报告，不保存场景、配置资产或玩家存档，结束后退出Play Mode。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionEdgeChecks | [`void Run()`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L45) | public static |
| WorldSessionEdgeChecks | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L59) | private static |
| WorldSessionEdgeChecks | [`void Tick()`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L81) | private static |
| WorldSessionEdgeChecks | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L107) | private static |
| WorldSessionEdgeChecks | [`void Check(bool condition, string description)`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L115) | private static |
| WorldSessionEdgeChecks | [`IEnumerator Scenario()`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L125) | private static |
| WorldSessionEdgeChecks | [`void BindProfile(WorldGenerationSettings value)`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L306) | private static |
| WorldSessionEdgeChecks | [`void ConfigureProfile(int loadRadius, int unloadRadius, float buildingChance)`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L313) | private static |
| WorldSessionEdgeChecks | [`void CreatePlayer()`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L327) | private static |
| WorldSessionEdgeChecks | [`Vector3 ReadGuardVector(object guard, string field)`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L359) | private static |
| WorldSessionEdgeChecks | [`void InvokeSessionPhysics()`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L364) | private static |
| WorldSessionEdgeChecks | [`void CheckSessionHealthy(double timeout, string operation)`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L369) | private static |
| WorldSessionEdgeChecks | [`void Finish(bool exitPlayMode)`](../Assets/Editor/World/WorldSessionEdgeChecks.cs#L377) | private static |

## Assets/Editor/World/WorldSessionPerformanceChecks.cs

// 职责：在真实 Main Play Mode 采样固定种子的准备、切换、静止和跨区块帧耗时。 // 入口：Run(label)；只使用运行时配置副本和测试载具，不保存玩家数据。 // 生命周期：临时启用后台帧推进；完成、中止和超时均恢复后台运行与原Profiler配置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionPerformanceChecks | [`void Run(string label)`](../Assets/Editor/World/WorldSessionPerformanceChecks.cs#L35) | public static |
| WorldSessionPerformanceChecks | [`void OnState(PlayModeStateChange state)`](../Assets/Editor/World/WorldSessionPerformanceChecks.cs#L52) | private static |
| WorldSessionPerformanceChecks | [`void Tick()`](../Assets/Editor/World/WorldSessionPerformanceChecks.cs#L91) | private static |
| WorldSessionPerformanceChecks | [`void Finish(bool exitPlayMode, bool completed)`](../Assets/Editor/World/WorldSessionPerformanceChecks.cs#L126) | private static |
| WorldSessionPerformanceChecks | [`void Next(string name)`](../Assets/Editor/World/WorldSessionPerformanceChecks.cs#L152) | private static |
| WorldSessionPerformanceChecks | [`void WriteCpuSamples()`](../Assets/Editor/World/WorldSessionPerformanceChecks.cs#L170) | private static |
| WorldSessionPerformanceChecks | [`IEnumerator Scenario()`](../Assets/Editor/World/WorldSessionPerformanceChecks.cs#L197) | private static |

## Assets/Editor/World/WorldSessionPlayChecks.cs

// 职责：在真实 Main Play Mode 验证后台预热、流式卸载、会话内修改及退出清空。 // 入口：Run()；只操作运行时测试对象，报告写 Temp/WorldSession，不保存玩家数据或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionPlayChecks | [`void Run()`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L36) | public static |
| WorldSessionPlayChecks | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L50) | private static |
| WorldSessionPlayChecks | [`void Tick()`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L70) | private static |
| WorldSessionPlayChecks | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L96) | private static |
| WorldSessionPlayChecks | [`void Check(bool condition, string description)`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L104) | private static |
| WorldSessionPlayChecks | [`IEnumerator Scenario()`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L114) | private static |
| WorldSessionPlayChecks | [`IEnumerator StreamingChecks(WorldGenerationSettings settings)`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L409) | private static |
| WorldSessionPlayChecks | [`bool NeighborhoodReady(ProceduralVoxelWorld world, Vector2Int center)`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L517) | private static |
| WorldSessionPlayChecks | [`void Finish(bool exitPlayMode)`](../Assets/Editor/World/WorldSessionPlayChecks.cs#L533) | private static |

## Assets/Editor/World/WorldTerrainIntegrationChecks.cs

// 职责：验证真实建筑库的旋转占地支撑、自然边缘恢复、密度与确定性；保存可复查报告。 // 入口：Run()在Edit Mode执行；不修改用户蓝图、场景或配置资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldTerrainIntegrationChecks | [`string Run()`](../Assets/Editor/World/WorldTerrainIntegrationChecks.cs#L13) | public static |
| WorldTerrainIntegrationChecks | [`int CountSites(WorldGenerationSettings settings, List<BlockDataList> templates)`](../Assets/Editor/World/WorldTerrainIntegrationChecks.cs#L103) | private static |
| WorldTerrainIntegrationChecks | [`void Require(bool valid, string message)`](../Assets/Editor/World/WorldTerrainIntegrationChecks.cs#L121) | private static |

## Assets/Scripts/Features/Bootstrap/GameManager.cs

// 职责：保存当前方块父对象，启动时初始化界面与默认存档加载。 // 入口：Init()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GameManager | [`void Awake()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L9) | private |
| GameManager | [`void Start()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L15) | private |
| GameManager | [`void Init()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L20) | public static |

## Assets/Scripts/Features/Bots/Bot.cs

// 职责：保存无人机任务/插槽配置并组织任务飞行、返航、离家和精确停靠状态。 // 入口：派生机器人的 FixedUpdate → TickNavigation() → NavigateToTarget()/NavigateHomeSmoothly()。 // 依赖：BotFlightMotion 处理飞行/制动；BotObstacleAvoidance 负责三维净空与脱困。 // 不变量：homeOffset 保持 Home 局部坐标；停靠切为运动学刚体，完成后恢复准确局部姿态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Bot | [`void Start()`](../Assets/Scripts/Features/Bots/Bot.cs#L52) | protected virtual |
| Bot | [`bool TickNavigation()`](../Assets/Scripts/Features/Bots/Bot.cs#L79) | protected |
| Bot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/Bot.cs#L93) | protected virtual |
| Bot | [`void OnDestroy()`](../Assets/Scripts/Features/Bots/Bot.cs#L112) | protected virtual |
| Bot | [`Power SetHomePowerRequested(bool requested)`](../Assets/Scripts/Features/Bots/Bot.cs#L118) | protected |
| Bot | [`void PrepareForHomeDestruction()`](../Assets/Scripts/Features/Bots/Bot.cs#L131) | public virtual |
| Bot | [`void InitializeTrail()`](../Assets/Scripts/Features/Bots/Bot.cs#L135) | private |
| Bot | [`void LateUpdate()`](../Assets/Scripts/Features/Bots/Bot.cs#L155) | private |
| Bot | [`void NavigateToTarget(Transform target)`](../Assets/Scripts/Features/Bots/Bot.cs#L168) | protected |
| Bot | [`void NavigateHomeSmoothly()`](../Assets/Scripts/Features/Bots/Bot.cs#L188) | protected |
| Bot | [`Vector3 GetRelativeHomeVelocity(Vector3 worldPosition)`](../Assets/Scripts/Features/Bots/Bot.cs#L225) | private |
| Bot | [`void ReturnHome()`](../Assets/Scripts/Features/Bots/Bot.cs#L231) | private |
| Bot | [`void LeaveHome()`](../Assets/Scripts/Features/Bots/Bot.cs#L273) | private |
| Bot | [`void SetNavigationState(NavigationState nextState)`](../Assets/Scripts/Features/Bots/Bot.cs#L285) | private |
| Bot | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Bots/Bot.cs#L310) | private |

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
| BotFlightMotion | [`void ResetDirection()`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L30) | public |
| BotFlightMotion | [`void ClearTarget()`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L35) | public |
| BotFlightMotion | [`void NavigateToTarget(Transform target)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L43) | public |
| BotFlightMotion | [`void NavigateHome(Transform home, Vector3 approachPosition, Rigidbody homeRigidbody, float rangeScale)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L48) | public |
| BotFlightMotion | [`void NavigateToPosition( Vector3 targetPosition, Transform targetReference, Rigidbody homeRigidbody, bool returningHome, float avoidanceRangeScale)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L53) | private |
| BotFlightMotion | [`void SampleGuidance( Transform targetReference, Vector3 targetDirection, float targetDistance, float avoidanceRangeScale)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L71) | private |
| BotFlightMotion | [`void UpdateDirectionAndSpeed(Vector3 targetDirection)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L96) | private |
| BotFlightMotion | [`void ApplyMovement( Vector3 targetPosition, Vector3 targetDirection, float targetDistance, Rigidbody homeRigidbody, bool returningHome)`](../Assets/Scripts/Features/Bots/BotFlightMotion.cs#L114) | private |

## Assets/Scripts/Features/Bots/BotNavigationGizmos.cs

// 职责：绘制最近一次避障采样、导航方向、速度和状态；仅显示，不修改机器人状态。 // 入口：Bot.OnDrawGizmos()；UnityEditor Handles 只在 Editor 编译分支使用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationGizmos | [`void Draw( Transform transform, Transform navigateTarget, Rigidbody rb, Bot.NavigationState currentState, BotFlightMotion motion, bool showAvoidanceRays, bool showDirectionVectors)`](../Assets/Scripts/Features/Bots/BotNavigationGizmos.cs#L7) | public static |

## Assets/Scripts/Features/Bots/BotNavigationPolicy.cs

// 职责：定义维修/回收 Bot 共用且不可由 Inspector/CSV 覆盖的导航规则。 // 契约：速度没有固定上限；只限制加速度、可制动距离及转弯净空，单位为米和秒。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationPolicy | [`BotAvoidanceSettings CreateAvoidanceSettings()`](../Assets/Scripts/Features/Bots/BotNavigationPolicy.cs#L19) | public static |
| BotNavigationPolicy | [`void ConfigureBody(Rigidbody body)`](../Assets/Scripts/Features/Bots/BotNavigationPolicy.cs#L35) | public static |

## Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs

// 职责：用机器人半径探测三维净空，保持绕障方向，并在没有目标进展时选择脱困路线。 // 入口：Evaluate() 采样导航，ConstrainVelocity() 在每个物理帧阻止继续朝近处障碍加速。 // 生命周期：每台机器人拥有独立缓冲和绕障记忆；切换目标时 Reset() 清理旧路线。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotObstacleAvoidance | [`void Reset()`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L50) | public |
| BotObstacleAvoidance | [`void ClearDebugHits()`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L63) | public |
| BotObstacleAvoidance | [`BotAvoidanceResult Evaluate( Transform target, Vector3 velocity, Vector3 targetDirection, float rangeScale, BotAvoidanceSettings settings, float targetDistance = float.PositiveInfinity)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L69) | public |
| BotObstacleAvoidance | [`bool CheckProgress(float targetDistance)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L166) | private |
| BotObstacleAvoidance | [`Vector3 ChooseClearDirection(Vector3 targetDirection, float range, bool stalled, out float bestClearDistance)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L186) | private |
| BotObstacleAvoidance | [`void ConsiderDirection( Vector3 candidate, Vector3 targetDirection, float range, bool stalled, ref float bestScore, ref Vector3 bestDirection, ref float bestClearDistance)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L234) | private |
| BotObstacleAvoidance | [`Vector3 ConstrainVelocity(Vector3 desiredVelocity, float deltaTime, Vector3 currentVelocity, float brakingAcceleration)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L258) | public |
| BotObstacleAvoidance | [`Vector3 LimitApproachSpeed(Vector3 desiredVelocity, Vector3 corridor, float lookAhead, float margin, float acceleration)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L285) | private |
| BotObstacleAvoidance | [`float GetClearDistance(Vector3 direction, float range)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L302) | private |
| BotObstacleAvoidance | [`bool Cast(Vector3 direction, float range, out RaycastHit nearest)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L307) | private |
| BotObstacleAvoidance | [`bool TryGetOverlapEscape(out Vector3 escape)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L340) | private |
| BotObstacleAvoidance | [`bool IsIgnored(Collider collider)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L395) | private |
| BotObstacleAvoidance | [`bool ShouldIgnoreCollider(Collider collider)`](../Assets/Scripts/Features/Bots/BotObstacleAvoidance.cs#L411) | private |

## Assets/Scripts/Features/Bots/CollectionBot.cs

// 职责：执行掉落物寻找、拾取、运输和入库，复用 Bot 导航。 // 入口：FixedUpdate() → 核对任务 → 寻找 / 拾取 / 返航交付。 // 生命周期：停用、销毁、Home 摧毁或任务失效时释放 LootDrop 认领；停靠后才入库。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CollectionBot | [`void Start()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L22) | protected override |
| CollectionBot | [`void FixedUpdate()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L31) | private |
| CollectionBot | [`bool IsMissionInvalid(ControlUnit owner, bool canWork)`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L77) | private |
| CollectionBot | [`void CollectTarget()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L82) | private |
| CollectionBot | [`void ReturnCargo()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L91) | private |
| CollectionBot | [`void FindTarget(ControlUnit owner)`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L106) | private |
| CollectionBot | [`void Abandon()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L143) | private |
| CollectionBot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L154) | protected override |
| CollectionBot | [`void OnDestroy()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L160) | protected override |
| CollectionBot | [`void PrepareForHomeDestruction()`](../Assets/Scripts/Features/Bots/CollectionBot.cs#L166) | public override |

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
| RepairBot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L91) | protected override |
| RepairBot | [`void FindDamagedBlock()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L97) | private |
| RepairBot | [`void CheckAndRepair()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L110) | private |
| RepairBot | [`void UpdateRepairBeam(bool active)`](../Assets/Scripts/Features/Bots/RepairBot.cs#L151) | private |
| RepairBot | [`void EnsureRepairBeamGradient()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L156) | private |
| RepairBot | [`void ClearTarget()`](../Assets/Scripts/Features/Bots/RepairBot.cs#L169) | public |

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
| BlockCsvDocument | [`string Normalize(BlockDataTableSchema.Column column, string value, string name)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L174) | private static |
| BlockCsvDocument | [`string Quote(string text)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L219) | private static |
| BlockCsvDocument | [`string WriteRecords(IEnumerable<IEnumerable<string>> records)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L225) | public static |
| BlockCsvDocument | [`List<List<string>> ParseRecords(string text)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L237) | public static |

## Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs

// 职责：定义 CSV 支持的配置字段、类型、枚举和有效范围，Editor 与运行时共享同一契约。 // 入口：CreateColumns()；不包含运行时状态或资源引用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Column | [`Component[] FindTargets(GameObject root)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L31) | public |
| BlockDataTableSchema | [`List<Column> CreateColumns()`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L66) | public static |
| BlockDataTableSchema | [`void AddFloat( List<Column> columns, string group, string property, string label, double minimum = 0, double maximum = double.PositiveInfinity)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L116) | private static |
| BlockDataTableSchema | [`void AddInteger( List<Column> columns, string group, string property, string label, double minimum = 0, double maximum = double.PositiveInfinity)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L135) | private static |
| BlockDataTableSchema | [`void AddBoolean(List<Column> columns, string group, string property, string label)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L154) | private static |
| BlockDataTableSchema | [`void AddEnum(List<Column> columns, string group, string property, string label, string[] names)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L167) | private static |

## Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs

// 职责：缓存加载过的方块预制体，避免 Resources.Load() 重复加载相同资源。 // 入口：GetPrefab()；支持清空缓存。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockPrefabCache | [`GameObject GetPrefab(string resourcePath)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs#L13) | public static |
| BlockPrefabCache | [`void Clear()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs#L37) | public static |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs

// 职责：从存档恢复一个方块的身份、尺寸、阵营、货仓和耐久。 // 入口：TryRestore()；只创建实体，不负责加载会话、统计、UI 或 JSON 写入。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintBlockFactory | [`bool TryRestore(BlockData data, Transform parent, UnitFaction faction, out Block block)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7) | public static |
| BlueprintBlockFactory | [`void ApplyFaction(Block block, UnitFaction faction)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L56) | public static |
| BlueprintBlockFactory | [`string ToResourcePath(string fullPath)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L64) | public static |

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
| BlueprintLoader | [`void Load( BuildTargetContext target, GameObject parentPrefab, BlueprintLoadSettings settings, Camera camera, Action createInitialBlock, Action<Transform, BlockDataList> prepareParent = null)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L43) | public |
| BlueprintLoader | [`bool IsLoadingTarget(BuildTargetContext target)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L88) | public |
| BlueprintLoader | [`void Cancel()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L94) | public |
| BlueprintLoader | [`IEnumerator LoadRoutine(int version, List<BlockData> blocks, BlueprintLoadSettings settings, Action createInitialBlock)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L107) | private |
| BlueprintLoader | [`bool TryRead(string path, out BlockDataList data)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L189) | private |
| BlueprintLoader | [`void ReplaceParent(GameObject prefab, string name)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L228) | private |
| BlueprintLoader | [`bool OwnsParent(int version)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L249) | private |
| BlueprintLoader | [`void RemoveMissingData(HashSet<string> ids)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L254) | private |
| BlueprintLoader | [`IEnumerator RebuildConnections(int version)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265) | private |
| BlueprintLoader | [`void ReleaseInvalidParent()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L283) | private |
| BlueprintLoader | [`void FinishSession()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L303) | private |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs

// 职责：统一蓝图完整刷新与增量恢复的质量、耗电和发电统计。 // 入口：Accumulate() / Calculate()；不读取管理器单例，不更新 UI。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintStatistics | [`void Accumulate(Block block)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs#L26) | public |
| BlueprintStatistics | [`BlueprintStatistics Calculate(List<BlockData> savedBlocks, Transform parent)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs#L47) | public static |

## Assets/Scripts/Features/Building/BlueprintModelPreview.cs

// 职责：复制蓝图模块的静态模型与材质，显示完整外形而不实例化战斗、连接或物理脚本。 // 入口：Create()；返回根对象由调用者销毁，网格和材质共享原资源，不拥有资源本体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintModelPreview | [`GameObject Create(IReadOnlyList<BlockData> blocks, Transform parent, string name)`](../Assets/Scripts/Features/Building/BlueprintModelPreview.cs#L8) | public static |
| BlueprintModelPreview | [`void CopyVisuals(Transform source, Transform target, bool root)`](../Assets/Scripts/Features/Building/BlueprintModelPreview.cs#L31) | private static |

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

// 职责：协调建造输入、选择与放置；蓝图加载委托 Building 功能，保留现有 Inspector 契约。 // 入口：SetBuildMode() / ToggleEnemyBlueprintBuildMode() / ToggleBuildingBlueprintBuildMode()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildManager | [`void Awake()`](../Assets/Scripts/Features/Building/BuildManager.cs#L75) | private |
| BuildManager | [`void OnDisable()`](../Assets/Scripts/Features/Building/BuildManager.cs#L80) | private |
| BuildManager | [`void OnDestroy()`](../Assets/Scripts/Features/Building/BuildManager.cs#L85) | private |
| BuildManager | [`void Start()`](../Assets/Scripts/Features/Building/BuildManager.cs#L102) | private |
| BuildManager | [`void Update()`](../Assets/Scripts/Features/Building/BuildManager.cs#L108) | private |
| BuildManager | [`void SetBuildMode(bool lockView)`](../Assets/Scripts/Features/Building/BuildManager.cs#L166) | public |
| BuildManager | [`void ToggleEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Building/BuildManager.cs#L178) | public |
| BuildManager | [`void EnterEnemyBlueprintBuildMode(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L190) | public |
| BuildManager | [`void ExitEnemyBlueprintBuildMode(bool reloadPlayerSave)`](../Assets/Scripts/Features/Building/BuildManager.cs#L220) | public |
| BuildManager | [`void ExitDeveloperBlueprintBuildMode(bool reloadPlayerSave)`](../Assets/Scripts/Features/Building/BuildManager.cs#L228) | public |
| BuildManager | [`void ToggleBuildingBlueprintBuildMode()`](../Assets/Scripts/Features/Building/BuildManager.cs#L247) | public |
| BuildManager | [`void EnterBuildingBlueprintBuildMode(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L259) | public |
| BuildManager | [`void PrepareBuildingFoundation(Transform parent, BlockDataList data)`](../Assets/Scripts/Features/Building/BuildManager.cs#L280) | private |
| BuildManager | [`bool ResizeBuildingFoundation(int width, int length, out string reason)`](../Assets/Scripts/Features/Building/BuildManager.cs#L303) | public |
| BuildManager | [`void SetCurrentBuildingBlueprintName(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L351) | public |
| BuildManager | [`void SetCurrentBlockResource(string resourcePath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L360) | public |
| BuildManager | [`void SetCurrentSaveName(string saveName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L375) | public |
| BuildManager | [`void SetCurrentEnemyBlueprintName(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L380) | public |
| BuildManager | [`void SetBuildType(BuildType buildType)`](../Assets/Scripts/Features/Building/BuildManager.cs#L385) | public |
| BuildManager | [`void EnsureBuildInteraction()`](../Assets/Scripts/Features/Building/BuildManager.cs#L412) | private |
| BuildManager | [`void HandleBuildingPreview()`](../Assets/Scripts/Features/Building/BuildManager.cs#L439) | private |
| BuildManager | [`void ClearCurrentGhost(bool hideHints = true)`](../Assets/Scripts/Features/Building/BuildManager.cs#L447) | private |
| BuildManager | [`void DeselectBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L455) | public |
| BuildManager | [`Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)`](../Assets/Scripts/Features/Building/BuildManager.cs#L462) | public |
| BuildManager | [`bool IsWithinBuildRange(Vector3 position)`](../Assets/Scripts/Features/Building/BuildManager.cs#L468) | public |
| BuildManager | [`void CreateBlock(GameObject prefab, string resourcePath, Vector3 pos, Quaternion rot)`](../Assets/Scripts/Features/Building/BuildManager.cs#L473) | public |
| BuildManager | [`void DeleteBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L500) | public |
| BuildManager | [`void EnsureBlueprintEditor()`](../Assets/Scripts/Features/Building/BuildManager.cs#L521) | private |
| BuildManager | [`Block CreateDuplicateBlock(GameObject prefab, string path, Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/Building/BuildManager.cs#L530) | private |
| BuildManager | [`void SaveBlock(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L537) | public |
| BuildManager | [`void RemoveBlock(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L555) | public |
| BuildManager | [`void LoadAllBlocks()`](../Assets/Scripts/Features/Building/BuildManager.cs#L563) | public |
| BuildManager | [`BlueprintStatistics GetBlueprintStatistics()`](../Assets/Scripts/Features/Building/BuildManager.cs#L587) | public |
| BuildManager | [`void EnsureBlueprintLoader()`](../Assets/Scripts/Features/Building/BuildManager.cs#L594) | private |
| BuildManager | [`void DisplayBlueprintStatistics(BlueprintStatistics statistics)`](../Assets/Scripts/Features/Building/BuildManager.cs#L603) | private |
| BuildManager | [`BlueprintLoadSettings CreateLoadSettings()`](../Assets/Scripts/Features/Building/BuildManager.cs#L611) | private |
| BuildManager | [`void ClearUnloadableData(string id)`](../Assets/Scripts/Features/Building/BuildManager.cs#L629) | public |
| BuildManager | [`bool IsLoadingBuildTarget( BuildTargetKind kind, string saveName, string enemyBlueprintName, string buildingBlueprintName = null)`](../Assets/Scripts/Features/Building/BuildManager.cs#L634) | public |
| BuildManager | [`bool IsLoadingBuildTarget(BuildTargetContext context)`](../Assets/Scripts/Features/Building/BuildManager.cs#L644) | private |
| BuildManager | [`void ClearUnloadableData(string id, string targetSavePath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L649) | private |
| BuildManager | [`void InitialBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L658) | private |
| BuildManager | [`bool CanCreateBlock(GameObject prefab, out string reason)`](../Assets/Scripts/Features/Building/BuildManager.cs#L666) | private |
| BuildManager | [`void ApplyBlockBuildDefaults(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L696) | public |
| BuildManager | [`int CountCockpitsInCurrentConstruct()`](../Assets/Scripts/Features/Building/BuildManager.cs#L701) | private |
| BuildManager | [`void ResetBuildState()`](../Assets/Scripts/Features/Building/BuildManager.cs#L711) | private |
| BuildManager | [`void SetBuildTarget(BuildTargetContext context)`](../Assets/Scripts/Features/Building/BuildManager.cs#L721) | private |
| BuildManager | [`void RefreshBlueprintUI()`](../Assets/Scripts/Features/Building/BuildManager.cs#L739) | private |
| BuildManager | [`void StopActiveBlockLoad()`](../Assets/Scripts/Features/Building/BuildManager.cs#L747) | private |
| BuildManager | [`void CancelBlueprintLoad()`](../Assets/Scripts/Features/Building/BuildManager.cs#L752) | public |
| BuildManager | [`string ConvertToResourcesPath(string fullPath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L757) | public static |

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

// 职责：定义玩家、敌方和建筑蓝图目标、名称、阵营及各自独立的存档路径。 // 入口：GetSavePath() / PlayerSave() / EnemyBlueprint()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildTargetContext | [`string GetSavePath(SaveManager saveManager)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L34) | public |
| BuildTargetContext | [`BuildTargetContext PlayerSave(string saveName, string enemyBlueprintName)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L39) | public static |
| BuildTargetContext | [`BuildTargetContext EnemyBlueprint(string saveName, string enemyBlueprintName)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L44) | public static |
| BuildTargetContext | [`BuildTargetContext BuildingBlueprint(string saveName, string enemyBlueprintName, string buildingBlueprintName)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L49) | public static |
| BuildTargetContext | [`string NormalizeName(string name, string fallback)`](../Assets/Scripts/Features/Building/BuildTargetContext.cs#L54) | private static |

## Assets/Scripts/Features/Building/BuildingEditorPanel.cs

// 职责：建筑编辑的可编辑 uGUI 面板，提交底座整数尺寸并显示结果。 // 入口：Refresh() / ApplySize()；Prefab 由 BuildingEditorAssets 制作，不改动主场景手工布局。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingEditorPanel | [`void Refresh()`](../Assets/Scripts/Features/Building/BuildingEditorPanel.cs#L16) | public |
| BuildingEditorPanel | [`void ApplySize()`](../Assets/Scripts/Features/Building/BuildingEditorPanel.cs#L25) | public |

## Assets/Scripts/Features/Building/BuildingFoundation.cs

// 职责：配置建筑薄平面底座、碰撞体及每格向上连接点；底座不进入普通方块存档或操作栈。 // 入口：Configure()；网格原点固定在 (-2, 0, -2)，扩缩尺寸不移动既有建筑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingFoundation | [`void Configure(BuildingFoundationData data)`](../Assets/Scripts/Features/Building/BuildingFoundation.cs#L15) | public |
| BuildingFoundation | [`void RefreshGeometry(Block block)`](../Assets/Scripts/Features/Building/BuildingFoundation.cs#L46) | internal |
| BuildingFoundation | [`void OnDestroy()`](../Assets/Scripts/Features/Building/BuildingFoundation.cs#L76) | private |

## Assets/Scripts/Features/Building/BuildingFoundationData.cs

// 职责：保存建筑底座的网格尺寸；缺省为 4×4，不改变原有方块存档坐标。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Building/BuildingOutpostEditor.cs

// 职责：在建筑编辑面板配置据点类型、敌机蓝图快照和逐名驻军本地部署姿态。 // 入口：Refresh() / AddDefender() / SavePose() / RemoveDefender() / SetOutpost()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingOutpostEditor | [`void Refresh()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L35) | public |
| BuildingOutpostEditor | [`void AddTemplate(string name, string json)`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L76) | private |
| BuildingOutpostEditor | [`void CreateMatchingBase()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L96) | public |
| BuildingOutpostEditor | [`void CopyBuildingTemplate()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L117) | public |
| BuildingOutpostEditor | [`void RefreshSlots(EnemyOutpostData data)`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L135) | private |
| BuildingOutpostEditor | [`EnemyOutpostData Copy()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L152) | private |
| BuildingOutpostEditor | [`void SetOutpost()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L163) | public |
| BuildingOutpostEditor | [`void AddDefender()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L180) | public |
| BuildingOutpostEditor | [`void SelectDefender()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L210) | public |
| BuildingOutpostEditor | [`void SavePose()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L233) | public |
| BuildingOutpostEditor | [`bool Parse(InputField field, out float value)`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L253) | private static |
| BuildingOutpostEditor | [`void RemoveDefender()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L258) | public |
| BuildingOutpostEditor | [`bool Save(EnemyOutpostData data)`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L268) | private |
| BuildingOutpostEditor | [`void ShowPreview(EnemyOutpostData data)`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L290) | private |
| BuildingOutpostEditor | [`void ClearPreview()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L300) | private |
| BuildingOutpostEditor | [`void OnDisable()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L309) | private |
| BuildingOutpostEditor | [`void LateUpdate()`](../Assets/Scripts/Features/Building/BuildingOutpostEditor.cs#L314) | private |

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

// 职责：处理所选方块的键盘移动/旋转、轴拖拽及复制，提交存档和 Undo 动作。 // 入口：Poll() / TryBeginDrag() / CancelDrag()；旋转锁定整数格锚点，尺寸半格偏移随姿态换轴。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildManipulation | [`void Configure(Camera camera, GameObject move, GameObject rotate)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L53) | public |
| BuildManipulation | [`void Poll(Block block, BuildType type)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L60) | public |
| BuildManipulation | [`bool TryBeginDrag(RaycastHit hit, Block block, BuildType type)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L81) | public |
| BuildManipulation | [`void CancelDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L120) | public |
| BuildManipulation | [`void AlignAxisToNearestWorldDir()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L126) | private |
| BuildManipulation | [`void HandleMovement()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L171) | private |
| BuildManipulation | [`void HandleRotation()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L241) | private |
| BuildManipulation | [`void HandleMoveAxisDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L303) | private |
| BuildManipulation | [`void HandleRotateAxisDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L364) | private |
| BuildManipulation | [`void HandleDuplicate(Vector3 newPos, Quaternion newRot)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L416) | private |

## Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs

// 职责：集中建造尺寸、连接点对齐、旋转网格锚点、半网格吸附与阻挡几何。 // 入口：CalculateRotationPosition() / SnapRotationToGrid() / TryCalculateConnectorAlignedPosition() / IsBlocked()。 // 依赖：显式网格尺寸和方块层；无管理器单例、UI 或存档依赖。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPlacementGeometry | [`Quaternion SnapRotationToGrid(Quaternion rotation)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L10) | public |
| BuildPlacementGeometry | [`Vector3 CalculateRotationPosition(Vector3 startCenter, Quaternion startRotation, Quaternion targetRotation, Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L19) | public |
| BuildPlacementGeometry | [`Vector3 GetBlockHalfExtents(Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L43) | public |
| BuildPlacementGeometry | [`float GetHalfExtentAlongDirection(Block block, Quaternion rotation, Vector3 worldDirection)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L54) | public |
| BuildPlacementGeometry | [`bool TryCalculateConnectorAlignedPosition( Block prefabBlock, Quaternion previewRotation, Vector3 targetConnectorPosition, Vector3 targetConnectorNormal, out Vector3 position)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L64) | public |
| BuildPlacementGeometry | [`Vector3 RemoveGridNoise(Vector3 position)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L109) | private |
| BuildPlacementGeometry | [`Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L126) | public |
| BuildPlacementGeometry | [`bool IsBlocked(Vector3 targetCenter, Quaternion targetRotation, Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L171) | public |
| BuildPlacementGeometry | [`float GetMoveStep(Block block, Vector3 moveDir)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L198) | public |

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

## Assets/Scripts/Features/Building/OutpostBaseDesigner.cs

// 职责：按敌机模块轮廓制作可编辑停机底座，敌机与建筑使用同一蓝图局部坐标。 // 入口：Create()生成独立组合蓝图；BoundsOf()用于尺寸适配、净空与验证。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostBaseDesigner | [`Bounds BoundsOf(IReadOnlyList<BlockData> blocks, Quaternion rotation)`](../Assets/Scripts/Features/Building/OutpostBaseDesigner.cs#L9) | public static |
| OutpostBaseDesigner | [`BlockDataList Create(BlockDataList enemy, string name)`](../Assets/Scripts/Features/Building/OutpostBaseDesigner.cs#L37) | public static |
| OutpostBaseDesigner | [`void Add(BlockDataList data, string resource, Vector3 position)`](../Assets/Scripts/Features/Building/OutpostBaseDesigner.cs#L133) | private static |

## Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs

// 职责：在建筑旁展示完整敌机模型、部署线框、朝向及保守包围盒重叠提示。 // 入口：构造后读取Hint；Dispose释放显示对象与临时网格/材质，不实例化敌机玩法组件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostDeploymentPreview | [`void DrawGuard(OutpostGarrisonData guard, Bounds bounds, int index, bool selected, Color color)`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L91) | private |
| OutpostDeploymentPreview | [`void FaceLabels(Quaternion rotation)`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L136) | internal |
| OutpostDeploymentPreview | [`bool OverlapsBuilding(OutpostGarrisonData guard, Matrix4x4 pose, List<BlockData> building)`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L144) | private static |
| OutpostDeploymentPreview | [`Bounds CalculateBounds(List<BlockData> blocks, Matrix4x4 deployment)`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L164) | private static |
| OutpostDeploymentPreview | [`Matrix4x4 BlockTransform(BlockData block)`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L189) | private static |
| OutpostDeploymentPreview | [`Vector3 Corner(int index)`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L197) | private static |
| OutpostDeploymentPreview | [`void AddBox(List<Vector3> vertices, List<int> indices, Matrix4x4 transform)`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L202) | private static |
| OutpostDeploymentPreview | [`void AddLine(List<Vector3> vertices, List<int> indices, Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L216) | private static |
| OutpostDeploymentPreview | [`void Dispose()`](../Assets/Scripts/Features/Building/OutpostDeploymentPreview.cs#L224) | public |

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

// 职责：管理玩家/敌方/建筑蓝图目录、名称和缓存，提供加载/复制/重命名入口。 // 入口：EnsureSaveDirectories() / GetAllSaveNames() / GetAllEnemyBlueprintNames()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SaveManager | [`BlueprintFileStore FilesFor(BuildTargetKind kind)`](../Assets/Scripts/Features/Building/SaveManager.cs#L20) | private |
| SaveManager | [`void Awake()`](../Assets/Scripts/Features/Building/SaveManager.cs#L38) | private |
| SaveManager | [`void OnDestroy()`](../Assets/Scripts/Features/Building/SaveManager.cs#L52) | private |
| SaveManager | [`void EnsureSaveDirectories()`](../Assets/Scripts/Features/Building/SaveManager.cs#L60) | public |
| SaveManager | [`void GetAllBuildingBlueprintNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L74) | public |
| SaveManager | [`void CreateNewBuildingBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L80) | public |
| SaveManager | [`void LoadBuildingBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L96) | public |
| SaveManager | [`void DeleteBuildingBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L101) | public |
| SaveManager | [`bool RenameBuildingBlueprint(string oldName, string newName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L112) | public |
| SaveManager | [`string GetBuildingBlueprintPath(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L117) | public |
| SaveManager | [`void SaveBuildingFoundation(BuildingFoundationData foundation)`](../Assets/Scripts/Features/Building/SaveManager.cs#L122) | public |
| SaveManager | [`void GetAllSaveNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L137) | public |
| SaveManager | [`void SaveBuildingOutpost(EnemyOutpostData outpost)`](../Assets/Scripts/Features/Building/SaveManager.cs#L147) | public |
| SaveManager | [`string CopyBundledBuilding(string template)`](../Assets/Scripts/Features/Building/SaveManager.cs#L172) | public |
| SaveManager | [`string SaveNewBuilding(string template, BlockDataList data)`](../Assets/Scripts/Features/Building/SaveManager.cs#L189) | public |
| SaveManager | [`void GetAllEnemyBlueprintNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L215) | public |
| SaveManager | [`void CreateNewSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L225) | public |
| SaveManager | [`void CreateNewEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L243) | public |
| SaveManager | [`void LoadSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L256) | public |
| SaveManager | [`void LoadEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L286) | public |
| SaveManager | [`void DeleteSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L314) | public |
| SaveManager | [`void DeleteEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L326) | public |
| SaveManager | [`bool RenameSave(string oldSaveName, string newSaveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L338) | public |
| SaveManager | [`bool RenameEnemyBlueprint(string oldBlueprintName, string newBlueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L343) | public |
| SaveManager | [`bool RenameBlueprint(string oldName, string newName, BuildTargetKind kind)`](../Assets/Scripts/Features/Building/SaveManager.cs#L348) | private |
| SaveManager | [`void DuplicateSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L414) | public |
| SaveManager | [`string GetSavePath(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L427) | public |
| SaveManager | [`string GetEnemyBlueprintPath(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L432) | public |
| SaveManager | [`string GetSaveFileSize(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L437) | public |

## Assets/Scripts/Features/Building/SaveUIPanel.cs

// 职责：展示玩家/敌方/建筑蓝图存档列表并绑定选择动作。 // 入口：RefreshList()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SaveUIPanel | [`void Awake()`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L12) | private |
| SaveUIPanel | [`void RefreshList()`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L17) | public |
| SaveUIPanel | [`void ConfigureSaveItem(GameObject obj, string saveName, UnityEngine.Events.UnityAction onOpen)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L62) | private |
| SaveUIPanel | [`Button CreateRenameButton(Button deleteButton, Transform parent)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L80) | private |
| SaveUIPanel | [`void OnSaveClicked(string saveName)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L97) | private |
| SaveUIPanel | [`void OnEnemyBlueprintClicked(string blueprintName)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L102) | private |
| SaveUIPanel | [`void OnDuplicateClicked(string saveName)`](../Assets/Scripts/Features/Building/SaveUIPanel.cs#L111) | private |

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
| DestroyManager | [`void Awake()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L27) | private |
| DestroyManager | [`void DestroyGameObject(GameObject obj)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L32) | public |
| DestroyManager | [`void ExplodeBlock(Block block)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L148) | public |
| DestroyManager | [`void ScheduleUnitCleanup(ControlUnit unit)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L185) | public |
| DestroyManager | [`void ScheduleDistantGroupCleanup(ControlUnit unit, Transform reference, float maxDistance)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L190) | public |
| DestroyManager | [`void EndSalvageSession()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L195) | public |
| DestroyManager | [`void NotifyObjectDestroyed()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L203) | public |
| DestroyManager | [`void ScheduleRefresh()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L209) | private |
| DestroyManager | [`IEnumerator DelayedRefresh()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L220) | private |
| DestroyManager | [`void ExecuteRefresh()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L227) | private |

## Assets/Scripts/Features/Combat/Durability.cs

// 职责：管理当前/最大耐久、损伤反馈及销毁通知。 // 入口：ApplyDamage() / ApplyCollisionEnergy() / ProtectFromExplosionCollisions()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Durability | [`void Awake()`](../Assets/Scripts/Features/Combat/Durability.cs#L43) | private |
| Durability | [`void OnEnable()`](../Assets/Scripts/Features/Combat/Durability.cs#L53) | private |
| Durability | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Durability.cs#L63) | private |
| Durability | [`void ApplyDamage(float amount, ControlUnit attacker)`](../Assets/Scripts/Features/Combat/Durability.cs#L68) | public |
| Durability | [`void ApplyCollisionEnergy(float reducedMass, float normalSpeed, float share)`](../Assets/Scripts/Features/Combat/Durability.cs#L79) | public |
| Durability | [`void ProtectFromExplosionCollisions(float duration)`](../Assets/Scripts/Features/Combat/Durability.cs#L105) | public |
| Durability | [`void UpdateDurablility(float value)`](../Assets/Scripts/Features/Combat/Durability.cs#L118) | public |
| Durability | [`void ChangeDurability(float value)`](../Assets/Scripts/Features/Combat/Durability.cs#L128) | private |

## Assets/Scripts/Features/Combat/EnemyBlueprintFactory.cs

// 职责：保留敌机生成公开入口；所有阵营共用ModularUnitFactory恢复模块与物理归属。 // 入口：IsValidBlueprint() / Spawn()；现有据点和普通刷怪调用者保持兼容。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyBlueprintFactory | [`bool IsValidBlueprint(BlockDataList dataList, string enemyBlueprint)`](../Assets/Scripts/Features/Combat/EnemyBlueprintFactory.cs#L7) | public static |
| EnemyBlueprintFactory | [`ControlUnit Spawn(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)`](../Assets/Scripts/Features/Combat/EnemyBlueprintFactory.cs#L12) | public static |

## Assets/Scripts/Features/Combat/EnemyController.cs

// 职责：为敌机与自主友舰选择敌对目标并生成驾驶意图；阵营和玩家驾驶权分别判断。 // 入口：FixedUpdate()；按固定周期更新目标和避障意图，逐物理步平滑输入。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyController | [`void Awake()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L35) | private |
| EnemyController | [`void FixedUpdate()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L40) | private |
| EnemyController | [`Vector3 CalculateDesiredMovement(Vector3 flatDirection)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L88) | private |
| EnemyController | [`void SetGuardAnchor(Transform anchor, float radius)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L113) | public |
| EnemyController | [`void SetPriorityTarget(ControlUnit targetUnit)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L120) | public |
| EnemyController | [`ControlUnit FindNearestOpponent()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L126) | private |
| EnemyController | [`bool IsValidOpponent(ControlUnit candidate)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L166) | private |
| EnemyController | [`void OnDisable()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L181) | private |
| EnemyController | [`void ConfigureHoverThrusters()`](../Assets/Scripts/Features/Combat/EnemyController.cs#L195) | private |
| EnemyController | [`Vector3 GetStrafeDirection(Vector3 flatDirection)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L234) | private |
| EnemyController | [`Vector3 ApplyObstacleAvoidance(Vector3 desiredMove, Vector3 targetDirection)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L245) | private |
| EnemyController | [`Vector3 ProbeObstacle(Vector3 direction, float weight)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L265) | private |
| RaycastHitDistanceComparer | [`int Compare(RaycastHit a, RaycastHit b)`](../Assets/Scripts/Features/Combat/EnemyController.cs#L313) | public |

## Assets/Scripts/Features/Combat/EnemyIdentity.cs

// 职责：保存敌方蓝图身份和生成信息，供 HUD 与击杀反馈使用。 // 入口：SetDisplayName() / SetSpawnMaxHealth()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyIdentity | [`void ReportDefeated()`](../Assets/Scripts/Features/Combat/EnemyIdentity.cs#L9) | public |
| EnemyIdentity | [`void SetDisplayName(string displayName)`](../Assets/Scripts/Features/Combat/EnemyIdentity.cs#L26) | public |
| EnemyIdentity | [`void SetSpawnMaxHealth(float maximum)`](../Assets/Scripts/Features/Combat/EnemyIdentity.cs#L31) | public |

## Assets/Scripts/Features/Combat/EnemySpawner.cs

// 职责：缓存敌方蓝图，并在普通建造游玩期间按间隔生成敌机；世界探索由据点驻军独占刷怪。 // 入口：BeginPlayMode() / EndPlayMode() / SpawnRandomEnemy() / SpawnEnemy()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemySpawner | [`void Start()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L21) | private |
| EnemySpawner | [`void Update()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L26) | private |
| EnemySpawner | [`void BeginPlayMode(Transform anchor)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L41) | public |
| EnemySpawner | [`void EndPlayMode()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L57) | public |
| EnemySpawner | [`void InitializeEnemyBlueprintPool()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L65) | private |
| EnemySpawner | [`void SpawnRandomEnemy()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L110) | public |
| EnemySpawner | [`void SpawnEnemy()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L133) | public |
| EnemySpawner | [`ControlUnit SpawnBlockData(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L144) | internal |
| EnemySpawner | [`Vector3 GetSpawnPosition()`](../Assets/Scripts/Features/Combat/EnemySpawner.cs#L149) | private |

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
| ProjectileTurret | [`bool MovesVisual(Transform part)`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L34) | internal |
| ProjectileTurret | [`void Awake()`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L52) | protected override |
| ProjectileTurret | [`void LateUpdate()`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L75) | private |
| ProjectileTurret | [`void UpdateRecoil(float deltaTime)`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L84) | private |
| ProjectileTurret | [`void ApplyRecoilPose()`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L102) | private |
| ProjectileTurret | [`void OnDisable()`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L123) | protected override |
| ProjectileTurret | [`void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)`](../Assets/Scripts/Features/Combat/ProjectileTurret.cs#L131) | protected override |

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
| TurretProjectile | [`bool ResolveOverlaps(Vector3 origin, float radius)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L166) | private |
| TurretProjectile | [`void OnTriggerEnter(Collider other)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L188) | private |
| TurretProjectile | [`bool CanHit(Collider collider)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L197) | private |
| TurretProjectile | [`void ResolveHit(Collider collider, Vector3 point, Vector3 normal)`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L202) | private |
| TurretProjectile | [`void Retire()`](../Assets/Scripts/Features/Combat/TurretProjectile.cs#L223) | private |

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
| TurretTargetSelector | [`ControlUnit FindNearestTarget( IReadOnlyList<ControlUnit> units, ControlUnit owner, UnitFaction faction, float range, bool prioritizeCockpit, out Durability nearestDurability)`](../Assets/Scripts/Features/Combat/TurretTargetSelector.cs#L17) | internal |
| TurretTargetSelector | [`Durability FindNearestDurability(ControlUnit unit)`](../Assets/Scripts/Features/Combat/TurretTargetSelector.cs#L112) | private |
| TurretTargetSelector | [`Durability[] GetDurabilities(ControlUnit unit)`](../Assets/Scripts/Features/Combat/TurretTargetSelector.cs#L135) | private |
| TurretTargetSelector | [`Durability FindCockpitDurability(ControlUnit unit)`](../Assets/Scripts/Features/Combat/TurretTargetSelector.cs#L151) | private |

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
| BotNavigationProbeDriver | [`void Initialize(string caseName, Transform target, Rigidbody body, Rigidbody home)`](../Assets/Scripts/Features/Diagnostics/BotNavigationProbeDriver.cs#L45) | public |
| BotNavigationProbeDriver | [`void FixedUpdate()`](../Assets/Scripts/Features/Diagnostics/BotNavigationProbeDriver.cs#L58) | private |
| BotNavigationProbeDriver | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Diagnostics/BotNavigationProbeDriver.cs#L88) | private |

## Assets/Scripts/Features/Diagnostics/DebugManager.cs

// 职责：保存调试显示配置和状态图标 Canvas；供电范围网格由 PowerRangeUnionView 拥有。 // 入口：GetPowerRangeColor() / TogglePowerRange() / TogglePowerConnections()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DebugManager | [`void Awake()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L43) | private |
| DebugManager | [`void OnDestroy()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L55) | private |
| DebugManager | [`Color GetPowerRangeColor(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L64) | public |
| DebugManager | [`void TogglePowerRange()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L79) | public |
| DebugManager | [`void TogglePowerConnections()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L92) | public |
| DebugManager | [`void ToggleConnectionStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L97) | public |
| DebugManager | [`void ToggleDurabilityStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L103) | public |
| DebugManager | [`void TogglePowerStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L109) | public |
| DebugManager | [`void EnsureStatusIconCanvas()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L115) | private |
| DebugManager | [`void RefreshPowerRangeMesh(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L137) | internal |
| DebugManager | [`void ClearPowerRangeMesh()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L142) | internal |

## Assets/Scripts/Features/Diagnostics/DurabilityDebugLabels.cs

// 职责：集中绘制已受损模块的可选耐久标签，避免每个模块注册IMGUI回调。 // 入口：DebugManager装配；标签关闭时不遍历模块，不更改伤害或耐久状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DurabilityDebugLabels | [`void OnGUI()`](../Assets/Scripts/Features/Diagnostics/DurabilityDebugLabels.cs#L9) | private |

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
| HoverFlightController | [`void ApplyRotationCorrection(Vector3 currentUp, float outputEfficiency)`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L396) | private |
| HoverFlightController | [`void OnDrawGizmosSelected()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L411) | private |

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
| InputManager | [`void EnterBuildMode()`](../Assets/Scripts/Features/Player/InputManager.cs#L163) | public |
| InputManager | [`void PollSiege(SiegeSession siege, Keyboard keyboard)`](../Assets/Scripts/Features/Player/InputManager.cs#L179) | private |
| InputManager | [`void ApplyCursorState(bool showCursor)`](../Assets/Scripts/Features/Player/InputManager.cs#L209) | private |

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

// 职责：按未缩放时间淡出能量线；池实例到期回收，非池实例仍销毁。 // 入口：Initialize()；复用时清零寿命，Update()完成淡出与释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BeamFade | [`void SetPool(TransientEffectPool owner, StylizedBeamEffect effect)`](../Assets/Scripts/Features/Presentation/BeamFade.cs#L14) | internal |
| BeamFade | [`void Initialize(StylizedBeamEffect effect, Color lineColor, float lifetime)`](../Assets/Scripts/Features/Presentation/BeamFade.cs#L20) | public |
| BeamFade | [`void Update()`](../Assets/Scripts/Features/Presentation/BeamFade.cs#L28) | private |

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
| IndustrialPartMotion | [`bool ControlsVisual(Transform part, Renderer renderer)`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L30) | internal |
| IndustrialPartMotion | [`void Awake()`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L35) | private |
| IndustrialPartMotion | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L40) | private |
| IndustrialPartMotion | [`void Update()`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L45) | private |
| IndustrialPartMotion | [`void Configure( Transform[] newSpinTargets, Vector3 newSpinAxis, float newDegreesPerSecond, Transform newBobTarget, float newBobAmplitude, float newBobFrequency, Renderer[] newGlowRenderers, Color newBaseEmission, float newEmissionPulse)`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L84) | public |
| IndustrialPartMotion | [`void CacheRuntimeState()`](../Assets/Scripts/Features/Presentation/IndustrialPartMotion.cs#L107) | private |

## Assets/Scripts/Features/Presentation/LightFade.cs

// 职责：按未缩放时间衰减闪光；池实例到期回收，非池实例仍销毁。 // 入口：Initialize()；复用时清零寿命，Update()完成淡出与释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| LightFade | [`void SetPool(TransientEffectPool owner, Light light)`](../Assets/Scripts/Features/Presentation/LightFade.cs#L14) | internal |
| LightFade | [`void Initialize(Light light, float lifetime, float intensity)`](../Assets/Scripts/Features/Presentation/LightFade.cs#L20) | public |
| LightFade | [`void Update()`](../Assets/Scripts/Features/Presentation/LightFade.cs#L28) | private |

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
| MainUIPanels | [`void Awake()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L47) | private |
| MainUIPanels | [`void Update()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L53) | private |
| MainUIPanels | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L76) | private |
| MainUIPanels | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L81) | private |
| MainUIPanels | [`void ShowCreatePanel()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L90) | public |
| MainUIPanels | [`void ShowRenamePanel(string save)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L105) | public |
| MainUIPanels | [`void HideCreatePanel()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L120) | public |
| MainUIPanels | [`void SetInputPlaceholder(string text)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L130) | private |
| MainUIPanels | [`void ShowDeletePanel(string save)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L139) | public |
| MainUIPanels | [`void HideDeletePanel()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L155) | public |
| MainUIPanels | [`void OnConfirmCreate()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L167) | public |
| MainUIPanels | [`void OnConfirmDelete(string save)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L207) | private |
| MainUIPanels | [`void PlayStart()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L230) | public |
| MainUIPanels | [`void CompletePlayStart()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L258) | public |
| MainUIPanels | [`void PlayEnd()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L278) | public |
| MainUIPanels | [`void CompletePlayEnd()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L294) | public |
| MainUIPanels | [`void PlayerDeath()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L304) | public |
| MainUIPanels | [`void EnterEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L332) | public |
| MainUIPanels | [`void ExitEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L337) | public |
| MainUIPanels | [`void RefreshBlueprintEditorMode()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L342) | public |
| MainUIPanels | [`void RefreshBuildingEditorPanel()`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L361) | public |
| MainUIPanels | [`void UpdateHealthBar(GameObject obj, float currentHealth, float maxHealth)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L366) | public |
| MainUIPanels | [`void SetHealthBar(float currentHealth, float maxHealth)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L381) | private |
| MainUIPanels | [`void SetCockpitHealth(Cockpit cockpit)`](../Assets/Scripts/Features/Presentation/MainUIPanels.cs#L397) | private |

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

## Assets/Scripts/Features/Presentation/ReturnStatusPanel.cs

// 职责：显示持续的返航保存失败原因与重试提示，不抢焦点或阻挡操作。 // 入口：ShowFailure() / Clear()；MainUIPanels在返航失败/成功时调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ReturnStatusPanel | [`void Awake()`](../Assets/Scripts/Features/Presentation/ReturnStatusPanel.cs#L15) | private |
| ReturnStatusPanel | [`void ShowFailure(string reason)`](../Assets/Scripts/Features/Presentation/ReturnStatusPanel.cs#L20) | public |
| ReturnStatusPanel | [`void Clear()`](../Assets/Scripts/Features/Presentation/ReturnStatusPanel.cs#L37) | public |

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

## Assets/Scripts/Features/Presentation/TransientEffectPool.cs

// 职责：按场景复用短时光束和点光源；只限制闲置缓存，不裁剪当前战斗效果。 // 入口：RentBeam() / RentLight()；淡出结束Return，场景卸载连同活动及闲置子对象释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TransientEffectPool | [`BeamFade RentBeam()`](../Assets/Scripts/Features/Presentation/TransientEffectPool.cs#L28) | public static |
| TransientEffectPool | [`LightFade RentLight(Vector3 position)`](../Assets/Scripts/Features/Presentation/TransientEffectPool.cs#L50) | public static |
| TransientEffectPool | [`void Return(BeamFade fade)`](../Assets/Scripts/Features/Presentation/TransientEffectPool.cs#L75) | internal |
| TransientEffectPool | [`void Return(LightFade fade)`](../Assets/Scripts/Features/Presentation/TransientEffectPool.cs#L94) | internal |
| TransientEffectPool | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/TransientEffectPool.cs#L113) | private |

## Assets/Scripts/Features/Presentation/TransientEffects.cs

// 职责：配置可复用能量线与灯光闪烁，统一径向碎光、颜色及可见网格包围盒规则。 // 入口：CreateLineStreak() / CreateRadialStreakBurst() / CreateLightFlash()；无模式或场景单例依赖。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TransientEffects | [`void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L7) | public static |
| TransientEffects | [`void CreateRadialStreakBurst( Vector3 center, Vector3 normal, Color color, int count, float length, float duration, float width)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L19) | public static |
| TransientEffects | [`void CreateLightFlash(Vector3 position, Color color, float intensity, float range, float duration)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L48) | public static |
| TransientEffects | [`Bounds GetBounds(GameObject target, Vector3 fallbackCenter, Vector3 fallbackSize)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L58) | public static |
| TransientEffects | [`Vector3 GetBlockSize(Block block)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L84) | public static |
| TransientEffects | [`Color WithAlpha(Color color, float alpha)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L89) | public static |
| TransientEffects | [`Color Brighten(Color color, float intensity, float whiteBlend = 0f)`](../Assets/Scripts/Features/Presentation/TransientEffects.cs#L95) | public static |

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
| VisualEffectsManager | [`VisualEffectsManager EnsureInstance()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L35) | public static |
| VisualEffectsManager | [`Material GetSharedLineMaterial()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L53) | public static |
| VisualEffectsManager | [`void TryPlayBlockPlaced(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L58) | public static |
| VisualEffectsManager | [`void TryPlayBlockRemoved(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L68) | public static |
| VisualEffectsManager | [`void TryPlayBlockExplosion(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L78) | public static |
| VisualEffectsManager | [`void TryPlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L88) | public static |
| VisualEffectsManager | [`void TryAttachDetachedPartSmoke(Rigidbody body, Vector3 worldAnchor, float intensity)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L93) | public static |
| VisualEffectsManager | [`void TryPlayObjectDestroyed(GameObject target)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L109) | public static |
| VisualEffectsManager | [`void TryPlayBlockMoved(Block block, Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L119) | public static |
| VisualEffectsManager | [`void TryPlayBlockRotated(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L129) | public static |
| VisualEffectsManager | [`void TryShowBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L139) | public static |
| VisualEffectsManager | [`void TryClearBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L149) | public static |
| VisualEffectsManager | [`void TryUpdateGhostPreview(GameObject ghost, bool isBlocked)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L159) | public static |
| VisualEffectsManager | [`void TryClearGhostPreview(GameObject ghost)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L169) | public static |
| VisualEffectsManager | [`void TryDecorateMeteor(Meteor meteor)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L179) | public static |
| VisualEffectsManager | [`void TryPlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L189) | public static |
| VisualEffectsManager | [`void Awake()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L194) | private |
| VisualEffectsManager | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L206) | private |
| VisualEffectsManager | [`void PlayCameraShake(float amplitude, float duration)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L212) | public |
| VisualEffectsManager | [`void PlaySpawnBeacon(Vector3 position)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L218) | public |
| VisualEffectsManager | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L238) | private |
| VisualEffectsManager | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L247) | private |
| VisualEffectsManager | [`void ApplySceneLook()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L252) | private |
| VisualEffectsManager | [`void PlayBlockPlaced(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L267) | private |
| VisualEffectsManager | [`void PlayBlockRemoved(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L291) | private |
| VisualEffectsManager | [`void PlayBlockExplosion(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L314) | private |
| VisualEffectsManager | [`void PlayObjectDestroyed(GameObject target)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L348) | private |
| VisualEffectsManager | [`void PlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L380) | private |
| VisualEffectsManager | [`void PlayBlockMoved(Block block, Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L407) | private |
| VisualEffectsManager | [`void PlayBlockRotated(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L420) | private |
| VisualEffectsManager | [`void ShowBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L441) | private |
| VisualEffectsManager | [`void ClearBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L455) | private |
| VisualEffectsManager | [`void UpdateGhostPreview(Transform ghost, bool isBlocked)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L460) | private |
| VisualEffectsManager | [`void ClearGhostPreview()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L465) | private |
| VisualEffectsManager | [`void DecorateMeteor(Meteor meteor)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L469) | private |
| VisualEffectsManager | [`void PlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L510) | private |

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
| CargoHold | [`CargoHold FindReceiver(CargoItem item, Vector3 position, ControlUnit owner = null, LootDrop reservation = null)`](../Assets/Scripts/Features/Salvage/CargoHold.cs#L232) | public static |

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
| CargoPersistence | [`bool SaveReturnCargo(bool survived, out string failureReason)`](../Assets/Scripts/Features/Salvage/CargoPersistence.cs#L16) | public static |

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

## Assets/Scripts/Features/Siege/SiegeBase.cs

// 职责：以大型建筑蓝图创建阵地战基地，独立绑定核心目标，并按建筑原蓝图报价/修缮。 // 入口：Create() / Quote() / Repair() / Dispose()；不复用舰船驾驶舱或舰船物理重组。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBase | [`SiegeBase Create(SiegeSession session, WorldBuildingManager buildings, Transform anchor)`](../Assets/Scripts/Features/Siege/SiegeBase.cs#L35) | public static |
| SiegeBase | [`long Quote()`](../Assets/Scripts/Features/Siege/SiegeBase.cs#L70) | public |
| SiegeBase | [`bool Repair(out string reason)`](../Assets/Scripts/Features/Siege/SiegeBase.cs#L98) | public |
| SiegeBase | [`void OnBlockDestroyed(Block block)`](../Assets/Scripts/Features/Siege/SiegeBase.cs#L123) | private |
| SiegeBase | [`void Dispose()`](../Assets/Scripts/Features/Siege/SiegeBase.cs#L131) | public |

## Assets/Scripts/Features/Siege/SiegeBlueprintCatalog.cs

// 职责：只读加载阵地战双方的用户蓝图快照，过滤不能部署的存档，不修改源文件。 // 入口：Load()；玩家来自Saves，攻方来自EnemyBlueprints，压力场可保留固定夹具。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBlueprintCatalog | [`SiegeShipOption[] Load(string directory, int coinsPerBlock, out string message)`](../Assets/Scripts/Features/Siege/SiegeBlueprintCatalog.cs#L10) | public static |

## Assets/Scripts/Features/Siege/SiegeDrivingPlayerChecks.cs

// 职责：显式Player验证通过虚拟Input System设备走键盘、HUD点击与真实飞行物理链。 // 入口：Run()；只由-siege-checks-driving调用，finally恢复设备与后台输入设置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeDrivingPlayerChecks | [`IEnumerator Run(SiegeSession session, Action<string, bool> check, Report report)`](../Assets/Scripts/Features/Siege/SiegeDrivingPlayerChecks.cs#L15) | internal static |
| SiegeDrivingPlayerChecks | [`Thruster[] DirectionalThrusters(ControlUnit unit)`](../Assets/Scripts/Features/Siege/SiegeDrivingPlayerChecks.cs#L186) | private static |
| SiegeDrivingPlayerChecks | [`float HorizontalDistance(Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Siege/SiegeDrivingPlayerChecks.cs#L191) | private static |

## Assets/Scripts/Features/Siege/SiegeEngineCounters.cs

// 职责：显式性能诊断时读取可用的引擎计时/GC计数，记录缺失和缓冲溢出，避免旧值冒充新帧。 // 入口：Read() / Dispose()；只读已完成样本并清空缓冲，跨帧发布延迟不保证与delta严格对齐。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeEngineCounters | [`Sample Read()`](../Assets/Scripts/Features/Siege/SiegeEngineCounters.cs#L60) | public |
| SiegeEngineCounters | [`long ReadCounter(int index)`](../Assets/Scripts/Features/Siege/SiegeEngineCounters.cs#L81) | private |
| SiegeEngineCounters | [`void Dispose()`](../Assets/Scripts/Features/Siege/SiegeEngineCounters.cs#L122) | public |

## Assets/Scripts/Features/Siege/SiegeFleet.cs

// 职责：部署固定泊位舰队、保持唯一玩家驾驶权，并在波次边界切换舰队物理状态。 // 入口：CreateBase() / Deploy() / TakeControl() / SetCombatActive() / NotifyLost()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFleet | [`bool CreateBase(Transform anchor)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L30) | public |
| SiegeFleet | [`bool Deploy(int optionIndex, out string reason)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L37) | public |
| SiegeFleet | [`bool TakeControl(int slot)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L111) | public |
| SiegeFleet | [`void SetCombatActive(bool active)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L144) | public |
| SiegeFleet | [`void RepairBetweenWaves(out int repairedCount, out int attemptedCount, out long spent, out string firstFailure)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L180) | public |
| SiegeFleet | [`void RefreshBindings()`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L220) | public |
| SiegeFleet | [`void NotifyLost(Cockpit cockpit)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L246) | public |
| SiegeFleet | [`SiegeFleetEntry Spawn( BlockDataList blueprint, string id, string displayName, int price, Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L272) | private |
| SiegeFleet | [`BlockDataList PrepareBlueprint(TextAsset asset, out string id)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L307) | public static |
| SiegeFleet | [`BlockDataList PrepareBlueprint(string json, out string id)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L312) | public static |
| SiegeFleet | [`void Park(ControlUnit unit)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L326) | private static |
| SiegeFleet | [`void RestoreView()`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L344) | public |
| SiegeFleet | [`void SetCamera(ControlUnit unit)`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L356) | private |
| SiegeFleet | [`void SetBaseCamera()`](../Assets/Scripts/Features/Siege/SiegeFleet.cs#L370) | private |

## Assets/Scripts/Features/Siege/SiegeFleetEntry.cs

// 职责：持有一艘部署舰的蓝图身份与驾驶舱引用；父物理组变化不改变舰艇身份。 // 入口：Unit / IsAlive；仅由SiegeFleet建立，维修按同一部署ID寻找原模块。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFleetEntry | [`void MarkLost()`](../Assets/Scripts/Features/Siege/SiegeFleetEntry.cs#L69) | public |

## Assets/Scripts/Features/Siege/SiegeHud.cs

// 职责：绑定可编辑UGUI阵地战面板，低频显示金币、波次、核心耐久和舰队驾驶权。 // 入口：OnEnable()注册按钮与缓存标签，OnDisable()解绑；规则与修缮由SiegeSession执行。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeHud | [`void OnEnable()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L108) | private |
| SiegeHud | [`void OnDisable()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L142) | private |
| SiegeHud | [`void Update()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L165) | private |
| SiegeHud | [`void UpdateTransient()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L281) | private |
| SiegeHud | [`void UpdatePhase(SiegeMatch match)`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L314) | private |
| SiegeHud | [`void ApplyDockLayout(bool battle)`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L416) | private |
| SiegeHud | [`void RefreshShipStatus()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L457) | private |
| SiegeHud | [`void SetBar(Image fill, Text value, float current, float maximum)`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L490) | private static |
| SiegeHud | [`void ShowBanner(string title, string subtitle)`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L506) | private |
| SiegeHud | [`void ToggleHelp()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L534) | private |
| SiegeHud | [`void PollCamera()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L544) | private |
| SiegeHud | [`void RefreshOptions()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L562) | private |
| SiegeHud | [`void RefreshBlueprints()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L595) | private |
| SiegeHud | [`void SelectAttacker(int index)`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L601) | private |
| SiegeHud | [`void SelectDefender(int index)`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L606) | private |
| SiegeHud | [`void StartWave()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L611) | private |
| SiegeHud | [`void RepairCore()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L616) | private |
| SiegeHud | [`void RepairShip()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L622) | private |
| SiegeHud | [`void RefreshRefitQuotes()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L629) | private |
| SiegeHud | [`void Restart()`](../Assets/Scripts/Features/Siege/SiegeHud.cs#L652) | private |

## Assets/Scripts/Features/Siege/SiegeMatch.cs

// 职责：保存阵地战阶段、波次名额、金币和失败状态；不依赖场景对象或物理引擎。 // 入口：BeginNextWave() / RegisterAttacker() / ResolveAttacker() / TrySpend() / LoseBase()。 // 约束：生成成功才消耗名额；同一敌机只结算一次；维修和部署只能发生在整备阶段。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeMatch | [`bool BeginNextWave()`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L101) | public |
| SiegeMatch | [`bool CanSpawn(int maximumActive)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L117) | public |
| SiegeMatch | [`bool RegisterAttacker(string id, int maximumActive)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L122) | public |
| SiegeMatch | [`bool ResolveAttacker(string id, bool defeated)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L134) | public |
| SiegeMatch | [`bool GrantBonus(int coins)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L157) | public |
| SiegeMatch | [`bool CanAfford(long price)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L168) | public |
| SiegeMatch | [`bool TrySpend(long price)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L173) | public |
| SiegeMatch | [`void LoseBase()`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L184) | public |

## Assets/Scripts/Features/Siege/SiegePlayerChecks.cs

// 职责：仅显式-siege-checks-report参数启动独立Player验证，记录真实帧、玩法契约与自然多波结果。 // 入口：Initialize()；smoke使用明确损伤刺激验证闭环，soak只自然战斗/整备，不把两种结果混用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayerChecks | [`void Initialize()`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L30) | private static |
| SiegePlayerChecks | [`void Update()`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L93) | private |
| SiegePlayerChecks | [`void LateUpdate()`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L114) | private |
| SiegePlayerChecks | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L132) | private |
| SiegePlayerChecks | [`IEnumerator Run()`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L140) | private |
| SiegePlayerChecks | [`string ImagePath(string suffix)`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L424) | private |
| SiegePlayerChecks | [`void Check(string name, bool passed)`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L431) | private |
| SiegePlayerChecks | [`void CaptureError(string condition, string trace, LogType type)`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L441) | private |
| SiegePlayerChecks | [`void Finish()`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L449) | private |
| SiegePlayerChecks | [`float Percentile(float fraction)`](../Assets/Scripts/Features/Siege/SiegePlayerChecks.cs#L468) | private |

## Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs

// 职责：显式Player检查的低频规模普查、有限慢帧记录和可选嵌套CPU计时；不参与正常游戏。 // 入口：Observe() / Dispose()；帧间隔对应前一观察窗口，普查自身开销单独记录，不能据相关性推断瓶颈。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayerSurvey | [`void Observe(SiegeSession session, float milliseconds)`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L39) | public |
| SiegePlayerSurvey | [`BodyCounts Classify(Rigidbody[] bodies)`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L75) | public static |
| SiegePlayerSurvey | [`Census TakeCensus(SiegeSession session, float seconds, Work work)`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L128) | private static |
| SiegePlayerSurvey | [`void KeepSlowFrame(SlowFrame frame)`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L156) | private |
| SiegePlayerSurvey | [`Work ReadWork()`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L175) | private |
| SiegePlayerSurvey | [`StageWork ReadStage(SimulationWorkMetrics.Stage stage)`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L199) | private static |
| SiegePlayerSurvey | [`void Dispose()`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L210) | public |
| Work | [`Work Subtract(Work current, Work previous)`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L307) | public static |
| Work | [`StageWork Difference(StageWork current, StageWork previous)`](../Assets/Scripts/Features/Siege/SiegePlayerSurvey.cs#L325) | private static |

## Assets/Scripts/Features/Siege/SiegeProgress.cs

// 职责：记录阵地战的跨启动战果（最高波次与累计击毁），只写本地PlayerPrefs，不参与单局规则。 // 入口：TryRecordResult() / BestWave / TotalKills；由SiegeSession在失败时调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeProgress | [`bool TryRecordResult(int wave, int kills)`](../Assets/Scripts/Features/Siege/SiegeProgress.cs#L15) | public static |
| SiegeProgress | [`void RestoreForProbe(int bestWave, int totalKills, int totalRuns)`](../Assets/Scripts/Features/Siege/SiegeProgress.cs#L35) | public static |

## Assets/Scripts/Features/Siege/SiegeRefit.cs

// 职责：验证泊位净空并报价/执行修缮，复用同源模块、补回缺件，成功后精确扣费。 // 入口：IsClear() / Quote() / Repair()；部署蓝图不含货物，不读取或改写WorldSession。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRefit | [`bool IsClear( BlockDataList blueprint, Vector3 position, Quaternion rotation, string ignoredUnitId, out string reason)`](../Assets/Scripts/Features/Siege/SiegeRefit.cs#L9) | public static |
| SiegeRefit | [`bool IsBuildingClear( BlockDataList blueprint, Vector3 position, Quaternion rotation, string ignoredUnitId, out string reason)`](../Assets/Scripts/Features/Siege/SiegeRefit.cs#L26) | public static |
| SiegeRefit | [`bool IsGeometryClear( BlockDataList blueprint, Vector3 position, Quaternion rotation, string ignoredUnitId, out string reason)`](../Assets/Scripts/Features/Siege/SiegeRefit.cs#L37) | private static |
| SiegeRefit | [`string DescribeObstruction(Collider hit)`](../Assets/Scripts/Features/Siege/SiegeRefit.cs#L65) | private static |
| SiegeRefit | [`long Quote(SiegeFleetEntry entry, SiegeSettings settings)`](../Assets/Scripts/Features/Siege/SiegeRefit.cs#L83) | public static |
| SiegeRefit | [`bool Repair(SiegeFleetEntry entry, SiegeSession session, out string reason)`](../Assets/Scripts/Features/Siege/SiegeRefit.cs#L115) | public static |
| SiegeRefit | [`Dictionary<string, Block> CollectOwned(SiegeFleetEntry entry)`](../Assets/Scripts/Features/Siege/SiegeRefit.cs#L195) | private static |

## Assets/Scripts/Features/Siege/SiegeSession.cs

// 职责：托管阵地战会话生命周期、波次入口和基地失败；不改变现有Main WorldSession流程。 // 入口：Begin() / BeginNextWave() / End()；场景通过Inspector引用设置和泊位。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeSession | [`void BindWorkshop(SiegeWorkshop workshop)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L26) | public |
| SiegeSession | [`void SelectAttacker(int index)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L96) | public |
| SiegeSession | [`void SelectDefender(int index)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L104) | public |
| SiegeSession | [`bool RefreshBlueprints()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L112) | public |
| SiegeSession | [`int FindOption(SiegeShipOption[] options, string displayName, int fallback)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L146) | private static |
| SiegeSession | [`void Awake()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L172) | private |
| SiegeSession | [`void Start()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L183) | private |
| SiegeSession | [`bool Begin()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L193) | public |
| SiegeSession | [`bool BeginNextWave()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L216) | public |
| SiegeSession | [`bool TryDeploy(int optionIndex, out string reason)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L243) | public |
| SiegeSession | [`bool TryTakeControl(int slot)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L251) | public |
| SiegeSession | [`bool TryRepair(bool repairBase)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L256) | public |
| SiegeSession | [`void End()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L264) | public |
| SiegeSession | [`void LoseBase()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L275) | public |
| SiegeSession | [`void NotifyCockpitDestroyed(Cockpit cockpit)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L294) | public |
| SiegeSession | [`void Update()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L299) | private |
| SiegeSession | [`void TickKillStreak()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L335) | private |
| SiegeSession | [`void RepairBaseBetweenWaves()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L360) | private |
| SiegeSession | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L394) | private |
| SiegeSession | [`void SetSimulationActive(bool active)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L401) | private |

## Assets/Scripts/Features/Siege/SiegeSettings.cs

// 职责：保存阵地战可编辑蓝图、经济、波次与部署预算；不持有运行中会话状态。 // 入口：CreateMatch()；由SiegeSession读取，场景制作工具创建默认配置资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeSettings | [`SiegeMatch CreateMatch()`](../Assets/Scripts/Features/Siege/SiegeSettings.cs#L105) | public |

## Assets/Scripts/Features/Siege/SiegeStressPopulation.cs

// 职责：在独立压力场景分帧恢复附加建筑，沿用生产建筑预算/支撑/损毁，不参与基地胜负。 // 入口：Start()；Inspector配置真实建筑蓝图与锚点，加载期间暂停HUD交互，场景退出释放拥有的建筑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeStressPopulation | [`IEnumerator Start()`](../Assets/Scripts/Features/Siege/SiegeStressPopulation.cs#L40) | private |
| SiegeStressPopulation | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/SiegeStressPopulation.cs#L138) | private |
| SiegeStressPopulation | [`void RestoreHud()`](../Assets/Scripts/Features/Siege/SiegeStressPopulation.cs#L154) | private |

## Assets/Scripts/Features/Siege/SiegeThreatMarkers.cs

// 职责：在HUD边缘标出不在视野内的来袭敌舰，给出方向与距离，夜色场景里仍能读到威胁。 // 入口：Start()建立有界标记池，Update()按间隔刷新；只读ControlUnit公开状态，不参与规则或物理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeThreatMarkers | [`void Awake()`](../Assets/Scripts/Features/Siege/SiegeThreatMarkers.cs#L34) | private |
| SiegeThreatMarkers | [`void Start()`](../Assets/Scripts/Features/Siege/SiegeThreatMarkers.cs#L40) | private |
| SiegeThreatMarkers | [`void Build()`](../Assets/Scripts/Features/Siege/SiegeThreatMarkers.cs#L49) | private |
| SiegeThreatMarkers | [`void Update()`](../Assets/Scripts/Features/Siege/SiegeThreatMarkers.cs#L99) | private |
| SiegeThreatMarkers | [`void PlaceMarker(int index, Camera camera, Vector3 world, Vector3 viewport, float distance)`](../Assets/Scripts/Features/Siege/SiegeThreatMarkers.cs#L150) | private |
| SiegeThreatMarkers | [`void HideAll()`](../Assets/Scripts/Features/Siege/SiegeThreatMarkers.cs#L189) | private |

## Assets/Scripts/Features/Siege/SiegeWaveDirector.cs

// 职责：在独立在场预算内按固定随机种子调度敌舰，并将真实死亡或移除结算给规则核心。 // 入口：Tick() / Stop()；死亡事件只记事实，由下一次Tick统一结算，避免事件中重入生成。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWaveDirector | [`void Tick(float time)`](../Assets/Scripts/Features/Siege/SiegeWaveDirector.cs#L19) | public |
| SiegeWaveDirector | [`SiegeShipOption PickBlueprint(SiegeSettings settings)`](../Assets/Scripts/Features/Siege/SiegeWaveDirector.cs#L79) | private |
| SiegeWaveDirector | [`void Stop()`](../Assets/Scripts/Features/Siege/SiegeWaveDirector.cs#L99) | public |
| Attacker | [`void OnDefeated()`](../Assets/Scripts/Features/Siege/SiegeWaveDirector.cs#L129) | private |
| Attacker | [`void RefreshTarget(ControlUnit core)`](../Assets/Scripts/Features/Siege/SiegeWaveDirector.cs#L134) | public |
| Attacker | [`void Unsubscribe()`](../Assets/Scripts/Features/Siege/SiegeWaveDirector.cs#L147) | public |

## Assets/Scripts/Features/Siege/SiegeWorkshop.cs

// 职责：在整备阶段复用 Main 蓝图编辑器，隔离已部署舰队与大型建筑，退出后恢复驾驶视图。 // 入口：Open() / Close()；只编辑存档预览，不免费替换战场机体或恢复耐久。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWorkshop | [`void Awake()`](../Assets/Scripts/Features/Siege/SiegeWorkshop.cs#L25) | private |
| SiegeWorkshop | [`void Start()`](../Assets/Scripts/Features/Siege/SiegeWorkshop.cs#L30) | private |
| SiegeWorkshop | [`void Update()`](../Assets/Scripts/Features/Siege/SiegeWorkshop.cs#L39) | private |
| SiegeWorkshop | [`void OpenSelected()`](../Assets/Scripts/Features/Siege/SiegeWorkshop.cs#L44) | private |
| SiegeWorkshop | [`bool Open(string saveName = null)`](../Assets/Scripts/Features/Siege/SiegeWorkshop.cs#L49) | public |
| SiegeWorkshop | [`bool Close()`](../Assets/Scripts/Features/Siege/SiegeWorkshop.cs#L103) | public |
| SiegeWorkshop | [`void SetPanels(bool editing)`](../Assets/Scripts/Features/Siege/SiegeWorkshop.cs#L152) | private |
| SiegeWorkshop | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/SiegeWorkshop.cs#L182) | private |

## Assets/Scripts/Features/Siege/SiegeWreckBudget.cs

// 职责：限制阵地战场景中敌方无驾驶舱碎片的存量和寿命，分批沿既有回收路径退场。 // 入口：Tick()；只处理同场景且所有模块仍属敌方的碎片，不影响友舰维修、建筑或存活驾驶舱。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWreckBudget | [`void Tick(float time, int maximumGroups, int maximumBlocks, float lifetime, int retirePerScan)`](../Assets/Scripts/Features/Siege/SiegeWreckBudget.cs#L34) | public |
| SiegeWreckBudget | [`bool Eligible(ControlUnit unit)`](../Assets/Scripts/Features/Siege/SiegeWreckBudget.cs#L106) | private |

## Assets/Scripts/Features/Simulation/BatchConnectionBuilder.cs

// 职责：批量重建方块邻接与双侧连接；每个执行片段统一同步物理。 // 入口：RebuildBatchRoutine() / RebuildBatchImmediate()；分帧版本用于加载中的静止蓝图。 // 约束：预算在完整单块操作后检查，不保证单个昂贵操作低于预算；恢复后重新同步物理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BatchConnectionBuilder | [`IEnumerator RebuildBatchRoutine()`](../Assets/Scripts/Features/Simulation/BatchConnectionBuilder.cs#L18) | public |
| BatchConnectionBuilder | [`void RebuildBatchImmediate()`](../Assets/Scripts/Features/Simulation/BatchConnectionBuilder.cs#L40) | public |
| BatchConnectionBuilder | [`void RefreshBlock(Block block)`](../Assets/Scripts/Features/Simulation/BatchConnectionBuilder.cs#L54) | private static |

## Assets/Scripts/Features/Simulation/Block.cs

// 职责：保存方块尺寸、质量、连接面和编辑器配置；由 BlockConnections 维护运行时连接。 // 入口：GetDisplayInfo() / GetConnectorWorldPosition() / GetConnectorWorldNormal()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Block | [`void CollectRuntimeNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/Block.cs#L52) | internal |
| Block | [`void RefreshRuntimeConnectionPose()`](../Assets/Scripts/Features/Simulation/Block.cs#L57) | internal |
| Block | [`void InvalidateRuntimeConnections()`](../Assets/Scripts/Features/Simulation/Block.cs#L62) | internal |
| Block | [`void OnEnable()`](../Assets/Scripts/Features/Simulation/Block.cs#L67) | private |
| Block | [`void OnDisable()`](../Assets/Scripts/Features/Simulation/Block.cs#L72) | private |
| Block | [`void Awake()`](../Assets/Scripts/Features/Simulation/Block.cs#L77) | private |
| Block | [`void OnValidate()`](../Assets/Scripts/Features/Simulation/Block.cs#L104) | private |
| Block | [`void RefreshAuthoringData()`](../Assets/Scripts/Features/Simulation/Block.cs#L121) | public |
| Block | [`void RefreshRuntimeData(bool rebuildConnectors = false)`](../Assets/Scripts/Features/Simulation/Block.cs#L150) | public |
| Block | [`void RefreshMass()`](../Assets/Scripts/Features/Simulation/Block.cs#L184) | public |
| Block | [`void SetMassSettings(bool useCustomMass, float value)`](../Assets/Scripts/Features/Simulation/Block.cs#L197) | public |
| Block | [`void Reset()`](../Assets/Scripts/Features/Simulation/Block.cs#L205) | private |
| Block | [`void GenerateConnectionPoints()`](../Assets/Scripts/Features/Simulation/Block.cs#L210) | private |
| Block | [`string GetDisplayInfo()`](../Assets/Scripts/Features/Simulation/Block.cs#L280) | public |
| Block | [`string GenerateInfo()`](../Assets/Scripts/Features/Simulation/Block.cs#L285) | private |
| Block | [`void CreateConnectionPoint(ConnectType connectType, Vector3 localPos, Vector3 normal, int order)`](../Assets/Scripts/Features/Simulation/Block.cs#L331) | private |
| Block | [`Vector3 GetConnectorWorldPosition(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L345) | public |
| Block | [`Vector3 GetConnectorWorldNormal(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L350) | public |
| Block | [`bool IsConnectorAvailableForPlacement(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L355) | public |
| Block | [`void CheckConnection()`](../Assets/Scripts/Features/Simulation/Block.cs#L360) | public |
| Block | [`void CheckConnectionAfterPhysicsSync()`](../Assets/Scripts/Features/Simulation/Block.cs#L365) | internal |
| Block | [`List<Block> Neighbors()`](../Assets/Scripts/Features/Simulation/Block.cs#L370) | public |
| Block | [`void CollectNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/Block.cs#L378) | internal |
| Block | [`void DisConnectAllConnectors(bool refreshNeighbors = true)`](../Assets/Scripts/Features/Simulation/Block.cs#L383) | public |
| Block | [`void RestoreConnectionParticipation()`](../Assets/Scripts/Features/Simulation/Block.cs#L389) | public |
| Block | [`void RefreshConnectionStatus()`](../Assets/Scripts/Features/Simulation/Block.cs#L394) | internal |
| Block | [`bool IsBlockedGhost()`](../Assets/Scripts/Features/Simulation/Block.cs#L402) | public |
| Block | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Simulation/Block.cs#L426) | private |

## Assets/Scripts/Features/Simulation/BlockConnectionSnapshot.cs

// 职责：精确校验运行连接缓存的模块局部姿态、连接面身份/几何、规范碰撞体与查询层配置。 // 入口：IsValid / Capture() / Invalidate()；刚体整体运动不失效，复杂层级回退物理探测。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockConnectionSnapshot | [`void Capture()`](../Assets/Scripts/Features/Simulation/BlockConnectionSnapshot.cs#L54) | internal |
| BlockConnectionSnapshot | [`void RefreshIfReady()`](../Assets/Scripts/Features/Simulation/BlockConnectionSnapshot.cs#L90) | internal |
| BlockConnectionSnapshot | [`void Invalidate()`](../Assets/Scripts/Features/Simulation/BlockConnectionSnapshot.cs#L98) | internal |
| BlockConnectionSnapshot | [`int ConnectionMask()`](../Assets/Scripts/Features/Simulation/BlockConnectionSnapshot.cs#L103) | private static |

## Assets/Scripts/Features/Simulation/BlockConnections.cs

// 职责：探测匹配连接面、维护双侧连接引用和邻接查询；与方块序列化配置分离。 // 入口：CheckConnection() / CollectNeighbors() / DisConnectAllConnectors()。 // 约束：批量图遍历自行统一同步物理；探测缓冲满时扩容，连接视觉不决定拓扑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockConnections | [`bool IsConnectorAvailableForPlacement(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L39) | internal |
| BlockConnections | [`void CheckConnection(bool synchronizePhysics = true)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L53) | internal |
| BlockConnections | [`void ReleaseExistingConnections()`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L102) | private |
| BlockConnections | [`void ReleaseConnectionPairWithoutVisual(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L130) | private |
| BlockConnections | [`void ReleaseConnectionReferences(GameObject connectorObject)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L150) | private |
| BlockConnections | [`Block FindBlockAcrossConnector(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L187) | private |
| BlockConnections | [`bool SharesBuildingScope(Block other)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L228) | private |
| BlockConnections | [`Connector FindMatchingConnector(Block otherBlock, Connector connector, bool requireAvailable = true)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L235) | private |
| BlockConnections | [`void ConnectTo(Connector connector, Block otherBlock, Connector otherConnector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L257) | private |
| BlockConnections | [`void ClearConnector(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L282) | private |
| BlockConnections | [`void CollectNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L311) | internal |
| BlockConnections | [`void DisConnectAllConnectors(bool refreshNeighbors = true)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L339) | internal |
| BlockConnections | [`void RestoreParticipation()`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L385) | internal |
| BlockConnections | [`void ReleaseRuntimePair(Connector connector)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L404) | private |
| BlockConnections | [`void CollectRuntimeNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L430) | internal |
| BlockConnections | [`void RefreshRuntimePoseIfReady()`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L452) | internal |
| BlockConnections | [`void InvalidateRuntimeLinks()`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L457) | internal |
| BlockConnections | [`void CaptureRuntimePose()`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L462) | private |
| BlockConnections | [`int ConnectionMask()`](../Assets/Scripts/Features/Simulation/BlockConnections.cs#L467) | private static |

## Assets/Scripts/Features/Simulation/BlockGroupManager.cs

// 职责：按方块连接关系分组并计算组中心，供游玩物理单元建立使用。 // 入口：GroupBlocks() / CalculateGroupCenter()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockGroupManager | [`List<List<Block>> GroupBlocks(List<Block> allBlocks)`](../Assets/Scripts/Features/Simulation/BlockGroupManager.cs#L9) | public static |
| BlockGroupManager | [`List<List<Block>> GroupRuntimeBlocks(List<Block> allBlocks)`](../Assets/Scripts/Features/Simulation/BlockGroupManager.cs#L15) | internal static |
| BlockGroupManager | [`List<List<Block>> Traverse(List<Block> allBlocks, bool runtimeLinks)`](../Assets/Scripts/Features/Simulation/BlockGroupManager.cs#L36) | private static |
| BlockGroupManager | [`Vector3 CalculateGroupCenter(List<Block> group)`](../Assets/Scripts/Features/Simulation/BlockGroupManager.cs#L104) | public static |

## Assets/Scripts/Features/Simulation/Cockpit.cs

// 职责：保存驾驶舱阵营与驾驶来源；驾驶权随驾驶舱迁移，不随物理父组重建丢失。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Cockpit | [`void SetControlMode(UnitControlMode mode)`](../Assets/Scripts/Features/Simulation/Cockpit.cs#L20) | public |
| Cockpit | [`void SetAnchored(bool value)`](../Assets/Scripts/Features/Simulation/Cockpit.cs#L25) | public |

## Assets/Scripts/Features/Simulation/ControlUnit.cs

// 职责：组织运行单元的方块、刚体、输入、总耐久与资源归属。 // 入口：ProtectExplosionContacts() / TryGetTotalDurability() / RefreshChildren()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ControlUnit | [`bool IsSceneSimulationActive(UnityEngine.SceneManagement.Scene scene)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L12) | public static |
| ControlUnit | [`void BindBuilding(BuildingRuntime building)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L32) | public |
| ControlUnit | [`void OnEnable()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L37) | private |
| ControlUnit | [`void OnDisable()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L45) | private |
| ControlUnit | [`void ProtectExplosionContacts(float duration)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L66) | public |
| ControlUnit | [`bool TryGetTotalDurability(out float current, out float maximum)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L78) | public |
| ControlUnit | [`void Start()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L101) | private |
| ControlUnit | [`void Update()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L113) | private |
| ControlUnit | [`void RefreshChildren()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L160) | public |
| ControlUnit | [`void AssignRuntimeOwnershipToBlocks(bool overwriteExisting)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L210) | public |
| ControlUnit | [`void EnsureRuntimeUnitId()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L231) | public |
| ControlUnit | [`void PlayEnd()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L239) | public |
| ControlUnit | [`void SetMovementInput(Vector3 worldDirection)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L249) | public |
| ControlUnit | [`void SetTarget(Transform newTarget)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L254) | public |
| ControlUnit | [`Vector3 GetPlayerMovementInput()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L259) | private |
| ControlUnit | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L302) | private |
| ControlUnit | [`void OnDestroy()`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L312) | private |
| RuntimeUnitMember | [`RuntimeUnitMember Ensure(GameObject obj, string unitId, UnitFaction faction)`](../Assets/Scripts/Features/Simulation/ControlUnit.cs#L325) | public static |

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
| ImpactPhysics | [`float BodyEnergyShare(float mass, float otherMass, bool otherIsStatic)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L28) | public static |
| ImpactPhysics | [`float Restitution(Collider first, Collider second, float normalSpeed)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L34) | public static |
| ImpactPhysics | [`int CombinePriority(PhysicsMaterialCombine mode)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L62) | private static |
| ImpactPhysics | [`Vector3 ClosestPoint(Collider collider, Vector3 position)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L77) | public static |
| ImpactPhysics | [`void ReportPlayerImpact(float energy, float impulse)`](../Assets/Scripts/Features/Simulation/ImpactPhysics.cs#L88) | public static |

## Assets/Scripts/Features/Simulation/ModularUnitFactory.cs

// 职责：为任意阵营从蓝图建立模块飞船，统一尺寸、货物、身份、连接和物理初始化。 // 入口：IsValidBlueprint() / Spawn()；不写存档、不控制波次、不决定玩家驾驶权。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ModularUnitFactory | [`bool IsValidBlueprint(BlockDataList dataList, string displayName)`](../Assets/Scripts/Features/Simulation/ModularUnitFactory.cs#L8) | public static |
| ModularUnitFactory | [`ControlUnit Spawn( BlockDataList blueprint, string displayName, Vector3 position, Quaternion rotation, UnitFaction faction)`](../Assets/Scripts/Features/Simulation/ModularUnitFactory.cs#L41) | public static |

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
| PlayManager | [`void BindSiege(SiegeSession session)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L21) | public |
| PlayManager | [`void Awake()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L63) | private |
| PlayManager | [`void FixedUpdate()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L68) | private |
| PlayManager | [`void PlayStart()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L100) | public |
| PlayManager | [`bool CanStartPlay(out string reason)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L148) | public |
| PlayManager | [`void RefreshGroup(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L183) | public |
| PlayManager | [`void SetPlayMode(bool lockView)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L214) | public |
| PlayManager | [`void CalculateVelocity()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L222) | private |
| PlayManager | [`void HandleSelection()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L231) | private |
| PlayManager | [`void SelectBlock(Block block)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L260) | private |
| PlayManager | [`void DeselectBlock()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L279) | public |
| PlayManager | [`void PlayEnd()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L285) | public |
| PlayManager | [`void AssignBlocksToParentGroups(List<Block> blocks)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L316) | public |
| PlayManager | [`void RegisterControlUnit(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L332) | public |
| PlayManager | [`void RegisterControlUnitWithoutCleanup(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L342) | internal |
| PlayManager | [`void UnregisterControlUnit(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L350) | public |
| PlayManager | [`IReadOnlyList<ControlUnit> GetControlUnits()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L358) | public |

## Assets/Scripts/Features/Simulation/Rack.cs

// 职责：管理机器人停泊容器及所属单元关系。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs

// 职责：保留含驾驶舱或最大分量的源刚体，仅迁移分离模块，并恢复捕获的源运动与归属。 // 入口：Rebuild()；先捕获所有源状态，再规划/迁移，最后局部刷新并一次执行外层清理。 // 约束：保留已有根的姿态与运行组件；新碎片不覆盖RuntimeUnitMember原始单位身份。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RuntimeGroupBuilder | [`void Rebuild(List<Block> blocks, GroupPhysicsSettings settings)`](../Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs#L15) | internal |
| RuntimeGroupBuilder | [`Dictionary<Block, SourceSnapshot> CaptureSourceBodies(List<Block> blocks)`](../Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs#L112) | private static |
| RuntimeGroupBuilder | [`List<GroupPlan> PlanGroups(List<List<Block>> groups, Dictionary<Block, SourceSnapshot> sources)`](../Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs#L141) | private static |
| RuntimeGroupBuilder | [`Dictionary<SourceSnapshot, GroupPlan> SelectRetainedGroups(List<GroupPlan> plans, Dictionary<Block, SourceSnapshot> sources)`](../Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs#L172) | private static |
| RuntimeGroupBuilder | [`void RestoreGroupPhysics(Rigidbody target, SourceSnapshot source, GroupPhysicsSettings settings)`](../Assets/Scripts/Features/Simulation/RuntimeGroupBuilder.cs#L211) | private static |

## Assets/Scripts/Features/Simulation/RuntimeGroupCleanup.cs

// 职责：按无驾驶舱组上限和玩家距离安排残骸清理，不立即销毁或更改归属。 // 入口：EnforceLimit() / EnforceDistance()；由 PlayManager 在原有时序调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RuntimeGroupCleanup | [`void EnforceLimit(List<ControlUnit> units, int maximumUselessGroups, DestroyManager destruction)`](../Assets/Scripts/Features/Simulation/RuntimeGroupCleanup.cs#L8) | internal static |
| RuntimeGroupCleanup | [`void EnforceDistance( List<ControlUnit> units, bool playMode, Transform playerRoot, float cleanupDistance, DestroyManager destruction)`](../Assets/Scripts/Features/Simulation/RuntimeGroupCleanup.cs#L51) | internal static |

## Assets/Scripts/Features/Simulation/SimulationWorkMetrics.cs

// 职责：按需累计连接遍历、刚体重建与完整刷新的CPU工作量，供可重复Play采样读取。 // 入口：Measure() / Read() / Reset()；默认关闭，嵌套阶段分别计时，不能相加为整帧耗时。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SimulationWorkMetrics | [`void RecordPhysicsNeighborQuery()`](../Assets/Scripts/Features/Simulation/SimulationWorkMetrics.cs#L44) | internal static |
| SimulationWorkMetrics | [`void RecordLegacyReferenceScan()`](../Assets/Scripts/Features/Simulation/SimulationWorkMetrics.cs#L52) | internal static |
| SimulationWorkMetrics | [`Scope Measure(Stage stage, int inputBlocks = 0)`](../Assets/Scripts/Features/Simulation/SimulationWorkMetrics.cs#L60) | public static |
| SimulationWorkMetrics | [`Counters Read(Stage stage)`](../Assets/Scripts/Features/Simulation/SimulationWorkMetrics.cs#L65) | public static |
| SimulationWorkMetrics | [`void Reset()`](../Assets/Scripts/Features/Simulation/SimulationWorkMetrics.cs#L70) | public static |
| Scope | [`void Dispose()`](../Assets/Scripts/Features/Simulation/SimulationWorkMetrics.cs#L89) | public |

## Assets/Scripts/Features/Simulation/UnitFaction.cs

// 职责：定义阵营和运行单元归属，支持友敌判断与残骸追踪。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/World/BuildingConnectionGraph.cs

// 职责：缓存单栋建筑的实际几何邻接；模块删除只更新受影响邻居，姿态/连接面/碰撞配置改变则重建。 // 入口：Prepare() / CollectNeighbors()；不创建连接视觉或跨建筑连边，不替代虚拟地面支撑规则。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingConnectionGraph | [`void Prepare(List<Block> blocks, Transform root)`](../Assets/Scripts/Features/World/BuildingConnectionGraph.cs#L15) | internal |
| BuildingConnectionGraph | [`void CollectNeighbors(Block block, List<Block> result)`](../Assets/Scripts/Features/World/BuildingConnectionGraph.cs#L109) | internal |
| BuildingConnectionGraph | [`bool TryGetGroundSupport(Block block, Vector2Int footprint, out bool supported)`](../Assets/Scripts/Features/World/BuildingConnectionGraph.cs#L126) | internal |
| BuildingConnectionGraph | [`void StoreGroundSupport(Block block, Vector2Int footprint, bool supported)`](../Assets/Scripts/Features/World/BuildingConnectionGraph.cs#L138) | internal |
| Node | [`bool IsValid(Block block)`](../Assets/Scripts/Features/World/BuildingConnectionGraph.cs#L165) | internal |

## Assets/Scripts/Features/World/BuildingRefit.cs

// 职责：先暂存建筑缺件，再按原蓝图恢复模块身份/姿态/耐久；存活模块保留原对象和货物。 // 入口：Apply()；调用者负责整备权限、空间净空和扣费，本类不替换建筑根或核心。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingRefit | [`bool Apply( BuildingRuntime building, List<Block> blocks, WorldBuildingManager manager, BlockDataList design, out string reason)`](../Assets/Scripts/Features/World/BuildingRefit.cs#L9) | internal static |

## Assets/Scripts/Features/World/BuildingRuntime.cs

// 职责：拥有一栋世界建筑的方块、虚拟支撑范围与刚体；处理损坏、断裂和脱锚。 // 入口：Initialize() / Activate() / DestroyBlock() / ReleaseAnchor() / Capture()。 // 生命周期：世界存档只捕获主体；断裂残骸由 WorldBuildingManager 限量管理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingRuntime | [`bool TryRefit(BlockDataList design, out string reason)`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L37) | internal |
| BuildingRuntime | [`void RefreshStructure()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L51) | internal |
| BuildingRuntime | [`void BeginDamageBatch()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L59) | internal |
| BuildingRuntime | [`void EndDamageBatch()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L64) | internal |
| BuildingRuntime | [`void Initialize(WorldBuildingManager owner, WorldBuildingState state)`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L74) | public |
| BuildingRuntime | [`IEnumerator InitializeRoutine(WorldBuildingManager owner, WorldBuildingState state)`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L82) | internal |
| BuildingRuntime | [`void Activate()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L130) | public |
| BuildingRuntime | [`IEnumerator ActivateRoutine()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L138) | internal |
| BuildingRuntime | [`void PrepareForUnload()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L181) | public |
| BuildingRuntime | [`WorldBuildingState Capture()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L189) | public |
| BuildingRuntime | [`void DestroyBlock(Block block)`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L242) | public |
| BuildingRuntime | [`void SplitDisconnected()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L288) | private |
| BuildingRuntime | [`bool HasPreparedGroundConnector(Block block)`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L374) | private |
| BuildingRuntime | [`bool HasGroundConnector(Block block)`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L387) | private |
| BuildingRuntime | [`void ReleaseAnchor()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L408) | public |
| BuildingRuntime | [`float TotalMass()`](../Assets/Scripts/Features/World/BuildingRuntime.cs#L419) | private |

## Assets/Scripts/Features/World/EnemyFlightPlayerChecks.cs

// 职责：显式Player路线读取既有敌机蓝图，验证自然AI飞行、供电和对玩家伤害。 // 入口：Run()；不改蓝图参数、不保存文件，结束清理本次敌机。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyFlightPlayerChecks | [`IEnumerator Run(ControlUnit player, EnemySpawner spawner, string blueprintName, Action<bool, string> check)`](../Assets/Scripts/Features/World/EnemyFlightPlayerChecks.cs#L12) | internal static |
| EnemyFlightPlayerChecks | [`bool IsFinite(Vector3 value)`](../Assets/Scripts/Features/World/EnemyFlightPlayerChecks.cs#L76) | private static |

## Assets/Scripts/Features/World/EnemyOutpostData.cs

// 职责：保存建筑附带的有限驻军蓝图与本地部署点，独立于用户敌机文件后续改动。 // 入口：Validate()；建筑蓝图缺少outpost字段时仍是普通建筑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyOutpostData | [`void Validate()`](../Assets/Scripts/Features/World/EnemyOutpostData.cs#L13) | public |
| OutpostGarrisonData | [`BlockDataList CreateBlueprint()`](../Assets/Scripts/Features/World/EnemyOutpostData.cs#L70) | public |

## Assets/Scripts/Features/World/OutpostEditorPlayerChecks.cs

// 职责：显式Development Player路线用隔离文件仓验证建筑编辑器快捷键、据点保存、完整模型与预览Shader。 // 入口：Run()；三个文件仓仅在测试期间反射替换，finally恢复，随后只读重载原玩家蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostEditorPlayerChecks | [`IEnumerator Run(Action<bool, string> check)`](../Assets/Scripts/Features/World/OutpostEditorPlayerChecks.cs#L17) | internal static |

## Assets/Scripts/Features/World/OutpostEncounterDirector.cs

// 职责：会话内管理据点有限驻军、击败记录及远距离休眠；退出探索即丢弃，不写永久存档。 // 入口：Tick() / Clear()；击败只由驾驶舱显式死亡通知，卸载不计击败。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostEncounterDirector | [`bool IsDefeated(string site, string guard)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L29) | public |
| OutpostEncounterDirector | [`bool TryGetDefender(string site, string guardId, out ControlUnit unit)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L34) | public |
| OutpostEncounterDirector | [`void Tick(Vector3 player)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L51) | public |
| OutpostEncounterDirector | [`bool Spawn(Encounter encounter, Guard guard, Vector3 position)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L170) | private |
| OutpostEncounterDirector | [`void UpdateParkedModel(Encounter encounter, Guard guard, Vector3 position, bool baseReady)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L242) | private |
| OutpostEncounterDirector | [`void ClearParkedModel(Guard guard)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L263) | private static |
| OutpostEncounterDirector | [`Bounds DeploymentBounds(BlockDataList blueprint, Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L273) | private static |
| OutpostEncounterDirector | [`void Hibernate(Guard guard)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L293) | private static |
| OutpostEncounterDirector | [`void RefreshOwner(Guard guard)`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L330) | private static |
| OutpostEncounterDirector | [`void Clear()`](../Assets/Scripts/Features/World/OutpostEncounterDirector.cs#L339) | public |

## Assets/Scripts/Features/World/OutpostHud.cs

// 职责：在游玩HUD显示最近据点剩余驻军及本次探索清剿数量。 // 入口：Update()；引用由OutpostHudAuthoring保存，不在运行时生成界面。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostHud | [`void Update()`](../Assets/Scripts/Features/World/OutpostHud.cs#L14) | private |
| OutpostHud | [`string Format(OutpostEncounterStatus? nearest, int clearedCount)`](../Assets/Scripts/Features/World/OutpostHud.cs#L26) | public |
| OutpostHud | [`string FitName(string name, string suffix)`](../Assets/Scripts/Features/World/OutpostHud.cs#L41) | private |

## Assets/Scripts/Features/World/OutpostPlayerChecks.cs

// 职责：显式Player烟测中验证真实据点部署、受损休眠恢复、驾驶舱击败与不重刷。 // 入口：Run()；只由-world-smoke-outposts启用，不写用户蓝图或永久进度。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostPlayerChecks | [`IEnumerator Run( WorldSessionManager session, ControlUnit player, Action<bool, string> check, Action<string> capture = null, bool salvage = false, bool recoverAllParts = false, bool movingTarget = false)`](../Assets/Scripts/Features/World/OutpostPlayerChecks.cs#L12) | public static |
| OutpostPlayerChecks | [`IEnumerator Flatten(IEnumerator root)`](../Assets/Scripts/Features/World/OutpostPlayerChecks.cs#L31) | private static |
| OutpostPlayerChecks | [`IEnumerator Scenario( WorldSessionManager session, ControlUnit player, Action<bool, string> check, Action<string> capture, bool salvage, bool recoverAllParts, bool movingTarget)`](../Assets/Scripts/Features/World/OutpostPlayerChecks.cs#L63) | private static |
| OutpostPlayerChecks | [`IEnumerator TravelTo(Transform player, Vector3 destination)`](../Assets/Scripts/Features/World/OutpostPlayerChecks.cs#L330) | private static |
| OutpostPlayerChecks | [`void Freeze(ControlUnit unit)`](../Assets/Scripts/Features/World/OutpostPlayerChecks.cs#L344) | private static |
| GuardObservation | [`void Sample(ControlUnit player, Durability health, float initialHealth)`](../Assets/Scripts/Features/World/OutpostPlayerChecks.cs#L376) | internal |
| GuardObservation | [`string Describe()`](../Assets/Scripts/Features/World/OutpostPlayerChecks.cs#L389) | internal |
| OutpostPlayerChecks | [`IEnumerator TravelWithDefender(Transform player, ControlUnit unit, Vector3 destination)`](../Assets/Scripts/Features/World/OutpostPlayerChecks.cs#L395) | private static |

## Assets/Scripts/Features/World/OutpostPursuitPlayerChecks.cs

// 职责：显式Player路线观察真实驻军追击移动目标、脱离索敌及重新接敌，不搬运或冻结敌机。 // 入口：Run()；玩家是2m/s移动的运动学测试目标，finally恢复其原位置，不改驻军参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostPursuitPlayerChecks | [`IEnumerator Run(ControlUnit player, IReadOnlyList<ControlUnit> defenders, Action<bool, string> check)`](../Assets/Scripts/Features/World/OutpostPursuitPlayerChecks.cs#L10) | internal static |
| Observation | [`void Sample(ControlUnit player, Durability health, float initialHealth)`](../Assets/Scripts/Features/World/OutpostPursuitPlayerChecks.cs#L100) | internal |
| Observation | [`string Describe()`](../Assets/Scripts/Features/World/OutpostPursuitPlayerChecks.cs#L112) | internal |

## Assets/Scripts/Features/World/OutpostSalvagePlayerChecks.cs

// 职责：显式Player路线验证正式据点货仓的原有内容掉落、吸附和机器人回收。 // 入口：Run()；仅-world-smoke-outpost-salvage启用，临时玩家模块在finally清理，不写用户数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostSalvagePlayerChecks | [`IEnumerator Run( BuildingRuntime building, ControlUnit player, Action<bool, string> check, bool recoverAllParts = false)`](../Assets/Scripts/Features/World/OutpostSalvagePlayerChecks.cs#L12) | internal static |
| OutpostSalvagePlayerChecks | [`GameObject Spawn(string resource, ControlUnit owner, Vector3 local, List<GameObject> fixtures)`](../Assets/Scripts/Features/World/OutpostSalvagePlayerChecks.cs#L170) | private static |

## Assets/Scripts/Features/World/ProceduralVoxelWorld.cs

// 职责：拥有有限或流式体素数据、分块网格与受影响块的重建队列。 // 入口：Generate()保留WorldLab；InitializeStreaming/LoadColumn供游玩会话逐帧准备地形。 // 约束：根Transform保持单位缩放；运行修改属于本次会话，不写玩家蓝图存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProceduralVoxelWorld | [`float SurfaceHeight(int x, int z)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L51) | public |
| ProceduralVoxelWorld | [`Vector3 FindSpawnSurface(Bounds construct)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L52) | public |
| ProceduralVoxelWorld | [`void Awake()`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L80) | private |
| ProceduralVoxelWorld | [`void Generate(System.Action<GameObject> registerCreated = null, System.Action<GameObject> destroyObsolete = null)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L99) | public |
| ProceduralVoxelWorld | [`void InitializeStreaming( WorldGenerationSettings settings, int sessionSeed, IReadOnlyList<BlockDataList> buildingTemplates = null)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L178) | public |
| ProceduralVoxelWorld | [`bool HasColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L203) | public |
| ProceduralVoxelWorld | [`bool IsColumnInBounds(Vector2Int coordinate)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L208) | public |
| ProceduralVoxelWorld | [`Vector2Int ColumnAt(Vector3 worldPosition)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L213) | public |
| ProceduralVoxelWorld | [`bool TryGetBuildingSite(Vector2Int coordinate, out Vector3 worldPosition, out Quaternion rotation)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L219) | public |
| ProceduralVoxelWorld | [`IEnumerator LoadColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L234) | public |
| ProceduralVoxelWorld | [`void UnloadColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L315) | public |
| ProceduralVoxelWorld | [`void ClearStreaming()`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L335) | public |
| ProceduralVoxelWorld | [`void DestroyChunk(VoxelWorldChunk chunk)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L354) | private |
| ProceduralVoxelWorld | [`void CacheChunks()`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L373) | private |
| ProceduralVoxelWorld | [`Vector3Int WorldToCell(Vector3 position)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L385) | public |
| ProceduralVoxelWorld | [`Vector3 CellCenter(Vector3Int cell)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L390) | public |
| ProceduralVoxelWorld | [`bool SetCell(Vector3Int cell, byte material)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L395) | public |
| ProceduralVoxelWorld | [`VoxelWorldEdits CaptureEdits()`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L408) | public |
| ProceduralVoxelWorld | [`void RestoreEdits(VoxelWorldEdits edits)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L418) | public |
| ProceduralVoxelWorld | [`int DamageSphere(Vector3 center, float radius, float energy)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L446) | public |
| ProceduralVoxelWorld | [`void QueueCell(Vector3Int cell)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L505) | private |
| ProceduralVoxelWorld | [`void QueueChunk(Vector3Int cell)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L516) | private |
| ProceduralVoxelWorld | [`void LateUpdate()`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L533) | private |
| ProceduralVoxelWorld | [`void FlushDirty(int budget = int.MaxValue, float milliseconds = float.PositiveInfinity)`](../Assets/Scripts/Features/World/ProceduralVoxelWorld.cs#L538) | public |

## Assets/Scripts/Features/World/StreamingWorldTerrain.cs

// 职责：按全局单元坐标确定性采样大世界地形、出生平台和建筑平台，无场景对象或全图数组。 // 入口：Sample() / TryGetBuildingSite()；高度缓存有界，区块卸载不改变同一种子的基线。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| StreamingWorldTerrain | [`float SurfaceHeight(int x, int z)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L26) | public |
| StreamingWorldTerrain | [`bool IsColumnInBounds(Vector2Int coordinate)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L49) | public |
| StreamingWorldTerrain | [`byte Sample(Vector3Int cell)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L55) | public |
| StreamingWorldTerrain | [`bool TryGetBuildingSite(Vector2Int coordinate, out Vector3 localPosition, out Quaternion rotation)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L66) | public |
| StreamingWorldTerrain | [`bool HasBuilding(Vector2Int coordinate)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L85) | private |
| StreamingWorldTerrain | [`int Height(int x, int z)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L100) | private |
| StreamingWorldTerrain | [`int PlateauHeight(int x, int z)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L139) | private |
| StreamingWorldTerrain | [`float RawHeight(int x, int z)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L144) | private |
| StreamingWorldTerrain | [`uint Hash(Vector2Int coordinate)`](../Assets/Scripts/Features/World/StreamingWorldTerrain.cs#L151) | private |

## Assets/Scripts/Features/World/VoxelDebrisPiece.cs

// 职责：拥有有限寿命的金属碎片刚体及单元BoxCollider，清理自身网格。 // 入口：Initialize()；碎片不是可建造Block，不进入玩家分组/供电/存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelDebrisPiece | [`void Initialize(List<Vector3Int> cells, float cellSize, Material[] materials, float lifetime)`](../Assets/Scripts/Features/World/VoxelDebrisPiece.cs#L11) | public |
| VoxelDebrisPiece | [`void Update()`](../Assets/Scripts/Features/World/VoxelDebrisPiece.cs#L49) | private |
| VoxelDebrisPiece | [`void OnDestroy()`](../Assets/Scripts/Features/World/VoxelDebrisPiece.cs#L57) | private |

## Assets/Scripts/Features/World/VoxelMeshData.cs

// 职责：保存不可变邻域快照和纯CPU网格结果，将工作线程计算与Unity对象写入分开。 // 入口：Capture()在主线程读取体素；Upload()在主线程创建Mesh，快照不引用可变世界。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelMeshData | [`Mesh Upload(Vector3Int start)`](../Assets/Scripts/Features/World/VoxelMeshData.cs#L18) | public |
| VoxelChunkSnapshot | [`VoxelChunkSnapshot Capture(VoxelWorldData data, Vector3Int start, int size)`](../Assets/Scripts/Features/World/VoxelMeshData.cs#L56) | public static |
| VoxelChunkSnapshot | [`byte Get(Vector3Int cell)`](../Assets/Scripts/Features/World/VoxelMeshData.cs#L73) | internal |
| VoxelChunkSnapshot | [`int Index(Vector3Int cell)`](../Assets/Scripts/Features/World/VoxelMeshData.cs#L78) | internal |
| VoxelChunkSnapshot | [`int Step(Vector3Int direction)`](../Assets/Scripts/Features/World/VoxelMeshData.cs#L83) | internal |
| VoxelChunkSnapshot | [`byte Get(int index)`](../Assets/Scripts/Features/World/VoxelMeshData.cs#L88) | internal |

## Assets/Scripts/Features/World/VoxelStructureCollapse.cs

// 职责：破坏后按预算判定金属支撑，将少量孤立结构转为有界物理碎片。 // 入口：RequestCheck()由世界成功破坏调用，LateUpdate避开未刷新的静态碰撞。 // 约束：仅土/岩邻接支撑，小片段研究模型，不等于全地形结构力学求解。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelStructureCollapse | [`void Awake()`](../Assets/Scripts/Features/World/VoxelStructureCollapse.cs#L28) | private |
| VoxelStructureCollapse | [`void RequestCheck()`](../Assets/Scripts/Features/World/VoxelStructureCollapse.cs#L33) | public |
| VoxelStructureCollapse | [`void LateUpdate()`](../Assets/Scripts/Features/World/VoxelStructureCollapse.cs#L38) | private |
| VoxelStructureCollapse | [`void ResolveDetached()`](../Assets/Scripts/Features/World/VoxelStructureCollapse.cs#L51) | public |
| VoxelStructureCollapse | [`void ClearDebris()`](../Assets/Scripts/Features/World/VoxelStructureCollapse.cs#L85) | public |
| VoxelStructureCollapse | [`void OnDisable()`](../Assets/Scripts/Features/World/VoxelStructureCollapse.cs#L101) | private |

## Assets/Scripts/Features/World/VoxelStructureSupport.cs

// 职责：按六邻接找金属连通结构，接触土/岩视为有支撑；只返回有限尺寸的无支撑片段。 // 入口：FindDetached()；只读取体素，不将土/岩地形转成动态刚体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelStructureSupport | [`List<List<Vector3Int>> FindDetached(VoxelWorldData data, int maximumCells, int maximumGroups)`](../Assets/Scripts/Features/World/VoxelStructureSupport.cs#L17) | public static |

## Assets/Scripts/Features/World/VoxelSurfaceMesher.cs

// 职责：从一格邻域快照合并同材质的暴露面，生成三材质贪心网格。 // 入口：Build()用于同步编辑；Generate()只计算数组，可在工作线程运行，Unity提交留在主线程。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelSurfaceMesher | [`Mesh Build(VoxelWorldData data, Vector3Int start, int chunkSize, float cellSize)`](../Assets/Scripts/Features/World/VoxelSurfaceMesher.cs#L61) | public static |
| VoxelSurfaceMesher | [`VoxelMeshData Generate(VoxelChunkSnapshot snapshot, float cellSize)`](../Assets/Scripts/Features/World/VoxelSurfaceMesher.cs#L67) | public static |
| VoxelSurfaceMesher | [`void MergeSlice( VoxelMeshData result, byte[] mask, int size, int face, Vector3Int origin, Vector3Int u, Vector3Int v, float scale)`](../Assets/Scripts/Features/World/VoxelSurfaceMesher.cs#L113) | private static |
| VoxelSurfaceMesher | [`void AddQuad( VoxelMeshData mesh, byte material, Vector3 corner, Vector3 u, Vector3 v, Vector3 normal, int width, int height, float scale)`](../Assets/Scripts/Features/World/VoxelSurfaceMesher.cs#L180) | private static |

## Assets/Scripts/Features/World/VoxelWorldChunk.cs

// 职责：持有一个分块的渲染/碰撞网格，替换时只清理本组件创建的临时Mesh。 // 入口：Rebuild()；烘焙资产引用不销毁，碰撞更新与渲染共用同一表面。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelWorldChunk | [`void Configure(Vector3Int cellStart)`](../Assets/Scripts/Features/World/VoxelWorldChunk.cs#L13) | public |
| VoxelWorldChunk | [`void Rebuild(VoxelWorldData data, int chunkSize, float cellSize, Material[] materials)`](../Assets/Scripts/Features/World/VoxelWorldChunk.cs#L18) | public |
| VoxelWorldChunk | [`void ApplyMesh(Mesh mesh, Material[] materials)`](../Assets/Scripts/Features/World/VoxelWorldChunk.cs#L23) | public |
| VoxelWorldChunk | [`void AdoptBakedMesh(Mesh asset)`](../Assets/Scripts/Features/World/VoxelWorldChunk.cs#L39) | public |
| VoxelWorldChunk | [`void OnDestroy()`](../Assets/Scripts/Features/World/VoxelWorldChunk.cs#L46) | private |
| VoxelWorldChunk | [`void ClearOwnedMesh()`](../Assets/Scripts/Features/World/VoxelWorldChunk.cs#L51) | public |
| VoxelWorldChunk | [`void ReleaseMesh()`](../Assets/Scripts/Features/World/VoxelWorldChunk.cs#L66) | private |

## Assets/Scripts/Features/World/VoxelWorldData.cs

// 职责：有限世界保存体素数组，大世界按需采样并仅保存本次会话的稀疏单元修改。 // 入口：Generate() / Set()；无场景或存档副作用，0为空气、1土、2岩、3金属。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelWorldData | [`byte Get(Vector3Int cell)`](../Assets/Scripts/Features/World/VoxelWorldData.cs#L32) | public |
| VoxelWorldData | [`bool Contains(Vector3Int cell)`](../Assets/Scripts/Features/World/VoxelWorldData.cs#L47) | public |
| VoxelWorldData | [`bool Set(Vector3Int cell, byte material)`](../Assets/Scripts/Features/World/VoxelWorldData.cs#L54) | public |
| VoxelWorldData | [`void Recount()`](../Assets/Scripts/Features/World/VoxelWorldData.cs#L88) | public |
| VoxelWorldData | [`void InitializeEmpty(Vector3Int dimensions)`](../Assets/Scripts/Features/World/VoxelWorldData.cs#L105) | public |
| VoxelWorldData | [`void Generate(Vector3Int dimensions, int seed)`](../Assets/Scripts/Features/World/VoxelWorldData.cs#L119) | public |
| VoxelWorldData | [`void InitializeStreaming(StreamingWorldTerrain terrain)`](../Assets/Scripts/Features/World/VoxelWorldData.cs#L177) | public |
| VoxelWorldData | [`uint Fingerprint()`](../Assets/Scripts/Features/World/VoxelWorldData.cs#L187) | public |

## Assets/Scripts/Features/World/VoxelWorldEdits.cs

// 职责：将体素修改保存为有版本/种子/基线指纹的差量；校验全部通过才返回新数据。 // 入口：Capture() / Restore()；不读写磁盘、不修改传入世界，拒绝不兼容或重复单元。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelWorldEdits | [`VoxelWorldEdits Capture(VoxelWorldData current, int worldSeed)`](../Assets/Scripts/Features/World/VoxelWorldEdits.cs#L22) | public static |
| VoxelWorldEdits | [`VoxelWorldData Restore(int expectedSeed, Vector3Int expectedSize)`](../Assets/Scripts/Features/World/VoxelWorldEdits.cs#L52) | public |

## Assets/Scripts/Features/World/WorldBuildingCatalog.cs

// 职责：可编辑的随项目建筑模板与初始布局；用户建筑文件由运行时面板另行读取。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/World/WorldBuildingDamage.cs

// 职责：顺序结算建筑范围伤害；爆炸连锁排队，避免递归销毁耗尽调用栈。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingDamage | [`void Apply(IReadOnlyList<BuildingRuntime> buildings, Vector3 center, float radius, float damage)`](../Assets/Scripts/Features/World/WorldBuildingDamage.cs#L18) | public |

## Assets/Scripts/Features/World/WorldBuildingFootprints.cs

// 职责：快照每套建筑真实支撑范围，并让地形和建筑生成使用同一确定性模板索引。 // 入口：构造后Get()读取；不包含驻军飞行范围，不访问场景或用户文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingFootprints | [`Bounds Get(int seed, Vector2Int column)`](../Assets/Scripts/Features/World/WorldBuildingFootprints.cs#L34) | public |
| WorldBuildingFootprints | [`int SelectTemplate(int seed, Vector2Int column, int count)`](../Assets/Scripts/Features/World/WorldBuildingFootprints.cs#L39) | public static |

## Assets/Scripts/Features/World/WorldBuildingLibrary.cs

// 职责：读取用户建筑目录的全部有效JSON，校验范围并覆盖同名模板；坏文件单独报告而不阻断世界。 // 入口：Load()；每次新会话重新读取，不修改用户蓝图文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingLibrary | [`IEnumerator LoadRoutine(string directory)`](../Assets/Scripts/Features/World/WorldBuildingLibrary.cs#L20) | internal |
| WorldBuildingLibrary | [`void CollectResources(string path, HashSet<string> resources)`](../Assets/Scripts/Features/World/WorldBuildingLibrary.cs#L58) | private static |
| WorldBuildingLibrary | [`void CollectBlueprintResources(BlockDataList blueprint, HashSet<string> resources)`](../Assets/Scripts/Features/World/WorldBuildingLibrary.cs#L81) | private static |
| WorldBuildingLibrary | [`void Load(string directory)`](../Assets/Scripts/Features/World/WorldBuildingLibrary.cs#L115) | internal |

## Assets/Scripts/Features/World/WorldBuildingManager.cs

// 职责：拥有 WorldLab 建筑实例、初始布局、地形支撑通知和有界残骸。 // 入口：Place() / Capture() / Restore() / ResetBuildings()；不调用玩家 SaveManager。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingManager | [`void OnEnable()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L40) | private |
| WorldBuildingManager | [`void Start()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L57) | private |
| WorldBuildingManager | [`void OnDisable()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L65) | private |
| WorldBuildingManager | [`bool CanPlace(BlockDataList blueprint, Vector3 position, Quaternion rotation, out string reason)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L76) | public |
| WorldBuildingManager | [`BuildingRuntime Place(BlockDataList blueprint, string name, Vector3 position, Quaternion rotation, UnitFaction faction)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L96) | public |
| WorldBuildingManager | [`BuildingRuntime RestoreBuilding(WorldBuildingState state)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L126) | public |
| WorldBuildingManager | [`IEnumerator RestoreBuildingRoutine(WorldBuildingState state, Action<BuildingRuntime> restored)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L145) | internal |
| WorldBuildingManager | [`void OptimizeVisuals(Block block)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L192) | internal |
| WorldBuildingManager | [`bool HasCapacity(int additionalBlocks)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L200) | public |
| WorldBuildingManager | [`void RemoveBuilding(BuildingRuntime building)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L211) | public |
| WorldBuildingManager | [`void PrepareForUnload()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L224) | public |
| WorldBuildingManager | [`void ClearSession()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L235) | public |
| WorldBuildingManager | [`void OnDestroy()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L242) | private |
| WorldBuildingManager | [`BuildingRuntime Stage(WorldBuildingState state, Transform parent)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L247) | private |
| WorldBuildingManager | [`WorldLabSnapshot Capture()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L265) | public |
| WorldBuildingManager | [`void Restore(WorldLabSnapshot snapshot)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L285) | public |
| WorldBuildingManager | [`void ResetBuildings()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L338) | public |
| WorldBuildingManager | [`void ClearBuildings()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L372) | private |
| WorldBuildingManager | [`void TerrainChanged(Bounds changed)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L388) | private |
| WorldBuildingManager | [`void FixedUpdate()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L399) | private |
| WorldBuildingManager | [`void DamageSphere(Vector3 center, float radius, float damage)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L416) | public |
| WorldBuildingManager | [`void CreateDebris(List<Block> group, Rigidbody source)`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L421) | internal |
| WorldBuildingManager | [`void ClearDebris()`](../Assets/Scripts/Features/World/WorldBuildingManager.cs#L464) | private |

## Assets/Scripts/Features/World/WorldBuildingPalette.cs

// 职责：WorldLab 建筑库面板、只含网格的放置预览与输入；模板读取失败不影响世界。 // 入口：Update() / OnGUI()；B开关建筑模式，Q/T旋转，左键放置，Esc取消。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingPalette | [`void Start()`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L38) | private |
| WorldBuildingPalette | [`void RefreshCatalog()`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L43) | public |
| WorldBuildingPalette | [`void AddTemplate(string name, string json)`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L84) | private |
| WorldBuildingPalette | [`void Update()`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L99) | private |
| WorldBuildingPalette | [`void BeginPlacement()`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L174) | public |
| WorldBuildingPalette | [`void AddPreviewMeshes(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3? size = null)`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L204) | private |
| WorldBuildingPalette | [`void Cancel()`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L244) | public |
| WorldBuildingPalette | [`void OnDisable()`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L259) | private |
| WorldBuildingPalette | [`void OnGUI()`](../Assets/Scripts/Features/World/WorldBuildingPalette.cs#L264) | private |

## Assets/Scripts/Features/World/WorldBuildingPlacement.cs

// 职责：在1m建筑网格与体素地形间转换位置，检测支撑、体素穿插和其他碰撞体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingPlacement | [`Vector3 Snap(ProceduralVoxelWorld world, Vector3 hit)`](../Assets/Scripts/Features/World/WorldBuildingPlacement.cs#L6) | public static |
| WorldBuildingPlacement | [`bool Check( ProceduralVoxelWorld world, BlockDataList blueprint, Vector3 position, Quaternion rotation, out string reason)`](../Assets/Scripts/Features/World/WorldBuildingPlacement.cs#L15) | public static |
| WorldBuildingPlacement | [`bool IntersectsTerrain(ProceduralVoxelWorld world, Vector3 center, Vector3 half, Quaternion rotation)`](../Assets/Scripts/Features/World/WorldBuildingPlacement.cs#L77) | private static |
| WorldBuildingPlacement | [`Vector3 LocalAnchor(int x, int z)`](../Assets/Scripts/Features/World/WorldBuildingPlacement.cs#L106) | public static |
| WorldBuildingPlacement | [`float Support(ProceduralVoxelWorld world, Vector2Int size, Transform pose)`](../Assets/Scripts/Features/World/WorldBuildingPlacement.cs#L111) | public static |
| WorldBuildingPlacement | [`Bounds Footprint(BuildingRuntime building)`](../Assets/Scripts/Features/World/WorldBuildingPlacement.cs#L129) | public static |

## Assets/Scripts/Features/World/WorldBuildingRenderCache.cs

// 职责：将世界建筑同父刚性装饰网格按材质合并，并在本会话的同类模块间共享。 // 入口：Apply()/Clear()；只替换表现，保留方块、碰撞、损坏及可移动炮塔/机械层级。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingRenderCache | [`void Apply(Block block)`](../Assets/Scripts/Features/World/WorldBuildingRenderCache.cs#L10) | internal |
| WorldBuildingRenderCache | [`bool IsVisible(Transform part, Transform model)`](../Assets/Scripts/Features/World/WorldBuildingRenderCache.cs#L85) | private static |
| WorldBuildingRenderCache | [`string RelativePath(Transform parent, Transform model)`](../Assets/Scripts/Features/World/WorldBuildingRenderCache.cs#L98) | private static |
| WorldBuildingRenderCache | [`Visual Build(Transform model, MeshRenderer[] renderers)`](../Assets/Scripts/Features/World/WorldBuildingRenderCache.cs#L109) | private static |
| WorldBuildingRenderCache | [`void Clear()`](../Assets/Scripts/Features/World/WorldBuildingRenderCache.cs#L189) | internal |

## Assets/Scripts/Features/World/WorldBuildingState.cs

// 职责：保存世界建筑独立身份、根姿态、蓝图快照与逐块耐久；坐标相对建筑底座。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/World/WorldBuildingStreaming.cs

// 职责：按世界区块生成建筑，将远处建筑休眠为快照，并保留本次探索中的损坏和货仓。 // 入口：LoadColumn() / UnloadDistantRoutine()；实例归WorldBuildingManager，缓存随会话退出丢弃。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingStreaming | [`void LoadColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/WorldBuildingStreaming.cs#L34) | internal |
| WorldBuildingStreaming | [`IEnumerator RestoreAvailable()`](../Assets/Scripts/Features/World/WorldBuildingStreaming.cs#L61) | internal |
| WorldBuildingStreaming | [`IEnumerator UnloadDistantRoutine(Vector2Int center)`](../Assets/Scripts/Features/World/WorldBuildingStreaming.cs#L106) | internal |
| WorldBuildingStreaming | [`int Distance(Vector2Int a, Vector2Int b)`](../Assets/Scripts/Features/World/WorldBuildingStreaming.cs#L147) | internal static |
| WorldBuildingStreaming | [`bool HasLoadedGround(Vector3 position, Quaternion rotation, Vector2Int size)`](../Assets/Scripts/Features/World/WorldBuildingStreaming.cs#L152) | private |

## Assets/Scripts/Features/World/WorldBuildingValidation.cs

// 职责：创建前校验建筑模板、实例状态和资源，拒绝部分加载与非法尺寸/坐标。 // 入口：ValidateBlueprint() / ValidateState()；不会修改资源或活动世界。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingValidation | [`void ValidateBlueprint(BlockDataList blueprint)`](../Assets/Scripts/Features/World/WorldBuildingValidation.cs#L11) | public static |
| WorldBuildingValidation | [`void ValidateState(WorldBuildingState state)`](../Assets/Scripts/Features/World/WorldBuildingValidation.cs#L71) | public static |
| WorldBuildingValidation | [`bool Finite(float value)`](../Assets/Scripts/Features/World/WorldBuildingValidation.cs#L93) | public static |
| WorldBuildingValidation | [`bool Finite(Vector3 value)`](../Assets/Scripts/Features/World/WorldBuildingValidation.cs#L98) | public static |
| WorldBuildingValidation | [`bool ValidRotation(Quaternion value)`](../Assets/Scripts/Features/World/WorldBuildingValidation.cs#L103) | public static |

## Assets/Scripts/Features/World/WorldCombatPlayerChecks.cs

// 职责：在显式Player烟测中验证输入推进、炮弹击毁与机器人回收入仓。 // 入口：Run()；只修改本进程临时夹具，迭代器释放时恢复输入设备与镜头。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldCombatPlayerChecks | [`IEnumerator Run(ControlUnit player, EnemySpawner spawner, Action<bool, string> check)`](../Assets/Scripts/Features/World/WorldCombatPlayerChecks.cs#L14) | internal static |
| WorldCombatPlayerChecks | [`GameObject Spawn(string resource, ControlUnit owner, Vector3 local, List<GameObject> fixtures)`](../Assets/Scripts/Features/World/WorldCombatPlayerChecks.cs#L135) | private static |
| WorldCombatPlayerChecks | [`BlockDataList EnemyCargoFixture()`](../Assets/Scripts/Features/World/WorldCombatPlayerChecks.cs#L144) | private static |

## Assets/Scripts/Features/World/WorldDistanceView.cs

// 职责：按列缓存无碰撞体素过渡环，再衔接低密度远景与距离雾。 // 入口：Refresh()分帧重建远景，Enter()/Dispose()成对应用和恢复相机/环境设置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldDistanceView | [`Vector2Int CenterAtFocus()`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L47) | private |
| WorldDistanceView | [`IEnumerator Refresh()`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L67) | internal |
| WorldDistanceView | [`IEnumerator Rebuild(Vector2Int center)`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L93) | private |
| WorldDistanceView | [`void UpdateRingVisibility()`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L232) | private |
| WorldDistanceView | [`void ReleaseExpiredRing(Vector2Int center, int radius)`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L250) | private |
| WorldDistanceView | [`MeshFilter BuildRingColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L270) | private |
| WorldDistanceView | [`void AddSkirt(List<Vector3> vertices, List<int> triangles, int a, int b)`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L292) | private |
| WorldDistanceView | [`void Enter(Camera view)`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L323) | internal |
| WorldDistanceView | [`void Dispose()`](../Assets/Scripts/Features/World/WorldDistanceView.cs#L350) | public |

## Assets/Scripts/Features/World/WorldGenerationSettings.cs

// 职责：保存大世界种子、区块预算、建筑平台及安全出生区的可编辑生成参数。 // 入口：WorldSessionManager.BeginPreparation()读取；不会写入玩家或WorldLab存档。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/World/WorldLabController.cs

// 职责：独立世界实验场的自由镜头、采掘/爆破/填补输入及性能读数。 // 入口：Update()读取Input System；不接管Main场景的玩家、蓝图与存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabController | [`void Awake()`](../Assets/Scripts/Features/World/WorldLabController.cs#L32) | private |
| WorldLabController | [`void OnEnable()`](../Assets/Scripts/Features/World/WorldLabController.cs#L41) | private |
| WorldLabController | [`void OnDisable()`](../Assets/Scripts/Features/World/WorldLabController.cs#L49) | private |
| WorldLabController | [`void Update()`](../Assets/Scripts/Features/World/WorldLabController.cs#L57) | private |
| WorldLabController | [`void MoveView(Keyboard keyboard, Mouse mouse)`](../Assets/Scripts/Features/World/WorldLabController.cs#L151) | private |
| WorldLabController | [`void ResetView()`](../Assets/Scripts/Features/World/WorldLabController.cs#L172) | private |
| WorldLabController | [`void TransferEdits(bool save)`](../Assets/Scripts/Features/World/WorldLabController.cs#L183) | private |
| WorldLabController | [`void OnGUI()`](../Assets/Scripts/Features/World/WorldLabController.cs#L223) | private |

## Assets/Scripts/Features/World/WorldLabDebrisProbe.cs

// 职责：记录真实帧物理碎片下落，并在完成后重生成清理测试碎片。 // 入口：Begin()；2秒后比较真实位置，非手动Physics.Simulate。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabDebrisProbe | [`void Begin(VoxelStructureCollapse owner, ProceduralVoxelWorld source)`](../Assets/Scripts/Features/World/WorldLabDebrisProbe.cs#L13) | public |
| WorldLabDebrisProbe | [`void FixedUpdate()`](../Assets/Scripts/Features/World/WorldLabDebrisProbe.cs#L21) | private |

## Assets/Scripts/Features/World/WorldLabPersistence.cs

// 职责：独立WorldLab差量JSON的原子保存、上一份备份与有大小限制的读取。 // 入口：Save() / Load()；仅写调用者传入路径，不接触玩家载具蓝图和货仓。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabPersistence | [`void Save(string path, VoxelWorldEdits edits)`](../Assets/Scripts/Features/World/WorldLabPersistence.cs#L10) | public static |
| WorldLabPersistence | [`void SaveSnapshot(string path, WorldLabSnapshot snapshot)`](../Assets/Scripts/Features/World/WorldLabPersistence.cs#L15) | public static |
| WorldLabPersistence | [`void WriteJson(string path, string json)`](../Assets/Scripts/Features/World/WorldLabPersistence.cs#L20) | private static |
| WorldLabPersistence | [`VoxelWorldEdits Load(string path)`](../Assets/Scripts/Features/World/WorldLabPersistence.cs#L57) | public static |
| WorldLabPersistence | [`WorldLabSnapshot LoadSnapshot(string path, float legacyCellSize)`](../Assets/Scripts/Features/World/WorldLabPersistence.cs#L68) | public static |
| WorldLabPersistence | [`string ReadJson(string path)`](../Assets/Scripts/Features/World/WorldLabPersistence.cs#L90) | private static |

## Assets/Scripts/Features/World/WorldLabPhysicsProbe.cs

// 职责：实验场真实帧落体与体素地面接触探针，输出接触证据而不驱动Physics.Simulate。 // 入口：Begin()由WorldLabChecks调用；10秒超时失败，结束移除探针对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabPhysicsProbe | [`void Begin(string path)`](../Assets/Scripts/Features/World/WorldLabPhysicsProbe.cs#L11) | public |
| WorldLabPhysicsProbe | [`void FixedUpdate()`](../Assets/Scripts/Features/World/WorldLabPhysicsProbe.cs#L17) | private |
| WorldLabPhysicsProbe | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/World/WorldLabPhysicsProbe.cs#L26) | private |
| WorldLabPhysicsProbe | [`void Save(bool contact)`](../Assets/Scripts/Features/World/WorldLabPhysicsProbe.cs#L34) | private |

## Assets/Scripts/Features/World/WorldLoadingPanel.cs

// 职责：呈现世界准备进度及取消入口；实际准备与模式切换由WorldSessionManager持有。 // 入口：Bind() / Show()；组件留在活动Canvas，隐藏仅改变CanvasGroup。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLoadingPanel | [`void Bind(WorldSessionManager owner)`](../Assets/Scripts/Features/World/WorldLoadingPanel.cs#L19) | public |
| WorldLoadingPanel | [`void Show(bool visible)`](../Assets/Scripts/Features/World/WorldLoadingPanel.cs#L39) | public |
| WorldLoadingPanel | [`void ShowStartup()`](../Assets/Scripts/Features/World/WorldLoadingPanel.cs#L63) | public |
| WorldLoadingPanel | [`void Update()`](../Assets/Scripts/Features/World/WorldLoadingPanel.cs#L69) | private |
| WorldLoadingPanel | [`void Cancel()`](../Assets/Scripts/Features/World/WorldLoadingPanel.cs#L96) | private |
| WorldLoadingPanel | [`void OnDestroy()`](../Assets/Scripts/Features/World/WorldLoadingPanel.cs#L101) | private |

## Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs

// 职责：仅在显式-world-smoke-report参数下自动验证真实Player的加载、游玩、区块移动和退出。 // 入口：命令行初始化；只替换临时运行时载具，不保存游戏数据，报告后正常退出进程。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldPlayerSmokeChecks | [`void Initialize()`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L69) | private static |
| WorldPlayerSmokeChecks | [`int ReadInteger(string[] args, int index, string name, int minimum, int maximum)`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L135) | private static |
| WorldPlayerSmokeChecks | [`void Start()`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L145) | private |
| WorldPlayerSmokeChecks | [`void Update()`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L162) | private |
| WorldPlayerSmokeChecks | [`IEnumerator Scenario()`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L191) | private |
| WorldPlayerSmokeChecks | [`BlockDataList EnemyFixture()`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L463) | private static |
| WorldPlayerSmokeChecks | [`IEnumerator ValidateEnemyBlueprints(EnemySpawner spawner, Vector3 origin)`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L499) | private |
| WorldPlayerSmokeChecks | [`void Check(bool valid, string name)`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L559) | private |
| WorldPlayerSmokeChecks | [`void Next(string name)`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L572) | private |
| WorldPlayerSmokeChecks | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L585) | private |
| WorldPlayerSmokeChecks | [`void Save()`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L593) | private |
| WorldPlayerSmokeChecks | [`void Finish()`](../Assets/Scripts/Features/World/WorldPlayerSmokeChecks.cs#L598) | private |

## Assets/Scripts/Features/World/WorldPlayerStreamingGuard.cs

// 职责：在地形碰撞尚未就绪时暂存载具姿态/速度，限制世界边界并在区块完成后恢复。 // 入口：Tick() / Release()；WorldSessionManager在FixedUpdate调用，退出或销毁必须释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldPlayerStreamingGuard | [`void Tick(Transform player)`](../Assets/Scripts/Features/World/WorldPlayerStreamingGuard.cs#L31) | internal |
| WorldPlayerStreamingGuard | [`bool HasTerrainForBounds(Transform player, Vector3 target, Vector3 prediction)`](../Assets/Scripts/Features/World/WorldPlayerStreamingGuard.cs#L99) | private |
| WorldPlayerStreamingGuard | [`Bounds CaptureLocalBounds(Transform construct)`](../Assets/Scripts/Features/World/WorldPlayerStreamingGuard.cs#L133) | internal static |
| WorldPlayerStreamingGuard | [`void Release()`](../Assets/Scripts/Features/World/WorldPlayerStreamingGuard.cs#L158) | internal |

## Assets/Scripts/Features/World/WorldRegionStreaming.cs

// 职责：按距离选择下一个地形列，先休眠建筑再卸载远处网格，并在列完成后装入建筑。 // 入口：LoadNext()；由WorldSessionManager的可取消协程逐帧驱动，无模式/UI副作用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldRegionStreaming | [`IEnumerator LoadNext(Vector2Int center)`](../Assets/Scripts/Features/World/WorldRegionStreaming.cs#L32) | internal |
| WorldRegionStreaming | [`bool FindNextColumn(Vector2Int center, out Vector2Int coordinate)`](../Assets/Scripts/Features/World/WorldRegionStreaming.cs#L100) | private |
| WorldRegionStreaming | [`IEnumerator RetireDistant(Vector2Int center)`](../Assets/Scripts/Features/World/WorldRegionStreaming.cs#L126) | private |

## Assets/Scripts/Features/World/WorldReturnPlayerChecks.cs

// 职责：显式Player隔离文件仓，验证正常返航写盘、重载、死亡清货与再次出发。 // 入口：Run()；固定两模块夹具，仅写唯一缓存目录，finally恢复原文件仓及名称。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldReturnPlayerChecks | [`IEnumerator Run(Action<bool, string> check)`](../Assets/Scripts/Features/World/WorldReturnPlayerChecks.cs#L13) | internal static |

## Assets/Scripts/Features/World/WorldSessionManager.cs

// 职责：在Main建造期间分帧预热世界，进入游玩后启用并跟踪区块，退出即丢弃整个探索会话。 // 入口：BeginPreparation() / RequestPlay() / EnterPlay() / EndSession()；不读写玩家或WorldLab世界存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionManager | [`void Start()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L68) | private |
| WorldSessionManager | [`void OnEnable()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L76) | private |
| WorldSessionManager | [`void BeginPreparation()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L85) | public |
| WorldSessionManager | [`IEnumerator PrepareWorld()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L125) | private |
| WorldSessionManager | [`void RequestPlay()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L174) | public |
| WorldSessionManager | [`void CancelPlayRequest()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L191) | public |
| WorldSessionManager | [`void CompletePendingPlay()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L197) | private |
| WorldSessionManager | [`bool CanEnterPlay(Transform construct, out string reason)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L209) | public |
| WorldSessionManager | [`void EnterPlay(Transform construct)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L231) | public |
| WorldSessionManager | [`IEnumerator EnableWorld()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L263) | private |
| WorldSessionManager | [`void FinishEntering()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L297) | private |
| WorldSessionManager | [`void EndSession()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L306) | public |
| WorldSessionManager | [`IEnumerator RunGuarded(int version, IEnumerator work)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L311) | private |
| WorldSessionManager | [`IEnumerator StreamWorld()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L342) | private |
| WorldSessionManager | [`void FixedUpdate()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L390) | private |
| WorldSessionManager | [`void Update()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L415) | private |
| WorldSessionManager | [`Bounds ConstructBounds(Transform construct)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L430) | private static |
| WorldSessionManager | [`void SetBuildEnvironmentVisible(bool visible)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L455) | private |
| WorldSessionManager | [`void StopPreparation()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L485) | private |
| WorldSessionManager | [`void Fail(Exception error)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L517) | private |
| WorldSessionManager | [`void OnDisable()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L532) | private |

## Assets/Scripts/Features/World/WorldTerrainTransitionMesh.cs

// 职责：生成无碰撞体素过渡列，保留格级轮廓并合并同高顶面。 // 入口：WorldDistanceView 分帧调用 Build()；返回的 Mesh 由调用者释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldTerrainTransitionMesh | [`Mesh Build(ProceduralVoxelWorld world, WorldGenerationSettings settings, Vector2Int coordinate)`](../Assets/Scripts/Features/World/WorldTerrainTransitionMesh.cs#L9) | internal static |
| WorldTerrainTransitionMesh | [`void AddRingWall( List<Vector3> points, List<int> faces, List<int> soilFaces, float cell, Vector3 first, Vector3 last, float bottom)`](../Assets/Scripts/Features/World/WorldTerrainTransitionMesh.cs#L104) | private static |
| WorldTerrainTransitionMesh | [`void AddRingFace( List<Vector3> points, List<int> faces, Vector3 a, Vector3 b, Vector3 c, Vector3 d)`](../Assets/Scripts/Features/World/WorldTerrainTransitionMesh.cs#L138) | private static |
