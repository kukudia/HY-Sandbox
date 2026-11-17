// 职责：共享战斗HUD入口，显式绑定战场/镜头/关注舰，限频采样耐久并逐帧投影名牌。
// 入口：Bind() / ResetHud() / ShowPlayerKill() / ShowCombatKill()；退出绑定时清空视图和通知。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(200)]
public sealed class CombatHud : MonoBehaviour
{
    [SerializeField]
    private RectTransform _overlay;
    [SerializeField]
    private RectTransform _enemyTemplate;
    [SerializeField]
    private CombatHudTheme theme;
    [SerializeField]
    private CombatNotifications notifications;
    [SerializeField]
    private CombatThreatMarkers threats;
    [SerializeField]
    private CombatHudPanels panels;
    [SerializeField, Min(0f), Tooltip("0沿用共享主题；远距观战场景可单独覆盖显示距离。")]
    private float nameplateDistanceOverride;
    private readonly Dictionary<Cockpit, View> views = new Dictionary<Cockpit, View>();
    private readonly Stack<CombatNameplate> pool = new Stack<CombatNameplate>();
    private readonly List<ControlUnit> candidates = new List<ControlUnit>();
    private readonly List<Cockpit> removed = new List<Cockpit>();
    private readonly RaycastHit[] occlusionHits = new RaycastHit[128];
    private readonly List<Rect> placed = new List<Rect>();
    private Scene battlefield;
    private Camera cameraOverride;
    private bool explicitBinding;
    private bool allTeams;
    private float nextSample;
    private int kills;
    public ControlUnit FocusedUnit
    {
        get; private set;
    }
    public Camera WorldCamera
    {
        get; private set;
    }
    public CombatHudTheme Theme => theme;
    public CombatNotifications Notifications => notifications;
    public CombatHudPanels Panels => panels;
    public bool EliteWave
    {
        get; set;
    }
    public IReadOnlyList<ControlUnit> Candidates => candidates;
    public bool IsBattleActive => explicitBinding || (PlayManager.instance != null && PlayManager.instance.playMode);
    public Scene Battlefield => explicitBinding ? battlefield : PlayManager.instance != null && PlayManager.instance.WorldSession != null ? PlayManager.instance.WorldSession.gameObject.scene : gameObject.scene;

    private sealed class View
    {
        public ControlUnit unit;
        public CombatNameplate plate;
        public float maximum;
        public float nextRay;
        public bool occluded;
    }

    public void Bind(Scene scene, Camera camera, bool showBothTeams)
    {
        ResetHud();
        battlefield = scene;
        cameraOverride = camera;
        allTeams = showBothTeams;
        explicitBinding = true;
    }

    public void SetFocus(ControlUnit unit)
    {
        FocusedUnit = unit;
    }

    public void Unbind()
    {
        explicitBinding = false;
        EliteWave = false;
        cameraOverride = null;
        FocusedUnit = null;
        ResetHud();
    }

    private void OnEnable()
    {
        ResetHud();
    }

    private void OnDisable()
    {
        Unbind();
    }

    public void ResetHud()
    {
        foreach (View view in views.Values)
        {
            view.plate.gameObject.SetActive(false);
            pool.Push(view.plate);
        }

        views.Clear();
        candidates.Clear();
        kills = 0;
        nextSample = 0f;
        notifications?.Clear();
        threats?.Clear();
    }

    public void ShowPlayerKill(string enemyName)
    {
        if (!IsBattleActive || !isActiveAndEnabled)
        {
            return;
        }

        kills++;
        notifications.Push(enemyName, null, $"KILLS  {kills:00}", true);
    }

    public void ShowCombatKill(string victim, string attacker, string score)
    {
        ShowDestruction(victim, attacker, score, true);
    }

    public void ShowDestruction(string victim, string attacker, string detail, bool celebrate)
    {
        if (IsBattleActive && isActiveAndEnabled && notifications != null)
        {
            notifications.Push(victim, attacker, detail, celebrate);
        }
    }

    private void LateUpdate()
    {
        if (!IsBattleActive || theme == null)
        {
            if (views.Count > 0 || candidates.Count > 0)
            {
                ResetHud();
            }

            return;
        }

        WorldCamera = cameraOverride != null ? cameraOverride : PlayManager.instance != null && PlayManager.instance.mainCamera != null ? PlayManager.instance.mainCamera : Camera.main;
        if (WorldCamera == null)
        {
            ResetHud();
            return;
        }

        if (!explicitBinding && PlayManager.instance != null)
        {
            EliteWave = PlayManager.instance.SiegeSession != null && PlayManager.instance.SiegeSession.Match != null && PlayManager.instance.SiegeSession.Match.IsEliteWave;
            FocusedUnit = PlayManager.instance.blocksParent != null ? PlayManager.instance.blocksParent.GetComponent<ControlUnit>() : null;
        }

        if (Time.unscaledTime >= nextSample)
        {
            Sample();
            nextSample = Time.unscaledTime + theme.sampleInterval;
        }

        Project();
    }

    private void Sample()
    {
        candidates.Clear();
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (unit != null && unit.gameObject.scene == Battlefield && unit.HasValidCockpit && unit.cockpit.isActiveAndEnabled && unit.isActiveAndEnabled && (unit.faction == UnitFaction.Enemy || allTeams || (PlayManager.instance != null && PlayManager.instance.Skirmish != null)))
            {
                candidates.Add(unit);
            }
        }

