// 职责：在独立在场预算内按固定随机种子调度敌舰，并将真实死亡或移除结算给规则核心。
// 入口：Tick() / Stop()；死亡事件只记事实，由下一次Tick统一结算，避免事件中重入生成。
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SiegeWaveDirector
{
    private readonly SiegeSession session;
    private readonly System.Random random;
    private readonly List<Attacker> attackers = new List<Attacker>();
    private float nextSpawn;
    public SiegeWaveDirector(SiegeSession session)
    {
        this.session = session;
        random = new System.Random(session.Settings.SpawnSeed);
    }

    public void Tick(float time)
    {
        if (session.Match.Phase != SiegePhase.Battle)
        {
            return;
        }

        for (int index = attackers.Count - 1; index >= 0; index--)
        {
            Attacker attacker = attackers[index];
            if (!attacker.Defeated && attacker.Cockpit != null && attacker.Cockpit.isActiveAndEnabled)
            {
                attacker.RefreshTarget(session.Fleet.Base.Unit);
                continue;
            }

            session.Match.ResolveAttacker(attacker.Id, attacker.Defeated);
            attacker.Unsubscribe();
            attackers.RemoveAt(index);
        }

        SiegeSettings settings = session.Settings;
        if (time < nextSpawn || !session.Match.CanSpawn(settings.MaximumActiveEnemies) || session.AttackerOptions.Length == 0 || session.Fleet.Base.Unit == null)
        {
            return;
        }

        float interval = settings.SpawnInterval * (session.Match.IsSwarmWave ? Mathf.Clamp(settings.SwarmSpawnIntervalScale, 0.2f, 1f) : 1f);
        nextSpawn = time + Mathf.Max(0.1f, interval);
        SiegeShipOption option = PickBlueprint(settings);
        double angle = random.NextDouble() * Math.PI * 2.0;
        Vector3 offset = new Vector3((float)Math.Cos(angle), 0f, (float)Math.Sin(angle)) * settings.AttackRadius;
        Vector3 position = session.Fleet.Base.Core.transform.position + offset + Vector3.up * 10f;
        BlockDataList blueprint = SiegeFleet.PrepareBlueprint(option.BlueprintJson, out string id);
        ControlUnit unit = EnemyBlueprintFactory.Spawn(
            blueprint,
            "Raider " + session.Match.Wave + " / " + option.DisplayName,
            position,
            Quaternion.LookRotation(-offset));
        if (unit == null)
        {
            return;
        }

        unit.runtimeUnitId = id;
        unit.AssignRuntimeOwnershipToBlocks(true);
        if (!session.Match.RegisterAttacker(id, settings.MaximumActiveEnemies))
        {
            unit.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(unit.gameObject);
            return;
        }

        unit.GetComponent<EnemyController>().SetPriorityTarget(session.Fleet.Base.Unit);
        attackers.Add(new Attacker(id, unit.cockpit));
        // 进场信标只做一次短时表现，帮助玩家在夜色里看清来袭方向。
        VisualEffectsManager.instance?.PlaySpawnBeacon(position);
    }

    /// <summary>精英波次固定挑最重的蓝图，其余波次沿用玩家指定或按种子随机。</summary>
    private SiegeShipOption PickBlueprint(SiegeSettings settings)
    {
        SiegeShipOption[] options = session.AttackerOptions;
        if (session.Match.IsEliteWave)
        {
            SiegeShipOption heaviest = options[0];
            for (int index = 1; index < options.Length; index++)
            {
                if (options[index].BlockCount > heaviest.BlockCount)
                {
                    heaviest = options[index];
                }
            }

            return heaviest;
        }

        return options[session.SelectedAttacker >= 0 ? session.SelectedAttacker : random.Next(options.Length)];
    }

    public void Stop()
    {
        foreach (Attacker attacker in attackers)
        {
            attacker.Unsubscribe();
        }

        attackers.Clear();
    }

    private sealed class Attacker
    {
        public readonly string Id;
        public readonly Cockpit Cockpit;
        private readonly EnemyIdentity identity;
        public bool Defeated
        {
            get; private set;
        }

        private ControlUnit boundUnit;
        private ControlUnit boundTarget;
        public Attacker(string id, Cockpit cockpit)
        {
            Id = id;
            Cockpit = cockpit;
            identity = cockpit.GetComponent<EnemyIdentity>();
            identity.Defeated += OnDefeated;
        }

        private void OnDefeated()
        {
            Defeated = true;
        }

        public void RefreshTarget(ControlUnit core)
        {
            ControlUnit unit = Cockpit.GetComponentInParent<ControlUnit>();
            if (unit == null || (unit == boundUnit && core == boundTarget))
            {
                return;
            }

            boundUnit = unit;
            boundTarget = core;
            unit.GetComponent<EnemyController>()?.SetPriorityTarget(core);
        }

        public void Unsubscribe()
        {
            if (identity != null)
            {
                identity.Defeated -= OnDefeated;
            }
        }
    }
}
