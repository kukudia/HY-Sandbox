# 基地自由建造首版验收

日期：2026-11-08。Unity 6000.6.8f1，Windows Editor，URP / Input System 使用工程现有配置。

## 交付

- Main新增BASE EDITOR入口；复用建筑编辑、尺寸/方块操作、命名文件仓与自动保存。COPY STARTER生成可编辑副本，MARK SELECTED CORE将一个已有耐久模块设为唯一核心。旧蓝图新增可选siegeCoreId，不迁移GUID，不自动猜测核心。
- SiegeGameplay保存专用部署/战斗uGUI、建筑管理器、独立照明、炮弹与GPU特效池。基地设计只读载入，模型预览不含玩法组件；整数位置、90度旋转、预算、尺寸、占格/净空、资源身份和实际连接图支撑共同限制部署。
- 设计预算默认20000、每体积格5，最大48×48、限高32m；局内维修金币初始6000。无发电、单电源、无炮塔提示风险，仍允许部署。
- 新旧入口共用SiegeBase、SiegeMatch、SiegeWaveDirector与BuildingRefit。基地有真实模块、供电、武器、断裂和核心毁伤；波间恢复原设计耐久及缺件，失败不扣费。主场景普通刷怪器按模式隔离。
- 失败/撤退显示局内结算，再返回Main同一原稿与镜头。原设计文件不捕获战斗损坏。旧Siege场景与HUD保留，旧UI复制工具排除新版基地工作坊入口。

制作：Tools/HY-Sandbox/Siege/Install base construction。参数资产位于Assets/Siege/Construction。恢复点：codex/checkpoint-base-building-20261108；原有脏文件另有带哈希清单备份，未暂存无关Prefab、渲染和用户设置。

## 实际验证

| 项目 | 结果与边界 |
| --- | --- |
| SiegeConstructionPlayChecks.Run | 46项通过、0错误。真实Main编辑/核心指定/尺寸保存、缺核心/重复ID/越界/重叠/无效旋转、悬空分量、旋转预览、真实部署、报价修缮、受阻/余额不足不扣费、缺件身份恢复、波次与真实敌机死亡、整备停模、核心死亡、撤退、两次返回与设计文件逐字不变 |
| GameModePlayChecks.Run | 38项通过、0错误。包含探索生成/副本/货仓/死亡与新Siege往返，原有模式生命周期没有退化 |
| BuildingEditorPlayProbe.Run | 73项通过、0错误。原建筑/据点编辑、蓝图兼容、尺寸UI和删除/加载取消生命周期回归，隔离文件仓不写玩家设计 |
| 旧SiegeBuildingChecks.Run | 20项通过。原位修缮、缺件身份、支撑、独立核心、舰船隔离、失败结算。测试使用运行时配置副本关闭纪录写入 |
| 旧Siege真实波次冒烟 | 第1波实际生成2个注册攻击者，待生成0；敌机均归Siege场景。未把此项当作旧玩法完整战斗验收 |
| 自然实战观察 | 新场景未辅助击毁的观察中基地由388模块受损到344，存活供电状态isWorking=true、efficiency=1。此观察用于确认真实敌机/伤害/供电参与，不证明自然胜率或平衡性；集成套件以显式销毁敌机加速结算 |
| 场景保存重载 | Main、Exploration、SiegeGameplay缺失脚本均为0，引用通过正式Play加载使用 |
| Windows Player脚本 | Development37、Release35个程序集编译通过；不是完整Player打包/运行 |
| 静态 | CodeReadability381脚本，格式差异0、错误0；dotnet build解决方案0错误、34警告（Unity API过时/引用框架等现有类别） |

证据：Temp/BaseConstruction/play-checks.json、Temp/GameModes/play-checks.json、Temp/Siege/building-checks.json；Player脚本输出Temp/BaseConstruction/PlayerDevelopment与PlayerRelease。截图来自Editor ScreenCapture，Temp/BaseConstruction/final-preview.png等保留当前Game View尺寸；本机开启Gizmos时可能包含调试线，不等同Player画面。

静态命令从工程外工作目录使用绝对路径执行，因工程锁定SDK9.0.318而本机为9.0.320；未修改global.json。导航源Tools/CodeReadability/generate_navigation.py及DEVELOPMENT、RuntimeFlow、CodeIndex同步。

## 未覆盖范围

首版不含科技树、生产链、战中扩建、多基地或损坏状态跨局存档。部署预算与维修币尚未进行难度/长期平衡。未验证完整发布包、实际Player键鼠全流程、长时内存/性能和全部旧压力场景。全工作区diff检查仍被Editor更新的既有UserSettings/Layouts/default-6000.dwlt尾空格阻挡；任务暂存内容单独检查，不提交该布局或其他无关设置。
