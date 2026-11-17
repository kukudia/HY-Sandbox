// 职责：识别受损后仅余无推进、无武器驾驶舱且持续停滞的来袭者，防止占住波次名额。
// 入口：ShouldRetire()；只提供退场判断，不产生击杀、奖励、伤害或凭空推力。
using System.Collections.Generic;
using UnityEngine;

public sealed class SiegeStrandedCockpit
{
    private readonly bool hadHull;
    private readonly List<Block> blocks = new List<Block>();
    private ControlUnit owner;
    private float nextScan;
    private float strandedSince = -1f;
    private Vector3 anchor;
    public SiegeStrandedCockpit(bool hadHull)
    {
        this.hadHull = hadHull;
    }

    public bool ShouldRetire(Cockpit cockpit, float time, float timeout)
    {
        if (!hadHull || timeout <= 0f || cockpit == null || time < nextScan)
        {
            return false;
        }

        nextScan = time + .5f;
        ControlUnit unit = cockpit.GetComponentInParent<ControlUnit>();
        if (unit == null || unit.Building != null || unit.faction != UnitFaction.Enemy || !unit.HasValidCockpit || unit.cockpit != cockpit)
        {
            strandedSince = -1f;
            return false;
        }

        unit.GetComponentsInChildren(false, blocks);
        bool bare = blocks.Count == 1 && blocks[0].gameObject == cockpit.gameObject && unit.Thrusters.Count == 0 && !unit.HasCombatCapability;
        if (!bare)
        {
            strandedSince = -1f;
            owner = unit;
            return false;
        }

        Vector3 position = cockpit.transform.position;
        // Parent changes and meaningful travel start a new window; temporary damage/reassembly never counts as retirement.
        if (strandedSince < 0f || owner != unit || (position - anchor).sqrMagnitude >= 4f)
        {
            owner = unit;
            anchor = position;
            strandedSince = time;
        }

        return time - strandedSince >= timeout;
    }
}
