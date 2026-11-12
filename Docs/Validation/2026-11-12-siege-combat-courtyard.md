# SiegeGameplay 交错防线基地

## 已保存内容

基地：`Assets/Siege/Construction/CombatCourtyard.json`，141模块，自动占地106×90米，最高18米。四角炮塔、中央双炮核心舱，共6炮、6发电机、6中继；两个28×12×30米机库，净空26×11×28米，门朝南；分段围墙和高低错位内墙保留多条接近路线。

个人蓝图 `Combat Courtyard` 已写入 `Application.persistentDataPath/BaseBlueprints/`，可从 Siege 或 Base Workshop 选择，战斗损伤不回写设计。没有修改默认StarterHangar、SiegeGameplay场景或既有个人作品；本轮初始失败草稿的个人副本有独立时间戳备份及原路径/SHA256清单，位于本地Temp/Siege。

制作入口：`Tools/HY-Sandbox/Siege/Generate combat courtyard base` / `SiegeCombatBaseAuthoring.Generate()`。固定布局配方保存在Editor源码，已有蓝图和Prefab存在时只读取，不覆盖后续手动调整。主要尺寸和坐标在配方中，生成后可在基地工坊编辑模块。

结构件为 `Assets/Resources/Blocks/CombatStructures/` 的10个真实Prefab。来源为项目既有2×2×2装甲的视觉和材质，按目标尺寸烘焙Model缩放、BoxCollider与可生成连接点，移除对原2米构件CSV行的绑定；保留原有游戏风格。未制作或声称新增AI模型，不引入外部素材及许可。规格：米、Y向上、中心Pivot、基本立方体碰撞；无新增纹理、骨骼或音频需求。模型、材质引用和生成脚本均可编辑。

## 实际验证

- Unity 6000.6.8f1连接Editor导入、加载；通过 MainMenu → Main → Additive SiegeGameplay 正常选择个人蓝图部署，未孤立启动副场景。
- 部署后完整支撑检查通过：141模块、6炮、6发电机、6中继、2机库。原固定装甲直接改尺寸曾造成连接不匹配；交付版使用独立烘焙结构件。机库直接锚定地面，平台绕开机库占地。
- 显式供电刺激检查：临时让6炮同时请求工作，使用生产PowerNetwork重建，6/6供电、效率均1.00；随后撤销请求。此检查证明供电覆盖和负载预算，不等同于自然战斗中6炮均找到目标或开火。
- Spaceship和Swordfish两个现有个人载具均通过尺寸/物理净空和金币检查，分别部署成功，余额6000→5070。选择敌方 CODEX_E01_Finch，正常启动第1波，2名敌人实际生成并接近/攻击基地。
- 首波观察中核心存活，基地从141减少到134模块，后续观察剩1名敌人；没有等到完整清波/返航结算。两艘守军未正常完成出库，停留在门内/附近低位，需要后续专项排查；本轮不把尺寸适配与部署成功称为出库成功。
- 最后退出该战斗并重新通过菜单部署完整基地，恢复第0波整备，便于继续实战测试。没有把战损保存进个人蓝图。

实拍：`Temp/Siege/courtyard-preparation.png`、`courtyard-battle.png`；供电刺激结果 `courtyard-power-probe.txt`，首波快照 `courtyard-first-battle.json.txt`。临时证据留在本机、不提交生成日志。

## 验证范围

已完成可编辑资产保存、Editor导入、真实支撑/部署、供电刺激和短时首波观察。完整多波守卫、守军正常出入库、战斗自然清波及多舰性能仍未通过验收。基地主要用于进一步观察敌方寻路；不修改攻击半径、经济、武器参数或生产AI算法。

静态检查：`dotnet build HY-Sandbox.sln --no-restore --nologo` 0错误、36条已有警告；CodeReadability 438脚本、0错误；本任务文件差异检查通过。新脚本由Editor导入并纳入其生成的csproj后编译，生成的工程文件改动不提交。
