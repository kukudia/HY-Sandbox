# 模块化机库验证 · 2026-11-16

逻辑机库默认外20×10×24m，净18×8×22m，三轴4–64偶数尺寸与2m步长。底、顶、左右墙、后墙使用现有2×2×1金属方块，共372块；前门保留可控滑门。战斗方块扁平注册于BuildingRuntime，预览层级隐藏，独立耐久/材料/拓扑/维修沿用生产系统。

机库展开前检查2048建筑模块上限，世界恢复/维修检查8192总量；逻辑报价为金属块材料费与面积装配费，展开快照不重复计墙费。旧奇数设计仅内存向上迁移并保留底部最小角，个人文件不自动重写。损伤快照通过hangarShellVersion保留缺口，hangarOwnerId跟随方块身份保存。旧损伤快照首次迁移补上原先不存在的模块墙并保留控制器耐久，此后读档不免费补墙。

官方StarterHangar重做为21个逻辑构件/393个部署方块：一座机库、独立核心塔、四座供电炮台。ModularBaseExampleAuthoring可复现配方，覆盖前在Library/ModularHangar保存唯一备份；不删除个人基地库。预研三库与压力测试蓝图保留。

## 实际验证

- Unity6000.6.8f1导入/重编译：0错误。
- HangarShellChecks真实Play15项通过：源数据不变、372墙块、材质/身份、幂等、预算拒绝、生产恢复、旧耐久保留、无重复战斗预览碰撞、全壳地面支撑、单块损毁、快照缺口、生产维修恢复。
- HangarAssetChecks：Edit22项、Play22项通过；含物理门开关、防夹与清障恢复。
- HangarWorkshopChecks真实输入/按钮53项通过：三轴2m步长、Undo/Redo、地面支撑、复制、六库限制、保存重开。
- SiegeHangarPlayChecks真实Play34项通过，覆盖完整出库/返航和失效分支。
- SiegeHangarHostChecks.RunStage主菜单真实新宿主41项通过：官方示例部署、机库卡片、舰队、Returning锁定、收益一次结算、付费维修、回菜单卸载。
- dotnet build HY-Sandbox.sln --no-restore：0错误，37个既有C#弃用警告；输出还含本机生成工程引用框架警告，不能当作Player构建。
- 真实Game Camera截图Temp/Hangars/modular-example.png已查看：金属铺块、机库门、核心与四炮台可见。工坊截图Temp/Hangars/workshop.png；测试JSON在同目录。
- 本轮旧测试曾因0.5m长板/旧净高预期失败，已改为实际1m墙块/2m步长契约并重跑通过。当前Console groundTruth为0错误，历史累计错误条目不代表本次全部已清除。

## 缺口

尚未Player构建、人工游玩与大规模连锁爆炸性能测试。六个默认机库加其他结构可能超过2048模块，会明确拒绝出战；可缩小机库。预览使用保存的嵌套视觉Prefab，战斗隐藏但仍有层级内存开销；后续性能测量再决定更轻的编辑专用预览。门目前仍朝+Z，可绕竖轴旋转；顶部/任意面门未实现。

后续进展（同日）：已增加真实Front/Roof开口和方向存档/工坊交互，见[方向验证](2026-11-16-hangar-entrance.md)。独立Player单库前/顶门闭环已通过，见[Player验证](2026-11-16-siege-gameplay-player.md)；任意面开口、拥挤多库和长期性能仍未覆盖。以上正文保留本阶段当时结果。
