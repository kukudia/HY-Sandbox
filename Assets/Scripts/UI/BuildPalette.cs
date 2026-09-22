using UnityEngine;
using UnityEngine.UI;

/// <summary>Saved scene catalog; asset discovery and thumbnail rendering happen only in the Editor.</summary>
public sealed class BuildPalette : MonoBehaviour
{
    [SerializeField] private Text _tooltip;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private Button[] _tabs;
    [SerializeField] private Color _selectedColor = new Color(0.35f, 0.85f, 1f);
    private BuildPaletteItem[] _items;
    private string _selection;
    private BuildPaletteItem _hovered;

    private void Awake() { _items = GetComponentsInChildren<BuildPaletteItem>(true); }
    private void OnEnable() { SelectCategory(0); }
    private void OnDisable() { HideTooltip(null); }

    private void LateUpdate()
    {
        string selection = BuildManager.instance != null ? BuildManager.instance.currentBlockResourcePath : null;
        if (_selection == selection) return;
        _selection = selection;
        foreach (BuildPaletteItem item in _items) item.SetSelected(selection == "Blocks/" + item.BlockName, _selectedColor);
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
        _tooltip.text = item.BlockName;
        _tooltip.gameObject.SetActive(true);
    }

    public void HideTooltip(BuildPaletteItem item)
    {
        if (item != null && _hovered != item) return;
        _hovered = null;
        if (_tooltip != null) _tooltip.gameObject.SetActive(false);
    }
}
