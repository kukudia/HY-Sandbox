// 职责：维护战斗总时限、返航总时限和逐舰位移停滞窗口，不结算经济或操作界面。
// 入口：BeginBattle() / Tick()；使用宿主传入的模拟时间增量，暂停不计时。
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SiegePhaseClock
{
    [SerializeField, Min(1f)]
    private float battleSeconds = 180f;
    [SerializeField, Min(1f)]
    private float returnSeconds = 90f;
    [SerializeField, Min(1f)]
    private float stalledSeconds = 15f;
    [SerializeField, Min(.1f)]
    private float significantMovement = 2f;
    private sealed class Progress
    {
        public Vector3 anchor;
        public float idle;
    }

    private readonly Dictionary<SiegeFleetEntry, Progress> progress = new Dictionary<SiegeFleetEntry, Progress>();
    private SiegePhase observed;
    public float Remaining
    {
        get; private set;
    }
    public float PreparationRemaining
    {
        get; private set;
    }
    public float StallRemaining
    {
        get; private set;
    }
    public bool Active => observed == SiegePhase.Battle || observed == SiegePhase.Returning;
    public string Label => PreparationRemaining > 0f ? "INCOMING" : observed == SiegePhase.Returning ? "RECOVERY" : "WAVE LIMIT";

    public void BeginBattle(float preparation)
    {
        observed = SiegePhase.Battle;
        PreparationRemaining = preparation;
        Remaining = battleSeconds;
        StallRemaining = stalledSeconds;
        progress.Clear();
    }

    public string Tick(SiegePhase phase, float delta, SiegeFleet fleet, SiegeReturnFlight guidance)
    {
        delta = Mathf.Max(0f, delta);
        if (phase != observed)
        {
            observed = phase;
            Remaining = phase == SiegePhase.Returning ? returnSeconds : 0f;
            PreparationRemaining = 0f;
            progress.Clear();
        }

        if (!Active)
        {
            return null;
        }

        float preparationStep = Mathf.Min(delta, PreparationRemaining);
        PreparationRemaining -= preparationStep;
        Remaining = Mathf.Max(0f, Remaining - (delta - preparationStep));
        if (Remaining <= 0f)
        {
            return phase == SiegePhase.Battle ? "Wave time limit exceeded" : "Recovery time limit exceeded";
        }

        StallRemaining = stalledSeconds;
        if (phase != SiegePhase.Returning || fleet == null || guidance == null)
        {
            return null;
        }

        foreach (SiegeFleetEntry ship in fleet.Ships)
        {
            if (ship == null || !ship.IsAlive || !guidance.IsGuiding(ship))
            {
                continue;
            }

            Vector3 position = ship.Cockpit.transform.position;
            if (!progress.TryGetValue(ship, out Progress sample))
            {
                sample = new Progress
                {
                    anchor = position
                };
                progress.Add(ship, sample);
            }

            if ((position - sample.anchor).sqrMagnitude >= significantMovement * significantMovement)
            {
                sample.anchor = position;
                sample.idle = 0f;
            }
            else
            {
                sample.idle += delta;
            }

            StallRemaining = Mathf.Min(StallRemaining, Mathf.Max(0f, stalledSeconds - sample.idle));
            if (sample.idle >= stalledSeconds)
            {
                return "Recovery stalled: " + ship.DisplayName;
            }
        }

        return null;
    }
}
