# 自有代码导航

由 Roslyn 实际语法节点生成；脚本介绍定位职责，方法链接定位实现。不是行为测试报告。完整入口链见 [RuntimeFlow](RuntimeFlow.md)。

## 代码文件夹位置

相对于项目根；完整列出 Roslyn 自有脚本清单与 [ToolIndex](ToolIndex.md) 所收录工具的文件及父目录。每层主要代码直接列在其功能目录，子功能继续细分。分类决策见 [CodeLayout](CodeLayout.md)。Assets/Scripts/Features 为运行时代码，Assets/Editor 为编辑器制作/验证工具；Tools、Blueprints 和 ArtSource 为游戏运行时之外的开发工具。不含第三方、Package、缓存、构建产物及历史归档。由 generate_navigation.py 同步生成；下方各脚本章节可查看具体职责和方法。

```text
HY-Sandbox/
├── ArtSource/
│   └── VfxGraph/
│       ├── prepare_textures.py
│       └── verify_assets.py
├── Assets/
│   ├── Editor/
│   │   ├── Architecture/
│   │   │   ├── ArchitecturePlayProbe.cs
│   │   │   ├── DetachedVehicleChecks.cs
│   │   │   ├── EditableComponentBaker.cs
│   │   │   ├── EditableComponentChecks.cs
│   │   │   ├── FragmentRetentionChecks.cs
│   │   │   ├── HoverControlPlayChecks.cs
│   │   │   ├── PresentationPlayChecks.cs
│   │   │   └── SharedMechanismChecks.cs
│   │   ├── Art/
│   │   │   ├── BlockArtIntegrator.cs
│   │   │   ├── Authoring/
│   │   │   │   ├── IndustrialArtGenerator.cs
│   │   │   │   └── SpaceKitCurator.cs
│   │   │   ├── Preview/
│   │   │   │   └── BlockArtPreview.cs
│   │   │   └── Validation/
│   │   │       ├── BlockArtDependencies.cs
│   │   │       ├── BlockArtPlayProbe.cs
│   │   │       └── BlockArtValidation.cs
│   │   ├── Bots/
│   │   │   ├── BotNavigationPlayProbe.cs
│   │   │   └── BotNavigationPolicyMigration.cs
│   │   ├── Build/
│   │   │   └── AutoBuildTool.cs
│   │   ├── Building/
│   │   │   ├── BuildingEditorAssets.cs
│   │   │   ├── BlockDataTables/
│   │   │   │   ├── BlockCsvAuthoring.cs
│   │   │   │   ├── BlockCsvChecks.cs
│   │   │   │   ├── BlockCsvPostprocessor.cs
│   │   │   │   └── BlockCsvRegressionChecks.cs
│   │   │   ├── Interface/
│   │   │   │   ├── BuildHotbarAuthoring.cs
│   │   │   │   ├── BuildInteractionSetup.cs
│   │   │   │   └── BuildPaletteBaker.cs
│   │   │   ├── Materials/
│   │   │   │   ├── BlockMaterialAuthoring.cs
│   │   │   │   └── BlockMaterialChecks.cs
│   │   │   ├── Outposts/
│   │   │   │   └── OutpostEditorAuthoring.cs
│   │   │   └── Validation/
│   │   │       ├── BaseLibraryChecks.cs
│   │   │       ├── BlueprintCapacityChecks.cs
│   │   │       ├── BlueprintCapacityPlayChecks.cs
│   │   │       ├── BlueprintIntegrityAudit.cs
│   │   │       ├── BlueprintSupportAudit.cs
│   │   │       ├── BuildHotbarChecks.cs
│   │   │       ├── BuildingEditorPlayProbe.cs
│   │   │       ├── BuildNavigationPlayChecks.cs
│   │   │       ├── BuildPalettePlayProbe.cs
│   │   │       └── WorldBuildingFootprintChecks.cs
│   │   ├── Combat/
│   │   │   ├── EnemyAvoidanceLabAuthoring.cs
│   │   │   ├── EnemyLabComplexAuthoring.cs
│   │   │   ├── EnemyLabExpansionAuthoring.cs
│   │   │   ├── ExplosionAuthoring.cs
│   │   │   ├── SkirmishArenaAuthoring.cs
│   │   │   ├── Diagnostics/
│   │   │   │   ├── CombatDiagnosticsAuthoring.cs
│   │   │   │   ├── FlightCourseChecks.cs
│   │   │   │   ├── NavigationSensorChecks.cs
│   │   │   │   ├── PropulsionInputChecks.cs
│   │   │   │   ├── SkirmishNavigationAuthoring.cs
│   │   │   │   └── SkirmishNavigationChecks.cs
│   │   │   ├── Interface/
│   │   │   │   ├── CombatHudSetup.cs
│   │   │   │   ├── HealthHudPreview.cs
│   │   │   │   ├── SharedCombatHudAuthoring.cs
│   │   │   │   └── SharedCombatHudPreview.cs
│   │   │   ├── Turrets/
│   │   │   │   └── TurretAssetBaker.cs
│   │   │   └── Validation/
│   │   │       ├── AiUpgradePlayChecks.cs
│   │   │       ├── CombatHudPlayProbe.cs
│   │   │       ├── EnemyBlueprintPoolChecks.cs
│   │   │       ├── EnemyLabEfficiencyChecks.cs
│   │   │       ├── EnemyLabExpansionChecks.cs
│   │   │       ├── EnemyLabPlayChecks.cs
│   │   │       ├── EnemyLabViewChecks.cs
│   │   │       ├── ExplosionIntegrationChecks.cs
│   │   │       ├── NightInteractionChecks.cs
│   │   │       ├── SkirmishArenaChecks.cs
│   │   │       ├── SkirmishPerformanceChecks.cs
│   │   │       ├── TurretFlightPlayProbe.cs
│   │   │       └── TurretRuntimeChecks.cs
│   │   ├── Diagnostics/
│   │   │   ├── ConcaveTerrainChecks.cs
│   │   │   ├── PhysicsSandboxProbe.cs
│   │   │   ├── ProfilerCaptureAnalysis.cs
│   │   │   ├── UpgradeCompatibilityChecks.cs
│   │   │   └── Benchmarks/
│   │   │       ├── FleetPresentationBenchmark.cs
│   │   │       ├── IndustrialMotionChecks.cs
│   │   │       ├── PowerPerformanceChecks.cs
│   │   │       ├── PresentationPerformanceChecks.cs
│   │   │       └── ProjectilePoolChecks.cs
│   │   ├── Flight/
│   │   │   └── ThrusterHudSetup.cs
│   │   ├── MainMenu/
│   │   │   ├── MainMenuAuthoring.cs
│   │   │   ├── MainMenuPlayChecks.cs
│   │   │   ├── NightMenuAuthoring.cs
│   │   │   └── WorkshopLayoutPreview.cs
│   │   ├── Navigation/
│   │   │   └── Validation/
│   │   │       ├── EnemyPredictionPlayChecks.cs
│   │   │       ├── NavigationCombatPlayChecks.cs
│   │   │       └── SharedNavigationPlayChecks.cs
│   │   ├── Power/
│   │   │   └── ActivePowerPlayChecks.cs
│   │   ├── Presentation/
│   │   │   ├── InterfaceBaker.cs
│   │   │   ├── Attract/
│   │   │   │   └── BattleAttractAuthoring.cs
│   │   │   ├── Camera/
│   │   │   │   └── SceneCameraPreviewSync.cs
│   │   │   ├── Effects/
│   │   │   │   ├── BlockVfxBaker.cs
│   │   │   │   ├── CombatVfxBaker.cs
│   │   │   │   ├── DebrisFireBaker.cs
│   │   │   │   ├── EnemySpawnPortalBaker.cs
│   │   │   │   ├── NativeVfxLibraryBuilder.cs
│   │   │   │   ├── NativeVfxMigration.cs
│   │   │   │   └── VfxGraphAuthoring.cs
│   │   │   ├── Interface/
│   │   │   │   ├── BlockStatusSetup.cs
│   │   │   │   ├── DropdownTemplateAuthoring.cs
│   │   │   │   └── ReturnStatusSetup.cs
│   │   │   ├── Resources/
│   │   │   │   ├── BlockVfxLibraryEditor.cs
│   │   │   │   ├── PresentationAssetIndex.cs
│   │   │   │   ├── PresentationResourceWindow.cs
│   │   │   │   └── VfxCatalogMigration.cs
│   │   │   └── Validation/
│   │   │       ├── CombatVfxCapture.cs
│   │   │       ├── CombatVfxChecks.cs
│   │   │       ├── NativeVfxPlaybackProbe.cs
│   │   │       ├── PresentationResourceChecks.cs
│   │   │       ├── VfxCatalogChecks.cs
│   │   │       ├── VfxPoolBudgetChecks.cs
│   │   │       └── VfxPoolChecks.cs
│   │   ├── Salvage/
│   │   │   ├── CargoReturnFileChecks.cs
│   │   │   ├── SalvageAssetBaker.cs
│   │   │   └── SalvagePlayProbe.cs
│   │   ├── Siege/
│   │   │   ├── SiegeConstructionPlayChecks.cs
│   │   │   ├── SiegePlayChecks.cs
│   │   │   ├── SiegeSceneAuthoring.cs
│   │   │   ├── Authoring/
│   │   │   │   ├── SiegeArenaLookAuthoring.cs
│   │   │   │   ├── SiegeBaseBlueprintAuthoring.cs
│   │   │   │   ├── SiegeBlueprintSelectionAuthoring.cs
│   │   │   │   ├── SiegeCleanupAuthoring.cs
│   │   │   │   ├── SiegeCombatBaseAuthoring.cs
│   │   │   │   ├── SiegeCommandHudAuthoring.cs
│   │   │   │   ├── SiegeConstructionAuthoring.cs
│   │   │   │   ├── SiegeGameplayHudAuthoring.cs
│   │   │   │   ├── SiegeInterfaceAuthoring.cs
│   │   │   │   ├── SiegeIslandAuthoring.cs
│   │   │   │   ├── SiegeMainInterfaceAuthoring.cs
│   │   │   │   ├── SiegeMixedStressAuthoring.cs
│   │   │   │   ├── SiegeObserverCameraAuthoring.cs
│   │   │   │   ├── SiegeProgressAuthoring.cs
│   │   │   │   └── SiegeStressAuthoring.cs
│   │   │   ├── Benchmarks/
│   │   │   │   ├── SiegeBuildingBenchmark.cs
│   │   │   │   ├── SiegePerformanceCapture.cs
│   │   │   │   ├── SiegeRegroupBaseline.cs
│   │   │   │   ├── SiegeRegroupBenchmark.cs
│   │   │   │   ├── SiegeTopologyBaseline.cs
│   │   │   │   └── SiegeTransientEffectBenchmark.cs
│   │   │   ├── Examples/
│   │   │   │   ├── SiegeBaseExamplesAuthoring.cs
│   │   │   │   ├── SiegeBaseExamplesChecks.cs
│   │   │   │   └── SiegeCompletionIntegration.cs
│   │   │   ├── Hangars/
│   │   │   │   ├── HangarAssetChecks.cs
│   │   │   │   ├── HangarPrefabAuthoring.cs
│   │   │   │   ├── HangarSceneIntegration.cs
│   │   │   │   ├── HangarShellChecks.cs
│   │   │   │   ├── HangarWorkshopChecks.cs
│   │   │   │   ├── ModularBaseExampleAuthoring.cs
│   │   │   │   ├── SiegeFlightAuthoring.cs
│   │   │   │   ├── SiegeHangarHostChecks.cs
│   │   │   │   ├── SiegeHangarInterfaceAuthoring.cs
│   │   │   │   ├── SiegeHangarPlayChecks.cs
│   │   │   │   └── SiegeReturnChecks.cs
│   │   │   ├── Player/
│   │   │   │   └── SiegePlayerBuild.cs
│   │   │   └── Validation/
│   │   │       ├── SiegeCleanupChecks.cs
│   │   │       ├── SiegeIslandChecks.cs
│   │   │       ├── Buildings/
│   │   │       │   ├── SiegeBuildingChecks.cs
│   │   │       │   ├── SiegeBuildingDamageChecks.cs
│   │   │       │   ├── SiegeBuildingGraphChecks.cs
│   │   │       │   ├── SiegeBuildingPowerProbe.cs
│   │   │       │   └── SiegeFoundationCacheChecks.cs
│   │   │       ├── Fleet/
│   │   │       │   ├── SiegeBlueprintCatalogChecks.cs
│   │   │       │   ├── SiegeFleetChecks.cs
│   │   │       │   ├── SiegePriorityTargetChecks.cs
│   │   │       │   ├── SiegePriorityTargetFlight.cs
│   │   │       │   ├── SiegeRegroupChecks.cs
│   │   │       │   ├── SiegeRuntimeConnectionChecks.cs
│   │   │       │   ├── SiegeStrandedCockpitChecks.cs
│   │   │       │   ├── SiegeTopologyChecks.cs
│   │   │       │   ├── SiegeWorkshopChecks.cs
│   │   │       │   └── SiegeWreckBudgetChecks.cs
│   │   │       ├── Interface/
│   │   │       │   ├── SiegeCombatFeedbackChecks.cs
│   │   │       │   ├── SiegeObserverCameraChecks.cs
│   │   │       │   ├── SiegeObserverInputChecks.cs
│   │   │       │   ├── SiegeSceneInteractionChecks.cs
│   │   │       │   └── SiegeUsabilityChecks.cs
│   │   │       └── Rules/
│   │   │           ├── SiegeDestructionLogChecks.cs
│   │   │           ├── SiegeMixedStressChecks.cs
│   │   │           ├── SiegePlayerSurveyChecks.cs
│   │   │           ├── SiegeRulesChecks.cs
│   │   │           ├── SiegeStressCancellationChecks.cs
│   │   │           └── SiegeTransientEffectChecks.cs
│   │   ├── Simulation/
│   │   │   ├── GameModeAuthoring.cs
│   │   │   └── GameModePlayChecks.cs
│   │   └── World/
│   │       ├── WorldSessionAuthoring.cs
│   │       ├── Buildings/
│   │       │   └── WorldBuildingAuthoring.cs
│   │       ├── Outposts/
│   │       │   ├── OutpostContentAuthoring.cs
│   │       │   └── OutpostHudAuthoring.cs
│   │       ├── Player/
│   │       │   └── WorldPlayerBuildChecks.cs
│   │       ├── Terrain/
│   │       │   └── ProceduralVoxelWorldEditor.cs
│   │       ├── Validation/
│   │       │   ├── Outposts/
│   │       │   │   ├── OutpostContentChecks.cs
│   │       │   │   └── OutpostResidentChecks.cs
│   │       │   ├── Session/
│   │       │   │   ├── WorldBuildingPlayProbe.cs
│   │       │   │   ├── WorldSessionEdgeChecks.cs
│   │       │   │   ├── WorldSessionPerformanceChecks.cs
│   │       │   │   └── WorldSessionPlayChecks.cs
│   │       │   ├── Terrain/
│   │       │   │   ├── VoxelMeshingChecks.cs
│   │       │   │   └── WorldTerrainIntegrationChecks.cs
│   │       │   └── WorldLab/
│   │       │       ├── WorldLabChecks.cs
│   │       │       ├── WorldLabCollapseChecks.cs
│   │       │       ├── WorldLabPerformanceChecks.cs
│   │       │       ├── WorldLabPersistenceChecks.cs
│   │       │       └── WorldLabSoakChecks.cs
│   │       └── WorldLab/
│   │           └── WorldLabAuthoring.cs
│   └── Scripts/
│       └── Features/
│           ├── Bootstrap/
│           │   ├── GameManager.cs
│           │   ├── RuntimeObjectTemplates.cs
│           │   └── SceneObjectQuery.cs
│           ├── Bots/
│           │   ├── Bot.cs
│           │   ├── Collection/
│           │   │   └── CollectionBot.cs
│           │   ├── Navigation/
│           │   │   ├── BotFlightMotion.cs
│           │   │   └── BotNavigationPolicy.cs
│           │   ├── Presentation/
│           │   │   ├── BotFlightEffects.cs
│           │   │   └── BotNavigationGizmos.cs
│           │   └── Repair/
│           │       ├── RepairBeamView.cs
│           │       ├── RepairBot.cs
│           │       └── RepairTargetSearch.cs
│           ├── Building/
│           │   ├── BuildManager.cs
│           │   ├── SaveManager.cs
│           │   ├── Actions/
│           │   │   ├── ActionManager.cs
│           │   │   ├── AddBlockAction.cs
│           │   │   ├── BlockActionState.cs
│           │   │   ├── DeleteBlockAction.cs
│           │   │   ├── GroupAction.cs
│           │   │   ├── IBlockAction.cs
│           │   │   ├── MoveBlockAction.cs
│           │   │   └── RotateBlockAction.cs
│           │   ├── BlockDataTables/
│           │   │   ├── BlockCsvBinding.cs
│           │   │   ├── BlockCsvDocument.cs
│           │   │   └── BlockDataTableSchema.cs
│           │   ├── BlueprintLoading/
│           │   │   ├── BaseBlueprintMigration.cs
│           │   │   ├── BlockData.cs
│           │   │   ├── BlockPrefabCache.cs
│           │   │   ├── BlueprintBlockFactory.cs
│           │   │   ├── BlueprintFileStore.cs
│           │   │   ├── BlueprintLoadCamera.cs
│           │   │   ├── BlueprintLoader.cs
│           │   │   ├── BlueprintLoadSettings.cs
│           │   │   ├── BlueprintModelPreview.cs
│           │   │   ├── BlueprintStatistics.cs
│           │   │   └── BuildTargetContext.cs
│           │   ├── BuildingEditor/
│           │   │   ├── BuildingEditorPanel.cs
│           │   │   ├── BuildingFootprint.cs
│           │   │   ├── BuildingFoundation.cs
│           │   │   ├── BuildingFoundationData.cs
│           │   │   ├── BuildingOutpostEditor.cs
│           │   │   ├── OutpostBaseDesigner.cs
│           │   │   └── OutpostDeploymentPreview.cs
│           │   ├── Interaction/
│           │   │   ├── BuildBlueprintEditor.cs
│           │   │   ├── BuildHandleAxis.cs
│           │   │   ├── BuildManipulation.cs
│           │   │   ├── BuildPlacementGeometry.cs
│           │   │   ├── BuildPreview.cs
│           │   │   ├── BuildSelection.cs
│           │   │   ├── ConnectorPlacementHints.cs
│           │   │   ├── MoveAxisHandle.cs
│           │   │   └── RotateAxisHandle.cs
│           │   ├── Interface/
│           │   │   ├── ActionCounterUI.cs
│           │   │   ├── BlueprintUIPanel.cs
│           │   │   ├── BuildHotbar.cs
│           │   │   ├── BuildHotbarCatalog.cs
│           │   │   ├── BuildHotbarSlot.cs
│           │   │   ├── BuildPalette.cs
│           │   │   ├── BuildPaletteItem.cs
│           │   │   └── SaveUIPanel.cs
│           │   └── Materials/
│           │       ├── BlockMaterial.cs
│           │       ├── BlockMaterialCatalog.cs
│           │       ├── BuildMaterialSelector.cs
│           │       └── MaterialBreakEffect.cs
│           ├── Combat/
│           │   ├── DestroyManager.cs
│           │   ├── Damage/
│           │   │   ├── CollisionDamageResolver.cs
│           │   │   ├── Durability.cs
│           │   │   ├── ExplosionDamageSession.cs
│           │   │   ├── ExplosionPropagation.cs
│           │   │   └── SalvageCleanupSession.cs
│           │   ├── Enemies/
│           │   │   ├── EnemyBlueprintFactory.cs
│           │   │   ├── EnemyController.cs
│           │   │   ├── EnemyIdentity.cs
│           │   │   └── EnemySpawner.cs
│           │   ├── Interface/
│           │   │   ├── CombatHud.cs
│           │   │   ├── CombatHudTheme.cs
│           │   │   ├── Nameplates/
│           │   │   │   └── CombatNameplate.cs
│           │   │   ├── Notifications/
│           │   │   │   ├── CombatFeedItem.cs
│           │   │   │   └── CombatNotifications.cs
│           │   │   ├── Panels/
│           │   │   │   └── CombatHudPanels.cs
│           │   │   └── Threats/
│           │   │       ├── CombatThreatIcon.cs
│           │   │       ├── CombatThreatMarkers.cs
│           │   │       └── CombatThreatView.cs
│           │   ├── Meteors/
│           │   │   ├── Meteor.cs
│           │   │   └── MeteorShower.cs
│           │   ├── Skirmish/
│           │   │   ├── SkirmishHud.cs
│           │   │   ├── SkirmishMatch.cs
│           │   │   └── Diagnostics/
│           │   │       └── SkirmishNavigationDebug.cs
│           │   └── Turrets/
│           │       ├── LaserTurret.cs
│           │       ├── ProjectileIntercept.cs
│           │       ├── ProjectileTurret.cs
│           │       ├── Turret.cs
│           │       ├── TurretHitResolver.cs
│           │       ├── TurretHitscan.cs
│           │       ├── TurretTargetSelector.cs
│           │       └── Projectiles/
│           │           ├── ProjectileTracer.cs
│           │           ├── TurretProjectile.cs
│           │           └── TurretProjectilePool.cs
│           ├── Diagnostics/
│           │   ├── DebugManager.cs
│           │   ├── Bots/
│           │   │   └── BotNavigationProbeDriver.cs
│           │   ├── Interface/
│           │   │   ├── CombatFlightTracks.cs
│           │   │   ├── DurabilityDebugLabels.cs
│           │   │   └── PerformanceMonitor.cs
│           │   └── Physics/
│           │       ├── AbnormalImpulseBodyProbe.cs
│           │       ├── AbnormalImpulseRecorder.cs
│           │       └── PhysicsTelemetryHud.cs
│           ├── Flight/
│           │   ├── FlightControlMath.cs
│           │   ├── FlightPropulsionControl.cs
│           │   ├── HoverFlightController.cs
│           │   ├── Hover/
│           │   │   ├── HoverControlSolver.cs
│           │   │   └── ThrusterAllocator.cs
│           │   ├── Presentation/
│           │   │   ├── ThrusterInfoPanel.cs
│           │   │   └── ThrusterVisualEffect.cs
│           │   └── Thrusters/
│           │       ├── HoverThruster.cs
│           │       ├── MainThruster.cs
│           │       ├── Thruster.cs
│           │       └── UniversalThruster.cs
│           ├── MainMenu/
│           │   ├── MainMenuController.cs
│           │   ├── Interface/
│           │   │   └── MainMenuView.cs
│           │   ├── Library/
│           │   │   ├── MenuBlueprintLibrary.cs
│           │   │   └── MenuBlueprintPreview.cs
│           │   ├── Navigation/
│           │   │   ├── MenuPlayDraft.cs
│           │   │   ├── PauseMenu.cs
│           │   │   └── WorkshopNavigation.cs
│           │   └── Settings/
│           │       └── MenuSettings.cs
│           ├── Navigation/
│           │   ├── NavigationAvoidanceResult.cs
│           │   ├── NavigationAvoidanceSettings.cs
│           │   ├── Diagnostics/
│           │   │   ├── EnemyNavigationSnapshot.cs
│           │   │   ├── Lab/
│           │   │   │   ├── EnemyAvoidanceCourse.cs
│           │   │   │   ├── EnemyAvoidanceLab.cs
│           │   │   │   ├── EnemyLabBlueprints.cs
│           │   │   │   ├── EnemyLabCamera.cs
│           │   │   │   ├── EnemyLabMovingObstacle.cs
│           │   │   │   ├── EnemyLabPanel.cs
│           │   │   │   └── EnemyLabRecorder.cs
│           │   │   └── Tracking/
│           │   │       ├── NavigationDebugText.cs
│           │   │       ├── NavigationFlightTrace.cs
│           │   │       └── NavigationTrack.cs
│           │   ├── Local/
│           │   │   ├── NavigationAvoidance.cs
│           │   │   └── NavigationObstacleQuery.cs
│           │   ├── Planning/
│           │   │   ├── NavigationLocalPath.cs
│           │   │   └── NavigationObstacleClimb.cs
│           │   ├── Prediction/
│           │   │   └── NavigationPrediction.cs
│           │   └── Vessels/
│           │       ├── VesselNavigation.cs
│           │       └── VesselNavigationSettings.cs
│           ├── Player/
│           │   ├── InputManager.cs
│           │   ├── Player.cs
│           │   ├── PlayerController.cs
│           │   └── PlayerHealth.cs
│           ├── Power/
│           │   ├── Power.cs
│           │   ├── PowerNetwork.cs
│           │   ├── Devices/
│           │   │   ├── PowerGeneratingUnit.cs
│           │   │   └── PowerTransmissionDevice.cs
│           │   ├── Network/
│           │   │   ├── PowerNetworkBaseline.cs
│           │   │   └── PowerPositionIndex.cs
│           │   └── Presentation/
│           │       ├── PowerConnectionVisuals.cs
│           │       └── PowerRangeUnionView.cs
│           ├── Presentation/
│           │   ├── VisualEffectsManager.cs
│           │   ├── Attract/
│           │   │   ├── BattleAttractCamera.cs
│           │   │   └── BattleAttractStage.cs
│           │   ├── Blocks/
│           │   │   ├── BlockStatusLight.cs
│           │   │   ├── CargoHoldView.cs
│           │   │   ├── CargoVisual.cs
│           │   │   ├── HoverControllerStatusLight.cs
│           │   │   ├── IndustrialEmissionBinding.cs
│           │   │   ├── IndustrialMotionDriver.cs
│           │   │   └── IndustrialPartMotion.cs
│           │   ├── Camera/
│           │   │   ├── CameraController.cs
│           │   │   ├── CameraFraming.cs
│           │   │   └── CameraShake.cs
│           │   ├── Effects/
│           │   │   ├── BeamFade.cs
│           │   │   ├── BlockVfxLibrary.cs
│           │   │   ├── DetachedPartSmokeTrail.cs
│           │   │   ├── LightFade.cs
│           │   │   ├── MeteorLightToken.cs
│           │   │   ├── MeteorTrailFollower.cs
│           │   │   ├── StylizedBeamEffect.cs
│           │   │   ├── TransientEffectPool.cs
│           │   │   ├── TransientEffects.cs
│           │   │   ├── VfxEffect.cs
│           │   │   └── VfxEffectPool.cs
│           │   └── Interface/
│           │       ├── GlobalTextStyler.cs
│           │       ├── IconManager.cs
│           │       ├── MainUIButtons.cs
│           │       ├── MainUIPanels.cs
│           │       ├── PanelTransitions.cs
│           │       ├── ReturnStatusPanel.cs
│           │       ├── StatusIcon.cs
│           │       └── UIInteractionFeedback.cs
│           ├── Salvage/
│           │   ├── SalvageSettings.cs
│           │   ├── Cargo/
│           │   │   ├── CargoHold.cs
│           │   │   ├── CargoItem.cs
│           │   │   └── CargoPersistence.cs
│           │   ├── Drops/
│           │   │   ├── LootDrop.cs
│           │   │   └── WreckSalvage.cs
│           │   └── Economy/
│           │       └── CoinManager.cs
│           ├── Siege/
│           │   ├── SiegeMatch.cs
│           │   ├── SiegeProgress.cs
│           │   ├── SiegeSession.cs
│           │   ├── SiegeSettings.cs
│           │   ├── Battle/
│           │   │   ├── EnemyWreckRetirement.cs
│           │   │   ├── SiegeBase.cs
│           │   │   ├── SiegeCleanupSweep.cs
│           │   │   ├── SiegeStrandedCockpit.cs
│           │   │   ├── SiegeWaveDirector.cs
│           │   │   └── SiegeWreckBudget.cs
│           │   ├── Fleet/
│           │   │   ├── SiegeBlueprintCatalog.cs
│           │   │   ├── SiegeDeploymentPad.cs
│           │   │   ├── SiegeFleet.cs
│           │   │   ├── SiegeFleetEntry.cs
│           │   │   ├── SiegeFleetPhysics.cs
│           │   │   ├── SiegeFlightSettings.cs
│           │   │   ├── SiegeRefit.cs
│           │   │   └── SiegeReturnFlight.cs
│           │   ├── Gameplay/
│           │   │   ├── SiegeBaseWorkshop.cs
│           │   │   ├── SiegeConstructionSettings.cs
│           │   │   ├── SiegeGameplayStage.cs
│           │   │   ├── SiegeIsland.cs
│           │   │   ├── SiegeIslandSettings.cs
│           │   │   ├── SiegePhaseClock.cs
│           │   │   └── Interface/
│           │   │       └── SiegeCombatHud.cs
│           │   ├── Hangars/
│           │   │   ├── ChangeHangarEntranceAction.cs
│           │   │   ├── HangarEntrance.cs
│           │   │   ├── HangarEntranceButton.cs
│           │   │   ├── HangarFacePanel.cs
│           │   │   ├── HangarLimits.cs
│           │   │   ├── HangarModule.cs
│           │   │   ├── HangarResizeButton.cs
│           │   │   ├── HangarRules.cs
│           │   │   ├── HangarShellBlueprint.cs
│           │   │   └── ResizeHangarAction.cs
│           │   ├── Interface/
│           │   │   ├── ISiegeDeploymentHost.cs
│           │   │   ├── SiegeBattleFeedback.cs
│           │   │   ├── SiegeDeploymentView.cs
│           │   │   ├── SiegeFleetView.cs
│           │   │   ├── SiegeHud.cs
│           │   │   └── SiegeObserverCamera.cs
│           │   ├── Validation/
│           │   │   ├── SiegeDrivingPlayerChecks.cs
│           │   │   ├── SiegeEngineCounters.cs
│           │   │   ├── SiegeGameplayPlayerChecks.cs
│           │   │   ├── SiegePlayerChecks.cs
│           │   │   ├── SiegePlayerSurvey.cs
│           │   │   └── SiegeStressPopulation.cs
│           │   └── Workshop/
│           │       ├── SiegeWorkshop.cs
│           │       └── SiegeWorkshopStage.cs
│           ├── Simulation/
│           │   ├── Block.cs
│           │   ├── PlayManager.cs
│           │   ├── Connections/
│           │   │   ├── BatchConnectionBuilder.cs
│           │   │   ├── BlockConnectionGraph.cs
│           │   │   ├── BlockConnections.cs
│           │   │   └── BlockConnectionSnapshot.cs
│           │   ├── Diagnostics/
│           │   │   └── SimulationWorkMetrics.cs
│           │   ├── Geometry/
│           │   │   └── BlockBounds.cs
│           │   ├── Groups/
│           │   │   ├── BlockGroupManager.cs
│           │   │   ├── GroupPhysicsSettings.cs
│           │   │   ├── RuntimeGroupBuilder.cs
│           │   │   └── RuntimeGroupCleanup.cs
│           │   ├── Modes/
│           │   │   ├── GameModeController.cs
│           │   │   ├── PlayDraft.cs
│           │   │   └── Interface/
│           │   │       └── GameModePanel.cs
│           │   ├── Physics/
│           │   │   ├── FragmentLifetime.cs
│           │   │   ├── ImpactPhysics.cs
│           │   │   └── RigidbodyMotion.cs
│           │   ├── Presentation/
│           │   │   └── PlayBlockSelection.cs
│           │   └── Units/
│           │       ├── Cockpit.cs
│           │       ├── ControlUnit.cs
│           │       ├── ModularUnitFactory.cs
│           │       ├── ModularUnitValidator.cs
│           │       ├── Rack.cs
│           │       ├── RuntimeUnitMember.cs
│           │       └── UnitFaction.cs
│           └── World/
│               ├── WorldGenerationSettings.cs
│               ├── WorldSessionManager.cs
│               ├── Buildings/
│               │   ├── BuildingRuntime.cs
│               │   ├── WorldBuildingManager.cs
│               │   ├── Data/
│               │   │   ├── WorldBuildingCatalog.cs
│               │   │   ├── WorldBuildingLibrary.cs
│               │   │   └── WorldBuildingState.cs
│               │   ├── Placement/
│               │   │   ├── WorldBuildingFootprints.cs
│               │   │   ├── WorldBuildingPalette.cs
│               │   │   ├── WorldBuildingPlacement.cs
│               │   │   └── WorldBuildingValidation.cs
│               │   ├── Presentation/
│               │   │   └── WorldBuildingRenderCache.cs
│               │   └── Topology/
│               │       └── BuildingRefit.cs
│               ├── Outposts/
│               │   ├── EnemyOutpostData.cs
│               │   ├── OutpostEncounterDirector.cs
│               │   └── OutpostHud.cs
│               ├── Persistence/
│               │   ├── VoxelWorldEdits.cs
│               │   └── WorldLabPersistence.cs
│               ├── Presentation/
│               │   ├── WorldDistanceView.cs
│               │   ├── WorldLoadingPanel.cs
│               │   └── WorldTerrainTransitionMesh.cs
│               ├── Streaming/
│               │   ├── WorldBuildingStreaming.cs
│               │   ├── WorldPlayerStreamingGuard.cs
│               │   └── WorldRegionStreaming.cs
│               ├── Terrain/
│               │   ├── FloatingIslandTerrain.cs
│               │   ├── ProceduralVoxelWorld.cs
│               │   ├── StreamingWorldTerrain.cs
│               │   ├── VoxelWorldChunk.cs
│               │   ├── VoxelWorldData.cs
│               │   ├── Destruction/
│               │   │   ├── VoxelDebrisPiece.cs
│               │   │   ├── VoxelStructureCollapse.cs
│               │   │   └── VoxelStructureSupport.cs
│               │   └── Meshing/
│               │       ├── VoxelMeshData.cs
│               │       └── VoxelSurfaceMesher.cs
│               ├── Validation/
│               │   ├── EnemyFlightPlayerChecks.cs
│               │   ├── OutpostEditorPlayerChecks.cs
│               │   ├── OutpostPlayerChecks.cs
│               │   ├── OutpostPursuitPlayerChecks.cs
│               │   ├── OutpostSalvagePlayerChecks.cs
│               │   ├── WorldCombatPlayerChecks.cs
│               │   ├── WorldPlayerSmokeChecks.cs
│               │   ├── WorldReturnPlayerChecks.cs
│               │   ├── WorldSessionValidationHost.cs
│               │   └── WorldLab/
│               │       ├── WorldLabDebrisProbe.cs
│               │       └── WorldLabPhysicsProbe.cs
│               └── WorldLab/
│                   └── WorldLabController.cs
├── Blueprints/
│   ├── CODEX/
│   │   ├── Generate.cs
│   │   ├── RuntimeProbe.cs
│   │   └── Validate.cs
│   ├── CODEX_Enemies/
│   │   ├── FlightProbe.cs
│   │   ├── Generate.cs
│   │   ├── Summarize.ps1
│   │   └── Validate.cs
│   └── DSH_Enemies/
│       ├── ArmamentProbe.cs
│       ├── design_model.py
│       ├── FlightProbe.cs
│       ├── Generate.cs
│       ├── power_check.py
│       ├── summarize.py
│       ├── Validate.cs
│       └── write_generator.py
└── Tools/
    └── CodeReadability/
        ├── check_tool_sources.py
        ├── generate_flow_browser.py
        ├── generate_navigation.py
        └── Program.cs
```

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

## Assets/Editor/Architecture/DetachedVehicleChecks.cs

// 职责：以真实玩家帧区分载具断片的即时销毁、延时清理和跨场景/保护状态丢失。 // 入口：Run()；要求已在Play Mode，报告Temp/Reuse/detached-vehicle.json，不操作Editor播放状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DetachedVehicleChecks | [`string Run()`](../Assets/Editor/Architecture/DetachedVehicleChecks.cs#L19) | public static |
| DetachedVehicleChecks | [`void Tick()`](../Assets/Editor/Architecture/DetachedVehicleChecks.cs#L35) | private static |
| DetachedVehicleChecks | [`IEnumerator Scenario()`](../Assets/Editor/Architecture/DetachedVehicleChecks.cs#L80) | private static |
| DetachedVehicleChecks | [`void Check(string label, bool result)`](../Assets/Editor/Architecture/DetachedVehicleChecks.cs#L258) | private static |

## Assets/Editor/Architecture/EditableComponentBaker.cs

// 职责：把运行对象组件组成烘焙为真实 Prefab，并迁移场景 Managers、模块依赖及 UI。 // 入口：Bake()；只补缺失组件/引用，原资产先备份，重复执行不覆盖用户已配置模板。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EditableComponentBaker | [`string Bake()`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L21) | public static |
| EditableComponentBaker | [`void Backup(string path)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L113) | private static |
| EditableComponentBaker | [`T Ensure(GameObject owner)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L128) | private static |
| EditableComponentBaker | [`void SetReference(Object owner, string field, Object value)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L141) | private static |
| EditableComponentBaker | [`T Template(string name, Action<GameObject> compose)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L153) | private static |
| EditableComponentBaker | [`void ConfigurePool(VfxEffectPool pool)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L183) | private static |
| EditableComponentBaker | [`void BakeTemplates()`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L197) | private static |
| EditableComponentBaker | [`void MigrateTree(GameObject root)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L316) | private static |
| EditableComponentBaker | [`T Service(Scene scene, Transform managers)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L459) | private static |
| EditableComponentBaker | [`void ConfigureServices(Scene scene, Transform managers)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L473) | private static |
| EditableComponentBuildStripper | [`void OnProcessScene(Scene scene, BuildReport report)`](../Assets/Editor/Architecture/EditableComponentBaker.cs#L513) | public |

## Assets/Editor/Architecture/EditableComponentChecks.cs

// 职责：验证显式组件迁移的资产完整性、场景入口及真实 Play 生命周期。 // 入口：AuditAssets() / RunPlay()；不写玩家存档，隔离夹具结束后恢复活动场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EditableComponentChecks | [`string RunWorkshop()`](../Assets/Editor/Architecture/EditableComponentChecks.cs#L19) | public static |
| EditableComponentChecks | [`string RunWorldPlay()`](../Assets/Editor/Architecture/EditableComponentChecks.cs#L41) | public static |
| EditableComponentChecks | [`string VerifyWorldMotion()`](../Assets/Editor/Architecture/EditableComponentChecks.cs#L93) | public static |
| EditableComponentChecks | [`string AuditAssets()`](../Assets/Editor/Architecture/EditableComponentChecks.cs#L101) | public static |
| EditableComponentChecks | [`void CheckTree(GameObject root, string path)`](../Assets/Editor/Architecture/EditableComponentChecks.cs#L179) | private static |
| EditableComponentChecks | [`string RunPlay()`](../Assets/Editor/Architecture/EditableComponentChecks.cs#L213) | public static |
| EditableComponentChecks | [`string WriteReport(string name, List<string> checks)`](../Assets/Editor/Architecture/EditableComponentChecks.cs#L314) | private static |
| EditableComponentChecks | [`void Require(bool condition, string message)`](../Assets/Editor/Architecture/EditableComponentChecks.cs#L322) | private static |

## Assets/Editor/Architecture/FragmentRetentionChecks.cs

// 职责：以真实 Play 帧回归建筑断片、旧清理计时器和驾驶舱恢复的最短保留期。 // 入口：Main 编辑态玩法下调用 Run()；报告 Temp/Reuse/fragment-retention.json。 // 隔离：独立 PhysicsScene，设置仅内存暂存；finally 恢复并卸载，不替代生产延迟清理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| FragmentRetentionChecks | [`string Run()`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L24) | public static |
| FragmentRetentionChecks | [`void Tick()`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L46) | private static |
| FragmentRetentionChecks | [`void Finish()`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L85) | private static |
| FragmentRetentionChecks | [`IEnumerator RunRoutine()`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L107) | private static |
| FragmentRetentionChecks | [`GameObject CreateDebris(WorldBuildingManager host, int count, float height)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L241) | private static |
| FragmentRetentionChecks | [`ControlUnit NewUnit(string name)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L272) | private static |
| FragmentRetentionChecks | [`Block NewBlock(string name, Transform parent)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L280) | private static |
| FragmentRetentionChecks | [`GameObject NewObject(string name, Transform parent = null)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L291) | private static |
| FragmentRetentionChecks | [`void Mark(GameObject root)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L299) | private static |
| FragmentRetentionChecks | [`Type RuntimeType(string name)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L304) | private static |
| FragmentRetentionChecks | [`void Set(object target, string field, object value)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L305) | private static |
| FragmentRetentionChecks | [`void Call(object target, string method, params object[] arguments)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L310) | private static |
| FragmentRetentionChecks | [`void Require(bool condition, string message)`](../Assets/Editor/Architecture/FragmentRetentionChecks.cs#L315) | private static |

## Assets/Editor/Architecture/HoverControlPlayChecks.cs

// 职责：以真实运行组件验证悬浮控制的有效归属、停供和禁用清理契约。 // 入口：Run()；由 ArchitecturePlayProbe 在暂停场景业务的 Play 会话中调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverControlPlayChecks | [`string[] Run()`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L11) | internal static |
| HoverControlPlayChecks | [`void Tick(HoverFlightController controller)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L172) | private static |
| HoverControlPlayChecks | [`void TickThruster(HoverThruster thruster)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L177) | private static |
| HoverControlPlayChecks | [`void CheckRampDown(List<string> checks, HoverThruster thruster, string reason)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L182) | private static |
| HoverControlPlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Architecture/HoverControlPlayChecks.cs#L205) | private static |

## Assets/Editor/Architecture/PresentationPlayChecks.cs

// 职责：在真实 Play 会话中验证镜头/面板协程取消与临时 VFX 的销毁生命周期。 // 入口：Run(checks)；外层探针逐步 MoveNext，Current 是以秒为单位的等待时长。 // 生命周期：仅创建隔离对象；恢复单例与 Random 状态，不控制 Editor 或改动场景资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PresentationPlayChecks | [`IEnumerator Run(List<string> checks)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L16) | internal static |
| PresentationPlayChecks | [`GameObject Own(List<GameObject> owned, string name, Transform parent = null)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L285) | private static |
| PresentationPlayChecks | [`Type RuntimeType(string name)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L297) | private static |
| PresentationPlayChecks | [`object CreateRuntimeObject(string name, MonoBehaviour runner)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L302) | private static |
| PresentationPlayChecks | [`object GetField(object target, string name)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L307) | private static |
| PresentationPlayChecks | [`void SetField(object target, string name, object value)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L312) | private static |
| PresentationPlayChecks | [`void Invoke(object target, string name, params object[] arguments)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L317) | private static |
| PresentationPlayChecks | [`void InvokeStatic(Type type, string name, params object[] arguments)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L322) | private static |
| PresentationPlayChecks | [`bool Near(Vector3 actual, Vector3 expected)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L327) | private static |
| PresentationPlayChecks | [`void Check(List<string> checks, List<string> failures, string name, bool passed)`](../Assets/Editor/Architecture/PresentationPlayChecks.cs#L332) | private static |

## Assets/Editor/Architecture/SharedMechanismChecks.cs

// 职责：在隔离场景回归共用包围盒、编辑动作恢复和建筑残骸运动继承。 // 入口：Main Play调用Run()，Editor update推进真实帧；报告Temp/Reuse/shared-mechanism-checks.json。 // 隔离：仅同步动作期间替换管理器实例，文件仓定向Temp；finally恢复静态状态并卸载测试场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SharedMechanismChecks | [`string Run()`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L28) | public static |
| SharedMechanismChecks | [`void Tick()`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L55) | private static |
| SharedMechanismChecks | [`void Finish()`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L84) | private static |
| SharedMechanismChecks | [`IEnumerator RunRoutine()`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L107) | private static |
| SharedMechanismChecks | [`void BoundsChecks()`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L144) | private static |
| SharedMechanismChecks | [`void MotionChecks()`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L247) | private static |
| SharedMechanismChecks | [`IEnumerator ActionChecks()`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L317) | private static |
| SharedMechanismChecks | [`void CheckRestored( string label, Block block, string id, float mass, Vector3 position, Quaternion rotation, int coins)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L399) | private static |
| SharedMechanismChecks | [`void WithManagers(Action operation)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L419) | private static |
| SharedMechanismChecks | [`GameObject NewObject(string name)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L445) | private static |
| SharedMechanismChecks | [`Type RuntimeType(string name)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L452) | private static |
| SharedMechanismChecks | [`object Call(Type type, object target, string name, params object[] arguments)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L457) | private static |
| SharedMechanismChecks | [`void Set(object target, string name, object value)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L462) | private static |
| SharedMechanismChecks | [`bool Near(Vector3 actual, Vector3 expected)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L467) | private static |
| SharedMechanismChecks | [`bool Close(Bounds actual, Bounds expected)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L472) | private static |
| SharedMechanismChecks | [`void Check(string name, bool passed)`](../Assets/Editor/Architecture/SharedMechanismChecks.cs#L477) | private static |

## Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs

// 职责：程序化生成工业方块模型、材质及组合资产。 // 入口：RebuildAll() / RebuildTurret()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IndustrialArtGenerator | [`void RebuildAll()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L48) | public static |
| IndustrialArtGenerator | [`void RebuildTurret()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L82) | public static |
| IndustrialArtGenerator | [`string[] GetBlockPrefabPaths()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L102) | private static |
| IndustrialArtGenerator | [`void EnsureFolders()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L107) | private static |
| IndustrialArtGenerator | [`void EnsureFolder(string parent, string child)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L116) | private static |
| IndustrialArtGenerator | [`void CreateSharedMeshes()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L125) | private static |
| IndustrialArtGenerator | [`Mesh SaveOrUpdateMesh(string path, Mesh generated)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L136) | private static |
| IndustrialArtGenerator | [`Mesh ClonePrimitiveMesh(PrimitiveType type)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L152) | private static |
| IndustrialArtGenerator | [`Mesh CreateRoundedCube(int segments, float radius)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L160) | private static |
| IndustrialArtGenerator | [`Vector3 FacePoint(int face, float u, float v)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L220) | private static |
| IndustrialArtGenerator | [`void AddFacingQuad( IReadOnlyList<Vector3> vertices, IReadOnlyList<Vector3> normals, ICollection<int> triangles, int a, int b, int c, int d)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L233) | private static |
| IndustrialArtGenerator | [`Mesh CreateTorus(int ringSegments, int tubeSegments, float radius, float tubeRadius)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L264) | private static |
| IndustrialArtGenerator | [`Mesh CreateCone(int segments)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L318) | private static |
| IndustrialArtGenerator | [`Mesh CreateWedge()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L353) | private static |
| IndustrialArtGenerator | [`void CreateSharedMaterials()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L416) | private static |
| IndustrialArtGenerator | [`Material CreateOrUpdateMaterial( string name, Color baseColor, float metallic, float smoothness, Color emission, bool transparent = false)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L456) | private static |
| IndustrialArtGenerator | [`void ConfigureRenderStyle()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L525) | private static |
| IndustrialArtGenerator | [`void ConfigureVolume(string path)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L537) | private static |
| IndustrialArtGenerator | [`T GetOrAdd(VolumeProfile profile)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L571) | private static |
| IndustrialArtGenerator | [`void Set(VolumeParameter<T> parameter, T value)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L577) | private static |
| IndustrialArtGenerator | [`void RebuildConnectorPrefab()`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L583) | private static |
| IndustrialArtGenerator | [`bool RebuildPrefab(string prefabPath)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L667) | private static |
| IndustrialArtGenerator | [`void RemovePlaceholderRenderers(Transform model)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L704) | private static |
| IndustrialArtGenerator | [`void BuildBlockVisual(string name, Block block, Transform model)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L723) | private static |
| IndustrialArtGenerator | [`bool ShouldUseLod(string name, Block block)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L772) | private static |
| IndustrialArtGenerator | [`void ApplyCategoryPalette(string name, Transform visualRoot)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L777) | private static |
| IndustrialArtGenerator | [`List<Renderer> BuildDetailedModel( string name, Block block, Transform parent, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L797) | private static |
| IndustrialArtGenerator | [`List<Renderer> BuildSimpleModel(string name, Block block, Transform parent)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L856) | private static |
| IndustrialArtGenerator | [`void BuildStructural( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L873) | private static |
| IndustrialArtGenerator | [`void BuildDoor( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L892) | private static |
| IndustrialArtGenerator | [`void BuildStairs(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L949) | private static |
| IndustrialArtGenerator | [`void BuildRack(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L992) | private static |
| IndustrialArtGenerator | [`void BuildCockpit( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1042) | private static |
| IndustrialArtGenerator | [`void BuildGenerator( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1145) | private static |
| IndustrialArtGenerator | [`void BuildRelay( Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1251) | private static |
| IndustrialArtGenerator | [`void BuildFlightController( Transform parent, ICollection<Renderer> renderers, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1314) | private static |
| IndustrialArtGenerator | [`void BuildThruster( Transform parent, Block block, string name, ICollection<Renderer> renderers, ICollection<Transform> spinTargets, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1362) | private static |
| IndustrialArtGenerator | [`void BuildTurret(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1456) | private static |
| IndustrialArtGenerator | [`void ConfigureTurretRig(Transform model, Transform detailedRoot)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1596) | private static |
| IndustrialArtGenerator | [`void BuildRepairBay(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1614) | private static |
| IndustrialArtGenerator | [`void RebuildBot(GameObject root)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1675) | private static |
| IndustrialArtGenerator | [`Transform CreateGroup(Transform parent, string name)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1754) | private static |
| IndustrialArtGenerator | [`MeshRenderer AddPart( Transform parent, string name, Mesh mesh, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material, bool castShadows = true)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1762) | private static |
| IndustrialArtGenerator | [`void SetLayerRecursively(GameObject root, int layer)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1788) | private static |
| IndustrialArtGenerator | [`void CreatePreviewScene(IReadOnlyList<string> prefabPaths)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1797) | private static |
| IndustrialArtGenerator | [`void AddPartToScene( string name, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation, Material material)`](../Assets/Editor/Art/Authoring/IndustrialArtGenerator.cs#L1927) | private static |

## Assets/Editor/Art/Authoring/SpaceKitCurator.cs

// 职责：筛选科幻素材并创建可编辑备用 Prefab、材质及分类目录。 // 入口：CreateLibrary() / RepairLegacyImportArtifacts() / ValidateLibrary()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SpaceKitCurator | [`bool IsSource(string path)`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L77) | private static |
| SpaceKitCurator | [`void EnsureFolder(string path)`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L78) | private static |
| SpaceKitCurator | [`string Destination(string path, Dictionary<string, string> selected)`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L89) | private static |
| SpaceKitCurator | [`string Hash(string path)`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L104) | private static |
| SpaceKitCurator | [`void CreateLibrary()`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L115) | public static |
| SpaceKitCurator | [`void Remap(Object target, Dictionary<Object, Object> objects)`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L254) | private static |
| SpaceKitCurator | [`void CleanLegacyArtifacts(GameObject instance)`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L281) | private static |
| SpaceKitCurator | [`void RepairLegacyImportArtifacts()`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L295) | public static |
| SpaceKitCurator | [`void ConvertMaterial(Material material)`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L315) | private static |
| SpaceKitCurator | [`void ValidateLibrary()`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L410) | public static |
| SpaceKitCurator | [`void RenderAllPreviews()`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L546) | public static |
| SpaceKitCurator | [`void RenderPreviews(int start, int count)`](../Assets/Editor/Art/Authoring/SpaceKitCurator.cs#L548) | public static |

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
| BlockArtIntegrator | [`void ConfigureBay(GameObject root, Transform model)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L502) | private static |
| BlockArtIntegrator | [`void StatusLights(GameObject root, Transform model, bool generator)`](../Assets/Editor/Art/BlockArtIntegrator.cs#L524) | private static |

## Assets/Editor/Art/Preview/BlockArtPreview.cs

// 职责：生成方块美术预览场景或图像供检查。 // 入口：Render() / RenderBlocks()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtPreview | [`void Render( string path, string output, bool effects = false, bool front = false, Vector3? view = null, Action<GameObject> configure = null, bool transparent = false)`](../Assets/Editor/Art/Preview/BlockArtPreview.cs#L16) | public static |
| BlockArtPreview | [`void RenderBlocks(string folder, bool effects = false)`](../Assets/Editor/Art/Preview/BlockArtPreview.cs#L100) | public static |

## Assets/Editor/Art/Validation/BlockArtDependencies.cs

// 职责：收集和处理方块美术集成所需的资源依赖。 // 入口：Resolve() / Unpack() / EnsureFolder()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtDependencies | [`bool IsVendor(string path)`](../Assets/Editor/Art/Validation/BlockArtDependencies.cs#L32) | private static |
| BlockArtDependencies | [`void Resolve()`](../Assets/Editor/Art/Validation/BlockArtDependencies.cs#L33) | public static |
| BlockArtDependencies | [`string Hash(string path)`](../Assets/Editor/Art/Validation/BlockArtDependencies.cs#L155) | private static |
| BlockArtDependencies | [`void Unpack(GameObject root)`](../Assets/Editor/Art/Validation/BlockArtDependencies.cs#L166) | public static |
| BlockArtDependencies | [`void EnsureFolder(string path)`](../Assets/Editor/Art/Validation/BlockArtDependencies.cs#L177) | public static |
| BlockArtDependencies | [`void Remap(Object target, Dictionary<Object, Object> map)`](../Assets/Editor/Art/Validation/BlockArtDependencies.cs#L189) | private static |

## Assets/Editor/Art/Validation/BlockArtPlayProbe.cs

// 职责：在 Play Mode 检查方块美术、挂点和运行表现，输出验证结果。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtPlayProbe | [`void Run()`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L54) | public static |
| BlockArtPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L68) | private static |
| BlockArtPlayProbe | [`void Setup()`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L107) | private static |
| BlockArtPlayProbe | [`ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool kinematic)`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L184) | private static |
| BlockArtPlayProbe | [`GameObject Spawn(string name, Transform parent, Vector3 local)`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L203) | private static |
| BlockArtPlayProbe | [`void Supply(Transform parent)`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L210) | private static |
| BlockArtPlayProbe | [`void Tick()`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L217) | private static |
| BlockArtPlayProbe | [`void Check(string name, bool pass)`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L430) | private static |
| BlockArtPlayProbe | [`void Capture(string message, string stack, LogType type)`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L442) | private static |
| BlockArtPlayProbe | [`void Finish()`](../Assets/Editor/Art/Validation/BlockArtPlayProbe.cs#L450) | private static |

## Assets/Editor/Art/Validation/BlockArtValidation.cs

// 职责：检查美术集成后的 Prefab 结构、引用与配置。 // 入口：Validate() / ValidateAssets()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockArtValidation | [`void Validate()`](../Assets/Editor/Art/Validation/BlockArtValidation.cs#L14) | public static |
| BlockArtValidation | [`void ValidateAssets(string[] roots)`](../Assets/Editor/Art/Validation/BlockArtValidation.cs#L16) | public static |

## Assets/Editor/Bots/BotNavigationPlayProbe.cs

// 职责：隔离 Play 场景验证 Bot 过滤、墙面、U 形围挡、惯性制动和移动 Home 导航。 // 入口：Run()；结果保存在 Temp/BotNavigation/PlayValidation.json；不修改作者场景或资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationPlayProbe | [`void Run()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L31) | public static |
| BotNavigationPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L50) | private static |
| BotNavigationPlayProbe | [`void Capture(string condition, string stack, LogType type)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L101) | private static |
| BotNavigationPlayProbe | [`void Tick()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L109) | private static |
| BotNavigationPlayProbe | [`void TickCase()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L122) | private static |
| BotNavigationPlayProbe | [`void ValidateBotFiltering()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L191) | private static |
| BotNavigationPlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L317) | private static |
| BotNavigationPlayProbe | [`void ValidateFrozenNavigation()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L329) | private static |
| BotNavigationPlayProbe | [`void StartCase()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L355) | private static |
| BotNavigationPlayProbe | [`void Wall(Vector3 position, Vector3 scale)`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L419) | private static |
| BotNavigationPlayProbe | [`void Finish()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L428) | private static |
| BotNavigationPlayProbe | [`void Restore()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L435) | private static |
| BotNavigationPlayProbe | [`void WriteReport()`](../Assets/Editor/Bots/BotNavigationPlayProbe.cs#L444) | private static |

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

## Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs

// 职责：初始化 CSV 与 Prefab 绑定，CSV 导入后自动备份/同步真实资产；校验失败不写入。 // 入口：CreateCsv() / Bake() / QueueBake()；只处理 Resources/Blocks，不保存当前场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvAuthoring | [`void OpenCsv()`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L34) | public static |
| BlockCsvAuthoring | [`void CreateCsv()`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L45) | public static |
| BlockCsvAuthoring | [`void Bake()`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L128) | public static |
| BlockCsvAuthoring | [`void QueueBake()`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L239) | public static |
| BlockCsvAuthoring | [`void ValidatePowerDependencies(GameObject root)`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L246) | public static |
| BlockCsvAuthoring | [`void RequestValidation(BlockCsvBinding binding)`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L279) | private static |
| BlockCsvAuthoring | [`void Update()`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L287) | private static |
| BlockCsvAuthoring | [`void OnPlayModeChanged(PlayModeStateChange state)`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L321) | private static |
| BlockCsvAuthoring | [`string ReadValue(SerializedProperty property)`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L329) | private static |
| BlockCsvAuthoring | [`string Snapshot(GameObject root)`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L348) | private static |
| BlockCsvAuthoring | [`string Hash(byte[] bytes)`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L355) | private static |
| BlockCsvAuthoring | [`void SaveBackup(string backup, Dictionary<string, byte[]> originals, Dictionary<string, string> hashes, string csv)`](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L361) | private static |

## Assets/Editor/Building/BlockDataTables/BlockCsvChecks.cs

// 职责：隔离验证实际 CSV→Prefab 同步、幂等/错误拒绝及真实 Awake/OnEnable 顺序。 // 入口：RunEditMode() / RunPlayMode()；Edit Mode 最终恢复 CSV 与 Prefab 原始字节，不保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvChecks | [`string RunEditMode()`](../Assets/Editor/Building/BlockDataTables/BlockCsvChecks.cs#L21) | public static |
| BlockCsvChecks | [`string RunPlayMode()`](../Assets/Editor/Building/BlockDataTables/BlockCsvChecks.cs#L205) | public static |
| BlockCsvChecks | [`GameObject Load(string name)`](../Assets/Editor/Building/BlockDataTables/BlockCsvChecks.cs#L255) | private static |
| BlockCsvChecks | [`void Set(List<List<string>> records, string name, string field, string value)`](../Assets/Editor/Building/BlockDataTables/BlockCsvChecks.cs#L260) | private static |
| BlockCsvChecks | [`void Write(List<List<string>> records)`](../Assets/Editor/Building/BlockDataTables/BlockCsvChecks.cs#L266) | private static |
| BlockCsvChecks | [`void Check(Report report, bool condition, string description)`](../Assets/Editor/Building/BlockDataTables/BlockCsvChecks.cs#L272) | private static |
| BlockCsvChecks | [`string SaveReport(Report report, string name)`](../Assets/Editor/Building/BlockDataTables/BlockCsvChecks.cs#L282) | private static |

## Assets/Editor/Building/BlockDataTables/BlockCsvPostprocessor.cs

// 职责：CSV 文件被 Unity 导入后请求延迟同步，避免在 AssetPostprocessor 内递归写资产。 // 入口：OnPostprocessAllAssets()；Prefab 自身重导入不会触发 CSV 同步循环。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvPostprocessor | [`void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)`](../Assets/Editor/Building/BlockDataTables/BlockCsvPostprocessor.cs#L8) | private static |

## Assets/Editor/Building/BlockDataTables/BlockCsvRegressionChecks.cs

// 职责：用真实维修仓跨物理帧验证 CSV、供电、离仓、维修和断电恢复，隔离创建并清理测试对象。 // 入口：Begin()（Play Mode）；结果写入 Temp/BlockCsvChecks/regression-play.json，不修改制作资产或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockCsvRegressionChecks | [`string Begin()`](../Assets/Editor/Building/BlockDataTables/BlockCsvRegressionChecks.cs#L31) | public static |
| BlockCsvRegressionChecks | [`void Tick()`](../Assets/Editor/Building/BlockDataTables/BlockCsvRegressionChecks.cs#L76) | private static |
| BlockCsvRegressionChecks | [`void CheckRuntimeState()`](../Assets/Editor/Building/BlockDataTables/BlockCsvRegressionChecks.cs#L143) | private static |
| BlockCsvRegressionChecks | [`void Check(bool condition, string description)`](../Assets/Editor/Building/BlockDataTables/BlockCsvRegressionChecks.cs#L169) | private static |
| BlockCsvRegressionChecks | [`void Finish()`](../Assets/Editor/Building/BlockDataTables/BlockCsvRegressionChecks.cs#L179) | private static |
| BlockCsvRegressionChecks | [`void WriteReport()`](../Assets/Editor/Building/BlockDataTables/BlockCsvRegressionChecks.cs#L197) | private static |

## Assets/Editor/Building/BuildingEditorAssets.cs

// 职责：保存建筑底座、网格材质及尺寸编辑 uGUI 为真实可编辑资产，不写入 Main 场景。 // 入口：Bake() 菜单；已存在的 Prefab/材质优先保留，删除指定资产后可重新生成。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingEditorAssets | [`void Bake()`](../Assets/Editor/Building/BuildingEditorAssets.cs#L14) | public static |
| BuildingEditorAssets | [`void MigrateFreeBuildingPanel()`](../Assets/Editor/Building/BuildingEditorAssets.cs#L38) | public static |
| BuildingEditorAssets | [`void EnsureCompleteButton(GameObject root)`](../Assets/Editor/Building/BuildingEditorAssets.cs#L79) | private static |
| BuildingEditorAssets | [`Material CreateGridMaterial()`](../Assets/Editor/Building/BuildingEditorAssets.cs#L101) | private static |
| BuildingEditorAssets | [`void CreateFoundation(Material material)`](../Assets/Editor/Building/BuildingEditorAssets.cs#L142) | private static |
| BuildingEditorAssets | [`void CreatePanel()`](../Assets/Editor/Building/BuildingEditorAssets.cs#L173) | private static |
| BuildingEditorAssets | [`GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position)`](../Assets/Editor/Building/BuildingEditorAssets.cs#L211) | private static |
| BuildingEditorAssets | [`Text Label( string name, Transform parent, string value, Vector2 size, Vector2 position, int fontSize)`](../Assets/Editor/Building/BuildingEditorAssets.cs#L223) | private static |

## Assets/Editor/Building/Interface/BuildHotbarAuthoring.cs

// 职责：将可编辑快捷栏、目录图标和默认槽位保存为真实配置资产及Main场景层级。 // 入口：Install()；只创建自己拥有的QuickBuild层级，保留已有目录和个人快捷栏配置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildHotbarAuthoring | [`void Install()`](../Assets/Editor/Building/Interface/BuildHotbarAuthoring.cs#L14) | public static |
| BuildHotbarAuthoring | [`RectTransform Child(Transform parent, string name)`](../Assets/Editor/Building/Interface/BuildHotbarAuthoring.cs#L180) | private static |
| BuildHotbarAuthoring | [`T Component(Component owner)`](../Assets/Editor/Building/Interface/BuildHotbarAuthoring.cs#L193) | private static |
| BuildHotbarAuthoring | [`Text Text(Transform parent, string name, Font font, int size)`](../Assets/Editor/Building/Interface/BuildHotbarAuthoring.cs#L199) | private static |
| BuildHotbarAuthoring | [`void Place(RectTransform target, Vector2 position, Vector2 size)`](../Assets/Editor/Building/Interface/BuildHotbarAuthoring.cs#L209) | private static |

## Assets/Editor/Building/Interface/BuildInteractionSetup.cs

// 职责：配置主场景建造交互、范围与连接点提示。 // 入口：Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildInteractionSetup | [`void Apply()`](../Assets/Editor/Building/Interface/BuildInteractionSetup.cs#L15) | public static |

## Assets/Editor/Building/Interface/BuildPaletteBaker.cs

// 职责：创建并保存建造分类目录、条目和图标引用。 // 入口：Bake() / RefreshHoveredBlockInfo() / ConfigureHintMaterial()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPaletteBaker | [`void Bake()`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L27) | public static |
| BuildPaletteBaker | [`void RefreshHoveredBlockInfo()`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L226) | public static |
| BuildPaletteBaker | [`void ConfigureThumbnail(GameObject root)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L274) | private static |
| BuildPaletteBaker | [`int Category(string name)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L291) | private static |
| BuildPaletteBaker | [`RectTransform Child(Transform parent, string name)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L316) | private static |
| BuildPaletteBaker | [`void Rect(RectTransform t, Vector2 min, Vector2 max, Vector2 low, Vector2 high)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L329) | private static |
| BuildPaletteBaker | [`Text Label(Transform parent, string name, Font font, int size)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L337) | private static |
| BuildPaletteBaker | [`void Set(Object target, string field, Object value)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L350) | private static |
| BuildPaletteBaker | [`void ImportSprite(string path)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L357) | private static |
| BuildPaletteBaker | [`void CreateBorder()`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L374) | private static |
| BuildPaletteBaker | [`void CreateHintAssets(out Mesh mesh, out Material material)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L392) | private static |
| BuildPaletteBaker | [`void ConfigureHintMaterial(Material material)`](../Assets/Editor/Building/Interface/BuildPaletteBaker.cs#L449) | public static |

## Assets/Editor/Building/Materials/BlockMaterialAuthoring.cs

// 职责：制作可重现的URP结构材质、碎片网格/粒子Prefab及目录配置，安装基础结构支持组件。 // 入口：Install()；现有材质/配置不覆盖，重复执行只补缺失资源，保留手动参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockMaterialAuthoring | [`void Install()`](../Assets/Editor/Building/Materials/BlockMaterialAuthoring.cs#L13) | public static |
| BlockMaterialAuthoring | [`void InstallSelector()`](../Assets/Editor/Building/Materials/BlockMaterialAuthoring.cs#L133) | public static |
| BlockMaterialAuthoring | [`Material Surface(string id, Color color)`](../Assets/Editor/Building/Materials/BlockMaterialAuthoring.cs#L210) | private static |
| BlockMaterialAuthoring | [`Texture2D Texture(string id)`](../Assets/Editor/Building/Materials/BlockMaterialAuthoring.cs#L248) | private static |
| BlockMaterialAuthoring | [`MaterialBreakEffect Break(string id, Material surface)`](../Assets/Editor/Building/Materials/BlockMaterialAuthoring.cs#L274) | private static |

## Assets/Editor/Building/Materials/BlockMaterialChecks.cs

// 职责：验证真实材质资产、存档恢复、旧蓝图兼容、健康比例、表现与有界碎片。 // 入口：Run()；Main Play中创建自有夹具，不保存玩家蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockMaterialChecks | [`string Run()`](../Assets/Editor/Building/Materials/BlockMaterialChecks.cs#L20) | public static |
| BlockMaterialChecks | [`Block Restore(BlockData data, Transform parent)`](../Assets/Editor/Building/Materials/BlockMaterialChecks.cs#L227) | private static |
| BlockMaterialChecks | [`void Check(Report report, bool valid, string description)`](../Assets/Editor/Building/Materials/BlockMaterialChecks.cs#L255) | private static |
| BlockMaterialChecks | [`string Save(Report report)`](../Assets/Editor/Building/Materials/BlockMaterialChecks.cs#L265) | private static |

## Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs

// 职责：为现有建筑编辑Prefab添加可编辑据点表单，保留底座尺寸控件与既有GUID。 // 入口：Upgrade()；通过PrefabUtility保存真实资产，重复执行不重复添加。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostEditorAuthoring | [`void Upgrade()`](../Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs#L12) | public static |
| OutpostEditorAuthoring | [`void AddTemplateCopy(GameObject root, BuildingOutpostEditor editor)`](../Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs#L108) | private static |
| OutpostEditorAuthoring | [`void GroupOutpostControls(GameObject root)`](../Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs#L141) | private static |
| OutpostEditorAuthoring | [`void AddMatchingBase(GameObject root, BuildingOutpostEditor editor)`](../Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs#L178) | private static |
| OutpostEditorAuthoring | [`Dropdown Dropdown(Transform parent, string name, string caption, float y)`](../Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs#L195) | private static |
| OutpostEditorAuthoring | [`Button Button(Transform parent, string name, string caption, float x)`](../Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs#L203) | private static |
| OutpostEditorAuthoring | [`Text Label(Transform parent, string name, string caption, Vector2 size, Vector2 position)`](../Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs#L211) | private static |
| OutpostEditorAuthoring | [`void Place(GameObject obj, Transform parent, string name, Vector2 size, Vector2 position)`](../Assets/Editor/Building/Outposts/OutpostEditorAuthoring.cs#L224) | private static |

## Assets/Editor/Building/Validation/BaseLibraryChecks.cs

// 职责：在临时目录验证基地库迁移的冲突保护、备份和重复执行，不触碰玩家文件。 // 入口：Run()；验证生产迁移入口，而非复制迁移算法。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BaseLibraryChecks | [`string Run()`](../Assets/Editor/Building/Validation/BaseLibraryChecks.cs#L11) | public static |
| BaseLibraryChecks | [`void Require(bool condition, string reason)`](../Assets/Editor/Building/Validation/BaseLibraryChecks.cs#L52) | private static |

## Assets/Editor/Building/Validation/BlueprintCapacityChecks.cs

// 职责：验证解除建筑模块硬上限后仍保留身份/数值校验及损坏蓝图审计边界。 // 入口：Run()；纯数据和Temp隔离文件，不加载大型实体、不访问个人蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintCapacityChecks | [`string Run()`](../Assets/Editor/Building/Validation/BlueprintCapacityChecks.cs#L12) | public static |
| BlueprintCapacityChecks | [`void Require(bool value, string name, List<string> checks)`](../Assets/Editor/Building/Validation/BlueprintCapacityChecks.cs#L97) | private static |

## Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs

// 职责：通过生产分帧恢复/修缮链验证大型建筑不再受2048/8192模块硬限制，并验证取消回滚。 // 入口：Start() / Status；仅Main Play，远端隔离夹具，串行逐帧执行，结束销毁本工具拥有的对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintCapacityPlayChecks | [`void Start()`](../Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs#L29) | public static |
| BlueprintCapacityPlayChecks | [`void Tick()`](../Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs#L46) | private static |
| BlueprintCapacityPlayChecks | [`IEnumerator Run()`](../Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs#L82) | private static |
| BlueprintCapacityPlayChecks | [`IEnumerator Restore(WorldBuildingManager manager, WorldBuildingState state, Action<BuildingRuntime> complete)`](../Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs#L174) | private static |
| BlueprintCapacityPlayChecks | [`BlockDataList CreateDesign()`](../Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs#L179) | private static |
| BlueprintCapacityPlayChecks | [`WorldBuildingState State(BlockDataList design, string id, int slot)`](../Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs#L202) | private static |
| BlueprintCapacityPlayChecks | [`void Require(bool value, string name)`](../Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs#L217) | private static |
| BlueprintCapacityPlayChecks | [`void Save()`](../Assets/Editor/Building/Validation/BlueprintCapacityPlayChecks.cs#L228) | private static |

## Assets/Editor/Building/Validation/BlueprintIntegrityAudit.cs

// 职责：审查官方/个人蓝图完整性，将确认损坏的个人文件移入可恢复隔离区，保留哈希和原路径。 // 入口：AuditAll() / QuarantineInvalidPersonal()；不把未完成设计当损坏，不自动移动被资产引用的官方文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintIntegrityAudit | [`string AuditAll()`](../Assets/Editor/Building/Validation/BlueprintIntegrityAudit.cs#L34) | public static |
| BlueprintIntegrityAudit | [`string QuarantineInvalidPersonal()`](../Assets/Editor/Building/Validation/BlueprintIntegrityAudit.cs#L44) | public static |
| BlueprintIntegrityAudit | [`Report InspectAll()`](../Assets/Editor/Building/Validation/BlueprintIntegrityAudit.cs#L87) | public static |
| BlueprintIntegrityAudit | [`Entry Inspect(string path, string category)`](../Assets/Editor/Building/Validation/BlueprintIntegrityAudit.cs#L135) | public static |
| BlueprintIntegrityAudit | [`void ValidateModules(BlockDataList data)`](../Assets/Editor/Building/Validation/BlueprintIntegrityAudit.cs#L182) | private static |
| BlueprintIntegrityAudit | [`string NewReportDirectory()`](../Assets/Editor/Building/Validation/BlueprintIntegrityAudit.cs#L222) | private static |
| BlueprintIntegrityAudit | [`string Hash(string path)`](../Assets/Editor/Building/Validation/BlueprintIntegrityAudit.cs#L232) | private static |

## Assets/Editor/Building/Validation/BlueprintSupportAudit.cs

// 职责：逐个恢复官方/个人基地并使用生产连接图检查地面支撑，定位JSON合法但不能部署的设计。 // 入口：Start() / Status；Main Play远端隔离逐帧检查，只报告，不删除或重写个人创作。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintSupportAudit | [`void Start()`](../Assets/Editor/Building/Validation/BlueprintSupportAudit.cs#L39) | public static |
| BlueprintSupportAudit | [`void Tick()`](../Assets/Editor/Building/Validation/BlueprintSupportAudit.cs#L54) | private static |
| BlueprintSupportAudit | [`IEnumerator Run()`](../Assets/Editor/Building/Validation/BlueprintSupportAudit.cs#L90) | private static |
| BlueprintSupportAudit | [`void Save()`](../Assets/Editor/Building/Validation/BlueprintSupportAudit.cs#L181) | private static |

## Assets/Editor/Building/Validation/BuildHotbarChecks.cs

// 职责：在真实Main界面验证快捷栏输入、拖放、持久化及不干扰文字/战斗的契约。 // 入口：RunLive()；恢复原PlayerPrefs与输入设备，不写玩家蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildHotbarChecks | [`void RunLive()`](../Assets/Editor/Building/Validation/BuildHotbarChecks.cs#L30) | public static |
| BuildHotbarChecks | [`IEnumerator Scenario()`](../Assets/Editor/Building/Validation/BuildHotbarChecks.cs#L62) | private static |
| BuildHotbarChecks | [`void Tick()`](../Assets/Editor/Building/Validation/BuildHotbarChecks.cs#L226) | private static |
| BuildHotbarChecks | [`void Check(bool passed, string name)`](../Assets/Editor/Building/Validation/BuildHotbarChecks.cs#L252) | private static |
| BuildHotbarChecks | [`void Finish(string error)`](../Assets/Editor/Building/Validation/BuildHotbarChecks.cs#L262) | private static |

## Assets/Editor/Building/Validation/BuildNavigationPlayChecks.cs

// 职责：跨真实帧验证工坊右键长按、UI捕获、方向键移动和失焦释放，不写个人存档。 // 入口：RunLive()；完成恢复输入设备/设置，在Temp记录观察结果。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildNavigationPlayChecks | [`void RunLive()`](../Assets/Editor/Building/Validation/BuildNavigationPlayChecks.cs#L24) | public static |
| BuildNavigationPlayChecks | [`void Tick()`](../Assets/Editor/Building/Validation/BuildNavigationPlayChecks.cs#L39) | private static |
| BuildNavigationPlayChecks | [`IEnumerator Scenario()`](../Assets/Editor/Building/Validation/BuildNavigationPlayChecks.cs#L74) | private static |
| BuildNavigationPlayChecks | [`void Check(bool passed, string name)`](../Assets/Editor/Building/Validation/BuildNavigationPlayChecks.cs#L218) | private static |

## Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs

// 职责：在 Play Mode 检查分类目录、选择、放置预览与连接点提示。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPalettePlayProbe | [`void Run()`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L35) | public static |
| BuildPalettePlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L58) | private static |
| BuildPalettePlayProbe | [`void Capture(string message, string stack, LogType type)`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L94) | private static |
| BuildPalettePlayProbe | [`void Tick()`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L102) | private static |
| BuildPalettePlayProbe | [`void Finish()`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L132) | private static |
| BuildPalettePlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L160) | private static |
| BuildPalettePlayProbe | [`void Invoke(object target, string name)`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L170) | private static |
| BuildPalettePlayProbe | [`void MovePointer(Vector2 position)`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L175) | private static |
| BuildPalettePlayProbe | [`Block RangeBlock(Vector3 position)`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L186) | private static |
| BuildPalettePlayProbe | [`float HintAlpha(ConnectorPlacementHints hints)`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L199) | private static |
| BuildPalettePlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Building/Validation/BuildPalettePlayProbe.cs#L211) | private static |

## Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs

// 职责：在隔离存档的真实 Play Mode 检查建筑快捷键、底座连接、尺寸 UI 和跨模式持久化。 // 入口：Run()；只写 Temp/BuildingEditor，不改动用户存档或保存 Main 场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingEditorPlayProbe | [`void Run()`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L32) | public static |
| BuildingEditorPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L45) | private static |
| BuildingEditorPlayProbe | [`void Capture(string message, string stack, LogType type)`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L84) | private static |
| BuildingEditorPlayProbe | [`void Tick()`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L92) | private static |
| BuildingEditorPlayProbe | [`void Finish()`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L122) | private static |
| BuildingEditorPlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L149) | private static |
| BuildingEditorPlayProbe | [`void Field(object target, string name, object value)`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L159) | private static |
| BuildingEditorPlayProbe | [`object Field(object target, string name)`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L164) | private static |
| BuildingEditorPlayProbe | [`object CreateFileStore(string directory)`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L169) | private static |
| BuildingEditorPlayProbe | [`void Shortcut(params Key[] keys)`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L175) | private static |
| BuildingEditorPlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Building/Validation/BuildingEditorPlayProbe.cs#L186) | private static |

## Assets/Editor/Building/Validation/WorldBuildingFootprintChecks.cs

// 职责：验证自动占地偏移的世界地形平台、虚拟锚点和建筑保存/修复契约，不改写玩家蓝图。 // 入口：RunEdit() 检查确定性地形；RunRuntime() 在Play内检查真实建筑支撑和快照。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingFootprintChecks | [`string RunEdit()`](../Assets/Editor/Building/Validation/WorldBuildingFootprintChecks.cs#L13) | public static |
| WorldBuildingFootprintChecks | [`string RunRuntime()`](../Assets/Editor/Building/Validation/WorldBuildingFootprintChecks.cs#L93) | public static |
| WorldBuildingFootprintChecks | [`BlockDataList Sample(int x, int z)`](../Assets/Editor/Building/Validation/WorldBuildingFootprintChecks.cs#L170) | private static |
| WorldBuildingFootprintChecks | [`void Check(List<string> checks, string label, bool condition)`](../Assets/Editor/Building/Validation/WorldBuildingFootprintChecks.cs#L191) | private static |
| WorldBuildingFootprintChecks | [`void CheckCondition(bool condition, string label)`](../Assets/Editor/Building/Validation/WorldBuildingFootprintChecks.cs#L197) | private static |
| WorldBuildingFootprintChecks | [`string Save(string phase, List<string> checks)`](../Assets/Editor/Building/Validation/WorldBuildingFootprintChecks.cs#L205) | private static |

## Assets/Editor/Combat/Diagnostics/CombatDiagnosticsAuthoring.cs

// 职责：迁移共享诊断抽屉并保存有界轨迹组件，保留现有HUD与各场景手工排版。 // 入口：Install()；Prefab改动向场景传播，轨迹池只制作在工坊服务场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatDiagnosticsAuthoring | [`void Install()`](../Assets/Editor/Combat/Diagnostics/CombatDiagnosticsAuthoring.cs#L14) | public static |
| CombatDiagnosticsAuthoring | [`void UpgradePanel(CombatHudPanels panel)`](../Assets/Editor/Combat/Diagnostics/CombatDiagnosticsAuthoring.cs#L67) | public static |
| CombatDiagnosticsAuthoring | [`LineRenderer Line(Transform parent, string name, Material material, Color color, float width)`](../Assets/Editor/Combat/Diagnostics/CombatDiagnosticsAuthoring.cs#L111) | private static |

## Assets/Editor/Combat/Diagnostics/FlightCourseChecks.cs

// 职责：验证直航锁定与横偏恢复、紧急接管及限角速度契约，并采集两艘船的直线飞行。 // 入口：Validate()/Begin(label)；仅在导航试验场Play会话运行，不保存测试期配置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| FlightCourseChecks | [`string Validate()`](../Assets/Editor/Combat/Diagnostics/FlightCourseChecks.cs#L17) | public static |
| FlightCourseChecks | [`void Begin(string label)`](../Assets/Editor/Combat/Diagnostics/FlightCourseChecks.cs#L119) | public static |
| FlightCourseChecks | [`Task<string> Run(string label)`](../Assets/Editor/Combat/Diagnostics/FlightCourseChecks.cs#L124) | private static async |
| FlightCourseChecks | [`void Require(bool condition, string message)`](../Assets/Editor/Combat/Diagnostics/FlightCourseChecks.cs#L175) | private static |

## Assets/Editor/Combat/Diagnostics/NavigationSensorChecks.cs

// 职责：回归多模块刚体导致感知缓冲误饱和的问题，检查重复采集与静态通道保留。 // 入口：Play Mode中Validate()；在隔离物理场景构造600模块交通对象，不修改已打开场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationSensorChecks | [`string Validate()`](../Assets/Editor/Combat/Diagnostics/NavigationSensorChecks.cs#L10) | public static |

## Assets/Editor/Combat/Diagnostics/PropulsionInputChecks.cs

// 职责：用用户Type 03蓝图采样真实直航、模拟输入与通用推进器朝向，避免只验证导航期望方向。 // 入口：Begin(label)；独立EnemyAvoidanceLab Play会话，结果写入Temp，不改源蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PropulsionInputChecks | [`void Begin(string label)`](../Assets/Editor/Combat/Diagnostics/PropulsionInputChecks.cs#L41) | public static |
| PropulsionInputChecks | [`string Validate()`](../Assets/Editor/Combat/Diagnostics/PropulsionInputChecks.cs#L46) | public static |
| PropulsionInputChecks | [`void Require(bool condition, string message)`](../Assets/Editor/Combat/Diagnostics/PropulsionInputChecks.cs#L117) | private static |
| PropulsionInputChecks | [`Task<string> Capture(string label)`](../Assets/Editor/Combat/Diagnostics/PropulsionInputChecks.cs#L125) | private static async |

## Assets/Editor/Combat/Diagnostics/SkirmishNavigationAuthoring.cs

// 职责：给现有随机5v5保存五组敌舰导航追踪、轨迹/路线与调试面板；不重建地图或阵容。 // 入口：Install()菜单 / Ensure(scene)；幂等，已有追踪配置保留，显式保存目标场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SkirmishNavigationAuthoring | [`void Install()`](../Assets/Editor/Combat/Diagnostics/SkirmishNavigationAuthoring.cs#L12) | public static |
| SkirmishNavigationAuthoring | [`void Ensure(Scene scene)`](../Assets/Editor/Combat/Diagnostics/SkirmishNavigationAuthoring.cs#L26) | public static |
| SkirmishNavigationAuthoring | [`LineRenderer Line(Transform parent, string name, Color color, float width)`](../Assets/Editor/Combat/Diagnostics/SkirmishNavigationAuthoring.cs#L75) | private static |

## Assets/Editor/Combat/Diagnostics/SkirmishNavigationChecks.cs

// 职责：验证竞技场显式追踪配置、共享轨迹环形边界及真实战斗中的绑定/换轮清理。 // 入口：ValidateSaved()/Begin()；Play检查保留开火并采集导航，末段通过正式淘汰流程验证重开。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SkirmishNavigationChecks | [`string ValidateSaved()`](../Assets/Editor/Combat/Diagnostics/SkirmishNavigationChecks.cs#L18) | public static |
| SkirmishNavigationChecks | [`void Begin()`](../Assets/Editor/Combat/Diagnostics/SkirmishNavigationChecks.cs#L70) | public static |
| SkirmishNavigationChecks | [`Task<string> Run()`](../Assets/Editor/Combat/Diagnostics/SkirmishNavigationChecks.cs#L75) | private static async |
| SkirmishNavigationChecks | [`string Save(string name, List<string> checks)`](../Assets/Editor/Combat/Diagnostics/SkirmishNavigationChecks.cs#L161) | private static |
| SkirmishNavigationChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Combat/Diagnostics/SkirmishNavigationChecks.cs#L169) | private static |

## Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs

// 职责：制作独立避障场景及持久化AI调试开关；不覆盖用户方块表和蓝图。 // 入口：Create() / EnableDebug()；重用既有服务配置，保存真实舰体和可编辑障碍布局。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyAvoidanceLabAuthoring | [`void Create()`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L13) | public static |
| EnemyAvoidanceLabAuthoring | [`void FrameLab()`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L171) | public static |
| EnemyAvoidanceLabAuthoring | [`void AddRoofedTrial()`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L184) | public static |
| EnemyAvoidanceLabAuthoring | [`void UpgradeTestField()`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L216) | public static |
| EnemyAvoidanceLabAuthoring | [`LineRenderer TraceLine(Transform parent, string name, Color color)`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L340) | private static |
| EnemyAvoidanceLabAuthoring | [`void RefreshPresentation()`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L359) | public static |
| EnemyAvoidanceLabAuthoring | [`Material LabMaterial(string name, Color color)`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L382) | private static |
| EnemyAvoidanceLabAuthoring | [`void EnableDebug()`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L397) | public static |
| EnemyAvoidanceLabAuthoring | [`void SetDebug(Scene scene)`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L447) | private static |
| EnemyAvoidanceLabAuthoring | [`void Set(UnityEngine.Object owner, string field, object value)`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L456) | private static |
| EnemyAvoidanceLabAuthoring | [`void Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)`](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L476) | private static |

## Assets/Editor/Combat/EnemyLabComplexAuthoring.cs

// 职责：保存综合障碍城区、可选起终点与自由相机，幂等扩展既有试验场。 // 入口：Upgrade()；只新增缺失对象，保留用户对已制作场地的手动调整。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabComplexAuthoring | [`void Upgrade()`](../Assets/Editor/Combat/EnemyLabComplexAuthoring.cs#L11) | public static |
| EnemyLabComplexAuthoring | [`void AddDistrictPresentation(Transform district)`](../Assets/Editor/Combat/EnemyLabComplexAuthoring.cs#L198) | private static |
| EnemyLabComplexAuthoring | [`Material MaterialAsset(string name, Color color)`](../Assets/Editor/Combat/EnemyLabComplexAuthoring.cs#L267) | private static |
| EnemyLabComplexAuthoring | [`void Decoration(Transform root, string name, Vector3 position, Vector3 size, Material material)`](../Assets/Editor/Combat/EnemyLabComplexAuthoring.cs#L281) | private static |
| EnemyLabComplexAuthoring | [`void Box(Transform root, string name, Vector3 position, Vector3 size, Material material)`](../Assets/Editor/Combat/EnemyLabComplexAuthoring.cs#L287) | private static |

## Assets/Editor/Combat/EnemyLabExpansionAuthoring.cs

// 职责：在既有综合城区上追加可编辑高架层、部署点和运动学障碍。 // 入口：Upgrade()菜单；只创建缺失分组，重复执行保留人工参数和布局。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabExpansionAuthoring | [`void Upgrade()`](../Assets/Editor/Combat/EnemyLabExpansionAuthoring.cs#L11) | public static |
| EnemyLabExpansionAuthoring | [`void AddLocations(EnemyAvoidanceCourse course, Material material)`](../Assets/Editor/Combat/EnemyLabExpansionAuthoring.cs#L154) | private static |
| EnemyLabExpansionAuthoring | [`void Moving( Transform root, string name, Vector3 position, Vector3 size, Vector3 travel, float period, float angle, Material material)`](../Assets/Editor/Combat/EnemyLabExpansionAuthoring.cs#L211) | private static |
| EnemyLabExpansionAuthoring | [`Transform Group(Transform parent, string name)`](../Assets/Editor/Combat/EnemyLabExpansionAuthoring.cs#L236) | private static |
| EnemyLabExpansionAuthoring | [`GameObject Box(Transform root, string name, Vector3 position, Vector3 size, Material material)`](../Assets/Editor/Combat/EnemyLabExpansionAuthoring.cs#L244) | private static |

## Assets/Editor/Combat/ExplosionAuthoring.cs

// 职责：为现有方块CSV增加独立爆炸伤害列，并通过现有备份/校验链保存真实Prefab。 // 入口：InstallDamageColumn()；重复运行保留已经编辑的伤害值，不写玩家蓝图或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ExplosionAuthoring | [`void Install()`](../Assets/Editor/Combat/ExplosionAuthoring.cs#L13) | public static |
| ExplosionAuthoring | [`string InstallDamageColumn()`](../Assets/Editor/Combat/ExplosionAuthoring.cs#L19) | public static |

## Assets/Editor/Combat/Interface/CombatHudSetup.cs

// 职责：配置主场景战斗 HUD 与界面反馈组件。 // 入口：UpgradeDualHealthHud() / Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatHudSetup | [`void UpgradeDualHealthHud()`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L18) | public static |
| CombatHudSetup | [`void Apply()`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L102) | public static |
| CombatHudSetup | [`RectTransform Child(Transform parent, string name)`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L125) | private static |
| CombatHudSetup | [`void Stretch(RectTransform rect)`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L138) | private static |
| CombatHudSetup | [`void Position(RectTransform rect, float left, float bottom, float right, float top)`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L146) | private static |
| CombatHudSetup | [`void BottomBox(RectTransform rect, float left, float bottom, float right, float top)`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L153) | private static |
| CombatHudSetup | [`Image Image(RectTransform rect, Color color)`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L162) | private static |
| CombatHudSetup | [`Text Text( Transform parent, string name, Font font, int size, Color color, TextAnchor alignment)`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L175) | private static |
| CombatHudSetup | [`void Set(UnityEngine.Object target, string name, UnityEngine.Object value)`](../Assets/Editor/Combat/Interface/CombatHudSetup.cs#L199) | private static |

## Assets/Editor/Combat/Interface/HealthHudPreview.cs

// 职责：创建耐久/血条布局的可编辑预览。 // 入口：Render()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HealthHudPreview | [`string Render()`](../Assets/Editor/Combat/Interface/HealthHudPreview.cs#L17) | public static |
| HealthHudPreview | [`void RenderOne(Transform source, Vector2Int size)`](../Assets/Editor/Combat/Interface/HealthHudPreview.cs#L44) | private static |

## Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs

// 职责：制作共享战斗HUD Prefab、TMP字体和主题，并幂等迁移真实场景的相关界面。 // 入口：Install() / InstallScene()；只重建自身HUD和旧战斗面板，保存前拒绝未保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SharedCombatHudAuthoring | [`void InstallNameplateExclusions()`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L18) | public static |
| SharedCombatHudAuthoring | [`void ConfigureNameplateExclusions(GameObject root)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L37) | private static |
| SharedCombatHudAuthoring | [`void Install()`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L58) | public static |
| SharedCombatHudAuthoring | [`void InstallScene(Scene scene)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L99) | public static |
| SharedCombatHudAuthoring | [`void EnsureAssets()`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L204) | private static |
| SharedCombatHudAuthoring | [`void RebuildPrefab()`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L249) | public static |
| SharedCombatHudAuthoring | [`void BakePrefab()`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L261) | private static |
| SharedCombatHudAuthoring | [`RectTransform BuildNameplate(Transform parent)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L294) | private static |
| SharedCombatHudAuthoring | [`CombatThreatMarkers BuildThreats(Transform parent, CombatHud hud)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L347) | private static |
| SharedCombatHudAuthoring | [`CombatNotifications BuildNotifications(RectTransform parent)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L399) | private static |
| SharedCombatHudAuthoring | [`void BuildPanels(RectTransform root)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L480) | private static |
| SharedCombatHudAuthoring | [`ThrusterInfoPanel BuildThrusters(Transform parent)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L546) | private static |
| SharedCombatHudAuthoring | [`RectTransform Drawer(string name, Transform parent, Vector2 size)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L643) | private static |
| SharedCombatHudAuthoring | [`T Find(Scene scene)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L651) | public static |
| SharedCombatHudAuthoring | [`void Set(Object owner, string field, Object value)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L657) | public static |
| SharedCombatHudAuthoring | [`void SetArray(Object owner, string field, Object[] values)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L664) | private static |
| SharedCombatHudAuthoring | [`RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L677) | private static |
| SharedCombatHudAuthoring | [`void Stretch(RectTransform rect)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L687) | private static |
| SharedCombatHudAuthoring | [`Image Back(RectTransform rect, Color color)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L695) | private static |
| SharedCombatHudAuthoring | [`RectTransform Fill(RectTransform parent, string name, Color color)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L703) | private static |
| SharedCombatHudAuthoring | [`CanvasGroup Group(RectTransform rect, bool interactive = false)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L712) | private static |
| SharedCombatHudAuthoring | [`TMP_Text Label( Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize, Color color)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L719) | private static |
| SharedCombatHudAuthoring | [`Button Button(Transform parent, string title, Vector2 position, Vector2 size)`](../Assets/Editor/Combat/Interface/SharedCombatHudAuthoring.cs#L743) | private static |

## Assets/Editor/Combat/Interface/SharedCombatHudPreview.cs

// 职责：用真实Prefab和uGUI相机渲染共享战斗HUD，检查多分辨率边界及字形。 // 入口：Render()；创建并清理独立Preview Scene，不修改生产场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SharedCombatHudPreview | [`string Render()`](../Assets/Editor/Combat/Interface/SharedCombatHudPreview.cs#L17) | public static |
| SharedCombatHudPreview | [`void RenderOne(Vector2Int size)`](../Assets/Editor/Combat/Interface/SharedCombatHudPreview.cs#L64) | private static |
| SharedCombatHudPreview | [`void RequireSeparate(RectTransform first, RectTransform second, Camera camera, Vector2Int size)`](../Assets/Editor/Combat/Interface/SharedCombatHudPreview.cs#L254) | private static |

## Assets/Editor/Combat/SkirmishArenaAuthoring.cs

// 职责：复制双层避障地图为独立5v5观战场景，保存十槽位、自由相机与复用战斗HUD。 // 入口：Create()菜单；已存在场景拒绝覆盖，不修改原避障场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SkirmishArenaAuthoring | [`void Create()`](../Assets/Editor/Combat/SkirmishArenaAuthoring.cs#L14) | public static |
| SkirmishArenaAuthoring | [`void UpgradeToFive()`](../Assets/Editor/Combat/SkirmishArenaAuthoring.cs#L145) | public static |
| SkirmishArenaAuthoring | [`void SpreadBattlefield(Scene scene)`](../Assets/Editor/Combat/SkirmishArenaAuthoring.cs#L191) | private static |
| SkirmishArenaAuthoring | [`T Find(Scene scene)`](../Assets/Editor/Combat/SkirmishArenaAuthoring.cs#L240) | private static |
| SkirmishArenaAuthoring | [`void UpgradeHud()`](../Assets/Editor/Combat/SkirmishArenaAuthoring.cs#L246) | public static |
| SkirmishArenaAuthoring | [`void ConfigureCombatHud(CombatHud hud)`](../Assets/Editor/Combat/SkirmishArenaAuthoring.cs#L259) | private static |
| SkirmishArenaAuthoring | [`void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)`](../Assets/Editor/Combat/SkirmishArenaAuthoring.cs#L264) | private static |
| SkirmishArenaAuthoring | [`Text Label( Transform parent, string name, Font font, Vector2 position, Vector2 size, int fontSize)`](../Assets/Editor/Combat/SkirmishArenaAuthoring.cs#L271) | private static |

## Assets/Editor/Combat/Turrets/TurretAssetBaker.cs

// 职责：从指定 SpaceKit 源制作单管/双管炮塔、真实炮弹及独立建造目录资产。 // 入口：Bake()；只保存拥有的 Prefab/材质/图标，不保存当前场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretAssetBaker | [`void RefreshTurretIcons()`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L16) | public static |
| TurretAssetBaker | [`void ApplyExistingButtonStyle(GameObject entry)`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L48) | private static |
| TurretAssetBaker | [`void ConfigureRecoil()`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L62) | public static |
| TurretAssetBaker | [`void Bake()`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L105) | public static |
| TurretAssetBaker | [`TurretProjectile BakeProjectile()`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L126) | private static |
| TurretAssetBaker | [`void ConfigureLaser()`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L171) | private static |
| TurretAssetBaker | [`void BakeTurret(string name, string variant, float demand, TurretProjectile projectile)`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L194) | private static |
| TurretAssetBaker | [`Sprite RenderIcon(string name)`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L288) | private static |
| TurretAssetBaker | [`void BakeEntry(string name)`](../Assets/Editor/Combat/Turrets/TurretAssetBaker.cs#L301) | private static |

## Assets/Editor/Combat/Validation/AiUpgradePlayChecks.cs

// 职责：用真实运行组件检查目标稳定性、局部物理场景避障、修复重组索敌及炮弹相对运动提前量。 // 入口：Run()；仅当前 Play Mode，夹具隔离于本地物理场景，结果写入 Temp/AIUpgrade。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| AiUpgradePlayChecks | [`string Run()`](../Assets/Editor/Combat/Validation/AiUpgradePlayChecks.cs#L14) | public static |
| AiUpgradePlayChecks | [`void CheckVisibility(Transform root, List<string> checks, Type selectorType, MethodInfo select)`](../Assets/Editor/Combat/Validation/AiUpgradePlayChecks.cs#L294) | private static |
| AiUpgradePlayChecks | [`ControlUnit Unit(Transform root, string name, UnitFaction faction, Vector3 position)`](../Assets/Editor/Combat/Validation/AiUpgradePlayChecks.cs#L367) | private static |
| AiUpgradePlayChecks | [`Durability Health(Transform root, string name, Vector3 position)`](../Assets/Editor/Combat/Validation/AiUpgradePlayChecks.cs#L383) | private static |
| AiUpgradePlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Combat/Validation/AiUpgradePlayChecks.cs#L394) | private static |

## Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs

// 职责：通过真实Main模式入口和真实伤害链验证共享HUD的投影、损伤动画、击杀去重与退出恢复。 // 入口：Run() / RunArena()；只在Play实例上刺激，不修改蓝图、场景或HUD资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatHudPlayProbe | [`void Run()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L34) | public static |
| CombatHudPlayProbe | [`void RunArena()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L40) | public static |
| CombatHudPlayProbe | [`void RunExploration()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L46) | public static |
| CombatHudPlayProbe | [`void RunLegacy()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L52) | public static |
| CombatHudPlayProbe | [`void RunFlow()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L58) | public static |
| CombatHudPlayProbe | [`void RunRecovery()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L63) | public static |
| CombatHudPlayProbe | [`void Begin(string kind, string path)`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L68) | private static |
| CombatHudPlayProbe | [`void OnPlayModeChanged(PlayModeStateChange state)`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L81) | private static |
| CombatHudPlayProbe | [`void Tick()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L111) | private static |
| CombatHudPlayProbe | [`IEnumerator Siege()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L140) | private static |
| CombatHudPlayProbe | [`IEnumerator Legacy()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L285) | private static |
| CombatHudPlayProbe | [`IEnumerator Flow()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L322) | private static |
| CombatHudPlayProbe | [`IEnumerator Recovery()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L465) | private static |
| CombatHudPlayProbe | [`IEnumerator Arena()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L598) | private static |
| CombatHudPlayProbe | [`IEnumerator Exploration()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L642) | private static |
| CombatHudPlayProbe | [`void Screenshot(string name)`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L683) | private static |
| CombatHudPlayProbe | [`void Check(string name, bool passed)`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L689) | private static |
| CombatHudPlayProbe | [`void Finish()`](../Assets/Editor/Combat/Validation/CombatHudPlayProbe.cs#L700) | private static |

## Assets/Editor/Combat/Validation/EnemyBlueprintPoolChecks.cs

// 职责：隔离临时蓝图库，验证真实敌机池初始化遇损坏文件仍保留有效蓝图。 // 入口：Run()；同步检查后恢复SaveManager私有文件存储和名称列表，不写用户目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyBlueprintPoolChecks | [`string Run()`](../Assets/Editor/Combat/Validation/EnemyBlueprintPoolChecks.cs#L23) | public static |
| EnemyBlueprintPoolChecks | [`string ValidBlueprint()`](../Assets/Editor/Combat/Validation/EnemyBlueprintPoolChecks.cs#L131) | private static |
| EnemyBlueprintPoolChecks | [`void Check(Report report, string label, bool passed)`](../Assets/Editor/Combat/Validation/EnemyBlueprintPoolChecks.cs#L139) | private static |

## Assets/Editor/Combat/Validation/EnemyLabEfficiencyChecks.cs

// 职责：用相同蓝图和路线顺序对比导航效率，保留逐轮记录供耗时、停顿和规划分析。 // 入口：Begin(label)；仅试验场Play Mode，固定物理步长，临时加速并在结束时恢复。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabEfficiencyChecks | [`void Begin(string label)`](../Assets/Editor/Combat/Validation/EnemyLabEfficiencyChecks.cs#L33) | public static |
| EnemyLabEfficiencyChecks | [`Task<string> RunCourses(string label)`](../Assets/Editor/Combat/Validation/EnemyLabEfficiencyChecks.cs#L43) | private static async |

## Assets/Editor/Combat/Validation/EnemyLabExpansionChecks.cs

// 职责：验证双层场地的持久资产、动态障碍物理运动及试验复位生命周期。 // 入口：ValidateSaved()用于Edit Mode；Begin()用于Play Mode，Pending返回实际检查结果。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabExpansionChecks | [`string ValidateSaved()`](../Assets/Editor/Combat/Validation/EnemyLabExpansionChecks.cs#L17) | public static |
| EnemyLabExpansionChecks | [`void Begin()`](../Assets/Editor/Combat/Validation/EnemyLabExpansionChecks.cs#L60) | public static |
| EnemyLabExpansionChecks | [`Task<string> Run()`](../Assets/Editor/Combat/Validation/EnemyLabExpansionChecks.cs#L65) | private static async |
| EnemyLabExpansionChecks | [`void Check(List<string> checks, string label, bool condition)`](../Assets/Editor/Combat/Validation/EnemyLabExpansionChecks.cs#L161) | private static |
| EnemyLabExpansionChecks | [`string Save(string file, List<string> checks)`](../Assets/Editor/Combat/Validation/EnemyLabExpansionChecks.cs#L170) | private static |

## Assets/Editor/Combat/Validation/EnemyLabPlayChecks.cs

// 职责：验证试验场蓝图替换、操作记录、暂停恢复、退出落盘和布局切换的完整主链。 // 入口：Begin()；仅在独立EnemyAvoidanceLab的Play Mode运行，结果写入Temp。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabPlayChecks | [`void Begin()`](../Assets/Editor/Combat/Validation/EnemyLabPlayChecks.cs#L18) | public static |
| EnemyLabPlayChecks | [`Task<string> Run()`](../Assets/Editor/Combat/Validation/EnemyLabPlayChecks.cs#L23) | private static async |
| EnemyLabPlayChecks | [`Vector3 GetGoal(EnemyAvoidanceLab lab)`](../Assets/Editor/Combat/Validation/EnemyLabPlayChecks.cs#L161) | private static |
| EnemyLabPlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Combat/Validation/EnemyLabPlayChecks.cs#L168) | private static |

## Assets/Editor/Combat/Validation/EnemyLabViewChecks.cs

// 职责：在Play验证综合场地、预设起终点、自由相机与暂停期间的真实输入链。 // 入口：Begin()；虚拟输入设备在finally清理，输出保存Temp/EnemyAvoidanceLab。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabViewChecks | [`void Begin()`](../Assets/Editor/Combat/Validation/EnemyLabViewChecks.cs#L19) | public static |
| EnemyLabViewChecks | [`Task<string> Run()`](../Assets/Editor/Combat/Validation/EnemyLabViewChecks.cs#L24) | private static async |
| EnemyLabViewChecks | [`void Check(List<string> checks, string name, bool value)`](../Assets/Editor/Combat/Validation/EnemyLabViewChecks.cs#L146) | private static |

## Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs

// 职责：在真实Play的独立PhysicsScene验证爆炸仅破坏体素、模块免伤、无连锁及冲量隔离。 // 入口：Run()；只创建自有夹具，不写玩家存档，报告Temp/Combat/explosion-integration.json。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ExplosionIntegrationChecks | [`string Run()`](../Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs#L21) | public static |
| ExplosionIntegrationChecks | [`void TerrainCheck(Scene scene, Report report)`](../Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs#L146) | private static |
| ExplosionIntegrationChecks | [`WorldBuildingManager Manager(Scene scene)`](../Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs#L170) | private static |
| ExplosionIntegrationChecks | [`BuildingRuntime Building(WorldBuildingManager manager, Vector3 center)`](../Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs#L183) | private static |
| ExplosionIntegrationChecks | [`Block Ship(Scene scene, Vector3 position)`](../Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs#L215) | private static |
| ExplosionIntegrationChecks | [`void Configure(Block block, float damage, float radius)`](../Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs#L239) | private static |
| ExplosionIntegrationChecks | [`void Check(Report report, string name, bool passed)`](../Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs#L248) | private static |
| ExplosionIntegrationChecks | [`string Save(Report report)`](../Assets/Editor/Combat/Validation/ExplosionIntegrationChecks.cs#L258) | private static |

## Assets/Editor/Combat/Validation/NightInteractionChecks.cs

// 职责：通过真实Siege生命周期与跨帧InputSystem事件回归夜班交互、筛选和重开。 // 入口：RunLive()；只操作Play实例，结束恢复硬件与背景输入设置，不写玩家蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NightInteractionChecks | [`void RunLive()`](../Assets/Editor/Combat/Validation/NightInteractionChecks.cs#L32) | public static |
| NightInteractionChecks | [`IEnumerator Scenario()`](../Assets/Editor/Combat/Validation/NightInteractionChecks.cs#L70) | private static |
| NightInteractionChecks | [`void Check(string name, bool passed)`](../Assets/Editor/Combat/Validation/NightInteractionChecks.cs#L214) | private static |
| NightInteractionChecks | [`void Tick()`](../Assets/Editor/Combat/Validation/NightInteractionChecks.cs#L225) | private static |
| NightInteractionChecks | [`void Finish(string error)`](../Assets/Editor/Combat/Validation/NightInteractionChecks.cs#L253) | private static |

## Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs

// 职责：验证自动5v5场景持久配置、正式死亡归因、五秒倒计时与连续轮次清理。 // 入口：ValidateSaved()在Edit Mode执行；Begin()在Play启动检查，Pending保存实际结果。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SkirmishArenaChecks | [`string ValidateSaved()`](../Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs#L18) | public static |
| SkirmishArenaChecks | [`void Begin()`](../Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs#L80) | public static |
| SkirmishArenaChecks | [`Task<string> Run()`](../Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs#L85) | private static async |
| SkirmishArenaChecks | [`PlayManager PlayManagerReference(SkirmishMatch match)`](../Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs#L294) | private static |
| SkirmishArenaChecks | [`void FreezeCombat(SkirmishMatch match)`](../Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs#L299) | private static |
| SkirmishArenaChecks | [`Task Until(Func<bool> condition, float timeout)`](../Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs#L320) | private static async |
| SkirmishArenaChecks | [`void Check(List<string> checks, string label, bool condition)`](../Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs#L334) | private static |
| SkirmishArenaChecks | [`string Save(string name, List<string> checks)`](../Assets/Editor/Combat/Validation/SkirmishArenaChecks.cs#L343) | private static |

## Assets/Editor/Combat/Validation/SkirmishPerformanceChecks.cs

// 职责：在同一批舰船、镜头与冻结模拟下对照属性块染色和共享材质绘制成本。 // 入口：Begin()启动有限帧Profiler捕获；Status显示进度，退出/重载时恢复全部临时状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SkirmishPerformanceChecks | [`void Begin()`](../Assets/Editor/Combat/Validation/SkirmishPerformanceChecks.cs#L43) | public static |
| SkirmishPerformanceChecks | [`void StartWindow()`](../Assets/Editor/Combat/Validation/SkirmishPerformanceChecks.cs#L92) | private static |
| SkirmishPerformanceChecks | [`void Tick()`](../Assets/Editor/Combat/Validation/SkirmishPerformanceChecks.cs#L104) | private static |
| SkirmishPerformanceChecks | [`void RestoreProperties()`](../Assets/Editor/Combat/Validation/SkirmishPerformanceChecks.cs#L170) | private static |
| SkirmishPerformanceChecks | [`void Restore()`](../Assets/Editor/Combat/Validation/SkirmishPerformanceChecks.cs#L181) | private static |

## Assets/Editor/Combat/Validation/TurretFlightPlayProbe.cs

// 职责：验证真实引擎帧中的无目标空闲、自动申请供电、双管发射及有限飞行时间。 // 入口：Run() / RunCrossing()；在已进入的Play Mode隔离执行，输出JSON与飞行截图，不保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretFlightPlayProbe | [`void RunCrossing()`](../Assets/Editor/Combat/Validation/TurretFlightPlayProbe.cs#L40) | public static |
| TurretFlightPlayProbe | [`void Run()`](../Assets/Editor/Combat/Validation/TurretFlightPlayProbe.cs#L46) | public static |
| TurretFlightPlayProbe | [`ControlUnit Unit(string name, Vector3 position, UnitFaction faction)`](../Assets/Editor/Combat/Validation/TurretFlightPlayProbe.cs#L100) | private static |
| TurretFlightPlayProbe | [`void Tick()`](../Assets/Editor/Combat/Validation/TurretFlightPlayProbe.cs#L123) | private static |
| TurretFlightPlayProbe | [`void Check(string label, bool passed)`](../Assets/Editor/Combat/Validation/TurretFlightPlayProbe.cs#L208) | private static |
| TurretFlightPlayProbe | [`void Capture()`](../Assets/Editor/Combat/Validation/TurretFlightPlayProbe.cs#L213) | private static |
| TurretFlightPlayProbe | [`void Finish()`](../Assets/Editor/Combat/Validation/TurretFlightPlayProbe.cs#L237) | private static |

## Assets/Editor/Combat/Validation/TurretRuntimeChecks.cs

// 职责：在当前 Play Mode 隔离检查真实炮塔资产、瞬时激光、炮管数量及炮弹物理扫掠。 // 入口：RunInCurrentPlayMode()；不保存场景、不切换模式，清理本次创建的检查对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretRuntimeChecks | [`string RunInCurrentPlayMode()`](../Assets/Editor/Combat/Validation/TurretRuntimeChecks.cs#L23) | public static |
| TurretRuntimeChecks | [`void PrepareWeapon(Turret turret, ControlUnit owner)`](../Assets/Editor/Combat/Validation/TurretRuntimeChecks.cs#L263) | private static |
| TurretRuntimeChecks | [`void CallFire(Turret turret, Vector3 origin, Vector3 direction)`](../Assets/Editor/Combat/Validation/TurretRuntimeChecks.cs#L271) | private static |
| TurretRuntimeChecks | [`ControlUnit Unit(Transform parent, string name, UnitFaction faction, Vector3 localPosition)`](../Assets/Editor/Combat/Validation/TurretRuntimeChecks.cs#L276) | private static |
| TurretRuntimeChecks | [`void Check(Report report, bool passed, string name)`](../Assets/Editor/Combat/Validation/TurretRuntimeChecks.cs#L293) | private static |

## Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs

// 职责：采集固定舰队的CPU整帧原始样本；记录当前绘制配置，不在运行中切换渲染系统。 // 入口：StartCapture() / Status / Finish()；Play部署阶段，结果写Temp/FleetPresentation。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| FleetPresentationBenchmark | [`string CreateFixedFleet()`](../Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs#L31) | public static |
| FleetPresentationBenchmark | [`void SpawnFleet()`](../Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs#L42) | private static |
| FleetPresentationBenchmark | [`string StartCapture(string label = "fixed-fleet")`](../Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs#L62) | public static |
| FleetPresentationBenchmark | [`void Tick()`](../Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs#L96) | private static |
| FleetPresentationBenchmark | [`string Finish(string reason = "Manual stop")`](../Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs#L137) | public static |
| FleetPresentationBenchmark | [`void OnReload()`](../Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs#L153) | private static |
| FleetPresentationBenchmark | [`void OnPlayModeChanged(PlayModeStateChange state)`](../Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs#L158) | private static |
| FleetPresentationBenchmark | [`bool IsInitialized()`](../Assets/Editor/Diagnostics/Benchmarks/FleetPresentationBenchmark.cs#L166) | private static |

## Assets/Editor/Diagnostics/Benchmarks/IndustrialMotionChecks.cs

// 职责：隔离验证工业动画共享材质、独立属性、高亮恢复、重配及启停生命周期。 // 入口：Run()，仅Play Mode；输出Temp/IndustrialMotion/checks.txt。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IndustrialMotionChecks | [`string Run()`](../Assets/Editor/Diagnostics/Benchmarks/IndustrialMotionChecks.cs#L12) | public static |
| IndustrialMotionChecks | [`string RunConventional()`](../Assets/Editor/Diagnostics/Benchmarks/IndustrialMotionChecks.cs#L128) | public static |
| IndustrialMotionChecks | [`object CallBinding(string method, params object[] arguments)`](../Assets/Editor/Diagnostics/Benchmarks/IndustrialMotionChecks.cs#L173) | private static |
| IndustrialMotionChecks | [`Renderer Visual(Transform parent, string name, Material source)`](../Assets/Editor/Diagnostics/Benchmarks/IndustrialMotionChecks.cs#L178) | private static |
| IndustrialMotionChecks | [`void Configure(IndustrialPartMotion motion, Renderer renderer, Color color)`](../Assets/Editor/Diagnostics/Benchmarks/IndustrialMotionChecks.cs#L187) | private static |
| IndustrialMotionChecks | [`int ActiveCount()`](../Assets/Editor/Diagnostics/Benchmarks/IndustrialMotionChecks.cs#L201) | private static |
| IndustrialMotionChecks | [`void Check(bool condition, string description, List<string> checks)`](../Assets/Editor/Diagnostics/Benchmarks/IndustrialMotionChecks.cs#L206) | private static |

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

## Assets/Editor/Diagnostics/ConcaveTerrainChecks.cs

// 职责：验证真实非凸地形网格接触时避障与炮弹复用兼容最近点查询，避免Unity不支持API日志。 // 入口：Run()；Play模式隔离远端对象，恢复模拟开关，不写场景或玩家数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ConcaveTerrainChecks | [`string Run()`](../Assets/Editor/Diagnostics/ConcaveTerrainChecks.cs#L12) | public static |
| ConcaveTerrainChecks | [`void Require(bool value, string name, List<string> checks)`](../Assets/Editor/Diagnostics/ConcaveTerrainChecks.cs#L109) | private static |

## Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs

// 职责：在 Play Mode 检查沙盒物理、控制和损伤相关场景。 // 入口：Run()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PhysicsSandboxProbe | [`string Run()`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L14) | public static |
| PhysicsSandboxProbe | [`void CheckLethalImpactExplosion(Scene scene, List<string> checks, bool explosive)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L194) | private static |
| PhysicsSandboxProbe | [`void CheckBuildingImpactExplosion(Scene scene, List<string> checks)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L220) | private static |
| PhysicsSandboxProbe | [`int CountExplosionEffects()`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L250) | private static |
| PhysicsSandboxProbe | [`float Collide( Scene scene, float mass, float speed, List<string> checks, bool checkContacts, float restitution = 0f, float tangentSpeed = 0f)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L266) | private static |
| PhysicsSandboxProbe | [`void CollideDynamicPair(Scene scene, List<string> checks)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L340) | private static |
| PhysicsSandboxProbe | [`Rigidbody Body(Scene scene, string name, float mass, Vector3 position)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L382) | private static |
| PhysicsSandboxProbe | [`void PrepareSceneEffects(Scene scene)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L395) | internal static |
| PhysicsSandboxProbe | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L408) | private static |
| PhysicsSandboxProbe | [`string Save(List<string> checks, string error)`](../Assets/Editor/Diagnostics/PhysicsSandboxProbe.cs#L418) | private static |

## Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs

// 职责：解析 Profiler 捕获数据并输出性能分析结果。 // 入口：AnalyzeLatest() / Analyze() / Add()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProfilerCaptureAnalysis | [`void AnalyzeLatest()`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L19) | public static |
| ProfilerCaptureAnalysis | [`void Analyze(string capturePath, string reportPath)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L27) | public static |
| ProfilerCaptureAnalysis | [`string GetLatestCapturePath()`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L125) | private static |
| ProfilerCaptureAnalysis | [`void AddRawMarkers( RawFrameDataView raw, Dictionary<string, MarkerAggregate> aggregate, Dictionary<string, MarkerAggregate> gcMarkers, Dictionary<string, MarkerAggregate> scriptMarkers, FrameSummary summary)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L136) | private static |
| ProfilerCaptureAnalysis | [`MarkerAggregate GetOrCreate(Dictionary<string, MarkerAggregate> map, string name)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L168) | private static |
| ProfilerCaptureAnalysis | [`void WriteFrameStats(StringBuilder report, List<FrameSummary> frames)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L182) | private static |
| ProfilerCaptureAnalysis | [`void AddCounters(RawFrameDataView raw, Dictionary<string, CounterAggregate> counters)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L204) | private static |
| ProfilerCaptureAnalysis | [`void WriteCounters(StringBuilder report, Dictionary<string, CounterAggregate> counters, int frameCount)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L260) | private static |
| ProfilerCaptureAnalysis | [`void WriteMarkers(StringBuilder report, string title, IEnumerable<MarkerAggregate> markers, int count)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L276) | private static |
| ProfilerCaptureAnalysis | [`void WriteWorstFrames(StringBuilder report, List<FrameSummary> frames, int count)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L286) | private static |
| ProfilerCaptureAnalysis | [`void WriteHierarchyDrilldowns(StringBuilder report, IEnumerable<int> frames)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L296) | private static |
| ProfilerCaptureAnalysis | [`void WriteHierarchyItem(StringBuilder report, HierarchyFrameDataView view, int id, int depth, int maxDepth)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L320) | private static |
| ProfilerCaptureAnalysis | [`float Percentile(float[] sorted, int pct)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L340) | private static |
| ProfilerCaptureAnalysis | [`void WriteReport(string reportPath, StringBuilder report)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L351) | private static |
| ProfilerCaptureAnalysis | [`void Finish(string message)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L362) | private static |
| ProfilerCaptureAnalysis | [`string F(float value)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L371) | private static |
| ProfilerCaptureAnalysis | [`string F(double value)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L376) | private static |
| ProfilerCaptureAnalysis | [`string Pad(string value, int width)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L381) | private static |
| MarkerAggregate | [`void Add(float ms, int frame)`](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L395) | public |

## Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs

// 职责：验证引擎升级后的场景查询、VFX 制作桥接和受影响 FBX 材质迁移。 // 入口：CheckSceneQueries() / CheckVfxBridge() / MigrateExhaustImporter()；结果写 Logs/UpgradeFollowup。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| UpgradeCompatibilityChecks | [`object CheckInputLifecycle()`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L20) | public static |
| UpgradeCompatibilityChecks | [`object EnableVfxMeshReadability()`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L91) | public static |
| UpgradeCompatibilityChecks | [`string VfxMeshSnapshot(string path)`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L126) | private static |
| UpgradeCompatibilityChecks | [`object CheckSceneQueries()`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L131) | public static |
| UpgradeCompatibilityChecks | [`object CheckVfxBridge()`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L181) | public static |
| UpgradeCompatibilityChecks | [`object MigrateExhaustImporter()`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L227) | public static |
| UpgradeCompatibilityChecks | [`string ModelSnapshot(string path)`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L269) | private static |
| UpgradeCompatibilityChecks | [`object Reference(Object asset)`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L280) | private static |
| UpgradeCompatibilityChecks | [`void Require(bool condition, string name)`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L291) | private static |
| UpgradeCompatibilityChecks | [`object Write(string name, object result)`](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L299) | private static |

## Assets/Editor/Flight/ThrusterHudSetup.cs

// 职责：配置推进器参数 HUD 与交互引用。 // 入口：Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ThrusterHudSetup | [`void Apply()`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L19) | public static |
| ThrusterHudSetup | [`void SetupLamp()`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L65) | private static |
| ThrusterHudSetup | [`RectTransform Child(Transform parent, string name)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L130) | private static |
| ThrusterHudSetup | [`void Stretch(RectTransform rect)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L143) | private static |
| ThrusterHudSetup | [`void Box(RectTransform rect, Vector2 topLeft, Vector2 bottomRight)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L151) | private static |
| ThrusterHudSetup | [`Image Paint(RectTransform rect, Color color, bool raycast)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L159) | private static |
| ThrusterHudSetup | [`Text Label( Transform parent, string name, Font font, int size, Color color, TextAnchor alignment)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L172) | private static |
| ThrusterHudSetup | [`void Set(UnityEngine.Object target, string name, UnityEngine.Object value)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L196) | private static |
| ThrusterHudSetup | [`void SetArray(UnityEngine.Object target, string name, T[] values)`](../Assets/Editor/Flight/ThrusterHudSetup.cs#L203) | private static |

## Assets/Editor/MainMenu/MainMenuAuthoring.cs

// 职责：制作可编辑主菜单场景、预览相机和Main返回入口；重复执行保留已制作的菜单布局。 // 入口：Install()；通过Editor API保存真实资产，不在运行时拼装菜单UI。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainMenuAuthoring | [`void Install()`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L21) | public static |
| MainMenuAuthoring | [`Scene CreateMenu()`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L148) | private static |
| MainMenuAuthoring | [`void BuildPreview(Transform parent, RawImage image, MenuBlueprintPreview preview)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L441) | private static |
| MainMenuAuthoring | [`RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L476) | private static |
| MainMenuAuthoring | [`RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L483) | private static |
| MainMenuAuthoring | [`void Place(RectTransform rect, Transform parent, Vector2 position, Vector2 size)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L491) | private static |
| MainMenuAuthoring | [`Text Text( string name, Transform parent, string content, Vector2 position, Vector2 size, int fontSize, Color color)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L499) | private static |
| MainMenuAuthoring | [`Button Button(string name, Transform parent, string content, Vector2 position, Vector2 size)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L518) | private static |
| MainMenuAuthoring | [`Dropdown Dropdown(string name, Transform parent, Vector2 position, Vector2 size)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L540) | private static |
| MainMenuAuthoring | [`Slider Slider( string name, Transform parent, string label, Vector2 position, float minimum, float maximum)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L552) | private static |
| MainMenuAuthoring | [`void StyleFonts(GameObject root, int size)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L577) | private static |
| MainMenuAuthoring | [`void Bind(Object owner, string field, Object value)`](../Assets/Editor/MainMenu/MainMenuAuthoring.cs#L587) | private static |

## Assets/Editor/MainMenu/MainMenuPlayChecks.cs

// 职责：真实Play验证冷启动、菜单导航、临时载具隔离、基地选择和保存失败保护。 // 入口：Run()；存档注入临时目录，偏好备份恢复，报告及截图仅写Temp/MainMenu。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HistoryProbe | [`void Undo()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L36) | public |
| HistoryProbe | [`void Redo()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L41) | public |
| MainMenuPlayChecks | [`void Run()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L60) | public static |
| MainMenuPlayChecks | [`void Changed(PlayModeStateChange state)`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L72) | private static |
| MainMenuPlayChecks | [`void Tick()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L92) | private static |
| MainMenuPlayChecks | [`IEnumerator Scenario()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L118) | private static |
| MainMenuPlayChecks | [`IEnumerator SurfacePlacement()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L799) | private static |
| MainMenuPlayChecks | [`IEnumerator EscapeAttract(MainMenuController menu)`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L876) | private static |
| MainMenuPlayChecks | [`void PointerClick(Selectable control)`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L926) | private static |
| MainMenuPlayChecks | [`void Select(MainMenuController menu, string name)`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L943) | private static |
| MainMenuPlayChecks | [`BlockDataList Vehicle()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L950) | private static |
| MainMenuPlayChecks | [`void InjectStore(string field, string directory)`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L973) | private static |
| MainMenuPlayChecks | [`int ActiveEvents()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L979) | private static |
| MainMenuPlayChecks | [`int ActiveScreenCameras()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L984) | private static |
| MainMenuPlayChecks | [`void Check(bool condition, string message)`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L989) | private static |
| MainMenuPlayChecks | [`void LogError(string message, string trace, LogType type)`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L999) | private static |
| MainMenuPlayChecks | [`void BackupPreferences()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L1007) | private static |
| MainMenuPlayChecks | [`void Finish()`](../Assets/Editor/MainMenu/MainMenuPlayChecks.cs#L1028) | private static |

## Assets/Editor/MainMenu/NightMenuAuthoring.cs

// 职责：为现有可编辑菜单增补设置导航和建造偏好，保留已有页面内容。 // 入口：InstallPreferences()；幂等制作并保存主菜单场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NightMenuAuthoring | [`void PolishPauseCard()`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L13) | public static |
| NightMenuAuthoring | [`void PauseLabel( Transform parent, string name, string value, float y, int size, Color color)`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L95) | private static |
| NightMenuAuthoring | [`void FitMenuCanvas()`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L114) | public static |
| NightMenuAuthoring | [`void InstallAttractEntry()`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L135) | public static |
| NightMenuAuthoring | [`void InstallPause()`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L166) | public static |
| NightMenuAuthoring | [`void InstallSiegeRestart()`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L242) | private static |
| NightMenuAuthoring | [`void InstallPreferences()`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L279) | public static |
| NightMenuAuthoring | [`void StylePreference(Toggle toggle)`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L358) | private static |
| NightMenuAuthoring | [`RectTransform Page(string name, Transform parent)`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L389) | private static |
| NightMenuAuthoring | [`Button Tab(Button template, Transform parent, string name, Vector2 position)`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L400) | private static |
| NightMenuAuthoring | [`void Place(RectTransform rect, Transform parent, Vector2 position, Vector2 size)`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L410) | private static |
| NightMenuAuthoring | [`void Bind(SerializedObject data, string name, UnityEngine.Object value)`](../Assets/Editor/MainMenu/NightMenuAuthoring.cs#L418) | private static |

## Assets/Editor/MainMenu/WorkshopLayoutPreview.cs

// 职责：以保存的真实Canvas制作工坊、暂停及设置页多分辨率预览，检查可见交互区域。 // 入口：Render()；独立PreviewScene，禁止运行态写回，不初始化个人存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorkshopLayoutPreview | [`string Render()`](../Assets/Editor/MainMenu/WorkshopLayoutPreview.cs#L15) | public static |
| WorkshopLayoutPreview | [`void RenderOne(Canvas source, string page, Vector2Int size)`](../Assets/Editor/MainMenu/WorkshopLayoutPreview.cs#L81) | private static |

## Assets/Editor/Navigation/Validation/EnemyPredictionPlayChecks.cs

// 职责：用隔离物理场景验证横向运动感知、相对碰撞时间和候选动作预算。 // 入口：Run()；仅Play Mode，真实PhysX采集，反射调用生产预测器，不修改试验场。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyPredictionPlayChecks | [`string Run()`](../Assets/Editor/Navigation/Validation/EnemyPredictionPlayChecks.cs#L13) | public static |
| EnemyPredictionPlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Navigation/Validation/EnemyPredictionPlayChecks.cs#L279) | private static |

## Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs

// 职责：在隔离物理场景验证稳定局部驾驶、爬升净空与两种炮塔的遮挡锁定。 // 入口：Run()；仅 Play Mode，结果写入 Temp/NavigationCombat，不修改玩家蓝图和场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationCombatPlayChecks | [`void Begin()`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L20) | public static |
| NavigationCombatPlayChecks | [`Task<string> Run()`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L25) | public static |
| NavigationCombatPlayChecks | [`void CheckStableSteering(List<string> checks, EnemyController ai, ControlUnit owner)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L161) | private static |
| NavigationCombatPlayChecks | [`void CheckContinuousCourse(List<string> checks, EnemyController ai)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L201) | private static |
| NavigationCombatPlayChecks | [`void CheckHullAvoidance(List<string> checks, Transform root, ControlUnit owner, EnemyController ai)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L241) | private static |
| NavigationCombatPlayChecks | [`void CheckObstacleClimb(List<string> checks, Transform root, ControlUnit owner, BoxCollider wall)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L297) | private static |
| NavigationCombatPlayChecks | [`void CheckDescentRetention(List<string> checks, ControlUnit owner, EnemyController ai)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L406) | private static |
| NavigationCombatPlayChecks | [`void CheckLocalPath(List<string> checks, Transform root, ControlUnit owner)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L432) | private static |
| NavigationCombatPlayChecks | [`void CheckMotionPrediction(List<string> checks, Transform root, ControlUnit owner, Type type, object planner)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L551) | private static |
| NavigationCombatPlayChecks | [`ControlUnit Unit(Transform root, string name, UnitFaction faction, Vector3 position)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L592) | private static |
| NavigationCombatPlayChecks | [`BoxCollider Box(Transform root, string name, Vector3 position, Vector3 size)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L612) | private static |
| NavigationCombatPlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs#L622) | private static |

## Assets/Editor/Navigation/Validation/SharedNavigationPlayChecks.cs

// 职责：在隔离 PhysX 场景验证 Bot/舰船共用避障的形状、过滤、查询预算和状态隔离。 // 入口：Run()；Play Mode 执行，结果写入 Temp/NavigationCombat/shared-checks.txt。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SharedNavigationPlayChecks | [`string Run()`](../Assets/Editor/Navigation/Validation/SharedNavigationPlayChecks.cs#L12) | public static |
| SharedNavigationPlayChecks | [`GameObject Box(Transform parent, string name, Vector3 position, Vector3 size)`](../Assets/Editor/Navigation/Validation/SharedNavigationPlayChecks.cs#L151) | private static |
| SharedNavigationPlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Navigation/Validation/SharedNavigationPlayChecks.cs#L160) | private static |

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

## Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs

// 职责：从独立战斗服务场景制作可编辑的待机空战舞台、出生点、烟火点和镜头轨道。 // 入口：Create()；已有场景拒绝覆盖，源5v5和用户蓝图均保持原样。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BattleAttractAuthoring | [`void Create()`](../Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs#L15) | public static |
| BattleAttractAuthoring | [`void PolishSavedScene()`](../Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs#L204) | public static |
| BattleAttractAuthoring | [`void Polish(Scene scene)`](../Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs#L224) | private static |
| BattleAttractAuthoring | [`void RepairStatusOverlay(Scene scene)`](../Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs#L300) | private static |
| BattleAttractAuthoring | [`Transform Point(Transform parent, string name, Vector3 position)`](../Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs#L323) | private static |
| BattleAttractAuthoring | [`void Ref(SerializedObject target, string name, Object value)`](../Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs#L331) | private static |
| BattleAttractAuthoring | [`void Bar(Transform parent, string name, bool top)`](../Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs#L336) | private static |

## Assets/Editor/Presentation/Camera/SceneCameraPreviewSync.cs

// 职责：同步 Scene 视图与游戏镜头预览，供编辑器检查。 // 入口：Toggle() / SyncNow()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SceneCameraPreviewSync | [`void Toggle()`](../Assets/Editor/Presentation/Camera/SceneCameraPreviewSync.cs#L21) | public static |
| SceneCameraPreviewSync | [`bool ValidateToggle()`](../Assets/Editor/Presentation/Camera/SceneCameraPreviewSync.cs#L33) | private static |
| SceneCameraPreviewSync | [`void Update()`](../Assets/Editor/Presentation/Camera/SceneCameraPreviewSync.cs#L40) | private static |
| SceneCameraPreviewSync | [`bool SyncNow()`](../Assets/Editor/Presentation/Camera/SceneCameraPreviewSync.cs#L48) | public static |

## Assets/Editor/Presentation/Effects/BlockVfxBaker.cs

// 职责：生成并保存方块效果所需的可编辑 VFX 资产。 // 入口：GraphName() / Continuous() / Load()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockVfxBaker | [`string GraphName(string name)`](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L31) | public static |
| BlockVfxBaker | [`bool Continuous(string name)`](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L62) | public static |
| BlockVfxBaker | [`GameObject Load(string name)`](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L63) | public static |
| BlockVfxBaker | [`VfxEffect Populate(GameObject root, string name, bool automatic = false)`](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L64) | public static |
| BlockVfxBaker | [`VfxEffect PopulateTemplate(GameObject root, string name, bool automatic = false)`](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L74) | public static |
| BlockVfxBaker | [`void BakeMissing()`](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L119) | public static |
| BlockVfxBaker | [`void SetObjects(Object target, string field, Object[] values)`](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L187) | public static |
| BlockVfxBaker | [`void SetObject(Object target, string field, Object value)`](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L202) | public static |

## Assets/Editor/Presentation/Effects/CombatVfxBaker.cs

// 职责：制作可编辑残骸烟迹、分层激光和炮弹曳光 Graph/Prefab，并接入集中库。 // 入口：BakeMissing()；已有图和模板不覆盖，调参在 VFX Graph / Prefab Inspector。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatVfxBaker | [`void BakeMissing()`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L15) | public static |
| CombatVfxBaker | [`void Register(BlockVfxLibrary library, string id)`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L54) | private static |
| CombatVfxBaker | [`void CreatePrefab(string name, string graphName, float glow)`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L80) | private static |
| CombatVfxBaker | [`void BuildGraph(string name, bool smoke)`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L114) | private static |
| CombatVfxBaker | [`void BuildSmoke(object graph, object init, object output)`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L163) | private static |
| CombatVfxBaker | [`void BuildResidualFire(object graph, object update)`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L198) | private static |
| CombatVfxBaker | [`void Curve(object output, string attribute, AnimationCurve curve)`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L236) | private static |
| CombatVfxBaker | [`void BuildBeam(object graph, object init, object update)`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L244) | private static |
| CombatVfxBaker | [`Mesh CreateBeamMesh()`](../Assets/Editor/Presentation/Effects/CombatVfxBaker.cs#L275) | private static |

## Assets/Editor/Presentation/Effects/DebrisFireBaker.cs

// 职责：制作随残骸模型移动的局部空间包裹火焰，组合进集中库烟迹模板。 // 入口：BakeMissing()；已有图和 Prefab 子节点保留，可在 Graph 中继续调参。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DebrisFireBaker | [`void BakeMissing()`](../Assets/Editor/Presentation/Effects/DebrisFireBaker.cs#L11) | public static |
| DebrisFireBaker | [`void BuildSmokeVeil(object graph)`](../Assets/Editor/Presentation/Effects/DebrisFireBaker.cs#L94) | private static |
| DebrisFireBaker | [`void Curve(object output, string name, AnimationCurve curve)`](../Assets/Editor/Presentation/Effects/DebrisFireBaker.cs#L120) | private static |

## Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs

// 职责：制作敌机出生传送门的程序化纹理、原生 GPU Graph 和可编辑 Prefab，接入集中播放库。 // 入口：BakeMissing()；只补缺失资源，保留已有 Graph、Prefab 和库选择。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemySpawnPortalBaker | [`void BakeMissing()`](../Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs#L17) | public static |
| EnemySpawnPortalBaker | [`void RebuildTexture()`](../Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs#L58) | public static |
| EnemySpawnPortalBaker | [`void CreateTexture(bool overwrite = false)`](../Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs#L69) | private static |
| EnemySpawnPortalBaker | [`void CreateGraph()`](../Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs#L120) | private static |
| EnemySpawnPortalBaker | [`void Curve(object output, string attribute, AnimationCurve curve)`](../Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs#L173) | private static |
| EnemySpawnPortalBaker | [`void CreatePrefab()`](../Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs#L181) | private static |

## Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs

// 职责：建立原生 VFX Graph 资源库并配置效果参数。 // 入口：Destination() / GraphPath() / RebuildPositionSafeEffects()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NativeVfxLibraryBuilder | [`string Destination(string source)`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L30) | public static |
| NativeVfxLibraryBuilder | [`string GraphPath(string name)`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L36) | public static |
| NativeVfxLibraryBuilder | [`void RebuildPositionSafeEffects()`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L39) | public static |
| NativeVfxLibraryBuilder | [`void SetSimulationSpace(object graph, string space)`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L147) | private static |
| NativeVfxLibraryBuilder | [`void PrepareSources()`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L156) | public static |
| NativeVfxLibraryBuilder | [`void ImportSources()`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L176) | public static |
| NativeVfxLibraryBuilder | [`void CreateUtilityGraphs()`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L275) | public static |
| NativeVfxLibraryBuilder | [`void BuildUtility(string name, bool loop, bool world, bool beam)`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L285) | private static |
| NativeVfxLibraryBuilder | [`string Hash(string path)`](../Assets/Editor/Presentation/Effects/NativeVfxLibraryBuilder.cs#L381) | private static |

## Assets/Editor/Presentation/Effects/NativeVfxMigration.cs

// 职责：迁移旧特效引用到原生 VFX Graph 并检查依赖。 // 入口：Run() / Convert() / Validate()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NativeVfxMigration | [`void Run()`](../Assets/Editor/Presentation/Effects/NativeVfxMigration.cs#L16) | public static |
| NativeVfxMigration | [`void Convert(GameObject root)`](../Assets/Editor/Presentation/Effects/NativeVfxMigration.cs#L48) | public static |
| NativeVfxMigration | [`void RemoveParticles(GameObject root)`](../Assets/Editor/Presentation/Effects/NativeVfxMigration.cs#L127) | private static |
| NativeVfxMigration | [`string Validate()`](../Assets/Editor/Presentation/Effects/NativeVfxMigration.cs#L140) | public static |

## Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs

// 职责：构建与配置原生 VFX Graph 节点和参数。 // 入口：Type() / Get() / Set()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxGraphAuthoring | [`Type Type(string name)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L17) | public static |
| VfxGraphAuthoring | [`PropertyInfo Property(object target, string name)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L18) | private static |
| VfxGraphAuthoring | [`object Get(object target, string name)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L32) | public static |
| VfxGraphAuthoring | [`void Set(object target, string name, object value)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L33) | public static |
| VfxGraphAuthoring | [`object Call(object target, string method, params object[] args)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L54) | public static |
| VfxGraphAuthoring | [`object[] Items(object target, string property)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L69) | public static |
| VfxGraphAuthoring | [`object Resource(string path)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L70) | public static |
| VfxGraphAuthoring | [`object Graph(string path)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L71) | public static |
| VfxGraphAuthoring | [`Object[] Contents(string path)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L78) | public static |
| VfxGraphAuthoring | [`void Save(string path)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L79) | public static |
| VfxGraphAuthoring | [`object Create(string name)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L87) | public static |
| VfxGraphAuthoring | [`void Add(object parent, object child)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L88) | public static |
| VfxGraphAuthoring | [`void Setting(object target, string fieldName, object value)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L89) | public static |
| VfxGraphAuthoring | [`object Slot(object target, string name)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L110) | public static |
| VfxGraphAuthoring | [`void Value(object target, string name, object value)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L111) | public static |
| VfxGraphAuthoring | [`object Parameter(object graph, string name, object value)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L112) | public static |
| VfxGraphAuthoring | [`void Link(object parameter, object input)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L129) | public static |
| VfxGraphAuthoring | [`object Attribute(object context, string name, object value, bool multiply = false)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L130) | public static |
| VfxGraphAuthoring | [`void AddIntensity(string path)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L144) | public static |
| VfxGraphAuthoring | [`object Describe(string path)`](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L162) | public static |

## Assets/Editor/Presentation/Interface/BlockStatusSetup.cs

// 职责：配置方块状态灯/图标的可编辑资源。 // 入口：Apply()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockStatusSetup | [`void Apply()`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L23) | public static |
| BlockStatusSetup | [`int ConfigureBlockPrefabs()`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L32) | private static |
| BlockStatusSetup | [`List<Status> BuildStatusList()`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L68) | private static |
| BlockStatusSetup | [`void ConfigureMainScene()`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L86) | private static |
| BlockStatusSetup | [`void ConfigureBlueprintPanel(BlueprintUIPanel panel)`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L121) | private static |
| BlockStatusSetup | [`Text GetOrCreateText(Transform parent, string objectName, Text template, string initialText)`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L132) | private static |
| BlockStatusSetup | [`void ConfigureDebugPanel(MainUIButtons mainButtons)`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L149) | private static |
| BlockStatusSetup | [`Button GetOrCreateButton(Transform panel, Transform rowTemplate, string rowName, string label)`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L189) | private static |
| BlockStatusSetup | [`T FindComponentInScene(Scene scene)`](../Assets/Editor/Presentation/Interface/BlockStatusSetup.cs#L213) | private static |

## Assets/Editor/Presentation/Interface/DropdownTemplateAuthoring.cs

// 职责：统一自有uGUI下拉项的行高、文字留白和对比度，保存可继续编辑的模板。 // 入口：Apply() / UpgradeAll()；制作工具重用相同规则，避免重新生成后退回默认20px行高。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DropdownTemplateAuthoring | [`void Apply(Dropdown dropdown)`](../Assets/Editor/Presentation/Interface/DropdownTemplateAuthoring.cs#L13) | public static |
| DropdownTemplateAuthoring | [`void UpgradeAll()`](../Assets/Editor/Presentation/Interface/DropdownTemplateAuthoring.cs#L65) | public static |

## Assets/Editor/Presentation/Interface/ReturnStatusSetup.cs

// 职责：只在Main创建/绑定可编辑返航失败状态栏，不重排既有面板。 // 入口：Apply()；保存真实场景，保留已有状态栏的布局与样式。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ReturnStatusSetup | [`void Apply()`](../Assets/Editor/Presentation/Interface/ReturnStatusSetup.cs#L11) | public static |

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

## Assets/Editor/Presentation/Resources/BlockVfxLibraryEditor.cs

// 职责：用玩法名称展示现有播放槽位并检查配置，保留原序列化字段及 GUID。 // 入口：BlockVfxLibrary Inspector；可从资源管理窗口直接编辑，支持 Undo。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockVfxLibraryEditor | [`void OnInspectorGUI()`](../Assets/Editor/Presentation/Resources/BlockVfxLibraryEditor.cs#L10) | public override |
| BlockVfxLibraryEditor | [`void DrawReference(SerializedProperty property, string label)`](../Assets/Editor/Presentation/Resources/BlockVfxLibraryEditor.cs#L60) | private static |

## Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs

// 职责：为特效和音频管理共用资源发现、搜索及引用查询，不持有运行时资源。 // 入口：Scan() / Matches() / FindReferencers()；仅在显式刷新或项目资源变更时扫描。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PresentationAssetIndex | [`List<Entry> Scan(Kind kind, bool includeDependencies)`](../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L37) | public static |
| PresentationAssetIndex | [`bool Matches(Entry entry, string search, string category)`](../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L83) | public static |
| PresentationAssetIndex | [`string[] FindReferencers(string path)`](../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L88) | public static |

## Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs

// 职责：统一浏览特效/音频资产，编辑原始配置并按需检查引用；不生成副本、不改场景。 // 入口：Tools/HY Sandbox/资源管理/特效与音频；关闭/重载释放内嵌 Inspector。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PresentationResourceWindow | [`void Open()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L25) | public static |
| PresentationResourceWindow | [`void OnEnable()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L31) | private |
| PresentationResourceWindow | [`void OnDisable()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L38) | private |
| PresentationResourceWindow | [`void Invalidate()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L44) | private |
| PresentationResourceWindow | [`void ReleaseInspector()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L51) | private |
| PresentationResourceWindow | [`void OnGUI()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L59) | private |
| PresentationResourceWindow | [`void DrawToolbar()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L76) | private |
| PresentationResourceWindow | [`void DrawList()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L103) | private |
| PresentationResourceWindow | [`void SelectAsset(string guid)`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L133) | private |
| PresentationResourceWindow | [`void DrawDetails()`](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L149) | private |

## Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs

// 职责：将项目 Prefab 中的内嵌效果迁移为集中库编号，保留挂载变换和现有组件身份。 // 入口：Run()；通过 Unity API 保存，重复执行跳过已迁移节点，差异外观保存为独立模板。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxCatalogMigration | [`void Run()`](../Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs#L18) | public static |
| VfxCatalogMigration | [`bool IsBuiltIn(string key)`](../Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs#L135) | private static |
| VfxCatalogMigration | [`VfxEffect CreateTemplate(VfxEffect source, string key)`](../Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs#L151) | private static |
| VfxCatalogMigration | [`string Signature(VfxEffect effect)`](../Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs#L177) | public static |
| VfxCatalogMigration | [`JObject ComponentData(Component component)`](../Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs#L198) | private static |

## Assets/Editor/Presentation/Validation/CombatVfxCapture.cs

// 职责：在隔离 Play 场景自动拍摄残骸尾烟、激光和弹道，记录销毁后烟雾存活证据。 // 入口：StartCapture()；完成/超时恢复原场景，释放截图资源，不保存用户场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatVfxCapture | [`void StartCapture()`](../Assets/Editor/Presentation/Validation/CombatVfxCapture.cs#L45) | public static |
| CombatVfxCapture | [`void StartEnvelopeCapture()`](../Assets/Editor/Presentation/Validation/CombatVfxCapture.cs#L67) | public static |
| CombatVfxCapture | [`void State(PlayModeStateChange state)`](../Assets/Editor/Presentation/Validation/CombatVfxCapture.cs#L73) | private static |
| CombatVfxCapture | [`void Setup()`](../Assets/Editor/Presentation/Validation/CombatVfxCapture.cs#L97) | private static |
| CombatVfxCapture | [`void Tick()`](../Assets/Editor/Presentation/Validation/CombatVfxCapture.cs#L157) | private static |
| CombatVfxCapture | [`void Capture(float elapsed)`](../Assets/Editor/Presentation/Validation/CombatVfxCapture.cs#L198) | private static |
| CombatVfxCapture | [`void Finish()`](../Assets/Editor/Presentation/Validation/CombatVfxCapture.cs#L232) | private static |

## Assets/Editor/Presentation/Validation/CombatVfxChecks.cs

// 职责：验证战斗特效资源与真实炮弹池复用的表现状态，不改变伤害/速度规则。 // 入口：RunPlay()；在隔离 Play 场景同步检查，finally 清理本次炮弹池。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatVfxChecks | [`string RunPlay()`](../Assets/Editor/Presentation/Validation/CombatVfxChecks.cs#L11) | public static |
| CombatVfxChecks | [`void Require(bool condition, string message)`](../Assets/Editor/Presentation/Validation/CombatVfxChecks.cs#L72) | private static |

## Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs

// 职责：在 Play Mode 检查原生 VFX 播放、参数和停止生命周期。 // 入口：RunSuite() / Run() / RunPositionTest()。 // 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NativeVfxPlaybackProbe | [`void NextQueued()`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L47) | private static |
| NativeVfxPlaybackProbe | [`void RunSuite()`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L67) | public static |
| NativeVfxPlaybackProbe | [`void RunSuite(string outputRoot)`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L73) | public static |
| NativeVfxPlaybackProbe | [`void Run(string prefab, string output)`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L82) | public static |
| NativeVfxPlaybackProbe | [`void RunPositionTest(string prefab, string output)`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L89) | public static |
| NativeVfxPlaybackProbe | [`void RunBlockTest(string prefab, string output)`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L96) | public static |
| NativeVfxPlaybackProbe | [`void StartCapture(string prefab, string output)`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L103) | private static |
| NativeVfxPlaybackProbe | [`void State(PlayModeStateChange state)`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L118) | private static |
| NativeVfxPlaybackProbe | [`void Tick()`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L233) | private static |
| NativeVfxPlaybackProbe | [`void Finish()`](../Assets/Editor/Presentation/Validation/NativeVfxPlaybackProbe.cs#L323) | private static |

## Assets/Editor/Presentation/Validation/PresentationResourceChecks.cs

// 职责：在真实 AssetDatabase 中验证特效/音频共享发现、过滤、引用与窗口生命周期。 // 入口：Run()；只读既有资源，临时验证资产在 finally 删除。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PresentationResourceChecks | [`string Run()`](../Assets/Editor/Presentation/Validation/PresentationResourceChecks.cs#L10) | public static |
| PresentationResourceChecks | [`void CheckLibraryEditing(string folder)`](../Assets/Editor/Presentation/Validation/PresentationResourceChecks.cs#L73) | private static |
| PresentationResourceChecks | [`void Require(bool condition, string message)`](../Assets/Editor/Presentation/Validation/PresentationResourceChecks.cs#L106) | private static |

## Assets/Editor/Presentation/Validation/VfxCatalogChecks.cs

// 职责：验证集中配置的覆盖、持久引用和真实 Play 挂载/替换/停止生命周期。 // 入口：CheckAssets() / CheckPlay()；Play 检查只创建隔离夹具，finally 还原内存配置并清理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxCatalogChecks | [`string CheckPreservation(string baselineRoot)`](../Assets/Editor/Presentation/Validation/VfxCatalogChecks.cs#L13) | public static |
| VfxCatalogChecks | [`string CheckAssets()`](../Assets/Editor/Presentation/Validation/VfxCatalogChecks.cs#L45) | public static |
| VfxCatalogChecks | [`string CheckPlay()`](../Assets/Editor/Presentation/Validation/VfxCatalogChecks.cs#L78) | public static |
| VfxCatalogChecks | [`VfxEffect LoadAnchor(string path, string id, List<GameObject> fixtures)`](../Assets/Editor/Presentation/Validation/VfxCatalogChecks.cs#L144) | private static |
| VfxCatalogChecks | [`void Require(bool value, string message)`](../Assets/Editor/Presentation/Validation/VfxCatalogChecks.cs#L152) | private static |

## Assets/Editor/Presentation/Validation/VfxPoolBudgetChecks.cs

// 职责：用真实VFX模板验证显式活动预算、复用、销毁及清场后容量恢复。 // 入口：RunPlay()；独立临时场景，退出恢复原活动场景，不修改生产资源。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxPoolBudgetChecks | [`string RunPlay()`](../Assets/Editor/Presentation/Validation/VfxPoolBudgetChecks.cs#L15) | public static |
| VfxPoolBudgetChecks | [`void Configure(SerializedObject settings, int total, int perTemplate)`](../Assets/Editor/Presentation/Validation/VfxPoolBudgetChecks.cs#L102) | private static |
| VfxPoolBudgetChecks | [`void Require(List<string> checks, string label, bool passed)`](../Assets/Editor/Presentation/Validation/VfxPoolBudgetChecks.cs#L110) | private static |

## Assets/Editor/Presentation/Validation/VfxPoolChecks.cs

// 职责：验证 GPU 特效峰值扩容、模板复位和旧尾烟名额限制的移除。 // 入口：RunPlay()；仅在隔离 Play 场景执行，销毁本次创建的池和刚体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxPoolChecks | [`string RunPlay()`](../Assets/Editor/Presentation/Validation/VfxPoolChecks.cs#L12) | public static |
| VfxPoolChecks | [`void CreatePool()`](../Assets/Editor/Presentation/Validation/VfxPoolChecks.cs#L128) | private static |
| VfxPoolChecks | [`void Require(bool condition, string message)`](../Assets/Editor/Presentation/Validation/VfxPoolChecks.cs#L138) | private static |

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
| SalvagePlayProbe | [`void Capture(string condition, string stack, LogType type)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L108) | private static |
| SalvagePlayProbe | [`void Tick()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L116) | private static |
| SalvagePlayProbe | [`void Finish()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L146) | private static |
| SalvagePlayProbe | [`void Check(string name, bool result)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L173) | private static |
| SalvagePlayProbe | [`ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool cockpit = true)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L185) | private static |
| SalvagePlayProbe | [`GameObject Spawn(string name, ControlUnit unit, Vector3 local)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L212) | private static |
| SalvagePlayProbe | [`CargoItem Coins(int count)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L227) | private static |
| SalvagePlayProbe | [`CargoItem Part()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L232) | private static |
| SalvagePlayProbe | [`BuildingRuntime SalvageBuilding(WorldBuildingManager manager, UnitFaction faction, bool withBot = false)`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L238) | private static |
| SalvagePlayProbe | [`IEnumerator BuildingSalvage()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L282) | private static |
| SalvagePlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/Salvage/SalvagePlayProbe.cs#L432) | private static |

## Assets/Editor/Siege/Authoring/SiegeArenaLookAuthoring.cs

// 职责：为阵地战场景建立夜色竞技场外观：程序化天空盒、雾色、环境光与主光，并保持可编辑资产。 // 入口：ApplyLook()；只改Siege场景与其天空盒材质，复用Main既有色调默认值，不改Main场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeArenaLookAuthoring | [`void ApplyLook()`](../Assets/Editor/Siege/Authoring/SiegeArenaLookAuthoring.cs#L18) | public static |
| SiegeArenaLookAuthoring | [`void Pylons()`](../Assets/Editor/Siege/Authoring/SiegeArenaLookAuthoring.cs#L111) | private static |
| SiegeArenaLookAuthoring | [`Material Material(string name, Color color, Color? emission)`](../Assets/Editor/Siege/Authoring/SiegeArenaLookAuthoring.cs#L157) | private static |

## Assets/Editor/Siege/Authoring/SiegeBaseBlueprintAuthoring.cs

// 职责：制作32×32m建筑蓝图基地，并通过Editor迁移已有Siege场景的建筑管理器/核心配置。 // 入口：UpgradeScene() / Generate()；写入真实资产，保留已有GUID，不修改Main或用户建筑目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBaseBlueprintAuthoring | [`void UpgradeScene()`](../Assets/Editor/Siege/Authoring/SiegeBaseBlueprintAuthoring.cs#L14) | public static |
| SiegeBaseBlueprintAuthoring | [`void ConfigureManager(WorldBuildingManager buildings)`](../Assets/Editor/Siege/Authoring/SiegeBaseBlueprintAuthoring.cs#L55) | internal static |
| SiegeBaseBlueprintAuthoring | [`TextAsset Generate(bool replace = false)`](../Assets/Editor/Siege/Authoring/SiegeBaseBlueprintAuthoring.cs#L62) | public static |
| SiegeBaseBlueprintAuthoring | [`void Add(BlockDataList blueprint, string resource, Vector3 position, string id = null)`](../Assets/Editor/Siege/Authoring/SiegeBaseBlueprintAuthoring.cs#L153) | private static |

## Assets/Editor/Siege/Authoring/SiegeBlueprintSelectionAuthoring.cs

// 职责：保留旧蓝图选择菜单并委托当前界面迁移，避免恢复固定Bays布局。 // 入口：Install()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBlueprintSelectionAuthoring | [`void Install()`](../Assets/Editor/Siege/Authoring/SiegeBlueprintSelectionAuthoring.cs#L7) | public static |

## Assets/Editor/Siege/Authoring/SiegeCleanupAuthoring.cs

// 职责：制作扫描线、回收粒子Prefab及原生HUD，将真实组件引用保存到全部Siege宿主。 // 入口：Install()/Configure()；独立备份场景，不覆盖已有手调特效参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCleanupAuthoring | [`string Install()`](../Assets/Editor/Siege/Authoring/SiegeCleanupAuthoring.cs#L16) | public static |
| SiegeCleanupAuthoring | [`void Configure(Scene scene)`](../Assets/Editor/Siege/Authoring/SiegeCleanupAuthoring.cs#L62) | public static |
| SiegeCleanupAuthoring | [`LineRenderer Line( Transform parent, string name, Material material, int count, float width, bool loop)`](../Assets/Editor/Siege/Authoring/SiegeCleanupAuthoring.cs#L126) | private static |
| SiegeCleanupAuthoring | [`Material Surface()`](../Assets/Editor/Siege/Authoring/SiegeCleanupAuthoring.cs#L149) | private static |
| SiegeCleanupAuthoring | [`MaterialBreakEffect Effect(Material material)`](../Assets/Editor/Siege/Authoring/SiegeCleanupAuthoring.cs#L170) | private static |

## Assets/Editor/Siege/Authoring/SiegeCombatBaseAuthoring.cs

// 职责：制作独立的交错防线实战基地蓝图，保留示例基地与用户设计。 // 入口：Generate()；固定配方可重复预览，输出真实JSON资产，个人副本通过正常菜单出战。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCombatBaseAuthoring | [`void Create()`](../Assets/Editor/Siege/Authoring/SiegeCombatBaseAuthoring.cs#L12) | public static |
| SiegeCombatBaseAuthoring | [`TextAsset Generate()`](../Assets/Editor/Siege/Authoring/SiegeCombatBaseAuthoring.cs#L18) | public static |
| SiegeCombatBaseAuthoring | [`void RaiseHangars()`](../Assets/Editor/Siege/Authoring/SiegeCombatBaseAuthoring.cs#L187) | public static |
| SiegeCombatBaseAuthoring | [`void Armor(BlockDataList design, Vector3 position, Vector3Int size)`](../Assets/Editor/Siege/Authoring/SiegeCombatBaseAuthoring.cs#L232) | private static |
| SiegeCombatBaseAuthoring | [`void Add( BlockDataList design, string resource, Vector3 position, Vector3Int? size = null, float yaw = 0f, string id = null)`](../Assets/Editor/Siege/Authoring/SiegeCombatBaseAuthoring.cs#L269) | private static |

## Assets/Editor/Siege/Authoring/SiegeCommandHudAuthoring.cs

// 职责：保留旧菜单入口，统一委托当前Siege界面制作工具，避免重新生成已移除的Bays。 // 入口：Apply()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCommandHudAuthoring | [`void Apply()`](../Assets/Editor/Siege/Authoring/SiegeCommandHudAuthoring.cs#L7) | public static |

## Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs

// 职责：保存新版基地战配置、Main编辑入口及副场景uGUI/建筑服务；保留旧Siege和Main手工布局。 // 入口：Install()；重建Base Workshop，战斗界面交给共享HUD制作器，不恢复已淘汰的施工面板。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeConstructionAuthoring | [`void Install()`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L16) | public static |
| SiegeConstructionAuthoring | [`SiegeConstructionSettings EnsureSettings()`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L120) | private static |
| SiegeConstructionAuthoring | [`void InstallPreselection(Scene main)`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L158) | public static |
| SiegeConstructionAuthoring | [`void CopyPool(Scene main, Scene target)`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L190) | private static |
| SiegeConstructionAuthoring | [`void RemoveOwned(Transform parent, string name)`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L201) | private static |
| SiegeConstructionAuthoring | [`RectTransform Rect(string name, Transform parent, Vector2 size)`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L210) | private static |
| SiegeConstructionAuthoring | [`void At(RectTransform rect, float x, float y, float width, float height)`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L219) | private static |
| SiegeConstructionAuthoring | [`Text Label( string name, Transform parent, string text, float x, float y, float width, float height, int size)`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L227) | private static |
| SiegeConstructionAuthoring | [`Button Button( string name, Transform parent, string title, float x, float y, float width)`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L249) | private static |
| SiegeConstructionAuthoring | [`void Set(SerializedObject data, string field, Object value)`](../Assets/Editor/Siege/Authoring/SiegeConstructionAuthoring.cs#L275) | private static |

## Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs

// 职责：原位迁移Siege战斗界面与共享Prefab，保留已有实例覆盖和用户调整。 // 入口：Upgrade() / ConfigureStage()；显式保存真实资产，修改前备份四场景与Prefab。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeGameplayHudAuthoring | [`void Upgrade()`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L16) | public static |
| SiegeGameplayHudAuthoring | [`void ConfigureStage(SiegeGameplayStage stage)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L91) | public static |
| SiegeGameplayHudAuthoring | [`void Restyle(Transform root)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L233) | public static |
| SiegeGameplayHudAuthoring | [`void Soften(Transform root)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L260) | private static |
| SiegeGameplayHudAuthoring | [`RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L270) | private static |
| SiegeGameplayHudAuthoring | [`TMP_Text Label( Transform parent, string name, TMP_FontAsset font, Vector2 position, Vector2 size, int fontSize, Vector2? anchor = null)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L284) | private static |
| SiegeGameplayHudAuthoring | [`Button Button( Transform parent, string name, string caption, TMP_FontAsset font, Vector2 anchor, Vector2 position, Vector2 size)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L308) | private static |
| SiegeGameplayHudAuthoring | [`void Set(SerializedObject data, string field, Object value)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L335) | private static |
| SiegeGameplayHudAuthoring | [`T Ensure(Transform owner)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L340) | private static |
| SiegeGameplayHudAuthoring | [`void Remove(Transform owned)`](../Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs#L347) | private static |

## Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs

// 职责：将当前Siege场景迁移为场景泊位交互、按需操作卡片和独立阶段HUD，保存可编辑UGUI资产。 // 入口：Apply()；只重建拥有的Siege HUD，保留竞技场布置、部署点和蓝图数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeInterfaceAuthoring | [`void Apply()`](../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L17) | public static |
| SiegeInterfaceAuthoring | [`RectTransform Panel( Transform parent, string name, float x, float y, float width, float height)`](../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L388) | private static |
| SiegeInterfaceAuthoring | [`Text Label( Transform parent, string name, string content, float x, float y, float width, float height, int size)`](../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L404) | private static |
| SiegeInterfaceAuthoring | [`Button Button( Transform parent, string name, string content, float x, float y, float width)`](../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L429) | private static |
| SiegeInterfaceAuthoring | [`Dropdown Dropdown( GameObject template, Transform parent, string name, float x, float y, float width)`](../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L461) | private static |
| SiegeInterfaceAuthoring | [`void Place(RectTransform rect, float x, float y, float width, float height)`](../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L484) | private static |
| SiegeInterfaceAuthoring | [`void Bind(Object target, string field, Object value)`](../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L491) | private static |

## Assets/Editor/Siege/Authoring/SiegeIslandAuthoring.cs

// 职责：为真实Siege场景保存浮空岛组件/配置及World地形材质，移除已替代的无限平板。 // 入口：Install() / Configure()；迁移前唯一备份，不改玩家基地或世界存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeIslandAuthoring | [`void PreviewCurrent()`](../Assets/Editor/Siege/Authoring/SiegeIslandAuthoring.cs#L14) | public static |
| SiegeIslandAuthoring | [`string BakeCurrent()`](../Assets/Editor/Siege/Authoring/SiegeIslandAuthoring.cs#L53) | public static |
| SiegeIslandAuthoring | [`string Install()`](../Assets/Editor/Siege/Authoring/SiegeIslandAuthoring.cs#L89) | public static |
| SiegeIslandAuthoring | [`void Configure(Scene scene)`](../Assets/Editor/Siege/Authoring/SiegeIslandAuthoring.cs#L131) | public static |

## Assets/Editor/Siege/Authoring/SiegeMainInterfaceAuthoring.cs

// 职责：将 Main 的真实建造UI、管理器与轴工具迁移至Siege，保留Main源场景与战场布局。 // 入口：Install()；显式菜单保存资产，可在Inspector继续编辑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeMainInterfaceAuthoring | [`void Install()`](../Assets/Editor/Siege/Authoring/SiegeMainInterfaceAuthoring.cs#L14) | public static |
| SiegeMainInterfaceAuthoring | [`void FitLayout()`](../Assets/Editor/Siege/Authoring/SiegeMainInterfaceAuthoring.cs#L167) | public static |

## Assets/Editor/Siege/Authoring/SiegeMixedStressAuthoring.cs

// 职责：制作四栋建筑/六舰/十二敌舰的独立混合压力Scene和配置，保留正常阵地战资产。 // 入口：Create()；首次创建后可编辑保存，已有目标不覆盖，恢复原活动场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeMixedStressAuthoring | [`void Create()`](../Assets/Editor/Siege/Authoring/SiegeMixedStressAuthoring.cs#L15) | public static |

## Assets/Editor/Siege/Authoring/SiegeObserverCameraAuthoring.cs

// 职责：将自由观察组件与使用提示保存到真实SiegeGameplay场景，保留已有布局与手工参数。 // 入口：Install()；可重复绑定，仅新增自己拥有的Camera Help节点，保存前创建场景备份。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeObserverCameraAuthoring | [`string Install()`](../Assets/Editor/Siege/Authoring/SiegeObserverCameraAuthoring.cs#L14) | public static |

## Assets/Editor/Siege/Authoring/SiegeProgressAuthoring.cs

// 职责：提供清除本机阵地战战果记录的显式菜单，便于测试或重新开始统计。 // 入口：ClearRecords()；只写本地PlayerPrefs，不触碰存档蓝图或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeProgressAuthoring | [`void ClearRecords()`](../Assets/Editor/Siege/Authoring/SiegeProgressAuthoring.cs#L8) | public static |

## Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs

// 职责：从阵地战场景制作六舰/2048模块建筑压力资产，隔离测试经济与正常游戏参数。 // 入口：Create() / CreateLarge()；首次创建不覆盖既有资产，保存后恢复原活动场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeStressAuthoring | [`void Create()`](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L18) | public static |
| SiegeStressAuthoring | [`void CreateLarge()`](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L24) | public static |
| SiegeStressAuthoring | [`void CreateProfile(string scenePath, string settingsPath, string sourcePath, bool large)`](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L31) | private static |
| SiegeStressAuthoring | [`TextAsset GenerateLargeBlueprint(TextAsset source)`](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L124) | private static |
| SiegeStressAuthoring | [`SiegeSession FindSession(Scene scene)`](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L166) | private static |

## Assets/Editor/Siege/Benchmarks/SiegeBuildingBenchmark.cs

// 职责：用真实两层建筑夹具测量完整支撑与单次范围损毁，保留生产同步开销与正确性计数。 // 入口：Start() / Status；远离战场、无物理步，不代表大规模交火帧率。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingBenchmark | [`string Start(string label = "geometry-baseline", bool large = false)`](../Assets/Editor/Siege/Benchmarks/SiegeBuildingBenchmark.cs#L17) | public static |
| SiegeBuildingBenchmark | [`void Execute(string label, bool large)`](../Assets/Editor/Siege/Benchmarks/SiegeBuildingBenchmark.cs#L42) | private static |
| SiegeBuildingBenchmark | [`Sample Measure(int count)`](../Assets/Editor/Siege/Benchmarks/SiegeBuildingBenchmark.cs#L98) | private static |
| SiegeBuildingBenchmark | [`Stages ReadStages()`](../Assets/Editor/Siege/Benchmarks/SiegeBuildingBenchmark.cs#L209) | private static |

## Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs

// 职责：采集阵地战真实Play帧观察间隔、分阶段CPU计时和低频场景规模，保存可审查原始样本。 // 入口：StartCapture() / Finish() / Status；Editor观察间隔含编辑器调度，不代表Player或GPU耗时。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePerformanceCapture | [`string StartCapture(string label = "natural-baseline", float seconds = 90f)`](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L35) | public static |
| SiegePerformanceCapture | [`void Tick()`](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L77) | private static |
| SiegePerformanceCapture | [`void TakeCensus(double now)`](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L114) | private static |
| SiegePerformanceCapture | [`string Finish(string reason = "Manual stop")`](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L146) | public static |
| SiegePerformanceCapture | [`double Percentile(List<double> values, double fraction)`](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L192) | private static |
| SiegePerformanceCapture | [`void BeforeReload()`](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L197) | private static |

## Assets/Editor/Siege/Benchmarks/SiegeRegroupBaseline.cs

// 职责：冻结3b1e13d的整组迁移实现，仅供Editor同布局性能对照，不进入运行时。 // 入口：Rebuild()；连接查询仍调用当前同一几何实现，隔离测量刚体迁移策略差异。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRegroupBaseline | [`void Rebuild(List<Block> blocks, GroupPhysicsSettings settings)`](../Assets/Editor/Siege/Benchmarks/SiegeRegroupBaseline.cs#L14) | public |
| SiegeRegroupBaseline | [`Dictionary<Block, Rigidbody> CaptureSourceBodies(List<Block> blocks)`](../Assets/Editor/Siege/Benchmarks/SiegeRegroupBaseline.cs#L74) | private static |
| SiegeRegroupBaseline | [`void RestoreGroupPhysics( Rigidbody targetBody, List<Block> group, Dictionary<Block, Rigidbody> sourceBodies, GroupPhysicsSettings settings)`](../Assets/Editor/Siege/Benchmarks/SiegeRegroupBaseline.cs#L94) | private static |

## Assets/Editor/Siege/Benchmarks/SiegeRegroupBenchmark.cs

// 职责：在相同真实方块链上交替比较冻结整组迁移与主体保留，分开记录创建/验证和同步刷新耗时。 // 入口：Run()；临时夹具每次立即清理，不将同步CPU对照称为物理帧率。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRegroupBenchmark | [`string Start()`](../Assets/Editor/Siege/Benchmarks/SiegeRegroupBenchmark.cs#L18) | public static |
| SiegeRegroupBenchmark | [`void ExecuteScheduled()`](../Assets/Editor/Siege/Benchmarks/SiegeRegroupBenchmark.cs#L32) | private static |
| SiegeRegroupBenchmark | [`string Run()`](../Assets/Editor/Siege/Benchmarks/SiegeRegroupBenchmark.cs#L50) | public static |
| SiegeRegroupBenchmark | [`double Measure(int count, bool split, bool baseline, SiegeRegroupBaseline previous, GroupPhysicsSettings settings)`](../Assets/Editor/Siege/Benchmarks/SiegeRegroupBenchmark.cs#L128) | private static |
| SiegeRegroupBenchmark | [`double Median(List<double> values)`](../Assets/Editor/Siege/Benchmarks/SiegeRegroupBenchmark.cs#L212) | private static |

## Assets/Editor/Siege/Benchmarks/SiegeTopologyBaseline.cs

// 职责：在远离战场的真实方块夹具上测量分组，并复现断链后输入顺序影响，保留优化前证据。 // 入口：Run()；不运行物理步，不将同步图查询耗时称为大规模战斗帧率。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeTopologyBaseline | [`string Run()`](../Assets/Editor/Siege/Benchmarks/SiegeTopologyBaseline.cs#L13) | public static |
| SiegeTopologyBaseline | [`string Partition(List<List<Block>> groups)`](../Assets/Editor/Siege/Benchmarks/SiegeTopologyBaseline.cs#L103) | private static |

## Assets/Editor/Siege/Benchmarks/SiegeTransientEffectBenchmark.cs

// 职责：测量实际短时光束/点光源的创建路径；远处隔离夹具不代表自然战斗FPS。 // 入口：Run(label)；每批同步到期，清理在计时外，不推进物理或保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeTransientEffectBenchmark | [`string Run(string label)`](../Assets/Editor/Siege/Benchmarks/SiegeTransientEffectBenchmark.cs#L15) | public static |
| SiegeTransientEffectBenchmark | [`bool IsOwned(MonoBehaviour item, Vector3 origin)`](../Assets/Editor/Siege/Benchmarks/SiegeTransientEffectBenchmark.cs#L81) | private static |

## Assets/Editor/Siege/Examples/SiegeBaseExamplesAuthoring.cs

// 职责：制作三个可独立编辑的官方基地示例，使用真实材质、仓储、无人机、电网和模块机库。 // 入口：Generate() / CreateDesign()；仅更新声明的官方资源，先备份，不改个人蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBaseExamplesAuthoring | [`string Generate()`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesAuthoring.cs#L25) | public static |
| SiegeBaseExamplesAuthoring | [`BlockDataList CreateDesign(int index)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesAuthoring.cs#L93) | public static |
| SiegeBaseExamplesAuthoring | [`void Hangar(BlockDataList design, string id, Vector3 position, HangarEntrance entrance)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesAuthoring.cs#L169) | private static |
| SiegeBaseExamplesAuthoring | [`void Core(BlockDataList design, float z)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesAuthoring.cs#L175) | private static |
| SiegeBaseExamplesAuthoring | [`void Defense(BlockDataList design, float x, float z, bool heavy)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesAuthoring.cs#L195) | private static |
| SiegeBaseExamplesAuthoring | [`void Service(BlockDataList design, float x, float z, int index)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesAuthoring.cs#L208) | private static |
| SiegeBaseExamplesAuthoring | [`BlockData Add(BlockDataList design, string id, string resource, Vector3 position, string material = null)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesAuthoring.cs#L234) | private static |

## Assets/Editor/Siege/Examples/SiegeBaseExamplesChecks.cs

// 职责：检查三份官方基地的实际目录、材质、无重叠、地面支撑、电网及机器人依赖。 // 入口：RunData() / RunPlay()；Play只创建远端临时实例，不读写个人蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBaseExamplesChecks | [`string RunData()`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesChecks.cs#L21) | public static |
| SiegeBaseExamplesChecks | [`string RunPlay()`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesChecks.cs#L77) | public static |
| SiegeBaseExamplesChecks | [`bool NoOverlaps(BlockDataList design)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesChecks.cs#L163) | private static |
| SiegeBaseExamplesChecks | [`void Check(Report report, bool passed, string name)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesChecks.cs#L184) | private static |
| SiegeBaseExamplesChecks | [`string Save(Report report, string label)`](../Assets/Editor/Siege/Examples/SiegeBaseExamplesChecks.cs#L194) | private static |

## Assets/Editor/Siege/Examples/SiegeCompletionIntegration.cs

// 职责：为既有Main模式选择绑定三例目录；保留场景和旧示例制作入口引用。 // 入口：Install()；仅Edit模式、无脏场景，通过Editor API保存引用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCompletionIntegration | [`string Install()`](../Assets/Editor/Siege/Examples/SiegeCompletionIntegration.cs#L12) | public static |

## Assets/Editor/Siege/Hangars/HangarAssetChecks.cs

// 职责：验证真实机库资产、存档恢复、非原点蓝图适配和物理门安全，不写玩家存档。 // 入口：RunEdit() 同步资产检查；Run() 在 Play 开始跨帧门检查，Result/报告保存最终结果。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarAssetChecks | [`string RunEdit()`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L30) | public static |
| HangarAssetChecks | [`string Run()`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L116) | public static |
| HangarAssetChecks | [`void GeometryChecks(HangarModule module, Report result)`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L151) | private static |
| HangarAssetChecks | [`void RestoreChecks(HangarModule module, Report result)`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L208) | private static |
| HangarAssetChecks | [`void Tick()`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L263) | private static |
| HangarAssetChecks | [`bool EntranceBlocked()`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L314) | private static |
| HangarAssetChecks | [`void Check(Report result, string name, bool condition)`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L321) | private static |
| HangarAssetChecks | [`void Finish(Exception error)`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L331) | private static |
| HangarAssetChecks | [`void Write()`](../Assets/Editor/Siege/Hangars/HangarAssetChecks.cs#L355) | private static |

## Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs

// 职责：制作可编辑机库 Prefab、材质和数量配置；尺寸控件、门和碰撞体均保存为真实子节点。 // 入口：Generate()；只重建本工具拥有的 Hangars 资产，不修改场景或玩家蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarPrefabAuthoring | [`GameObject Generate()`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L12) | public static |
| HangarPrefabAuthoring | [`void AddEntranceMarker()`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L98) | public static |
| HangarPrefabAuthoring | [`void EnsureEntranceMarker(GameObject root)`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L113) | private static |
| HangarPrefabAuthoring | [`GameObject MakePreviewTile()`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L137) | private static |
| HangarPrefabAuthoring | [`void EnsureLimits()`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L156) | private static |
| HangarPrefabAuthoring | [`Transform Cube(Transform parent, string name, Material material, bool collider)`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L169) | private static |
| HangarPrefabAuthoring | [`Canvas Canvas(Transform parent, string name, Vector2 size)`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L184) | private static |
| HangarPrefabAuthoring | [`HangarFacePanel Face(Transform parent, HangarModule module, Vector3 normal, int index)`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L196) | private static |
| HangarPrefabAuthoring | [`void AddEntranceControls()`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L238) | public static |
| HangarPrefabAuthoring | [`void EntranceControl(Transform panel, HangarModule module)`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L269) | private static |
| HangarPrefabAuthoring | [`void ResizeButton(Transform parent, HangarModule module, int axis, int delta, Vector2 position)`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L306) | private static |
| HangarPrefabAuthoring | [`Text Text( Transform parent, string name, string value, int size, Vector2 dimensions, Vector2 position)`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L336) | private static |
| HangarPrefabAuthoring | [`Material Material(string name, Color color, bool transparent = false)`](../Assets/Editor/Siege/Hangars/HangarPrefabAuthoring.cs#L362) | private static |

## Assets/Editor/Siege/Hangars/HangarSceneIntegration.cs

// 职责：迁移预研泊位为建筑蓝图机库，并保存Main基地编辑入口；保留旧基地原始资产。 // 入口：Install()；仅在保存后的Edit Mode执行，可重复更新工具拥有的引用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarSceneIntegration | [`void Install()`](../Assets/Editor/Siege/Hangars/HangarSceneIntegration.cs#L15) | public static |
| HangarSceneIntegration | [`void InstallPrototype()`](../Assets/Editor/Siege/Hangars/HangarSceneIntegration.cs#L36) | private static |
| HangarSceneIntegration | [`void InstallStarter()`](../Assets/Editor/Siege/Hangars/HangarSceneIntegration.cs#L88) | private static |
| HangarSceneIntegration | [`void InstallWorkshop()`](../Assets/Editor/Siege/Hangars/HangarSceneIntegration.cs#L110) | private static |

## Assets/Editor/Siege/Hangars/HangarShellChecks.cs

// 职责：验证逻辑机库展开、预算拒绝、独立墙体损伤、快照缺口与原设计维修。 // 入口：Run()；只使用隔离建筑实例，不读写玩家存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarShellChecks | [`string Run()`](../Assets/Editor/Siege/Hangars/HangarShellChecks.cs#L22) | public static |
| HangarShellChecks | [`void Check(Report report, string name, bool condition)`](../Assets/Editor/Siege/Hangars/HangarShellChecks.cs#L215) | private static |

## Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs

// 职责：通过真实基地放置、已制作按钮和输入管线验证机库尺寸、移动、旋转与六库限制。 // 入口：Run()；从保存的 Main 启动隔离 Play，所有蓝图只写 Temp/Hangars，结束自动退出。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarWorkshopChecks | [`void Run()`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L47) | public static |
| HangarWorkshopChecks | [`void Changed(PlayModeStateChange state)`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L60) | private static |
| HangarWorkshopChecks | [`void Tick()`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L125) | private static |
| HangarWorkshopChecks | [`IEnumerator Scenario()`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L173) | private static |
| HangarWorkshopChecks | [`IEnumerator Frames(int count)`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L503) | private static |
| HangarWorkshopChecks | [`Button ResizeButton(HangarFacePanel panel, int axis, int delta)`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L512) | private static |
| HangarWorkshopChecks | [`BlockData Saved(Block block)`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L517) | private static |
| HangarWorkshopChecks | [`int Dimension(BlockData data, int axis)`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L522) | private static |
| HangarWorkshopChecks | [`void Inject(SaveManager saves, string field, string directory)`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L527) | private static |
| HangarWorkshopChecks | [`void Check(bool condition, string message)`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L533) | private static |
| HangarWorkshopChecks | [`void Capture(string message, string stack, LogType type)`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L543) | private static |
| HangarWorkshopChecks | [`void Finish()`](../Assets/Editor/Siege/Hangars/HangarWorkshopChecks.cs#L551) | private static |

## Assets/Editor/Siege/Hangars/ModularBaseExampleAuthoring.cs

// 职责：以可复现配方重做官方机库示例；统一更新三套真实示例资产并保留带时间戳备份。 // 入口：Generate()；不清空个人基地库，核心ID与官方菜单引用保持稳定。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ModularBaseExampleAuthoring | [`string Generate()`](../Assets/Editor/Siege/Hangars/ModularBaseExampleAuthoring.cs#L7) | public static |

## Assets/Editor/Siege/Hangars/SiegeFlightAuthoring.cs

// 职责：将已验证起降预设保存到真实Siege场景，保留显式改过的参数和无关对象。 // 入口：UpgradeDefaults()；只将原8/6预设升级12/12，保存前备份原场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFlightAuthoring | [`string UpgradeDefaults()`](../Assets/Editor/Siege/Hangars/SiegeFlightAuthoring.cs#L13) | public static |

## Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs

// 职责：验证真实菜单新版基地战与独立预研会话复用机库UI、舰队与返航结算。 // 入口：RunStage() / RunPrototype()；只注入运行时蓝图目录，报告写Temp/Hangars/host-*.json。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeHangarHostChecks | [`void RunStage()`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L41) | public static |
| SiegeHangarHostChecks | [`void RunWideStage()`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L47) | public static |
| SiegeHangarHostChecks | [`void RunPrototype()`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L52) | public static |
| SiegeHangarHostChecks | [`void Start(string target, string path)`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L58) | private static |
| SiegeHangarHostChecks | [`void Changed(PlayModeStateChange state)`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L69) | private static |
| SiegeHangarHostChecks | [`void Tick()`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L90) | private static |
| SiegeHangarHostChecks | [`IEnumerator Scenario()`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L116) | private static |
| SiegeHangarHostChecks | [`int ClearWave(SiegeMatch match)`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L434) | private static |
| SiegeHangarHostChecks | [`void BackupPreferences()`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L460) | private static |
| SiegeHangarHostChecks | [`void RestorePreferences()`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L475) | private static |
| SiegeHangarHostChecks | [`void Check(bool success, string description)`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L497) | private static |
| SiegeHangarHostChecks | [`void LogError(string message, string stack, LogType type)`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L507) | private static |
| SiegeHangarHostChecks | [`void Finish()`](../Assets/Editor/Siege/Hangars/SiegeHangarHostChecks.cs#L515) | private static |

## Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs

// 职责：为新版基地战保存共享机库部署UI；只重建工具拥有的Hangar Deployment Interface。 // 入口：Install()；与旧原型使用同一部署面板组件和筛选规则。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeHangarInterfaceAuthoring | [`void Install()`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L13) | public static |
| SiegeHangarInterfaceAuthoring | [`void UpgradeScenes()`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L129) | public static |
| SiegeHangarInterfaceAuthoring | [`void UpgradeActions(SiegeDeploymentView view)`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L163) | public static |
| SiegeHangarInterfaceAuthoring | [`void PlaceAction(SerializedObject data, string field, Vector2 position, Vector2 size)`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L248) | private static |
| SiegeHangarInterfaceAuthoring | [`void Bind(SerializedObject data, string field, UnityEngine.Object value)`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L256) | private static |
| SiegeHangarInterfaceAuthoring | [`RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size)`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L261) | private static |
| SiegeHangarInterfaceAuthoring | [`Text Label( string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize)`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L270) | private static |
| SiegeHangarInterfaceAuthoring | [`Button Button(string name, Transform parent, string caption, Vector2 position, Vector2 size)`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L291) | private static |
| SiegeHangarInterfaceAuthoring | [`void Place(RectTransform rect, Vector2 position, Vector2 size)`](../Assets/Editor/Siege/Hangars/SiegeHangarInterfaceAuthoring.cs#L308) | private static |

## Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs

// 职责：在Main真实Play中验证机库容量、独占绑定、净空、毁坏重建和基地战共享舰队。 // 入口：Run()；夹具远离地图，不写玩家存档，报告写Temp/Hangars/play-checks.json。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Host | [`void SelectDefender(int index)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L114) | public |
| Host | [`void SelectAttacker(int index)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L118) | public |
| Host | [`bool RefreshBlueprints()`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L122) | public |
| Host | [`bool TryDeployAt(int optionIndex, int dock, out string reason)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L123) | public |
| Host | [`bool TryReplaceAt(int optionIndex, int dock, out string reason)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L124) | public |
| Host | [`bool TryCancelDeployment(int dock, out string reason)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L125) | public |
| Host | [`bool TryTakeControl(int slot)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L126) | public |
| Host | [`bool TryTakeControl(string id)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L127) | public |
| Host | [`bool TryRepairShip(string id, bool repairBase = false)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L128) | public |
| SiegeHangarPlayChecks | [`void RunRoof()`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L136) | public static |
| SiegeHangarPlayChecks | [`void Run()`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L143) | public static |
| SiegeHangarPlayChecks | [`void Begin()`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L149) | private static |
| SiegeHangarPlayChecks | [`void Changed(PlayModeStateChange state)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L160) | private static |
| SiegeHangarPlayChecks | [`void Tick()`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L180) | private static |
| SiegeHangarPlayChecks | [`IEnumerator Scenario()`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L206) | private static |
| SiegeHangarPlayChecks | [`void SetSimulation(WorldBuildingManager manager, bool active)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L447) | private static |
| SiegeHangarPlayChecks | [`IEnumerator GuideFlight(SiegeReturnFlight flight, SiegeFleetEntry entry, string phase, Action completed)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L453) | private static |
| SiegeHangarPlayChecks | [`void SampleFlight(SiegeReturnFlight flight, SiegeFleetEntry entry, string phase)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L475) | private static |
| SiegeHangarPlayChecks | [`void CheckGuidedFeedback(string phase)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L560) | private static |
| SiegeHangarPlayChecks | [`void Check(bool success, string description)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L581) | private static |
| SiegeHangarPlayChecks | [`void LogError(string message, string stack, LogType type)`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L591) | private static |
| SiegeHangarPlayChecks | [`void Finish()`](../Assets/Editor/Siege/Hangars/SiegeHangarPlayChecks.cs#L599) | private static |

## Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs

// 职责：验证返航经济与真实刚体导引，不修改玩家蓝图或持久场景。 // 入口：Run()，需在有 PlayManager 的 Play Mode 运行；独立 PhysicsScene 手动推进物理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Host | [`void SelectDefender(int index)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L49) | public |
| Host | [`void SelectAttacker(int index)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L53) | public |
| Host | [`bool RefreshBlueprints()`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L57) | public |
| Host | [`bool TryDeployAt(int optionIndex, int dock, out string reason)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L58) | public |
| Host | [`bool TryReplaceAt(int optionIndex, int dock, out string reason)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L64) | public |
| Host | [`bool TryCancelDeployment(int dock, out string reason)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L65) | public |
| Host | [`bool TryTakeControl(int slot)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L66) | public |
| Host | [`bool TryTakeControl(string id)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L67) | public |
| Host | [`bool TryRepairShip(string id, bool repairBase = false)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L68) | public |
| SiegeReturnChecks | [`string Run()`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L77) | public static |
| SiegeReturnChecks | [`SiegeFleetEntry Spawn(Scene scene, Transform dock)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L180) | private static |
| SiegeReturnChecks | [`void Step(SiegeReturnFlight flight, PhysicsScene physics, bool powered, int maximumSteps)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L203) | private static |
| SiegeReturnChecks | [`void CheckEconomy(Report report)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L224) | private static |
| SiegeReturnChecks | [`void Check(Report report, string description, bool success)`](../Assets/Editor/Siege/Hangars/SiegeReturnChecks.cs#L282) | private static |

## Assets/Editor/Siege/Player/SiegePlayerBuild.cs

// 职责：构建以Siege为入口的独立Windows D3D11验证包，不改全局构建场景或用户发布包。 // 入口：Build() / BuildStress() / BuildLargeStress() / BuildMixedStress() / BuildLargeProfiling()；独立Development包用于引擎归因。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayerBuild | [`void Build()`](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L16) | public static |
| SiegePlayerBuild | [`void BuildStress()`](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L22) | public static |
| SiegePlayerBuild | [`void BuildLargeStress()`](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L32) | public static |
| SiegePlayerBuild | [`void BuildLargeProfiling()`](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L42) | public static |
| SiegePlayerBuild | [`void BuildMixedStress()`](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L53) | public static |
| SiegePlayerBuild | [`void Schedule(string scene, string output, string statusPath, BuildOptions options = BuildOptions.None)`](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L63) | private static |
| SiegePlayerBuild | [`void BuildNow(string scene, string output, string statusPath, BuildOptions options)`](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L83) | private static |

## Assets/Editor/Siege/SiegeConstructionPlayChecks.cs

// 职责：在真实Main Play Mode验证副场景切换、取消/重试、建造原稿隔离和旧Siege不被启动。 // 入口：Run()；只修改运行时夹具，报告写Temp/BaseConstruction，不写玩家存档或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeConstructionPlayChecks | [`void Run()`](../Assets/Editor/Siege/SiegeConstructionPlayChecks.cs#L35) | public static |
| SiegeConstructionPlayChecks | [`void Changed(PlayModeStateChange state)`](../Assets/Editor/Siege/SiegeConstructionPlayChecks.cs#L47) | private static |
| SiegeConstructionPlayChecks | [`void Tick()`](../Assets/Editor/Siege/SiegeConstructionPlayChecks.cs#L67) | private static |
| SiegeConstructionPlayChecks | [`IEnumerator Scenario()`](../Assets/Editor/Siege/SiegeConstructionPlayChecks.cs#L93) | private static |
| SiegeConstructionPlayChecks | [`void Check(bool success, string description)`](../Assets/Editor/Siege/SiegeConstructionPlayChecks.cs#L352) | private static |
| SiegeConstructionPlayChecks | [`void LogError(string message, string stack, LogType type)`](../Assets/Editor/Siege/SiegeConstructionPlayChecks.cs#L362) | private static |
| SiegeConstructionPlayChecks | [`void Finish()`](../Assets/Editor/Siege/SiegeConstructionPlayChecks.cs#L370) | private static |

## Assets/Editor/Siege/SiegePlayChecks.cs

// 职责：在独立Siege真实Play中验证部署/接管、修缮身份与金币、驾驶舰死亡和基地失败。 // 入口：Run()；损伤/缺件/断裂为显式夹具，死亡走生产伤害链；不代表自然交火或性能测试。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayChecks | [`string Run()`](../Assets/Editor/Siege/SiegePlayChecks.cs#L14) | public static |
| SiegePlayChecks | [`int SetComboBonus(SiegeSession session, int value)`](../Assets/Editor/Siege/SiegePlayChecks.cs#L230) | private static |
| SiegePlayChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/SiegePlayChecks.cs#L241) | private static |

## Assets/Editor/Siege/SiegeSceneAuthoring.cs

// 职责：通过Unity API创建独立、可编辑的阵地战场景和默认配置，不重写Main场景或用户存档。 // 入口：Create() / Open()；已有场景不覆盖，泊位、UI、材质和波次配置均保存为真实资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeSceneAuthoring | [`void Create()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L20) | public static |
| SiegeSceneAuthoring | [`void Open()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L85) | public static |
| SiegeSceneAuthoring | [`void FitHudToViewport()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L96) | public static |
| SiegeSceneAuthoring | [`void RegenerateDefaultBase()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L122) | public static |
| SiegeSceneAuthoring | [`TextAsset CreateBaseBlueprint(bool replace = false)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L140) | private static |
| SiegeSceneAuthoring | [`SiegeSettings CreateSettings(TextAsset baseBlueprint)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L145) | private static |
| SiegeSceneAuthoring | [`void CreateManagers()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L190) | private static |
| SiegeSceneAuthoring | [`void CreateEnvironment()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L206) | private static |
| SiegeSceneAuthoring | [`Transform[] CreateDocks(Transform parent)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L229) | private static |
| SiegeSceneAuthoring | [`Material MaterialAsset(string name, Color color)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L250) | private static |
| SiegeSceneAuthoring | [`void CreateCamera()`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L267) | private static |
| SiegeSceneAuthoring | [`void CreateHud(SiegeSession session, SiegeSettings settings, int bays)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L284) | private static |
| SiegeSceneAuthoring | [`void SetArray(SerializedProperty property, Object[] values)`](../Assets/Editor/Siege/SiegeSceneAuthoring.cs#L298) | private static |

## Assets/Editor/Siege/Validation/Buildings/SiegeBuildingChecks.cs

// 职责：验证真实大型建筑蓝图基地的支撑/净空、原位修缮、独立核心以及舰船生命周期隔离。 // 入口：Run()；全新Siege Play，显式损坏夹具不代表自然波次或性能结论。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingChecks.cs#L13) | public static |
| SiegeBuildingChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingChecks.cs#L151) | private static |

## Assets/Editor/Siege/Validation/Buildings/SiegeBuildingDamageChecks.cs

// 职责：验证建筑批量损毁的命中顺序独立、一次支撑刷新、链式爆炸及单块即时断裂契约。 // 入口：Run()；仅使用远离场景的自有夹具，恢复阵地战伤害门控，不改资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingDamageChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingDamageChecks.cs#L13) | public static |
| SiegeBuildingDamageChecks | [`void CheckPartialBlast(List<string> checks, bool reverse)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingDamageChecks.cs#L52) | private static |
| SiegeBuildingDamageChecks | [`void CheckChain(List<string> checks, bool projectile)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingDamageChecks.cs#L87) | private static |
| SiegeBuildingDamageChecks | [`void CheckImmediate(List<string> checks)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingDamageChecks.cs#L139) | private static |
| SiegeBuildingDamageChecks | [`GameObject CreateTower(bool reverse, out WorldBuildingManager manager, out BuildingRuntime tower)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingDamageChecks.cs#L164) | private static |
| SiegeBuildingDamageChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingDamageChecks.cs#L207) | private static |

## Assets/Editor/Siege/Validation/Buildings/SiegeBuildingGraphChecks.cs

// 职责：对照实时几何验证建筑邻接缓存的热路径、根运动、局部修改、断链与删除失效。 // 入口：Run()；隔离真实方块夹具，不运行物理步，不修改场景资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingGraphChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingGraphChecks.cs#L14) | public static |
| SiegeBuildingGraphChecks | [`void DenseCandidateChecks(List<string> checks, GameObject prefab, Type graphType, MethodInfo prepare, MethodInfo collect)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingGraphChecks.cs#L180) | private static |
| SiegeBuildingGraphChecks | [`void ConnectionVisualPolicyChecks(List<string> checks, GameObject prefab, Type graphType, MethodInfo prepare, MethodInfo collect)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingGraphChecks.cs#L242) | private static |
| SiegeBuildingGraphChecks | [`void SparseRemovalChecks(List<string> checks, GameObject prefab, Type graphType, MethodInfo prepare, MethodInfo collect)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingGraphChecks.cs#L305) | private static |

## Assets/Editor/Siege/Validation/Buildings/SiegeBuildingPowerProbe.cs

// 职责：用基地中央的高耐久敌方固定靶验证四座建筑炮塔真实索敌、供电和弹道命中。 // 入口：Start() / Status；真实推进物理帧，固定靶是显式夹具，不代表自然波次完成。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBuildingPowerProbe | [`string Start()`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingPowerProbe.cs#L18) | public static |
| SiegeBuildingPowerProbe | [`void Tick()`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingPowerProbe.cs#L58) | private static |
| SiegeBuildingPowerProbe | [`void Finish(string result)`](../Assets/Editor/Siege/Validation/Buildings/SiegeBuildingPowerProbe.cs#L87) | private static |

## Assets/Editor/Siege/Validation/Buildings/SiegeFoundationCacheChecks.cs

// 职责：在真实基地上对照缓存与实时底座判定，验证根运动和局部连接几何失效。 // 入口：Run()；同步隔离变化后恢复，不推进物理步或保存场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFoundationCacheChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Buildings/SiegeFoundationCacheChecks.cs#L14) | public static |

## Assets/Editor/Siege/Validation/Fleet/SiegeBlueprintCatalogChecks.cs

// 职责：验证攻守蓝图目录隔离、坏文件容错、身份复制与源文件只读边界。 // 入口：Run()；使用Temp内独立目录，不读写用户存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBlueprintCatalogChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Fleet/SiegeBlueprintCatalogChecks.cs#L10) | public static |
| SiegeBlueprintCatalogChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegeBlueprintCatalogChecks.cs#L51) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegeFleetChecks.cs

// 职责：在真实Play环境验证共用蓝图恢复、阵营与驾驶权隔离、AI接管及旧敌机入口。 // 入口：Run()；创建隔离舰艇，结束后停用并销毁，不保存玩家文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFleetChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Fleet/SiegeFleetChecks.cs#L12) | public static |
| SiegeFleetChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegeFleetChecks.cs#L108) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegePriorityTargetChecks.cs

// 职责：用真实基地与敌舰验证任务目标远程追击、普通索敌边界及阵营/启用/守卫约束。 // 入口：Run()；全新Siege Play的同步隔离夹具，不推进物理步，不把该检查当作AI实飞验证。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePriorityTargetChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Fleet/SiegePriorityTargetChecks.cs#L14) | public static |
| SiegePriorityTargetChecks | [`void Check(Report report, string name, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegePriorityTargetChecks.cs#L128) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegePriorityTargetFlight.cs

// 职责：真实Play跨帧验证范围外任务目标的接近输入与刚体位移，保留生产飞行和供电参数。 // 入口：Start() / Status；仅创建并清理自己的敌舰，不代表自然整波性能或全部AI导航。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePriorityTargetFlight | [`string Start()`](../Assets/Editor/Siege/Validation/Fleet/SiegePriorityTargetFlight.cs#L18) | public static |
| SiegePriorityTargetFlight | [`void Tick()`](../Assets/Editor/Siege/Validation/Fleet/SiegePriorityTargetFlight.cs#L57) | private static |
| SiegePriorityTargetFlight | [`void BeforeReload()`](../Assets/Editor/Siege/Validation/Fleet/SiegePriorityTargetFlight.cs#L83) | private static |
| SiegePriorityTargetFlight | [`void Finish(string reason)`](../Assets/Editor/Siege/Validation/Fleet/SiegePriorityTargetFlight.cs#L88) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegeRegroupChecks.cs

// 职责：验证增量重组保留驾驶主体、世界姿态、源点速度、阵营/货物归属及局部刷新边界。 // 入口：Run()；要求全新Siege Play，受控断链夹具不代表自然战斗性能。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRegroupChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Fleet/SiegeRegroupChecks.cs#L13) | public static |
| SiegeRegroupChecks | [`void CheckSingleBlock(List<string> checks)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRegroupChecks.cs#L118) | private static |
| SiegeRegroupChecks | [`void CheckEmptyRoot(List<string> checks)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRegroupChecks.cs#L147) | private static |
| SiegeRegroupChecks | [`void CheckCockpitlessSplit(List<string> checks)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRegroupChecks.cs#L178) | private static |
| SiegeRegroupChecks | [`bool PosesUnchanged(Dictionary<Block, Pose> poses)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRegroupChecks.cs#L234) | private static |
| SiegeRegroupChecks | [`bool Near(Vector3 left, Vector3 right)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRegroupChecks.cs#L239) | private static |
| SiegeRegroupChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRegroupChecks.cs#L244) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegeRuntimeConnectionChecks.cs

// 职责：验证缓存连接的无查询热路径、断链、制作变更失效和无视觉/双侧视觉生命周期。 // 入口：Run()；调用真实内部运行分组入口，物理探测/旧视觉扫描计数由生产代码按需记录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRuntimeConnectionChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Fleet/SiegeRuntimeConnectionChecks.cs#L15) | public static |
| SiegeRuntimeConnectionChecks | [`List<List<Block>> Groups(List<Block> blocks)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRuntimeConnectionChecks.cs#L170) | private static |
| SiegeRuntimeConnectionChecks | [`void RobotContainerChecks(List<string> checks)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRuntimeConnectionChecks.cs#L175) | private static |
| SiegeRuntimeConnectionChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegeRuntimeConnectionChecks.cs#L268) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegeStrandedCockpitChecks.cs

// 职责：用真实受损舰和生产波次导演验证残舱退场、恢复边界及零击杀奖励。 // 入口：RunLive()为同步规则；RunNaturalLive()跨真实帧损毁并自然等待，均在Build Play运行。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeStrandedCockpitChecks | [`void RunNaturalLive()`](../Assets/Editor/Siege/Validation/Fleet/SiegeStrandedCockpitChecks.cs#L20) | public static |
| SiegeStrandedCockpitChecks | [`void CaptureNaturalError(string message, string trace, LogType type)`](../Assets/Editor/Siege/Validation/Fleet/SiegeStrandedCockpitChecks.cs#L36) | private static |
| SiegeStrandedCockpitChecks | [`void TickNatural()`](../Assets/Editor/Siege/Validation/Fleet/SiegeStrandedCockpitChecks.cs#L44) | private static |
| SiegeStrandedCockpitChecks | [`IEnumerator NaturalScenario()`](../Assets/Editor/Siege/Validation/Fleet/SiegeStrandedCockpitChecks.cs#L74) | private static |
| SiegeStrandedCockpitChecks | [`string RunLive()`](../Assets/Editor/Siege/Validation/Fleet/SiegeStrandedCockpitChecks.cs#L205) | public static |
| SiegeStrandedCockpitChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegeStrandedCockpitChecks.cs#L311) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegeTopologyChecks.cs

// 职责：用真实方块验证断链对称性、碰撞层恢复以及移动/禁用连接面等分组契约。 // 入口：Run()；临时夹具与现有单位隔离，失败抛出并在finally清理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeTopologyChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Fleet/SiegeTopologyChecks.cs#L12) | public static |
| SiegeTopologyChecks | [`bool IsAllSingleton(List<List<Block>> groups)`](../Assets/Editor/Siege/Validation/Fleet/SiegeTopologyChecks.cs#L136) | private static |
| SiegeTopologyChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegeTopologyChecks.cs#L141) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegeWorkshopChecks.cs

// 职责：在真实Siege Play帧验证Main工作台、异步取消、存档写入和战场隔离。 // 入口：Run()；只创建唯一临时Saves蓝图，finally清理，绝不写用户已有蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWorkshopChecks | [`void Run()`](../Assets/Editor/Siege/Validation/Fleet/SiegeWorkshopChecks.cs#L18) | public static |
| SiegeWorkshopChecks | [`IEnumerator CheckRoutine(SiegeSession session)`](../Assets/Editor/Siege/Validation/Fleet/SiegeWorkshopChecks.cs#L33) | private static |
| SiegeWorkshopChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegeWorkshopChecks.cs#L236) | private static |

## Assets/Editor/Siege/Validation/Fleet/SiegeWreckBudgetChecks.cs

// 职责：验证敌方碎片预算、寿命、节流及阵营/驾驶舱/场景保护，不制造真实击杀或修改蓝图。 // 入口：Run()；普通Siege全新Play，夹具同步检查后清除。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWreckBudgetChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Fleet/SiegeWreckBudgetChecks.cs#L15) | public static |
| SiegeWreckBudgetChecks | [`ControlUnit Create(string label, UnitFaction faction, int count)`](../Assets/Editor/Siege/Validation/Fleet/SiegeWreckBudgetChecks.cs#L143) | private static |
| SiegeWreckBudgetChecks | [`void Check(string label, bool passed)`](../Assets/Editor/Siege/Validation/Fleet/SiegeWreckBudgetChecks.cs#L162) | private static |

## Assets/Editor/Siege/Validation/Interface/SiegeCombatFeedbackChecks.cs

// 职责：验证精英波次奖励倍数与战斗中连击加成的纯规则行为，不依赖场景、物理或敌舰生成。 // 入口：Run()；只构造SiegeMatch并登记/结算虚构敌机，不写资产、不改设置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCombatFeedbackChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Interface/SiegeCombatFeedbackChecks.cs#L16) | public static |
| SiegeCombatFeedbackChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Interface/SiegeCombatFeedbackChecks.cs#L238) | private static |

## Assets/Editor/Siege/Validation/Interface/SiegeObserverCameraChecks.cs

// 职责：验证真实共享相机的自由观察所有权、总览复位、重入及禁用清理，不保存场景。 // 入口：Run()；Main Play中运行，完成后恢复原相机模式、位置与旋转。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeObserverCameraChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverCameraChecks.cs#L11) | public static |
| SiegeObserverCameraChecks | [`bool SamePose(Transform view, Vector3 position, Quaternion rotation)`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverCameraChecks.cs#L84) | private static |
| SiegeObserverCameraChecks | [`void Require(bool value, string name, List<string> checks)`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverCameraChecks.cs#L89) | private static |

## Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs

// 职责：通过真实InputSystem设备和Main的EventSystem验证观察相机键鼠、UI/菜单阻挡与光标释放。 // 入口：RunLive()；异步报告Temp/SiegeCamera/input.json，恢复设备/输入/相机/菜单显示，不写玩家蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeObserverInputChecks | [`void RunLive()`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L57) | public static |
| SiegeObserverInputChecks | [`IEnumerator Scenario()`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L106) | private static |
| SiegeObserverInputChecks | [`bool HasGraphicHit(Vector2 point)`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L306) | private static |
| SiegeObserverInputChecks | [`Vector2 FindOpenPoint()`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L313) | private static |
| SiegeObserverInputChecks | [`void Queue(bool right, Vector2 position, Key[] keys = null, Vector2 delta = default, Vector2 scroll = default)`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L346) | private static |
| SiegeObserverInputChecks | [`void Tick()`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L363) | private static |
| SiegeObserverInputChecks | [`void Require(bool value, string name)`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L389) | private static |
| SiegeObserverInputChecks | [`void Save()`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L400) | private static |
| SiegeObserverInputChecks | [`void Finish(string error)`](../Assets/Editor/Siege/Validation/Interface/SiegeObserverInputChecks.cs#L407) | private static |

## Assets/Editor/Siege/Validation/Interface/SiegeSceneInteractionChecks.cs

// 职责：在真实Play帧验证场景泊位点击、无副作用预览、精确扣款、友舰接管及Workshop隔离往返。 // 入口：Run()；使用临时输入设备和阻挡夹具，不写用户蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeSceneInteractionChecks | [`void Run()`](../Assets/Editor/Siege/Validation/Interface/SiegeSceneInteractionChecks.cs#L18) | public static |
| SiegeSceneInteractionChecks | [`IEnumerator RunChecks(SiegeSession session)`](../Assets/Editor/Siege/Validation/Interface/SiegeSceneInteractionChecks.cs#L30) | private static |
| SiegeSceneInteractionChecks | [`Button Named(SiegeDeploymentView view, string name)`](../Assets/Editor/Siege/Validation/Interface/SiegeSceneInteractionChecks.cs#L200) | private static |
| SiegeSceneInteractionChecks | [`IEnumerator Click(Button button, Mouse mouse)`](../Assets/Editor/Siege/Validation/Interface/SiegeSceneInteractionChecks.cs#L205) | private static |
| SiegeSceneInteractionChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Interface/SiegeSceneInteractionChecks.cs#L222) | private static |

## Assets/Editor/Siege/Validation/Interface/SiegeUsabilityChecks.cs

// 职责：以真实部署净空和工作台往返检查阵地战泊位选择与蓝图选择保持，保护金币和用户存档。 // 入口：RunDeployment() / RunSelection()；分别在全新 Siege Play 中执行，不修改源蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeUsabilityChecks | [`string RunDeployment()`](../Assets/Editor/Siege/Validation/Interface/SiegeUsabilityChecks.cs#L15) | public static |
| SiegeUsabilityChecks | [`string RunSelection()`](../Assets/Editor/Siege/Validation/Interface/SiegeUsabilityChecks.cs#L96) | public static |
| SiegeUsabilityChecks | [`SiegeSession FreshSession()`](../Assets/Editor/Siege/Validation/Interface/SiegeUsabilityChecks.cs#L174) | private static |
| SiegeUsabilityChecks | [`void RefreshHud(SiegeHud hud)`](../Assets/Editor/Siege/Validation/Interface/SiegeUsabilityChecks.cs#L185) | private static |
| SiegeUsabilityChecks | [`void Check(List<string> checks, string label, bool passed)`](../Assets/Editor/Siege/Validation/Interface/SiegeUsabilityChecks.cs#L191) | private static |
| SiegeUsabilityChecks | [`string Write(string name, List<string> checks, Exception error = null)`](../Assets/Editor/Siege/Validation/Interface/SiegeUsabilityChecks.cs#L201) | private static |

## Assets/Editor/Siege/Validation/Rules/SiegeDestructionLogChecks.cs

// 职责：隔离检查销毁/VFX诊断默认值、可恢复开关及ABBA日志路径开销，不刺激自然战斗。 // 入口：Run()；禁用效果的同方法对照只测诊断路径，不代表交火帧率或真实爆炸成本。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeDestructionLogChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Rules/SiegeDestructionLogChecks.cs#L15) | public static |
| SiegeDestructionLogChecks | [`void Check(Report report, string name, bool passed)`](../Assets/Editor/Siege/Validation/Rules/SiegeDestructionLogChecks.cs#L118) | private static |

## Assets/Editor/Siege/Validation/Rules/SiegeMixedStressChecks.cs

// 职责：验证独立混合压力场的实际建筑规模、预算、模块身份和卫星损毁/清理边界。 // 入口：Start() / Status；真实Play加载完成后执行，仅作用于该测试会话。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeMixedStressChecks | [`void Start()`](../Assets/Editor/Siege/Validation/Rules/SiegeMixedStressChecks.cs#L16) | public static |
| SiegeMixedStressChecks | [`void Tick()`](../Assets/Editor/Siege/Validation/Rules/SiegeMixedStressChecks.cs#L31) | private static |
| SiegeMixedStressChecks | [`void Check(Report report, string label, bool passed)`](../Assets/Editor/Siege/Validation/Rules/SiegeMixedStressChecks.cs#L117) | private static |

## Assets/Editor/Siege/Validation/Rules/SiegePlayerSurveyChecks.cs

// 职责：在真实Siege Play中校验刚体分类、闲置池过滤与显式采样的计时开关恢复，避免诊断污染玩法。 // 入口：Run()；分类用真实基地/舰船，慢帧边界用合成delta，不是性能或物理步证据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayerSurveyChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Rules/SiegePlayerSurveyChecks.cs#L12) | public static |
| SiegePlayerSurveyChecks | [`Rigidbody CreateBody(List<GameObject> roots, string name)`](../Assets/Editor/Siege/Validation/Rules/SiegePlayerSurveyChecks.cs#L128) | private static |
| SiegePlayerSurveyChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Rules/SiegePlayerSurveyChecks.cs#L138) | private static |

## Assets/Editor/Siege/Validation/Rules/SiegeRulesChecks.cs

// 职责：验证阵地战阶段、波次预算和经济的边界；无需进入Play或读写玩家存档。 // 入口：Run() / 菜单 HY-Sandbox/Validation/Siege Rules。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRulesChecks | [`void RunFromMenu()`](../Assets/Editor/Siege/Validation/Rules/SiegeRulesChecks.cs#L11) | public static |
| SiegeRulesChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Rules/SiegeRulesChecks.cs#L17) | public static |
| SiegeRulesChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/Siege/Validation/Rules/SiegeRulesChecks.cs#L117) | private static |

## Assets/Editor/Siege/Validation/Rules/SiegeStressCancellationChecks.cs

// 职责：在首次分帧恢复中取消压力组件，验证HUD恢复与未发布建筑的回滚。 // 入口：Start() / Status；全新普通Siege Play，两个实际帧后检查无残留根对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeStressCancellationChecks | [`string Start()`](../Assets/Editor/Siege/Validation/Rules/SiegeStressCancellationChecks.cs#L19) | public static |
| SiegeStressCancellationChecks | [`void Complete()`](../Assets/Editor/Siege/Validation/Rules/SiegeStressCancellationChecks.cs#L66) | private static |
| SiegeStressCancellationChecks | [`void Check(string label, bool passed)`](../Assets/Editor/Siege/Validation/Rules/SiegeStressCancellationChecks.cs#L96) | private static |

## Assets/Editor/Siege/Validation/Rules/SiegeTransientEffectChecks.cs

// 职责：检查短时效果复用、重置、闲置上限及场景卸载；不替代自然画面验证。 // 入口：Run() / Status；卸载异步完成后写出最终报告。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeTransientEffectChecks | [`string Run()`](../Assets/Editor/Siege/Validation/Rules/SiegeTransientEffectChecks.cs#L23) | public static |
| SiegeTransientEffectChecks | [`void CompleteUnload()`](../Assets/Editor/Siege/Validation/Rules/SiegeTransientEffectChecks.cs#L145) | private static |
| SiegeTransientEffectChecks | [`T Read(object owner, string field)`](../Assets/Editor/Siege/Validation/Rules/SiegeTransientEffectChecks.cs#L174) | private static |
| SiegeTransientEffectChecks | [`void Expire(MonoBehaviour fade)`](../Assets/Editor/Siege/Validation/Rules/SiegeTransientEffectChecks.cs#L179) | private static |
| SiegeTransientEffectChecks | [`void Check(string label, bool passed)`](../Assets/Editor/Siege/Validation/Rules/SiegeTransientEffectChecks.cs#L186) | private static |

## Assets/Editor/Siege/Validation/SiegeCleanupChecks.cs

// 职责：验证真实扫描组件的停留期、幂等掉落、阵营/场景保护与暂停模拟下回收。 // 入口：RunLive()；隔离临时场景与真实宿主，恢复回收参数/随机数，绝不保存玩家数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCleanupChecks | [`void RunTiming()`](../Assets/Editor/Siege/Validation/SiegeCleanupChecks.cs#L24) | public static |
| SiegeCleanupChecks | [`IEnumerator WaitAndMark(IEnumerator productionWait)`](../Assets/Editor/Siege/Validation/SiegeCleanupChecks.cs#L49) | private static |
| SiegeCleanupChecks | [`void TimingTick()`](../Assets/Editor/Siege/Validation/SiegeCleanupChecks.cs#L55) | private static |
| SiegeCleanupChecks | [`string RunLive()`](../Assets/Editor/Siege/Validation/SiegeCleanupChecks.cs#L107) | public static |
| SiegeCleanupChecks | [`ControlUnit Create(Transform root, string name, UnitFaction faction, int count)`](../Assets/Editor/Siege/Validation/SiegeCleanupChecks.cs#L262) | private static |
| SiegeCleanupChecks | [`void Check(Report report, bool passed, string name)`](../Assets/Editor/Siege/Validation/SiegeCleanupChecks.cs#L281) | private static |

## Assets/Editor/Siege/Validation/SiegeIslandChecks.cs

// 职责：验证浮空岛确定性、空气边界、基地支撑及DestroyManager到地形网格/碰撞的生产路径。 // 入口：RunData() / RunLive()；实战检查仅用于临时示例会话，会在基地外制造一个坑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeIslandChecks | [`string RunData()`](../Assets/Editor/Siege/Validation/SiegeIslandChecks.cs#L22) | public static |
| SiegeIslandChecks | [`string RunLive()`](../Assets/Editor/Siege/Validation/SiegeIslandChecks.cs#L81) | public static |
| SiegeIslandChecks | [`string RunBoundary()`](../Assets/Editor/Siege/Validation/SiegeIslandChecks.cs#L181) | public static |
| SiegeIslandChecks | [`string Save(Report report, string name)`](../Assets/Editor/Siege/Validation/SiegeIslandChecks.cs#L232) | private static |
| SiegeIslandChecks | [`void Check(Report report, string name, bool condition)`](../Assets/Editor/Siege/Validation/SiegeIslandChecks.cs#L240) | private static |

## Assets/Editor/Simulation/GameModeAuthoring.cs

// 职责：将Main中的世界会话迁移为探索副场景，保存模式入口与独立阵地战制作骨架。 // 入口：Install()；保留已有世界Prefab覆盖、Main界面布局和旧Siege场景，重复执行不重建资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GameModeAuthoring | [`void Install()`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L18) | public static |
| GameModeAuthoring | [`GameModePanel CreateModePanel(Scene main)`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L117) | private static |
| GameModeAuthoring | [`void EnsureValidationTemplate()`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L168) | private static |
| GameModeAuthoring | [`void EnsureCombatPools(Scene main)`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L193) | private static |
| GameModeAuthoring | [`void CopyPool(Scene main, Scene target)`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L212) | private static |
| GameModeAuthoring | [`void CreateSiegeScene(Scene main)`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L227) | private static |
| GameModeAuthoring | [`RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L290) | private static |
| GameModeAuthoring | [`Text TextAt( string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize)`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L300) | private static |
| GameModeAuthoring | [`Button ButtonAt(string name, Transform parent, string title, Vector2 position, Vector2 size)`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L318) | private static |
| GameModeAuthoring | [`void Set(SerializedObject data, string name, Object value)`](../Assets/Editor/Simulation/GameModeAuthoring.cs#L335) | private static |

## Assets/Editor/Simulation/GameModePlayChecks.cs

// 职责：在真实Main Play Mode验证副场景切换、取消/重试、建造原稿隔离和旧Siege不被启动。 // 入口：Run()；只修改运行时夹具，报告写Temp/GameModes，不写玩家存档或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GameModePlayChecks | [`void Run()`](../Assets/Editor/Simulation/GameModePlayChecks.cs#L35) | public static |
| GameModePlayChecks | [`void Changed(PlayModeStateChange state)`](../Assets/Editor/Simulation/GameModePlayChecks.cs#L47) | private static |
| GameModePlayChecks | [`void Tick()`](../Assets/Editor/Simulation/GameModePlayChecks.cs#L67) | private static |
| GameModePlayChecks | [`IEnumerator Scenario()`](../Assets/Editor/Simulation/GameModePlayChecks.cs#L93) | private static |
| GameModePlayChecks | [`void Check(bool success, string description)`](../Assets/Editor/Simulation/GameModePlayChecks.cs#L314) | private static |
| GameModePlayChecks | [`void LogError(string message, string stack, LogType type)`](../Assets/Editor/Simulation/GameModePlayChecks.cs#L324) | private static |
| GameModePlayChecks | [`void Finish()`](../Assets/Editor/Simulation/GameModePlayChecks.cs#L332) | private static |

## Assets/Editor/World/Buildings/WorldBuildingAuthoring.cs

// 职责：为独立 WorldLab 安装建筑库、示例模板和场景组合；已有模板与布局不覆盖。 // 入口：Tools/HY-Sandbox/World/Install building integration；只保存 WorldLab 与 Assets/WorldLab/Buildings。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingAuthoring | [`void Install()`](../Assets/Editor/World/Buildings/WorldBuildingAuthoring.cs#L12) | public static |
| WorldBuildingAuthoring | [`BlockDataList CreateSample()`](../Assets/Editor/World/Buildings/WorldBuildingAuthoring.cs#L93) | public static |
| WorldBuildingAuthoring | [`void Add(BlockDataList blueprint, string resource, Vector3 position)`](../Assets/Editor/World/Buildings/WorldBuildingAuthoring.cs#L107) | private static |
| WorldBuildingAuthoring | [`Vector3 FindFlatPosition(ProceduralVoxelWorld world, BlockDataList blueprint)`](../Assets/Editor/World/Buildings/WorldBuildingAuthoring.cs#L113) | private static |

## Assets/Editor/World/Outposts/OutpostContentAuthoring.cs

// 职责：制作三套普通建筑与三套敌机轮廓底座JSON，保留可重建的项目源。 // 入口：Bake()；只写项目Resources，不修改用户蓝图或存档；已有JSON保留，编辑后可单独删除重建。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostContentAuthoring | [`void Bake()`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L12) | public static |
| OutpostContentAuthoring | [`void Rebuild()`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L18) | public static |
| OutpostContentAuthoring | [`void Bake(bool overwrite)`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L24) | private static |
| OutpostContentAuthoring | [`void ExportMatchingBases()`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L60) | public static |
| OutpostContentAuthoring | [`BlockDataList Base(int width, int length)`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L89) | private static |
| OutpostContentAuthoring | [`BlockDataList Workshop()`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L109) | private static |
| OutpostContentAuthoring | [`BlockDataList Storehouse()`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L146) | private static |
| OutpostContentAuthoring | [`BlockDataList Ruins()`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L173) | private static |
| OutpostContentAuthoring | [`void Loot(BlockDataList data, string resource, Vector3 position, CargoKind kind, int amount)`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L202) | private static |
| OutpostContentAuthoring | [`void Add( BlockDataList data, string resource, Vector3 position, int x = 0, int y = 0, int z = 0)`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L217) | private static |
| OutpostContentAuthoring | [`Vector3 SnapPosition(Vector3 position, int x, int y, int z)`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L234) | private static |
| OutpostContentAuthoring | [`void UpgradeGeometry()`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L243) | public static |
| OutpostContentAuthoring | [`string CreateStructuralPart(int x, int y, int z)`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L289) | private static |
| OutpostContentAuthoring | [`void Write(string name, BlockDataList data, bool overwrite)`](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L329) | private static |

## Assets/Editor/World/Outposts/OutpostHudAuthoring.cs

// 职责：为Main保存据点状态HUD；世界会话运行时由EnemySpawner自身门控全局刷怪。 // 入口：Apply()；只新增所属HUD，保留既有界面布局。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostHudAuthoring | [`void Apply()`](../Assets/Editor/World/Outposts/OutpostHudAuthoring.cs#L13) | public static |

## Assets/Editor/World/Player/WorldPlayerBuildChecks.cs

// 职责：构建D3D11发布验证包或独立Development制作工具验证包，结果写Temp，不覆盖用户发布包。 // 入口：Build()/BuildAuthoring()从已保存Main的Edit Mode调用，分别写Builds下两个验证目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldPlayerBuildChecks | [`void Build()`](../Assets/Editor/World/Player/WorldPlayerBuildChecks.cs#L14) | public static |
| WorldPlayerBuildChecks | [`void BuildNow()`](../Assets/Editor/World/Player/WorldPlayerBuildChecks.cs#L26) | private static |
| WorldPlayerBuildChecks | [`void BuildAuthoring()`](../Assets/Editor/World/Player/WorldPlayerBuildChecks.cs#L34) | public static |
| WorldPlayerBuildChecks | [`void Schedule(Action action)`](../Assets/Editor/World/Player/WorldPlayerBuildChecks.cs#L45) | private static |
| WorldPlayerBuildChecks | [`void BuildAuthoringNow()`](../Assets/Editor/World/Player/WorldPlayerBuildChecks.cs#L70) | private static |
| WorldPlayerBuildChecks | [`void BuildPlayer(string output, string reportPath, BuildOptions options)`](../Assets/Editor/World/Player/WorldPlayerBuildChecks.cs#L78) | private static |

## Assets/Editor/World/Terrain/ProceduralVoxelWorldEditor.cs

// 职责：为选中的程序化世界提供Undo预览、重生成与真实网格Bake入口。 // 入口：ProceduralVoxelWorld Inspector；只写所选世界及WorldLab网格目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProceduralVoxelWorldEditor | [`void OnInspectorGUI()`](../Assets/Editor/World/Terrain/ProceduralVoxelWorldEditor.cs#L9) | public override |

## Assets/Editor/World/Validation/Outposts/OutpostContentChecks.cs

// 职责：在Main Play隔离物理场景逐套验证建筑模型/连接/供电/驻军数据并渲染预览。 // 入口：Run()；仅写Temp，不保存场景或用户数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostContentChecks | [`string Run()`](../Assets/Editor/World/Validation/Outposts/OutpostContentChecks.cs#L14) | public static |
| OutpostContentChecks | [`void Check(List<string> checks, string name, bool passed)`](../Assets/Editor/World/Validation/Outposts/OutpostContentChecks.cs#L157) | private static |
| OutpostContentChecks | [`void Render(BuildingRuntime building, string path)`](../Assets/Editor/World/Validation/Outposts/OutpostContentChecks.cs#L167) | private static |

## Assets/Editor/World/Validation/Outposts/OutpostResidentChecks.cs

// 职责：在隔离真实流式地形验证停机显示、原位激活和击败后不复活的据点生命周期。 // 入口：Run()由建筑编辑Play探针逐帧驱动；finally清理实例，不写用户数据或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostResidentChecks | [`IEnumerator Run(Action<string, bool> check)`](../Assets/Editor/World/Validation/Outposts/OutpostResidentChecks.cs#L12) | public static |

## Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs

// 职责：在 WorldLab 真实物理帧验证建筑放置、战斗、事务恢复、支撑坍塌和输入预览。 // 入口：Run()；临时文件仓/目录自包含，结束恢复运行状态；报告只写 Temp/Reuse，不覆盖历史验证。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProbeFixture | [`void Install()`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L63) | public |
| ProbeFixture | [`void Dispose()`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L98) | public |
| WorldBuildingPlayProbe | [`void Run()`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L139) | public static |
| WorldBuildingPlayProbe | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L151) | private static |
| WorldBuildingPlayProbe | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L169) | private static |
| WorldBuildingPlayProbe | [`void Tick()`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L177) | private static |
| WorldBuildingPlayProbe | [`void Check(bool result, string description)`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L206) | private static |
| WorldBuildingPlayProbe | [`IEnumerator Scenario()`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L216) | private static |
| WorldBuildingPlayProbe | [`void InvokeUpdate(MonoBehaviour behaviour)`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L521) | private static |
| WorldBuildingPlayProbe | [`void ExpectRejected(WorldBuildingManager manager, WorldLabSnapshot snapshot, string name)`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L526) | private static |
| WorldBuildingPlayProbe | [`void Finish()`](../Assets/Editor/World/Validation/Session/WorldBuildingPlayProbe.cs#L541) | private static |

## Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs

// 职责：用运行时配置副本验证建筑预算延期、高速区块等待及脱锚建筑休眠恢复。 // 入口：Run()；只写Temp报告，不保存场景、配置资产或玩家存档，结束后退出Play Mode。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionEdgeChecks | [`void Run()`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L45) | public static |
| WorldSessionEdgeChecks | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L59) | private static |
| WorldSessionEdgeChecks | [`void Tick()`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L81) | private static |
| WorldSessionEdgeChecks | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L107) | private static |
| WorldSessionEdgeChecks | [`void Check(bool condition, string description)`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L115) | private static |
| WorldSessionEdgeChecks | [`IEnumerator Scenario()`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L125) | private static |
| WorldSessionEdgeChecks | [`void BindProfile(WorldGenerationSettings value)`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L307) | private static |
| WorldSessionEdgeChecks | [`void ConfigureProfile(int loadRadius, int unloadRadius, float buildingChance)`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L314) | private static |
| WorldSessionEdgeChecks | [`void CreatePlayer()`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L328) | private static |
| WorldSessionEdgeChecks | [`Vector3 ReadGuardVector(object guard, string field)`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L360) | private static |
| WorldSessionEdgeChecks | [`void InvokeSessionPhysics()`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L365) | private static |
| WorldSessionEdgeChecks | [`void CheckSessionHealthy(double timeout, string operation)`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L370) | private static |
| WorldSessionEdgeChecks | [`void Finish(bool exitPlayMode)`](../Assets/Editor/World/Validation/Session/WorldSessionEdgeChecks.cs#L378) | private static |

## Assets/Editor/World/Validation/Session/WorldSessionPerformanceChecks.cs

// 职责：在真实 Main Play Mode 采样固定种子的准备、切换、静止和跨区块帧耗时。 // 入口：Run(label)；只使用运行时配置副本和测试载具，不保存玩家数据。 // 生命周期：临时启用后台帧推进；完成、中止和超时均恢复后台运行与原Profiler配置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionPerformanceChecks | [`void Run(string label)`](../Assets/Editor/World/Validation/Session/WorldSessionPerformanceChecks.cs#L35) | public static |
| WorldSessionPerformanceChecks | [`void OnState(PlayModeStateChange state)`](../Assets/Editor/World/Validation/Session/WorldSessionPerformanceChecks.cs#L52) | private static |
| WorldSessionPerformanceChecks | [`void Tick()`](../Assets/Editor/World/Validation/Session/WorldSessionPerformanceChecks.cs#L91) | private static |
| WorldSessionPerformanceChecks | [`void Finish(bool exitPlayMode, bool completed)`](../Assets/Editor/World/Validation/Session/WorldSessionPerformanceChecks.cs#L126) | private static |
| WorldSessionPerformanceChecks | [`void Next(string name)`](../Assets/Editor/World/Validation/Session/WorldSessionPerformanceChecks.cs#L152) | private static |
| WorldSessionPerformanceChecks | [`void WriteCpuSamples()`](../Assets/Editor/World/Validation/Session/WorldSessionPerformanceChecks.cs#L170) | private static |
| WorldSessionPerformanceChecks | [`IEnumerator Scenario()`](../Assets/Editor/World/Validation/Session/WorldSessionPerformanceChecks.cs#L197) | private static |

## Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs

// 职责：在真实 Main Play Mode 验证后台预热、流式卸载、会话内修改及退出清空。 // 入口：Run()；只操作运行时测试对象，报告写 Temp/WorldSession，不保存玩家数据或场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionPlayChecks | [`void Run()`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L36) | public static |
| WorldSessionPlayChecks | [`void StateChanged(PlayModeStateChange state)`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L50) | private static |
| WorldSessionPlayChecks | [`void Tick()`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L70) | private static |
| WorldSessionPlayChecks | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L96) | private static |
| WorldSessionPlayChecks | [`void Check(bool condition, string description)`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L104) | private static |
| WorldSessionPlayChecks | [`bool IsPopulatedBuilding(string path)`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L114) | private static |
| WorldSessionPlayChecks | [`IEnumerator Scenario()`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L129) | private static |
| WorldSessionPlayChecks | [`IEnumerator StreamingChecks(WorldGenerationSettings settings)`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L458) | private static |
| WorldSessionPlayChecks | [`bool NeighborhoodReady(ProceduralVoxelWorld world, Vector2Int center)`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L566) | private static |
| WorldSessionPlayChecks | [`void Finish(bool exitPlayMode)`](../Assets/Editor/World/Validation/Session/WorldSessionPlayChecks.cs#L582) | private static |

## Assets/Editor/World/Validation/Terrain/VoxelMeshingChecks.cs

// 职责：以逐体素暴露面为独立基准，验证贪心网格覆盖、材质、法线、接缝与快照隔离。 // 入口：Run()；只创建临时数据/Mesh，报告保存到Temp，不修改场景和玩家数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelMeshingChecks | [`string Run()`](../Assets/Editor/World/Validation/Terrain/VoxelMeshingChecks.cs#L12) | public static |
| VoxelMeshingChecks | [`void Verify(VoxelWorldData data, Vector3Int start, int size)`](../Assets/Editor/World/Validation/Terrain/VoxelMeshingChecks.cs#L61) | private static |
| VoxelMeshingChecks | [`string Key(Vector3Int cell, Vector3Int normal, int material)`](../Assets/Editor/World/Validation/Terrain/VoxelMeshingChecks.cs#L136) | private static |
| VoxelMeshingChecks | [`void Check(bool valid, string message)`](../Assets/Editor/World/Validation/Terrain/VoxelMeshingChecks.cs#L141) | private static |

## Assets/Editor/World/Validation/Terrain/WorldTerrainIntegrationChecks.cs

// 职责：验证真实建筑库的旋转占地支撑、自然边缘恢复、密度与确定性；保存可复查报告。 // 入口：Run()在Edit Mode执行；不修改用户蓝图、场景或配置资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldTerrainIntegrationChecks | [`string Run()`](../Assets/Editor/World/Validation/Terrain/WorldTerrainIntegrationChecks.cs#L13) | public static |
| WorldTerrainIntegrationChecks | [`int CountSites(WorldGenerationSettings settings, List<BlockDataList> templates)`](../Assets/Editor/World/Validation/Terrain/WorldTerrainIntegrationChecks.cs#L103) | private static |
| WorldTerrainIntegrationChecks | [`void Require(bool valid, string message)`](../Assets/Editor/World/Validation/Terrain/WorldTerrainIntegrationChecks.cs#L121) | private static |

## Assets/Editor/World/Validation/WorldLab/WorldLabChecks.cs

// 职责：检查种子、网格边界与真实PhysX破坏/填补/炮弹命中，并将结果写入验证文件。 // 入口：RunDataChecks() / RunPlayChecks()；只在独立WorldLab执行运行检查，不操作玩家存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabChecks | [`void Check(bool result, string name)`](../Assets/Editor/World/Validation/WorldLab/WorldLabChecks.cs#L12) | private static |
| WorldLabChecks | [`string RunDataChecks()`](../Assets/Editor/World/Validation/WorldLab/WorldLabChecks.cs#L22) | public static |
| WorldLabChecks | [`void Clear(VoxelWorldData data)`](../Assets/Editor/World/Validation/WorldLab/WorldLabChecks.cs#L66) | private static |
| WorldLabChecks | [`string RunPlayChecks()`](../Assets/Editor/World/Validation/WorldLab/WorldLabChecks.cs#L81) | public static |
| WorldLabChecks | [`string Save(string phase)`](../Assets/Editor/World/Validation/WorldLab/WorldLabChecks.cs#L176) | private static |

## Assets/Editor/World/Validation/WorldLab/WorldLabCollapseChecks.cs

// 职责：验证六邻接支撑、尺寸/数量上限及真实世界片段转刚体、再生清理。 // 入口：RunData() / RunPlay()；动态检查仅在WorldLab，真实下落由配套跨帧探针记录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabCollapseChecks | [`void Check(bool value, string name)`](../Assets/Editor/World/Validation/WorldLab/WorldLabCollapseChecks.cs#L13) | private static |
| WorldLabCollapseChecks | [`string RunData()`](../Assets/Editor/World/Validation/WorldLab/WorldLabCollapseChecks.cs#L23) | public static |
| WorldLabCollapseChecks | [`string RunPlay()`](../Assets/Editor/World/Validation/WorldLab/WorldLabCollapseChecks.cs#L54) | public static |
| WorldLabCollapseChecks | [`string Save(string phase)`](../Assets/Editor/World/Validation/WorldLab/WorldLabCollapseChecks.cs#L102) | private static |

## Assets/Editor/World/Validation/WorldLab/WorldLabPerformanceChecks.cs

// 职责：在WorldLab测量真实生成、球形破坏和脏块网格/碰撞重建的同步CPU成本。 // 入口：Run()；固定种子/爆点和样本记录，不把微基准当作帧率或GPU性能。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabPerformanceChecks | [`string Run()`](../Assets/Editor/World/Validation/WorldLab/WorldLabPerformanceChecks.cs#L13) | public static |
| WorldLabPerformanceChecks | [`double Median(double[] values)`](../Assets/Editor/World/Validation/WorldLab/WorldLabPerformanceChecks.cs#L80) | private static |

## Assets/Editor/World/Validation/WorldLab/WorldLabPersistenceChecks.cs

// 职责：验证差量保存/读取、原子替换备份与损坏文件拒绝，使用独立临时路径。 // 入口：Run()；不读写玩家或WorldLab真实存档；结束删除本次测试文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabPersistenceChecks | [`string Run()`](../Assets/Editor/World/Validation/WorldLab/WorldLabPersistenceChecks.cs#L9) | public static |

## Assets/Editor/World/Validation/WorldLab/WorldLabSoakChecks.cs

// 职责：在真实帧后台持续爆破/重生成，周期落盘并检查分块/网格有界及输入遮罩下帧推进。 // 入口：Begin(minutes)；最多15分钟，Stop()或离开Play安全结束，不接触真实玩家存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabSoakChecks | [`string Begin(int minutes = 15)`](../Assets/Editor/World/Validation/WorldLab/WorldLabSoakChecks.cs#L28) | public static |
| WorldLabSoakChecks | [`void Tick()`](../Assets/Editor/World/Validation/WorldLab/WorldLabSoakChecks.cs#L55) | private static |
| WorldLabSoakChecks | [`void Stop()`](../Assets/Editor/World/Validation/WorldLab/WorldLabSoakChecks.cs#L138) | public static |
| WorldLabSoakChecks | [`void Finish(bool complete)`](../Assets/Editor/World/Validation/WorldLab/WorldLabSoakChecks.cs#L147) | private static |
| WorldLabSoakChecks | [`void WriteCheckpoint()`](../Assets/Editor/World/Validation/WorldLab/WorldLabSoakChecks.cs#L162) | private static |

## Assets/Editor/World/WorldLab/WorldLabAuthoring.cs

// 职责：创建独立WorldLab场景并将生成网格保存为真实资产；Inspector支持种子与分块参数。 // 入口：Tools/HY-Sandbox/World/Create or open World Lab；不保存或改写Main。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabAuthoring | [`void CreateOrOpen()`](../Assets/Editor/World/WorldLab/WorldLabAuthoring.cs#L13) | public static |
| WorldLabAuthoring | [`Material MaterialAt(string name, Color color, float metallic)`](../Assets/Editor/World/WorldLab/WorldLabAuthoring.cs#L79) | private static |
| WorldLabAuthoring | [`void BakeMeshes(ProceduralVoxelWorld world)`](../Assets/Editor/World/WorldLab/WorldLabAuthoring.cs#L99) | public static |

## Assets/Editor/World/WorldSessionAuthoring.cs

// 职责：保留世界配置与制作入口，安装委托模式场景迁移，预览通过正式模式入口加载探索。 // 入口：Install()；重复执行保留配置、Prefab 参数及已存在的界面布局。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionAuthoring | [`void Install()`](../Assets/Editor/World/WorldSessionAuthoring.cs#L13) | public static |
| WorldSessionAuthoring | [`void SelectConfiguration()`](../Assets/Editor/World/WorldSessionAuthoring.cs#L19) | public static |
| WorldSessionAuthoring | [`void RegeneratePreview()`](../Assets/Editor/World/WorldSessionAuthoring.cs#L25) | public static |

## Assets/Scripts/Features/Bootstrap/GameManager.cs

// 职责：保存当前方块父对象；直接Main启动默认存档，菜单启动时由显式蓝图请求接管初始化。 // 入口：Init()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GameManager | [`void Awake()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L11) | private |
| GameManager | [`void Start()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L17) | private |
| GameManager | [`void Init()`](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L25) | public static |

## Assets/Scripts/Features/Bootstrap/RuntimeObjectTemplates.cs

// 职责：保存数量由玩法决定的运行对象模板；组件组成在 Prefab 中制作，运行时只实例化。 // 入口：Instance；场景常驻服务直接放在 Managers，不通过此库自动创建。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs

// 职责：显式限定场景的服务查找，拒绝用对象创建顺序解决重复实例。 // 入口：FindInScene()/FindInActiveScene()；缺失返回null，同场景重复抛出明确错误。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SceneObjectQuery | [`T FindInActiveScene(FindObjectsInactive inactive = FindObjectsInactive.Exclude)`](../Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs#L9) | public static |
| SceneObjectQuery | [`T FindInScene(Scene scene, FindObjectsInactive inactive = FindObjectsInactive.Exclude)`](../Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs#L15) | public static |

## Assets/Scripts/Features/Bots/Bot.cs

// 职责：保存无人机任务/插槽配置并组织任务飞行、返航、离家和精确停靠状态。 // 入口：派生机器人的 FixedUpdate → TickNavigation() → NavigateToTarget()/NavigateHomeSmoothly()。 // 依赖：BotFlightMotion 处理飞行/制动；NavigationAvoidance 负责三维净空与脱困。 // 不变量：homeOffset 保持 Home 局部坐标；停靠切为运动学刚体，完成后恢复准确局部姿态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Bot | [`void Start()`](../Assets/Scripts/Features/Bots/Bot.cs#L53) | protected virtual |
| Bot | [`bool TickNavigation()`](../Assets/Scripts/Features/Bots/Bot.cs#L75) | protected |
| Bot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/Bot.cs#L89) | protected virtual |
| Bot | [`void OnDestroy()`](../Assets/Scripts/Features/Bots/Bot.cs#L108) | protected virtual |
| Bot | [`Power SetHomePowerRequested(bool requested)`](../Assets/Scripts/Features/Bots/Bot.cs#L114) | protected |
| Bot | [`bool CanLaunchForWork(bool requiresPower, bool requested)`](../Assets/Scripts/Features/Bots/Bot.cs#L128) | protected |
| Bot | [`void PrepareForHomeDestruction()`](../Assets/Scripts/Features/Bots/Bot.cs#L134) | public virtual |
| Bot | [`void InitializeTrail()`](../Assets/Scripts/Features/Bots/Bot.cs#L138) | private |
| Bot | [`void LateUpdate()`](../Assets/Scripts/Features/Bots/Bot.cs#L158) | private |
| Bot | [`void NavigateToTarget(Transform target)`](../Assets/Scripts/Features/Bots/Bot.cs#L171) | protected |
| Bot | [`void NavigateHomeSmoothly()`](../Assets/Scripts/Features/Bots/Bot.cs#L191) | protected |
| Bot | [`Vector3 GetRelativeHomeVelocity(Vector3 worldPosition)`](../Assets/Scripts/Features/Bots/Bot.cs#L228) | private |
| Bot | [`void ReturnHome()`](../Assets/Scripts/Features/Bots/Bot.cs#L234) | private |
| Bot | [`void LeaveHome()`](../Assets/Scripts/Features/Bots/Bot.cs#L276) | private |
| Bot | [`void SetNavigationState(NavigationState nextState)`](../Assets/Scripts/Features/Bots/Bot.cs#L288) | private |
| Bot | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Bots/Bot.cs#L313) | private |

## Assets/Scripts/Features/Bots/Collection/CollectionBot.cs

// 职责：执行掉落物寻找、拾取、运输和入库，复用 Bot 导航。 // 入口：FixedUpdate() → 核对任务 → 寻找 / 拾取 / 返航交付。 // 生命周期：停用、销毁、Home 摧毁或任务失效时释放 LootDrop 认领；停靠后才入库。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CollectionBot | [`void Start()`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L22) | protected override |
| CollectionBot | [`void FixedUpdate()`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31) | private |
| CollectionBot | [`bool IsMissionInvalid(ControlUnit owner, bool canWork)`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L74) | private |
| CollectionBot | [`void CollectTarget()`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L79) | private |
| CollectionBot | [`void ReturnCargo()`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L88) | private |
| CollectionBot | [`void FindTarget(ControlUnit owner)`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L103) | private |
| CollectionBot | [`void Abandon()`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L140) | private |
| CollectionBot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L151) | protected override |
| CollectionBot | [`void OnDestroy()`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L157) | protected override |
| CollectionBot | [`void PrepareForHomeDestruction()`](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L163) | public override |

## Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs

// 职责：缓存导航采样、平滑飞行方向，执行任务飞行与移动 Home 的相对速度制动。 // 入口：NavigateToTarget()/NavigateHome()；只能由机器人 FixedUpdate 导航路径调用。 // 不变量：Home 目标点每次重新计算；只缓存避障建议，不缓存移动目标的世界位置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotFlightMotion | [`void ResetDirection()`](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L30) | public |
| BotFlightMotion | [`void ClearTarget()`](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L35) | public |
| BotFlightMotion | [`void NavigateToTarget(Transform target)`](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L43) | public |
| BotFlightMotion | [`void NavigateHome(Transform home, Vector3 approachPosition, Rigidbody homeRigidbody, float rangeScale)`](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L48) | public |
| BotFlightMotion | [`void NavigateToPosition( Vector3 targetPosition, Transform targetReference, Rigidbody homeRigidbody, bool returningHome, float avoidanceRangeScale)`](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L53) | private |
| BotFlightMotion | [`void SampleGuidance( Transform targetReference, Vector3 targetDirection, float targetDistance, float avoidanceRangeScale)`](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L71) | private |
| BotFlightMotion | [`void UpdateDirectionAndSpeed(Vector3 targetDirection)`](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L96) | private |
| BotFlightMotion | [`void ApplyMovement( Vector3 targetPosition, Vector3 targetDirection, float targetDistance, Rigidbody homeRigidbody, bool returningHome)`](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L114) | private |

## Assets/Scripts/Features/Bots/Navigation/BotNavigationPolicy.cs

// 职责：定义维修/回收 Bot 共用且不可由 Inspector/CSV 覆盖的导航规则。 // 契约：速度没有固定上限；只限制加速度、可制动距离及转弯净空，单位为米和秒。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationPolicy | [`NavigationAvoidanceSettings CreateAvoidanceSettings()`](../Assets/Scripts/Features/Bots/Navigation/BotNavigationPolicy.cs#L19) | public static |
| BotNavigationPolicy | [`void ConfigureBody(Rigidbody body)`](../Assets/Scripts/Features/Bots/Navigation/BotNavigationPolicy.cs#L35) | public static |

## Assets/Scripts/Features/Bots/Presentation/BotFlightEffects.cs

// 职责：按实际飞行速度更新已创作的飞行 VFX，并在停靠状态清理尾迹。 // 入口：Bot.LateUpdate() 调用 Update()；导航状态切换调用 SetDocked()。 // 不变量：只改变 VFX 的显示强度/启用状态，不修改刚体、导航或 Inspector 参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotFlightEffects | [`void Update( Rigidbody rb, bool flying, bool showTrail, float movementSpeed, VfxEffect trailRenderer, VfxEffect trailCoreRenderer, VfxEffect flightEffect)`](../Assets/Scripts/Features/Bots/Presentation/BotFlightEffects.cs#L8) | public static |
| BotFlightEffects | [`void SetDocked(bool docked, VfxEffect trailRenderer, VfxEffect trailCoreRenderer, VfxEffect flightEffect)`](../Assets/Scripts/Features/Bots/Presentation/BotFlightEffects.cs#L40) | public static |

## Assets/Scripts/Features/Bots/Presentation/BotNavigationGizmos.cs

// 职责：绘制最近一次避障采样、导航方向、速度和状态；仅显示，不修改机器人状态。 // 入口：Bot.OnDrawGizmos()；UnityEditor Handles 只在 Editor 编译分支使用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationGizmos | [`void Draw( Transform transform, Transform navigateTarget, Rigidbody rb, Bot.NavigationState currentState, BotFlightMotion motion, bool showAvoidanceRays, bool showDirectionVectors)`](../Assets/Scripts/Features/Bots/Presentation/BotNavigationGizmos.cs#L7) | public static |

## Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs

// 职责：初始化与显示维修光束、命中 VFX，并计算可见目标表面的光束落点。 // 入口：Initialize()/Update()/Hide()；只修改表现，维修计时和耐久规则由 RepairBot 持有。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RepairBeamView | [`void Initialize(GameObject gameObject, VfxEffect impact, float beamWidth)`](../Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs#L15) | public |
| RepairBeamView | [`void Hide()`](../Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs#L31) | public |
| RepairBeamView | [`void Update(bool active, Durability target, Vector3 origin, Gradient gradient)`](../Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs#L44) | public |
| RepairBeamView | [`Vector3 GetRepairTargetPoint(Durability target, Vector3 origin)`](../Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs#L72) | public |
| RepairBeamView | [`void UpdateRepairImpact(Vector3 targetPoint, Vector3 origin, float pulse)`](../Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs#L93) | private |
| RepairBeamView | [`void SetRepairImpactActive(bool active)`](../Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs#L110) | private |

## Assets/Scripts/Features/Bots/Repair/RepairBot.cs

// 职责：寻找受损友方设备并维修，复用 Bot 的导航和返航能力。 // 入口：FixedUpdate() → 搜索受损目标 / 导航并维修；ClearTarget() 取消当前维修。 // 依赖：RepairTargetSearch 扫描归属与范围；RepairBeamView 显示光束与命中效果。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RepairBot | [`void Start()`](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L27) | protected override |
| RepairBot | [`void FixedUpdate()`](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L37) | private |
| RepairBot | [`void OnDisable()`](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L89) | protected override |
| RepairBot | [`void FindDamagedBlock()`](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L95) | private |
| RepairBot | [`void CheckAndRepair()`](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L108) | private |
| RepairBot | [`void UpdateRepairBeam(bool active)`](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L149) | private |
| RepairBot | [`void EnsureRepairBeamGradient()`](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L154) | private |
| RepairBot | [`void ClearTarget()`](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L167) | public |

## Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs

// 职责：扫描 Home 范围内的友方耐久对象，并选择距离机器人最近的受损目标。 // 入口：Scan()/FindClosestDamaged()；IsValidTarget() 用于执行维修前重新核对归属与范围。 // 生命周期：非分配碰撞缓冲按需扩展至 1024；多个 Collider 归属的耐久对象只记录一次。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RepairTargetSearch | [`void Scan(Transform home, int targetRange)`](../Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs#L15) | public |
| RepairTargetSearch | [`Durability FindClosestDamaged(Vector3 position)`](../Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs#L58) | public |
| RepairTargetSearch | [`bool IsValidTarget(Durability target, Transform home, int targetRange)`](../Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs#L80) | public static |

## Assets/Scripts/Features/Building/Actions/ActionManager.cs

// 职责：管理建造动作的 Undo/Redo 栈与操作计数。 // 入口：Push() / Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ActionManager | [`void Awake()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L18) | private |
| ActionManager | [`void Push(IBlockAction action)`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L26) | public |
| ActionManager | [`void Undo()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L37) | public |
| ActionManager | [`void Redo()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L50) | public |
| ActionManager | [`void Clear()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L60) | public |
| ActionManager | [`(IBlockAction[] Undo, IBlockAction[] Redo) CaptureHistory()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L66) | internal |
| ActionManager | [`void RestoreHistory((IBlockAction[] Undo, IBlockAction[] Redo) history)`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L71) | internal |
| ActionManager | [`void CountAction(IBlockAction action)`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L88) | private |
| ActionManager | [`int GetTotalActionCount()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L103) | public |
| ActionManager | [`int GetActionCount(string actionName)`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L107) | public |
| ActionManager | [`void ShowDebug()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L112) | public |
| ActionManager | [`int GetUndoCount()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L118) | public |
| ActionManager | [`int GetRedoCount()`](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L120) | public |

## Assets/Scripts/Features/Building/Actions/AddBlockAction.cs

// 职责：记录创建动作，委托共享方块状态完成删除与恢复。 // 入口：Undo() / Redo()；撤销时捕获当前货仓，重做沿用同一方块身份。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CreateBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/Actions/AddBlockAction.cs#L13) | public |
| CreateBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/Actions/AddBlockAction.cs#L18) | public |

## Assets/Scripts/Features/Building/Actions/BlockActionState.cs

// 职责：保存建造动作的稳定方块身份、精确世界姿态和货仓，统一删除与蓝图工厂恢复。 // 入口：Remove() / Restore()；创建和删除动作只选择操作方向，当前建造目标切换时由ActionManager清空。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockActionState | [`void Remove(bool captureCargo)`](../Assets/Scripts/Features/Building/Actions/BlockActionState.cs#L25) | internal |
| BlockActionState | [`void Restore()`](../Assets/Scripts/Features/Building/Actions/BlockActionState.cs#L43) | internal |

## Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs

// 职责：记录删除动作，委托共享方块状态完成恢复与删除。 // 入口：Undo() / Redo()；货仓与姿态来自删除时快照，撤销沿用同一方块身份。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DeleteBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs#L13) | public |
| DeleteBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs#L18) | public |

## Assets/Scripts/Features/Building/Actions/GroupAction.cs

// 职责：将多项建造动作组成一次可撤销/重做的事务。 // 入口：Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GroupAction | [`void Undo()`](../Assets/Scripts/Features/Building/Actions/GroupAction.cs#L15) | public |
| GroupAction | [`void Redo()`](../Assets/Scripts/Features/Building/Actions/GroupAction.cs#L24) | public |

## Assets/Scripts/Features/Building/Actions/IBlockAction.cs

// 职责：约定建造动作的撤销、重做和计数名称。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/Actions/IBlockAction.cs#L4) |  |
| IBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/Actions/IBlockAction.cs#L5) |  |

## Assets/Scripts/Features/Building/Actions/MoveBlockAction.cs

// 职责：保存移动前后位置，撤销/重做后同步方块存档。 // 入口：Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MoveBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/Actions/MoveBlockAction.cs#L19) | public |
| MoveBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/Actions/MoveBlockAction.cs#L29) | public |

## Assets/Scripts/Features/Building/Actions/RotateBlockAction.cs

// 职责：保存旋转前后姿态及位置，撤销/重做后同步存档。 // 入口：Undo() / Redo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RotateBlockAction | [`void Undo()`](../Assets/Scripts/Features/Building/Actions/RotateBlockAction.cs#L23) | public |
| RotateBlockAction | [`void Redo()`](../Assets/Scripts/Features/Building/Actions/RotateBlockAction.cs#L34) | public |

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
| BlockDataTableSchema | [`void AddFloat( List<Column> columns, string group, string property, string label, double minimum = 0, double maximum = double.PositiveInfinity)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L117) | private static |
| BlockDataTableSchema | [`void AddInteger( List<Column> columns, string group, string property, string label, double minimum = 0, double maximum = double.PositiveInfinity)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L136) | private static |
| BlockDataTableSchema | [`void AddBoolean(List<Column> columns, string group, string property, string label)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L155) | private static |
| BlockDataTableSchema | [`void AddEnum(List<Column> columns, string group, string property, string label, string[] names)`](../Assets/Scripts/Features/Building/BlockDataTables/BlockDataTableSchema.cs#L168) | private static |

## Assets/Scripts/Features/Building/BlueprintLoading/BaseBlueprintMigration.cs

// 职责：将旧建筑库中已标记核心的基地迁移到独立目录，保留原始字节备份且不覆盖同名设计。 // 入口：Run()；复制校验完成后才归档源文件，重复执行可安全恢复中断的迁移。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BaseBlueprintMigration | [`void Run(string buildings, string bases)`](../Assets/Scripts/Features/Building/BlueprintLoading/BaseBlueprintMigration.cs#L11) | public static |

## Assets/Scripts/Features/Building/BlueprintLoading/BlockData.cs

// 职责：定义方块 JSON 存档字段及方块与数据的转换，包括货仓内容。

数据/配置/接口定义，无方法体。

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
| BlueprintBlockFactory | [`void ApplyFaction(Block block, UnitFaction faction)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L68) | public static |
| BlueprintBlockFactory | [`string ToResourcePath(string fullPath)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L76) | public static |

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
| BlueprintLoadCamera | [`Bounds CalculateBlockDataBounds(BlockData data)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L136) | private |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadSettings.cs

// 职责：保存一次蓝图加载所使用的延时与镜头参数快照。 // 来源：BuildManager 的现有 Inspector 字段；不改变场景或 Prefab 的字段身份。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintLoadSettings | [`float GetInterval(double elapsedSeconds)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadSettings.cs#L21) | public |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs

// 职责：管理逐帧蓝图恢复、请求所有权、取消、统计及连接重建。 // 入口：Load() / Cancel()；依赖由 BuildManager 装配，UI 通过 StatisticsChanged 消费结果。 // 生命周期：新请求使旧版本失效；结束、取消及异常均释放镜头与会话状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintLoader | [`void Load( BuildTargetContext target, GameObject parentPrefab, BlueprintLoadSettings settings, Camera camera, Action createInitialBlock, Action<Transform, BlockDataList> prepareParent = null)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L43) | public |
| BlueprintLoader | [`bool IsLoadingTarget(BuildTargetContext target)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L93) | public |
| BlueprintLoader | [`void Cancel()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L99) | public |
| BlueprintLoader | [`IEnumerator LoadRoutine(int version, List<BlockData> blocks, BlueprintLoadSettings settings, Action createInitialBlock)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L112) | private |
| BlueprintLoader | [`bool TryRead(string path, out BlockDataList data)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L207) | private |
| BlueprintLoader | [`void ReplaceParent(GameObject prefab, string name)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L246) | private |
| BlueprintLoader | [`bool OwnsParent(int version)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L267) | private |
| BlueprintLoader | [`void RemoveMissingData(HashSet<string> ids)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L272) | private |
| BlueprintLoader | [`IEnumerator RebuildConnections(int version)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L283) | private |
| BlueprintLoader | [`void ReleaseInvalidParent()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L301) | private |
| BlueprintLoader | [`void FinishSession()`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L321) | private |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs

// 职责：复制蓝图模块的静态模型与材质，显示完整外形而不实例化战斗、连接或物理脚本。 // 入口：Create()；返回根对象由调用者销毁，网格和材质共享原资源，不拥有资源本体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintModelPreview | [`GameObject Create(IReadOnlyList<BlockData> blocks, Transform parent, string name)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs#L8) | public static |
| BlueprintModelPreview | [`void CopyVisuals(Transform source, Transform target, bool root, Material material)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs#L31) | private static |

## Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs

// 职责：统一蓝图完整刷新与增量恢复的质量、耗电和发电统计。 // 入口：Accumulate() / Calculate()；不读取管理器单例，不更新 UI。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintStatistics | [`void Accumulate(Block block)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs#L26) | public |
| BlueprintStatistics | [`BlueprintStatistics Calculate(List<BlockData> savedBlocks, Transform parent)`](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs#L47) | public static |

## Assets/Scripts/Features/Building/BlueprintLoading/BuildTargetContext.cs

// 职责：定义玩家、敌方和建筑蓝图目标、名称、阵营及各自独立的存档路径。 // 入口：GetSavePath() / PlayerSave() / EnemyBlueprint()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildTargetContext | [`string GetSavePath(SaveManager saveManager)`](../Assets/Scripts/Features/Building/BlueprintLoading/BuildTargetContext.cs#L36) | public |
| BuildTargetContext | [`BuildTargetContext PlayerSave(string saveName, string enemyBlueprintName)`](../Assets/Scripts/Features/Building/BlueprintLoading/BuildTargetContext.cs#L41) | public static |
| BuildTargetContext | [`BuildTargetContext EnemyBlueprint(string saveName, string enemyBlueprintName)`](../Assets/Scripts/Features/Building/BlueprintLoading/BuildTargetContext.cs#L46) | public static |
| BuildTargetContext | [`BuildTargetContext BuildingBlueprint(string saveName, string enemyBlueprintName, string buildingBlueprintName)`](../Assets/Scripts/Features/Building/BlueprintLoading/BuildTargetContext.cs#L51) | public static |
| BuildTargetContext | [`BuildTargetContext BaseBlueprint(string saveName, string enemyBlueprintName, string name)`](../Assets/Scripts/Features/Building/BlueprintLoading/BuildTargetContext.cs#L56) | public static |
| BuildTargetContext | [`string NormalizeName(string name, string fallback)`](../Assets/Scripts/Features/Building/BlueprintLoading/BuildTargetContext.cs#L61) | private static |

## Assets/Scripts/Features/Building/BuildManager.cs

// 职责：协调建造输入、选择与放置；蓝图加载委托 Building 功能，保留现有 Inspector 契约。 // 入口：SetBuildMode() / ToggleEnemyBlueprintBuildMode() / ToggleBuildingBlueprintBuildMode()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildManager | [`void SelectMaterial(string id)`](../Assets/Scripts/Features/Building/BuildManager.cs#L46) | public |
| BuildManager | [`void RestoreWorkshopContext(BuildTargetContext context, BuildingFoundation foundation)`](../Assets/Scripts/Features/Building/BuildManager.cs#L83) | internal |
| BuildManager | [`void Awake()`](../Assets/Scripts/Features/Building/BuildManager.cs#L117) | private |
| BuildManager | [`void OnDisable()`](../Assets/Scripts/Features/Building/BuildManager.cs#L122) | private |
| BuildManager | [`void OnEnable()`](../Assets/Scripts/Features/Building/BuildManager.cs#L128) | private |
| BuildManager | [`void OnDestroy()`](../Assets/Scripts/Features/Building/BuildManager.cs#L133) | private |
| BuildManager | [`void Start()`](../Assets/Scripts/Features/Building/BuildManager.cs#L150) | private |
| BuildManager | [`void Update()`](../Assets/Scripts/Features/Building/BuildManager.cs#L156) | private |
| BuildManager | [`void SetBuildMode(bool lockView)`](../Assets/Scripts/Features/Building/BuildManager.cs#L221) | public |
| BuildManager | [`void ToggleEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Building/BuildManager.cs#L233) | public |
| BuildManager | [`void EnterEnemyBlueprintBuildMode(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L245) | public |
| BuildManager | [`void ExitEnemyBlueprintBuildMode(bool reloadPlayerSave)`](../Assets/Scripts/Features/Building/BuildManager.cs#L276) | public |
| BuildManager | [`void ExitDeveloperBlueprintBuildMode(bool reloadPlayerSave)`](../Assets/Scripts/Features/Building/BuildManager.cs#L284) | public |
| BuildManager | [`void ToggleBuildingBlueprintBuildMode()`](../Assets/Scripts/Features/Building/BuildManager.cs#L304) | public |
| BuildManager | [`void EnterBuildingBlueprintBuildMode(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L319) | public |
| BuildManager | [`void ToggleBaseBlueprintBuildMode()`](../Assets/Scripts/Features/Building/BuildManager.cs#L324) | public |
| BuildManager | [`void EnterBaseBlueprintBuildMode(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L337) | public |
| BuildManager | [`void EnterStructureBlueprintBuildMode(string blueprintName, bool siegeBase)`](../Assets/Scripts/Features/Building/BuildManager.cs#L342) | private |
| BuildManager | [`void PrepareBuildingFoundation(Transform parent, BlockDataList data)`](../Assets/Scripts/Features/Building/BuildManager.cs#L409) | private |
| BuildManager | [`void MarkBuildingFootprintDirty()`](../Assets/Scripts/Features/Building/BuildManager.cs#L430) | public |
| BuildManager | [`void CompleteBuildingBlueprint()`](../Assets/Scripts/Features/Building/BuildManager.cs#L438) | public |
| BuildManager | [`void RefreshBuildingFootprint()`](../Assets/Scripts/Features/Building/BuildManager.cs#L470) | public |
| BuildManager | [`void SetCurrentBuildingBlueprintName(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L487) | public |
| BuildManager | [`void SetCurrentBlockResource(string resourcePath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L496) | public |
| BuildManager | [`void SetCurrentSaveName(string saveName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L511) | public |
| BuildManager | [`void SetCurrentEnemyBlueprintName(string blueprintName)`](../Assets/Scripts/Features/Building/BuildManager.cs#L516) | public |
| BuildManager | [`void SetBuildType(BuildType buildType)`](../Assets/Scripts/Features/Building/BuildManager.cs#L521) | public |
| BuildManager | [`void EnsureBuildInteraction()`](../Assets/Scripts/Features/Building/BuildManager.cs#L548) | private |
| BuildManager | [`void HandleBuildingPreview()`](../Assets/Scripts/Features/Building/BuildManager.cs#L581) | private |
| BuildManager | [`void ClearCurrentGhost(bool hideHints = true)`](../Assets/Scripts/Features/Building/BuildManager.cs#L589) | private |
| BuildManager | [`void DeselectBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L597) | public |
| BuildManager | [`Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)`](../Assets/Scripts/Features/Building/BuildManager.cs#L604) | public |
| BuildManager | [`bool IsWithinBuildRange(Vector3 position)`](../Assets/Scripts/Features/Building/BuildManager.cs#L610) | public |
| BuildManager | [`void CreateBlock(GameObject prefab, string resourcePath, Vector3 pos, Quaternion rot)`](../Assets/Scripts/Features/Building/BuildManager.cs#L615) | public |
| BuildManager | [`Block CreateBlockFromTemplate( GameObject prefab, string resourcePath, Vector3 pos, Quaternion rot, Vector3Int? dimensions, HangarEntrance entrance = HangarEntrance.Front)`](../Assets/Scripts/Features/Building/BuildManager.cs#L620) | private |
| BuildManager | [`void DeleteBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L674) | public |
| BuildManager | [`void EnsureBlueprintEditor()`](../Assets/Scripts/Features/Building/BuildManager.cs#L695) | private |
| BuildManager | [`Block CreateDuplicateBlock(GameObject prefab, string path, Vector3 position, Quaternion rotation, Vector3Int dimensions)`](../Assets/Scripts/Features/Building/BuildManager.cs#L704) | private |
| BuildManager | [`void SaveBlock(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L716) | public |
| BuildManager | [`void RemoveBlock(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L734) | public |
| BuildManager | [`void LoadAllBlocks()`](../Assets/Scripts/Features/Building/BuildManager.cs#L742) | public |
| BuildManager | [`BlueprintStatistics GetBlueprintStatistics()`](../Assets/Scripts/Features/Building/BuildManager.cs#L771) | public |
| BuildManager | [`void EnsureBlueprintLoader()`](../Assets/Scripts/Features/Building/BuildManager.cs#L778) | private |
| BuildManager | [`void DisplayBlueprintStatistics(BlueprintStatistics statistics)`](../Assets/Scripts/Features/Building/BuildManager.cs#L787) | private |
| BuildManager | [`BlueprintLoadSettings CreateLoadSettings()`](../Assets/Scripts/Features/Building/BuildManager.cs#L795) | private |
| BuildManager | [`void ClearUnloadableData(string id)`](../Assets/Scripts/Features/Building/BuildManager.cs#L814) | public |
| BuildManager | [`bool IsLoadingBuildTarget( BuildTargetKind kind, string saveName, string enemyBlueprintName, string buildingBlueprintName = null)`](../Assets/Scripts/Features/Building/BuildManager.cs#L819) | public |
| BuildManager | [`bool IsLoadingBuildTarget(BuildTargetContext context)`](../Assets/Scripts/Features/Building/BuildManager.cs#L829) | private |
| BuildManager | [`void ClearUnloadableData(string id, string targetSavePath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L839) | private |
| BuildManager | [`void InitialBlock()`](../Assets/Scripts/Features/Building/BuildManager.cs#L848) | private |
| BuildManager | [`bool CanCreateBlock(GameObject prefab, out string reason)`](../Assets/Scripts/Features/Building/BuildManager.cs#L856) | private |
| BuildManager | [`void ApplyBlockBuildDefaults(Block block)`](../Assets/Scripts/Features/Building/BuildManager.cs#L895) | public |
| BuildManager | [`int CountCockpitsInCurrentConstruct()`](../Assets/Scripts/Features/Building/BuildManager.cs#L900) | private |
| BuildManager | [`void ResetBuildState()`](../Assets/Scripts/Features/Building/BuildManager.cs#L910) | private |
| BuildManager | [`void SetBuildTarget(BuildTargetContext context)`](../Assets/Scripts/Features/Building/BuildManager.cs#L920) | private |
| BuildManager | [`void RefreshBlueprintUI()`](../Assets/Scripts/Features/Building/BuildManager.cs#L939) | private |
| BuildManager | [`void StopActiveBlockLoad()`](../Assets/Scripts/Features/Building/BuildManager.cs#L947) | private |
| BuildManager | [`void CancelBlueprintLoad()`](../Assets/Scripts/Features/Building/BuildManager.cs#L952) | public |
| BuildManager | [`string ConvertToResourcesPath(string fullPath)`](../Assets/Scripts/Features/Building/BuildManager.cs#L957) | public static |

## Assets/Scripts/Features/Building/BuildingEditor/BuildingEditorPanel.cs

// 职责：显示自动占地和地面自由建造提示，建筑哨站控件独立保留。 // 入口：Refresh()；面板没有手动地基尺寸输入，实际范围在加载、退出编辑或明确完成时计算。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingEditorPanel | [`void Complete()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingEditorPanel.cs#L15) | public |
| BuildingEditorPanel | [`void Refresh()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingEditorPanel.cs#L32) | public |

## Assets/Scripts/Features/Building/BuildingEditor/BuildingFootprint.cs

// 职责：从真实旋转模块包围盒收紧建筑占地；只更新元数据，不平移模块或改变玩家铺设的平台。 // 入口：Calculate()；空设计保留默认小网格，安全上限由部署校验另行处理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingFootprint | [`BuildingFoundationData Calculate(IReadOnlyList<BlockData> blocks, int padding = 1)`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFootprint.cs#L8) | public static |

## Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs

// 职责：配置建筑薄平面底座、碰撞体及每格向上连接点；底座不进入普通方块存档或操作栈。 // 入口：Configure()；显示自动裁切网格，范围以外仍可在虚拟地面放置，不移动既有建筑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingFoundation | [`void Configure(BuildingFoundationData data)`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs#L17) | public |
| BuildingFoundation | [`void RefreshGeometry(Block block)`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs#L55) | internal |
| BuildingFoundation | [`void OnDestroy()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs#L88) | private |

## Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundationData.cs

// 职责：保存自动占地的网格范围；偏移缺省为零，保持旧蓝图 (-2, 0, -2) 原点契约。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingFoundationData | [`BuildingFoundationData Copy()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundationData.cs#L19) | public |

## Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs

// 职责：在建筑编辑面板配置据点类型、敌机蓝图快照和逐名驻军本地部署姿态。 // 入口：Refresh() / AddDefender() / SavePose() / RemoveDefender() / SetOutpost()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingOutpostEditor | [`void Refresh()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L35) | public |
| BuildingOutpostEditor | [`void AddTemplate(string name, string json)`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L76) | private |
| BuildingOutpostEditor | [`void CreateMatchingBase()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L96) | public |
| BuildingOutpostEditor | [`void CopyBuildingTemplate()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L117) | public |
| BuildingOutpostEditor | [`void RefreshSlots(EnemyOutpostData data)`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L135) | private |
| BuildingOutpostEditor | [`EnemyOutpostData Copy()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L152) | private |
| BuildingOutpostEditor | [`void SetOutpost()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L163) | public |
| BuildingOutpostEditor | [`void AddDefender()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L180) | public |
| BuildingOutpostEditor | [`void SelectDefender()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L210) | public |
| BuildingOutpostEditor | [`void SavePose()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L233) | public |
| BuildingOutpostEditor | [`bool Parse(InputField field, out float value)`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L253) | private static |
| BuildingOutpostEditor | [`void RemoveDefender()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L258) | public |
| BuildingOutpostEditor | [`bool Save(EnemyOutpostData data)`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L268) | private |
| BuildingOutpostEditor | [`void ShowPreview(EnemyOutpostData data)`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L290) | private |
| BuildingOutpostEditor | [`void ClearPreview()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L300) | private |
| BuildingOutpostEditor | [`void OnDisable()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L309) | private |
| BuildingOutpostEditor | [`void LateUpdate()`](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L314) | private |

## Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs

// 职责：按敌机模块轮廓制作可编辑停机底座，敌机与建筑使用同一蓝图局部坐标。 // 入口：Create()生成独立组合蓝图；BoundsOf()用于尺寸适配、净空与验证。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostBaseDesigner | [`Bounds BoundsOf(IReadOnlyList<BlockData> blocks, Quaternion rotation)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L9) | public static |
| OutpostBaseDesigner | [`BlockDataList Create(BlockDataList enemy, string name)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L14) | public static |
| OutpostBaseDesigner | [`void Add(BlockDataList data, string resource, Vector3 position)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L110) | private static |

## Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs

// 职责：在建筑旁展示完整敌机模型、部署线框、朝向及保守包围盒重叠提示。 // 入口：构造后读取Hint；Dispose释放显示对象与临时网格/材质，不实例化敌机玩法组件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostDeploymentPreview | [`void DrawGuard(OutpostGarrisonData guard, Bounds bounds, int index, bool selected, Color color)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L91) | private |
| OutpostDeploymentPreview | [`void FaceLabels(Quaternion rotation)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L133) | internal |
| OutpostDeploymentPreview | [`bool OverlapsBuilding(OutpostGarrisonData guard, Matrix4x4 pose, List<BlockData> building)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L141) | private static |
| OutpostDeploymentPreview | [`Bounds CalculateBounds(List<BlockData> blocks, Matrix4x4 deployment)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L161) | private static |
| OutpostDeploymentPreview | [`Vector3 Corner(int index)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L166) | private static |
| OutpostDeploymentPreview | [`void AddBox(List<Vector3> vertices, List<int> indices, Matrix4x4 transform)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L171) | private static |
| OutpostDeploymentPreview | [`void AddLine(List<Vector3> vertices, List<int> indices, Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L185) | private static |
| OutpostDeploymentPreview | [`void Dispose()`](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L193) | public |

## Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs

// 职责：持久化建造编辑并同步受影响连接；不负责输入、Ghost 或加载会话。 // 入口：Save() / Remove() / RemoveData()；SaveManager 和几何查询由组合入口注入。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildBlueprintEditor | [`void Save(Block block, string path, UnitFaction faction)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L17) | public |
| BuildBlueprintEditor | [`void Remove(Block block, string path)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L35) | public |
| BuildBlueprintEditor | [`bool RemoveData(string id, string path)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L50) | public |
| BuildBlueprintEditor | [`void Write(string path)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L63) | private |
| BuildBlueprintEditor | [`void RefreshConnectionsAround(Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L79) | private |

## Assets/Scripts/Features/Building/Interaction/BuildHandleAxis.cs

// 职责：统一移动/旋转操作轴的方向推断；保持既有本地位置及名称回退语义。 // 入口：Local() / World()；不拥有组件身份或序列化字段。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildHandleAxis | [`Vector3 World(Transform handle)`](../Assets/Scripts/Features/Building/Interaction/BuildHandleAxis.cs#L7) | internal static |
| BuildHandleAxis | [`Vector3 Local(Transform handle)`](../Assets/Scripts/Features/Building/Interaction/BuildHandleAxis.cs#L13) | internal static |

## Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs

// 职责：处理所选方块的键盘移动/旋转、轴拖拽及复制，提交存档和 Undo 动作。 // 入口：Poll() / TryBeginDrag() / CancelDrag()；旋转锁定整数格锚点，尺寸半格偏移随姿态换轴。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildManipulation | [`void Configure(Camera camera, GameObject move, GameObject rotate)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L56) | public |
| BuildManipulation | [`void Poll(Block block, BuildType type)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L63) | public |
| BuildManipulation | [`bool TryBeginDrag(RaycastHit hit, Block block, BuildType type)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L84) | public |
| BuildManipulation | [`void CancelDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L123) | public |
| BuildManipulation | [`void AlignAxisToNearestWorldDir()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L129) | private |
| BuildManipulation | [`void HandleMovement()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L174) | private |
| BuildManipulation | [`void HandleRotation()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L244) | private |
| BuildManipulation | [`void HandleMoveAxisDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L312) | private |
| BuildManipulation | [`void HandleRotateAxisDrag()`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L376) | private |
| BuildManipulation | [`void HandleDuplicate(Vector3 newPos, Quaternion newRot)`](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L428) | private |

## Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs

// 职责：集中建造尺寸、连接点对齐、旋转网格锚点、半网格吸附与阻挡几何。 // 入口：CalculateRotationPosition() / SnapRotationToGrid() / TryCalculateConnectorAlignedPosition() / IsBlocked()。 // 依赖：显式网格尺寸和方块层；无管理器单例、UI 或存档依赖。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPlacementGeometry | [`Quaternion SnapRotationToGrid(Quaternion rotation)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L10) | public |
| BuildPlacementGeometry | [`Vector3 CalculateRotationPosition(Vector3 startCenter, Quaternion startRotation, Quaternion targetRotation, Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L19) | public |
| BuildPlacementGeometry | [`Vector3 GetBlockHalfExtents(Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L43) | public |
| BuildPlacementGeometry | [`float GetHalfExtentAlongDirection(Block block, Quaternion rotation, Vector3 worldDirection)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L54) | public |
| BuildPlacementGeometry | [`bool TryCalculateConnectorAlignedPosition( Block prefabBlock, Quaternion previewRotation, Vector3 targetConnectorPosition, Vector3 targetConnectorNormal, out Vector3 position)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L64) | public |
| BuildPlacementGeometry | [`Quaternion GetSurfaceRotation(Block block, Quaternion authored, Vector3 surfaceNormal)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L109) | public |
| BuildPlacementGeometry | [`Vector3 RemoveGridNoise(Vector3 position)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L145) | private |
| BuildPlacementGeometry | [`Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L162) | public |
| BuildPlacementGeometry | [`bool IsBlocked(Vector3 targetCenter, Quaternion targetRotation, Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L174) | public |
| BuildPlacementGeometry | [`float GetMoveStep(Block block, Vector3 moveDir)`](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L201) | public |

## Assets/Scripts/Features/Building/Interaction/BuildPreview.cs

// 职责：管理建造 Ghost、连接点提示、有效位置判定和确认放置。 // 入口：Configure() / Poll() / Clear()；不负责选择、加载、JSON 或 Undo。 // 生命周期：退出建造或销毁时 Clear() 释放 Ghost 和提示。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPreview | [`void Configure( Camera camera, float range, Material highlight, ConnectorPlacementHints hints, bool groundPlacement = false)`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L30) | public |
| BuildPreview | [`bool IsWithinBuildRange(Vector3 position)`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L44) | public |
| BuildPreview | [`void Poll()`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L49) | public |
| BuildPreview | [`void EnsureGhost(GameObject prefab)`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L230) | private |
| BuildPreview | [`void Clear(bool hideHints = true)`](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L245) | public |

## Assets/Scripts/Features/Building/Interaction/BuildSelection.cs

// 职责：选择方块、恢复高亮材质和管理移动/旋转 Gizmo 的可见性。 // 入口：Poll() / SelectBlock() / DeselectBlock()；轴拖拽由 BuildManipulation 管理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildSelection | [`void Configure( Camera camera, float range, LayerMask axes, LayerMask blocks, Material highlight, GameObject move, GameObject rotate)`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L30) | public |
| BuildSelection | [`void Poll()`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L48) | public |
| BuildSelection | [`void SelectBlock(Block block)`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L86) | public |
| BuildSelection | [`void DeselectBlock()`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L135) | public |
| BuildSelection | [`void SetGizmosEnabled(bool enabled)`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L152) | public |
| BuildSelection | [`void RefreshBuildGizmos()`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L158) | public |
| BuildSelection | [`void HideBuildGizmos(GameObject keepVisible = null)`](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L192) | private |

## Assets/Scripts/Features/Building/Interaction/ConnectorPlacementHints.cs

// 职责：显示可用连接点提示，按镜头、建造范围及占用状态筛选。 // 入口：Show() / Hide()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ConnectorPlacementHints | [`void Show(Camera camera, float radius, LayerMask layerMask, GameObject excludedGhost = null)`](../Assets/Scripts/Features/Building/Interaction/ConnectorPlacementHints.cs#L52) | public |
| ConnectorPlacementHints | [`void Hide()`](../Assets/Scripts/Features/Building/Interaction/ConnectorPlacementHints.cs#L72) | public |
| ConnectorPlacementHints | [`void OnDisable()`](../Assets/Scripts/Features/Building/Interaction/ConnectorPlacementHints.cs#L78) | private |
| ConnectorPlacementHints | [`void ScanNearbyBlocks()`](../Assets/Scripts/Features/Building/Interaction/ConnectorPlacementHints.cs#L86) | private |
| ConnectorPlacementHints | [`bool CanDisplay(Block block, Connector connector)`](../Assets/Scripts/Features/Building/Interaction/ConnectorPlacementHints.cs#L115) | private |
| ConnectorPlacementHints | [`void LateUpdate()`](../Assets/Scripts/Features/Building/Interaction/ConnectorPlacementHints.cs#L120) | private |

## Assets/Scripts/Features/Building/Interaction/MoveAxisHandle.cs

// 职责：标记并显示建造移动轴，供 BuildManager 拖拽操作使用。 // 入口：Awake()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MoveAxisHandle | [`void Awake()`](../Assets/Scripts/Features/Building/Interaction/MoveAxisHandle.cs#L10) | private |

## Assets/Scripts/Features/Building/Interaction/RotateAxisHandle.cs

// 职责：标记并显示建造旋转轴，供 BuildManager 拖拽操作使用。 // 入口：Awake()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RotateAxisHandle | [`void Awake()`](../Assets/Scripts/Features/Building/Interaction/RotateAxisHandle.cs#L10) | private |

## Assets/Scripts/Features/Building/Interface/ActionCounterUI.cs

// 职责：展示建造动作历史计数及 Undo/Redo 状态。 // 入口：UpdateUndoText() / UpdateRedoText()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ActionCounterUI | [`void Awake()`](../Assets/Scripts/Features/Building/Interface/ActionCounterUI.cs#L11) | private |
| ActionCounterUI | [`void FixedUpdate()`](../Assets/Scripts/Features/Building/Interface/ActionCounterUI.cs#L16) | private |
| ActionCounterUI | [`void UpdateUndoText(int count)`](../Assets/Scripts/Features/Building/Interface/ActionCounterUI.cs#L22) | public |
| ActionCounterUI | [`void UpdateRedoText(int count)`](../Assets/Scripts/Features/Building/Interface/ActionCounterUI.cs#L37) | public |

## Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs

// 职责：展示蓝图名称、数量、质量和功率；业务统计由 BlueprintStatistics 提供。 // 入口：Refresh() / DisplayStatistics() / UpdateCurrentSaveName()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlueprintUIPanel | [`void Awake()`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L14) | private |
| BlueprintUIPanel | [`void Start()`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L19) | private |
| BlueprintUIPanel | [`void OnDestroy()`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L24) | private |
| BlueprintUIPanel | [`void Refresh()`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L33) | public |
| BlueprintUIPanel | [`void DisplayStatistics(BlueprintStatistics statistics)`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L44) | public |
| BlueprintUIPanel | [`void UpdateCurrentSaveName(string newName)`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L49) | public |
| BlueprintUIPanel | [`void UpdateStatistics(int blockCount, float mass, float requiredPower, float generatorOutput)`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L57) | public |
| BlueprintUIPanel | [`void UpdateTotalNumber(int newNumber)`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L65) | public |
| BlueprintUIPanel | [`void UpdateTotalMass(float newMass)`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L73) | public |
| BlueprintUIPanel | [`void UpdateTotalRequiredPower(float newRequiredPower)`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L82) | public |
| BlueprintUIPanel | [`void UpdateTotalGeneratorOutput(float newGeneratorOutput)`](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L91) | public |

## Assets/Scripts/Features/Building/Interface/BuildHotbar.cs

// 职责：协调建造快捷栏的输入、稳定ID配置与选择；不修改蓝图、金币或货仓。 // 入口：Choose() / Assign() / Swap() / Clear()；仅建造且无文本输入时响应按键。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildHotbar | [`BuildHotbarCatalog.Slot GetSlot(int index)`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L31) | public |
| BuildHotbar | [`void PreviewDrag(string path, PointerEventData pointer)`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L35) | public |
| BuildHotbar | [`void EndDrag()`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L55) | public |
| BuildHotbar | [`void OnDisable()`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L63) | private |
| BuildHotbar | [`void Awake()`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L68) | private |
| BuildHotbar | [`BuildHotbarCatalog.Slot Copy(BuildHotbarCatalog.Slot source)`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L104) | private static |
| BuildHotbar | [`bool Valid(BuildHotbarCatalog.Slot value)`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L113) | private |
| BuildHotbar | [`void Update()`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L129) | private |
| BuildHotbar | [`bool Choose(int index)`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L168) | public |
| BuildHotbar | [`bool Assign(int index, string path, string materialId)`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L186) | public |
| BuildHotbar | [`void Swap(int first, int second)`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L209) | public |
| BuildHotbar | [`void Clear(int index)`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L220) | public |
| BuildHotbar | [`void Save()`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L229) | private |
| BuildHotbar | [`void Refresh()`](../Assets/Scripts/Features/Building/Interface/BuildHotbar.cs#L236) | private |

## Assets/Scripts/Features/Building/Interface/BuildHotbarCatalog.cs

// 职责：保存快捷栏可选部件、稳定Resources路径、图标与默认槽位；个人配置独立保存。 // 入口：Find() / Entries / Defaults；不代表有限库存或货仓物品。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildHotbarCatalog | [`Entry Find(string path)`](../Assets/Scripts/Features/Building/Interface/BuildHotbarCatalog.cs#L30) | public |

## Assets/Scripts/Features/Building/Interface/BuildHotbarSlot.cs

// 职责：显示保存的部件/材质并处理点击、目录拖入、槽位互换和右键清空。 // 入口：Bind()/Show()/OnDrop()；所有修改委托BuildHotbar验证和保存。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildHotbarSlot | [`void Bind(BuildHotbar hotbar, int slot)`](../Assets/Scripts/Features/Building/Interface/BuildHotbarSlot.cs#L20) | public |
| BuildHotbarSlot | [`void Show(BuildHotbarCatalog.Slot slot, Sprite preview, bool selected)`](../Assets/Scripts/Features/Building/Interface/BuildHotbarSlot.cs#L27) | public |
| BuildHotbarSlot | [`void OnPointerClick(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildHotbarSlot.cs#L36) | public |
| BuildHotbarSlot | [`void OnBeginDrag(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildHotbarSlot.cs#L53) | public |
| BuildHotbarSlot | [`void OnDrag(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildHotbarSlot.cs#L59) | public |
| BuildHotbarSlot | [`void OnEndDrag(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildHotbarSlot.cs#L64) | public |
| BuildHotbarSlot | [`void OnDrop(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildHotbarSlot.cs#L70) | public |

## Assets/Scripts/Features/Building/Interface/BuildPalette.cs

// 职责：管理建造分类、选择与目录滚动显示。 // 入口：SelectCategory() / ShowTooltip() / HideTooltip()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPalette | [`void PreviewDrag(BuildPaletteItem item, UnityEngine.EventSystems.PointerEventData pointer)`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L29) | public |
| BuildPalette | [`void EndDrag()`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L34) | public |
| BuildPalette | [`void Awake()`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L39) | private |
| BuildPalette | [`void OnValidate()`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L65) | private |
| BuildPalette | [`void OnEnable()`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L79) | private |
| BuildPalette | [`void OnDisable()`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L84) | private |
| BuildPalette | [`void LateUpdate()`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L89) | private |
| BuildPalette | [`void SelectCategory(int category)`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L110) | public |
| BuildPalette | [`void ShowTooltip(BuildPaletteItem item)`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L136) | public |
| BuildPalette | [`void HideTooltip(BuildPaletteItem item)`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L145) | public |
| BuildPalette | [`BuildPaletteItem GetSelectedItem()`](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L157) | public |

## Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs

// 职责：展示单个建造条目的图标、选择反馈与悬停参数。 // 入口：SetSelected() / OnPointerEnter() / OnPointerExit()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildPaletteItem | [`void OnBeginDrag(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L26) | public |
| BuildPaletteItem | [`void OnDrag(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L31) | public |
| BuildPaletteItem | [`void OnEndDrag(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L36) | public |
| BuildPaletteItem | [`void Awake()`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L41) | private |
| BuildPaletteItem | [`void BindPalette(BuildPalette palette)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L50) | public |
| BuildPaletteItem | [`void ApplyButtonStyle(BuildPaletteItem template)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L60) | public |
| BuildPaletteItem | [`void SetSelected(bool selected, Color color)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L74) | public |
| BuildPaletteItem | [`void OnPointerEnter(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L79) | public |
| BuildPaletteItem | [`void OnPointerExit(PointerEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L84) | public |
| BuildPaletteItem | [`void OnSelect(BaseEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L97) | public |
| BuildPaletteItem | [`void OnDeselect(BaseEventData eventData)`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L102) | public |
| BuildPaletteItem | [`void OnDisable()`](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L107) | private |

## Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs

// 职责：展示玩家/敌方/建筑蓝图存档列表并绑定选择动作。 // 入口：RefreshList()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SaveUIPanel | [`void Awake()`](../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L12) | private |
| SaveUIPanel | [`void RefreshList()`](../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L17) | public |
| SaveUIPanel | [`void ShowEntries(IEnumerable<string> names, System.Action<string> open)`](../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L44) | private |
| SaveUIPanel | [`void ConfigureSaveItem(GameObject obj, string saveName, UnityEngine.Events.UnityAction onOpen)`](../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L53) | private |
| SaveUIPanel | [`void OnSaveClicked(string saveName)`](../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L71) | private |
| SaveUIPanel | [`void OnEnemyBlueprintClicked(string blueprintName)`](../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L76) | private |
| SaveUIPanel | [`void OnDuplicateClicked(string saveName)`](../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L81) | private |

## Assets/Scripts/Features/Building/Materials/BlockMaterial.cs

// 职责：将可选材质配置应用到基础结构方块；保留旧蓝图的Prefab参数和健康比例。 // 入口：SetMaterial()/Refresh()/Restore()；仅预挂在支持材质的Prefab，运行时不补组件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockMaterial | [`void SetMaterial(string id)`](../Assets/Scripts/Features/Building/Materials/BlockMaterial.cs#L17) | public |
| BlockMaterial | [`void Refresh()`](../Assets/Scripts/Features/Building/Materials/BlockMaterial.cs#L33) | public |
| BlockMaterial | [`void Validate(string id, GameObject prefab)`](../Assets/Scripts/Features/Building/Materials/BlockMaterial.cs#L66) | public static |
| BlockMaterial | [`void Restore(Block block, string id)`](../Assets/Scripts/Features/Building/Materials/BlockMaterial.cs#L74) | public static |
| BlockMaterial | [`bool PlayBreak()`](../Assets/Scripts/Features/Building/Materials/BlockMaterial.cs#L83) | public |

## Assets/Scripts/Features/Building/Materials/BlockMaterialCatalog.cs

// 职责：保存结构材质稳定ID、每立方米物理/造价参数及共享表现资产。 // 入口：Find()/Instance；空ID代表旧蓝图原始Prefab参数，未知ID必须拒绝。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockMaterialCatalog | [`Definition Find(string id)`](../Assets/Scripts/Features/Building/Materials/BlockMaterialCatalog.cs#L31) | public |
| BlockMaterialCatalog | [`Definition Resolve(string id)`](../Assets/Scripts/Features/Building/Materials/BlockMaterialCatalog.cs#L44) | public static |
| BlockMaterialCatalog | [`long Price(BlockData block)`](../Assets/Scripts/Features/Building/Materials/BlockMaterialCatalog.cs#L49) | public static |

## Assets/Scripts/Features/Building/Materials/BuildMaterialSelector.cs

// 职责：将已保存材质下拉框连接到当前建造选择，显示可用结构材质及成本提示。 // 入口：Select()；只影响随后放置的方块，不改已放置实体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildMaterialSelector | [`void LateUpdate()`](../Assets/Scripts/Features/Building/Materials/BuildMaterialSelector.cs#L12) | private |
| BuildMaterialSelector | [`void OnEnable()`](../Assets/Scripts/Features/Building/Materials/BuildMaterialSelector.cs#L24) | private |
| BuildMaterialSelector | [`void OnDisable()`](../Assets/Scripts/Features/Building/Materials/BuildMaterialSelector.cs#L50) | private |
| BuildMaterialSelector | [`void Select(int index)`](../Assets/Scripts/Features/Building/Materials/BuildMaterialSelector.cs#L58) | public |

## Assets/Scripts/Features/Building/Materials/MaterialBreakEffect.cs

// 职责：播放材质碎片ParticleSystem模板，预算有界且不生成会伤害/碰撞的实体。 // 入口：Play()；模板及粒子参数保存在Prefab，结束后销毁并归还活动预算。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MaterialBreakEffect | [`bool Play(MaterialBreakEffect template, Block block)`](../Assets/Scripts/Features/Building/Materials/MaterialBreakEffect.cs#L15) | public static |
| MaterialBreakEffect | [`void OnDestroy()`](../Assets/Scripts/Features/Building/Materials/MaterialBreakEffect.cs#L40) | private |
| MaterialBreakEffect | [`void ResetBudget()`](../Assets/Scripts/Features/Building/Materials/MaterialBreakEffect.cs#L48) | private static |

## Assets/Scripts/Features/Building/SaveManager.cs

// 职责：管理玩家/敌方/建筑蓝图目录、名称和缓存，提供加载/复制/重命名入口。 // 入口：EnsureSaveDirectories() / GetAllSaveNames() / GetAllEnemyBlueprintNames()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SaveManager | [`BlueprintFileStore FilesFor(BuildTargetKind kind)`](../Assets/Scripts/Features/Building/SaveManager.cs#L32) | private |
| SaveManager | [`void Awake()`](../Assets/Scripts/Features/Building/SaveManager.cs#L45) | private |
| SaveManager | [`void OnDestroy()`](../Assets/Scripts/Features/Building/SaveManager.cs#L59) | private |
| SaveManager | [`void EnsureSaveDirectories()`](../Assets/Scripts/Features/Building/SaveManager.cs#L67) | public |
| SaveManager | [`BlueprintFileStore GetBuildingBlueprintFiles()`](../Assets/Scripts/Features/Building/SaveManager.cs#L84) | internal static |
| SaveManager | [`BlueprintFileStore GetPlayerBlueprintFiles()`](../Assets/Scripts/Features/Building/SaveManager.cs#L89) | internal static |
| SaveManager | [`BlueprintFileStore GetBaseBlueprintFiles()`](../Assets/Scripts/Features/Building/SaveManager.cs#L94) | internal static |
| SaveManager | [`string GetBaseBlueprintPath(string name)`](../Assets/Scripts/Features/Building/SaveManager.cs#L104) | public |
| SaveManager | [`string GetEditedStructurePath(string name)`](../Assets/Scripts/Features/Building/SaveManager.cs#L114) | public |
| SaveManager | [`void GetAllBaseBlueprintNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L119) | public |
| SaveManager | [`void GetAllStructureBlueprintNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L126) | public |
| SaveManager | [`void LoadBaseBlueprint(string name)`](../Assets/Scripts/Features/Building/SaveManager.cs#L138) | public |
| SaveManager | [`void LoadEditedStructure(string name)`](../Assets/Scripts/Features/Building/SaveManager.cs#L143) | public |
| SaveManager | [`void GetAllBuildingBlueprintNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L155) | public |
| SaveManager | [`void CreateNewBuildingBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L162) | public |
| SaveManager | [`void LoadBuildingBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L186) | public |
| SaveManager | [`void DeleteBuildingBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L191) | public |
| SaveManager | [`bool RenameBuildingBlueprint(string oldName, string newName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L221) | public |
| SaveManager | [`string GetBuildingBlueprintPath(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L226) | public |
| SaveManager | [`void GetAllSaveNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L236) | public |
| SaveManager | [`void SaveBuildingOutpost(EnemyOutpostData outpost)`](../Assets/Scripts/Features/Building/SaveManager.cs#L246) | public |
| SaveManager | [`string CopyBundledBuilding(string template)`](../Assets/Scripts/Features/Building/SaveManager.cs#L271) | public |
| SaveManager | [`string SaveNewBuilding(string template, BlockDataList data)`](../Assets/Scripts/Features/Building/SaveManager.cs#L288) | public |
| SaveManager | [`void GetAllEnemyBlueprintNames()`](../Assets/Scripts/Features/Building/SaveManager.cs#L315) | public |
| SaveManager | [`void CreateNewSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L325) | public |
| SaveManager | [`void CreateNewEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L343) | public |
| SaveManager | [`void LoadSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L356) | public |
| SaveManager | [`void LoadEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L361) | public |
| SaveManager | [`void LoadBlueprint(string name, BuildTargetKind kind)`](../Assets/Scripts/Features/Building/SaveManager.cs#L366) | private |
| SaveManager | [`void DeleteSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L418) | public |
| SaveManager | [`void DeleteEnemyBlueprint(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L423) | public |
| SaveManager | [`void DeleteBlueprint(string name, BuildTargetKind kind)`](../Assets/Scripts/Features/Building/SaveManager.cs#L428) | private |
| SaveManager | [`bool DeleteBlueprintFile(string path)`](../Assets/Scripts/Features/Building/SaveManager.cs#L439) | private static |
| SaveManager | [`bool RenameSave(string oldSaveName, string newSaveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L450) | public |
| SaveManager | [`bool RenameEnemyBlueprint(string oldBlueprintName, string newBlueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L455) | public |
| SaveManager | [`bool RenameBlueprint(string oldName, string newName, BuildTargetKind kind)`](../Assets/Scripts/Features/Building/SaveManager.cs#L460) | private |
| SaveManager | [`void DuplicateSave(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L531) | public |
| SaveManager | [`string GetSavePath(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L544) | public |
| SaveManager | [`string GetEnemyBlueprintPath(string blueprintName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L549) | public |
| SaveManager | [`string GetSaveFileSize(string saveName)`](../Assets/Scripts/Features/Building/SaveManager.cs#L554) | public |

## Assets/Scripts/Features/Combat/Damage/CollisionDamageResolver.cs

// 职责：将单次碰撞的法向求解器能量分摊给本控制单元实际接触的模块。 // 入口：ApplyContactDamage()；仅由 ControlUnit.OnCollisionEnter() 调用。 // 约束：爆炸保护检查双方；伤害前记录玩家冲击，避免重组丢失原事件信息。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CollisionDamageResolver | [`void ApplyContactDamage(ControlUnit unit, Collision collision)`](../Assets/Scripts/Features/Combat/Damage/CollisionDamageResolver.cs#L10) | internal |

## Assets/Scripts/Features/Combat/Damage/Durability.cs

// 职责：管理当前/最大耐久、损伤反馈及销毁通知。 // 入口：ApplyDamage() / ApplyCollisionEnergy() / ProtectFromExplosionCollisions()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Durability | [`void OnTransformParentChanged()`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L14) | private |
| Durability | [`void SetCollisionDamageScale(float scale)`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L39) | internal |
| Durability | [`void Awake()`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L62) | private |
| Durability | [`void OnEnable()`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L78) | private |
| Durability | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L98) | private |
| Durability | [`void ApplyDamage(float amount, ControlUnit attacker)`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L108) | public |
| Durability | [`void ApplyCollisionEnergy(float reducedMass, float normalSpeed, float share)`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L119) | public |
| Durability | [`void ProtectFromExplosionCollisions(float duration)`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L145) | public |
| Durability | [`void UpdateDurablility(float value)`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L158) | public |
| Durability | [`void ChangeDurability(float value)`](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L168) | private |

## Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs

// 职责：爆炸只破坏体素地形；显式建筑伤害工具独立结算，批末刷新拓扑与冲量。 // 入口：ApplyExplosion() / ApplyBuildings() / RefreshUnit()；实例由显式销毁服务或独立建筑工具拥有。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ExplosionDamageSession | [`void ApplyBuildings(IReadOnlyList<BuildingRuntime> buildings, Vector3 center, float radius, float damage)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L27) | public |
| ExplosionDamageSession | [`void ApplyBuildingLoss(BuildingRuntime building, Action destroy)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L32) | public |
| ExplosionDamageSession | [`void ApplyExplosion(Block source, ControlUnit attacker, ControlUnit dirtyUnit, Action impulse)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L45) | public |
| ExplosionDamageSession | [`void RefreshUnit(ControlUnit unit)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L65) | public |
| ExplosionDamageSession | [`void Enqueue(Blast blast)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L82) | private |
| ExplosionDamageSession | [`void Execute(Action initial)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L105) | private |
| ExplosionDamageSession | [`void Apply(Blast blast)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L129) | private |
| ExplosionDamageSession | [`void AddTarget(Durability health, Blast blast)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L174) | private |
| ExplosionDamageSession | [`void FinishBatch()`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L190) | private |
| ExplosionDamageSession | [`void Attempt(Action action, ref Exception failure)`](../Assets/Scripts/Features/Combat/Damage/ExplosionDamageSession.cs#L238) | private static |

## Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs

// 职责：在源PhysicsScene对受影响刚体施加有限冲量及双向碰撞保护；不破坏健康模块连接。 // 入口：ApplyExplosionForce()；由 DestroyManager 在源模块移除与重组后调用。 // 约束：本类仅负责断链与冲量；直接伤害由ExplosionDamageSession统一结算。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ExplosionPropagation | [`void ApplyDefaultExplosionForce(Block block)`](../Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs#L14) | internal static |
| ExplosionPropagation | [`void ApplyExplosionForce( Block block, string ownerUnitId, UnitFaction ownerFaction, Vector3 explosionPosition, float upwardsModifier, float impulseScale, float maximumSpeedChange, float collisionProtection)`](../Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs#L28) | internal static |

## Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs

// 职责：按原始单位 ID 去重延迟清理；分别校验整单位、无驾驶舱组和远离组。 // 入口：ScheduleUnit() / ScheduleGroup() / ScheduleDistantGroup()。 // 生命周期：协程由 DestroyManager 承载；结束会话先停止协程再 Clear()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SalvageCleanupSession | [`void ScheduleUnit(GameObject destroyedObject, string ownerUnitId, float fallbackDelay)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L17) | internal |
| SalvageCleanupSession | [`void ScheduleGroup(ControlUnit unit, float fallbackDelay)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L28) | internal |
| SalvageCleanupSession | [`void ScheduleDistantGroup(ControlUnit unit, Transform reference, float maxDistance, float fallbackDelay)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L45) | internal |
| SalvageCleanupSession | [`void Clear()`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L62) | internal |
| SalvageCleanupSession | [`bool TrySchedule(string ownerUnitId)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L67) | private |
| SalvageCleanupSession | [`float GetCleanupDelay(float fallbackDelay)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L72) | private static |
| SalvageCleanupSession | [`IEnumerator CleanupDistantGroupAfterDelay( ControlUnit unit, string ownerUnitId, Transform reference, float maxDistanceSqr, float fallbackDelay)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L77) | private |
| SalvageCleanupSession | [`IEnumerator CleanupGroupAfterDelay(ControlUnit unit, string ownerUnitId, float fallbackDelay)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L95) | private |
| SalvageCleanupSession | [`IEnumerator CleanupUnitAfterDelay(string ownerUnitId, float fallbackDelay)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L108) | private |
| SalvageCleanupSession | [`IEnumerator WaitForFragment(ControlUnit unit)`](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L166) | private static |

## Assets/Scripts/Features/Combat/DestroyManager.cs

// 职责：协调销毁主链、资源释放与驾驶舱死亡；配置并调用爆炸规则及残骸清理会话。 // 入口：DestroyGameObject() / ExplodeBlock() / ScheduleUnitCleanup()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DestroyManager | [`void Awake()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L25) | private |
| DestroyManager | [`void DestroyGameObject(GameObject obj)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L30) | public |
| DestroyManager | [`void ExplodeBlock(Block block)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L140) | public |
| DestroyManager | [`void DamageBuildings(IReadOnlyList<BuildingRuntime> buildings, Vector3 center, float radius, float damage)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L187) | internal |
| DestroyManager | [`void ApplyBuildingLoss(BuildingRuntime building, System.Action destroy)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L192) | internal |
| DestroyManager | [`void ScheduleUnitCleanup(ControlUnit unit)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L197) | public |
| DestroyManager | [`void ScheduleDistantGroupCleanup(ControlUnit unit, Transform reference, float maxDistance)`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L202) | public |
| DestroyManager | [`void EndSalvageSession()`](../Assets/Scripts/Features/Combat/DestroyManager.cs#L207) | public |

## Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs

// 职责：保留敌机生成公开入口；所有阵营共用ModularUnitFactory恢复模块与物理归属。 // 入口：IsValidBlueprint() / Spawn()；现有据点和普通刷怪调用者保持兼容。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyBlueprintFactory | [`bool IsValidBlueprint(BlockDataList dataList, string enemyBlueprint)`](../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L7) | public static |
| EnemyBlueprintFactory | [`ControlUnit Spawn(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)`](../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12) | public static |

## Assets/Scripts/Features/Combat/Enemies/EnemyController.cs

// 职责：为敌机与自主友舰选择敌对目标并生成驾驶意图；阵营和玩家驾驶权分别判断。 // 入口：FixedUpdate()；战术意图交给 VesselNavigation 选路，ControlUnit/推进器负责物理执行。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyController | [`void ConfigureDebugDisplay(bool enabled, bool ranges)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L144) | internal |
| EnemyController | [`EnemyNavigationSnapshot CaptureNavigation(int previousRouteRevision = -1)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L150) | public |
| EnemyController | [`void OnEnable()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L228) | private |
| EnemyController | [`void Awake()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L251) | private |
| EnemyController | [`void FixedUpdate()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L259) | private |
| EnemyController | [`Vector3 CalculateDesiredMovement(Vector3 flatDirection)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L326) | private |
| EnemyController | [`Vector3 GetApproachDirection(Vector3 destination, Vector3 fallback)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L363) | private |
| EnemyController | [`Vector3 PredictMovement(Vector3 proposed)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L370) | private |
| EnemyController | [`float GetMovementInterval()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L406) | private |
| EnemyController | [`Vector3 GetReferenceVelocity()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L411) | private |
| EnemyController | [`void UpdatePropulsionInput()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L438) | private |
| EnemyController | [`Vector3 StabilizeDirection(Vector3 candidate)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L452) | private |
| EnemyController | [`void SetGuardAnchor(Transform anchor, float radius)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L493) | public |
| EnemyController | [`void SetPriorityTarget(ControlUnit targetUnit)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L501) | public |
| EnemyController | [`ControlUnit FindNearestOpponent()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L508) | private |
| EnemyController | [`bool IsValidOpponent(ControlUnit candidate)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L559) | private |
| EnemyController | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L574) | private |
| EnemyController | [`void ConfigureHoverThrusters()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L598) | private |
| EnemyController | [`float ConstrainDescent(float requested, Vector3 direction)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L640) | private |
| EnemyController | [`float GetBaseHoverHeight()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L658) | private |
| EnemyController | [`Vector3 GetStrafeDirection(Vector3 flatDirection)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L676) | private |
| EnemyController | [`Vector3 ApplyObstacleAvoidance(Vector3 desiredMove, Vector3 targetDirection)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L692) | private |
| EnemyController | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L745) | private |
| EnemyController | [`void DrawDebug()`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L750) | private |
| EnemyController | [`void DrawDebugCircle(Vector3 origin, float radius, Color color)`](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L813) | private static |

## Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs

// 职责：保存敌方蓝图身份和生成信息，供 HUD 与击杀反馈使用。 // 入口：SetDisplayName() / SetSpawnMaxHealth()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyIdentity | [`void ReportDefeated()`](../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L9) | public |
| EnemyIdentity | [`void SetDisplayName(string displayName)`](../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L27) | public |
| EnemyIdentity | [`void SetSpawnMaxHealth(float maximum)`](../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L32) | public |

## Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs

// 职责：缓存敌方蓝图，并在普通建造游玩期间按间隔生成敌机；世界探索由据点驻军独占刷怪。 // 入口：BeginPlayMode() / EndPlayMode() / SpawnRandomEnemy() / SpawnEnemy()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemySpawner | [`void Start()`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L21) | private |
| EnemySpawner | [`void Update()`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L26) | private |
| EnemySpawner | [`void BeginPlayMode(Transform anchor)`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L41) | public |
| EnemySpawner | [`void EndPlayMode()`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L57) | public |
| EnemySpawner | [`void InitializeEnemyBlueprintPool()`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L65) | private |
| EnemySpawner | [`void SpawnRandomEnemy()`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L110) | public |
| EnemySpawner | [`void SpawnEnemy()`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L133) | public |
| EnemySpawner | [`ControlUnit SpawnBlockData(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144) | internal |
| EnemySpawner | [`Vector3 GetSpawnPosition()`](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L151) | private |

## Assets/Scripts/Features/Combat/Interface/CombatHud.cs

// 职责：共享战斗HUD入口，显式绑定战场/镜头/关注舰，限频采样耐久并逐帧投影名牌。 // 入口：Bind() / ResetHud() / ShowPlayerKill() / ShowCombatKill()；退出绑定时清空视图和通知。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatHud | [`void Bind(Scene scene, Camera camera, bool showBothTeams)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L67) | public |
| CombatHud | [`void SetFocus(ControlUnit unit)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L76) | public |
| CombatHud | [`void Unbind()`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L81) | public |
| CombatHud | [`void OnEnable()`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L90) | private |
| CombatHud | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L95) | private |
| CombatHud | [`void ResetHud()`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L100) | public |
| CombatHud | [`void ShowPlayerKill(string enemyName)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L116) | public |
| CombatHud | [`void ShowCombatKill(string victim, string attacker, string score)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L127) | public |
| CombatHud | [`void ShowDestruction(string victim, string attacker, string detail, bool celebrate)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L132) | public |
| CombatHud | [`void LateUpdate()`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L140) | private |
| CombatHud | [`void Sample()`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L174) | private |
| CombatHud | [`int ComparePriority(ControlUnit left, ControlUnit right)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L246) | private |
| CombatHud | [`void Project()`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L257) | private |
| CombatHud | [`bool HasVisibleNameplate(ControlUnit unit)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L300) | public |
| CombatHud | [`bool IsNameplateAreaAvailable(Rect bounds)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L305) | public |
| CombatHud | [`bool IsWorldPointOccluded(Camera camera, Vector3 target, Transform owner, RaycastHit[] hits)`](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L331) | public static |

## Assets/Scripts/Features/Combat/Interface/CombatHudTheme.cs

// 职责：集中保存共享战斗界面的配色、采样预算与动效时间，所有消费者只读。 // 入口：由 CombatHud / CombatNameplate / CombatNotifications 的 Inspector 引用。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Combat/Interface/Nameplates/CombatNameplate.cs

// 职责：显示整舰/驾驶舱耐久与延迟损伤条；只接收数据，不扫描场景或修改耐久。 // 入口：Bind() / Sample() / Present()；对象回收后 Bind 重置全部动画状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatNameplate | [`void Bind(CombatHudTheme style, string title, Color color)`](../Assets/Scripts/Features/Combat/Interface/Nameplates/CombatNameplate.cs#L35) | public |
| CombatNameplate | [`void Sample(float health, float maximum, float cockpit, float cockpitMaximum, bool detailed)`](../Assets/Scripts/Features/Combat/Interface/Nameplates/CombatNameplate.cs#L47) | public |
| CombatNameplate | [`void Present(Vector2 position, bool visible, float delta)`](../Assets/Scripts/Features/Combat/Interface/Nameplates/CombatNameplate.cs#L67) | public |

## Assets/Scripts/Features/Combat/Interface/Notifications/CombatFeedItem.cs

// 职责：维护一条可复用战报的展示数据和补位动画。 // 入口：Set() / CopyFrom() / Present()；实例由制作工具预存，不在击杀时创建。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatFeedItem | [`void Set(string text, string subtitle, float time)`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatFeedItem.cs#L18) | public |
| CombatFeedItem | [`void CopyFrom(CombatFeedItem source)`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatFeedItem.cs#L28) | public |
| CombatFeedItem | [`void Clear()`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatFeedItem.cs#L39) | public |
| CombatFeedItem | [`void Present(float now, CombatHudTheme theme, int index)`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatFeedItem.cs#L45) | public |

## Assets/Scripts/Features/Combat/Interface/Notifications/CombatNotifications.cs

// 职责：用有界战报池和有界提示队列展示战斗事实，处理补位、入场和退场动画。 // 入口：Push() / Clear()；不结算奖励，禁用时释放待播消息。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatNotifications | [`void SetToolsOpen(bool open, bool immediate = false)`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatNotifications.cs#L37) | public |
| CombatNotifications | [`void Awake()`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatNotifications.cs#L54) | private |
| CombatNotifications | [`void Push(string victim, string attacker, string detail, bool celebrate)`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatNotifications.cs#L60) | public |
| CombatNotifications | [`void Clear()`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatNotifications.cs#L90) | public |
| CombatNotifications | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatNotifications.cs#L107) | private |
| CombatNotifications | [`void Update()`](../Assets/Scripts/Features/Combat/Interface/Notifications/CombatNotifications.cs#L112) | private |

## Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs

// 职责：协调调试/推进器抽屉互斥、可取消过渡和调试状态恢复。 // 入口：Bind() / Restore() / ToggleDebug() / ToggleThrusters()；不修改战斗规则。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatHudPanels | [`void Awake()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L31) | private |
| CombatHudPanels | [`void Bind()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L48) | public |
| CombatHudPanels | [`void Restore()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L64) | public |
| CombatHudPanels | [`void ToggleDebug()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L80) | public |
| CombatHudPanels | [`void ToggleThrusters()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L87) | public |
| CombatHudPanels | [`void ClearDebug()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L93) | public |
| CombatHudPanels | [`void CloseAll()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L102) | public |
| CombatHudPanels | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L111) | private |
| CombatHudPanels | [`void Update()`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L116) | private |
| CombatHudPanels | [`bool GetDebug(int index)`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L150) | private |
| CombatHudPanels | [`void SetDebug(int index, bool value)`](../Assets/Scripts/Features/Combat/Interface/Panels/CombatHudPanels.cs#L177) | private |

## Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatIcon.cs

// 职责：以无贴图矢量网格绘制双层威胁箭头，保持各分辨率下的清晰边缘。 // 入口：uGUI OnPopulateMesh；组件与颜色保存在HUD Prefab，不创建运行时对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatThreatIcon | [`void OnPopulateMesh(VertexHelper mesh)`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatIcon.cs#L8) | protected override |
| CombatThreatIcon | [`void Chevron(VertexHelper mesh, Vector2 center, float scale, float offset, Color tint)`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatIcon.cs#L19) | private static |

## Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatMarkers.cs

// 职责：对共享HUD候选敌舰逐帧投影，稳定绑定屏内定位框/屏外箭头并合并同向威胁。 // 入口：Clear()；LateUpdate在镜头和CombatHud之后运行，文字独立限频。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatThreatMarkers | [`void Clear()`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatMarkers.cs#L21) | public |
| CombatThreatMarkers | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatMarkers.cs#L30) | private |
| CombatThreatMarkers | [`void LateUpdate()`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatMarkers.cs#L35) | private |
| CombatThreatMarkers | [`bool ContainsCandidate(Cockpit cockpit)`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatMarkers.cs#L156) | private |

## Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatView.cs

// 职责：显示单个威胁的箭头、定位框、距离和聚合数量；标签保持水平。 // 入口：BeginFrame() / Place() / EndFrame()；实例由Editor保存。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatThreatView | [`void Clear()`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatView.cs#L37) | public |
| CombatThreatView | [`void BeginFrame()`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatView.cs#L44) | public |
| CombatThreatView | [`void Place( Cockpit owner, Vector2 position, bool offscreen, bool rear, float meters, bool eliteWave, CombatHudTheme theme)`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatView.cs#L50) | public |
| CombatThreatView | [`void AddCluster()`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatView.cs#L83) | public |
| CombatThreatView | [`void EndFrame()`](../Assets/Scripts/Features/Combat/Interface/Threats/CombatThreatView.cs#L88) | public |

## Assets/Scripts/Features/Combat/Meteors/Meteor.cs

// 职责：维护陨石刚体、飞行和碰撞相关行为。 // 入口：Start() / OnCollisionEnter()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Meteor | [`void Start()`](../Assets/Scripts/Features/Combat/Meteors/Meteor.cs#L15) | private |
| Meteor | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Combat/Meteors/Meteor.cs#L26) | private |

## Assets/Scripts/Features/Combat/Meteors/MeteorShower.cs

// 职责：根据配置周期生成陨石并设置位置与运动参数。 // 入口：MeteorDestroyed()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MeteorShower | [`void Start()`](../Assets/Scripts/Features/Combat/Meteors/MeteorShower.cs#L26) | private |
| MeteorShower | [`IEnumerator SpawnMeteors()`](../Assets/Scripts/Features/Combat/Meteors/MeteorShower.cs#L31) | private |
| MeteorShower | [`void SpawnMeteor()`](../Assets/Scripts/Features/Combat/Meteors/MeteorShower.cs#L45) | private |
| MeteorShower | [`void MeteorDestroyed()`](../Assets/Scripts/Features/Combat/Meteors/MeteorShower.cs#L77) | public |

## Assets/Scripts/Features/Combat/Skirmish/Diagnostics/SkirmishNavigationDebug.cs

// 职责：在随机5v5中连续追踪红队五个槽位，展示可切换导航文字、实速轨迹与未来路线。 // 入口：Update()降频绑定/采样；OnGUI()只读缓存；FocusSelected()保持当前相机朝向观察所选单位。 // 生命周期：换轮复用场景保存的五组追踪对象；驾驶舱重组续接，死亡保留轨迹，退出自动释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SkirmishNavigationDebug | [`NavigationTrack TrackAt(int index)`](../Assets/Scripts/Features/Combat/Skirmish/Diagnostics/SkirmishNavigationDebug.cs#L36) | public |
| SkirmishNavigationDebug | [`void Update()`](../Assets/Scripts/Features/Combat/Skirmish/Diagnostics/SkirmishNavigationDebug.cs#L37) | private |
| SkirmishNavigationDebug | [`void SelectEnemy(int index)`](../Assets/Scripts/Features/Combat/Skirmish/Diagnostics/SkirmishNavigationDebug.cs#L69) | public |
| SkirmishNavigationDebug | [`void FocusSelected()`](../Assets/Scripts/Features/Combat/Skirmish/Diagnostics/SkirmishNavigationDebug.cs#L75) | public |
| SkirmishNavigationDebug | [`void OnGUI()`](../Assets/Scripts/Features/Combat/Skirmish/Diagnostics/SkirmishNavigationDebug.cs#L84) | private |
| SkirmishNavigationDebug | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Combat/Skirmish/Diagnostics/SkirmishNavigationDebug.cs#L140) | private |

## Assets/Scripts/Features/Combat/Skirmish/SkirmishHud.cs

// 职责：显示自动对战轮次、十槽位阵容和击杀列表，复用探索模式击杀提示表现。 // 入口：SetState() / ShowKill() / ResetRound()；UI引用由场景制作保存。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SkirmishHud | [`void ResetRound()`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishHud.cs#L19) | public |
| SkirmishHud | [`void SetState(string title, string lineup)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishHud.cs#L25) | public |
| SkirmishHud | [`void ShowKill(string victim, string attacker, int blueAlive, int redAlive)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishHud.cs#L31) | public |

## Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs

// 职责：自动填充随机5v5、按驾驶舱判定淘汰，结算等待后完整清场并开始下一轮。 // 入口：Start()自动开局；NotifyCockpitDestroyed()接收正式销毁主链，Update()处理胜负。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SkirmishMatch | [`Color TeamColor(bool blue)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L57) | public |
| SkirmishMatch | [`Cockpit CockpitAt(int index)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L70) | public |
| SkirmishMatch | [`string NameAt(int index)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L71) | public |
| SkirmishMatch | [`void Start()`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L72) | private |
| SkirmishMatch | [`void BeginRound()`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L106) | private |
| SkirmishMatch | [`bool NotifyCockpitDestroyed(Cockpit cockpit, ControlUnit attacker)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L161) | public |
| SkirmishMatch | [`void Eliminate(int index, string attacker)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L178) | private |
| SkirmishMatch | [`int CountAlive(int first)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L190) | private |
| SkirmishMatch | [`void Update()`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L204) | private |
| SkirmishMatch | [`bool TeamCanFight(int first)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L269) | private |
| SkirmishMatch | [`void ApplyTeamColor(ControlUnit unit, bool blue)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L286) | private |
| SkirmishMatch | [`void OnDestroy()`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L343) | private |
| SkirmishMatch | [`void ReleaseTeamMaterial(Material material)`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L360) | private static |
| SkirmishMatch | [`IEnumerator ClearAndRestart()`](../Assets/Scripts/Features/Combat/Skirmish/SkirmishMatch.cs#L374) | private |

## Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs

// 职责：瞬时激光命中与短时光束表现；不对目标施加冲量。 // 入口：Turret 固定步调用 Fire()；公用配置通过基类保持原序列化字段。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| LaserTurret | [`void Awake()`](../Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs#L20) | protected override |
| LaserTurret | [`void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)`](../Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs#L35) | protected override |
| LaserTurret | [`void HideShotFeedback()`](../Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs#L52) | protected override |
| LaserTurret | [`void UpdateShotFeedback()`](../Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs#L58) | protected override |

## Assets/Scripts/Features/Combat/Turrets/ProjectileIntercept.cs

// 职责：求解无重力、恒速炮弹与匀速目标的最早正交会时间。 // 入口：TryGetTime()；速度采用炮口继承速度的相对参考系，无解或超寿命时由调用者回退。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProjectileIntercept | [`bool TryGetTime(Vector3 offset, Vector3 relativeVelocity, float speed, float lifetime, out float time)`](../Assets/Scripts/Features/Combat/Turrets/ProjectileIntercept.cs#L8) | internal static |

## Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs

// 职责：每炮管发射真实炮弹并播放后退/复位动画；炮弹负责飞行和命中。 // 入口：Turret.Fire()；炮管、速度、寿命和冲量可在 Prefab Inspector 配置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProjectileTurret | [`bool MovesVisual(Transform part)`](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L36) | internal |
| ProjectileTurret | [`void Awake()`](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L54) | protected override |
| ProjectileTurret | [`void LateUpdate()`](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L77) | private |
| ProjectileTurret | [`void UpdateRecoil(float deltaTime)`](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L86) | private |
| ProjectileTurret | [`void ApplyRecoilPose()`](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L104) | private |
| ProjectileTurret | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L125) | protected override |
| ProjectileTurret | [`Vector3 GetAimPosition(Durability target, Vector3 origin)`](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L133) | protected override |
| ProjectileTurret | [`void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)`](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L164) | protected override |

## Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs

// 职责：根据炮弹真实位置历史绘制短曳光，池复用时清空历史，不参与命中/物理计算。 // 入口：TurretProjectile.Launch() 调用 Begin()；LateUpdate 更新两端，OnDisable 清理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProjectileTracer | [`void Begin()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L21) | public |
| ProjectileTracer | [`void Record()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L31) | private |
| ProjectileTracer | [`void LateUpdate()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L39) | private |
| ProjectileTracer | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L68) | private |

## Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs

// 职责：真实刚体炮弹飞行、每步扫掠防穿透、敌对伤害和寿命清理。 // 入口：Launch()；触发器避免物理引擎额外施加冲量，自身构造始终过滤。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretProjectile | [`void SetPool(TurretProjectilePool pool, TurretProjectile prefab)`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L26) | internal |
| TurretProjectile | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L32) | private |
| TurretProjectile | [`void ClearIgnoredCollisions()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L39) | private |
| TurretProjectile | [`void Awake()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L52) | private |
| TurretProjectile | [`void Launch( ControlUnit owner, UnitFaction faction, float damage, float impulse, Vector3 velocity, float lifetime, float maxDistance, LayerMask hitLayers)`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L65) | public |
| TurretProjectile | [`void FixedUpdate()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L99) | private |
| TurretProjectile | [`bool ResolveOverlaps(Vector3 origin, float radius)`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L169) | private |
| TurretProjectile | [`void OnTriggerEnter(Collider other)`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L191) | private |
| TurretProjectile | [`bool CanHit(Collider collider)`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L200) | private |
| TurretProjectile | [`void ResolveHit(Collider collider, Vector3 point, Vector3 normal)`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L205) | private |
| TurretProjectile | [`void Retire()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L226) | private |

## Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs

// 职责：按炮弹 Prefab 复用运行实例，限制闲置数量并在模拟结束时清理。 // 入口：Rent() / Return()；炮弹仍独立执行原有物理与命中流程。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretProjectilePool | [`TurretProjectile Rent( TurretProjectile prefab, Vector3 position, Quaternion rotation, UnityEngine.SceneManagement.Scene scene = default)`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L12) | public static |
| TurretProjectilePool | [`void Return(TurretProjectile shot, TurretProjectile prefab)`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L58) | internal |
| TurretProjectilePool | [`void ClearAll()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L71) | public |
| TurretProjectilePool | [`void Update()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L82) | private |
| TurretProjectilePool | [`void OnDestroy()`](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L98) | private |

## Assets/Scripts/Features/Combat/Turrets/Turret.cs

// 职责：共享炮塔启动、独立索敌、功率请求和双轴瞄准；武器种类只实现发射与表现。 // 入口：SetStarted() / FixedUpdate()；禁用和退出运行时撤销功率需求。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Turret | [`void DelayTargetSearch(float seconds)`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L46) | internal |
| Turret | [`void Awake()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L51) | protected virtual |
| Turret | [`void OnValidate()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L59) | protected virtual |
| Turret | [`void SetStarted(bool started)`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L70) | public |
| Turret | [`void FixedUpdate()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L80) | protected virtual |
| Turret | [`void OnDisable()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L103) | protected virtual |
| Turret | [`void StopWorking()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L109) | private |
| Turret | [`void RefreshTarget()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L118) | private |
| Turret | [`void AimAndFire(Durability aimTarget)`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L145) | private |
| Turret | [`void AimAt(Vector3 worldDirection)`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L165) | private |
| Turret | [`void ResolveAimingRig()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L195) | private |
| Turret | [`Vector3 GetMuzzlePosition()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L229) | protected |
| Turret | [`Vector3 GetAimForward()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L240) | protected |
| Turret | [`UnitFaction GetEffectiveTargetFaction()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L246) | protected |
| Turret | [`Vector3 GetAimPosition(Durability target, Vector3 origin)`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L252) | protected virtual |
| Turret | [`void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L257) | protected abstract |
| Turret | [`void HideShotFeedback()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L258) | protected virtual |
| Turret | [`void UpdateShotFeedback()`](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L262) | protected virtual |

## Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs

// 职责：共享激光与实体炮弹的命中反馈、敌对伤害和可选炮弹冲量。 // 入口：Apply()；先施加炮弹冲量，确保拆组后的刚体继承命中速度。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretHitResolver | [`void Apply( Collider collider, Vector3 point, Vector3 normal, Vector3 direction, ControlUnit owner, UnitFaction faction, float damage, float impulse)`](../Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs#L7) | internal static |

## Assets/Scripts/Features/Combat/Turrets/TurretHitscan.cs

// 职责：激光沿射线瞬时命中最近的非自身障碍，结算伤害但不施加冲量。 // 入口：Fire()；由 LaserTurret 调用并返回光束终点。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretHitscan | [`Vector3 Fire( Vector3 origin, Vector3 direction, ControlUnit owner, UnitFaction targetFaction, float range, LayerMask hitLayers, float damage)`](../Assets/Scripts/Features/Combat/Turrets/TurretHitscan.cs#L10) | internal |
| RaycastHitDistanceComparer | [`int Compare(RaycastHit a, RaycastHit b)`](../Assets/Scripts/Features/Combat/Turrets/TurretHitscan.cs#L59) | public |

## Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs

// 职责：从炮口可见的敌对组中选择最近伤害目标，支持玩家优先攻击可见驾驶舱。 // 入口：FindNearestTarget()；由 Turret 按索敌周期调用，不依赖当前供电。 // 生命周期：共享单位当前物理步的层级快照，实时验证伤势、位置和归属；修复/重组使快照失效。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TurretTargetSelector | [`void ConfigureVisibility(Vector3 origin, LayerMask hitLayers)`](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L36) | internal |
| TurretTargetSelector | [`bool HasLineOfSight(ControlUnit owner, ControlUnit target, Durability durability)`](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L42) | internal |
| TurretTargetSelector | [`ControlUnit FindNearestTarget( IReadOnlyList<ControlUnit> units, ControlUnit owner, UnitFaction faction, float range, bool prioritizeCockpit, out Durability nearestDurability)`](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L105) | internal |
| TurretTargetSelector | [`Durability FindNearestDurability(ControlUnit unit, ControlUnit owner, Vector3 origin, float nearestSqrDistance)`](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L188) | private |
| TurretTargetSelector | [`Durability FindCockpitDurability(ControlUnit unit)`](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L225) | private |

## Assets/Scripts/Features/Diagnostics/Bots/BotNavigationProbeDriver.cs

// 职责：在探针物理帧调用生产导航并记录到达、碰撞和爬升，不影响真实机器人任务。 // 入口：BotNavigationPlayProbe.Initialize() 创建一次性驱动；只在 Editor Play 验证使用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BotNavigationProbeDriver | [`void Initialize(string caseName, Transform target, Rigidbody body, Rigidbody home)`](../Assets/Scripts/Features/Diagnostics/Bots/BotNavigationProbeDriver.cs#L49) | public |
| BotNavigationProbeDriver | [`void FixedUpdate()`](../Assets/Scripts/Features/Diagnostics/Bots/BotNavigationProbeDriver.cs#L62) | private |
| BotNavigationProbeDriver | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Diagnostics/Bots/BotNavigationProbeDriver.cs#L92) | private |

## Assets/Scripts/Features/Diagnostics/DebugManager.cs

// 职责：保存调试显示配置和状态图标 Canvas；供电范围网格由 PowerRangeUnionView 拥有。 // 入口：GetPowerRangeColor() / TogglePowerRange() / TogglePowerConnections()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DebugManager | [`bool Includes(Component component)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L22) | public |
| DebugManager | [`void SetFactionFilter(int value)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L34) | public |
| DebugManager | [`bool ShowsAI(UnitFaction faction)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L67) | public |
| DebugManager | [`void SetAIVisibility(UnitFaction faction, bool visible)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L68) | public |
| DebugManager | [`void Awake()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L94) | private |
| DebugManager | [`void OnDestroy()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L101) | private |
| DebugManager | [`Color GetPowerRangeColor(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L110) | public |
| DebugManager | [`void TogglePowerRange()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L125) | public |
| DebugManager | [`void TogglePowerConnections()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L138) | public |
| DebugManager | [`void ToggleConnectionStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L143) | public |
| DebugManager | [`void ToggleDurabilityStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L149) | public |
| DebugManager | [`void TogglePowerStatus()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L155) | public |
| DebugManager | [`void EnsureStatusIconCanvas()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L161) | private |
| DebugManager | [`void RefreshPowerRangeMesh(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L171) | internal |
| DebugManager | [`void ClearPowerRangeMesh()`](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L185) | internal |

## Assets/Scripts/Features/Diagnostics/Interface/CombatFlightTracks.cs

// 职责：用场景保存的有界追踪槽展示当前战场飞行历史与规划路线，共用5v5的NavigationTrack。 // 入口：Update()每0.1秒按阵营绑定/采样；关闭诊断后隐藏并释放绑定，不创建运行时组件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CombatFlightTracks | [`void Update()`](../Assets/Scripts/Features/Diagnostics/Interface/CombatFlightTracks.cs#L12) | private |

## Assets/Scripts/Features/Diagnostics/Interface/DurabilityDebugLabels.cs

// 职责：集中绘制已受损模块的可选耐久标签，避免每个模块注册IMGUI回调。 // 入口：DebugManager装配；标签关闭时不遍历模块，不更改伤害或耐久状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DurabilityDebugLabels | [`void OnGUI()`](../Assets/Scripts/Features/Diagnostics/Interface/DurabilityDebugLabels.cs#L9) | private |

## Assets/Scripts/Features/Diagnostics/Interface/PerformanceMonitor.cs

// 职责：采集运行性能指标供调试展示。 // 入口：Start() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PerformanceMonitor | [`void Start()`](../Assets/Scripts/Features/Diagnostics/Interface/PerformanceMonitor.cs#L35) | private |
| PerformanceMonitor | [`void Update()`](../Assets/Scripts/Features/Diagnostics/Interface/PerformanceMonitor.cs#L59) | private |
| PerformanceMonitor | [`void UpdateCpuUsage()`](../Assets/Scripts/Features/Diagnostics/Interface/PerformanceMonitor.cs#L80) | private |
| PerformanceMonitor | [`void UpdateGpuFrameTime()`](../Assets/Scripts/Features/Diagnostics/Interface/PerformanceMonitor.cs#L89) | private |
| PerformanceMonitor | [`void OnGUI()`](../Assets/Scripts/Features/Diagnostics/Interface/PerformanceMonitor.cs#L103) | private |

## Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseBodyProbe.cs

// 职责：采集单个刚体的异常速度/冲量线索，供调试记录器分析。 // 入口：Awake() / OnCollisionEnter()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| AbnormalImpulseBodyProbe | [`void Awake()`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseBodyProbe.cs#L10) | private |
| AbnormalImpulseBodyProbe | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseBodyProbe.cs#L15) | private |

## Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseRecorder.cs

// 职责：在编辑器调试时记录异常刚体冲量并输出诊断数据。 // 入口：RecordCollision()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| AbnormalImpulseRecorder | [`void RecordCollision(Rigidbody body, Collision collision)`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseRecorder.cs#L31) | public static |
| AbnormalImpulseRecorder | [`void Awake()`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseRecorder.cs#L49) | private |
| AbnormalImpulseRecorder | [`void FixedUpdate()`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseRecorder.cs#L67) | private |
| AbnormalImpulseRecorder | [`void Record( Rigidbody body, string source, float impulse, float deltaVelocity, Vector3 point, string other)`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseRecorder.cs#L125) | private |
| AbnormalImpulseRecorder | [`string Csv(string value)`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseRecorder.cs#L180) | private static |
| AbnormalImpulseRecorder | [`string HierarchyPath(Transform node)`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseRecorder.cs#L181) | private static |
| AbnormalImpulseRecorder | [`void OnDestroy()`](../Assets/Scripts/Features/Diagnostics/Physics/AbnormalImpulseRecorder.cs#L193) | private |

## Assets/Scripts/Features/Diagnostics/Physics/PhysicsTelemetryHud.cs

// 职责：展示玩家单元的物理遥测与运行状态。 // 入口：OnEnable() / OnDisable() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PhysicsTelemetryHud | [`void OnEnable()`](../Assets/Scripts/Features/Diagnostics/Physics/PhysicsTelemetryHud.cs#L19) | private |
| PhysicsTelemetryHud | [`void OnDisable()`](../Assets/Scripts/Features/Diagnostics/Physics/PhysicsTelemetryHud.cs#L26) | private |
| PhysicsTelemetryHud | [`void OnImpact(float energy, float impulse)`](../Assets/Scripts/Features/Diagnostics/Physics/PhysicsTelemetryHud.cs#L31) | private |
| PhysicsTelemetryHud | [`void Update()`](../Assets/Scripts/Features/Diagnostics/Physics/PhysicsTelemetryHud.cs#L39) | private |

## Assets/Scripts/Features/Flight/FlightControlMath.cs

// 职责：集中悬浮姿态响应与自主航迹的限速计算，不读取场景或写刚体。 // 入口：ResponseBlend()/StepCourse()；调用者持有各自状态并决定紧急动作策略。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| FlightControlMath | [`float ResponseBlend(float response, float deltaTime)`](../Assets/Scripts/Features/Flight/FlightControlMath.cs#L7) | internal static |
| FlightControlMath | [`Vector3 StepCourse(Vector3 current, Vector3 desired, float turnRate, float acceleration, float deltaTime)`](../Assets/Scripts/Features/Flight/FlightControlMath.cs#L12) | internal static |

## Assets/Scripts/Features/Flight/FlightPropulsionControl.cs

// 职责：把参考速度转成独立的喷口朝向、比例油门与显式制动，不把细小速度误差当成直航方向。 // 入口：Evaluate()；已确认的供电能力与刚体阻尼提供前馈，真实施力仍由推进器执行。 // 生命周期：每舰独立持有油门与制动迟滞，停用/重组Reset()；不直接写刚体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| FlightPropulsionControl | [`void Reset()`](../Assets/Scripts/Features/Flight/FlightPropulsionControl.cs#L26) | internal |
| FlightPropulsionControl | [`Command Evaluate( ControlUnit owner, Rigidbody body, Vector3 reference, bool requestBrake, float brakingAcceleration, float response, float deltaTime)`](../Assets/Scripts/Features/Flight/FlightPropulsionControl.cs#L32) | internal |
| FlightPropulsionControl | [`float AvailableAcceleration(ControlUnit owner, Vector3 direction, float mass)`](../Assets/Scripts/Features/Flight/FlightPropulsionControl.cs#L93) | private static |

## Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs

// 职责：保存高度 PID 与接管航向，计算高度、重力补偿、倾斜及偏航稳定建议。 // 入口：StepHeight() / StepHeading() / CalculateTilt() / TryCalculateAngularVelocity()；不读取组件或写刚体。 // 生命周期：停止输出及切换运行归属时 Reset()，不将旧积分带入新物理会话。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverControlSolver | [`void Reset()`](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L25) | internal |
| HoverControlSolver | [`float StepHeading(Vector3 forward, float angularSpeed, float response, float deltaTime, float efficiency)`](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L32) | internal |
| HoverControlSolver | [`float StepHeight( float heightError, float proportionalGain, float integralGain, float derivativeGain, float deltaTime)`](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L54) | internal |
| HoverControlSolver | [`float CalculateGravityCompensation( float mass, float gravityMagnitude, float compensationFactor, float absoluteHeightError, float heightTolerance)`](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L68) | internal static |
| HoverControlSolver | [`TiltCorrection CalculateTilt( Vector3 currentUp, Vector3 angularVelocity, float maximumTiltCos, float proportionalGain, float derivativeGain)`](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L80) | internal static |
| HoverControlSolver | [`bool TryCalculateAngularVelocity( Quaternion rotation, Vector3 currentUp, float rotationSmoothing, float deltaTime, float outputEfficiency, out Vector3 velocity)`](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L106) | internal static |

## Assets/Scripts/Features/Flight/Hover/ThrusterAllocator.cs

// 职责：把期望的力/力矩分配到单元推进器。 // 入口：Solve()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ThrusterAllocator | [`float[] Solve( Vector3[] forceDirs, // 每个推进器的力方向（单位向量，一般 t.transform.up） Vector3[] torqueDirs, // 每个推进器对单位推力产生的力矩方向 = r × dir float[] fMax, // 每个推进器的最大推力 Vector3 desiredForce, // 期望总力（世界坐标） Vector3 desiredTorque, // 期望总力矩（世界坐标） float forceWeight = 1f, // 力的权重（放大力误差在目标函数中的比重） float torqueWeight = 1f, // 力矩的权重 float damping = 1e-3f, // 阻尼λ（数值稳定） int maxIters = 8 // 主动集最大迭代次数 )`](../Assets/Scripts/Features/Flight/Hover/ThrusterAllocator.cs#L15) | public static |
| ThrusterAllocator | [`float[,] MultiplyAT_A(float[,] A, int rows, int cols)`](../Assets/Scripts/Features/Flight/Hover/ThrusterAllocator.cs#L192) | static |
| ThrusterAllocator | [`float[] MultiplyAT_b(float[,] A, int rows, int cols, float[] b)`](../Assets/Scripts/Features/Flight/Hover/ThrusterAllocator.cs#L213) | static |
| ThrusterAllocator | [`void AddDamping(float[,] H, int n, float lambda)`](../Assets/Scripts/Features/Flight/Hover/ThrusterAllocator.cs#L230) | private static |
| ThrusterAllocator | [`bool CholeskySolveInPlace(float[,] H, float[] rhs, int n)`](../Assets/Scripts/Features/Flight/Hover/ThrusterAllocator.cs#L242) | private static |

## Assets/Scripts/Features/Flight/HoverFlightController.cs

// 职责：装配悬浮参数和推进器缓存，在有效游玩归属下应用 HoverControlSolver 的控制输出。 // 入口：Init()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverFlightController | [`void Init()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L66) | public |
| HoverFlightController | [`void OnEnable()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L87) | private |
| HoverFlightController | [`void OnDisable()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L93) | private |
| HoverFlightController | [`void OnValidate()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L99) | private |
| HoverFlightController | [`void FixedUpdate()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L105) | private |
| HoverFlightController | [`bool HasActiveHoverThruster()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L159) | private |
| HoverFlightController | [`void StopControlOutput()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L172) | private |
| HoverFlightController | [`void ClearHoverThrust()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L179) | private |
| HoverFlightController | [`bool EnsureControllerReady()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L204) | private |
| HoverFlightController | [`void RefreshCachedPhysicsValues()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L245) | private |
| HoverFlightController | [`void RefreshTiltLimitCache()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L272) | private |
| HoverFlightController | [`void RebuildThrusterCache()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L278) | private |
| HoverFlightController | [`void DistributeThrust(float heightAdjustment, Vector3 currentUp)`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L350) | private |
| HoverFlightController | [`void ApplyRotationCorrection(Vector3 currentUp, float outputEfficiency)`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L398) | private |
| HoverFlightController | [`void OnDrawGizmosSelected()`](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L423) | private |

## Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs

// 职责：以缓存推进器列表展示关注舰的真实推力和供电状态，支持驾驶、AI与返航导引。 // 入口：BindOwner() / Toggle() / Close() / SelectCategory()；不写推进器参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ThrusterInfoPanel | [`void Awake()`](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L47) | private |
| ThrusterInfoPanel | [`void BindOwner(ControlUnit owner, string status)`](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L60) | public |
| ThrusterInfoPanel | [`void Toggle()`](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L72) | public |
| ThrusterInfoPanel | [`void Close()`](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L78) | public |
| ThrusterInfoPanel | [`void OnDisable()`](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L83) | private |
| ThrusterInfoPanel | [`void SelectCategory(int selected)`](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L91) | public |
| ThrusterInfoPanel | [`void Update()`](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L102) | private |
| ThrusterInfoPanel | [`bool MatchesCategory(Thruster thruster)`](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L177) | private |

## Assets/Scripts/Features/Flight/Presentation/ThrusterVisualEffect.cs

// 职责：依据推进器激活和输出强度更新喷焰表现。 // 入口：Initialize() / SetThrust()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ThrusterVisualEffect | [`void Initialize(Thruster owner)`](../Assets/Scripts/Features/Flight/Presentation/ThrusterVisualEffect.cs#L19) | public |
| ThrusterVisualEffect | [`void SetThrust(float thrustRatio, Vector3 thrustDirection)`](../Assets/Scripts/Features/Flight/Presentation/ThrusterVisualEffect.cs#L24) | public |
| ThrusterVisualEffect | [`void LateUpdate()`](../Assets/Scripts/Features/Flight/Presentation/ThrusterVisualEffect.cs#L30) | private |
| ThrusterVisualEffect | [`void OnDisable()`](../Assets/Scripts/Features/Flight/Presentation/ThrusterVisualEffect.cs#L47) | private |

## Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs

// 职责：计算悬浮推进输出并应用高度/重力/姿态相关反馈。 // 入口：ApplyThrust() / ShouldActivate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverThruster | [`void SetControlledThrust(float desiredThrust)`](../Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs#L11) | public |
| HoverThruster | [`void FixedUpdate()`](../Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs#L16) | private |
| HoverThruster | [`void ApplyThrust()`](../Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs#L51) | public virtual |
| HoverThruster | [`bool ShouldActivate()`](../Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs#L62) | public override |

## Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs

// 职责：将移动输入投影到主推进方向，其余输入推进生命周期复用 Thruster。 // 入口：FixedUpdate() → TickInputThrust()；保留 ApplyThrust() / ShouldActivate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainThruster | [`void FixedUpdate()`](../Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs#L10) | private |
| MainThruster | [`float CalculateInputThrust(Vector3 input, bool active)`](../Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs#L15) | protected override |
| MainThruster | [`void ApplyThrust()`](../Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs#L21) | public |
| MainThruster | [`bool ShouldActivate()`](../Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs#L26) | public override |

## Assets/Scripts/Features/Flight/Thrusters/Thruster.cs

// 职责：提供推进器共用的供电门控、运行归属和力学输出生命周期。 // 入口：TickInputThrust()；BeginGuidance/SetGuidanceOutput/EndGuidance 为机库辅助导引独占输出。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Thruster | [`void BeginGuidance(Object owner)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L31) | public |
| Thruster | [`void SetGuidanceOutput(Object owner, float appliedForce, Vector3 worldDirection)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L47) | public |
| Thruster | [`void AlignGuidanceVisual(Vector3 worldDirection)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L66) | protected virtual |
| Thruster | [`void EndGuidance(Object owner)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L70) | public |
| Thruster | [`bool ShouldActivate()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L83) | public abstract |
| Thruster | [`void Awake()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L84) | protected virtual |
| Thruster | [`void Start()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L91) | protected virtual |
| Thruster | [`void OnTransformChildrenChanged()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L98) | private |
| Thruster | [`void SetRuntimeReferences(ControlUnit owner, Rigidbody ownerRigidbody)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L103) | public |
| Thruster | [`bool RefreshRuntimeReferences()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L110) | protected |
| Thruster | [`bool TryEnsureRigidbody()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L122) | protected |
| Thruster | [`bool IsPlayModeActive()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L132) | protected |
| Thruster | [`bool HasValidRuntimeOwner()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L137) | protected |
| Thruster | [`void TickInputThrust(ref Vector3 input)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L143) | protected |
| Thruster | [`float CalculateInputThrust(Vector3 input, bool active)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L168) | protected virtual |
| Thruster | [`bool ShouldActivateInputThrust(Vector3 input)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L173) | protected |
| Thruster | [`void ApplyDirectionalThrust()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L178) | protected virtual |
| Thruster | [`void CacheLocalReferences()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L217) | protected |
| Thruster | [`void ApplyThrustChangeRateLimit()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L226) | public virtual |
| Thruster | [`void SetPowerRequested(bool requested)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L236) | protected |
| Thruster | [`void OnDisable()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L246) | protected virtual |
| Thruster | [`bool CanApplyThrust()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L254) | protected |
| Thruster | [`void VisualizeThrust()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L260) | public virtual |
| Thruster | [`void VisualizeThrust(bool forceUpdate)`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L265) | public virtual |
| Thruster | [`void DisableLegacyLineRenderer()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L281) | private |
| Thruster | [`void EnsureVisualEffect()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L290) | private |
| Thruster | [`Vector3 GetNormalizedThrustDirection()`](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L300) | private |

## Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs

// 职责：按明确的推进朝向对齐喷口，按油门比例输出；转向未对准时不施加侧向残余推力。 // 入口：FixedUpdate() → TickInputThrust()；保留 ApplyThrust() / ShouldActivate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| UniversalThruster | [`void FixedUpdate()`](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L10) | private |
| UniversalThruster | [`float CalculateInputThrust(Vector3 input, bool active)`](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L15) | protected override |
| UniversalThruster | [`bool IsAligned(Vector3 direction)`](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L22) | private |
| UniversalThruster | [`void ApplyDirectionalThrust()`](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L27) | protected override |
| UniversalThruster | [`void RotateThruster(Vector3 worldDir, bool active)`](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L36) | private |
| UniversalThruster | [`void AlignGuidanceVisual(Vector3 worldDirection)`](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L47) | protected override |
| UniversalThruster | [`void ApplyThrust()`](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L53) | public |
| UniversalThruster | [`bool ShouldActivate()`](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L58) | public override |

## Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs

// 职责：主菜单原生uGUI展示与按钮绑定，导航及文件操作由MainMenuController协调。 // 入口：Bind() / Home() / Picker() / Settings() / SetChoices() / SetBusy()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainMenuView | [`void Bind(MainMenuController owner)`](../Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs#L57) | public |
| MainMenuView | [`void Home(bool canResume, string recentName)`](../Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs#L81) | public |
| MainMenuView | [`void Picker(string title, bool editing)`](../Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs#L92) | public |
| MainMenuView | [`void Settings()`](../Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs#L101) | public |
| MainMenuView | [`void SetChoices(List<string> labels, int selected)`](../Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs#L109) | public |
| MainMenuView | [`void Selection(string text, bool canLaunch, string action)`](../Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs#L118) | public |
| MainMenuView | [`void Message(string text)`](../Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs#L125) | public |
| MainMenuView | [`void SetBusy(bool busy)`](../Assets/Scripts/Features/MainMenu/Interface/MainMenuView.cs#L130) | public |

## Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs

// 职责：菜单中的只读蓝图目录、示例和出战校验；明确编辑示例时才创建独立副本。 // 入口：Read() / Inspect() / CopyExample()；与工坊共用蓝图目录和建造/部署规则。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MenuBlueprintLibrary | [`List<MenuBlueprintEntry> Read(bool building, TextAsset example)`](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L37) | public static |
| MenuBlueprintLibrary | [`List<MenuBlueprintEntry> ReadBases(SiegeConstructionSettings settings)`](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L49) | public static |
| MenuBlueprintLibrary | [`BlueprintFileStore Files(bool building)`](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L73) | internal static |
| MenuBlueprintLibrary | [`BlockDataList Inspect( MenuBlueprintEntry entry, SiegeConstructionSettings siege, out bool playable, out string reason, bool forEditing = false)`](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L78) | public static |
| MenuBlueprintLibrary | [`string CopyExample(MenuBlueprintEntry entry)`](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L136) | public static |

## Assets/Scripts/Features/MainMenu/Library/MenuBlueprintPreview.cs

// 职责：使用现有静态模块预览展示菜单蓝图；独立渲染层不会创建控制单元或参与战斗。 // 入口：Show() / Clear()；所有相机、灯光和预览挂点均保存在MainMenu场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MenuBlueprintPreview | [`void Show(BlockDataList data)`](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintPreview.cs#L14) | public |
| MenuBlueprintPreview | [`void Clear()`](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintPreview.cs#L50) | public |

## Assets/Scripts/Features/MainMenu/MainMenuController.cs

// 职责：独立菜单启动、蓝图选择、工坊/玩法导航；Main按需加载并暂停，保留当前原稿。 // 入口：Choose() / LaunchSelected() / ResumeWorkshop() / ReturnToMenu()。 // 生命周期：菜单服务常驻自身场景，单一协程拥有切换；先结束玩法/保存货仓，再暂停Main。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainMenuController | [`void Start()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L70) | private |
| MainMenuController | [`void ShowHome()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L82) | public |
| MainMenuController | [`void Update()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L91) | private |
| MainMenuController | [`void Choose(MenuDestination target)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L109) | public |
| MainMenuController | [`void ShowBattleAttract()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L123) | public |
| MainMenuController | [`IEnumerator EnterAttract()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L131) | private |
| MainMenuController | [`void ReturnFromAttract()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L159) | public |
| MainMenuController | [`IEnumerator LeaveAttract()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L167) | private |
| MainMenuController | [`void RefreshLibrary()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L189) | public |
| MainMenuController | [`void SelectBlueprint(int index)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L210) | public |
| MainMenuController | [`void LaunchSelected()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L239) | public |
| MainMenuController | [`void CreateBlueprint()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L249) | public |
| MainMenuController | [`IEnumerator CreateAndEdit(bool building)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L257) | private |
| MainMenuController | [`IEnumerator Launch(MenuBlueprintEntry entry, MenuDestination target)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L286) | private |
| MainMenuController | [`void OpenBlueprint(bool building, string name)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L382) | private static |
| MainMenuController | [`void ResumeWorkshop()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L405) | public |
| MainMenuController | [`IEnumerator Resume()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L413) | private |
| MainMenuController | [`void OpenRecent()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L425) | public |
| MainMenuController | [`void ReturnToMenu()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L437) | public |
| MainMenuController | [`IEnumerator Return()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L451) | private |
| MainMenuController | [`IEnumerator EnsureMain()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L499) | private |
| MainMenuController | [`IEnumerator WaitForBlueprint()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L528) | private |
| MainMenuController | [`void Remember(bool building, string name)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L544) | private static |
| MainMenuController | [`void SetPresentation(bool visible)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L551) | private |
| MainMenuController | [`void Quit()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L561) | public |
| MainMenuController | [`void Run(IEnumerator work)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L579) | private |
| MainMenuController | [`void CancelNavigation()`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L587) | public |
| MainMenuController | [`IEnumerator Guard(IEnumerator work)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L596) | private |
| MainMenuController | [`void SetStatus(string message)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L677) | private |
| MainMenuController | [`bool IsDataError(Exception error)`](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L683) | private static |

## Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs

// 职责：菜单临时选择其他探索载具时保留工坊对象、蓝图上下文、缓存和底座。 // 入口：Park() / Restore()；只暂停原稿，绝不把运行结果写回原稿文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MenuPlayDraft | [`void Park()`](../Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs#L20) | public |
| MenuPlayDraft | [`void Restore()`](../Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs#L37) | public |

## Assets/Scripts/Features/MainMenu/Navigation/PauseMenu.cs

// 职责：拥有暂停期间的时间、音频和输入快照，恢复后经既有导航主链返回或重新开始。 // 入口：Toggle() / Resume() / Restart()；预挂于工坊服务，界面由Editor保存。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PauseMenu | [`void Awake()`](../Assets/Scripts/Features/MainMenu/Navigation/PauseMenu.cs#L30) | private |
| PauseMenu | [`void Toggle()`](../Assets/Scripts/Features/MainMenu/Navigation/PauseMenu.cs#L38) | public |
| PauseMenu | [`void Resume()`](../Assets/Scripts/Features/MainMenu/Navigation/PauseMenu.cs#L73) | public |
| PauseMenu | [`void Restart()`](../Assets/Scripts/Features/MainMenu/Navigation/PauseMenu.cs#L91) | public |
| PauseMenu | [`void ReturnToMenu()`](../Assets/Scripts/Features/MainMenu/Navigation/PauseMenu.cs#L97) | private |
| PauseMenu | [`void OnDisable()`](../Assets/Scripts/Features/MainMenu/Navigation/PauseMenu.cs#L103) | private |

## Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs

// 职责：Main工坊的显式菜单返回入口，兼容直接运行Main的开发流程。 // 入口：OpenMenu()；菜单已加载时保持唯一EventSystem，Escape与按钮共用返回主链。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorkshopNavigation | [`void Awake()`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L24) | private |
| WorkshopNavigation | [`void UseMenuEvents()`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L34) | public |
| WorkshopNavigation | [`void OnEnable()`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L39) | private |
| WorkshopNavigation | [`void Update()`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L48) | private |
| WorkshopNavigation | [`void OpenMenu()`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L56) | public |
| WorkshopNavigation | [`void TogglePause()`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L74) | private |
| WorkshopNavigation | [`void ShowNotice(string message)`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L82) | public |
| WorkshopNavigation | [`IEnumerator LoadMenu()`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L88) | private |
| WorkshopNavigation | [`void OnDestroy()`](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L96) | private |

## Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs

// 职责：菜单设置与本地偏好存储；控件值只在用户操作时落盘，初次运行沿用项目默认值。 // 入口：Initialize() / ApplyToWorkshop()；音量、画质、全屏和镜头灵敏度可在Inspector配置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MenuSettings | [`void Initialize()`](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L31) | public |
| MenuSettings | [`void ApplyToWorkshop()`](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L74) | public |
| MenuSettings | [`void SetVolume(float value)`](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L89) | private |
| MenuSettings | [`void SetSensitivity(float value)`](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L96) | private |
| MenuSettings | [`void SetQuality(int value)`](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L103) | private |
| MenuSettings | [`void SetFullscreen(bool value)`](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L110) | private |
| MenuSettings | [`void ShowPreferences(bool preferences)`](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L117) | private |
| MenuSettings | [`void SetAutoRotate(bool value)`](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L123) | private |

## Assets/Scripts/Features/Navigation/Diagnostics/EnemyNavigationSnapshot.cs

// 职责：描述导航控制器当前真实决策，供测试场记录与可视化，不参与驾驶。 // 入口：EnemyController.CaptureNavigation()；路线仅在版本变化时复制。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceCourse.cs

// 职责：保存试验布局的推荐起点、目标点和观察时长，保留可编辑场景参数。 // 入口：EnemyAvoidanceLab.SelectLayout()读取；不生成运行时障碍。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyAvoidanceCourse | [`Transform Location(int index)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceCourse.cs#L19) | public |

## Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs

// 职责：协调可编辑导航试验、手动蓝图部署、目标设置与操作/导航记录。 // 入口：Start() / DeploySelected() / SelectLayout()；真实施力仍由生产AI与推进器完成。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyAvoidanceLab | [`string LayoutName(int index)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L80) | internal |
| EnemyAvoidanceLab | [`void RecordOperation(string action, string detail)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L81) | public |
| EnemyAvoidanceLab | [`void SetRoofVisibility(bool visible)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L82) | internal |
| EnemyAvoidanceLab | [`void BeginPointPick(bool start)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L98) | public |
| EnemyAvoidanceLab | [`string LocationName(int index)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L106) | public |
| EnemyAvoidanceLab | [`bool SelectLocation(int index, bool asStart)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L107) | public |
| EnemyAvoidanceLab | [`void SetStart(Vector3 position)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L128) | public |
| EnemyAvoidanceLab | [`void Start()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L136) | private |
| EnemyAvoidanceLab | [`void RefreshBlueprints()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L155) | public |
| EnemyAvoidanceLab | [`void SelectBlueprint(int index)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L162) | public |
| EnemyAvoidanceLab | [`bool DeploySelected()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L173) | public |
| EnemyAvoidanceLab | [`void BindShip(ControlUnit value)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L219) | private |
| EnemyAvoidanceLab | [`void SelectLayout(int index)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L240) | public |
| EnemyAvoidanceLab | [`void ResetTrial()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L262) | public |
| EnemyAvoidanceLab | [`void FixedUpdate()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L327) | private |
| EnemyAvoidanceLab | [`void Sample()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L360) | private |
| EnemyAvoidanceLab | [`void FinishTrial(string reason)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L381) | private |
| EnemyAvoidanceLab | [`void OnShipCollision(Collision collision)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L417) | private |
| EnemyAvoidanceLab | [`void StopTrial()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L451) | public |
| EnemyAvoidanceLab | [`void BeginSuite()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L460) | public |
| EnemyAvoidanceLab | [`void SetGoal(Vector3 point)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L467) | public |
| EnemyAvoidanceLab | [`void SetPaused(bool value)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L482) | public |
| EnemyAvoidanceLab | [`void Update()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L489) | private |
| EnemyAvoidanceLab | [`void OnGUI()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L494) | private |
| EnemyAvoidanceLab | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L499) | private |
| EnemyAvoidanceLab | [`void OnDisable()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyAvoidanceLab.cs#L514) | private |

## Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabBlueprints.cs

// 职责：聚合测试用只读蓝图目录，复用正式部署校验，保存明确的来源与JSON快照。 // 入口：Refresh()；包含内置敌舰、玩家Saves和EnemyBlueprints，不写源目录。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabBlueprints | [`void Refresh()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabBlueprints.cs#L22) | internal |

## Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs

// 职责：提供试验场透视自由飞行及正交总览，暂停时仍可观察，不接管玩家相机。 // 入口：Update() / SetFreeView() / ResetOverview()；组件由制作工具预挂在Lab Camera上。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabCamera | [`void Awake()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L33) | private |
| EnemyLabCamera | [`void SetFreeView(bool enabled)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L39) | public |
| EnemyLabCamera | [`void ResetOverview()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L63) | public |
| EnemyLabCamera | [`void SyncAngles()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L68) | private |
| EnemyLabCamera | [`void Update()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L74) | private |
| EnemyLabCamera | [`void Record(string action)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L176) | private |
| EnemyLabCamera | [`void ReleasePointer()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L184) | private |
| EnemyLabCamera | [`void OnDisable()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L196) | private |
| EnemyLabCamera | [`void OnApplicationFocus(bool focused)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabCamera.cs#L201) | private |

## Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabMovingObstacle.cs

// 职责：按可重复的周期驱动试验场运动学障碍，保留真实碰撞及暂停语义。 // 入口：OnEnable() / ResetMotion()重置相位；FixedUpdate()提交物理运动。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabMovingObstacle | [`void Awake()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabMovingObstacle.cs#L20) | private |
| EnemyLabMovingObstacle | [`void CaptureOrigin()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabMovingObstacle.cs#L25) | private |
| EnemyLabMovingObstacle | [`void OnEnable()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabMovingObstacle.cs#L37) | private |
| EnemyLabMovingObstacle | [`void ResetMotion()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabMovingObstacle.cs#L42) | public |
| EnemyLabMovingObstacle | [`void FixedUpdate()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabMovingObstacle.cs#L52) | private |
| EnemyLabMovingObstacle | [`void OnDisable()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabMovingObstacle.cs#L61) | private |
| EnemyLabMovingObstacle | [`void OnDrawGizmosSelected()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabMovingObstacle.cs#L69) | private |

## Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabPanel.cs

// 职责：绘制试验场控制面板并采集限定场景内的键鼠操作，调用试验协调器。 // 入口：PollInput() / Draw()；普通对象，不动态补挂组件、不改玩家输入系统。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabPanel | [`void BeginPick(bool start)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabPanel.cs#L23) | internal |
| EnemyLabPanel | [`void PollInput()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabPanel.cs#L30) | internal |
| EnemyLabPanel | [`void ChangeStartHeight(float delta)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabPanel.cs#L144) | private |
| EnemyLabPanel | [`void Draw()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabPanel.cs#L149) | internal |

## Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs

// 职责：将测试元数据、操作事件及导航采样增量写入独立会话目录，避免长测常驻大日志。 // 入口：Begin() / Event() / Sample() / Finish()；文件错误只关闭记录，不影响飞行。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyLabRecorder | [`void Begin( string course, string blueprint, string json, EnemyController ai, Vector3 start, Vector3 goal, float interval)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L75) | public |
| EnemyLabRecorder | [`void Event(string action, string detail)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L129) | public |
| EnemyLabRecorder | [`EnemyNavigationSnapshot Sample(EnemyController ai)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L135) | public |
| EnemyLabRecorder | [`void Finish(string json)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L143) | public |
| EnemyLabRecorder | [`void EnsureSession()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L162) | private |
| EnemyLabRecorder | [`void Write(Entry entry)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L185) | private |
| EnemyLabRecorder | [`void Fail(Exception exception)`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L219) | private |
| EnemyLabRecorder | [`void CloseTrial()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L227) | private |
| EnemyLabRecorder | [`void Dispose()`](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs#L244) | public |

## Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationDebugText.cs

// 职责：把真实导航快照与轨迹指标格式化为可读的追踪报告；不额外查询物理或影响驾驶。 // 入口：Build()生成降频缓存文本；Mode()也用于场景标签。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationDebugText | [`string Mode(EnemyNavigationSnapshot value)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationDebugText.cs#L8) | public static |
| NavigationDebugText | [`string Build(NavigationTrack track)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationDebugText.cs#L18) | public static |
| NavigationDebugText | [`string TimeText(float seconds)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationDebugText.cs#L52) | private static |

## Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs

// 职责：用有界环形历史绘制实际飞行轨迹，沿累计航程编码固定速度标尺。 // 入口：Sample()/Clear()/DrawGizmos()；由试验场与实战追踪共同调用。 // 生命周期：Dispose()恢复作者材质并释放本实例纹理和材质；不写驾驶或刚体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationFlightTrace | [`int Index(int offset)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs#L37) | private |
| NavigationFlightTrace | [`void Clear()`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs#L38) | public |
| NavigationFlightTrace | [`void Sample(Vector3 position, float speed, float minimumSpacing, float fastSpeed)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs#L48) | public |
| NavigationFlightTrace | [`void UpdateColors(float fastSpeed)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs#L94) | private |
| NavigationFlightTrace | [`Color SpeedColor(float speed, float fastSpeed)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs#L138) | private static |
| NavigationFlightTrace | [`void DrawGizmos(float fastSpeed)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs#L146) | public |
| NavigationFlightTrace | [`void Dispose()`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs#L155) | public |
| NavigationFlightTrace | [`void Release(Object value)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationFlightTrace.cs#L168) | private static |

## Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationTrack.cs

// 职责：采样一艘舰船的生产导航、实际航程/停滞和接触，驱动预制轨迹与规划线。 // 入口：Bind()/Sample()/SetVisible()；场景协调器负责选人和采样频率。 // 生命周期：换轮清空历史，同驾驶舱重组可续接；解绑注销碰撞事件，禁用释放速度纹理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationTrack | [`void Bind(ControlUnit owner, string label, bool clearHistory)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationTrack.cs#L53) | public |
| NavigationTrack | [`void ClearHistory()`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationTrack.cs#L75) | public |
| NavigationTrack | [`void Sample()`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationTrack.cs#L93) | public |
| NavigationTrack | [`void SetVisible(bool showTrace, bool showRoute)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationTrack.cs#L134) | public |
| NavigationTrack | [`void OnContact(Collision collision)`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationTrack.cs#L147) | private |
| NavigationTrack | [`void Unsubscribe()`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationTrack.cs#L155) | private |
| NavigationTrack | [`void OnDisable()`](../Assets/Scripts/Features/Navigation/Diagnostics/Tracking/NavigationTrack.cs#L163) | private |

## Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs

// 职责：为 Bot 与舰船探测球形/整舰净空，保持绕障方向，并在没有目标进展时选择脱困路线。 // 入口：Evaluate() 采样导航，ConstrainVelocity() 在每个物理帧阻止继续朝近处障碍加速。 // 生命周期：每个单位拥有独立缓冲和绕障记忆；切换目标时 Reset() 清理旧路线。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationAvoidance | [`void UpdateHull(Bounds bounds)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L44) | public |
| NavigationAvoidance | [`void Configure(NavigationAvoidanceSettings settings)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L49) | public |
| NavigationAvoidance | [`void Reset()`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L55) | public |
| NavigationAvoidance | [`void ClearDebugHits()`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L67) | public |
| NavigationAvoidance | [`NavigationAvoidanceResult Evaluate( Transform target, Vector3 velocity, Vector3 targetDirection, float rangeScale, NavigationAvoidanceSettings settings, float targetDistance = float.PositiveInfinity)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L73) | public |
| NavigationAvoidance | [`bool CheckProgress(float targetDistance)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L169) | private |
| NavigationAvoidance | [`Vector3 ChooseClearDirection(Vector3 targetDirection, float range, bool stalled, out float bestClearDistance)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L189) | private |
| NavigationAvoidance | [`void ConsiderDirection( Vector3 candidate, Vector3 targetDirection, float range, bool stalled, ref float bestScore, ref Vector3 bestDirection, ref float bestClearDistance)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L243) | private |
| NavigationAvoidance | [`Vector3 ConstrainVelocity(Vector3 desiredVelocity, float deltaTime, Vector3 currentVelocity, float brakingAcceleration)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L267) | public |
| NavigationAvoidance | [`Vector3 LimitApproachSpeed(Vector3 desiredVelocity, Vector3 corridor, float lookAhead, float margin, float acceleration)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L300) | private |
| NavigationAvoidance | [`float GetClearDistance(Vector3 direction, float range)`](../Assets/Scripts/Features/Navigation/Local/NavigationAvoidance.cs#L317) | private |

## Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs

// 职责：执行单位所属 PhysicsScene 的球形/整舰查询、碰撞体过滤与重叠分离。 // 入口：Configure()/BeginQuery()、Cast()/TryGetOverlapEscape()；缓存仅属于本单位。 // 饱和查询保守阻止通行；动态舰体缓存定期刷新，忽略条件每次公开决策/物理步失效。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationObstacleQuery | [`void Configure(NavigationAvoidanceSettings settings, Transform target)`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L38) | internal |
| NavigationObstacleQuery | [`void BeginQuery()`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L54) | internal |
| NavigationObstacleQuery | [`void UpdateHull(Bounds bounds)`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L59) | internal |
| NavigationObstacleQuery | [`void ClearOverlap()`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L65) | internal |
| NavigationObstacleQuery | [`bool Cast(Vector3 direction, float range, out RaycastHit nearest)`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L71) | internal |
| NavigationObstacleQuery | [`bool TryGetOverlapEscape(out Vector3 escape)`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L158) | internal |
| NavigationObstacleQuery | [`Bounds CurrentBounds()`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L240) | private |
| NavigationObstacleQuery | [`bool IsIgnored(Collider collider)`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L247) | private |
| NavigationObstacleQuery | [`bool ShouldIgnoreCollider(Collider collider)`](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs#L263) | private |

## Assets/Scripts/Features/Navigation/NavigationAvoidanceResult.cs

// 职责：表达一次导航采样的方向修正、强度、速度比例和障碍数量。 // Direction 在世界坐标中；没有障碍时 SpeedMultiplier 为 1。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Navigation/NavigationAvoidanceSettings.cs

// 职责：携带单次障碍采样的距离、权重、过滤条件；由 Bot 策略或舰船 Inspector 参数构造。 // 单位：Range/Radius/ProgressDistance 为米，Timeout/HoldTime 为秒，BrakingAcceleration 为米/秒²。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs

// 职责：在有限三维栅格中规划整舰可通行路线，缓存并简化航点，不直接操纵刚体。 // 入口：TryFollow()；转弯代价、限额可视简化和短期运动包络共用整舰净空，重规划限频。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationLocalPath | [`void Push(Vector3Int key, float cost, float score)`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L71) | private |
| NavigationLocalPath | [`OpenNode Pop()`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L96) | private |
| NavigationLocalPath | [`void Clear()`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L128) | internal |
| NavigationLocalPath | [`bool TryFollow( ControlUnit owner, Bounds hull, Vector3 goal, LayerMask layers, float cellSize, float radius, int budget, float retry, float minHeight, float maxHeight, out Vector3 point)`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L140) | internal |
| NavigationLocalPath | [`Vector3 World(Vector3Int cell, float size)`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L388) | private |
| NavigationLocalPath | [`void Simplify(Vector3 position)`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L393) | private |
| NavigationLocalPath | [`void Capture(ControlUnit owner, Bounds hull, LayerMask layers, float radius)`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L419) | private |
| NavigationLocalPath | [`bool ClearFlight(Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L485) | private |
| NavigationLocalPath | [`bool ClearSegment(Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Navigation/Planning/NavigationLocalPath.cs#L491) | private |

## Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs

// 职责：为自主舰按真实航迹优先选择上/下避障，验证整舰净空并保持高度直到舰尾越障。 // 入口：TryGetHeight()；仅输出目标高度，不写刚体，不替代顶棚下的水平寻路。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationObstacleClimb | [`void Clear()`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L18) | internal |
| NavigationObstacleClimb | [`bool TryGetHeight( ControlUnit unit, Vector3 direction, float baseHeight, float lookAhead, float maxRise, float clearance, LayerMask layers, out float targetHeight, float maxDrop = 0f)`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L25) | internal |
| NavigationObstacleClimb | [`bool CanPass( PhysicsScene physics, Bounds bounds, Vector3 direction, float distance, float deltaHeight, LayerMask layers, ControlUnit unit)`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L131) | private |
| NavigationObstacleClimb | [`float ForwardRatio( ControlUnit owner, Vector3 direction, float targetHeight, float cruiseSpeed, float clearance, float lead, LayerMask layers)`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L158) | internal |
| NavigationObstacleClimb | [`bool CanLowerTo(ControlUnit unit, float targetHeight, Vector3 direction, float distance, LayerMask layers)`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L211) | internal |
| NavigationObstacleClimb | [`Bounds GetHullBounds(ControlUnit unit)`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L225) | internal |
| NavigationObstacleClimb | [`bool Blocked( PhysicsScene physics, Vector3 center, Vector3 extents, Vector3 direction, float distance, LayerMask layers, ControlUnit owner)`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L247) | private |
| NavigationObstacleClimb | [`int Cast( PhysicsScene physics, Vector3 center, Vector3 extents, Vector3 direction, float distance, LayerMask layers)`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L279) | private |
| NavigationObstacleClimb | [`bool IsGeometry(Collider collider, ControlUnit owner)`](../Assets/Scripts/Features/Navigation/Planning/NavigationObstacleClimb.cs#L309) | private static |

## Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs

// 职责：周向采集远场路况，沿路线预读距离/时间，按相对运动扫掠近场候选航迹并提前调速或让行。 // 入口：Observe() / CopyPlanningBounds() / Choose()；降频采集快照供规划与动作验证，不写刚体，不读取试验场未来参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| NavigationPrediction | [`void Clear()`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L109) | internal |
| NavigationPrediction | [`void Observe( ControlUnit owner, Bounds hull, Vector3 velocity, LayerMask layers, float speed, float horizon, Vector3 destination, System.Collections.Generic.IReadOnlyList<Vector3> route, int waypoint, float height)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L138) | internal |
| NavigationPrediction | [`void CopyPlanningBounds(System.Collections.Generic.List<Bounds> destination, Vector3 padding, float predictionTime)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L171) | internal |
| NavigationPrediction | [`Vector3 Choose( ControlUnit owner, Bounds hull, Vector3 velocity, Vector3 desired, float targetHeight, LayerMask layers, float cruiseSpeed, float horizon, float acceleration, float turnRate, bool preserveCourse, Vector3 destination, System.Collections.Generic.IReadOnlyList<Vector3> route, int waypoint)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L193) | internal |
| NavigationPrediction | [`void Capture( ControlUnit owner, Bounds hull, Vector3 velocity, LayerMask layers, float cruiseSpeed, float horizon)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L343) | private |
| NavigationPrediction | [`void Preview( Bounds hull, Vector3 destination, System.Collections.Generic.IReadOnlyList<Vector3> route, int waypoint, Vector3 root, float height, float speed)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L440) | private |
| NavigationPrediction | [`Outcome Evaluate( Bounds hull, Vector3 velocity, Vector3 input, float deltaHeight, float speed, float horizon, float acceleration, float turnRate)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L522) | private |
| NavigationPrediction | [`void Consider(ref Outcome best, Outcome candidate, Vector3 desired)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L604) | private |
| NavigationPrediction | [`float Score(Outcome candidate, Vector3 desired)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L622) | private |
| NavigationPrediction | [`float Clearance(Bounds bounds, Vector3 point)`](../Assets/Scripts/Features/Navigation/Prediction/NavigationPrediction.cs#L629) | private static |

## Assets/Scripts/Features/Navigation/Vessels/VesselNavigation.cs

// 职责：组合整舰 A*、升降与动态预测，并复用 NavigationAvoidance 的绕行、脱困和速度安全约束。 // 入口：Evaluate() 降频选路；ConstrainVelocity() 逐物理步验证实际运动走廊；不写刚体。 // 生命周期：每艘友舰/敌舰独占实例；任务改变、禁用与回收均 Reset()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VesselNavigation | [`void Reset()`](../Assets/Scripts/Features/Navigation/Vessels/VesselNavigation.cs#L26) | internal |
| VesselNavigation | [`void ConfigureLocal(VesselNavigationSettings settings)`](../Assets/Scripts/Features/Navigation/Vessels/VesselNavigation.cs#L41) | private |
| VesselNavigation | [`Vector3 ConstrainVelocity(ControlUnit unit, Vector3 desired, Vector3 velocity, float brakingAcceleration)`](../Assets/Scripts/Features/Navigation/Vessels/VesselNavigation.cs#L61) | internal |
| VesselNavigation | [`Vector3 Evaluate( ControlUnit unit, Rigidbody movementBody, Vector3 desiredMove, Vector3 destination, bool navigationRequested, ControlUnit target, VesselNavigationSettings settings)`](../Assets/Scripts/Features/Navigation/Vessels/VesselNavigation.cs#L74) | internal |
| VesselNavigation | [`bool TryClimbObstacle( ControlUnit unit, Vector3 desiredMove, Vector3 probeDirection, float speed, float horizon, VesselNavigationSettings settings, out Vector3 movement)`](../Assets/Scripts/Features/Navigation/Vessels/VesselNavigation.cs#L205) | private |

## Assets/Scripts/Features/Navigation/Vessels/VesselNavigationSettings.cs

// 职责：传递舰船 Inspector 导航参数的单次快照；不持有单位或运行状态。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Player/InputManager.cs

// 职责：采样输入并协调建造/游玩模式、快捷键和光标状态。 // 入口：EnterBuildMode()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| InputManager | [`void Awake()`](../Assets/Scripts/Features/Player/InputManager.cs#L30) | private |
| InputManager | [`void Start()`](../Assets/Scripts/Features/Player/InputManager.cs#L35) | private |
| InputManager | [`void Update()`](../Assets/Scripts/Features/Player/InputManager.cs#L46) | private |
| InputManager | [`void EnterBuildMode()`](../Assets/Scripts/Features/Player/InputManager.cs#L168) | public |
| InputManager | [`void PollBuildNavigation()`](../Assets/Scripts/Features/Player/InputManager.cs#L186) | private |
| InputManager | [`void OnApplicationFocus(bool focused)`](../Assets/Scripts/Features/Player/InputManager.cs#L212) | private |
| InputManager | [`void OnApplicationPause(bool paused)`](../Assets/Scripts/Features/Player/InputManager.cs#L220) | private |
| InputManager | [`void ReleaseBuildNavigation()`](../Assets/Scripts/Features/Player/InputManager.cs#L228) | private |
| InputManager | [`void PollSiege(SiegeSession siege, Keyboard keyboard)`](../Assets/Scripts/Features/Player/InputManager.cs#L239) | private |
| InputManager | [`void ApplyCursorState(bool showCursor)`](../Assets/Scripts/Features/Player/InputManager.cs#L265) | private |

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

## Assets/Scripts/Features/Power/Devices/PowerGeneratingUnit.cs

// 职责：保存发电量并登记已启用的发电机，不承担电网遍历或负载分配。 // 入口：OnEnable() / OnDisable()；PowerNetwork.Rebuild() 读取 ActiveUnits 和 outputPower。 // 生命周期：退出时清除中继的反向引用，下一次电网重建排除该发电机。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerGeneratingUnit | [`void OnEnable()`](../Assets/Scripts/Features/Power/Devices/PowerGeneratingUnit.cs#L16) | private |
| PowerGeneratingUnit | [`void OnDisable()`](../Assets/Scripts/Features/Power/Devices/PowerGeneratingUnit.cs#L21) | private |

## Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs

// 职责：保存中继参数，登记生命周期并装配每帧一次的无线电网重建。 // 入口：Update() → PowerNetwork.Rebuild()；调试连接由 PowerConnectionVisuals 独立绘制。 // 生命周期：退出时移除双向连接；最后一个中继退出立即清除全体负载供电。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerTransmissionDevice | [`void Awake()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L40) | private |
| PowerTransmissionDevice | [`void OnValidate()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L45) | private |
| PowerTransmissionDevice | [`void OnEnable()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L50) | private |
| PowerTransmissionDevice | [`void Update()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L61) | private |
| PowerTransmissionDevice | [`void OnDisable()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L74) | private |
| PowerTransmissionDevice | [`void Disconnect()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L92) | private |
| PowerTransmissionDevice | [`void CacheDebugReferences()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L115) | private |
| PowerTransmissionDevice | [`void UpdateDebugVisuals()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L129) | private |
| PowerTransmissionDevice | [`void OnDrawGizmosSelected()`](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L142) | private |

## Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs



| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerNetworkBaseline | [`void Rebuild( IEnumerable<PowerTransmissionDevice> devices, IEnumerable<PowerGeneratingUnit> generators, IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L23) | public |
| PowerNetworkBaseline | [`void Clear(IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L36) | public |
| PowerNetworkBaseline | [`void CollectActiveParticipants( IEnumerable<PowerTransmissionDevice> devices, IEnumerable<PowerGeneratingUnit> generators, IEnumerable<Power> loads)`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L57) | private |
| PowerNetworkBaseline | [`void ResetConnectionsAndPower()`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L90) | private |
| PowerNetworkBaseline | [`void BuildConnections()`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L110) | private |
| PowerNetworkBaseline | [`void ConnectGenerators(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L130) | private |
| PowerNetworkBaseline | [`void DistributeNetworkPower()`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L142) | private |
| PowerNetworkBaseline | [`void CollectConnectedComponent(PowerTransmissionDevice rootDevice)`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L155) | private |
| PowerNetworkBaseline | [`void VisitDevices(List<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L180) | private |
| PowerNetworkBaseline | [`void CollectDeviceLoads(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L191) | private |
| PowerNetworkBaseline | [`void SupplyConnectedComponent()`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L202) | private |
| PowerNetworkBaseline | [`void CommitPowerStatuses()`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L239) | private |
| PowerNetworkBaseline | [`bool IsWithinBoxRange(Vector3 firstPosition, Vector3 secondPosition, float range)`](../Assets/Scripts/Features/Power/Network/PowerNetworkBaseline.cs#L247) | private static |

## Assets/Scripts/Features/Power/Network/PowerPositionIndex.cs

// 职责：按世界坐标网格索引供电参与者，复用桶与候选列表以减少全体范围扫描。 // 入口：Clear() / Add() / Query()；候选仍须由调用者执行精确盒边界检查。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerPositionIndex | [`void Clear()`](../Assets/Scripts/Features/Power/Network/PowerPositionIndex.cs#L12) | internal |
| PowerPositionIndex | [`void Add(Vector3 position, int index)`](../Assets/Scripts/Features/Power/Network/PowerPositionIndex.cs#L24) | internal |
| PowerPositionIndex | [`void Query(Vector3 position, float range, List<int> result)`](../Assets/Scripts/Features/Power/Network/PowerPositionIndex.cs#L37) | internal |
| PowerPositionIndex | [`Vector3Int GetCell(Vector3 position)`](../Assets/Scripts/Features/Power/Network/PowerPositionIndex.cs#L70) | private static |

## Assets/Scripts/Features/Power/Power.cs

// 职责：记录各使用者的工作意图及当前供电，空闲负载不占功率，供电短缺提供效率。 // 入口：使用者先 SetWorkRequested()；PowerNetwork.Rebuild() 顺序调用 ResetPower() / ReceivePower() / CommitPowerStatus()。 // 生命周期：启用时登记负载；最后请求撤销或退出立即清零；独立电网累加供电但不超出需求。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Power | [`void OnEnable()`](../Assets/Scripts/Features/Power/Power.cs#L58) | private |
| Power | [`void OnDisable()`](../Assets/Scripts/Features/Power/Power.cs#L65) | private |
| Power | [`void LateUpdate()`](../Assets/Scripts/Features/Power/Power.cs#L75) | private |
| Power | [`void SetWorkRequested(Object requester, bool requested)`](../Assets/Scripts/Features/Power/Power.cs#L85) | public |
| Power | [`void TrackAvailability(bool tracked)`](../Assets/Scripts/Features/Power/Power.cs#L110) | internal |
| Power | [`void ResetPower()`](../Assets/Scripts/Features/Power/Power.cs#L116) | public |
| Power | [`void ReceivePower(float suppliedPower)`](../Assets/Scripts/Features/Power/Power.cs#L123) | public |
| Power | [`void CommitPowerStatus()`](../Assets/Scripts/Features/Power/Power.cs#L128) | public |
| Power | [`bool IsDestroyedRequester(Object requester)`](../Assets/Scripts/Features/Power/Power.cs#L136) | private static |

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
| PowerNetwork | [`void DistributeNetworkPower()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L309) | private |
| PowerNetwork | [`void CollectConnectedComponent(PowerTransmissionDevice rootDevice)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L332) | private |
| PowerNetwork | [`void VisitDevices(List<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L356) | private |
| PowerNetwork | [`void CollectDeviceLoads(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L367) | private |
| PowerNetwork | [`void SupplyConnectedComponent()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L383) | private |
| PowerNetwork | [`void CommitPowerStatuses()`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L421) | private |
| PowerNetwork | [`bool IsWithinBoxRange(Vector3 firstPosition, Vector3 secondPosition, float range)`](../Assets/Scripts/Features/Power/PowerNetwork.cs#L429) | private static |

## Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs

// 职责：复用 LineRenderer 绘制中继与发电机之间的虚线连接。 // 入口：Refresh() / Hide()；由所属 PowerTransmissionDevice 的生命周期调用。 // 边界：只读取电网结果，不计算供电；线对象由所属 GameObject 持有并随其销毁。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerConnectionVisuals | [`void Refresh(DebugManager settings)`](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L19) | public |
| PowerConnectionVisuals | [`void Hide()`](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L48) | public |
| PowerConnectionVisuals | [`void HideUnusedLines(int firstUnusedIndex)`](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L53) | private |
| PowerConnectionVisuals | [`void DrawConnection(int index, Vector3 end, Color color, DebugManager settings)`](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L64) | private |
| PowerConnectionVisuals | [`LineRenderer GetOrCreateLine(int index)`](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L92) | private |
| PowerConnectionVisuals | [`void ConfigureLine(LineRenderer line)`](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L114) | private static |
| PowerConnectionVisuals | [`Material GetDashedLineMaterial()`](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L126) | private static |
| PowerConnectionVisuals | [`Texture2D CreateDashTexture()`](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L148) | private static |

## Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs

// 职责：构建供电范围盒的并集表面，缓存网格签名并拥有临时网格/材质资源。 // 入口：Refresh() / Clear() / Hide() / Dispose()；由 DebugManager 管理生命周期。 // 边界：只读取供电结果；不创建状态图标 Canvas，不处理玩法或全局调试开关。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PowerRangeUnionView | [`void Refresh( IList<PowerTransmissionDevice> devices, bool show, Color isolatedColor, Color connectedColor, Color poweredColor)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L36) | internal |
| PowerRangeUnionView | [`void Invalidate()`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L61) | internal |
| PowerRangeUnionView | [`void Hide()`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L66) | internal |
| PowerRangeUnionView | [`void Clear()`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L71) | internal |
| PowerRangeUnionView | [`void Dispose()`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L79) | internal |
| PowerRangeUnionView | [`void EnsurePowerRangeObject()`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L100) | private |
| PowerRangeUnionView | [`void BuildPowerRangeMesh(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L125) | private |
| PowerRangeUnionView | [`void CollectCoordinates(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L227) | private |
| PowerRangeUnionView | [`void AddXFace(int x, int y, int z, bool positive, int materialIndex)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L255) | private |
| PowerRangeUnionView | [`void AddYFace(int x, int y, int z, bool positive, int materialIndex)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L267) | private |
| PowerRangeUnionView | [`void AddZFace(int x, int y, int z, bool positive, int materialIndex)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L279) | private |
| PowerRangeUnionView | [`void AddQuad( Vector3 a, Vector3 b, Vector3 c, Vector3 d, int materialIndex, bool reverse)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L291) | private |
| PowerRangeUnionView | [`void EnsurePowerRangeMaterials( IList<PowerTransmissionDevice> devices, Color isolatedRangeColor, Color connectedRangeColor, Color poweredRangeColor)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L326) | private |
| PowerRangeUnionView | [`int CalculatePowerRangeSignature(IList<PowerTransmissionDevice> devices)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L382) | private static |
| PowerRangeUnionView | [`int GetPowerState(PowerTransmissionDevice device)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L406) | private static |
| PowerRangeUnionView | [`int FindCoordinateIndex(List<float> values, float target)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L416) | private static |
| PowerRangeUnionView | [`void SortAndUnique(List<float> values)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L433) | private static |
| PowerRangeUnionView | [`void SetPowerRangeMeshVisible(bool visible)`](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L445) | private |

## Assets/Scripts/Features/Presentation/Attract/BattleAttractCamera.cs

// 职责：轮换可编辑镜头轨道、基地仰视和存活飞船特写，独立于玩家输入相机。 // 入口：LateUpdate()；每段5–10秒，参数和路径保存在场景，暂停时不推进。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BattleAttractCamera | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/Attract/BattleAttractCamera.cs#L28) | private |

## Assets/Scripts/Features/Presentation/Attract/BattleAttractStage.cs

// 职责：独立待机战场的内置舰队、基地恢复和有限烟火编排，不访问个人蓝图或结算金币。 // 入口：Start()/Update()；复用生产模块工厂、建筑和武器链，按槽位替换阵亡/越界舰。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BattleAttractStage | [`void Start()`](../Assets/Scripts/Features/Presentation/Attract/BattleAttractStage.cs#L51) | private |
| BattleAttractStage | [`void RestoreBase()`](../Assets/Scripts/Features/Presentation/Attract/BattleAttractStage.cs#L62) | private |
| BattleAttractStage | [`void Update()`](../Assets/Scripts/Features/Presentation/Attract/BattleAttractStage.cs#L95) | private |
| BattleAttractStage | [`void Spawn(int index)`](../Assets/Scripts/Features/Presentation/Attract/BattleAttractStage.cs#L173) | private |
| BattleAttractStage | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Attract/BattleAttractStage.cs#L217) | private |

## Assets/Scripts/Features/Presentation/Blocks/BlockStatusLight.cs

// 职责：将设备供电、机器人任务与方块状态映射为状态灯表现。 // 入口：Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockStatusLight | [`void Update()`](../Assets/Scripts/Features/Presentation/Blocks/BlockStatusLight.cs#L20) | private |

## Assets/Scripts/Features/Presentation/Blocks/CargoHoldView.cs

// 职责：根据货仓内容刷新可见货物，不参与库存存档或结算。 // 入口：Awake()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoHoldView | [`void Awake()`](../Assets/Scripts/Features/Presentation/Blocks/CargoHoldView.cs#L27) | private |
| CargoHoldView | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/Blocks/CargoHoldView.cs#L33) | private |

## Assets/Scripts/Features/Presentation/Blocks/CargoVisual.cs

// 职责：为货物或掉落对象建立对应的视觉表示。 // 入口：Create()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoVisual | [`GameObject Create(string resourcePath, Transform parent, float size)`](../Assets/Scripts/Features/Presentation/Blocks/CargoVisual.cs#L8) | public static |

## Assets/Scripts/Features/Presentation/Blocks/HoverControllerStatusLight.cs

// 职责：将悬浮控制状态映射为控制器灯光反馈。 // 入口：Awake() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HoverControllerStatusLight | [`void Awake()`](../Assets/Scripts/Features/Presentation/Blocks/HoverControllerStatusLight.cs#L20) | private |
| HoverControllerStatusLight | [`void Update()`](../Assets/Scripts/Features/Presentation/Blocks/HoverControllerStatusLight.cs#L26) | private |

## Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs

// 职责：按源材质和脉冲配置共享发光材质；保留外部PropertyBlock及选择高亮的所有权。 // 入口：IndustrialPartMotion创建/释放绑定，Tick()每帧更新唯一材质一次。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IndustrialEmissionBinding | [`bool UsesGpuDrawing()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L67) | private static |
| IndustrialEmissionBinding | [`void Apply(Color emission)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L73) | internal |
| IndustrialEmissionBinding | [`void Tick(float unscaledTime)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L96) | internal static |
| IndustrialEmissionBinding | [`Material[] CaptureSelection(Renderer target)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L111) | internal static |
| IndustrialEmissionBinding | [`void RestoreSelection(Renderer target, Material[] saved)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L124) | internal static |
| IndustrialEmissionBinding | [`void Dispose()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L130) | public |
| IndustrialEmissionBinding | [`bool Matches(Material[] left, Material[] right)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L166) | private static |
| PulseKey | [`bool Equals(PulseKey other)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L198) | public |
| PulseKey | [`bool Equals(object other)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L203) | public override |
| PulseKey | [`int GetHashCode()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L208) | public override |
| Pulse | [`void Update(float unscaledTime)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L230) | internal |

## Assets/Scripts/Features/Presentation/Blocks/IndustrialMotionDriver.cs

// 职责：每场景一个可见调度器，集中驱动该场景工业零件表现。 // 入口：Update()；组件由场景 Managers 持有。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IndustrialMotionDriver | [`void Update()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialMotionDriver.cs#L7) | private |

## Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs

// 职责：为工业零件提供配置驱动的机械运动表现。 // 入口：Configure()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IndustrialPartMotion | [`bool ControlsVisual(Transform part, Renderer renderer)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L32) | internal |
| IndustrialPartMotion | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L37) | private |
| IndustrialPartMotion | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L52) | private |
| IndustrialPartMotion | [`void AnimateAll()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L68) | internal static |
| IndustrialPartMotion | [`void AnimateScene(UnityEngine.SceneManagement.Scene scene)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L79) | internal static |
| IndustrialPartMotion | [`void Animate(float deltaTime, float unscaledTime)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L93) | private |
| IndustrialPartMotion | [`void Configure( Transform[] newSpinTargets, Vector3 newSpinAxis, float newDegreesPerSecond, Transform newBobTarget, float newBobAmplitude, float newBobFrequency, Renderer[] newGlowRenderers, Color newBaseEmission, float newEmissionPulse)`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L117) | public |
| IndustrialPartMotion | [`void CacheRuntimeState()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L141) | private |
| IndustrialPartMotion | [`void ReleaseEmission()`](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L159) | private |

## Assets/Scripts/Features/Presentation/Camera/CameraController.cs

// 职责：处理自由镜头、目标聚焦、平滑移动及加载环绕运动。 // 入口：FocusCameraOnBlock() / SmoothFocusCameraOnBlock() / SmoothFocusCameraOnBlockFramedBy()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CameraController | [`void Awake()`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L44) | private |
| CameraController | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L49) | private |
| CameraController | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L54) | private |
| CameraController | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L63) | private |
| CameraController | [`void HandleLook()`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L72) | private |
| CameraController | [`Vector3 ResolveThirdPersonCollision(Vector3 lookTarget, Vector3 desiredPosition)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L132) | private |
| CameraController | [`bool IsPlayerCollider(Collider collider)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L183) | private |
| CameraController | [`void HandleMovement()`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L188) | private |
| CameraController | [`void FocusCameraOnBlock(GameObject obj)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L208) | public |
| CameraController | [`void SmoothFocusCameraOnBlock(GameObject obj, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L231) | public |
| CameraController | [`void SmoothFocusCameraOnBlockFramedBy(GameObject lookObj, GameObject frameObj, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L251) | public |
| CameraController | [`void SmoothOrbitCameraAroundBlock(GameObject frameObj, float yawDegrees, float pitchDegrees, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L281) | public |
| CameraController | [`void SmoothOrbitCameraAroundBlock(GameObject frameObj, float yawDegrees, float pitchDegrees, float radiusMultiplier, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L286) | public |
| CameraController | [`void SmoothOrbitCameraAroundBlock( GameObject frameObj, Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radiusMultiplier, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L307) | public |
| CameraController | [`void StartContinuousOrbitCameraAroundBlock( GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L335) | public |
| CameraController | [`void StartContinuousOrbitCameraAroundBounds( Bounds frameBounds, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L366) | public |
| CameraController | [`void StopCameraMotion()`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L397) | public |
| CameraController | [`IEnumerator SmoothFocusRoutine(Vector3 targetPosition, Quaternion targetRotation, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L408) | private |
| CameraController | [`IEnumerator ContinuousOrbitRoutine( GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L436) | private |
| CameraController | [`IEnumerator ContinuousOrbitRoutine( Bounds frameBounds, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L477) | private |
| CameraController | [`float CalculateOrbitRadiusMultiplier(float yawDegrees, float radiusVariation, float radiusWaveDegrees)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L512) | private |
| CameraController | [`IEnumerator SmoothOrbitRoutine(Vector3 orbitCenter, float targetYaw, float targetPitch, float targetRadius, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L524) | private |
| CameraController | [`void SetOrbitPose(Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radius)`](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L565) | private |

## Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs

// 职责：计算目标包围盒、视锥取景距离及环绕姿态，不处理输入或协程。 // 入口：TryGetFocusPose() / CalculateFramingDistance()；镜头和网格尺寸由调用者提供。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CameraFraming | [`Vector3 GetOrbitCameraDirection(float yawDegrees, float pitchDegrees)`](../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L18) | public |
| CameraFraming | [`bool TryGetFocusPose(GameObject obj, out Vector3 targetPosition, out Quaternion targetRotation)`](../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L26) | public |
| CameraFraming | [`bool TryGetFocusPose(Bounds frameBounds, Vector3 lookPoint, out Vector3 targetPosition, out Quaternion targetRotation)`](../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L38) | public |
| CameraFraming | [`bool TryGetOrbitPose( Bounds frameBounds, float yawDegrees, float pitchDegrees, float radiusMultiplier, out Vector3 targetPosition, out Quaternion targetRotation)`](../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L61) | public |
| CameraFraming | [`float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint)`](../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L89) | public |
| CameraFraming | [`float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint, Vector3 cameraDirection)`](../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L94) | public |
| CameraFraming | [`bool TryCalculateBlockBounds(GameObject obj, out Bounds bounds)`](../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L139) | public |

## Assets/Scripts/Features/Presentation/Camera/CameraShake.cs

// 职责：管理叠加镜头偏移及协程，取消或销毁宿主时移除残留偏移。 // 入口：Play() / Cancel()；显式注入宿主与目标 Camera。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CameraShake | [`void Cancel()`](../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L17) | public |
| CameraShake | [`void Play(Camera camera, float amplitude, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L29) | public |
| CameraShake | [`IEnumerator CameraShakeRoutine(float amplitude, float duration)`](../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L57) | private |
| CameraShake | [`void ClearCameraOffset()`](../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L76) | private |

## Assets/Scripts/Features/Presentation/Effects/BeamFade.cs

// 职责：按未缩放时间淡出能量线；池实例到期回收，非池实例仍销毁。 // 入口：Initialize()；复用时清零寿命，Update()完成淡出与释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BeamFade | [`void SetPool(TransientEffectPool owner, StylizedBeamEffect effect)`](../Assets/Scripts/Features/Presentation/Effects/BeamFade.cs#L13) | internal |
| BeamFade | [`void Initialize(StylizedBeamEffect effect, float lifetime)`](../Assets/Scripts/Features/Presentation/Effects/BeamFade.cs#L19) | public |
| BeamFade | [`void Update()`](../Assets/Scripts/Features/Presentation/Effects/BeamFade.cs#L26) | private |

## Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs

// 职责：管理可编辑 VFX 资源和效果生成/回收预算，供玩法调用。 // 入口：Play()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockVfxLibrary | [`VfxEffect Resolve(string id)`](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L53) | public |
| BlockVfxLibrary | [`VfxEffect Burst(Effect kind)`](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L89) | private |
| BlockVfxLibrary | [`SpawnResult Play(Effect kind, Vector3 position, Quaternion rotation, float scale = 1f)`](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112) | public static |
| BlockVfxLibrary | [`SpawnResult Play(string id, Vector3 position, Quaternion rotation, float scale = 1f)`](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L124) | public static |
| BlockVfxLibrary | [`SpawnResult PlayTemplate(VfxEffect template, Vector3 position, Quaternion rotation, float scale)`](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L135) | private static |

## Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs

// 职责：管理断裂零件烟迹和停止后的尾烟生命周期。 // 入口：Attach()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| DetachedPartSmokeTrail | [`void Attach(Rigidbody body, Vector3 worldAnchor, float effectIntensity)`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L37) | public static |
| DetachedPartSmokeTrail | [`void Initialize(Rigidbody body, Vector3 worldAnchor, float effectIntensity)`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L62) | private |
| DetachedPartSmokeTrail | [`void ConfigureEnvelope()`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L90) | private |
| DetachedPartSmokeTrail | [`void UpdateEnvelope()`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L120) | private |
| DetachedPartSmokeTrail | [`void Refresh(Vector3 worldAnchor, float effectIntensity)`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L136) | private |
| DetachedPartSmokeTrail | [`void Update()`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L148) | private |
| DetachedPartSmokeTrail | [`void StopAndRelease()`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L185) | private |
| DetachedPartSmokeTrail | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L209) | private |
| DetachedPartSmokeTrail | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L214) | private |

## Assets/Scripts/Features/Presentation/Effects/LightFade.cs

// 职责：按未缩放时间衰减闪光；池实例到期回收，非池实例仍销毁。 // 入口：Initialize()；复用时清零寿命，Update()完成淡出与释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| LightFade | [`void SetPool(TransientEffectPool owner, Light light)`](../Assets/Scripts/Features/Presentation/Effects/LightFade.cs#L14) | internal |
| LightFade | [`void Initialize(Light light, float lifetime, float intensity)`](../Assets/Scripts/Features/Presentation/Effects/LightFade.cs#L20) | public |
| LightFade | [`void Update()`](../Assets/Scripts/Features/Presentation/Effects/LightFade.cs#L28) | private |

## Assets/Scripts/Features/Presentation/Effects/MeteorLightToken.cs

// 职责：统计仍存活的陨石灯，释放时归还灯光预算。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MeteorLightToken | [`void Awake()`](../Assets/Scripts/Features/Presentation/Effects/MeteorLightToken.cs#L12) | private |
| MeteorLightToken | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Effects/MeteorLightToken.cs#L17) | private |

## Assets/Scripts/Features/Presentation/Effects/MeteorTrailFollower.cs

// 职责：让独立尾迹跟随陨石，陨石销毁后停止并释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MeteorTrailFollower | [`void Initialize(Transform meteor, VfxEffect effect)`](../Assets/Scripts/Features/Presentation/Effects/MeteorTrailFollower.cs#L9) | public |
| MeteorTrailFollower | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/Effects/MeteorTrailFollower.cs#L18) | private |
| MeteorTrailFollower | [`void Follow()`](../Assets/Scripts/Features/Presentation/Effects/MeteorTrailFollower.cs#L36) | private |

## Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs

// 职责：更新能量/维修光束的起终点、材质与可见性。 // 入口：Configure() / SetEndpoints() / SetColor()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| StylizedBeamEffect | [`void Configure(float width, string effectId = "EnergyBeam")`](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L17) | public |
| StylizedBeamEffect | [`void SetEndpoints(Vector3 start, Vector3 end)`](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L29) | public |
| StylizedBeamEffect | [`void SetColor(Color color)`](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L36) | public |
| StylizedBeamEffect | [`void SetIntensity(float value)`](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L45) | public |
| StylizedBeamEffect | [`void SetVisible(bool value)`](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L54) | public |
| StylizedBeamEffect | [`void UpdateBeam()`](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L84) | private |
| StylizedBeamEffect | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L106) | private |

## Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs

// 职责：按场景复用短时光束和点光源；只限制闲置缓存，不裁剪当前战斗效果。 // 入口：RentBeam() / RentLight()；淡出结束Return，场景卸载连同活动及闲置子对象释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TransientEffectPool | [`BeamFade RentBeam()`](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L32) | public static |
| TransientEffectPool | [`LightFade RentLight(Vector3 position)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L52) | public static |
| TransientEffectPool | [`void Return(BeamFade fade)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L73) | internal |
| TransientEffectPool | [`void Return(LightFade fade)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L92) | internal |
| TransientEffectPool | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L111) | private |
| TransientEffectPool | [`void ClearActive()`](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L122) | public |

## Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs

// 职责：配置可复用能量线与灯光闪烁，统一径向碎光、颜色及可见网格包围盒规则。 // 入口：CreateLineStreak() / CreateRadialStreakBurst() / CreateLightFlash()；对象池由当前场景显式配置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| TransientEffects | [`void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L7) | public static |
| TransientEffects | [`void CreateRadialStreakBurst( Vector3 center, Vector3 normal, Color color, int count, float length, float duration, float width)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L19) | public static |
| TransientEffects | [`void CreateLightFlash(Vector3 position, Color color, float intensity, float range, float duration)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L48) | public static |
| TransientEffects | [`Bounds GetBounds(GameObject target, Vector3 fallbackCenter, Vector3 fallbackSize)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L58) | public static |
| TransientEffects | [`Vector3 GetBlockSize(Block block)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L84) | public static |
| TransientEffects | [`Color WithAlpha(Color color, float alpha)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L89) | public static |
| TransientEffects | [`Color Brighten(Color color, float intensity, float whiteBlend = 0f)`](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L95) | public static |

## Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs

// 职责：由集中配置库生成挂载特效，封装播放、停止、参数与尾烟处理。 // 入口：SetIntensity() / SetColor() / SetSpawnCount()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxEffect | [`void EnsureContent()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L47) | private |
| VfxEffect | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L97) | private |
| VfxEffect | [`void SetIntensity(float value)`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L106) | public |
| VfxEffect | [`void SetColor(Color color)`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L143) | public |
| VfxEffect | [`void SetSpawnCount(float count)`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L155) | public |
| VfxEffect | [`void Clear()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L167) | public |
| VfxEffect | [`void PlayOnce()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L182) | public |
| VfxEffect | [`void ConfigurePool(VfxEffectPool pool, VfxEffect prefab)`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L188) | internal |
| VfxEffect | [`void ResetForRent(Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L209) | internal |
| VfxEffect | [`void ReleaseAfterPlayback()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252) | public |
| VfxEffect | [`void StopAndRelease()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L264) | public |
| VfxEffect | [`void FinishPlayback()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L275) | private |
| VfxEffect | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L302) | private |
| VfxEffect | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L303) | private |

## Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs

// 职责：按场景和模板复用GPU特效；默认按需扩容，演出场可显式配置活动预算。 // 入口：Rent() / Return()；场景卸载销毁池及其全部实例。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VfxEffectPool | [`VfxEffect Rent(VfxEffect prefab, Vector3 position, Quaternion rotation, Scene scene)`](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L34) | public static |
| VfxEffectPool | [`void Return(VfxEffect effect, VfxEffect prefab)`](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L89) | internal |
| VfxEffectPool | [`void Forget(VfxEffect effect, VfxEffect prefab)`](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L117) | internal |
| VfxEffectPool | [`void ClearActive()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L125) | public |
| VfxEffectPool | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L142) | private |

## Assets/Scripts/Features/Presentation/Interface/GlobalTextStyler.cs

// 职责：统一自有界面的文字样式和辅助效果。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GlobalTextStyler | [`IEnumerator Start()`](../Assets/Scripts/Features/Presentation/Interface/GlobalTextStyler.cs#L14) | private |
| GlobalTextStyler | [`void ApplyToSceneTexts()`](../Assets/Scripts/Features/Presentation/Interface/GlobalTextStyler.cs#L23) | private static |

## Assets/Scripts/Features/Presentation/Interface/IconManager.cs

// 职责：管理方块/资源图标查询，供建造与存档界面使用。 // 入口：Register() / Unregister()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| IconManager | [`void Awake()`](../Assets/Scripts/Features/Presentation/Interface/IconManager.cs#L18) | private |
| IconManager | [`void Update()`](../Assets/Scripts/Features/Presentation/Interface/IconManager.cs#L23) | private |
| IconManager | [`void Register(CanvasGroup icon)`](../Assets/Scripts/Features/Presentation/Interface/IconManager.cs#L39) | public |
| IconManager | [`void Unregister(CanvasGroup icon)`](../Assets/Scripts/Features/Presentation/Interface/IconManager.cs#L47) | public |

## Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs

// 职责：绑定主界面按钮到建造、游玩和存档动作。 // 入口：SetDefault() / SetMove() / SetRotate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainUIButtons | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L45) | private |
| MainUIButtons | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L50) | private |
| MainUIButtons | [`void Awake()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L58) | private |
| MainUIButtons | [`void OnValidate()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L63) | private |
| MainUIButtons | [`void Start()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L77) | private |
| MainUIButtons | [`void SetDefault()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L109) | public |
| MainUIButtons | [`void SetMove()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L115) | public |
| MainUIButtons | [`void SetRotate()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L121) | public |
| MainUIButtons | [`void RefreshBuildModeButtons(BuildType buildType)`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L127) | public |
| MainUIButtons | [`void SetBuildButtonColor(Button button, bool selected)`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L136) | private |
| MainUIButtons | [`void SetCurrentBlock(string fileName)`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L162) | public |
| MainUIButtons | [`void ShowPowerRange()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L169) | private |
| MainUIButtons | [`void ShowPowerConnections()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L183) | private |
| MainUIButtons | [`void ShowConnectionStatus()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L197) | private |
| MainUIButtons | [`void ShowDurabilityStatus()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L208) | private |
| MainUIButtons | [`void ShowPowerStatus()`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L219) | private |
| MainUIButtons | [`void SetDebugButtonState(Button button, bool enabled)`](../Assets/Scripts/Features/Presentation/Interface/MainUIButtons.cs#L230) | private static |

## Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs

// 职责：管理主面板、弹窗和模式切换时的过渡与交互状态。 // 入口：ShowCreatePanel() / ShowRenamePanel() / HideCreatePanel()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| MainUIPanels | [`void Awake()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L47) | private |
| MainUIPanels | [`void Update()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L53) | private |
| MainUIPanels | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L76) | private |
| MainUIPanels | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L81) | private |
| MainUIPanels | [`void ShowCreatePanel()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L90) | public |
| MainUIPanels | [`void ShowRenamePanel(string save)`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L105) | public |
| MainUIPanels | [`void HideCreatePanel()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L120) | public |
| MainUIPanels | [`void SetInputPlaceholder(string text)`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L130) | private |
| MainUIPanels | [`void ShowDeletePanel(string save)`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L139) | public |
| MainUIPanels | [`void HideDeletePanel()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L155) | public |
| MainUIPanels | [`void OnConfirmCreate()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L167) | public |
| MainUIPanels | [`void OnConfirmDelete(string save)`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L207) | private |
| MainUIPanels | [`void PlayStart()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233) | public |
| MainUIPanels | [`void CompletePlayStart()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L267) | public |
| MainUIPanels | [`void PlayEnd()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L287) | public |
| MainUIPanels | [`void CompletePlayEnd()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304) | public |
| MainUIPanels | [`void RestoreBuildPanels()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L315) | public |
| MainUIPanels | [`void PlayerDeath()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L323) | public |
| MainUIPanels | [`void EnterEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L351) | public |
| MainUIPanels | [`void ExitEnemyBlueprintBuildMode()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L356) | public |
| MainUIPanels | [`void RefreshBlueprintEditorMode()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L361) | public |
| MainUIPanels | [`void RefreshBuildingEditorPanel()`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L380) | public |
| MainUIPanels | [`void UpdateHealthBar(GameObject obj, float currentHealth, float maxHealth)`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L385) | public |
| MainUIPanels | [`void SetHealthBar(float currentHealth, float maxHealth)`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L400) | private |
| MainUIPanels | [`void SetCockpitHealth(Cockpit cockpit)`](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L416) | private |

## Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs

// 职责：控制面板淡入淡出、交互状态和取消；不参与游戏模式及存档决策。 // 入口：Show() / CancelAll()；由 MainUIPanels 注入协程宿主，禁用时取消。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PanelTransitions | [`void CancelAll()`](../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L16) | public |
| PanelTransitions | [`void Show(GameObject panel, bool show, float duration)`](../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L29) | public |
| PanelTransitions | [`IEnumerator Fade(GameObject panel, bool show, float duration)`](../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L53) | private |
| PanelTransitions | [`void SetInteraction(GameObject panel, bool interactable)`](../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L88) | public static |

## Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs

// 职责：显示持续的返航保存失败原因与重试提示，不抢焦点或阻挡操作。 // 入口：ShowFailure() / Clear()；MainUIPanels在返航失败/成功时调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ReturnStatusPanel | [`void Awake()`](../Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs#L15) | private |
| ReturnStatusPanel | [`void ShowFailure(string reason)`](../Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs#L20) | public |
| ReturnStatusPanel | [`void Clear()`](../Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs#L37) | public |

## Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs

// 职责：显示方块状态图标与参数提示，管理提示对象的可见性。 // 入口：CheckConnectionStatus() / CheckDurabilityStatus() / CheckPowerStatus()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| StatusIcon | [`void Awake()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L39) | private |
| StatusIcon | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L47) | private |
| StatusIcon | [`void LateUpdate()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L56) | private |
| StatusIcon | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L74) | private |
| StatusIcon | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L80) | private |
| StatusIcon | [`void CheckConnectionStatus()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L88) | public |
| StatusIcon | [`void CheckDurabilityStatus()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L111) | public |
| StatusIcon | [`void CheckPowerStatus()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L138) | public |
| StatusIcon | [`void SetConnectionStatus(ConnectionStatus nextStatus)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L165) | private |
| StatusIcon | [`void RefreshStatusIcons()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L176) | public |
| StatusIcon | [`void RefreshAllStatusIcons()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L186) | public static |
| StatusIcon | [`void RefreshIcons(DebugManager manager)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L197) | private |
| StatusIcon | [`void EnsureIconVisuals(DebugManager manager)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L224) | private |
| StatusIcon | [`Image CreateIcon(string iconName)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L255) | private |
| StatusIcon | [`bool SetIcon(Image image, string statusName)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L270) | private |
| StatusIcon | [`Sprite FindStatusIcon(string statusName)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L278) | private |
| StatusIcon | [`void PositionVisibleIcons(int visibleCount)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L297) | private |
| StatusIcon | [`void PositionIcon(Image image, ref float x)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L307) | private static |
| StatusIcon | [`void UpdateIconScreenPosition(DebugManager manager)`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L318) | private |
| StatusIcon | [`void HideIcons()`](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L367) | private |

## Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs

// 职责：为按钮悬停、按下和焦点提供缩放反馈。 // 入口：OnPointerEnter() / OnPointerExit() / OnPointerDown()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| UIInteractionFeedback | [`void Awake()`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L20) | private |
| UIInteractionFeedback | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L25) | private |
| UIInteractionFeedback | [`void OnPointerEnter(PointerEventData eventData)`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L42) | public |
| UIInteractionFeedback | [`void OnPointerExit(PointerEventData eventData)`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L48) | public |
| UIInteractionFeedback | [`void OnPointerDown(PointerEventData eventData)`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L55) | public |
| UIInteractionFeedback | [`void OnPointerUp(PointerEventData eventData)`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L61) | public |
| UIInteractionFeedback | [`void OnSelect(BaseEventData eventData)`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L67) | public |
| UIInteractionFeedback | [`void OnDeselect(BaseEventData eventData)`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L73) | public |
| UIInteractionFeedback | [`void Animate()`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L79) | private |
| UIInteractionFeedback | [`IEnumerator AnimateScale()`](../Assets/Scripts/Features/Presentation/Interface/UIInteractionFeedback.cs#L89) | private |

## Assets/Scripts/Features/Presentation/VisualEffectsManager.cs

// 职责：为建造、维修、损伤、爆炸、敌机出生和能量反馈提供统一效果入口。 // 入口：EnsureInstance() / GetSharedLineMaterial() / TryPlayBlockPlaced() / TryPlayEnemySpawnPortal()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VisualEffectsManager | [`VisualEffectsManager EnsureInstance()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L42) | public static |
| VisualEffectsManager | [`Material GetSharedLineMaterial()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L59) | public static |
| VisualEffectsManager | [`void TryPlayBlockPlaced(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L64) | public static |
| VisualEffectsManager | [`void TryPlayBlockRemoved(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L74) | public static |
| VisualEffectsManager | [`void TryPlayBlockExplosion(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L84) | public static |
| VisualEffectsManager | [`void TryPlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L94) | public static |
| VisualEffectsManager | [`void TryAttachDetachedPartSmoke(Rigidbody body, Vector3 worldAnchor, float intensity)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L99) | public static |
| VisualEffectsManager | [`void TryPlayObjectDestroyed(GameObject target)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L115) | public static |
| VisualEffectsManager | [`void TryPlayBlockMoved(Block block, Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L125) | public static |
| VisualEffectsManager | [`void TryPlayBlockRotated(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L135) | public static |
| VisualEffectsManager | [`void TryShowBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L145) | public static |
| VisualEffectsManager | [`void TryClearBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L155) | public static |
| VisualEffectsManager | [`void TryUpdateGhostPreview(GameObject ghost, bool isBlocked)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L165) | public static |
| VisualEffectsManager | [`void TryClearGhostPreview(GameObject ghost)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L175) | public static |
| VisualEffectsManager | [`void TryDecorateMeteor(Meteor meteor)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L185) | public static |
| VisualEffectsManager | [`void TryPlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L195) | public static |
| VisualEffectsManager | [`void Awake()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L200) | private |
| VisualEffectsManager | [`void OnDisable()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L213) | private |
| VisualEffectsManager | [`void PlayCameraShake(float amplitude, float duration)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L219) | public |
| VisualEffectsManager | [`void TryPlayEnemySpawnPortal(ControlUnit unit)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L232) | public static |
| VisualEffectsManager | [`void PlayEnemySpawnPortal(ControlUnit unit)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L242) | private |
| VisualEffectsManager | [`void OnDestroy()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L266) | private |
| VisualEffectsManager | [`void OnEnable()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L275) | private |
| VisualEffectsManager | [`void ApplySceneLook()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L280) | private |
| VisualEffectsManager | [`void PlayBlockPlaced(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L295) | private |
| VisualEffectsManager | [`void PlayBlockRemoved(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L319) | private |
| VisualEffectsManager | [`void PlayBlockExplosion(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L348) | private |
| VisualEffectsManager | [`void PlayObjectDestroyed(GameObject target)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L382) | private |
| VisualEffectsManager | [`void PlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L414) | private |
| VisualEffectsManager | [`void PlayBlockMoved(Block block, Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L441) | private |
| VisualEffectsManager | [`void PlayBlockRotated(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L454) | private |
| VisualEffectsManager | [`void ShowBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L475) | private |
| VisualEffectsManager | [`void ClearBlockSelection(Block block)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L489) | private |
| VisualEffectsManager | [`void UpdateGhostPreview(Transform ghost, bool isBlocked)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L494) | private |
| VisualEffectsManager | [`void ClearGhostPreview()`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L499) | private |
| VisualEffectsManager | [`void DecorateMeteor(Meteor meteor)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L503) | private |
| VisualEffectsManager | [`void PlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L543) | private |

## Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs

// 职责：管理货仓容量、内容增减、恢复和快照，不负责无人机导航。 // 入口：CanReceive() / Reserve() / ReleaseReservation()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoHold | [`void Awake()`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L56) | private |
| CargoHold | [`void OnValidate()`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L62) | private |
| CargoHold | [`void OnEnable()`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L68) | private |
| CargoHold | [`void OnDisable()`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L73) | private |
| CargoHold | [`void Update()`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L80) | private |
| CargoHold | [`void RefreshPowerRequest()`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L85) | private |
| CargoHold | [`bool CanReceive(CargoItem item, LootDrop reservation = null)`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L93) | public |
| CargoHold | [`bool Reserve(LootDrop drop)`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L109) | public |
| CargoHold | [`void ReleaseReservation(LootDrop drop)`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L122) | public |
| CargoHold | [`int Store(CargoItem item, LootDrop reservation = null)`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L128) | public |
| CargoHold | [`List<CargoItem> CaptureContents()`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L156) | public |
| CargoHold | [`void RestoreContents(List<CargoItem> items)`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L170) | public |
| CargoHold | [`void ReleaseContents(UnitFaction faction)`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L206) | public |
| CargoHold | [`CargoHold FindReceiver(CargoItem item, Vector3 position, ControlUnit owner = null, LootDrop reservation = null)`](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L232) | public static |

## Assets/Scripts/Features/Salvage/Cargo/CargoItem.cs

// 职责：定义回收物品类型、存储条目及货仓分配规则。 // 入口：Copy()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoItem | [`CargoItem Copy(int count = -1)`](../Assets/Scripts/Features/Salvage/Cargo/CargoItem.cs#L20) | public |

## Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs

// 职责：在返回/死亡时持久化或清空玩家货仓，保持蓝图几何数据。 // 入口：SaveReturnCargo()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CargoPersistence | [`bool SaveReturnCargo(bool survived)`](../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L11) | public static |
| CargoPersistence | [`bool SaveReturnCargo(bool survived, out string failureReason)`](../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L16) | public static |
| CargoPersistence | [`Dictionary<string, List<CargoItem>> CapturePlayerContents(bool survived)`](../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L59) | public static |

## Assets/Scripts/Features/Salvage/Drops/LootDrop.cs

// 职责：维护掉落物品身份、携带/认领、吸附入库，以及散落悬浮和文字遮挡表现。 // 入口：Spawn() / Initialize() / Claim()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| LootDrop | [`void OnEnable()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L65) | private |
| LootDrop | [`void OnDisable()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L70) | private |
| LootDrop | [`void Start()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L76) | private |
| LootDrop | [`LootDrop Spawn(CargoItem item, Vector3 position)`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L84) | public static |
| LootDrop | [`LootDrop Spawn(CargoItem item, Vector3 position, Scene scene)`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L89) | public static |
| LootDrop | [`void Initialize(CargoItem item)`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L139) | public |
| LootDrop | [`void RefreshLabel()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L184) | private |
| LootDrop | [`bool Claim(CollectionBot bot, CargoHold destination)`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L195) | public |
| LootDrop | [`void Carry(Transform socket)`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L207) | public |
| LootDrop | [`void Release()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L221) | public |
| LootDrop | [`bool Deliver(CargoHold hold)`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L238) | public |
| LootDrop | [`void Update()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L268) | private |
| LootDrop | [`void LateUpdate()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L327) | private |
| LootDrop | [`void ResetDisplayPose()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L371) | private |
| LootDrop | [`void ClearSession()`](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L386) | public static |

## Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs

// 职责：处理残骸所有权和可回收内容，生成对应掉落。 // 入口：Convert() / ConvertGroup()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WreckSalvage | [`void Convert(Block block)`](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10) | public static |
| WreckSalvage | [`void Convert(Block block, bool allowPausedSimulation)`](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L15) | private static |
| WreckSalvage | [`void ReleaseContents(GameObject module, UnitFaction faction)`](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L59) | public static |
| WreckSalvage | [`void ConvertGroup(ControlUnit unit, bool allowPausedSimulation = false)`](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L69) | public static |

## Assets/Scripts/Features/Salvage/Economy/CoinManager.cs

// 职责：保存和展示玩家货币状态，提供增减入口。 // 入口：Start() / Update()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| CoinManager | [`void Start()`](../Assets/Scripts/Features/Salvage/Economy/CoinManager.cs#L8) | private |
| CoinManager | [`void Update()`](../Assets/Scripts/Features/Salvage/Economy/CoinManager.cs#L13) | private |

## Assets/Scripts/Features/Salvage/SalvageSettings.cs

// 职责：提供回收物品、无人机、残骸和掉落的可编辑配置。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Siege/Battle/EnemyWreckRetirement.cs

// 职责：共用敌方残骸筛选与幂等回收退场规则，保护建筑、友军、混合归属和存活驾驶舱。 // 入口：Eligible()/Retire()；预算与波次扫描共用，调用者负责保留期和分批节流。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyWreckRetirement | [`bool Eligible(ControlUnit unit, Scene scene, List<Block> blocks)`](../Assets/Scripts/Features/Siege/Battle/EnemyWreckRetirement.cs#L9) | public static |
| EnemyWreckRetirement | [`bool Retire(ControlUnit unit, Scene scene, List<Block> blocks, bool allowPausedSimulation)`](../Assets/Scripts/Features/Siege/Battle/EnemyWreckRetirement.cs#L35) | public static |

## Assets/Scripts/Features/Siege/Battle/SiegeBase.cs

// 职责：以大型建筑蓝图创建阵地战基地，独立绑定核心目标，并按建筑原蓝图报价/修缮。 // 入口：Create() / Quote() / Repair() / Dispose()；不复用舰船驾驶舱或舰船物理重组。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBase | [`SiegeBase Create(SiegeSession session, WorldBuildingManager buildings, Transform anchor)`](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L48) | public static |
| SiegeBase | [`SiegeBase Create( SiegeMatch match, SiegeSettings settings, WorldBuildingManager buildings, BlockDataList source, string coreId, string name, Vector3 position, Quaternion rotation, int replacementValue, Action coreLost)`](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L75) | public static |
| SiegeBase | [`long Quote()`](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L123) | public |
| SiegeBase | [`bool Repair(out string reason)`](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151) | public |
| SiegeBase | [`void OnBlockDestroyed(Block block)`](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L176) | private |
| SiegeBase | [`void Dispose()`](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L184) | public |

## Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs

// 职责：在返航/整备期展示场地扫描并分批回收敌方残骸，保留最短物理展示期与既有掉落规则。 // 入口：Begin()/Tick()/Stop()；由宿主显式调度，无额外金币、自动修复或场景外清理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCleanupSweep | [`bool OwnsRetirement(ControlUnit unit)`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L10) | public static |
| SiegeCleanupSweep | [`void Begin(int completedWave, Bounds area, float time)`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L62) | public |
| SiegeCleanupSweep | [`void Tick(float time)`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L85) | public |
| SiegeCleanupSweep | [`void Scan(float time, float progress)`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L133) | private |
| SiegeCleanupSweep | [`void Stop()`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L181) | public |
| SiegeCleanupSweep | [`void Show(bool visible)`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L190) | private |
| SiegeCleanupSweep | [`void Awake()`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L208) | private |
| SiegeCleanupSweep | [`void ResetRegistry()`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L213) | private static |
| SiegeCleanupSweep | [`void OnDisable()`](../Assets/Scripts/Features/Siege/Battle/SiegeCleanupSweep.cs#L219) | private |

## Assets/Scripts/Features/Siege/Battle/SiegeStrandedCockpit.cs

// 职责：识别受损后仅余无推进、无武器驾驶舱且持续停滞的来袭者，防止占住波次名额。 // 入口：ShouldRetire()；只提供退场判断，不产生击杀、奖励、伤害或凭空推力。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeStrandedCockpit | [`bool ShouldRetire(Cockpit cockpit, float time, float timeout)`](../Assets/Scripts/Features/Siege/Battle/SiegeStrandedCockpit.cs#L19) | public |

## Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs

// 职责：在独立在场预算内按固定随机种子调度敌舰，并将真实死亡或移除结算给规则核心。 // 入口：Tick() / Stop()；死亡事件只记事实，由下一次Tick统一结算，避免事件中重入生成。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWaveDirector | [`void BeginWave(float time)`](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L21) | public |
| SiegeWaveDirector | [`void Tick(float time)`](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L58) | public |
| SiegeWaveDirector | [`SiegeShipOption PickBlueprint(SiegeSettings settings)`](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L142) | private |
| SiegeWaveDirector | [`void Stop()`](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L162) | public |
| Attacker | [`void OnDefeated()`](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L208) | private |
| Attacker | [`void RefreshTarget(ControlUnit core)`](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L217) | public |
| Attacker | [`void Unsubscribe()`](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L230) | public |

## Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs

// 职责：限制阵地战场景中敌方无驾驶舱碎片的存量和寿命，分批沿既有回收路径退场。 // 入口：Tick()；只处理同场景且所有模块仍属敌方的碎片，不影响友舰维修、建筑或存活驾驶舱。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWreckBudget | [`void Tick(float time, int maximumGroups, int maximumBlocks, float lifetime, int retirePerScan)`](../Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs#L34) | public |
| SiegeWreckBudget | [`bool Eligible(ControlUnit unit)`](../Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs#L113) | private |

## Assets/Scripts/Features/Siege/Fleet/SiegeBlueprintCatalog.cs

// 职责：只读加载阵地战双方的用户蓝图快照，过滤不能部署的存档，不修改源文件。 // 入口：Load()；玩家来自Saves，攻方来自EnemyBlueprints，压力场可保留固定夹具。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBlueprintCatalog | [`SiegeShipOption[] Load(string directory, int coinsPerBlock, out string message)`](../Assets/Scripts/Features/Siege/Fleet/SiegeBlueprintCatalog.cs#L10) | public static |

## Assets/Scripts/Features/Siege/Fleet/SiegeDeploymentPad.cs

// 职责：把场景泊位关联到部署位置；不拥有舰船、经济或输入状态。 // 入口：Index；由可重复执行的场景迁移工具配置。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs

// 职责：部署固定泊位舰队、保持唯一玩家驾驶权，并在波次边界切换舰队物理状态。 // 入口：CreateBase() / DeployAt() / ReplaceAt() / CancelDeployment() / SetCombatActive()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFleet | [`Transform GetDeploymentPoint(int index)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L41) | public |
| SiegeFleet | [`void RefreshHangars(HangarModule[] hangars)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L47) | public |
| SiegeFleet | [`HangarModule GetHangar(int slot)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L66) | public |
| SiegeFleet | [`bool FitsHangar(int slot, BlockDataList blueprint)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L90) | public |
| SiegeFleet | [`Pose GetDeploymentPose(int slot, BlockDataList blueprint)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L101) | public |
| SiegeFleet | [`void Scrap(int slot)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L113) | public |
| SiegeFleet | [`SiegeFleetEntry Find(string id)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L140) | public |
| SiegeFleet | [`bool CreateBase(Transform anchor)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L145) | public |
| SiegeFleet | [`bool Deploy(int optionIndex, out string reason)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L159) | public |
| SiegeFleet | [`bool DeployAt(int optionIndex, int requestedDock, out string reason)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L164) | public |
| SiegeFleet | [`long CancellationCredit(int slot)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L257) | public |
| SiegeFleet | [`bool CancelDeployment(int slot, out string reason)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L263) | public |
| SiegeFleet | [`bool ReplaceAt(int optionIndex, int slot, out string reason)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L283) | public |
| SiegeFleet | [`bool TakeControl(int slot)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L342) | public |
| SiegeFleet | [`void SetCombatActive(bool active)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L375) | public |
| SiegeFleet | [`void ReleaseControl()`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L399) | public |
| SiegeFleet | [`void RepairBetweenWaves( out int repairedCount, out int attemptedCount, out long spent, out string firstFailure, bool forceAutomatic = false)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L419) | public |
| SiegeFleet | [`void RefreshBindings()`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L464) | public |
| SiegeFleet | [`void NotifyLost(Cockpit cockpit)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L490) | public |
| SiegeFleet | [`SiegeFleetEntry Spawn( BlockDataList blueprint, string id, string displayName, int price, Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L516) | private |
| SiegeFleet | [`BlockDataList PrepareBlueprint(TextAsset asset, out string id)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L552) | public static |
| SiegeFleet | [`BlockDataList PrepareBlueprint(string json, out string id)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L557) | public static |
| SiegeFleet | [`void RestoreView()`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L571) | public |
| SiegeFleet | [`void SetCamera(ControlUnit unit)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L583) | private |
| SiegeFleet | [`void SetBaseCamera()`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L593) | private |

## Assets/Scripts/Features/Siege/Fleet/SiegeFleetEntry.cs

// 职责：持有一艘部署舰的蓝图身份与驾驶舱引用；父物理组变化不改变舰艇身份。 // 入口：Unit / IsAlive；仅由SiegeFleet建立，维修按同一部署ID寻找原模块。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFleetEntry | [`void MarkLost()`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleetEntry.cs#L69) | public |

## Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs

// 职责：在准备和战斗边界更新友舰刚体、悬浮和AI；不拥有舰船身份或镜头。 // 入口：Park() / Activate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFleetPhysics | [`void Park(ControlUnit unit)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs#L7) | public static |
| SiegeFleetPhysics | [`void Activate(ControlUnit unit)`](../Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs#L25) | public static |

## Assets/Scripts/Features/Siege/Fleet/SiegeFlightSettings.cs

// 职责：保存机库出入航线、速度与失能报废阈值，供预研和正式玩法共享。 // 入口：SiegeReturnFlight 读取；宿主通过 Inspector 编辑，不持有运行状态。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs

// 职责：验证泊位净空并报价/执行修缮，复用同源模块、补回缺件，成功后精确扣费。 // 入口：IsClear() / Quote() / Repair()；部署蓝图不含货物，不读取或改写WorldSession。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeRefit | [`bool IsClear( BlockDataList blueprint, Vector3 position, Quaternion rotation, string ignoredUnitId, out string reason)`](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L9) | public static |
| SiegeRefit | [`bool IsBuildingClear( BlockDataList blueprint, Vector3 position, Quaternion rotation, string ignoredUnitId, out string reason)`](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L26) | public static |
| SiegeRefit | [`bool IsGeometryClear( BlockDataList blueprint, Vector3 position, Quaternion rotation, string ignoredUnitId, out string reason)`](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L37) | private static |
| SiegeRefit | [`string DescribeObstruction(Collider hit)`](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L85) | private static |
| SiegeRefit | [`long Quote(SiegeFleetEntry entry, SiegeSettings settings)`](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L103) | public static |
| SiegeRefit | [`bool Repair(SiegeFleetEntry entry, ISiegeDeploymentHost session, out string reason)`](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L135) | public static |
| SiegeRefit | [`Dictionary<string, Block> CollectOwned(SiegeFleetEntry entry)`](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L215) | private static |

## Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs

// 职责：共享机库起飞/返航导引，以有电推进能力限定刚体运动，逐舰入库或报废。 // 入口：BeginLaunch() / BeginReturn()；宿主在 FixedUpdate 调用 Tick()，退出调用 Stop()。 // 约束：自动导引暂时接管姿态与推力；不传送舰艇、不在报废前发放返航奖励。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeReturnFlight | [`bool IsGuiding(SiegeFleetEntry entry)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L63) | public |
| SiegeReturnFlight | [`void BeginLaunch()`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L68) | public |
| SiegeReturnFlight | [`void BeginReturn()`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L73) | public |
| SiegeReturnFlight | [`void Begin(bool returning)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L78) | private |
| SiegeReturnFlight | [`void BindUnit(Flight flight)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L119) | private |
| SiegeReturnFlight | [`void TakeGuidanceControl(Flight flight)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L146) | private |
| SiegeReturnFlight | [`Vector3 DockPosition(Flight flight)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L179) | private |
| SiegeReturnFlight | [`void BuildRoute(Flight flight, bool returning)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L204) | private |
| SiegeReturnFlight | [`void BuildRoofRoute(Flight flight, bool returning, Vector3 dock, Bounds hull)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L295) | private |
| SiegeReturnFlight | [`void Tick(float deltaTime)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L353) | public |
| SiegeReturnFlight | [`void TickFlight(Flight flight, float dt)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L369) | private |
| SiegeReturnFlight | [`void ReadPropulsion(Flight flight, out float driveForce, out float liftForce)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L432) | private static |
| SiegeReturnFlight | [`void ApplyGuidance(Flight flight, Vector3 target, float driveAcceleration, float liftAcceleration, float dt)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L455) | private |
| SiegeReturnFlight | [`void PublishPropulsion(Flight flight, Vector3 horizontal, float vertical, float driveAcceleration, float liftAcceleration)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L493) | private |
| SiegeReturnFlight | [`void Finish(Flight flight, bool success, string reason = null)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L511) | private |
| SiegeReturnFlight | [`void ReleasePropulsion(Flight flight)`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L544) | private |
| SiegeReturnFlight | [`void Stop()`](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L573) | public |

## Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs

// 职责：把新版基地战绑定到共享HUD，管理友舰关注、核心/舰船摘要与抽屉状态。 // 入口：Enter() / Exit() / AttackerResolved()；场景卸载前恢复调试开关，不接管经济或驾驶权。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeCombatHud | [`void Awake()`](../Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs#L46) | private |
| SiegeCombatHud | [`void Enter(SiegeGameplayStage owner)`](../Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs#L63) | public |
| SiegeCombatHud | [`void Exit()`](../Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs#L74) | public |
| SiegeCombatHud | [`void OnDisable()`](../Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs#L81) | private |
| SiegeCombatHud | [`void AttackerResolved(string victim, string attacker, int reward, bool playerCredit)`](../Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs#L86) | public |
| SiegeCombatHud | [`void Update()`](../Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs#L91) | private |
| SiegeCombatHud | [`void PollShipControl()`](../Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs#L170) | private |
| SiegeCombatHud | [`string CoreText(Durability health)`](../Assets/Scripts/Features/Siege/Gameplay/Interface/SiegeCombatHud.cs#L228) | private static |

## Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs

// 职责：在Main暴露建筑编辑、示例复制和唯一核心指定；战斗永不回写这些设计文件。 // 入口：ToggleEditor() / CopyStarter() / DesignateCore()；编辑尺寸和方块继续使用原建筑工具。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBaseWorkshop | [`void Start()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L24) | private |
| SiegeBaseWorkshop | [`void ToggleEditor()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L34) | public |
| SiegeBaseWorkshop | [`void SelectHangar()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L43) | public |
| SiegeBaseWorkshop | [`void CopyStarter()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L52) | public |
| SiegeBaseWorkshop | [`void DesignateCore()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L68) | public |
| SiegeBaseWorkshop | [`void TrySave(Action action)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L97) | private |
| SiegeBaseWorkshop | [`void Update()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L109) | private |

## Assets/Scripts/Features/Siege/Gameplay/SiegeConstructionSettings.cs

// 职责：保存阵地战基地参考造价及共享战斗配置；出战不限制尺寸、预算和高度，供Inspector独立调参。 // 入口：Cost() / Validate()；不读写玩家蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeConstructionSettings | [`long Cost(BlockDataList data)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeConstructionSettings.cs#L30) | public |
| SiegeConstructionSettings | [`bool Validate(BlockDataList data, out string reason)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeConstructionSettings.cs#L47) | public |
| SiegeConstructionSettings | [`string Warnings(BlockDataList data)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeConstructionSettings.cs#L81) | public |

## Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs

// 职责：新版基地战的自动部署、真实守波和整备；共享波次/修缮机制，不依赖旧Session/HUD。 // 入口：Enter() / BeginWave() / Repair() / Retreat() / Exit()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeGameplayStage | [`void Enter( GameModeController host, GameObject[] buildEnvironment, string blueprintName = null, TextAsset example = null, string exampleName = null)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L96) | public |
| SiegeGameplayStage | [`void ValidatePose()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L184) | private |
| SiegeGameplayStage | [`bool Deploy()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L202) | private |
| SiegeGameplayStage | [`bool BeginWave()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L271) | public |
| SiegeGameplayStage | [`bool Repair()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L290) | public |
| SiegeGameplayStage | [`void Update()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L303) | private |
| SiegeGameplayStage | [`void SelectDefender(int index)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L369) | public |
| SiegeGameplayStage | [`void SelectAttacker(int index)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L377) | public |
| SiegeGameplayStage | [`bool RefreshBlueprints()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L385) | public |
| SiegeGameplayStage | [`void RefreshDefenders()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L396) | private |
| SiegeGameplayStage | [`bool TryDeployAt(int optionIndex, int dock, out string reason)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L407) | public |
| SiegeGameplayStage | [`bool TryReplaceAt(int optionIndex, int dock, out string reason)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L416) | public |
| SiegeGameplayStage | [`bool TryCancelDeployment(int dock, out string reason)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L425) | public |
| SiegeGameplayStage | [`bool TryTakeControl(int slot)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L434) | public |
| SiegeGameplayStage | [`bool TryTakeControl(string id)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L450) | public |
| SiegeGameplayStage | [`void Observe()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L456) | public |
| SiegeGameplayStage | [`bool TryRepairShip(string id, bool repairBase = false)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L470) | public |
| SiegeGameplayStage | [`void FixedUpdate()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L484) | private |
| SiegeGameplayStage | [`void SetSimulation(bool active)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L523) | private |
| SiegeGameplayStage | [`void LoseBase()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L566) | private |
| SiegeGameplayStage | [`void Retreat()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L571) | public |
| SiegeGameplayStage | [`void Settle(string reason)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L576) | private |
| SiegeGameplayStage | [`void ReturnToMenu()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L605) | public |
| SiegeGameplayStage | [`void ShowCoreMarker(Transform parent, Vector3 localPosition)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L625) | private |
| SiegeGameplayStage | [`void LateUpdate()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L634) | private |
| SiegeGameplayStage | [`void Exit()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L642) | public |
| SiegeGameplayStage | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L684) | private |

## Assets/Scripts/Features/Siege/Gameplay/SiegeIsland.cs

// 职责：将基地占地转换为固定有限浮空岛，提供地面、敌人入口和坠落/远离边界。 // 入口：GenerateFor() / SpawnPosition() / BeyondRecovery()；复用ProceduralVoxelWorld的破坏和网格。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeIsland | [`void GenerateFor(BuildingFoundationData foundation, Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeIsland.cs#L19) | public |
| SiegeIsland | [`bool RecoverFleet(SiegeFleet fleet, SiegeBase fortress)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeIsland.cs#L33) | public |
| SiegeIsland | [`void GenerateFor(Bounds foundation)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeIsland.cs#L55) | public |
| SiegeIsland | [`Vector3 SpawnPosition(float angle, float desiredRadius)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeIsland.cs#L86) | public |
| SiegeIsland | [`bool BeyondRecovery(Vector3 position)`](../Assets/Scripts/Features/Siege/Gameplay/SiegeIsland.cs#L96) | public |

## Assets/Scripts/Features/Siege/Gameplay/SiegeIslandSettings.cs

// 职责：保存阵地战浮空岛可编辑生成规格与坠落边界；尺寸按基地占地只在部署时确定。 // 入口：Inspector / SiegeIsland.GenerateFor()；单位为游戏米。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Siege/Gameplay/SiegePhaseClock.cs

// 职责：维护战斗总时限、返航总时限和逐舰位移停滞窗口，不结算经济或操作界面。 // 入口：BeginBattle() / Tick()；使用宿主传入的模拟时间增量，暂停不计时。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePhaseClock | [`void BeginBattle(float preparation)`](../Assets/Scripts/Features/Siege/Gameplay/SiegePhaseClock.cs#L41) | public |
| SiegePhaseClock | [`string Tick(SiegePhase phase, float delta, SiegeFleet fleet, SiegeReturnFlight guidance)`](../Assets/Scripts/Features/Siege/Gameplay/SiegePhaseClock.cs#L50) | public |

## Assets/Scripts/Features/Siege/Hangars/ChangeHangarEntranceAction.cs

// 职责：撤销和重做机库开口方向，同时刷新几何与编辑中的蓝图。 // 入口：Undo() / Redo()；由机库编辑命令创建。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ChangeHangarEntranceAction | [`void Undo()`](../Assets/Scripts/Features/Siege/Hangars/ChangeHangarEntranceAction.cs#L17) | public |
| ChangeHangarEntranceAction | [`void Redo()`](../Assets/Scripts/Features/Siege/Hangars/ChangeHangarEntranceAction.cs#L22) | public |
| ChangeHangarEntranceAction | [`void Apply(HangarEntrance value)`](../Assets/Scripts/Features/Siege/Hangars/ChangeHangarEntranceAction.cs#L27) | private |

## Assets/Scripts/Features/Siege/Hangars/HangarEntrance.cs

// 职责：保存机库开口方向；零值维持旧蓝图的前门行为。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Siege/Hangars/HangarEntranceButton.cs

// 职责：把已制作的机库方向按钮接入蓝图编辑命令，显示当前前门或顶门。 // 入口：OnEnable() / OnDisable() 对称绑定；Toggle() 供按钮调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarEntranceButton | [`void OnEnable()`](../Assets/Scripts/Features/Siege/Hangars/HangarEntranceButton.cs#L14) | private |
| HangarEntranceButton | [`void OnDisable()`](../Assets/Scripts/Features/Siege/Hangars/HangarEntranceButton.cs#L20) | private |
| HangarEntranceButton | [`void LateUpdate()`](../Assets/Scripts/Features/Siege/Hangars/HangarEntranceButton.cs#L28) | private |
| HangarEntranceButton | [`void Toggle()`](../Assets/Scripts/Features/Siege/Hangars/HangarEntranceButton.cs#L36) | public |

## Assets/Scripts/Features/Siege/Hangars/HangarFacePanel.cs

// 职责：只显示面向镜头的已选机库尺寸控件；保持背面按钮不拦截点击。 // 入口：ConfigureGeometry() / LateUpdate()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarFacePanel | [`void ConfigureGeometry(Vector3 outer)`](../Assets/Scripts/Features/Siege/Hangars/HangarFacePanel.cs#L13) | public |
| HangarFacePanel | [`void LateUpdate()`](../Assets/Scripts/Features/Siege/Hangars/HangarFacePanel.cs#L20) | private |

## Assets/Scripts/Features/Siege/Hangars/HangarLimits.cs

// 职责：保存基地机库数量上限；尺寸使用基地自身边界，不与数量限制混用。 // 入口：MaximumCount；Resources/Hangars/Limits 是共享编辑与部署配置。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Siege/Hangars/HangarModule.cs

// 职责：按方块存档尺寸维护机库外壳、净空、泊位和安全滑门，不保存战斗损伤到设计。 // 入口：RefreshGeometry() / TryResize() / Fits() / GetDockPose() / SetCombat() / SetDoorOpen()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarModule | [`void OnEnable()`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L59) | private |
| HangarModule | [`void RefreshGeometry()`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L70) | public |
| HangarModule | [`void RefreshFloorConnections()`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L121) | private |
| HangarModule | [`bool IsCapacityRenderer(Renderer renderer)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L175) | public |
| HangarModule | [`bool Fits(BlockDataList blueprint)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L180) | public |
| HangarModule | [`Pose GetDockPose(BlockDataList blueprint)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L192) | public |
| HangarModule | [`void SetCombat(bool active)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L200) | public |
| HangarModule | [`void ApplyPresentation()`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L217) | private |
| HangarModule | [`void SetDoorOpen(bool open)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L253) | public |
| HangarModule | [`void FixedUpdate()`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L258) | private |
| HangarModule | [`void LateUpdate()`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L271) | private |
| HangarModule | [`bool IsDoorwayClear()`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L279) | private |
| HangarModule | [`void ApplyDoorPose()`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L308) | private |
| HangarModule | [`void GetShellBoxes(Vector3 outer, List<Bounds> boxes)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L332) | public static |
| HangarModule | [`bool TrySetEntrance(HangarEntrance value, out string reason)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L344) | public |
| HangarModule | [`void RestoreEntrance(HangarEntrance value)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L366) | public |
| HangarModule | [`bool TryResize(int axis, int delta, out string reason)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L377) | public |
| HangarModule | [`void ApplySize(Vector3Int dimensions, Vector3 position)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L425) | internal |
| HangarModule | [`void RefreshShellPreview(Vector3Int size)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L436) | private |
| HangarModule | [`void SetShape(Transform piece, Vector3 position, Vector3 size)`](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L480) | private static |

## Assets/Scripts/Features/Siege/Hangars/HangarResizeButton.cs

// 职责：将已制作的世界空间按钮转成机库尺寸命令，不动态创建 UI。 // 入口：OnEnable() 绑定按钮；OnDisable() 对称解除。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarResizeButton | [`void OnEnable()`](../Assets/Scripts/Features/Siege/Hangars/HangarResizeButton.cs#L16) | private |
| HangarResizeButton | [`void OnDisable()`](../Assets/Scripts/Features/Siege/Hangars/HangarResizeButton.cs#L22) | private |
| HangarResizeButton | [`void Resize()`](../Assets/Scripts/Features/Siege/Hangars/HangarResizeButton.cs#L30) | public |

## Assets/Scripts/Features/Siege/Hangars/HangarRules.cs

// 职责：共享基地编辑和加载时的机库数量、直立姿态规则。 // 入口：MaximumCount / Validate()；不修改蓝图或玩家存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarRules | [`long ShellCost(BlockData block)`](../Assets/Scripts/Features/Siege/Hangars/HangarRules.cs#L21) | public static |
| HangarRules | [`bool IsHangar(BlockData block)`](../Assets/Scripts/Features/Siege/Hangars/HangarRules.cs#L33) | public static |
| HangarRules | [`bool Validate(BlockDataList data, out string reason)`](../Assets/Scripts/Features/Siege/Hangars/HangarRules.cs#L39) | public static |

## Assets/Scripts/Features/Siege/Hangars/HangarShellBlueprint.cs

// 职责：将逻辑机库展开成可独立损毁的 2×2×1 建筑模块；版本标记防止读档补回已毁墙体。 // 入口：Expand() / Tiles() / EvenSize()；不写入玩家蓝图文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| HangarShellBlueprint | [`int EvenSize(int value)`](../Assets/Scripts/Features/Siege/Hangars/HangarShellBlueprint.cs#L11) | public static |
| HangarShellBlueprint | [`List<Pose> Tiles(Vector3Int size, HangarEntrance entrance = HangarEntrance.Front)`](../Assets/Scripts/Features/Siege/Hangars/HangarShellBlueprint.cs#L16) | public static |
| HangarShellBlueprint | [`int TileCount(Vector3Int size, HangarEntrance entrance = HangarEntrance.Front)`](../Assets/Scripts/Features/Siege/Hangars/HangarShellBlueprint.cs#L66) | public static |
| HangarShellBlueprint | [`BlockData Normalize(BlockData source)`](../Assets/Scripts/Features/Siege/Hangars/HangarShellBlueprint.cs#L76) | public static |
| HangarShellBlueprint | [`BlockDataList Expand(BlockDataList source)`](../Assets/Scripts/Features/Siege/Hangars/HangarShellBlueprint.cs#L103) | public static |
| HangarShellBlueprint | [`WorldBuildingState ExpandState(WorldBuildingState source)`](../Assets/Scripts/Features/Siege/Hangars/HangarShellBlueprint.cs#L173) | public static |

## Assets/Scripts/Features/Siege/Hangars/ResizeHangarAction.cs

// 职责：以原尺寸和底部锚定姿态撤销/重做机库扩缩，并同步蓝图。 // 入口：Undo() / Redo()；由 HangarModule.TryResize() 提交。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ResizeHangarAction | [`void Undo()`](../Assets/Scripts/Features/Siege/Hangars/ResizeHangarAction.cs#L28) | public |
| ResizeHangarAction | [`void Redo()`](../Assets/Scripts/Features/Siege/Hangars/ResizeHangarAction.cs#L36) | public |

## Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs

// 职责：定义预研与新版基地战共享的舰队部署界面契约，避免复制部署和修缮规则。 // 入口：SiegeFleet / SiegeDeploymentView；会话持有经济、目录和生命周期。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ISiegeDeploymentHost | [`void SelectDefender(int index)`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L58) |  |
| ISiegeDeploymentHost | [`void SelectAttacker(int index)`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L59) |  |
| ISiegeDeploymentHost | [`bool RefreshBlueprints()`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L60) |  |
| ISiegeDeploymentHost | [`bool TryDeployAt(int optionIndex, int dock, out string reason)`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L61) |  |
| ISiegeDeploymentHost | [`bool TryReplaceAt(int optionIndex, int dock, out string reason)`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L62) |  |
| ISiegeDeploymentHost | [`bool TryCancelDeployment(int dock, out string reason)`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L63) |  |
| ISiegeDeploymentHost | [`bool TryTakeControl(int slot)`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L64) |  |
| ISiegeDeploymentHost | [`bool TryTakeControl(string id)`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L65) |  |
| ISiegeDeploymentHost | [`bool TryRepairShip(string id, bool repairBase = false)`](../Assets/Scripts/Features/Siege/Interface/ISiegeDeploymentHost.cs#L66) |  |

## Assets/Scripts/Features/Siege/Interface/SiegeBattleFeedback.cs

// 职责：显示短时波次和核心受击反馈；击杀统一由共享战斗HUD显示。 // 入口：Update()；关闭时不改变战场状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeBattleFeedback | [`void Update()`](../Assets/Scripts/Features/Siege/Interface/SiegeBattleFeedback.cs#L18) | private |

## Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs

// 职责：点击场景泊位预览并购买飞船，点击友舰打开驾驶/修缮卡片；预览没有物理和战斗组件。 // 入口：SelectDock() / SelectShip() / Confirm() / Cancel()；确认时重新验证净空和余额。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeDeploymentView | [`void OnEnable()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L57) | private |
| SiegeDeploymentView | [`void OnDisable()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L82) | private |
| SiegeDeploymentView | [`void Update()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L106) | private |
| SiegeDeploymentView | [`void SelectDock(int index)`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L198) | public |
| SiegeDeploymentView | [`void SelectShip(string id)`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L218) | public |
| SiegeDeploymentView | [`void SelectOption(int index)`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L231) | private |
| SiegeDeploymentView | [`void SelectAttacker(int index)`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L241) | private |
| SiegeDeploymentView | [`void RefreshOptions()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L246) | public |
| SiegeDeploymentView | [`void RefreshBlueprints()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L290) | private |
| SiegeDeploymentView | [`void RefreshSelection()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L297) | public |
| SiegeDeploymentView | [`void Confirm()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L360) | public |
| SiegeDeploymentView | [`void BeginReplacement()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L373) | public |
| SiegeDeploymentView | [`void Undeploy()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L386) | public |
| SiegeDeploymentView | [`void Pilot()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L395) | private |
| SiegeDeploymentView | [`void Repair()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L401) | private |
| SiegeDeploymentView | [`void CycleShip()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L406) | public |
| SiegeDeploymentView | [`void Cancel()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L422) | public |
| SiegeDeploymentView | [`void ClearPreview()`](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L430) | private |

## Assets/Scripts/Features/Siege/Interface/SiegeFleetView.cs

// 职责：将唯一驾驶权绑定到共用镜头和玩家状态；不创建舰船或改变经济。 // 入口：Focus() / FocusBase()；物理重组后可重新绑定同一舰船。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeFleetView | [`void Focus(ControlUnit unit, float maximum = 0f)`](../Assets/Scripts/Features/Siege/Interface/SiegeFleetView.cs#L7) | public static |
| SiegeFleetView | [`void FocusBase(SiegeBase station)`](../Assets/Scripts/Features/Siege/Interface/SiegeFleetView.cs#L26) | public static |

## Assets/Scripts/Features/Siege/Interface/SiegeHud.cs

// 职责：按阶段显示精简的指挥栏、整备操作与结算；布局由场景制作工具保存。 // 入口：Refresh()；部署交互交给SiegeDeploymentView，战斗反馈交给SiegeBattleFeedback。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeHud | [`void OnEnable()`](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L33) | private |
| SiegeHud | [`void OnDisable()`](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L41) | private |
| SiegeHud | [`void Update()`](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L49) | private |
| SiegeHud | [`void Refresh()`](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L63) | public |
| SiegeHud | [`void RefreshQuote()`](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L105) | private |
| SiegeHud | [`void StartWave()`](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L110) | private |
| SiegeHud | [`void RepairCore()`](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L115) | private |
| SiegeHud | [`void Restart()`](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L120) | private |

## Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs

// 职责：基地战独占共享相机的自由观察输入，保留UI指针并在菜单/失焦/退出时释放捕获。 // 入口：Enter() / Exit() / ResetOverview()；场景预挂，右键拖看、WASD/QE飞行、Shift加速、滚轮调速、F总览。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeObserverCamera | [`void Enter(CameraController camera, Transform overviewPose)`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L42) | public |
| SiegeObserverCamera | [`void ResetOverview()`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L65) | public |
| SiegeObserverCamera | [`void SyncAngles()`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L78) | private |
| SiegeObserverCamera | [`void Update()`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L84) | private |
| SiegeObserverCamera | [`bool IsTyping()`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L152) | private static |
| SiegeObserverCamera | [`bool PointerOverInterface(Vector2 position)`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L158) | private |
| SiegeObserverCamera | [`void RefreshHint()`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L189) | private |
| SiegeObserverCamera | [`void ReleasePointer()`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L197) | private |
| SiegeObserverCamera | [`void Exit()`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L209) | public |
| SiegeObserverCamera | [`void OnDisable()`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L224) | private |
| SiegeObserverCamera | [`void OnApplicationFocus(bool focused)`](../Assets/Scripts/Features/Siege/Interface/SiegeObserverCamera.cs#L229) | private |

## Assets/Scripts/Features/Siege/SiegeMatch.cs

// 职责：保存阵地战阶段、波次名额、金币和失败状态；不依赖场景对象或物理引擎。 // 入口：BeginNextWave() / RegisterAttacker() / ResolveAttacker() / TrySpend() / LoseBase()。 // 约束：生成成功才消耗名额；同一敌机只结算一次；维修和部署只能发生在整备阶段。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeMatch | [`bool BeginNextWave()`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L109) | public |
| SiegeMatch | [`bool CanSpawn(int maximumActive)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L125) | public |
| SiegeMatch | [`bool RegisterAttacker(string id, int maximumActive)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L130) | public |
| SiegeMatch | [`bool ResolveAttacker(string id, bool defeated)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142) | public |
| SiegeMatch | [`bool GrantBonus(int coins)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L165) | public |
| SiegeMatch | [`bool EnableReturnSettlement()`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L177) | public |
| SiegeMatch | [`bool CompleteReturn()`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L189) | public |
| SiegeMatch | [`void AddReward(int amount)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L202) | private |
| SiegeMatch | [`bool CanAfford(long price)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L214) | public |
| SiegeMatch | [`bool TrySpend(long price)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L219) | public |
| SiegeMatch | [`bool CanExchange(long price, long credit)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L231) | public |
| SiegeMatch | [`bool TryExchange(long price, long credit)`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L236) | public |
| SiegeMatch | [`void LoseBase()`](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L247) | public |

## Assets/Scripts/Features/Siege/SiegeProgress.cs

// 职责：记录阵地战的跨启动战果（最高波次与累计击毁），只写本地PlayerPrefs，不参与单局规则。 // 入口：TryRecordResult() / BestWave / TotalKills；由SiegeSession在失败时调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeProgress | [`bool TryRecordResult(int wave, int kills)`](../Assets/Scripts/Features/Siege/SiegeProgress.cs#L15) | public static |
| SiegeProgress | [`void RestoreForProbe(int bestWave, int totalKills, int totalRuns)`](../Assets/Scripts/Features/Siege/SiegeProgress.cs#L35) | public static |

## Assets/Scripts/Features/Siege/SiegeSession.cs

// 职责：托管阵地战会话生命周期、波次入口和基地失败；不改变现有Main WorldSession流程。 // 入口：Begin() / BeginNextWave() / End()；场景通过Inspector引用设置和泊位。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeSession | [`void BindWorkshop(SiegeWorkshop workshop)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L37) | public |
| SiegeSession | [`void SelectAttacker(int index)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L110) | public |
| SiegeSession | [`void SelectDefender(int index)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L118) | public |
| SiegeSession | [`bool RefreshBlueprints()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L126) | public |
| SiegeSession | [`int FindOption(SiegeShipOption[] options, string displayName, int fallback)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L160) | private static |
| SiegeSession | [`void Awake()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L186) | private |
| SiegeSession | [`void Start()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L197) | private |
| SiegeSession | [`bool Begin()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L207) | public |
| SiegeSession | [`void OnAttackerResolved(string victim, string attacker, int reward, bool playerCredit)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L240) | private |
| SiegeSession | [`bool BeginNextWave()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L245) | public |
| SiegeSession | [`void NotifyRefitChanged()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L275) | public |
| SiegeSession | [`bool TryDeployAt(int optionIndex, int dock, out string reason)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L280) | public |
| SiegeSession | [`bool TryDeploy(int optionIndex, out string reason)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L289) | public |
| SiegeSession | [`bool TryReplaceAt(int optionIndex, int dock, out string reason)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L298) | public |
| SiegeSession | [`bool TryCancelDeployment(int dock, out string reason)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L307) | public |
| SiegeSession | [`bool TryTakeControl(int slot)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L316) | public |
| SiegeSession | [`bool TryTakeControl(string id)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L327) | public |
| SiegeSession | [`bool TryRepair(bool repairBase)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L333) | public |
| SiegeSession | [`bool TryRepairShip(string id, bool repairBase = false)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L338) | public |
| SiegeSession | [`void End()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L357) | public |
| SiegeSession | [`void LoseBase()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L370) | public |
| SiegeSession | [`void NotifyCockpitDestroyed(Cockpit cockpit)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L391) | public |
| SiegeSession | [`void Update()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L397) | private |
| SiegeSession | [`void FixedUpdate()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L451) | private |
| SiegeSession | [`int ComboBonusFor(int combo, int perKill, int cap)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L472) | public static |
| SiegeSession | [`void TickKillStreak()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L484) | private |
| SiegeSession | [`void RepairBaseBetweenWaves()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L509) | private |
| SiegeSession | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L544) | private |
| SiegeSession | [`void SetSimulationActive(bool active)`](../Assets/Scripts/Features/Siege/SiegeSession.cs#L557) | private |

## Assets/Scripts/Features/Siege/SiegeSettings.cs

// 职责：保存阵地战可编辑蓝图、经济、波次与部署预算；不持有运行中会话状态。 // 入口：CreateMatch()；由SiegeSession读取，场景制作工具创建默认配置资产。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeSettings | [`SiegeMatch CreateMatch()`](../Assets/Scripts/Features/Siege/SiegeSettings.cs#L116) | public |

## Assets/Scripts/Features/Siege/Validation/SiegeDrivingPlayerChecks.cs

// 职责：显式Player验证通过虚拟Input System设备走键盘、HUD点击与真实飞行物理链。 // 入口：Run()；只由-siege-checks-driving调用，finally恢复设备与后台输入设置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeDrivingPlayerChecks | [`IEnumerator Run(SiegeSession session, Action<string, bool> check, Report report)`](../Assets/Scripts/Features/Siege/Validation/SiegeDrivingPlayerChecks.cs#L15) | internal static |
| SiegeDrivingPlayerChecks | [`Thruster[] DirectionalThrusters(ControlUnit unit)`](../Assets/Scripts/Features/Siege/Validation/SiegeDrivingPlayerChecks.cs#L194) | private static |
| SiegeDrivingPlayerChecks | [`float HorizontalDistance(Vector3 from, Vector3 to)`](../Assets/Scripts/Features/Siege/Validation/SiegeDrivingPlayerChecks.cs#L199) | private static |

## Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs

// 职责：显式性能诊断时读取可用的引擎计时/GC计数，记录缺失和缓冲溢出，避免旧值冒充新帧。 // 入口：Read() / Dispose()；只读已完成样本并清空缓冲，跨帧发布延迟不保证与delta严格对齐。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeEngineCounters | [`Sample Read()`](../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L60) | public |
| SiegeEngineCounters | [`long ReadCounter(int index)`](../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L81) | private |
| SiegeEngineCounters | [`void Dispose()`](../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L122) | public |

## Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs

// 职责：仅显式命令行参数启用新版菜单到Siege的独立Player闭环和自然战斗观察，不写玩家蓝图。 // 入口：Initialize()；smoke明确施加真实伤害，soak只观察自然战斗；两类结果分开报告。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeGameplayPlayerChecks | [`void Initialize()`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L68) | private static |
| SiegeGameplayPlayerChecks | [`void Update()`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L112) | private |
| SiegeGameplayPlayerChecks | [`void LateUpdate()`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L138) | private |
| SiegeGameplayPlayerChecks | [`IEnumerator Run()`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L160) | private |
| SiegeGameplayPlayerChecks | [`Cockpit[] Enemies()`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L348) | private |
| SiegeGameplayPlayerChecks | [`T Get(object owner, string field)`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L353) | private static |
| SiegeGameplayPlayerChecks | [`void Set(object owner, string field, object value)`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L358) | private static |
| SiegeGameplayPlayerChecks | [`void Capture(string label)`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L363) | private |
| SiegeGameplayPlayerChecks | [`void Check(bool success, string name)`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L368) | private |
| SiegeGameplayPlayerChecks | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L379) | private |
| SiegeGameplayPlayerChecks | [`void Save()`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L390) | private |
| SiegeGameplayPlayerChecks | [`void Finish()`](../Assets/Scripts/Features/Siege/Validation/SiegeGameplayPlayerChecks.cs#L396) | private |

## Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs

// 职责：仅显式-siege-checks-report参数启动独立Player验证，记录真实帧、玩法契约与自然多波结果。 // 入口：Initialize()；smoke使用明确损伤刺激验证闭环，soak只自然战斗/整备，不把两种结果混用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayerChecks | [`void Initialize()`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L30) | private static |
| SiegePlayerChecks | [`void Update()`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L93) | private |
| SiegePlayerChecks | [`void LateUpdate()`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L114) | private |
| SiegePlayerChecks | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L132) | private |
| SiegePlayerChecks | [`IEnumerator Run()`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L140) | private |
| SiegePlayerChecks | [`bool SupportsDriving(SiegeShipOption option)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L470) | private static |
| SiegePlayerChecks | [`string ImagePath(string suffix)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L477) | private |
| SiegePlayerChecks | [`void Check(string name, bool passed)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L484) | private |
| SiegePlayerChecks | [`void CaptureError(string condition, string trace, LogType type)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L494) | private |
| SiegePlayerChecks | [`void Finish()`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L502) | private |
| SiegePlayerChecks | [`float Percentile(float fraction)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L521) | private |

## Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs

// 职责：显式Player检查的低频规模普查、有限慢帧记录和可选嵌套CPU计时；不参与正常游戏。 // 入口：Observe() / Dispose()；帧间隔对应前一观察窗口，普查自身开销单独记录，不能据相关性推断瓶颈。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegePlayerSurvey | [`void Observe(SiegeSession session, float milliseconds)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L39) | public |
| SiegePlayerSurvey | [`BodyCounts Classify(Rigidbody[] bodies)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L75) | public static |
| SiegePlayerSurvey | [`Census TakeCensus(SiegeSession session, float seconds, Work work)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L128) | private static |
| SiegePlayerSurvey | [`void KeepSlowFrame(SlowFrame frame)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L156) | private |
| SiegePlayerSurvey | [`Work ReadWork()`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L175) | private |
| SiegePlayerSurvey | [`StageWork ReadStage(SimulationWorkMetrics.Stage stage)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L203) | private static |
| SiegePlayerSurvey | [`void Dispose()`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L214) | public |
| Work | [`Work Subtract(Work current, Work previous)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L315) | public static |
| Work | [`StageWork Difference(StageWork current, StageWork previous)`](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L337) | private static |

## Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs

// 职责：在独立压力场景分帧恢复附加建筑，沿用生产建筑预算/支撑/损毁，不参与基地胜负。 // 入口：Start()；Inspector配置真实建筑蓝图与锚点，加载期间暂停HUD交互，场景退出释放拥有的建筑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeStressPopulation | [`IEnumerator Start()`](../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L40) | private |
| SiegeStressPopulation | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L138) | private |
| SiegeStressPopulation | [`void RestoreHud()`](../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L154) | private |

## Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs

// 职责：在整备阶段复用 Main 蓝图编辑器，隔离已部署舰队与大型建筑，退出后恢复驾驶视图。 // 入口：Open() / Close()；只编辑存档预览，不免费替换战场机体或恢复耐久。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWorkshop | [`void Awake()`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L23) | private |
| SiegeWorkshop | [`void Start()`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L28) | private |
| SiegeWorkshop | [`void Update()`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L37) | private |
| SiegeWorkshop | [`void OpenSelected()`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L42) | private |
| SiegeWorkshop | [`bool Open(string saveName = null)`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L47) | public |
| SiegeWorkshop | [`bool Close()`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L88) | public |
| SiegeWorkshop | [`void SetPanels(bool editing)`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L123) | private |
| SiegeWorkshop | [`void OnDestroy()`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L143) | private |

## Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs

// 职责：为蓝图编辑建立独立预览场景，将战场暂存到不模拟的物理场景，保留组件启用状态和伤势。 // 入口：Open() / Dispose()；恢复原父节点、场景、镜头与活动场景，异步卸载仅限本对象创建的场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SiegeWorkshopStage | [`void Open(Scene scene)`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L21) | public |
| SiegeWorkshopStage | [`void Move(GameObject target, Scene scene, List<Placement> records)`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L76) | private static |
| SiegeWorkshopStage | [`void Transfer(GameObject target, Scene scene)`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L83) | private static |
| SiegeWorkshopStage | [`void Dispose()`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L107) | public |
| SiegeWorkshopStage | [`void Restore(List<Placement> records)`](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L143) | private static |

## Assets/Scripts/Features/Simulation/Block.cs

// 职责：保存方块尺寸、质量、连接面和编辑器配置；由 BlockConnections 维护运行时连接。 // 入口：GetDisplayInfo() / GetConnectorWorldPosition() / GetConnectorWorldNormal()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Block | [`void CollectRuntimeNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/Block.cs#L62) | internal |
| Block | [`void RefreshRuntimeConnectionPose()`](../Assets/Scripts/Features/Simulation/Block.cs#L67) | internal |
| Block | [`void InvalidateRuntimeConnections()`](../Assets/Scripts/Features/Simulation/Block.cs#L72) | internal |
| Block | [`void OnEnable()`](../Assets/Scripts/Features/Simulation/Block.cs#L77) | private |
| Block | [`void OnDisable()`](../Assets/Scripts/Features/Simulation/Block.cs#L83) | private |
| Block | [`void RestoreDimensions(int width, int height, int depth)`](../Assets/Scripts/Features/Simulation/Block.cs#L89) | internal |
| Block | [`void Awake()`](../Assets/Scripts/Features/Simulation/Block.cs#L102) | private |
| Block | [`void OnValidate()`](../Assets/Scripts/Features/Simulation/Block.cs#L139) | private |
| Block | [`void RefreshAuthoringData()`](../Assets/Scripts/Features/Simulation/Block.cs#L156) | public |
| Block | [`void RefreshRuntimeData(bool rebuildConnectors = false)`](../Assets/Scripts/Features/Simulation/Block.cs#L178) | public |
| Block | [`void RefreshMass()`](../Assets/Scripts/Features/Simulation/Block.cs#L219) | public |
| Block | [`void SetMassSettings(bool useCustomMass, float value)`](../Assets/Scripts/Features/Simulation/Block.cs#L232) | public |
| Block | [`void Reset()`](../Assets/Scripts/Features/Simulation/Block.cs#L240) | private |
| Block | [`void GenerateConnectionPoints()`](../Assets/Scripts/Features/Simulation/Block.cs#L245) | private |
| Block | [`string GetDisplayInfo()`](../Assets/Scripts/Features/Simulation/Block.cs#L315) | public |
| Block | [`string GenerateInfo()`](../Assets/Scripts/Features/Simulation/Block.cs#L320) | private |
| Block | [`void CreateConnectionPoint(ConnectType connectType, Vector3 localPos, Vector3 normal, int order)`](../Assets/Scripts/Features/Simulation/Block.cs#L373) | private |
| Block | [`Vector3 GetConnectorWorldPosition(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L387) | public |
| Block | [`Vector3 GetConnectorWorldNormal(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L392) | public |
| Block | [`bool IsConnectorAvailableForPlacement(Connector connector)`](../Assets/Scripts/Features/Simulation/Block.cs#L397) | public |
| Block | [`void CheckConnection()`](../Assets/Scripts/Features/Simulation/Block.cs#L402) | public |
| Block | [`void CheckConnectionAfterPhysicsSync()`](../Assets/Scripts/Features/Simulation/Block.cs#L407) | internal |
| Block | [`List<Block> Neighbors()`](../Assets/Scripts/Features/Simulation/Block.cs#L412) | public |
| Block | [`void CollectNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/Block.cs#L420) | internal |
| Block | [`void DisConnectAllConnectors(bool refreshNeighbors = true)`](../Assets/Scripts/Features/Simulation/Block.cs#L425) | public |
| Block | [`void RestoreConnectionParticipation()`](../Assets/Scripts/Features/Simulation/Block.cs#L431) | public |
| Block | [`void RefreshConnectionStatus()`](../Assets/Scripts/Features/Simulation/Block.cs#L436) | internal |
| Block | [`bool IsBlockedGhost()`](../Assets/Scripts/Features/Simulation/Block.cs#L444) | public |
| Block | [`void OnDrawGizmos()`](../Assets/Scripts/Features/Simulation/Block.cs#L484) | private |

## Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs

// 职责：批量重建方块邻接与双侧连接；每个执行片段统一同步物理。 // 入口：RebuildBatchRoutine() / RebuildBatchImmediate()；分帧版本用于加载中的静止蓝图。 // 约束：预算在完整单块操作后检查，不保证单个昂贵操作低于预算；恢复后重新同步物理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BatchConnectionBuilder | [`IEnumerator RebuildBatchRoutine()`](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L18) | public |
| BatchConnectionBuilder | [`void RebuildBatchImmediate()`](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L40) | public |
| BatchConnectionBuilder | [`void RefreshBlock(Block block)`](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L54) | private static |

## Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs

// 职责：为建筑与载具准备同一份双侧连接图；几何失效重建，删除成员仅重探附近幸存模块。 // 入口：Prepare() / PrepareBlocks() / CollectNeighbors()；支撑和驾驶舱策略由消费者决定。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockConnectionGraph | [`void Prepare(List<Block> blocks, Transform root)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L19) | internal |
| BlockConnectionGraph | [`Bounds ProbeBounds(Block block, Matrix4x4 worldToRoot)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L87) | private static |
| BlockConnectionGraph | [`bool PrepareBlocks(List<Block> blocks, bool force = false)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L109) | internal static |
| BlockConnectionGraph | [`void CollectNeighbors(Block block, List<Block> result)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L131) | internal |

## Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs

// 职责：精确校验运行连接缓存的模块局部姿态、连接面身份/几何、规范碰撞体与查询层配置。 // 入口：IsValid / Capture() / Invalidate()；刚体整体运动不失效，复杂层级回退物理探测。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockConnectionSnapshot | [`void Capture()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs#L71) | internal |
| BlockConnectionSnapshot | [`bool HasCanonicalColliderLayout()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs#L120) | private |
| BlockConnectionSnapshot | [`void RefreshIfReady()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs#L142) | internal |
| BlockConnectionSnapshot | [`void Invalidate()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs#L150) | internal |
| BlockConnectionSnapshot | [`int ConnectionMask()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs#L155) | private static |

## Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs

// 职责：探测匹配连接面、维护双侧连接引用和邻接查询；与方块序列化配置分离。 // 入口：CheckConnection() / CollectNeighbors() / DisConnectAllConnectors()。 // 约束：批量图遍历自行统一同步物理；探测缓冲满时扩容，连接视觉不决定拓扑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockConnections | [`bool IsConnectorAvailableForPlacement(Connector connector)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L45) | internal |
| BlockConnections | [`void CheckConnection(bool synchronizePhysics = true)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L59) | internal |
| BlockConnections | [`void ReleaseExistingConnections()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L103) | private |
| BlockConnections | [`void ReleaseConnectionReferences(GameObject connectorObject)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L131) | private |
| BlockConnections | [`Block FindBlockAcrossConnector(Connector connector)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L168) | private |
| BlockConnections | [`bool SharesBuildingScope(Block other)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L236) | private |
| BlockConnections | [`Connector FindMatchingConnector(Block otherBlock, Connector connector, bool requireAvailable = true)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L243) | private |
| BlockConnections | [`void ConnectTo(Connector connector, Block otherBlock, Connector otherConnector)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L265) | private |
| BlockConnections | [`void ClearConnector(Connector connector)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L292) | private |
| BlockConnections | [`void DestroyConnectorVisual(GameObject connectorObject)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L319) | private static |
| BlockConnections | [`void CollectNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L334) | internal |
| BlockConnections | [`void DisConnectAllConnectors(bool refreshNeighbors = true)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L362) | internal |
| BlockConnections | [`void RestoreParticipation()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L408) | internal |
| BlockConnections | [`void ReleaseRuntimePair(Connector connector)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L427) | private |
| BlockConnections | [`void CollectRuntimeNeighbors(List<Block> result)`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L453) | internal |
| BlockConnections | [`void RefreshRuntimePoseIfReady()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L475) | internal |
| BlockConnections | [`void InvalidateRuntimeLinks()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L480) | internal |
| BlockConnections | [`void CaptureRuntimePose()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L485) | private |
| BlockConnections | [`int ConnectionMask()`](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L490) | private static |

## Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs

// 职责：按需累计连接遍历、刚体重建与完整刷新的CPU工作量，供可重复Play采样读取。 // 入口：Measure() / Read() / Reset()；默认关闭，嵌套阶段分别计时，不能相加为整帧耗时。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| SimulationWorkMetrics | [`void RecordPhysicsNeighborQuery()`](../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L48) | internal static |
| SimulationWorkMetrics | [`void RecordLegacyReferenceScan()`](../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L56) | internal static |
| SimulationWorkMetrics | [`Scope Measure(Stage stage, int inputBlocks = 0)`](../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L64) | public static |
| SimulationWorkMetrics | [`Counters Read(Stage stage)`](../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L69) | public static |
| SimulationWorkMetrics | [`void Reset()`](../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L74) | public static |
| Scope | [`void Dispose()`](../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L93) | public |

## Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs

// 职责：统一逻辑方块与场景方块的变换包围盒，不包含地基、净空或加载窗口策略。 // 入口：FromBlueprint() / FromConstruct() / Transform()；调用者显式选择输出坐标和是否包含额外原点。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockBounds | [`Bounds FromBlueprint(BlockData block, Matrix4x4 localToOutput)`](../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L8) | public static |
| BlockBounds | [`Bounds FromBlueprint(IReadOnlyList<BlockData> blocks, Matrix4x4 localToOutput)`](../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L17) | public static |
| BlockBounds | [`Bounds FromConstruct(Transform construct, Matrix4x4 worldToOutput, Vector3 emptyCenter)`](../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L29) | public static |
| BlockBounds | [`Bounds Transform(Bounds local, Matrix4x4 localToOutput)`](../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L42) | public static |
| BlockBounds | [`void Encapsulate(ref Bounds bounds, ref bool found, Bounds next)`](../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L62) | public static |

## Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs

// 职责：按方块连接关系分组并计算组中心，供游玩物理单元建立使用。 // 入口：GroupBlocks() / CalculateGroupCenter()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BlockGroupManager | [`List<List<Block>> GroupBlocks(List<Block> allBlocks)`](../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L9) | public static |
| BlockGroupManager | [`List<List<Block>> GroupRuntimeBlocks(List<Block> allBlocks)`](../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L15) | internal static |
| BlockGroupManager | [`List<List<Block>> GroupPreparedBlocks(List<Block> blocks)`](../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L27) | internal static |
| BlockGroupManager | [`List<List<Block>> Traverse(List<Block> allBlocks, bool runtimeLinks)`](../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L32) | private static |
| BlockGroupManager | [`Vector3 CalculateGroupCenter(List<Block> group)`](../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L100) | public static |

## Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs

// 职责：传递重组刚体的阻尼、解穿透及继承速度限制，不拥有 Inspector 数据。 // 入口：由 PlayManager 从现有序列化字段构造，载具与建筑共用 ApplyTo() 和运动继承限制。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GroupPhysicsSettings | [`void ApplyTo(Rigidbody body)`](../Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs#L12) | internal |
| GroupPhysicsSettings | [`GroupPhysicsSettings FromBody(Rigidbody body)`](../Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs#L19) | internal static |

## Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs

// 职责：保留含驾驶舱或最大分量的源刚体，仅迁移分离模块，并恢复捕获的源运动与归属。 // 入口：Rebuild()；先捕获所有源状态，再规划/迁移，最后局部刷新并一次执行外层清理。 // 约束：保留已有根的姿态与运行组件；新碎片不覆盖RuntimeUnitMember原始单位身份。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RuntimeGroupBuilder | [`void Rebuild(List<Block> blocks, GroupPhysicsSettings settings)`](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L16) | internal |
| RuntimeGroupBuilder | [`Dictionary<Block, SourceSnapshot> CaptureSourceBodies(List<Block> blocks)`](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L131) | private static |
| RuntimeGroupBuilder | [`List<GroupPlan> PlanGroups(List<List<Block>> groups, Dictionary<Block, SourceSnapshot> sources)`](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L160) | private static |
| RuntimeGroupBuilder | [`Dictionary<SourceSnapshot, GroupPlan> SelectRetainedGroups(List<GroupPlan> plans, Dictionary<Block, SourceSnapshot> sources)`](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L191) | private static |

## Assets/Scripts/Features/Simulation/Groups/RuntimeGroupCleanup.cs

// 职责：按无驾驶舱组上限和玩家距离安排残骸清理，不立即销毁或更改归属。 // 入口：EnforceLimit() / EnforceDistance()；由 PlayManager 在原有时序调用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RuntimeGroupCleanup | [`void EnforceLimit(List<ControlUnit> units, int maximumUselessGroups, DestroyManager destruction)`](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupCleanup.cs#L8) | internal static |
| RuntimeGroupCleanup | [`void EnforceDistance( List<ControlUnit> units, bool playMode, Transform playerRoot, float cleanupDistance, DestroyManager destruction)`](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupCleanup.cs#L51) | internal static |

## Assets/Scripts/Features/Simulation/Modes/GameModeController.cs

// 职责：由Main拥有模式选择、Additive场景加载、探索绑定和返回；共享服务始终留在Main活动场景。 // 入口：ShowSelection() / SelectExploration() / SelectSiege() / ReturnToBuild()；同一时间只持有一个玩法场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GameModeController | [`void Start()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L54) | private |
| GameModeController | [`void ShowSelection()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L61) | public |
| GameModeController | [`void SelectExploration()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L79) | public |
| GameModeController | [`void SelectSiege()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L84) | public |
| GameModeController | [`void LaunchSiege(string blueprintName, TextAsset example = null, string exampleName = null)`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L92) | public |
| GameModeController | [`void Select(bool exploration)`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L107) | private |
| GameModeController | [`IEnumerator LoadMode()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L127) | private |
| GameModeController | [`void CreateExplorationCopy()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L197) | public |
| GameModeController | [`void CaptureReturnCargo(bool survived)`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L202) | public |
| GameModeController | [`void ReturnToBuild()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L207) | public |
| GameModeController | [`void ExplorationEnded()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L227) | public |
| GameModeController | [`void Retry()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L236) | public |
| GameModeController | [`void Restart()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L241) | public |
| GameModeController | [`IEnumerator RestartRoutine()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L249) | private |
| GameModeController | [`IEnumerator GuardTransition(IEnumerator work)`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L278) | private |
| GameModeController | [`void BeginReturn(bool failed = false)`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L325) | private |
| GameModeController | [`IEnumerator ReturnRoutine(bool failed)`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L332) | private |
| GameModeController | [`void CaptureInput()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L376) | private |
| GameModeController | [`void RestoreInput()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L392) | private |
| GameModeController | [`void OnDestroy()`](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L403) | private |

## Assets/Scripts/Features/Simulation/Modes/Interface/GameModePanel.cs

// 职责：展示模式选择、场景加载和失败重试；按钮仅调用Main的模式协调器。 // 入口：Bind() / Show()；布局与引用通过Editor制作并保存在Main场景。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| GameModePanel | [`void Bind(GameModeController controller)`](../Assets/Scripts/Features/Simulation/Modes/Interface/GameModePanel.cs#L32) | public |
| GameModePanel | [`void Show(bool visible)`](../Assets/Scripts/Features/Simulation/Modes/Interface/GameModePanel.cs#L41) | public |
| GameModePanel | [`void Update()`](../Assets/Scripts/Features/Simulation/Modes/Interface/GameModePanel.cs#L60) | private |
| GameModePanel | [`void Refresh()`](../Assets/Scripts/Features/Simulation/Modes/Interface/GameModePanel.cs#L68) | private |
| GameModePanel | [`void OnDestroy()`](../Assets/Scripts/Features/Simulation/Modes/Interface/GameModePanel.cs#L83) | private |

## Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs

// 职责：保留未保存的建造原稿与镜头，给探索创建独立副本；返回仅迁移既有货仓结果。 // 入口：Park() / CreateRuntimeCopy() / Restore()；原稿不参加运行时控制单元清理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PlayDraft | [`void Park()`](../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L17) | public |
| PlayDraft | [`void CreateRuntimeCopy(Scene scene)`](../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L44) | public |
| PlayDraft | [`void Restore()`](../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L55) | public |
| PlayDraft | [`void CaptureReturnCargo(bool survived)`](../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L89) | public |

## Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs

// 职责：为建筑与载具碎片提供同一最短物理保留期，避免预算和旧清理计时器吞掉新断片。 // 入口：Mark() 在脱落时调用；Remaining() / CanRetire() 在真正清理前检查；不阻止伤害和场景卸载。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| FragmentLifetime | [`void Awake()`](../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L11) | private |
| FragmentLifetime | [`void Mark(GameObject root)`](../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L16) | internal static |
| FragmentLifetime | [`float Remaining(GameObject root)`](../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L31) | internal static |
| FragmentLifetime | [`bool CanRetire(float age)`](../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L37) | internal static |

## Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs

// 职责：集中计算碰撞/爆炸的力学与伤害反馈规则。 // 入口：ReducedMass() / DamageEnergy() / ClosestPoint()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ImpactPhysics | [`void ResetEvents()`](../Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs#L10) | private static |
| ImpactPhysics | [`float ReducedMass(float mass, float otherMass, bool otherIsStatic)`](../Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs#L16) | public static |
| ImpactPhysics | [`float DamageEnergy(float reducedMass, float normalSpeed, float safeSpeed)`](../Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs#L23) | public static |
| ImpactPhysics | [`float BodyEnergyShare(float mass, float otherMass, bool otherIsStatic)`](../Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs#L28) | public static |
| ImpactPhysics | [`float Restitution(Collider first, Collider second, float normalSpeed)`](../Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs#L34) | public static |
| ImpactPhysics | [`int CombinePriority(PhysicsMaterialCombine mode)`](../Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs#L62) | private static |
| ImpactPhysics | [`Vector3 ClosestPoint(Collider collider, Vector3 position)`](../Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs#L77) | public static |
| ImpactPhysics | [`void ReportPlayerImpact(float energy, float impulse)`](../Assets/Scripts/Features/Simulation/Physics/ImpactPhysics.cs#L88) | public static |

## Assets/Scripts/Features/Simulation/Physics/RigidbodyMotion.cs

// 职责：在拆分层级前捕获刚体运动，供载具分组和建筑残骸共同继承质心速度与角速度。 // 入口：构造时捕获；Restore() 在迁移和质量更新后调用，避免源质心变化污染后续碎片。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RigidbodyMotion | [`void Restore(Rigidbody target, GroupPhysicsSettings settings)`](../Assets/Scripts/Features/Simulation/Physics/RigidbodyMotion.cs#L17) | internal |

## Assets/Scripts/Features/Simulation/PlayManager.cs

// 职责：协调建造/游玩会话、组注册、选中表现及遥测；重组和清理规则由 Simulation 功能实现。 // 入口：PlayStart() / CanStartPlay() / RefreshGroup()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PlayManager | [`void BindWorldSession(WorldSessionManager session)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L21) | public |
| PlayManager | [`void BindSiege(SiegeSession session)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L34) | public |
| PlayManager | [`void Awake()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L76) | private |
| PlayManager | [`void FixedUpdate()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L81) | private |
| PlayManager | [`void PlayStart()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L113) | public |
| PlayManager | [`bool CanStartPlay(out string reason)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L162) | public |
| PlayManager | [`void RefreshGroup(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L197) | public |
| PlayManager | [`void SetPlayMode(bool lockView)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L228) | public |
| PlayManager | [`void CalculateVelocity()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L236) | private |
| PlayManager | [`void HandleSelection()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L245) | private |
| PlayManager | [`void SelectBlock(Block block)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L274) | private |
| PlayManager | [`void DeselectBlock()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L293) | public |
| PlayManager | [`void PlayEnd()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L299) | public |
| PlayManager | [`void AssignBlocksToParentGroups(List<Block> blocks)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L334) | public |
| PlayManager | [`void RegisterControlUnit(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L351) | public |
| PlayManager | [`void RegisterControlUnitWithoutCleanup(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L361) | internal |
| PlayManager | [`void UnregisterControlUnit(ControlUnit unit)`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L369) | public |
| PlayManager | [`IReadOnlyList<ControlUnit> GetControlUnits()`](../Assets/Scripts/Features/Simulation/PlayManager.cs#L377) | public |

## Assets/Scripts/Features/Simulation/Presentation/PlayBlockSelection.cs

// 职责：保存并恢复选中方块的材质，维护选中视觉效果；不参与连接或组重建。 // 入口：Highlight() / Clear()；由 PlayManager 负责选中状态及 Rack 的玩法副作用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| PlayBlockSelection | [`void Highlight(Block block, Material highlightMaterial)`](../Assets/Scripts/Features/Simulation/Presentation/PlayBlockSelection.cs#L9) | internal |
| PlayBlockSelection | [`void Clear(Block block)`](../Assets/Scripts/Features/Simulation/Presentation/PlayBlockSelection.cs#L37) | internal |

## Assets/Scripts/Features/Simulation/Units/Cockpit.cs

// 职责：保存驾驶舱阵营与驾驶来源；驾驶权随驾驶舱迁移，不随物理父组重建丢失。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| Cockpit | [`void SetControlMode(UnitControlMode mode)`](../Assets/Scripts/Features/Simulation/Units/Cockpit.cs#L21) | public |
| Cockpit | [`void SetAnchored(bool value)`](../Assets/Scripts/Features/Simulation/Units/Cockpit.cs#L26) | public |

## Assets/Scripts/Features/Simulation/Units/ControlUnit.cs

// 职责：组织运行单元的方块、刚体、输入、总耐久与资源归属。 // 入口：ProtectExplosionContacts() / TryGetTotalDurability() / RefreshChildren()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ControlUnit | [`IReadOnlyList<Durability> GetDamageTargets()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L19) | internal |
| ControlUnit | [`void OnTransformChildrenChanged()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L32) | private |
| ControlUnit | [`bool IsSceneSimulationActive(UnityEngine.SceneManagement.Scene scene)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L37) | public static |
| ControlUnit | [`void BindBuilding(BuildingRuntime building)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L84) | public |
| ControlUnit | [`void OnEnable()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L89) | private |
| ControlUnit | [`void OnDisable()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L97) | private |
| ControlUnit | [`void ProtectExplosionContacts(float duration)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L125) | public |
| ControlUnit | [`bool TryGetTotalDurability(out float current, out float maximum)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L137) | public |
| ControlUnit | [`void Start()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L162) | private |
| ControlUnit | [`void Update()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L174) | private |
| ControlUnit | [`void RefreshChildren()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L221) | public |
| ControlUnit | [`void AssignRuntimeOwnershipToBlocks(bool overwriteExisting)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L281) | public |
| ControlUnit | [`void EnsureRuntimeUnitId()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L302) | public |
| ControlUnit | [`void PlayEnd()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L310) | public |
| ControlUnit | [`void SetMovementInput(Vector3 worldDirection)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L320) | public |
| ControlUnit | [`void SetPropulsionInput(Vector3 worldDirection, float throttle)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L325) | public |
| ControlUnit | [`void SetTarget(Transform newTarget)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L332) | public |
| ControlUnit | [`Vector3 GetPlayerMovementInput()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L337) | private |
| ControlUnit | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L381) | private |
| ControlUnit | [`void OnDestroy()`](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L392) | private |

## Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs

// 职责：为任意阵营从蓝图建立模块飞船，统一尺寸、货物、身份、连接和物理初始化。 // 入口：IsValidBlueprint() / Spawn()；不写存档、不控制波次、不决定玩家驾驶权。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ModularUnitFactory | [`bool IsValidBlueprint(BlockDataList dataList, string displayName)`](../Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs#L8) | public static |
| ModularUnitFactory | [`ControlUnit Spawn( BlockDataList blueprint, string displayName, Vector3 position, Quaternion rotation, UnitFaction faction)`](../Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs#L41) | public static |

## Assets/Scripts/Features/Simulation/Units/ModularUnitValidator.cs

// 职责：检查模块单元组合与必要部件的有效性。 // 入口：TryGetSingleCockpit() / CountCockpits() / CountLoadedCockpits()。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ModularUnitValidator | [`bool TryGetSingleCockpit(Component root, out Cockpit cockpit, out string reason)`](../Assets/Scripts/Features/Simulation/Units/ModularUnitValidator.cs#L8) | public static |
| ModularUnitValidator | [`int CountCockpits(IEnumerable<Block> blocks)`](../Assets/Scripts/Features/Simulation/Units/ModularUnitValidator.cs#L29) | public static |
| ModularUnitValidator | [`int CountLoadedCockpits()`](../Assets/Scripts/Features/Simulation/Units/ModularUnitValidator.cs#L48) | public static |

## Assets/Scripts/Features/Simulation/Units/Rack.cs

// 职责：管理机器人停泊容器及所属单元关系。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/Simulation/Units/RuntimeUnitMember.cs

// 职责：保存模块的运行单元归属；组件预挂在模块 Prefab，运行时只更新数据。 // 入口：Ensure()；脱离主体后仍保留原阵营及身份。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| RuntimeUnitMember | [`RuntimeUnitMember Ensure(GameObject obj, string unitId, UnitFaction faction)`](../Assets/Scripts/Features/Simulation/Units/RuntimeUnitMember.cs#L9) | public static |

## Assets/Scripts/Features/Simulation/Units/UnitFaction.cs

// 职责：定义阵营和运行单元归属，支持友敌判断与残骸追踪。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs

// 职责：拥有一栋世界建筑的方块、虚拟支撑范围与刚体；处理损坏、断裂和脱锚。 // 入口：Initialize() / Activate() / DestroyBlock() / ReleaseAnchor() / Capture()。 // 生命周期：世界存档只捕获主体；断裂残骸由 WorldBuildingManager 限量管理。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingRuntime | [`bool TryRefit(BlockDataList design, out string reason)`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L42) | internal |
| BuildingRuntime | [`void RefreshStructure()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L56) | internal |
| BuildingRuntime | [`void BeginDamageBatch()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L64) | internal |
| BuildingRuntime | [`void EndDamageBatch()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L69) | internal |
| BuildingRuntime | [`void Initialize(WorldBuildingManager owner, WorldBuildingState state)`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L79) | public |
| BuildingRuntime | [`IEnumerator InitializeRoutine(WorldBuildingManager owner, WorldBuildingState state)`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L87) | internal |
| BuildingRuntime | [`void Activate()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L132) | public |
| BuildingRuntime | [`IEnumerator ActivateRoutine()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L140) | internal |
| BuildingRuntime | [`void PrepareForUnload()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L184) | public |
| BuildingRuntime | [`WorldBuildingState Capture()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L192) | public |
| BuildingRuntime | [`void DestroyBlock(Block block)`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L242) | public |
| BuildingRuntime | [`void DestroyBlockCore(Block block)`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L256) | private |
| BuildingRuntime | [`void SplitDisconnected()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L290) | private |
| BuildingRuntime | [`bool HasFullFoundationSupport()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L317) | public |
| BuildingRuntime | [`bool HasPreparedGroundConnector(Block block)`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L332) | private |
| BuildingRuntime | [`bool HasGroundConnector(Block block)`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L351) | private |
| BuildingRuntime | [`void ReleaseAnchor()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L372) | public |
| BuildingRuntime | [`float TotalMass()`](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L383) | private |

## Assets/Scripts/Features/World/Buildings/Data/WorldBuildingCatalog.cs

// 职责：保存建筑制作参考与WorldLab初始布局；运行时内容只从BuildingBlueprints按名称解析。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs

// 职责：仅从SaveManager共用文件仓读取建筑快照；World与WorldLab共用解析和错误隔离。 // 入口：LoadRoutine()预热世界资源；Load()刷新WorldLab目录；仅世界生成检查平台范围，不修改源文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingLibrary | [`IEnumerator LoadRoutine(BlueprintFileStore files)`](../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L19) | internal |
| WorldBuildingLibrary | [`void Load(BlueprintFileStore files)`](../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L51) | internal |
| WorldBuildingLibrary | [`IEnumerable<string> ReadSources(BlueprintFileStore files, bool forWorldGeneration, HashSet<string> warmupResources)`](../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L59) | private |
| WorldBuildingLibrary | [`void ReadSource(string name, string path, bool forWorldGeneration, HashSet<string> warmupResources)`](../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L82) | private |
| WorldBuildingLibrary | [`void CollectBlueprintResources(BlockDataList blueprint, HashSet<string> resources)`](../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L115) | private static |

## Assets/Scripts/Features/World/Buildings/Data/WorldBuildingState.cs

// 职责：保存世界建筑独立身份、根姿态、蓝图快照与逐块耐久；坐标相对建筑底座。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingFootprints.cs

// 职责：快照每套建筑真实支撑范围，并让地形和建筑生成使用同一确定性模板索引。 // 入口：构造后Get()读取；不包含驻军飞行范围，不访问场景或用户文件。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingFootprints | [`Bounds Get(int seed, Vector2Int column)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingFootprints.cs#L32) | public |
| WorldBuildingFootprints | [`int SelectTemplate(int seed, Vector2Int column, int count)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingFootprints.cs#L37) | public static |

## Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs

// 职责：WorldLab 建筑库面板、只含网格的放置预览与输入；模板读取失败不影响世界。 // 入口：Update() / OnGUI()；B开关建筑模式，Q/T旋转，左键放置，Esc取消。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingPalette | [`void Start()`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L37) | private |
| WorldBuildingPalette | [`void RefreshCatalog()`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L42) | public |
| WorldBuildingPalette | [`void Update()`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L56) | private |
| WorldBuildingPalette | [`void BeginPlacement()`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L131) | public |
| WorldBuildingPalette | [`void AddPreviewMeshes(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3? size = null)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L161) | private |
| WorldBuildingPalette | [`void Cancel()`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L203) | public |
| WorldBuildingPalette | [`void OnDisable()`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L218) | private |
| WorldBuildingPalette | [`void OnGUI()`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L223) | private |

## Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs

// 职责：在1m建筑网格与体素地形间转换位置，检测支撑、体素穿插和其他碰撞体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingPlacement | [`Vector3 Snap(ProceduralVoxelWorld world, Vector3 hit)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L6) | public static |
| WorldBuildingPlacement | [`bool Check( ProceduralVoxelWorld world, BlockDataList blueprint, Vector3 position, Quaternion rotation, out string reason)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L15) | public static |
| WorldBuildingPlacement | [`bool IntersectsTerrain(ProceduralVoxelWorld world, Vector3 center, Vector3 half, Quaternion rotation)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L77) | private static |
| WorldBuildingPlacement | [`Vector3 LocalAnchor(int x, int z)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L106) | public static |
| WorldBuildingPlacement | [`Vector3 LocalAnchor(BuildingFoundationData footprint, int x, int z)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L111) | public static |
| WorldBuildingPlacement | [`float Support(ProceduralVoxelWorld world, Vector2Int size, Transform pose)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L116) | public static |
| WorldBuildingPlacement | [`float Support(ProceduralVoxelWorld world, BuildingFoundationData footprint, Transform pose)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L121) | public static |
| WorldBuildingPlacement | [`Bounds Footprint(BuildingRuntime building)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L140) | public static |

## Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs

// 职责：创建前校验建筑模板、实例状态和资源，拒绝部分加载与非法尺寸/坐标。 // 入口：ValidateBlueprint() / ValidateState()；不会修改资源或活动世界。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingValidation | [`void ValidateBlueprint(BlockDataList blueprint)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L9) | public static |
| WorldBuildingValidation | [`void ValidateState(WorldBuildingState state)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L70) | public static |
| WorldBuildingValidation | [`bool Finite(float value)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L92) | public static |
| WorldBuildingValidation | [`bool Finite(Vector3 value)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L97) | public static |
| WorldBuildingValidation | [`bool ValidRotation(Quaternion value)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L102) | public static |
| WorldBuildingValidation | [`void ValidateWorldTemplate(BlockDataList blueprint)`](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L108) | public static |

## Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs

// 职责：将世界建筑同父刚性装饰网格按材质合并，并在本会话的同类模块间共享。 // 入口：Apply()/Clear()；只替换表现，保留方块、碰撞、损坏及可移动炮塔/机械层级。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingRenderCache | [`void Apply(Block block)`](../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L10) | internal |
| WorldBuildingRenderCache | [`bool IsVisible(Transform part, Transform model)`](../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L87) | private static |
| WorldBuildingRenderCache | [`string RelativePath(Transform parent, Transform model)`](../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L100) | private static |
| WorldBuildingRenderCache | [`Visual Build(Transform model, MeshRenderer[] renderers)`](../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L111) | private static |
| WorldBuildingRenderCache | [`void Clear()`](../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L191) | internal |

## Assets/Scripts/Features/World/Buildings/Topology/BuildingRefit.cs

// 职责：先暂存建筑缺件，再按原蓝图恢复模块身份/姿态/耐久；存活模块保留原对象和货物。 // 入口：Apply()；调用者负责整备权限、空间净空和扣费，本类不替换建筑根或核心。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| BuildingRefit | [`bool Apply( BuildingRuntime building, List<Block> blocks, WorldBuildingManager manager, BlockDataList design, out string reason)`](../Assets/Scripts/Features/World/Buildings/Topology/BuildingRefit.cs#L9) | internal static |

## Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs

// 职责：拥有 WorldLab 建筑实例、初始布局、地形支撑通知和有界残骸。 // 入口：Place() / Capture() / Restore() / ResetBuildings()；初始布局仅按名称查找共用建筑文件仓。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingManager | [`void OnEnable()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L40) | private |
| WorldBuildingManager | [`void Start()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L57) | private |
| WorldBuildingManager | [`void OnDisable()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L65) | private |
| WorldBuildingManager | [`bool CanPlace(BlockDataList blueprint, Vector3 position, Quaternion rotation, out string reason)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L76) | public |
| WorldBuildingManager | [`BuildingRuntime Place(BlockDataList blueprint, string name, Vector3 position, Quaternion rotation, UnitFaction faction)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L88) | public |
| WorldBuildingManager | [`BuildingRuntime RestoreBuilding(WorldBuildingState state)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L118) | public |
| WorldBuildingManager | [`IEnumerator RestoreBuildingRoutine(WorldBuildingState state, Action<BuildingRuntime> restored)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L139) | internal |
| WorldBuildingManager | [`void OptimizeVisuals(Block block)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L188) | internal |
| WorldBuildingManager | [`bool HasCapacity(int additionalBlocks)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L196) | public |
| WorldBuildingManager | [`bool CanAddBlocks(int additionalBlocks)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L201) | public |
| WorldBuildingManager | [`void RemoveBuilding(BuildingRuntime building)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L208) | public |
| WorldBuildingManager | [`void PrepareForUnload()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L221) | public |
| WorldBuildingManager | [`void ClearSession()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L232) | public |
| WorldBuildingManager | [`void OnDestroy()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L239) | private |
| WorldBuildingManager | [`BuildingRuntime Stage(WorldBuildingState state, Transform parent)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L244) | private |
| WorldBuildingManager | [`WorldLabSnapshot Capture()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L262) | public |
| WorldBuildingManager | [`void Restore(WorldLabSnapshot snapshot)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L282) | public |
| WorldBuildingManager | [`void ResetBuildings()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L337) | public |
| WorldBuildingManager | [`void ClearBuildings()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L381) | private |
| WorldBuildingManager | [`void TerrainChanged(Bounds changed)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L397) | private |
| WorldBuildingManager | [`void FixedUpdate()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L408) | private |
| WorldBuildingManager | [`void DamageSphere(Vector3 center, float radius, float damage)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L425) | public |
| WorldBuildingManager | [`void ExplodeBlock(Block block)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L437) | internal |
| WorldBuildingManager | [`void ApplyBuildingLoss(BuildingRuntime building, Action destroy)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L456) | internal |
| WorldBuildingManager | [`void CreateDebris(List<Block> group, Rigidbody source, RigidbodyMotion motion)`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L468) | internal |
| WorldBuildingManager | [`void ClearDebris()`](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L506) | private |

## Assets/Scripts/Features/World/Outposts/EnemyOutpostData.cs

// 职责：保存建筑附带的有限驻军蓝图与本地部署点，独立于用户敌机文件后续改动。 // 入口：Validate()；建筑蓝图缺少outpost字段时仍是普通建筑。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyOutpostData | [`void Validate()`](../Assets/Scripts/Features/World/Outposts/EnemyOutpostData.cs#L13) | public |
| OutpostGarrisonData | [`BlockDataList CreateBlueprint()`](../Assets/Scripts/Features/World/Outposts/EnemyOutpostData.cs#L70) | public |

## Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs

// 职责：会话内管理据点有限驻军、击败记录及远距离休眠；退出探索即丢弃，不写永久存档。 // 入口：Tick() / Clear()；击败只由驾驶舱显式死亡通知，卸载不计击败。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostEncounterDirector | [`bool IsDefeated(string site, string guard)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L29) | public |
| OutpostEncounterDirector | [`bool TryGetDefender(string site, string guardId, out ControlUnit unit)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L34) | public |
| OutpostEncounterDirector | [`void Tick(Vector3 player)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51) | public |
| OutpostEncounterDirector | [`bool Spawn(Encounter encounter, Guard guard, Vector3 position)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L170) | private |
| OutpostEncounterDirector | [`void UpdateParkedModel(Encounter encounter, Guard guard, Vector3 position, bool baseReady)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L248) | private |
| OutpostEncounterDirector | [`void ClearParkedModel(Guard guard)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L269) | private static |
| OutpostEncounterDirector | [`Bounds DeploymentBounds(BlockDataList blueprint, Vector3 position, Quaternion rotation)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L279) | private static |
| OutpostEncounterDirector | [`void Hibernate(Guard guard)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L291) | private static |
| OutpostEncounterDirector | [`void RefreshOwner(Guard guard)`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L328) | private static |
| OutpostEncounterDirector | [`void Clear()`](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L337) | public |

## Assets/Scripts/Features/World/Outposts/OutpostHud.cs

// 职责：在游玩HUD显示最近据点剩余驻军及本次探索清剿数量。 // 入口：Update()；引用由OutpostHudAuthoring保存，不在运行时生成界面。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostHud | [`void Bind(WorldSessionManager session)`](../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L14) | public |
| OutpostHud | [`void Update()`](../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L19) | private |
| OutpostHud | [`string Format(OutpostEncounterStatus? nearest, int clearedCount)`](../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L31) | public |
| OutpostHud | [`string FitName(string name, string suffix)`](../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L46) | private |

## Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs

// 职责：将体素修改保存为有版本/种子/基线指纹的差量；校验全部通过才返回新数据。 // 入口：Capture() / Restore()；不读写磁盘、不修改传入世界，拒绝不兼容或重复单元。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelWorldEdits | [`VoxelWorldEdits Capture(VoxelWorldData current, int worldSeed)`](../Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs#L22) | public static |
| VoxelWorldEdits | [`VoxelWorldData Restore(int expectedSeed, Vector3Int expectedSize)`](../Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs#L52) | public |

## Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs

// 职责：独立WorldLab差量JSON的原子保存、上一份备份与有大小限制的读取。 // 入口：Save() / Load()；仅写调用者传入路径，不接触玩家载具蓝图和货仓。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabPersistence | [`void Save(string path, VoxelWorldEdits edits)`](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L10) | public static |
| WorldLabPersistence | [`void SaveSnapshot(string path, WorldLabSnapshot snapshot)`](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L15) | public static |
| WorldLabPersistence | [`void WriteJson(string path, string json)`](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L20) | private static |
| WorldLabPersistence | [`VoxelWorldEdits Load(string path)`](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L57) | public static |
| WorldLabPersistence | [`WorldLabSnapshot LoadSnapshot(string path, float legacyCellSize)`](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L68) | public static |
| WorldLabPersistence | [`string ReadJson(string path)`](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L90) | private static |

## Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs

// 职责：按列缓存无碰撞体素过渡环，再衔接低密度远景与距离雾。 // 入口：Refresh()分帧重建远景，Enter()/Dispose()成对应用和恢复相机/环境设置。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldDistanceView | [`Vector2Int CenterAtFocus()`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L47) | private |
| WorldDistanceView | [`IEnumerator Refresh()`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L69) | internal |
| WorldDistanceView | [`IEnumerator Rebuild(Vector2Int center)`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L95) | private |
| WorldDistanceView | [`void OnColumnVisibilityChanged(Vector2Int coordinate, bool loaded)`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L234) | private |
| WorldDistanceView | [`void UpdateRingVisibility()`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L248) | private |
| WorldDistanceView | [`void ReleaseExpiredRing(Vector2Int center, int radius)`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L266) | private |
| WorldDistanceView | [`MeshFilter BuildRingColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L286) | private |
| WorldDistanceView | [`void AddSkirt(List<Vector3> vertices, List<int> triangles, int a, int b)`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L309) | private |
| WorldDistanceView | [`void Enter(Camera view)`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L340) | internal |
| WorldDistanceView | [`void Dispose()`](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L367) | public |

## Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs

// 职责：呈现世界准备进度及取消入口；实际准备与模式切换由WorldSessionManager持有。 // 入口：Bind() / Show()；组件留在活动Canvas，隐藏仅改变CanvasGroup。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLoadingPanel | [`void Bind(WorldSessionManager owner)`](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L19) | public |
| WorldLoadingPanel | [`void Show(bool visible)`](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L39) | public |
| WorldLoadingPanel | [`void ShowStartup()`](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L63) | public |
| WorldLoadingPanel | [`void Update()`](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L69) | private |
| WorldLoadingPanel | [`void Cancel()`](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L96) | private |
| WorldLoadingPanel | [`void OnDestroy()`](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L101) | private |

## Assets/Scripts/Features/World/Presentation/WorldTerrainTransitionMesh.cs

// 职责：生成无碰撞体素过渡列，保留格级轮廓并合并同高顶面。 // 入口：WorldDistanceView 分帧调用 Build()；返回的 Mesh 由调用者释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldTerrainTransitionMesh | [`Mesh Build(ProceduralVoxelWorld world, WorldGenerationSettings settings, Vector2Int coordinate)`](../Assets/Scripts/Features/World/Presentation/WorldTerrainTransitionMesh.cs#L9) | internal static |
| WorldTerrainTransitionMesh | [`void AddRingWall( List<Vector3> points, List<int> faces, List<int> soilFaces, float cell, Vector3 first, Vector3 last, float bottom)`](../Assets/Scripts/Features/World/Presentation/WorldTerrainTransitionMesh.cs#L104) | private static |
| WorldTerrainTransitionMesh | [`void AddRingFace( List<Vector3> points, List<int> faces, Vector3 a, Vector3 b, Vector3 c, Vector3 d)`](../Assets/Scripts/Features/World/Presentation/WorldTerrainTransitionMesh.cs#L138) | private static |

## Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs

// 职责：按世界区块生成建筑，将远处建筑休眠为快照，并保留本次探索中的损坏和货仓。 // 入口：LoadColumn() / UnloadDistantRoutine()；实例归WorldBuildingManager，缓存随会话退出丢弃。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldBuildingStreaming | [`void LoadColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L34) | internal |
| WorldBuildingStreaming | [`IEnumerator RestoreAvailable()`](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L61) | internal |
| WorldBuildingStreaming | [`IEnumerator UnloadDistantRoutine(Vector2Int center)`](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L105) | internal |
| WorldBuildingStreaming | [`int Distance(Vector2Int a, Vector2Int b)`](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L146) | internal static |
| WorldBuildingStreaming | [`bool HasLoadedGround(Vector3 position, Quaternion rotation, BuildingFoundationData footprint)`](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L151) | private |

## Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs

// 职责：在地形碰撞尚未就绪时暂存载具姿态/速度，限制世界边界并在区块完成后恢复。 // 入口：Tick() / Release()；WorldSessionManager在FixedUpdate调用，退出或销毁必须释放。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldPlayerStreamingGuard | [`void Tick(Transform player)`](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L31) | internal |
| WorldPlayerStreamingGuard | [`bool HasTerrainForBounds(Transform player, Vector3 target, Vector3 prediction)`](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L99) | private |
| WorldPlayerStreamingGuard | [`Bounds CaptureLocalBounds(Transform construct)`](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L133) | internal static |
| WorldPlayerStreamingGuard | [`void Release()`](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L138) | internal |

## Assets/Scripts/Features/World/Streaming/WorldRegionStreaming.cs

// 职责：按距离选择下一个地形列，先休眠建筑再卸载远处网格，并在列完成后装入建筑。 // 入口：LoadNext()；由WorldSessionManager的可取消协程逐帧驱动，无模式/UI副作用。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldRegionStreaming | [`IEnumerator LoadNext(Vector2Int center)`](../Assets/Scripts/Features/World/Streaming/WorldRegionStreaming.cs#L32) | internal |
| WorldRegionStreaming | [`bool FindNextColumn(Vector2Int center, out Vector2Int coordinate)`](../Assets/Scripts/Features/World/Streaming/WorldRegionStreaming.cs#L100) | private |
| WorldRegionStreaming | [`IEnumerator RetireDistant(Vector2Int center)`](../Assets/Scripts/Features/World/Streaming/WorldRegionStreaming.cs#L126) | private |

## Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs

// 职责：拥有有限寿命的金属碎片刚体及单元BoxCollider，清理自身网格。 // 入口：Initialize()；碎片不是可建造Block，不进入玩家分组/供电/存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelDebrisPiece | [`void Initialize(List<Vector3Int> cells, float cellSize, Material[] materials, float lifetime)`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L11) | public |
| VoxelDebrisPiece | [`void Update()`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L49) | private |
| VoxelDebrisPiece | [`void OnDestroy()`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L57) | private |

## Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs

// 职责：破坏后按预算判定金属支撑，将少量孤立结构转为有界物理碎片。 // 入口：RequestCheck()由世界成功破坏调用，LateUpdate避开未刷新的静态碰撞。 // 约束：仅土/岩邻接支撑，小片段研究模型，不等于全地形结构力学求解。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelStructureCollapse | [`void Awake()`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L28) | private |
| VoxelStructureCollapse | [`void RequestCheck()`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L33) | public |
| VoxelStructureCollapse | [`void LateUpdate()`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L38) | private |
| VoxelStructureCollapse | [`void ResolveDetached()`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L51) | public |
| VoxelStructureCollapse | [`void ClearDebris()`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L88) | public |
| VoxelStructureCollapse | [`void OnDisable()`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L104) | private |

## Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureSupport.cs

// 职责：按六邻接找金属连通结构，接触土/岩视为有支撑；只返回有限尺寸的无支撑片段。 // 入口：FindDetached()；只读取体素，不将土/岩地形转成动态刚体。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelStructureSupport | [`List<List<Vector3Int>> FindDetached(VoxelWorldData data, int maximumCells, int maximumGroups)`](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureSupport.cs#L17) | public static |

## Assets/Scripts/Features/World/Terrain/FloatingIslandTerrain.cs

// 职责：生成有空气边界、平整中心与收尖岩底的有限浮空岛体素数据。 // 入口：Generate()；共享World体素与材质编号，不创建场景对象或修改随机全局状态。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| FloatingIslandTerrain | [`VoxelWorldData Generate(Vector3Int size, int seed, int surface, Vector2 flatHalfSize, int depth)`](../Assets/Scripts/Features/World/Terrain/FloatingIslandTerrain.cs#L8) | public static |

## Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs

// 职责：保存不可变邻域快照和纯CPU网格结果，将工作线程计算与Unity对象写入分开。 // 入口：Capture()在主线程读取体素；Upload()在主线程创建Mesh，快照不引用可变世界。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelMeshData | [`Mesh Upload(Vector3Int start)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L18) | public |
| VoxelChunkSnapshot | [`VoxelChunkSnapshot Capture(VoxelWorldData data, Vector3Int start, int size)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L56) | public static |
| VoxelChunkSnapshot | [`byte Get(Vector3Int cell)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L73) | internal |
| VoxelChunkSnapshot | [`int Index(Vector3Int cell)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L78) | internal |
| VoxelChunkSnapshot | [`int Step(Vector3Int direction)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L83) | internal |
| VoxelChunkSnapshot | [`byte Get(int index)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L88) | internal |

## Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs

// 职责：从一格邻域快照合并同材质的暴露面，生成三材质贪心网格。 // 入口：Build()用于同步编辑；Generate()只计算数组，可在工作线程运行，Unity提交留在主线程。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelSurfaceMesher | [`Mesh Build(VoxelWorldData data, Vector3Int start, int chunkSize, float cellSize)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L61) | public static |
| VoxelSurfaceMesher | [`VoxelMeshData Generate(VoxelChunkSnapshot snapshot, float cellSize)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L67) | public static |
| VoxelSurfaceMesher | [`void MergeSlice( VoxelMeshData result, byte[] mask, int size, int face, Vector3Int origin, Vector3Int u, Vector3Int v, float scale)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L113) | private static |
| VoxelSurfaceMesher | [`void AddQuad( VoxelMeshData mesh, byte material, Vector3 corner, Vector3 u, Vector3 v, Vector3 normal, int width, int height, float scale)`](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L180) | private static |

## Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs

// 职责：拥有有限或流式体素数据、分块网格与受影响块的重建队列。 // 入口：Generate()保留WorldLab；InitializeStreaming/LoadColumn供游玩会话逐帧准备地形。 // 约束：根Transform保持单位缩放；运行修改属于本次会话，不写玩家蓝图存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| ProceduralVoxelWorld | [`float SurfaceHeight(int x, int z)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L53) | public |
| ProceduralVoxelWorld | [`Vector3 FindSpawnSurface(Bounds construct)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L54) | public |
| ProceduralVoxelWorld | [`void Awake()`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L82) | private |
| ProceduralVoxelWorld | [`void Generate(System.Action<GameObject> registerCreated = null, System.Action<GameObject> destroyObsolete = null)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101) | public |
| ProceduralVoxelWorld | [`void InitializeFinite(VoxelWorldData terrain, float cellWidth, int terrainSeed)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L124) | public |
| ProceduralVoxelWorld | [`void RebuildFinite(Action<GameObject> registerCreated, Action<GameObject> destroyObsolete)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L151) | private |
| ProceduralVoxelWorld | [`void InitializeStreaming( WorldGenerationSettings settings, int sessionSeed, IReadOnlyList<BlockDataList> buildingTemplates = null)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L213) | public |
| ProceduralVoxelWorld | [`bool HasColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L238) | public |
| ProceduralVoxelWorld | [`bool IsColumnInBounds(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L243) | public |
| ProceduralVoxelWorld | [`Vector2Int ColumnAt(Vector3 worldPosition)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L248) | public |
| ProceduralVoxelWorld | [`bool TryGetBuildingSite(Vector2Int coordinate, out Vector3 worldPosition, out Quaternion rotation)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L254) | public |
| ProceduralVoxelWorld | [`IEnumerator LoadColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L269) | public |
| ProceduralVoxelWorld | [`void UnloadColumn(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L351) | public |
| ProceduralVoxelWorld | [`void ClearStreaming()`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L372) | public |
| ProceduralVoxelWorld | [`void DestroyChunk(VoxelWorldChunk chunk)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L392) | private |
| ProceduralVoxelWorld | [`void CacheChunks()`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L411) | private |
| ProceduralVoxelWorld | [`Vector3Int WorldToCell(Vector3 position)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L423) | public |
| ProceduralVoxelWorld | [`Vector3 CellCenter(Vector3Int cell)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L428) | public |
| ProceduralVoxelWorld | [`bool SetCell(Vector3Int cell, byte material)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L433) | public |
| ProceduralVoxelWorld | [`VoxelWorldEdits CaptureEdits()`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L446) | public |
| ProceduralVoxelWorld | [`void RestoreEdits(VoxelWorldEdits edits)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L456) | public |
| ProceduralVoxelWorld | [`int DamageSphere(Vector3 center, float radius, float energy)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L484) | public |
| ProceduralVoxelWorld | [`void QueueCell(Vector3Int cell)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543) | private |
| ProceduralVoxelWorld | [`void QueueChunk(Vector3Int cell)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L554) | private |
| ProceduralVoxelWorld | [`void LateUpdate()`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L571) | private |
| ProceduralVoxelWorld | [`void FlushDirty(int budget = int.MaxValue, float milliseconds = float.PositiveInfinity)`](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L576) | public |

## Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs

// 职责：按全局单元坐标确定性采样大世界地形、出生平台和建筑平台，无场景对象或全图数组。 // 入口：Sample() / TryGetBuildingSite()；高度缓存有界，区块卸载不改变同一种子的基线。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| StreamingWorldTerrain | [`float SurfaceHeight(int x, int z)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L26) | public |
| StreamingWorldTerrain | [`bool IsColumnInBounds(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L49) | public |
| StreamingWorldTerrain | [`byte Sample(Vector3Int cell)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L55) | public |
| StreamingWorldTerrain | [`bool TryGetBuildingSite(Vector2Int coordinate, out Vector3 localPosition, out Quaternion rotation)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L66) | public |
| StreamingWorldTerrain | [`bool HasBuilding(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L85) | private |
| StreamingWorldTerrain | [`int Height(int x, int z)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L100) | private |
| StreamingWorldTerrain | [`int PlateauHeight(int x, int z)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L139) | private |
| StreamingWorldTerrain | [`float RawHeight(int x, int z)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L144) | private |
| StreamingWorldTerrain | [`uint Hash(Vector2Int coordinate)`](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L151) | private |

## Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs

// 职责：持有一个分块的渲染/碰撞网格，替换时只清理本组件创建的临时Mesh。 // 入口：Rebuild()；烘焙资产引用不销毁，碰撞更新与渲染共用同一表面。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelWorldChunk | [`void Configure(Vector3Int cellStart)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L13) | public |
| VoxelWorldChunk | [`void Rebuild(VoxelWorldData data, int chunkSize, float cellSize, Material[] materials)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L18) | public |
| VoxelWorldChunk | [`void ApplyMesh(Mesh mesh, Material[] materials)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L23) | public |
| VoxelWorldChunk | [`void AdoptBakedMesh(Mesh asset)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L39) | public |
| VoxelWorldChunk | [`void OnDestroy()`](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L46) | private |
| VoxelWorldChunk | [`void ClearOwnedMesh()`](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L51) | public |
| VoxelWorldChunk | [`void ReleaseMesh()`](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L66) | private |

## Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs

// 职责：有限世界保存体素数组，大世界按需采样并仅保存本次会话的稀疏单元修改。 // 入口：Generate() / Set()；无场景或存档副作用，0为空气、1土、2岩、3金属。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| VoxelWorldData | [`byte Get(Vector3Int cell)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L32) | public |
| VoxelWorldData | [`bool Contains(Vector3Int cell)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L47) | public |
| VoxelWorldData | [`bool Set(Vector3Int cell, byte material)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L54) | public |
| VoxelWorldData | [`void Recount()`](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L88) | public |
| VoxelWorldData | [`void InitializeEmpty(Vector3Int dimensions)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L105) | public |
| VoxelWorldData | [`void Generate(Vector3Int dimensions, int seed)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L119) | public |
| VoxelWorldData | [`void InitializeStreaming(StreamingWorldTerrain terrain)`](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L177) | public |
| VoxelWorldData | [`uint Fingerprint()`](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L187) | public |

## Assets/Scripts/Features/World/Validation/EnemyFlightPlayerChecks.cs

// 职责：显式Player路线读取既有敌机蓝图，验证自然AI飞行、供电和对玩家伤害。 // 入口：Run()；不改蓝图参数、不保存文件，结束清理本次敌机。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| EnemyFlightPlayerChecks | [`IEnumerator Run(ControlUnit player, EnemySpawner spawner, string blueprintName, Action<bool, string> check)`](../Assets/Scripts/Features/World/Validation/EnemyFlightPlayerChecks.cs#L12) | internal static |
| EnemyFlightPlayerChecks | [`bool IsFinite(Vector3 value)`](../Assets/Scripts/Features/World/Validation/EnemyFlightPlayerChecks.cs#L76) | private static |

## Assets/Scripts/Features/World/Validation/OutpostEditorPlayerChecks.cs

// 职责：显式Development Player路线用隔离文件仓验证建筑编辑器快捷键、据点保存、完整模型与预览Shader。 // 入口：Run()；三个文件仓仅在测试期间反射替换，finally恢复，随后只读重载原玩家蓝图。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostEditorPlayerChecks | [`IEnumerator Run(Action<bool, string> check)`](../Assets/Scripts/Features/World/Validation/OutpostEditorPlayerChecks.cs#L17) | internal static |

## Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs

// 职责：显式Player烟测中验证真实据点部署、受损休眠恢复、驾驶舱击败与不重刷。 // 入口：Run()；只由-world-smoke-outposts启用，不写用户蓝图或永久进度。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostPlayerChecks | [`IEnumerator Run( WorldSessionManager session, ControlUnit player, Action<bool, string> check, Action<string> capture = null, bool salvage = false, bool recoverAllParts = false, bool movingTarget = false)`](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L12) | public static |
| OutpostPlayerChecks | [`IEnumerator Flatten(IEnumerator root)`](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L31) | private static |
| OutpostPlayerChecks | [`IEnumerator Scenario( WorldSessionManager session, ControlUnit player, Action<bool, string> check, Action<string> capture, bool salvage, bool recoverAllParts, bool movingTarget)`](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L63) | private static |
| OutpostPlayerChecks | [`IEnumerator TravelTo(Transform player, Vector3 destination)`](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L330) | private static |
| OutpostPlayerChecks | [`void Freeze(ControlUnit unit)`](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L344) | private static |
| GuardObservation | [`void Sample(ControlUnit player, Durability health, float initialHealth)`](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L376) | internal |
| GuardObservation | [`string Describe()`](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L389) | internal |
| OutpostPlayerChecks | [`IEnumerator TravelWithDefender(Transform player, ControlUnit unit, Vector3 destination)`](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L395) | private static |

## Assets/Scripts/Features/World/Validation/OutpostPursuitPlayerChecks.cs

// 职责：显式Player路线观察真实驻军追击移动目标、脱离索敌及重新接敌，不搬运或冻结敌机。 // 入口：Run()；玩家是2m/s移动的运动学测试目标，finally恢复其原位置，不改驻军参数。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostPursuitPlayerChecks | [`IEnumerator Run(ControlUnit player, IReadOnlyList<ControlUnit> defenders, Action<bool, string> check)`](../Assets/Scripts/Features/World/Validation/OutpostPursuitPlayerChecks.cs#L10) | internal static |
| Observation | [`void Sample(ControlUnit player, Durability health, float initialHealth)`](../Assets/Scripts/Features/World/Validation/OutpostPursuitPlayerChecks.cs#L100) | internal |
| Observation | [`string Describe()`](../Assets/Scripts/Features/World/Validation/OutpostPursuitPlayerChecks.cs#L112) | internal |

## Assets/Scripts/Features/World/Validation/OutpostSalvagePlayerChecks.cs

// 职责：显式Player路线验证正式据点货仓的原有内容掉落、吸附和机器人回收。 // 入口：Run()；仅-world-smoke-outpost-salvage启用，临时玩家模块在finally清理，不写用户数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| OutpostSalvagePlayerChecks | [`IEnumerator Run( BuildingRuntime building, ControlUnit player, Action<bool, string> check, bool recoverAllParts = false)`](../Assets/Scripts/Features/World/Validation/OutpostSalvagePlayerChecks.cs#L12) | internal static |
| OutpostSalvagePlayerChecks | [`GameObject Spawn(string resource, ControlUnit owner, Vector3 local, List<GameObject> fixtures)`](../Assets/Scripts/Features/World/Validation/OutpostSalvagePlayerChecks.cs#L170) | private static |

## Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs

// 职责：在显式Player烟测中验证输入推进、炮弹击毁与机器人回收入仓。 // 入口：Run()；只修改本进程临时夹具，迭代器释放时恢复输入设备与镜头。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldCombatPlayerChecks | [`IEnumerator Run(ControlUnit player, EnemySpawner spawner, Action<bool, string> check)`](../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L14) | internal static |
| WorldCombatPlayerChecks | [`GameObject Spawn(string resource, ControlUnit owner, Vector3 local, List<GameObject> fixtures)`](../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L135) | private static |
| WorldCombatPlayerChecks | [`BlockDataList EnemyCargoFixture()`](../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L144) | private static |

## Assets/Scripts/Features/World/Validation/WorldLab/WorldLabDebrisProbe.cs

// 职责：记录真实帧物理碎片下落，并在完成后重生成清理测试碎片。 // 入口：Begin()；2秒后比较真实位置，非手动Physics.Simulate。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabDebrisProbe | [`void Begin(VoxelStructureCollapse owner, ProceduralVoxelWorld source)`](../Assets/Scripts/Features/World/Validation/WorldLab/WorldLabDebrisProbe.cs#L13) | public |
| WorldLabDebrisProbe | [`void FixedUpdate()`](../Assets/Scripts/Features/World/Validation/WorldLab/WorldLabDebrisProbe.cs#L21) | private |

## Assets/Scripts/Features/World/Validation/WorldLab/WorldLabPhysicsProbe.cs

// 职责：实验场真实帧落体与体素地面接触探针，输出接触证据而不驱动Physics.Simulate。 // 入口：Begin()由WorldLabChecks调用；10秒超时失败，结束移除探针对象。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabPhysicsProbe | [`void Begin(string path)`](../Assets/Scripts/Features/World/Validation/WorldLab/WorldLabPhysicsProbe.cs#L11) | public |
| WorldLabPhysicsProbe | [`void FixedUpdate()`](../Assets/Scripts/Features/World/Validation/WorldLab/WorldLabPhysicsProbe.cs#L17) | private |
| WorldLabPhysicsProbe | [`void OnCollisionEnter(Collision collision)`](../Assets/Scripts/Features/World/Validation/WorldLab/WorldLabPhysicsProbe.cs#L26) | private |
| WorldLabPhysicsProbe | [`void Save(bool contact)`](../Assets/Scripts/Features/World/Validation/WorldLab/WorldLabPhysicsProbe.cs#L34) | private |

## Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs

// 职责：仅在显式-world-smoke-report参数下自动验证真实Player的加载、游玩、区块移动和退出。 // 入口：命令行初始化；只替换临时运行时载具，不保存游戏数据，报告后正常退出进程。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldPlayerSmokeChecks | [`void Initialize()`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L69) | private static |
| WorldPlayerSmokeChecks | [`int ReadInteger(string[] args, int index, string name, int minimum, int maximum)`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L136) | private static |
| WorldPlayerSmokeChecks | [`void Start()`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L146) | private |
| WorldPlayerSmokeChecks | [`void LateUpdate()`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L164) | private |
| WorldPlayerSmokeChecks | [`IEnumerator Scenario()`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L193) | private |
| WorldPlayerSmokeChecks | [`BlockDataList EnemyFixture()`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L467) | private static |
| WorldPlayerSmokeChecks | [`IEnumerator ValidateEnemyBlueprints(EnemySpawner spawner, Vector3 origin)`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L503) | private |
| WorldPlayerSmokeChecks | [`void Check(bool valid, string name)`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L563) | private |
| WorldPlayerSmokeChecks | [`void Next(string name)`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L576) | private |
| WorldPlayerSmokeChecks | [`void CaptureError(string message, string stack, LogType type)`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L589) | private |
| WorldPlayerSmokeChecks | [`void Save()`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L597) | private |
| WorldPlayerSmokeChecks | [`void Finish()`](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L602) | private |

## Assets/Scripts/Features/World/Validation/WorldReturnPlayerChecks.cs

// 职责：显式Player隔离文件仓，验证正常返航写盘、重载、死亡清货与再次出发。 // 入口：Run()；固定两模块夹具，仅写唯一缓存目录，finally恢复原文件仓及名称。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldReturnPlayerChecks | [`IEnumerator Run(Action<bool, string> check)`](../Assets/Scripts/Features/World/Validation/WorldReturnPlayerChecks.cs#L13) | internal static |

## Assets/Scripts/Features/World/Validation/WorldSessionValidationHost.cs

// 职责：为历史世界组件/性能套件显式装配旧生命周期夹具；正式Main不会调用，也不用于模式集成验收。 // 入口：Attach()；仅由Editor测试或带显式-world-smoke-report参数的Player调用，不保存场景和玩家数据。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionValidationHost | [`WorldSessionManager Attach()`](../Assets/Scripts/Features/World/Validation/WorldSessionValidationHost.cs#L10) | public static |

## Assets/Scripts/Features/World/WorldGenerationSettings.cs

// 职责：保存大世界种子、区块预算、建筑平台及安全出生区的可编辑生成参数。 // 入口：WorldSessionManager.BeginPreparation()读取；不会写入玩家或WorldLab存档。

数据/配置/接口定义，无方法体。

## Assets/Scripts/Features/World/WorldLab/WorldLabController.cs

// 职责：独立世界实验场的自由镜头、采掘/爆破/填补输入及性能读数。 // 入口：Update()读取Input System；不接管Main场景的玩家、蓝图与存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldLabController | [`void Awake()`](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L32) | private |
| WorldLabController | [`void OnEnable()`](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L41) | private |
| WorldLabController | [`void OnDisable()`](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L49) | private |
| WorldLabController | [`void Update()`](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L57) | private |
| WorldLabController | [`void MoveView(Keyboard keyboard, Mouse mouse)`](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L150) | private |
| WorldLabController | [`void ResetView()`](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L171) | private |
| WorldLabController | [`void TransferEdits(bool save)`](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L182) | private |
| WorldLabController | [`void OnGUI()`](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L222) | private |

## Assets/Scripts/Features/World/WorldSessionManager.cs

// 职责：准备并运行独立探索世界；Main通过模式入口加载，独立验证夹具仍可选择启动时预热。 // 入口：BeginPreparation() / RequestPlay() / EnterPlay() / EndSession()；不读写玩家或WorldLab世界存档。

| 类型 | 方法签名 | 可见性 |
| --- | --- | --- |
| WorldSessionManager | [`void BindHost(GameObject[] environment, WorldLoadingPanel panel, GameModeController owner)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L53) | public |
| WorldSessionManager | [`void Start()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L79) | private |
| WorldSessionManager | [`void OnEnable()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L90) | private |
| WorldSessionManager | [`void BeginPreparation()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L99) | public |
| WorldSessionManager | [`IEnumerator PrepareWorld()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L139) | private |
| WorldSessionManager | [`void RequestPlay()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L188) | public |
| WorldSessionManager | [`void CancelPlayRequest()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L205) | public |
| WorldSessionManager | [`void CompletePendingPlay()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L212) | private |
| WorldSessionManager | [`bool CanEnterPlay(Transform construct, out string reason)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L230) | public |
| WorldSessionManager | [`void EnterPlay(Transform construct)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L252) | public |
| WorldSessionManager | [`IEnumerator EnableWorld()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L284) | private |
| WorldSessionManager | [`void FinishEntering()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L318) | private |
| WorldSessionManager | [`void EndSession()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L327) | public |
| WorldSessionManager | [`IEnumerator RunGuarded(int version, IEnumerator work)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L339) | private |
| WorldSessionManager | [`IEnumerator StreamWorld()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L370) | private |
| WorldSessionManager | [`void FixedUpdate()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L418) | private |
| WorldSessionManager | [`void Update()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L443) | private |
| WorldSessionManager | [`Bounds ConstructBounds(Transform construct)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L458) | private static |
| WorldSessionManager | [`void SetBuildEnvironmentVisible(bool visible)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L463) | private |
| WorldSessionManager | [`void StopPreparation()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L493) | private |
| WorldSessionManager | [`void Fail(Exception error)`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L525) | private |
| WorldSessionManager | [`void OnDisable()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L540) | private |
| WorldSessionManager | [`void StopSession()`](../Assets/Scripts/Features/World/WorldSessionManager.cs#L545) | public |
