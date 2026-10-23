# 2026-10-23 阵地战真实输入链

独立Player新增显式 `-siege-checks-driving` 路线，与自然soak分开。只在该参数下添加Input System虚拟键鼠；通过真实Update、FixedUpdate、HUD指针事件测试，不把直接写movementInput当成键盘输入。结束、异常或验证器销毁会释放迭代器，并在finally移除设备、恢复原后台输入设置及当前真实设备。

正常388模块建筑基地、两舰发布Player87项通过；2048模块建筑基地、六舰发布Player91项通过。覆盖W/Q、松键、Space单次切换/再次恢复、真实鼠标点击Bay1、唯一驾驶权、旧舰恢复AI、D/E新舰运动、死亡自动接管及镜头重绑定、基地失守和场景重开。虚拟设备和后台设置恢复标志均为true。

六舰路线W/Q区间80帧、78帧观测到正确方向，实际水平位移约1.02m、推力峰3726；Q悬浮目标增加14.8m。HUD接管后D/E区间52帧、50帧方向正确，水平位移约5.64m、推力峰7200，E悬浮目标降低11.8m。数值是短时受控输入结果，不是人工驾驶体验评分、操纵精度或FPS证据。

首轮夹具错误地要求主推进器，所选Pike实际使用通用推进器，因此前提检查失败；保存该失败并将探针改为识别两种方向推进器，未为让测试通过修改飞船蓝图或生产控制逻辑。之后正常配置85项通过，再补设备恢复/死亡镜头检查，最终87/91项通过。

大基地路线进程23160于19:11:51.462启动、19:11:52.386恢复本次UnityWndClass窗口；实际驾驶PNG已查看，HUD显示Finch已接管、其余AI。托管errors为空，但大基地路线原生日志仍有1条D3D11 PresentFrame时序提示。该路线只有约11秒，不能代表长期交战稳定性。

两份发布构建0错误/1警告；dotnet静态编译0错误/4既有警告；Python工具9通过；306脚本/38图仅既有MainUIPanels格式差异；未执行完整人工驾驶。

[初轮夹具失败](2026-10-23-siege-driving-precondition-failed.json)、[正常配置最终](2026-10-23-siege-driving-normal.json)、[六舰配置](2026-10-23-siege-driving-large.json)、[驾驶画面](2026-10-23-siege-driving-large.png)。复现参数为 `-siege-checks-report <absolute.json> -siege-checks-ships 2 -siege-checks-driving`；大基地包可用6舰，额外capture参数保存画面。不要同时启用自然soak。
