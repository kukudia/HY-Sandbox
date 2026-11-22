// 职责：集中保存用于性能对照录制的运行时开关。
// 生命周期：仅用于临时采样；完成对照后将开关恢复为 false，再进行行为验证。
public static class RuntimePerformanceSwitches
{
    // 关闭后不执行地形变化触发的建筑支撑重算和脱锚判定。
    public const bool DisableBuildingSupportChecks = true;

    // 关闭后炮塔不索敌、不瞄准、不发射，也不持续请求工作功率。
    public const bool DisableTurretCombat = true;

    // 关闭后敌舰跳过局部避障评估，仍保留目标更新和推进器物理控制。
    public const bool DisableEnemyAvoidance = true;
}
