# Main 大世界会话

Main 在建造模式下分帧准备出生区域的地形和真实 Buildings；世界根保持隐藏，建筑不进行物理、供电或战斗模拟。点击 Play 后，准备未完成时显示进度和 Cancel，完成后才显示世界、启用模拟并安全放置玩家。Cancel 只取消这次进入请求，后台准备继续。

退出游玩或死亡立即丢弃本次世界的地形修改、建筑、损坏和休眠缓存，恢复建造地面并准备新世界。关闭随机种子后，每次按固定种子重建，仍不保留变化。正常返回的既有货仓转存流程不变；世界数据不写入玩家蓝图或 WorldLab 存档。

## 可编辑入口

- `Assets/World/WorldGeneration.asset`：种子、随机种子开关、体素大小、区块大小、世界边界、加载/卸载半径、建筑概率、平台、安全出生区、Spawn Clearance和View Distance。
- `Assets/World/WorldSession.prefab`：WorldSessionManager 装配，WorldRoot 下分别持有 Streaming Terrain 和 Streaming Buildings。Main 实例绑定原有九片建造地面、PlayManager 和原生 UI 加载面板。
- Main读取 `Application.persistentDataPath/BuildingBlueprints` 下所有有效 `.json`。本机路径是 `C:/Users/12046/AppData/LocalLow/DefaultCompany/HY-Sandbox/BuildingBlueprints`，每次准备新会话重新读取；模板池包含全部有效文件，落点按种子选择模板与朝向。当前为base1、base2、default_building。坏文件单独警告并跳过，空目录仍可生成纯地形，不以内置Outpost替代；文件不被生成器修改。WorldLab仍使用独立Catalog和手工放置目录。
- 菜单 `Tools > HY-Sandbox > World > Install Main world session`：在已保存的 Main 中安装缺少的资产与绑定；已有配置、Prefab 和面板布局保留。安装后可在 Inspector 修改并保存。

## 生成与运行边界

默认体素 2m、立方区块每边 16 格、垂直 32 格，世界从出生列向两侧延伸各64列，总宽 4128m。加载半径4列（初始81列），卸载半径5列，保留一圈回滞区。附近维护可破坏网格与碰撞，远处另建无碰撞低密度地形；生成仍通过主线程协程逐帧执行。

出生周围默认3×3列不生成敌方建筑，保留自然高度和坡度，不再挖低成方形平台。进入前按玩家方块整体尺寸检查净空，并采样载具整个水平覆盖范围，底部放在最高地面上方Spawn Clearance（默认6m）。过大的载具拒绝进入，并提示扩大安全出生区。原有地面只在游玩期间隐藏。

建筑落点与平台由同一世界种子确定，加载完整地形列后生成真实模板，维持1m方块和2m体素的既有尺度。平台按全部蓝图的底座和模块角点自动扩大；较大平台增加候选位置间距并跨列采样，避免相互重叠。低于底座或半径超过128m的异常蓝图单独报告。平台只整平地形，不生成实体建筑地基。建筑数和方块数沿用WorldBuildingManager预算，超过活动预算的快照延期实例化。

默认View Distance为600m，距离雾从270m开始至600m完全遮盖；远景网格再多生成若干列，确保边界位于雾后。低密度高度网格分帧重建，分辨率有界，避开实际加载列并以垂直裙边衔接，避免重新填上近处已挖开的洞；远处建筑仍随实体区块加载。远景不参与射线/物理，玩家前方就绪保护仍只认真实区块碰撞。游玩时相机背景临时设为Distance Fog Color，退出恢复原雾颜色、参数、背景与远裁剪面；灯光不变。

同一次游玩内，区块卸载后再回来保留地形差量、建筑剩余模块、尺寸、耐久、货仓和锚定状态；动态主体同时暂存姿态与速度。已完全摧毁的建筑不会因重新加载而复活。脱锚建筑离开已加载地基覆盖范围时先休眠，地形就绪后恢复，避免在未加载碰撞上继续下落。缓存只活到本次会话结束；残骸、飞行中的机器人/炮弹与运输任务不属于休眠快照，远程临时对象沿用原有寿命清理。

FixedUpdate 检查玩家整体范围和下一物理步预测路径；缺少地形时暂存速度、冻结载具并加载前方，完整路径就绪后恢复。世界边缘限制玩家位置；极端速度限制为单步能容纳于加载窗口的距离。此保护针对玩家，尚不代表任意高速敌机的跨区域碰撞保证。

WorldLab 保留原来的有限数组、Bake 和 F5/F9 快照工作流。Main 流式世界拒绝调用 WorldLab 的全图快照接口，避免遍历或分配整个大世界。原有限范围的金属连通坍塌实验也不在 Main 全图运行。准备失败可通过再次 Play 重试；游玩中生成故障下一帧通过统一退出清理返回建造，该故障回退不进行正常返回的货物写盘。

## 验证

本次修正Main专项49项通过，包含真实用户目录全部模板、出生高度随种子变化、整体范围最高地面净空、远景网格范围/无碰撞、雾过渡与退出恢复；原23项边界检查再次通过。见[本次主流程](Validation/2026-10-15-world-session-followup.json)、[本次边界](Validation/2026-10-15-world-session-followup-edges.json)。[D3D11实际画面](Validation/2026-10-15-world-session-distance.png)检查了自然出生地与远处地形/雾；225份脚本格式/导航、静态编译（0错误/4个既有警告）、diff检查通过。未重新验证D3D12稳定性。

真实 Main Play Mode：42项通过，覆盖隐藏预热、Ready、真实建筑启用、安全出生碰撞、正负坐标流式有界加载、会话内修改恢复、退出/死亡清空、取消和重复请求，以及半完成列取消后的网格释放。另23项边界检查通过，覆盖81列高密度预算延期、4000m/s预测冻结和恢复后真实物理步、动态建筑无地形休眠恢复。报告见 `Validation/2026-10-15-world-session.json` 与 `Validation/2026-10-15-world-session-edges.json`。

WorldLab数据12项、建筑43项回归通过。已观察Main地形和建筑总览；继续细节截图期间Editor发生D3D12 GPU等待超时并退出。临时以D3D11重启后完成[加载面板](Validation/2026-10-15-world-session-loading.png)与[建筑近景](Validation/2026-10-15-world-session-building.png)检查，未修改项目图形API设置；D3D12稳定性仍未验收。

专项检查未执行故障注入；正式 Player 构建、长时大地图性能、最大尺寸载具及完整运输战斗压力尚未验收。验证菜单分别为 `Validate Main world session` 和 `Validate world session edges`，只操作运行时测试对象，报告写 Temp，不写玩家存档。
