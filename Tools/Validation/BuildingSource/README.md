# 建筑来源回归

通过连接的Unity Editor执行CSX片段；脚本用反射访问内部文件仓，不参与游戏程序集。原始结果写入Temp/BuildingSource。

在干净Main场景的Edit Mode执行：

```powershell
New-Item -ItemType Directory -Force Temp/BuildingSource | Out-Null
$testCode = Get-Content Tools/Validation/BuildingSource/editor-checks.csx -Raw
unity command eval --code $testCode --json
```

Edit检查创建并销毁临时对象，可能标记场景为脏；不要保存这些测试状态。检查使用独立临时目录并在finally恢复SaveManager引用，不改真实用户JSON。

真实世界检查：进入Main Play Mode，等待WorldSessionManager.State为Ready，然后执行：

```powershell
$testCode = Get-Content Tools/Validation/BuildingSource/play-checks.csx -Raw
unity command eval --code $testCode --json
```

Play脚本通过Editor更新回调等候两轮真实世界准备；最多120秒。它临时替换现有SaveManager的建筑文件仓，不替换玩家存档仓；完成后恢复原仓、结束会话并退出Play。结果见Temp/BuildingSource/play.json的passed/checks/errors。测试期间不编辑源码、不保存场景、不手动退出Play，以保证回调清理执行。

结束后重新打开磁盘上的Main，不保存临时状态。Player构建与人工视觉效果不在此检查范围内。
