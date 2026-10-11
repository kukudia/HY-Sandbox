// 职责：展示单个建造条目的图标、选择反馈与悬停参数。
// 入口：SetSelected() / OnPointerEnter() / OnPointerExit()。
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BuildPaletteItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField]
    private string _blockName;
    [SerializeField]
    private string _info;
    [SerializeField]
    private string _description;
    [SerializeField, Range(1, 5)]
    private int _category = 1;
    [SerializeField]
    private Image _outline;
    [SerializeField]
    private BuildPalette _palette;
    public string BlockName => _blockName;
    public string BlockInfo => _info;
    public int Category => _category;

    private void Awake()
    {
        GameObject prefab = Resources.Load<GameObject>("Blocks/" + _blockName);
        if (prefab != null && prefab.TryGetComponent(out Block block))
        {
            _info = block.GetDisplayInfo();
        }
    }

    public void BindPalette(BuildPalette palette)
    {
        _palette = palette;
        GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => BuildManager.instance.SetCurrentBlockResource("Blocks/" + _blockName));
    }

    public void SetSelected(bool selected, Color color)
    {
        _outline.color = selected ? color : Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _palette.ShowTooltip(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        BuildPaletteItem item = _palette.GetSelectedItem();
        if (item != null)
        {
            _palette.ShowTooltip(item);
        }
        else
        {
            _palette.HideTooltip(this);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        _palette.ShowTooltip(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _palette.HideTooltip(this);
    }

    private void OnDisable()
    {
        if (_palette != null)
        {
            _palette.HideTooltip(this);
        }
    }
}