        candidates.Sort(ComparePriority);
        removed.Clear();
        foreach (KeyValuePair<Cockpit, View> pair in views)
        {
            int rank = pair.Value.unit != null ? candidates.IndexOf(pair.Value.unit) : -1;
            if (pair.Key == null || !pair.Key.isActiveAndEnabled || rank < 0 || rank >= theme.maximumNameplates)
            {
                removed.Add(pair.Key);
            }
        }

        foreach (Cockpit cockpit in removed)
        {
            View old = views[cockpit];
            old.plate.gameObject.SetActive(false);
            pool.Push(old.plate);
            views.Remove(cockpit);
        }

        int budget = Mathf.Min(candidates.Count, theme.maximumNameplates);
        for (int i = 0; i < budget; i++)
        {
            ControlUnit unit = candidates[i];
            if (!views.TryGetValue(unit.cockpit, out View view))
            {
                CombatNameplate plate = pool.Count > 0 ? pool.Pop() : Instantiate(_enemyTemplate, _overlay).GetComponent<CombatNameplate>();
                EnemyIdentity identity = unit.cockpit.GetComponent<EnemyIdentity>();
                Color color = unit.faction == UnitFaction.Player ? theme.friendly : EliteWave ? theme.warning : theme.hostile;
                plate.Bind(
                    theme,
                    (unit.faction == UnitFaction.Player ? "ALLY  /  " : EliteWave ? "ELITE  /  " : "HOSTILE  /  ") + (identity != null ? identity.DisplayName : unit.name),
                    color);
                view = new View
                {
                    unit = unit,
                    plate = plate,
                    maximum = identity != null ? identity.SpawnMaxHealth : 0f,
                    nextRay = Time.unscaledTime + (i % 6) * .025f
                };
                views.Add(unit.cockpit, view);
            }

            view.unit = unit;
            unit.TryGetTotalDurability(out float health, out float maximum);
            if (view.maximum <= 0f)
            {
                view.maximum = maximum;
            }

            view.maximum = Mathf.Max(view.maximum, maximum);
            Durability core = unit.cockpit.GetComponent<Durability>();
            bool detailed = unit == FocusedUnit || (unit.transform.position - WorldCamera.transform.position).sqrMagnitude < 6400f;
            view.plate.Sample(
                health,
                view.maximum,
                core != null ? core.currentDurability : 0f,
                core != null ? core.maxDurability : 0f,
                detailed);
        }
    }

    private int ComparePriority(ControlUnit left, ControlUnit right)
    {
        if (left == FocusedUnit || right == FocusedUnit)
        {
            return left == right ? 0 : left == FocusedUnit ? -1 : 1;
        }

        Vector3 origin = WorldCamera.transform.position;
        return (left.transform.position - origin).sqrMagnitude.CompareTo((right.transform.position - origin).sqrMagnitude);
    }

    private void Project()
    {
        placed.Clear();
        foreach (ControlUnit candidate in candidates)
        {
            if (candidate == null || candidate.cockpit == null || !views.TryGetValue(candidate.cockpit, out View view))
            {
                continue;
            }

            Vector3 world = candidate.cockpit.transform.position + Vector3.up * 3f;
            Vector3 screen = WorldCamera.WorldToScreenPoint(world);
            float distanceLimit = nameplateDistanceOverride > 0f ? nameplateDistanceOverride : theme.nameplateDistance;
            bool visible = screen.z > 0f && screen.x > 0f && screen.x < Screen.width && screen.y > 0f && screen.y < Screen.height && (world - WorldCamera.transform.position).sqrMagnitude < distanceLimit * distanceLimit;
            if (visible && Time.unscaledTime >= view.nextRay)
            {
                view.occluded = IsWorldPointOccluded(WorldCamera, world, candidate.transform, occlusionHits);
                view.nextRay = Time.unscaledTime + theme.occlusionInterval;
            }

            visible &= !view.occluded;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, null, out Vector2 local);
            Rect bounds = new Rect(local - new Vector2(120f, 0f), new Vector2(240f, 64f));
            foreach (Rect other in placed)
            {
                if (bounds.Overlaps(other))
                {
                    visible = false;
                    break;
                }
            }

            if (visible)
            {
                placed.Add(bounds);
            }

            view.plate.Present(local, visible, Time.unscaledDeltaTime);
        }
    }

    public bool HasVisibleNameplate(ControlUnit unit)
    {
        return unit != null && unit.cockpit != null && views.TryGetValue(unit.cockpit, out View view) && view.plate.IsVisible;
    }

    public static bool IsWorldPointOccluded(Camera camera, Vector3 target, Transform owner, RaycastHit[] hits)
    {
        Vector3 delta = target - camera.transform.position;
        if (delta.magnitude < .01f)
        {
            return false;
        }

        int count = Physics.RaycastNonAlloc(
            camera.transform.position,
            delta.normalized,
            hits,
            delta.magnitude - .01f,
            ~((1 << 2) | (1 << 5)),
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider collider = hits[i].collider;
            if (collider != null && (owner == null || !collider.transform.IsChildOf(owner)))
            {
                return true;
            }
        }

        return count == hits.Length;
    }
}
