// 职责：以缓存推进器列表展示关注舰的真实推力和供电状态，支持驾驶、AI与返航导引。
// 入口：BindOwner() / Toggle() / Close() / SelectCategory()；不写推进器参数。
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ThrusterInfoPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject _window;
    [SerializeField]
    private Button[] _tabs;
    [SerializeField]
    private TMP_Text[] _tabLabels;
    [SerializeField]
    private RectTransform _content;
    [SerializeField]
    private RectTransform _rowTemplate;
    [SerializeField]
    private TMP_Text _emptyState;
    [SerializeField]
    private Image _tabIndicator;
    [SerializeField]
    private TMP_Text _ownerLabel;
    [SerializeField]
    private float _updateInterval = .1f;
    private readonly List<Row> rows = new List<Row>();
    private readonly List<Thruster> thrusters = new List<Thruster>();
    private ControlUnit boundOwner;
    private bool explicitOwner;
    private bool open;
    private int category;
    private float nextRefresh;
    private float alpha;
    public bool IsOpen => open;

    private sealed class Row
    {
        public RectTransform root;
        public TMP_Text name;
        public TMP_Text value;
        public RectTransform fill;
        public float target;
    }

    private void Awake()
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            int index = i;
            _tabs[i].onClick.AddListener(() => SelectCategory(index));
        }

        SelectCategory(0);
        Close();
        _window.GetComponent<CanvasGroup>().alpha = 0f;
    }

    public void BindOwner(ControlUnit owner, string status)
    {
        explicitOwner = true;
        if (boundOwner != owner)
        {
            nextRefresh = 0f;
        }

        boundOwner = owner;
        _ownerLabel.text = owner != null ? owner.name + " / " + status : "SELECT AN ALLIED SHIP";
    }

    public void Toggle()
    {
        open = !open;
        nextRefresh = 0f;
    }

    public void Close()
    {
        open = false;
    }

    private void OnDisable()
    {
        open = false;
        alpha = 0f;
        explicitOwner = false;
        boundOwner = null;
    }

    public void SelectCategory(int selected)
    {
        category = Mathf.Clamp(selected, 0, 2);
        for (int i = 0; i < _tabs.Length; i++)
        {
            _tabLabels[i].color = i == category ? new Color(.3f, .88f, .95f) : Color.white;
        }

        nextRefresh = 0f;
    }

    private void Update()
    {
        CanvasGroup group = _window.GetComponent<CanvasGroup>();
        alpha = Mathf.MoveTowards(alpha, open ? 1f : 0f, Time.unscaledDeltaTime / .18f);
        group.alpha = alpha;
        group.interactable = group.blocksRaycasts = open;
        ((RectTransform)_window.transform).anchoredPosition = new Vector2(-20f + (1f - alpha) * 30f, -65f);
        if (_tabIndicator != null)
        {
            RectTransform indicator = _tabIndicator.rectTransform;
            float blend = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
            indicator.anchorMin = Vector2.Lerp(indicator.anchorMin, new Vector2(category / 3f, 0f), blend);
            indicator.anchorMax = Vector2.Lerp(indicator.anchorMax, new Vector2((category + 1f) / 3f, 0f), blend);
        }

        foreach (Row row in rows)
        {
            row.fill.anchorMax = new Vector2(Mathf.Lerp(row.fill.anchorMax.x, row.target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime)), 1f);
        }

        if (!open || Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + _updateInterval;
        ControlUnit owner = explicitOwner ? boundOwner : PlayManager.instance != null && PlayManager.instance.blocksParent != null ? PlayManager.instance.blocksParent.GetComponent<ControlUnit>() : null;
        if (!explicitOwner && _ownerLabel != null)
        {
            _ownerLabel.text = owner != null ? owner.name : "NO ACTIVE SHIP";
        }

        thrusters.Clear();
        if (owner != null && owner.HasValidCockpit && owner.faction == UnitFaction.Player)
        {
            foreach (Thruster thruster in owner.Thrusters)
            {
                if (thruster != null && thruster.isActiveAndEnabled && MatchesCategory(thruster))
                {
                    thrusters.Add(thruster);
                }
            }
        }

        _emptyState.gameObject.SetActive(thrusters.Count == 0);
        for (int i = 0; i < thrusters.Count; i++)
        {
            if (i >= rows.Count)
            {
                RectTransform root = Instantiate(_rowTemplate, _content);
                rows.Add(new Row { root = root, name = root.Find("Name").GetComponent<TMP_Text>(), value = root.Find("Value").GetComponent<TMP_Text>(), fill = (RectTransform)root.Find("Track/Fill") });
            }

            Row row = rows[i];
            Thruster thruster = thrusters[i];
            float efficiency = thruster.PowerSupplyEfficiency;
            Color color = efficiency <= 0f ? new Color(1f, .36f, .33f) : efficiency < .999f ? new Color(1f, .81f, .34f) : new Color(.3f, .88f, .95f);
            row.root.gameObject.SetActive(true);
            row.root.anchoredPosition = new Vector2(0f, -i * 66f);
            row.name.text = thruster.name;
            row.name.color = color;
            row.value.text = $"{thruster.thrust:0.0} / {thruster.maxThrust:0.0} N  |  PWR {efficiency:P0}";
            row.value.color = color;
            row.fill.GetComponent<Image>().color = color;
            row.target = thruster.maxThrust > 0f ? Mathf.Clamp01(thruster.thrust / thruster.maxThrust) : 0f;
        }

        for (int i = thrusters.Count; i < rows.Count; i++)
        {
            rows[i].root.gameObject.SetActive(false);
        }

        _content.sizeDelta = new Vector2(0f, thrusters.Count * 66f);
    }

    private bool MatchesCategory(Thruster thruster)
    {
        return category == 0 ? thruster is MainThruster : category == 1 ? thruster is UniversalThruster : thruster is HoverThruster;
    }
}
