// 职责：集中保存用于性能对照录制的运行时开关。
// 生命周期：仅用于临时采样；完成对照后将开关恢复为 false，再进行行为验证。
public static class RuntimePerformanceSwitches
{
    // 关闭后敌舰跳过局部避障评估，仍保留目标更新和推进器物理控制。
    public static readonly bool DisableEnemyAvoidance = false;
}
