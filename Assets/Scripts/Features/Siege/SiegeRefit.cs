// 职责：验证泊位净空并报价/执行修缮，复用同源模块、补回缺件，成功后精确扣费。
// 入口：IsClear() / Quote() / Repair()；部署蓝图不含货物，不读取或改写WorldSession。
using UnityEngine;
using System;
using System.Collections.Generic;

public static class SiegeRefit
{
    public static bool IsClear(
        BlockDataList blueprint,
        Vector3 position,
        Quaternion rotation,
        string ignoredUnitId,
        out string reason)
    {
        reason = string.Empty;
        if (!ModularUnitFactory.IsValidBlueprint(blueprint, "siege deployment"))
        {
            reason = "The ship blueprint is invalid.";
            return false;
        }

        return IsGeometryClear(blueprint, position, rotation, ignoredUnitId, out reason);
    }

    public static bool IsBuildingClear(
        BlockDataList blueprint,
        Vector3 position,
        Quaternion rotation,
        string ignoredUnitId,
        out string reason)
    {
        WorldBuildingValidation.ValidateBlueprint(blueprint);
        return IsGeometryClear(blueprint, position, rotation, ignoredUnitId, out reason);
    }

    private static bool IsGeometryClear(
        BlockDataList blueprint,
        Vector3 position,
        Quaternion rotation,
        string ignoredUnitId,
        out string reason)
    {
        reason = string.Empty;
        Physics.SyncTransforms();
        foreach (BlockData block in blueprint.blocks)
        {
            Vector3 center = position + rotation * new Vector3(block.posX, block.posY, block.posZ);
            Vector3 half = new Vector3(block.x, block.y, block.z) * 0.5f - Vector3.one * 0.06f;
            Quaternion blockRotation = rotation * new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW);
            foreach (Collider hit in Physics.OverlapBox(center, half, blockRotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                RuntimeUnitMember member = hit.GetComponentInParent<RuntimeUnitMember>();
                if (member == null || member.ownerUnitId != ignoredUnitId)
                {
                    reason = "The deployment bay is occupied.";
                    return false;
                }
            }
        }

        return true;
    }

    public static long Quote(SiegeFleetEntry entry, SiegeSettings settings)
    {
        if (entry?.Unit == null)
        {
            return 0;
        }

        Dictionary<string, Block> owned = CollectOwned(entry);
        int missing = 0;
        double lostHealth = 0;
        bool detached = false;
        foreach (BlockData data in entry.Blueprint.blocks)
        {
            if (!owned.TryGetValue(data.id, out Block block))
            {
                missing++;
                continue;
            }

            Durability health = block.GetComponent<Durability>();
            if (health != null)
            {
                lostHealth += Mathf.Max(0f, health.maxDurability - health.currentDurability);
            }

            detached |= block.GetComponentInParent<ControlUnit>() != entry.Unit;
        }

        long price = (long)Math.Ceiling(lostHealth * settings.RepairCoinsPerHealth + (double)entry.PurchasePrice * missing / entry.Blueprint.blocks.Count);
        return detached ? Math.Max(1, price) : price;
    }

    public static bool Repair(SiegeFleetEntry entry, SiegeSession session, out string reason)
    {
        reason = "Repairs require a surviving core and an intermission.";
        if (entry?.Unit == null || !session.Match.CanRefit)
        {
            return false;
        }

        long price = Quote(entry, session.Settings);
        if (!session.Match.CanAfford(price))
        {
            reason = "Insufficient coins for repair: " + price;
            return false;
        }

        if (!IsClear(entry.Blueprint, entry.DockPosition, entry.DockRotation, entry.Id, out reason))
        {
            return false;
        }

        Dictionary<string, Block> owned = CollectOwned(entry);
        var created = new List<Block>();
        foreach (BlockData data in entry.Blueprint.blocks)
        {
            if (owned.ContainsKey(data.id))
            {
                continue;
            }

            if (!BlueprintBlockFactory.TryRestore(data, entry.Unit.transform, UnitFaction.Player, out Block block))
            {
                foreach (Block restored in created)
                {
                    restored.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(restored.gameObject);
                }

                reason = "A replacement module could not be restored; no coins spent.";
                return false;
            }

            created.Add(block);
            owned.Add(data.id, block);
        }

        ControlUnit unit = entry.Unit;
        Rigidbody body = unit.GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.position = entry.DockPosition;
        body.rotation = entry.DockRotation;
        unit.transform.SetPositionAndRotation(entry.DockPosition, entry.DockRotation);
        var blocks = new List<Block>(entry.Blueprint.blocks.Count);
        float mass = 0f;
        foreach (BlockData data in entry.Blueprint.blocks)
        {
            Block block = owned[data.id];
            block.RestoreConnectionParticipation();
            block.transform.SetParent(unit.transform, false);
            block.transform.SetLocalPositionAndRotation(
                new Vector3(data.posX, data.posY, data.posZ),
                new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW));
            RuntimeUnitMember.Ensure(block.gameObject, entry.Id, UnitFaction.Player);
            Durability health = block.GetComponent<Durability>();
            if (health != null)
            {
                health.UpdateDurablility(Mathf.Max(0f, health.maxDurability - health.currentDurability));
            }

            blocks.Add(block);
            mass += block.mass;
        }

        body.mass = Mathf.Max(1f, mass);
        new BatchConnectionBuilder(blocks).RebuildBatchImmediate();
        unit.RefreshChildren();
        session.Match.TrySpend(price);
        reason = "Refitted " + entry.DisplayName + " for " + price + " coins; replaced " + created.Count + " modules.";
        return true;
    }

    private static Dictionary<string, Block> CollectOwned(SiegeFleetEntry entry)
    {
        var result = new Dictionary<string, Block>(StringComparer.Ordinal);
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (unit == null || unit.gameObject.scene != entry.Cockpit.gameObject.scene)
            {
                continue;
            }

            foreach (Block block in unit.GetComponentsInChildren<Block>())
            {
                RuntimeUnitMember member = block.GetComponent<RuntimeUnitMember>();
                if (block.isActiveAndEnabled && member != null && member.ownerUnitId == entry.Id && !string.IsNullOrEmpty(block.uniqueId))
                {
                    result[block.uniqueId] = block;
                }
            }
        }

        return result;
    }
}
