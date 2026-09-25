using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CombatHud : MonoBehaviour
{
    [SerializeField] private RectTransform _overlay;
    [SerializeField] private RectTransform _enemyTemplate;
    [SerializeField] private CanvasGroup _killGroup;
    [SerializeField] private Text _killName;
    [SerializeField] private Text _killCount;
    [SerializeField, Min(0.05f)] private float _refreshInterval = 0.2f;
    [SerializeField, Min(1f)] private float _maxLabelDistance = 140f;
    [SerializeField, Min(0f)] private float _labelHeight = 2.5f;
    [SerializeField, Min(0.1f)] private float _killDuration = 2.8f;

    private readonly Dictionary<ControlUnit, EnemyView> _views = new Dictionary<ControlUnit, EnemyView>();
    private readonly Stack<EnemyView> _pool = new Stack<EnemyView>();
    private readonly List<ControlUnit> _remove = new List<ControlUnit>();
    private readonly HashSet<ControlUnit> _seen = new HashSet<ControlUnit>();
    private readonly RaycastHit[] _occlusionHits = new RaycastHit[64];
    private Camera _camera;
    private float _nextRefresh;
    private float _killUntil;
    private int _kills;

    private sealed class EnemyView
    {
        public RectTransform root;
        public Text name;
        public RectTransform fill;
        public Text value;
        public float health;
        public float maximum;
    }

    private void OnEnable()
    {
        ResetHud();
    }

    private void OnDisable()
    {
        foreach (EnemyView view in _views.Values) Recycle(view);
        _views.Clear();
        if (_killGroup != null) _killGroup.gameObject.SetActive(false);
    }

    public void ResetHud()
    {
        _kills = 0;
        _killUntil = 0f;
        _nextRefresh = 0f;
        if (_killGroup != null) _killGroup.gameObject.SetActive(false);
    }

    public void ShowPlayerKill(string enemyName)
    {
        if (!isActiveAndEnabled || PlayManager.instance == null || !PlayManager.instance.playMode) return;

        _kills++;
        _killName.text = string.IsNullOrWhiteSpace(enemyName) ? "ENEMY DESTROYED" : enemyName;
        _killCount.text = $"KILLS  {_kills:00}";
        _killUntil = Time.unscaledTime + _killDuration;
        _killGroup.alpha = 0f;
        _killGroup.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode) return;
        _camera = PlayManager.instance.mainCamera != null ? PlayManager.instance.mainCamera : Camera.main;
        if (_camera == null || _overlay == null || _enemyTemplate == null) return;

        if (Time.unscaledTime >= _nextRefresh)
        {
            RefreshEnemies();
            _nextRefresh = Time.unscaledTime + _refreshInterval;
        }

        foreach (KeyValuePair<ControlUnit, EnemyView> pair in _views)
        {
            ControlUnit unit = pair.Key;
            EnemyView view = pair.Value;
            if (unit == null || unit.cockpit == null) continue;
            Vector3 world = unit.cockpit.transform.position + Vector3.up * _labelHeight;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            bool visible = screen.z > 0f && screen.x > 0f && screen.x < Screen.width
                && screen.y > 0f && screen.y < Screen.height
                && (world - _camera.transform.position).sqrMagnitude < _maxLabelDistance * _maxLabelDistance
                && !IsOccluded(world, unit);
            view.root.gameObject.SetActive(visible);
            if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, null, out Vector2 local))
                view.root.anchoredPosition = local;
        }

        if (_killGroup != null && _killGroup.gameObject.activeSelf)
        {
            float remaining = _killUntil - Time.unscaledTime;
            if (remaining <= 0f) _killGroup.gameObject.SetActive(false);
            else _killGroup.alpha = Mathf.Min(1f, (_killDuration - remaining) * 5f, remaining * 2f);
        }
    }

    private bool IsOccluded(Vector3 target, ControlUnit unit)
    {
        Vector3 origin = _camera.transform.position;
        Vector3 delta = target - origin;
        float distance = delta.magnitude;
        if (distance < 0.01f) return false;
        int count = Physics.RaycastNonAlloc(origin, delta / distance, _occlusionHits, distance - 0.01f,
            ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider collider = _occlusionHits[i].collider;
            if (collider != null && !collider.transform.IsChildOf(unit.transform)) return true;
        }
        return false;
    }

    private void RefreshEnemies()
    {
        _seen.Clear();
        foreach (ControlUnit unit in PlayManager.instance.GetControlUnits())
        {
            if (unit == null || unit.faction != UnitFaction.Enemy || !unit.HasValidCockpit) continue;
            _seen.Add(unit);
            if (!_views.TryGetValue(unit, out EnemyView view))
            {
                view = _pool.Count > 0 ? _pool.Pop() : CreateView();
                _views.Add(unit, view);
                EnemyIdentity identity = unit.cockpit.GetComponent<EnemyIdentity>();
                view.name.text = identity != null ? identity.DisplayName : unit.name;
                view.maximum = identity != null && identity.SpawnMaxHealth > 0f
                    ? identity.SpawnMaxHealth : 0f;
            }

            unit.TryGetTotalDurability(out float health, out float maximum);

            view.health = health;
            if (view.maximum <= 0f) view.maximum = maximum;
            float ratio = view.maximum > 0f ? Mathf.Clamp01(health / view.maximum) : 0f;
            view.fill.anchorMax = new Vector2(ratio, 1f);
            view.value.text = $"{health:0} / {view.maximum:0}";
        }

        _remove.Clear();
        foreach (ControlUnit unit in _views.Keys)
            if (unit == null || !_seen.Contains(unit)) _remove.Add(unit);
        foreach (ControlUnit unit in _remove)
        {
            Recycle(_views[unit]);
            _views.Remove(unit);
        }
    }

    private EnemyView CreateView()
    {
        RectTransform root = Instantiate(_enemyTemplate, _overlay);
        root.name = "Enemy Nameplate";
        root.SetSiblingIndex(1);
        return new EnemyView
        {
            root = root,
            name = root.Find("Name").GetComponent<Text>(),
            fill = (RectTransform)root.Find("Bar/Fill"),
            value = root.Find("Value").GetComponent<Text>()
        };
    }

    private void Recycle(EnemyView view)
    {
        view.root.gameObject.SetActive(false);
        _pool.Push(view);
    }
}
