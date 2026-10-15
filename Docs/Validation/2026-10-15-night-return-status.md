# 返航保存失败的可见反馈（2026-10-15夜间）

此前缺失存档保护虽保留会话，按钮失败只有Console错误，Player不知道为什么仍留在世界。Main现在有Inspector可编辑的Return Status栏，持续显示未写回货物、会话保留与重试办法。缺失文件提示恢复，坏JSON提示恢复有效副本，其他写入失败提示检查空间/访问/文件锁；不在产品标签中暴露堆栈或机器路径。

## 实现与编辑入口

MainUIPanels的序列化ReturnStatus绑定真实MainCanvas/Return Status对象。ReturnStatusPanel只控制标签与CanvasGroup；背景和Text关闭raycastTarget，CanvasGroup关闭interactable/blocksRaycasts，不禁用世界输入。成功CompletePlayEnd清除提示，失败持续保留以便处理后重试。没有自动关闭或覆盖文件。

CargoPersistence保留原bool方法，新增out failureReason重载供UI使用；错误日志仍保留诊断信息。菜单Tools/HY-Sandbox/Setup Return Status可安装/修复绑定，已有面板位置、字体、样式不重设。新面板默认顶部中央760×104，Inspector可调整；安装只新增对象和MainUIPanels引用，没有重新布局旧面板。

## 实际验证

- 连接Unity6000.3.11f1 Editor导入脚本/meta，保存并重载Main检查绑定。Editor保存时产生的45块自动布局/滚动条序列化变化已核对并排除；场景提交只有185行新增对象/引用，保留既有布局与Camera碰撞关闭配置。
- 真实Main世界游玩中，唯一Temp文件仓隔离缺失、坏JSON、独占锁与目录被文件占用故障，通过正式PlayEnd显示提示并保留同一会话/载具。提示含retry Return及session仍开放说明，非阻挡/显隐与原子保存/几何检查共 [16项通过](2026-10-15-night-return-status.json)。
- [实际Game画面](2026-10-15-night-return-status.png)在725×488显示最长通用错误说明，文字preferredHeight72.67、标签可用84逻辑高度；提示不覆盖右侧推进器窗和底部操作信息。该画面是显式显示提示的视觉检查，不是一次真实磁盘满故障截图。其他分辨率未逐一视觉验收。
- 新Windows非Development构建0错误/1警告；独立D3D11/1280×720/seed14721 `-world-smoke-return` 一轮 [29类检查通过、errors为空](2026-10-15-night-return-status-player.json)，包含成功UI返航清除旧提示，随后重载、舱毁重生清货与再次出发；进程正常退出。文件故障提示仍由Editor真实Play覆盖，没有在Player中注入这些预期错误。
- 239份源码格式检查、26张源码函数图、Python工具9项、dotnet build 0错误/4个既有警告及diff通过。28份用户JSON起始SHA256不变，六份起始工程/配置/布局差异不提交。

## 失败尝试与边界

清理Editor自动布局序列化后出现Main外部修改重载对话框，导致CLI调用超时；已核对仅本任务Main变化，通过准确Reload按钮恢复，没有关闭用户Unity。随后一次Play观察到管理器静态实例为空并出现运行错误，已立即停止该测试；在无后续脚本写入/编译的条件下重新进入Play，管理器有效，完成上述16项和新Player29项。该无效Play不计为通过，根因未作独立生产复现。

开发期间NightDesk发生接管与用户显式恢复，未用循环重遮罩。截图技能的系统检查图仅保存在本机临时目录，交付画面来自Unity ScreenCapture。Main最终为干净EditMode，无脚本编译失败。没有把本次提示或短时路线宣称完整P1动态巡航/长期压力验收。
