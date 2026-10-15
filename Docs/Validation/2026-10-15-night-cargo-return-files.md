# 返航文件故障保留当前会话（2026-10-15夜间）

## 问题与修复

修复前在Main Play实测：将当前名称临时设为不存在的唯一名字，文件exists=false时，真实CargoPersistence.SaveReturnCargo(true)仍返回true。MainUIPanels.PlayEnd把这个返回值当作成功，继续CompletePlayEnd并销毁本轮载具/世界，即使货物没有写回。该问题可在外部移动/删除当前命名存档后触发。

现在有名称却缺失文件时抛出FileNotFoundException并由既有保存失败边界返回false；UI拒绝清理，保留会话供恢复文件后重试。同时把GetSavePath移入try，使目录创建/路径解析的IO故障同样返回false，避免异常逃出按钮入口。没有选择存档时仍允许退出；没有新建空存档覆盖丢失数据。

## 实际验证

Unity6000.3.11f1连接Editor、Main真实Play，原载具恢复并经正式PlayStart进入WorldSessionState.Playing后运行 `CargoReturnFileChecks.Run()`。开启本次Play的runInBackground，NightDesk保持黑屏；未保存任何ProjectSettings改动。

测试用反射仅替换SaveManager私有BlueprintFileStore为唯一Temp子目录，保留实际Main会话/载具。四类故障均通过正式MainUIPanels.PlayEnd进入：命名文件不存在、截断JSON、FileShare.None独占锁、目录路径被普通文件占用。每次检查恰好一个预期保存失败错误，playMode仍true、blocksParent原对象不变、世界仍Playing。错误是故障注入预期记录，不能以Console出现红字就当回归失败，也不把它们当正常游玩无错误证明。

随后在隔离文件中验证生产SaveReturnCargo成功、备份原文、全部模块/几何不变、没有遗留tmp；坏JSON和被锁文件原文不变。共 [10项通过、errors为空](2026-10-15-night-cargo-return-files.json)。finally恢复文件仓、名称、缓存；退出测试Play，Main回到干净EditMode。

236份脚本可读性、25张源码函数图、Python工具9项通过；dotnet build 0错误/4个既有警告；git diff --check通过。28份用户JSON与起始SHA256相同，六份起始工程/配置/布局差异未提交。新增Editor脚本已实际导入并生成meta。

## 验证边界

这是Editor真实世界中的UI失败分支与同步磁盘原子替换检查；成功分支调用的是CargoPersistence，没有执行完整UI正常返航、载具重新加载、死亡重生和再次出发。未重新构建独立Player。本修复不宣称完成P1整个闭环，也不改变世界退出后丢弃变化的现有产品规则。
