# 建造快捷栏 · 2026-11-16

真实Main场景保存BuildHotbar、8个槽位及拖拽预览Image；Resources/Building/BuildHotbar.asset保存全体Blocks目录路径、现有图标和默认配置。首四槽为金属结构；后四槽为Cockpit、UniversalThruster、PowerGeneratingUnit、Turret。用户个人UI配置不进入蓝图与Git，不新增背包数量/资源消耗规则。

## 已验证

- Unity6000.6.8f1重编译0错误。
- BuildHotbarChecks.RunLive真实Main Play 26项通过：8个槽位、真实目录、鼠标顶层命中、统计卡无重叠、材质/模块选择、未知资源/材质拒绝、目录drop、槽位swap、右键清空、InputSystem真实Digit2与Shift+5、启用输入框真实焦点与数字屏蔽、模态屏蔽、新视图恢复PlayerPrefs、损坏配置回退、战斗拒绝、恢复建造、材质下拉同步；InputSystem模拟鼠标完整按下→移动→释放经真实uGUI完成目录拖入，拖动图标启停正确。
- 截图Temp/BuildHotbar/hotbar.png人工查看；发现首版挡住统计卡后通过Editor API调整统计卡位置，最终图卡片和快捷栏分开，操作说明常显。
- 测试初次在Main默认蓝图加载期间提前断言，后改为等待生产加载完成。隐藏输入框被uGUI取消焦点，改为启用真实输入框并先断言EventSystem焦点。测试恢复原PlayerPrefs及输入设备，不写个人蓝图。

## 限制

拖放包含模拟设备完整鼠标轨迹，尚未人工硬件鼠标重复检查。截图在2560×1440；小屏幕适配和Player构建另验。当前显示为所选Prefab原始尺寸报价，不代表之后拖拽缩放后的整栋总价。机库可经Shift记录资源路径，无单独缩略图；模块多样材质未启用。

静态复查：dotnet build 0错误、36项既有过时API警告；格式475脚本0差异0错误；函数导航72图。

后续进展（同日）：独立Player从Siege返回工坊后实际检查8槽保留，见[Player验证](2026-11-16-siege-gameplay-player.md)。Player完整拖拽/偏好交互、小屏与人工硬件鼠标仍未覆盖。
