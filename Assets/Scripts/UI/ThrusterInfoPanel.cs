using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ThrusterInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _window;
    [SerializeField] private Button[] _tabs;
    [SerializeField] private Text[] _tabLabels;
    [SerializeField] private RectTransform _content;
    [SerializeField] private RectTransform _rowTemplate;
    [SerializeField] private Text _emptyState;
    [SerializeField] private Image _tabIndicator;
    [SerializeField] private float _updateInterval = 0.1f;

    private readonly List<Row> _rows = new List<Row>();
    private readonly List<Thruster> _thrusters = new List<Thruster>();
    private ControlUnit _owner;
    private int _category;
    private float _nextRefresh;

    private sealed class Row
    {
        public RectTransform root;
        public Text name;
        public Text value;
        public RectTransform fill;
    }

    public bool IsOpen => _window != null && _window.activeSelf;

    private void Awake()
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            int category = i;
            _tabs[i].onClick.AddListener(() => SelectCategory(category));
        }
        if (_window != null) _window.SetActive(false);
    }

    private void OnDisable()
    {
        if (_window != null) _window.SetActive(false);
    }

    public void Toggle()
    {
        if (_window == null) return;
        _window.SetActive(!_window.activeSelf);
        if (_window.activeSelf)
        {
            SelectCategory(_category);
            _nextRefresh = 0f;
        }
    }

    public void Close()
    {
        if (_window != null) _window.SetActive(false);
    }

    public void SelectCategory(int category)
    {
        _category = Mathf.Clamp(category, 0, 2);
        for (int i = 0; i < _tabs.Length; i++)
        {
            _tabLabels[i].color = i == _category ? new Color(0.31f, 0.88f, 0.95f) : Color.white;
            _tabs[i].image.color = i == _category
                ? new Color(0.08f, 0.19f, 0.22f, 1f) : new Color(0.045f, 0.08f, 0.1f, 1f);
        }
        if (_tabIndicator != null)
        {
            RectTransform indicator = _tabIndicator.rectTransform;
            indicator.anchorMin = new Vector2(_category / 3f, 0f);
            indicator.anchorMax = new Vector2((_category + 1f) / 3f, 0f);
            indicator.offsetMin = Vector2.zero;
            indicator.offsetMax = new Vector2(0f, 3f);
        }
        _nextRefresh = 0f;
    }

    private void Update()
    {
        if (!IsOpen || PlayManager.instance == null || !PlayManager.instance.playMode) return;
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + _updateInterval;
        ControlUnit owner = PlayManager.instance.blocksParent != null
            ? PlayManager.instance.blocksParent.GetComponent<ControlUnit>() : null;
        if (owner != _owner)
        {
            _owner = owner;
            _thrusters.Clear();
        }
        _thrusters.Clear();
        if (owner != null && owner.HasValidCockpit && owner.faction == UnitFaction.Player)
        {
            foreach (Thruster thruster in owner.GetComponentsInChildren<Thruster>())
                if (thruster != null && thruster.IsPlayerControllable && MatchesCategory(thruster))
                    _thrusters.Add(thruster);
        }
        _emptyState.gameObject.SetActive(_thrusters.Count == 0);
        for (int i = 0; i < _thrusters.Count; i++)
        {
            if (i == _rows.Count) _rows.Add(CreateRow());
            UpdateRow(_rows[i], _thrusters[i], i);
        }
        for (int i = _thrusters.Count; i < _rows.Count; i++) _rows[i].root.gameObject.SetActive(false);
        _content.sizeDelta = new Vector2(0f, Mathf.Max(0f, _thrusters.Count * 62f));
    }

    private bool MatchesCategory(Thruster thruster)
    {
        return _category == 0 ? thruster is MainThruster
            : _category == 1 ? thruster is UniversalThruster : thruster is HoverThruster;
    }

    private Row CreateRow()
    {
        RectTransform root = Instantiate(_rowTemplate, _content);
        root.name = "Thruster Row";
        root.gameObject.SetActive(true);
        return new Row
        {
            root = root,
            name = root.Find("Name").GetComponent<Text>(),
            value = root.Find("Value").GetComponent<Text>(),
            fill = (RectTransform)root.Find("Track/Fill")
        };
    }

    private void UpdateRow(Row row, Thruster thruster, int index)
    {
        row.root.gameObject.SetActive(true);
        row.root.anchoredPosition = new Vector2(0f, -index * 62f);
        float efficiency = thruster.PowerEfficiency;
        Color state = efficiency <= 0f ? new Color(1f, 0.36f, 0.33f)
            : efficiency < 0.999f ? new Color(1f, 0.81f, 0.34f) : Color.white;
        row.name.text = thruster.name;
        row.name.color = state;
        row.value.text = $"{thruster.thrust:0.0} / {thruster.maxThrust:0.0}";
        row.value.color = state;
        row.fill.anchorMax = new Vector2(thruster.maxThrust > 0f
            ? Mathf.Clamp01(thruster.thrust / thruster.maxThrust) : 0f, 1f);
        row.fill.GetComponent<Image>().color = efficiency <= 0f ? state : new Color(0.31f, 0.88f, 0.95f);
    }
}
