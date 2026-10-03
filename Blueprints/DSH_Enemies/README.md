# DSH 敌方蓝图（2026-10-04）

三份文件名带 `DSH_` 前缀的敌方蓝图，全部使用原装部件与原装数值，不修改 Prefab、AI、PID、
场景或全局设置。设计目标是**机动性强 + 悬浮稳定**：环形悬浮推进器左右对称环绕重心、水平
重心居中、升重比 2.8～3.9、动力输出留有 1.7～3 倍余量。

| 蓝图 | 定位 | 部件 | 质量 | 升重比 | 环形悬浮推进器 | 水平推力 | 炮塔 | 发电机 |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `DSH_E01_Skimmer` | 宽体轻型截击机 | 66 | 360 | 3.85 | 17 | 2000 N | 2 | 3 |
| `DSH_E02_Halberd` | 重型双体炮舰 | 129 | 696 | 3.05 | 26 | 4000 N | 4 | 3 |
| `DSH_E03_Talos` | 圆盘炮台 | 73 | 367 | 3.78 | 17 | 2300 N | 6 | 3 |

三份蓝图已安装到本机 `C:/Users/12046/AppData/LocalLow/DefaultCompany/HY-Sandbox/EnemyBlueprints`，
重新进入 Play Mode 后进入敌人池；仓库内 `Blueprints/DSH_Enemies` 保存同名副本，两者内容一致
（`Validate.cs` 会逐字节比对）。

## 三份设计的差异

三者共用同一套经过验证的构造规则，但轮廓、质量、火力和机动分配互不相同：

- **E01 Skimmer**：13×13 m 方板削去四角，17 个悬浮推进器沿板缘成环，中部 3 台发电机与
  1 个驾驶舱，2 座炮塔，10 个甲板下万向推进器。最轻、升重比最高，转向最快。
- **E02 Halberd**：21 m 长双体结构，两舷各 9 个悬浮推进器、首尾各 4 个，3 台发电机
  （其中两台在两舷），4 座炮塔，24 个甲板下 + 16 个甲板面万向推进器。质量接近另两艘
  的两倍，速度与升重比最低，但火力和冗余最高。
- **E03 Talos**：与 E01 同尺寸圆盘，17 个悬浮推进器成环，6 座炮塔（三对，沿中轴与两舷
  分布），12 个甲板下 + 12 个甲板面万向推进器。火力最密，体积最小。

## 构造规则（由原装连接点几何推导并验证）

这些规则不是约定，而是原装部件连接点实际能接上的唯一解，全部在
`design_model.py` 中以断言形式固定，并由 `Validate.cs` 在真实 Prefab 上复核：

1. **2 m 晶格**：2×2×2 部件占据 `[c-1, c+1]`，因此甲板与推进器中心必须落在**偶数**坐标；
   1×1×1 部件落在**半整数**坐标。
2. **悬浮环挂在甲板下方**：`HoverThrusterBig` 中心在 y = -2 时，其启用的 Up 连接点位于
   y = -1，正好是上方甲板块的 Down 连接点，所以推进器直接吊在甲板下。
3. **驾驶舱与发电机抬高一级**：它们位于 (x, 2, z)，也就是甲板块正上方一格；此时它们的
   Down 连接点与甲板块的 Up 连接点重合。若与甲板块同心则相差 1 m，永远接不上。
4. **1×1×1 设备吊在甲板下并翻转 180°**：甲板**顶面**被驾驶舱/发电机占据（它们占据
   y ∈ [1, 3]），唯一空余的安装面是甲板底面。翻转后其 Down 连接点朝上，在 y = -1 与甲板
   块的 Down 连接点相接。
5. **`HoverFlightController` 必须正立**：它把自己的 `transform.up` 当作机体上方向
   （`CalculateTiltAdjustment` / `ApplyRotationCorrection`）。翻转安装会让它把机体压成
   倒扣，实测表现为 180° 倾角并持续下坠。因此它是唯一以正立姿态装在甲板顶面的部件。
6. **万向推进器可以倒装**：`UniversalThruster` 每个 `FixedUpdate` 用
   `Quaternion.LookRotation(worldDir, up)` 把模型转向世界空间的移动方向，与自身旋转无关，
   所以倒吊安装照常工作。
7. **供电按距离而非连接**：`PowerTransmissionDevice` 以 10 m 方块距离连发电机和相邻继电器、
   以 5 m 方块距离给 `Power` 部件供电，并把该连通分量的总输出**均分**给分量内所有负载。
   因此继电器必须先布置——落在所有继电器 5 m 盒之外的部件永远不工作。

## 重新生成与检查

