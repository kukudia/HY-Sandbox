// 职责：显示保存的部件/材质并处理点击、目录拖入、槽位互换和右键清空。
// 入口：Bind()/Show()/OnDrop()；所有修改委托BuildHotbar验证和保存。
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BuildHotbarSlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField]
    private Image icon;
    [SerializeField]
    private Image border;
    [SerializeField]
    private Text label;
    [SerializeField]
    private Text shortcut;
    private BuildHotbar owner;
    private int index;
    private bool dragging;
    public void Bind(BuildHotbar hotbar, int slot)
    {
        owner = hotbar;
        index = slot;
        shortcut.text = (slot + 1).ToString();
    }

    public void Show(BuildHotbarCatalog.Slot slot, Sprite preview, bool selected)
    {
        icon.sprite = preview;
        icon.enabled = preview != null;
        string path = slot?.resourcePath;
        label.text = string.IsNullOrEmpty(path) ? "EMPTY" : path.Substring(path.LastIndexOf('/') + 1) + (string.IsNullOrEmpty(slot.materialId) ? "" : "\n" + slot.materialId.ToUpperInvariant());
        border.color = selected ? new Color(.3f, .85f, 1f) : new Color(.3f, .4f, .46f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dragging || owner == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            owner.Clear(index);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            owner.Choose(index);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        owner?.PreviewDrag(owner.GetSlot(index)?.resourcePath, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.PreviewDrag(owner.GetSlot(index)?.resourcePath, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        owner?.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null || eventData.pointerDrag == null)
        {
            return;
        }

        BuildHotbarSlot source = eventData.pointerDrag.GetComponent<BuildHotbarSlot>();
        if (source != null && source.owner == owner)
        {
            owner.Swap(source.index, index);
            return;
        }

        BuildPaletteItem item = eventData.pointerDrag.GetComponent<BuildPaletteItem>();
        if (item != null)
        {
            owner.Assign(
                index,
                "Blocks/" + item.BlockName,
                BuildManager.instance != null ? BuildManager.instance.CurrentMaterialId : "metal");
        }
    }
}
