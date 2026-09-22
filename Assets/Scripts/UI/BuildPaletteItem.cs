using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BuildPaletteItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private string _blockName;
    [SerializeField, Range(1, 5)] private int _category = 1;
    [SerializeField] private Image _outline;
    [SerializeField] private BuildPalette _palette;
    public string BlockName => _blockName;
    public int Category => _category;
    public void SetSelected(bool selected, Color color) { _outline.color = selected ? color : Color.white; }
    public void OnPointerEnter(PointerEventData eventData) { _palette.ShowTooltip(this); }
    public void OnPointerExit(PointerEventData eventData) { _palette.HideTooltip(this); }
    public void OnSelect(BaseEventData eventData) { _palette.ShowTooltip(this); }
    public void OnDeselect(BaseEventData eventData) { _palette.HideTooltip(this); }
    private void OnDisable() { if (_palette != null) _palette.HideTooltip(this); }
}
