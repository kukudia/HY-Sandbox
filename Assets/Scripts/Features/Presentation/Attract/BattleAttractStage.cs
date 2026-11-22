// 职责：独立待机战场的内置舰队、基地恢复和有限烟火编排，不访问个人蓝图或结算金币。
// 入口：Start()/Update()；复用生产模块工厂、建筑和武器链，按槽位替换阵亡/越界舰。
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattleAttractStage : MonoBehaviour
{
    [SerializeField]
    private SiegeSettings baseSettings;
    [SerializeField]
    private BlueprintStructureRegistry buildings;
    [SerializeField]
    private Transform baseAnchor;
    [SerializeField]
    private TextAsset[] blueprints;
    [SerializeField]
    private Transform[] slots;
    [SerializeField]
    private Transform[] explosionPoints;
    [SerializeField]
    private VfxEffectPool effects;
    [SerializeField, Min(.25f)]
    private float explosionInterval = 2.2f;
    [SerializeField, Min(1)]
    private int maximumAtmosphereEffects = 6;
    [SerializeField, Min(10f)]
    private float encounterSeconds = 65f;
    [SerializeField, Min(.2f), Tooltip("演出场索敌周期；错峰搜索遮挡模块，瞄准和开火仍逐物理步更新。")]
    private float targetScanInterval = 1.2f;
    [SerializeField]
    private int seed = 1811;
    private Cockpit[] fleet;
    private float[] respawnAt;
    private SiegeBase bastion;
    private System.Random random;
    private float nextExplosion;
    private float resetAt;
    public int SpawnedShips
    {
        get; private set;
    }
    public int AtmosphereBursts
    {
        get; private set;
    }
    public ControlUnit Featured => fleet?.FirstOrDefault(cockpit => cockpit != null && cockpit.isActiveAndEnabled)?.GetComponentInParent<ControlUnit>();
    public bool BaseAlive => bastion?.IsAlive == true;

    private void Start()
    {
        Application.runInBackground = true;
        random = new System.Random(seed);
        fleet = new Cockpit[slots.Length];
        respawnAt = new float[slots.Length];
        PlayManager.instance.playMode = true;
        resetAt = Time.time + encounterSeconds;
        RestoreBase();
    }

    private void RestoreBase()
    {
        bastion?.Dispose();
        buildings.ClearSession();
        BlockDataList design = JsonUtility.FromJson<BlockDataList>(baseSettings.BaseBlueprint.text);
        bastion = SiegeBase.Create(
            new SiegeMatch(
                0,
                1,
                0,
                1,
                0,
                0),
            baseSettings,
            buildings,
            design,
            baseSettings.BaseCoreBlockId,
            "Attract Bastion",
            baseAnchor.position,
            baseAnchor.rotation,
            0,
            null);
        foreach (Turret turret in bastion.Building.GetComponentsInChildren<Turret>())
        {
            turret.range = 220f;
            turret.fireInterval = Mathf.Max(.35f, turret.fireInterval * 1.4f);
            turret.damage *= .18f;
            turret.targetRefreshInterval = targetScanInterval;
            turret.DelayTargetSearch((float)random.NextDouble() * targetScanInterval);
            turret.SetStarted(true);
        }
    }

    private void Update()
    {
        if (fleet == null || PauseMenu.IsPaused)
        {
            return;
        }

        if (Time.time >= resetAt)
        {
            foreach (ControlUnit unit in ControlUnit.RuntimeUnits.ToArray())
            {
                if (unit != null && unit.Building == null && unit.gameObject.scene == gameObject.scene)
                {
                    unit.gameObject.SetActive(false);
                    Destroy(unit.gameObject);
                }
            }

            Array.Clear(fleet, 0, fleet.Length);
            resetAt = Time.time + encounterSeconds;
            RestoreBase();
        }

        if (!BaseAlive)
        {
            RestoreBase();
        }

        // At most one ship per frame, with staggered replacement after loss.
        for (int index = 0; index < fleet.Length; index++)
        {
            Cockpit cockpit = fleet[index];
            if (cockpit != null && cockpit.isActiveAndEnabled)
            {
                if ((cockpit.transform.position - baseAnchor.position).sqrMagnitude < 500f * 500f)
                {
                    continue;
                }

                ControlUnit escaped = cockpit.GetComponentInParent<ControlUnit>();
                if (escaped != null)
                {
                    escaped.gameObject.SetActive(false);
                    Destroy(escaped.gameObject);
                }

                fleet[index] = null;
                respawnAt[index] = Time.time + 2f;
            }

            if (Time.time < respawnAt[index])
            {
                continue;
            }

            Spawn(index);
            break;
        }

        if (Time.time >= nextExplosion && effects.ActiveCount < maximumAtmosphereEffects && explosionPoints.Length > 0)
        {
            nextExplosion = Time.time + explosionInterval;
            Vector3 center = explosionPoints[random.Next(explosionPoints.Length)].position;
            center += new Vector3(
                (float)random.NextDouble() * 14f - 7f,
                (float)random.NextDouble() * 8f,
                (float)random.NextDouble() * 14f - 7f);
            if (BlockVfxLibrary.Play(
                BlockVfxLibrary.Effect.Explosion,
                center,
                Quaternion.identity,
                1.2f + (float)random.NextDouble() * 1.4f) == BlockVfxLibrary.SpawnResult.Spawned)
            {
                AtmosphereBursts++;
            }
        }
    }

    private void Spawn(int index)
    {
        TextAsset source = blueprints[index % blueprints.Length];
        BlockDataList design = SiegeFleet.PrepareBlueprint(source, out _);
        bool friendly = index < slots.Length / 2;
        ControlUnit unit = ModularUnitFactory.Spawn(
            design,
            (friendly ? "DEFENDER / " : "RAIDER / ") + source.name,
            slots[index].position,
            slots[index].rotation,
            friendly ? UnitFaction.Player : UnitFaction.Enemy);
        if (unit == null)
        {
            respawnAt[index] = Time.time + 5f;
            return;
        }

        SceneManager.MoveGameObjectToScene(unit.gameObject, gameObject.scene);
        unit.cockpit.SetControlMode(UnitControlMode.Autonomous);
        unit.RefreshChildren();
        EnemyController ai = unit.GetComponent<EnemyController>();
        ai.detectionRange = 600f;
        ai.desiredDistance = 38f;
        if (!friendly)
        {
            ai.SetPriorityTarget(bastion.Unit);
        }

        foreach (Turret turret in unit.GetComponentsInChildren<Turret>())
        {
            turret.range = 160f;
            turret.fireInterval = Mathf.Max(.35f, turret.fireInterval * 1.5f);
            turret.damage *= .12f;
            turret.targetRefreshInterval = targetScanInterval;
            turret.DelayTargetSearch((float)random.NextDouble() * targetScanInterval);
            turret.SetStarted(true);
        }

        fleet[index] = unit.cockpit;
        respawnAt[index] = Time.time + 3f;
        SpawnedShips++;
        VisualEffectsManager.TryPlayEnemySpawnPortal(unit);
    }

    private void OnDestroy()
    {
        bastion?.Dispose();
    }
}
