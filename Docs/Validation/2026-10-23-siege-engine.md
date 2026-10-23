# 2026-10-23 独立引擎计数能力校准

## 实现

`SiegeEngineCounters` 仅随显式profile诊断创建，发现真实句柄、校验单位，记录available、observations、samples、累计值、最大值与缓冲覆盖。每次读取已完成样本后清空缓冲，不重复累加旧GC；空桶和缺失返回-1。采样为引擎默认线程覆盖，worker合计不是主线程墙钟，嵌套/重叠标记不能相加。完成、异常或销毁释放记录器，恢复原metrics开关。报告新增development和profilerEnabledAtStartup。

菜单 `Build Windows 2048-Module Profiling Player` 保存独立Development包到Builds/SiegeLargeProfiling，同一压力场景，不覆盖发布验证包或默认BuildSettings；不使用Deep Profiling或自动连接。

## 能力探针和停止条件

1. 普通发布包30秒：物理/VFX/渲染标记大部分剥离，Main Thread句柄有效但只有空桶。初始实现把空桶计为observations，已修正，归档保留samples/value揭示该问题。
2. Development、current-thread过滤30秒：VFX/GC读到实际样本，Physics/Main Thread/RenderLoop没有有效样本。与Editor检查同期，不能作为性能比较。
3. 同过滤加-profiler-enable：仍无物理/Main Thread样本；没有改善，不继续重复该路线。
4. 去掉线程过滤，30秒：RenderLoop.Draw读到跨记录线程的实际样本；物理/Main Thread仍只有空桶。报告development=true、profilerEnabledAtStartup=false。**停止此ProfilerRecorder物理归因路线**，不把Valid=true当成实际测量可用。

最终窗口约32秒、70项玩法检查通过、托管errors为空。VFX.Update最大2.43ms，VFX.ProcessCamera最大2.15ms，RenderLoop.Draw最大7.02ms，GC.Collect最大1.32ms。这些是各自独立最大值，不能相加，不能解释142ms最大delta，更不是GPU耗时。此次仅能力校准，不是自然多波、性能提升或发布基准。

## 原生图形诊断修正

最终原生PNG已查看，游戏正常绘制，但Development控制台显示 `GfxDeviceD3D11Base::PresentFrame() was called without calling ...WaitForLastPresentationAndGetTimestamp() first. This will result in increased input latency.` 最终窗口日志3条；另外三轮分别3/1/1条。**它们没有进入Application.logMessageReceived托管errors数组。** 因此JSON passed只证明受控托管契约，不能证明原生日志无诊断或性能窗口完全干净。原生提示摘录与原始JSON压缩归档保留。

回查上一轮survey自然窗口发现同类5条，priority可见自然窗口11条，已补充原有报告限制；不因截图渲染正常而忽略。没有证明原因与窗口恢复、NightDesk、计时器或游戏生产代码相关，未擅自修改图形项目配置。

## 最终验证

- 新鲜Siege真实Play 16项通过，包括真实分类、能力表9项、同线程GC缓冲读后清空、开关恢复与有界慢帧。首次Play CLI短超时后先核验场景，重新在就绪会话运行；最终看落盘16项报告。
- 最终发布版0errors/1warning构建，Hidden smoke75项通过、托管errors为空、原生日志没有上述呈现诊断。仅闭环检查，不作渲染或帧率基准。
- Development构建0errors/1warning；静态构建0errors/4既有warnings；38图/300脚本可读性只既有MainUIPanels差异；Python工具9通过；diff检查通过。

[Play](2026-10-23-siege-engine-play.json)、[最终smoke](2026-10-23-siege-engine-smoke.json)、[四轮能力及原始JSON压缩清单](2026-10-23-siege-engine-calibrations.json)、[部署](2026-10-23-siege-engine-deployment.png)、[终态含原生控制台](2026-10-23-siege-engine-final.png)。下一步需要更完整轨迹或小型可重复生产夹具，不能用这批计数作物理归因。
