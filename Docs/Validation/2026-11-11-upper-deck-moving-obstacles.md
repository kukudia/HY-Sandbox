# 双层综合城区与动态障碍

场景：`Assets/Scenes/EnemyAvoidanceLab.unity`，保留十二组互斥布局和原有地面城区；扩建属于默认综合城区。

## 内容与编辑入口

- 高架桥面高度60米：东西平台、南北连桥、支柱、外围矮墙、错位挡板、四个部署坪，中央天井和平台外侧保留跨层飞行空间。
- 原六个地面起终点保留，新增四个高架点：Upper West South / North、Upper East South / North，分别位于 `(±88, 68, ±65)`。面板位置列表支持任意选择起终点。
- 四组橙色动态障碍：地面西侧横移闸门、天井东侧升降挡板、南侧高架摆动横梁、北侧高架穿行滑块。
- `EnemyLabMovingObstacle` 的 Travel 是世界坐标单侧位移，Period 是完整往返模拟秒数，Rotation Axis 是初始姿态的局部转轴，Swing Angle 是单侧摆角。默认周期分别为18、22、20、24秒；运动在 FixedUpdate 使用运动学刚体 MovePosition / MoveRotation，保留真实碰撞和现有导航过滤。
- 暂停同时冻结障碍；重试、重新部署、切换布局后返回均从初始相位开始。停下试验舰不会自动冻结场地，观察时可使用暂停。
- `Tools/HY-Sandbox/Combat/Add Upper Deck And Moving Obstacles` 保存真实场景对象，重复执行保留已存在的高架/动态分组与人工参数；不会清理用户对象。新建 Lab 也调用此扩建。默认透视相机抬高到 `(-155, 135, -175)`，便于同时观察两层。

生产入口：`EnemyAvoidanceLab.ResetTrial` → `EnemyLabMovingObstacle.ResetMotion`；Unity 独立调度障碍 FixedUpdate。制作入口：`EnemyLabExpansionAuthoring.Upgrade`。源码关联图见 [RuntimeFlow](../RuntimeFlow.md)，函数导航见 [CodeIndex](../CodeIndex.md)。

## 记录

`metadata.json` 的 schema 升为2，geometry 同时包含静态碰撞体与本场景运动障碍。动态项的 motionSettings 保存序列化运动参数，position / rotation 保存该轮初始姿态；没有添加播放/回放系统。导航、碰撞和操作记录沿用既有 timeline 与 operations。

## 验证

- Unity 6000.6.8f1 实际导入编译、保存重载；Edit 检查20项通过，覆盖重复扩建不增加对象、十个位置持久化、四个刚体/碰撞体配置、无丢失脚本及高架点中心净空/脚下桥面。
- `EnemyLabExpansionChecks.Begin()` Play 检查21项通过：四障碍实际物理运动、当前位置物理查询、暂停冻结、每轮位置/姿态复位、布局停用/再启用、记录参数、四个高架起终点可选。结果位于 `Temp/EnemyAvoidanceLab/expansion-{edit,play}-checks.txt`。
- 已查看抬高视角的实际 Camera 渲染，确认两层、连桥、天井、部署点和橙色障碍可见；预览为 `Temp/EnemyAvoidanceLab/upper-deck.png`。
- Finch 高架南侧西→东实飞：32.82秒到达，行程173.74米，障碍/地面/目标接触均为0；记录 `NavigationLabRecords/20261111-125251-362-ec36b3/trial-0026`。到达判定距目标10.41米，属于导航到达范围测试，没有验证精确停靠。Upper East North→East Dock短测33.9秒后手动停止，距目标25.4米、有2次障碍接触，未通过到达验收；其他跨层短测被操作切换中断，不记为通过。
- 自由视角完整输入回归未通过：`EnemyLabViewChecks` 在“Right mouse captures free look”失败，当时 `Application.isFocused` 为false，Editor焦点命令后仍为false；本轮不宣称相机输入回归通过。期间一次脚本重新编译清空静态Pending，停止Play后重启扩建检查并取得上述21项通过结果。CLI曾报告5000ms主线程超时，但已单独读取实际检查结果，未用超时响应当作成功证据。
- 可读性检查：430份脚本，0格式差异、0错误；`dotnet build HY-Sandbox.sln --no-restore`：0错误、37条警告（框架依赖及旧API）；`git diff --check` 通过。

以上是场地结构和动态生命周期验收，不代表全部尺寸蓝图、全部跨层组合均可通过；没有修改生产AI算法、寻路预算或精确停靠行为。
