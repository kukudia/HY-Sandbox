// 职责：自动填充随机3v3、按驾驶舱判定淘汰，结算等待后完整清场并开始下一轮。
// 入口：Start()自动开局；NotifyCockpitDestroyed()接收正式销毁主链，Update()处理胜负。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SkirmishMatch : MonoBehaviour
{
    [SerializeField, Tooltip("仅使用敌舰蓝图；两个阵营从同一池独立抽取，允许重复。")]
    private TextAsset[] blueprints;
    [SerializeField, Tooltip("只读加入persistentDataPath/EnemyBlueprints中的有效敌舰。")]
    private bool includeSavedEnemies = true;
    [SerializeField, Tooltip("前3个为蓝队，后3个为红队；位置与朝向均可编辑。")]
    private Transform[] slots;
    [SerializeField, Min(0f)]
    private float restartDelay = 5f;
    [SerializeField]
    private SkirmishHud hud;
    [SerializeField]
    private PlayManager play;
    [SerializeField]
    private DestroyManager destruction;
    [SerializeField]
    private TurretProjectilePool projectiles;
    [SerializeField]
    private VfxEffectPool effects;
    private readonly HashSet<GameObject> authoredRoots = new HashSet<GameObject>();
    private readonly List<SiegeShipOption> pool = new List<SiegeShipOption>();
    private readonly Cockpit[] cockpits = new Cockpit[6];
    private readonly bool[] eliminated = new bool[6];
    private readonly string[] names = new string[6];
    private readonly StringBuilder lineup = new StringBuilder();
    private bool cleaning;
    private float nextHud;
    private float restartAt;
    private string result;
    public int Round
    {
        get; private set;
    }
    public bool Settling
    {
        get; private set;
    }
    public int BlueAlive => CountAlive(0);
    public int RedAlive => CountAlive(3);
    public SkirmishHud Hud => hud;

    public Cockpit CockpitAt(int index) => index >= 0 && index < 6 ? cockpits[index] : null;
    public string NameAt(int index) => index >= 0 && index < 6 ? names[index] : string.Empty;
    private void Start()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            authoredRoots.Add(root);
        }

        foreach (TextAsset blueprint in blueprints)
        {
            if (blueprint != null && ModularUnitFactory.IsValidBlueprint(JsonUtility.FromJson<BlockDataList>(blueprint.text), blueprint.name))
            {
                pool.Add(new SiegeShipOption(blueprint.name, blueprint.text, 1));
            }
        }

        if (includeSavedEnemies)
        {
            pool.AddRange(SiegeBlueprintCatalog.Load(Path.Combine(Application.persistentDataPath, "EnemyBlueprints"), 1, out _));
        }

        if (slots == null || slots.Length != 6 || Array.Exists(slots, slot => slot == null) || pool.Count == 0)
        {
            hud.SetState("3v3 CONFIGURATION ERROR", "Requires six spawn transforms and valid enemy blueprints.");
            enabled = false;
            return;
        }

        play.playMode = true;
        BeginRound();
    }

    private void BeginRound()
    {
        Round++;
        Settling = false;
        result = null;
        hud.ResetRound();
        foreach (GameObject root in authoredRoots)
        {
            if (root != null)
            {
                foreach (EnemyLabMovingObstacle obstacle in root.GetComponentsInChildren<EnemyLabMovingObstacle>())
                {
                    obstacle.ResetMotion();
                }
            }
        }

        for (int i = 0; i < 6; i++)
        {
            SiegeShipOption blueprint = pool[UnityEngine.Random.Range(0, pool.Count)];
            names[i] = (i < 3 ? "BLUE-" : "RED-") + (i % 3 + 1) + " " + blueprint.DisplayName;
            ControlUnit unit = ModularUnitFactory.Spawn(
                JsonUtility.FromJson<BlockDataList>(blueprint.BlueprintJson),
                names[i],
                slots[i].position,
                slots[i].rotation,
                i < 3 ? UnitFaction.Player : UnitFaction.Enemy);
            if (unit == null)
            {
                throw new InvalidOperationException("Validated combat blueprint failed to spawn: " + blueprint.DisplayName);
            }

            SceneManager.MoveGameObjectToScene(unit.gameObject, gameObject.scene);
            unit.cockpit.SetControlMode(UnitControlMode.Autonomous);
            unit.RefreshChildren();
            cockpits[i] = unit.cockpit;
            eliminated[i] = false;
            EnemyController ai = unit.GetComponent<EnemyController>();
            ai.detectionRange = 450f;
            ai.ConfigureDebugDisplay(false, false);
            foreach (Turret turret in unit.GetComponentsInChildren<Turret>())
            {
                turret.SetStarted(true);
            }
        }

        nextHud = 0f;
        Physics.SyncTransforms();
    }

    public bool NotifyCockpitDestroyed(Cockpit cockpit, ControlUnit attacker)
    {
        if (!isActiveAndEnabled || cleaning || cockpit == null)
        {
            return false;
        }

        int index = Array.IndexOf(cockpits, cockpit);
        if (index < 0)
        {
            return false;
        }

        Eliminate(index, attacker != null ? attacker.name : null);
        return true;
    }

    private void Eliminate(int index, string attacker)
    {
        if (eliminated[index])
        {
            return;
        }

        eliminated[index] = true;
        hud.ShowKill(names[index], attacker, BlueAlive, RedAlive);
        nextHud = 0f;
    }

    private int CountAlive(int first)
    {
        int count = 0;
        for (int i = first; i < first + 3; i++)
        {
            if (!eliminated[i] && cockpits[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void Update()
    {
        if (cleaning || Round == 0)
        {
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            if (!eliminated[i] && (cockpits[i] == null || !cockpits[i].gameObject.activeInHierarchy || cockpits[i].GetComponent<Durability>().currentDurability <= 0f))
            {
                Eliminate(i, null);
            }
        }

        // 同一物理步的连锁死亡全部处理后再结算，支持双方同时全灭。
        if (!Settling && (BlueAlive == 0 || RedAlive == 0))
        {
            Settling = true;
            result = BlueAlive == 0 && RedAlive == 0 ? "DRAW" : BlueAlive > 0 ? "BLUE WINS" : "RED WINS";
            restartAt = Time.time + restartDelay;
            foreach (ControlUnit unit in play.GetControlUnits())
            {
                if (unit != null && unit.gameObject.scene == gameObject.scene)
                {
                    unit.GetComponent<EnemyController>().enabled = false;
                    unit.SetMovementInput(Vector3.zero);
                    foreach (Turret turret in unit.GetComponentsInChildren<Turret>())
                    {
                        turret.SetStarted(false);
                    }
                }
            }
        }

        if (Settling && Time.time >= restartAt)
        {
            StartCoroutine(ClearAndRestart());
            return;
        }

        if (Time.unscaledTime >= nextHud)
        {
            nextHud = Time.unscaledTime + 0.2f;
            lineup.Clear();
            for (int i = 0; i < 6; i++)
            {
                lineup.Append(eliminated[i] ? "[OUT] " : "[LIVE] ").AppendLine(names[i]);
            }

            string title = $"ROUND {Round:000}   BLUE {BlueAlive} : {RedAlive} RED";
            hud.SetState(
                Settling ? title + $"\n{result} — NEXT ROUND IN {Mathf.CeilToInt(Mathf.Max(0f, restartAt - Time.time))}s" : title + "\nAUTOMATIC RANDOM 3v3",
                lineup.ToString());
        }
    }

    private IEnumerator ClearAndRestart()
    {
        cleaning = true;
        destruction.EndSalvageSession();
        projectiles.ClearAll();
        effects.ClearActive();
        // 本独立场景所有新增根节点均属于本轮：涵盖重组残骸、脱离模块与掉落。
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (!authoredRoots.Contains(root))
            {
                root.SetActive(false);
                Destroy(root);
            }
        }

        play.allControlUnits.RemoveAll(unit => unit == null || unit.gameObject.scene == gameObject.scene);
        yield return null;
        BeginRound();
        cleaning = false;
    }
}
