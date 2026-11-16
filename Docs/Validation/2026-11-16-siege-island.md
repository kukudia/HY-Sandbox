# Siege浮空岛 · 2026-11-16

两套宿主与三个压力场保存SiegeIsland/ProceduralVoxelWorld真实组件与共用设置。旧Arena Ground删除。正式场景已烘焙72个真实网格资产，Editor重开可见；参数可在Assets/Siege/FloatingIsland.asset编辑，再Preview/Bake。运行部署根据本次基地占地重新生成一次，正常战斗不扩图/补坑。默认192m最小跨度、2m单元、40m厚，数据上限128×32×128；大基地按需增大单元。脚本FloatingIslandTerrain只生成数据，网格、碰撞与受损走World原管线。

## 已实际验证

- Unity6000.6.8f1重编译0错误。
- RunData7项：种子确定性/变化、四边与下方空气、平整中心、真实土/岩材料、有限密度。
- RunLive11项：从主菜单部署393块官方示例；真实地形/无旧地板/100%基地支撑；岛外向下无隐藏碰撞；恢复边界与16方向入口；DestroyManager爆炸移除体素，FlushDirty后PhysX射线命中降低超过2m，远处基地保持锚定。
- RunBoundary6项：通过真实舰队部署，将测试舰/敌舰/核心移动到界外后调用生产边界消费者；友舰Scrap、敌舰释放波次名额且击杀数不变、真实宿主失败。属于边界注入验证，不是自然坠落轨迹实测。
- SiegeHangarHostChecks：新版41、宽基地42、预研三库37项通过（以Temp/Hangars报告实际计数为准）；入库/返航/结算/付费维修保持。
- WorldLabChecks.RunPlayChecks16项通过；最后world.Generate走重构后的共用重建，实际刚体180固定步接触地形为true。新物理证据复制到Temp/SiegeIsland/worldlab-physics.json，旧历史报告保留。
- 实际Game Camera截图Temp/SiegeIsland/island-game.png已查看：有限起伏边缘、悬空岩层、中心基地清晰可见。无需外部美术资产，土/岩/金属材质复用Assets/WorldLab。
- BakeCurrent首次调用超出CLI5秒主线程响应窗口；操作仍完成，后续明确核验72个Mesh资产、IsGenerated=true、场景已保存，未盲目重跑。

## 限制

尚未Player构建、长期大规模战斗/自然跌落轨迹与多机库性能实测。没有整座土岩岛屿的结构力学坍塌；沿用World土岩静态体素，只建筑支撑会响应挖空。地形损坏仅本局，不回写个人基地设计。当前天空/地表为已有WorldLab表现，未声称完成专用浮岛美术。

## 阶段检查

- dotnet build：0错误，37项既有过时API警告；格式检查470脚本、0差异、0错误。
- 主菜单→Siege后台回归：进入前runInBackground=false，部署后true，后台采样帧8344→8895、游戏时间139.62→148.81；ReturnToMenu后恢复false且Stage已卸载。退出Play后确认stopped。

后续进展（同日）：完整Windows Player构建、实际部署及短时自然交战已覆盖；发现并修复非凸网格最近点API调用，见[Player验证](2026-11-16-siege-gameplay-player.md)。长期、大规模、多库和自然跌落轨迹仍未验收。
