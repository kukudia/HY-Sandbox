using UnityEngine;
using UnityEngine.UI;

/// <summary>Saved scene catalog; asset discovery and thumbnail rendering happen only in the Editor.</summary>
public sealed class BuildPalette : MonoBehaviour
{
    [SerializeField] private Text _hoveredName;
    [SerializeField] private Text _hoveredInfo;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private Button[] _tabs;
    [SerializeField] private Color _selectedColor = new Color(0.35f, 0.85f, 1f);
    [SerializeField] private BuildPaletteItem[] _items;
    [SerializeField] private string _selection;
    [SerializeField] private BuildPaletteItem _selected;
    [SerializeField] private BuildPaletteItem _hovered;

    private void Awake() { _items = GetComponentsInChildren<BuildPaletteItem>(true); }
    private void OnEnable() { SelectCategory(0); }
    private void OnDisable() { HideTooltip(null); }

    private void LateUpdate()
    {
        string selection = BuildManager.instance != null ? BuildManager.instance.currentBlockResourcePath : null;
        if (_selection == selection) return;
        _selection = selection;
        _selected = null;
        foreach (BuildPaletteItem item in _items)
        {
            bool selected = selection == "Blocks/" + item.BlockName;
            item.SetSelected(selected, _selectedColor);
            if (selected)
            {
                _selected = item;
            }
        }
    }

    public void SelectCategory(int category)
    {
        if (_items == null) _items = GetComponentsInChildren<BuildPaletteItem>(true);
        HideTooltip(null);
        foreach (BuildPaletteItem item in _items) item.gameObject.SetActive(category == 0 || item.Category == category);
        for (int i = 0; i < _tabs.Length; i++) _tabs[i].image.color = i == category ? _selectedColor : Color.white;
        Canvas.ForceUpdateCanvases();
        _scroll.StopMovement();
        _scroll.verticalNormalizedPosition = 1f;
        Vector2 position = _scroll.content.anchoredPosition;
        position.y = 0f;
        _scroll.content.anchoredPosition = position;
    }

    public void ShowTooltip(BuildPaletteItem item)
    {
        _hovered = item;
        _hoveredName.text = item.BlockName;
        _hoveredInfo.text = item.BlockInfo;
        _hoveredName.gameObject.SetActive(true);
        _hoveredInfo.gameObject.SetActive(true);
    }

    public void HideTooltip(BuildPaletteItem item)
    {
        if (item != null && _hovered != item) return;
        _hovered = null;
        _hoveredName.gameObject.SetActive(false);
        _hoveredInfo.gameObject.SetActive(false);
    }

    public BuildPaletteItem GetSelectedItem()
    {
        if (string.IsNullOrEmpty(_selection))
        {
            return null;
        }
        else
        {
            return _selected;
        }
    }
}
