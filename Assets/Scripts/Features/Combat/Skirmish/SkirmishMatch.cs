// 职责：自动填充随机5v5、按驾驶舱判定淘汰，结算等待后完整清场并开始下一轮。
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
    [SerializeField, Tooltip("敌方内置蓝图；玩家方只读取Saves玩家蓝图池。")]
    private TextAsset[] blueprints;
    [SerializeField, Tooltip("只读加入persistentDataPath/EnemyBlueprints中的有效敌舰。")]
    private bool includeSavedEnemies = true;
    [SerializeField, Tooltip("前5个为蓝队，后5个为红队；位置与朝向均可编辑。")]
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
    private readonly List<SiegeShipOption> playerPool = new List<SiegeShipOption>();
    private readonly List<SiegeShipOption> pool = new List<SiegeShipOption>();
    private readonly Cockpit[] cockpits = new Cockpit[10];
    private readonly bool[] eliminated = new bool[10];
    private readonly string[] names = new string[10];
    private readonly StringBuilder lineup = new StringBuilder();
    private bool cleaning;
    private float nextHud;
    private float restartAt;
    private string result;
    [SerializeField, Min(0.5f), Tooltip("双方连续失能的确认时间，避免初始化和电网重建瞬态误判。")]
    private float incapacityGrace = 3f;
    [SerializeField]
    private Color blueColor = new Color(0.18f, 0.58f, 1f);
    [SerializeField]
    private Color redColor = new Color(1f, 0.22f, 0.18f);
    private float powerlessSince = -1f;
    private float nextCapabilityCheck;
    private bool bothPowerless;
    private readonly Dictionary<Material, Material> blueMaterials = new Dictionary<Material, Material>();
    private readonly Dictionary<Material, Material> redMaterials = new Dictionary<Material, Material>();
    public string Result => result;
    public int PlayerPoolCount => playerPool.Count;
    public int EnemyPoolCount => pool.Count;

    public Color TeamColor(bool blue) => blue ? blueColor : redColor;
    public int Round
    {
        get; private set;
    }
    public bool Settling
    {
        get; private set;
    }
    public int BlueAlive => CountAlive(0);
    public int RedAlive => CountAlive(5);
    public SkirmishHud Hud => hud;

    public Cockpit CockpitAt(int index) => index >= 0 && index < 10 ? cockpits[index] : null;
    public string NameAt(int index) => index >= 0 && index < 10 ? names[index] : string.Empty;
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

        playerPool.AddRange(SiegeBlueprintCatalog.Load(Path.Combine(Application.persistentDataPath, "Saves"), 1, out _));
        if (slots == null || slots.Length != 10 || Array.Exists(slots, slot => slot == null) || pool.Count == 0 || playerPool.Count == 0)
        {
            hud.SetState(
                "5v5 CONFIGURATION ERROR",
                "Requires ten slots, player blueprints in Saves and enemy blueprints in EnemyBlueprints or the configured enemy assets.");
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
        powerlessSince = -1f;
        bothPowerless = false;
        nextCapabilityCheck = Time.time + 0.5f;
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

        for (int i = 0; i < 10; i++)
        {
            List<SiegeShipOption> teamPool = i < 5 ? playerPool : pool;
            SiegeShipOption blueprint = teamPool[UnityEngine.Random.Range(0, teamPool.Count)];
            names[i] = (i < 5 ? "BLUE-" : "RED-") + (i % 5 + 1) + " " + blueprint.DisplayName;
            ControlUnit unit = ModularUnitFactory.Spawn(
                JsonUtility.FromJson<BlockDataList>(blueprint.BlueprintJson),
                names[i],
                slots[i].position,
                slots[i].rotation,
                i < 5 ? UnitFaction.Player : UnitFaction.Enemy);
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
            ai.detectionRange = 800f;
            ApplyTeamColor(unit, i < 5);
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
        for (int i = first; i < first + 5; i++)
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

        for (int i = 0; i < 10; i++)
        {
            if (!eliminated[i] && (cockpits[i] == null || !cockpits[i].gameObject.activeInHierarchy || cockpits[i].GetComponent<Durability>().currentDurability <= 0f))
            {
                Eliminate(i, null);
            }
        }

        if (!Settling && Time.time >= nextCapabilityCheck)
        {
            nextCapabilityCheck = Time.time + 0.5f;
            bothPowerless = !TeamCanFight(0) && !TeamCanFight(5);
            powerlessSince = bothPowerless ? (powerlessSince < 0f ? Time.time : powerlessSince) : -1f;
        }

        bool incapacityDraw = bothPowerless && powerlessSince >= 0f && Time.time - powerlessSince >= incapacityGrace;
        // 同一物理步的连锁死亡全部处理后再结算，支持双方同时全灭。
        if (!Settling && (BlueAlive == 0 || RedAlive == 0 || incapacityDraw))
        {
            Settling = true;
            result = incapacityDraw || BlueAlive == 0 && RedAlive == 0 ? "DRAW" : BlueAlive > 0 ? "BLUE WINS" : "RED WINS";
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
            for (int i = 0; i < 10; i++)
            {
                lineup.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(TeamColor(i < 5))).Append(">").Append(eliminated[i] ? "[OUT] " : "[LIVE] ").Append(names[i]).AppendLine("</color>");
            }

            string title = $"ROUND {Round:000}   BLUE {BlueAlive} : {RedAlive} RED";
            hud.SetState(
                Settling ? title + $"\n{result} — NEXT ROUND IN {Mathf.CeilToInt(Mathf.Max(0f, restartAt - Time.time))}s" : title + "\nAUTOMATIC RANDOM 5v5",
                lineup.ToString());
        }
    }

    private bool TeamCanFight(int first)
    {
        for (int i = first; i < first + 5; i++)
        {
            if (!eliminated[i] && cockpits[i] != null)
            {
                ControlUnit unit = cockpits[i].GetComponentInParent<ControlUnit>();
                if (unit != null && unit.HasCombatCapability)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ApplyTeamColor(ControlUnit unit, bool blue)
    {
        var animated = new List<IndustrialPartMotion>();
        foreach (IndustrialPartMotion motion in unit.GetComponentsInChildren<IndustrialPartMotion>())
        {
            if (motion.enabled)
            {
                animated.Add(motion);
                motion.enabled = false;
            }
        }

        Dictionary<Material, Material> cache = blue ? blueMaterials : redMaterials;
        Color color = TeamColor(blue);
        foreach (MeshRenderer renderer in unit.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null)
                {
                    continue;
                }

                if (!cache.TryGetValue(source, out Material tinted))
                {
                    tinted = new Material(source)
                    {
                        name = source.name + (blue ? " (Blue Team)" : " (Red Team)"),
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    if (tinted.HasProperty("_BaseColor"))
                    {
                        tinted.SetColor("_BaseColor", color);
                    }

                    if (tinted.HasProperty("_Color"))
                    {
                        tinted.SetColor("_Color", color);
                    }

                    cache.Add(source, tinted);
                }

                materials[i] = tinted;
            }

            renderer.sharedMaterials = materials;
        }

        foreach (IndustrialPartMotion motion in animated)
        {
            motion.enabled = true;
        }
    }

    private void OnDestroy()
    {
        // 本场景持有的共享材质只在退出时释放；轮次间复用，避免每回合重建。
        foreach (Material material in blueMaterials.Values)
        {
            ReleaseTeamMaterial(material);
        }

        foreach (Material material in redMaterials.Values)
        {
            ReleaseTeamMaterial(material);
        }

        blueMaterials.Clear();
        redMaterials.Clear();
    }

    private static void ReleaseTeamMaterial(Material material)
    {
#if UNITY_EDITOR
        // 退出Play时不会再处理延迟销毁队列，显式释放DontSave材质，避免Editor残留。
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            DestroyImmediate(material);
            return;
        }

#endif
        Destroy(material);
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