```powershell
# 由已验证的布局模型重写 C# 生成器（模型是唯一事实来源）
python Blueprints/DSH_Enemies/write_generator.py

# 生成三份 JSON 到仓库与运行目录
unity command eval_file --file Blueprints/DSH_Enemies/Generate.cs --project-path D:/git_projects/Unity/HY-Sandbox --json

# 在真实 Prefab 上校验几何、连接、供电与朝向
unity command eval_file --file Blueprints/DSH_Enemies/Validate.cs --project-path D:/git_projects/Unity/HY-Sandbox --timeout 180000 --json
```

生成器会拒绝覆盖内容不同的同名文件，因此手工改过的版本必须先备份或改名。生成前后可运行
`python Blueprints/DSH_Enemies/design_model.py` 做离线自检——它会一次性打印部件数、质量、
重心、升重比、水平推力、供电余量与连接性，任一不达标即失败。

## 校验结果

`Validate.cs` 使用游戏自身的规则逐项检查（资源路径、尺寸、唯一 ID、恰好一个驾驶舱、启用
连接点几何、连通分量、体积重叠、供电覆盖与均分、爬升余量、控制器朝向）：

| 蓝图 | 结果 | 部件 | 接合连接点 | 可达部件 | 每负载功率 |
| --- | --- | ---: | ---: | ---: | ---: |
| DSH_E01_Skimmer | 通过 | 66 | 236 | 66/66 | 300 |
| DSH_E02_Halberd | 通过 | 129 | 512 | 129/129 | 169 |
| DSH_E03_Talos | 通过 | 73 | 243 | 73/73 | 255 |

## 实际飞行验证

Unity 6000.3.11f1，Main 场景，真实 `EnemySpawner.SpawnBlockData` + 真实 `EnemyController`。
炮塔关闭以隔离飞行因素，质量、推力、功率与 PID 均未改动。90 秒覆盖无目标悬停、15 秒获取
目标、45 秒目标换向、75 秒失去目标，每秒采样 5 次。原始数据见 `Flight.csv`，汇总脚本
`summarize.py`，机器可读结果 `Flight-summary.json`。

| 蓝图 | 峰值水平速度 | 最大倾角 | 收敛到 <1 m | 悬停高度误差 | 悬停垂直速度 |
| --- | ---: | ---: | ---: | ---: | ---: |
| DSH_E01_Skimmer | 2.76 m/s | 1.36° | 3.4 s | 0.476 m | 0.113 m/s |
| DSH_E02_Halberd | 2.84 m/s | 1.75° | 17.8 s | 0.742 m | 0.021 m/s |
| DSH_E03_Talos | 3.12 m/s | 2.01° | 3.4 s | 1.202 m | 0.099 m/s |

三艘均单连通（`BlockGroupManager.GroupBlocks` 返回 1 组）、无掉块、无失电导致的失稳。

### 两点需要明确记录

1. **E02 收敛更慢是原装 PID 的性质，不是机体缺陷。** `HoverFlightController` 的高度积分
   增益是固定 0.1，**不随质量缩放**（只有比例项通过 `currentHeightP = mass * 0.5` 缩放）。
   因此 696 kg 的 E02 比 360 kg 的 E01 需要更长时间把残余偏差拉回来，且 15 秒的设定点阶跃
   会再激励它一次。它最终稳定在 0.74 m 内、垂直速度 0.02 m/s。
2. **`Flight.csv` 中的 `unpowered` 列存在采样假象。** `PowerTransmissionDevice.Update`
   在同一帧内先 `ResetPower` 再重新分配，而探针从 `EditorApplication.update` 采样，二者
   不同步，因此偶尔会读到转瞬即逝的 0；实测被标记的采样点其倾角与高度误差与未标记点**相同
   或更好**，说明并未真正失去升力。`summarize.py` 因此把该列作为参考信息而非判据，并且
   只在同一读数连续多帧出现时才认定为真实断电。

## 目录内容

| 文件 | 说明 |
| --- | --- |
| `design_model.py` | 布局模型与离线自检：晶格、连接点、质量、升重比、供电分量的唯一事实来源 |
| `write_generator.py` | 由模型生成 `Generate.cs` |
| `Generate.cs` | 在 Unity 内生成三份 JSON（仓库 + 运行目录） |
| `Validate.cs` | 用真实 Prefab 校验几何、连接、供电与控制器朝向 |
| `FlightProbe.cs` | Main Play Mode 的 90 秒实飞探针 |
| `summarize.py` | 汇总 `Flight.csv`，输出收敛与悬停指标 |
| `power_check.py` | 在生成的 JSON 上复算继电器连通分量与均分功率 |
| `DSH_E0*.json` | 三份蓝图（与运行目录副本一致） |
| `Flight.csv` / `Flight-summary.json` / `Flight.completed` | 原始采样与结果 |

未验证：受损结构与密集交火下的表现、与其他敌机的拥挤交互、正式构建下的性能。
