// 职责：协调建造快捷栏的输入、稳定ID配置与选择；不修改蓝图、金币或货仓。
// 入口：Choose() / Assign() / Swap() / Clear()；仅建造且无文本输入时响应按键。
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class BuildHotbar : MonoBehaviour
{
    public const int Capacity = 8;
    public const string PreferenceKey = "HY.BuildHotbar.v1";
    [SerializeField]
    private BuildHotbarCatalog catalog;
    [SerializeField]
    private BuildHotbarSlot[] views;
    [SerializeField]
    private Text details;
    [SerializeField]
    private Image dragPreview;
    private BuildHotbarCatalog.Slot[] slots;
    private string displayedPath;
    private string displayedMaterial;
    [Serializable]
    private sealed class Saved
    {
        public int version = 1;
        public BuildHotbarCatalog.Slot[] slots;
    }

    public BuildHotbarCatalog.Slot GetSlot(int index) => slots != null && index >= 0 && index < slots.Length ? slots[index] : null;
    public BuildHotbarCatalog Catalog => catalog;
    public bool CanUse => BuildManager.instance != null && BuildManager.instance.isActiveAndEnabled && !BuildManager.instance.IsLoadingBlocks && InputManager.instance != null && InputManager.instance.keyInput && (PlayManager.instance == null || !PlayManager.instance.playMode);

    public void PreviewDrag(string path, PointerEventData pointer)
    {
        if (dragPreview == null)
        {
            return;
        }

        Sprite sprite = CanUse ? catalog?.Find(path)?.icon : null;
        dragPreview.sprite = sprite;
        dragPreview.gameObject.SetActive(sprite != null);
        if (sprite != null)
        {
            RectTransform parent = (RectTransform)dragPreview.transform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pointer.position, pointer.pressEventCamera, out Vector2 point))
            {
                dragPreview.rectTransform.localPosition = point;
            }
        }
    }

    public void EndDrag()
    {
        if (dragPreview != null)
        {
            dragPreview.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        EndDrag();
    }

    private void Awake()
    {
        slots = new BuildHotbarCatalog.Slot[Capacity];
        for (int i = 0; i < slots.Length; i++)
        {
            BuildHotbarCatalog.Slot source = catalog != null && i < catalog.Defaults.Length ? catalog.Defaults[i] : null;
            slots[i] = Copy(source);
        }

        if (PlayerPrefs.HasKey(PreferenceKey))
        {
            try
            {
                Saved saved = JsonUtility.FromJson<Saved>(PlayerPrefs.GetString(PreferenceKey));
                if (saved != null && saved.version == 1 && saved.slots != null)
                {
                    for (int i = 0; i < slots.Length && i < saved.slots.Length; i++)
                    {
                        slots[i] = Valid(saved.slots[i]) ? Copy(saved.slots[i]) : new BuildHotbarCatalog.Slot();
                    }
                }
            }
            catch (ArgumentException)
            {
                // A malformed UI preference must never block opening a real blueprint.
            }
        }

        for (int i = 0; i < views.Length; i++)
        {
            views[i].Bind(this, i);
        }

        Refresh();
    }

    private static BuildHotbarCatalog.Slot Copy(BuildHotbarCatalog.Slot source)
    {
        return new BuildHotbarCatalog.Slot
        {
            resourcePath = source?.resourcePath,
            materialId = source?.materialId
        };
    }

    private bool Valid(BuildHotbarCatalog.Slot value)
    {
        if (value == null || string.IsNullOrEmpty(value.resourcePath))
        {
            return true;
        }

        if (catalog == null || catalog.Find(value.resourcePath) == null)
        {
            return false;
        }

        GameObject prefab = Resources.Load<GameObject>(value.resourcePath);
        return prefab != null && prefab.GetComponent<Block>() != null && (string.IsNullOrEmpty(value.materialId) || (prefab.GetComponent<BlockMaterial>() != null && BlockMaterialCatalog.Resolve(value.materialId) != null));
    }

    private void Update()
    {
        if (!CanUse)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && !TextInputFocus.IsEditingText && !keyboard.ctrlKey.isPressed && !keyboard.altKey.isPressed)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                {
                    continue;
                }

                if (keyboard.shiftKey.isPressed)
                {
                    Assign(i, BuildManager.instance.currentBlockResourcePath, BuildManager.instance.CurrentMaterialId);
                }
                else
                {
                    Choose(i);
                }
            }
        }

        string path = BuildManager.instance.currentBuildType == BuildType.Build ? BuildManager.instance.currentBlockResourcePath : null;
        string material = BuildManager.instance.CurrentMaterialId;
        if (path != displayedPath || material != displayedMaterial)
        {
            Refresh();
        }
    }

    public bool Choose(int index)
    {
        BuildHotbarCatalog.Slot slot = GetSlot(index);
        if (!CanUse || slot == null || string.IsNullOrEmpty(slot.resourcePath) || !Valid(slot))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(slot.materialId))
        {
            BuildManager.instance.SelectMaterial(slot.materialId);
        }

        BuildManager.instance.SetCurrentBlockResource(slot.resourcePath);
        Refresh();
        return true;
    }

    public bool Assign(int index, string path, string materialId)
    {
        if (!CanUse || GetSlot(index) == null || string.IsNullOrEmpty(path))
        {
            return false;
        }

        GameObject prefab = Resources.Load<GameObject>(path);
        var value = new BuildHotbarCatalog.Slot
        {
            resourcePath = path,
            materialId = prefab != null && prefab.GetComponent<BlockMaterial>() != null ? materialId : string.Empty
        };
        if (!Valid(value))
        {
            return false;
        }

        slots[index] = value;
        Save();
        return true;
    }

    public void Swap(int first, int second)
    {
        if (!CanUse || GetSlot(first) == null || GetSlot(second) == null)
        {
            return;
        }

        (slots[first], slots[second]) = (slots[second], slots[first]);
        Save();
    }

    public void Clear(int index)
    {
        if (CanUse && GetSlot(index) != null)
        {
            slots[index] = new BuildHotbarCatalog.Slot();
            Save();
        }
    }

    private void Save()
    {
        PlayerPrefs.SetString(PreferenceKey, JsonUtility.ToJson(new Saved { slots = slots }));
        PlayerPrefs.Save();
        Refresh();
    }

    private void Refresh()
    {
        BuildManager build = BuildManager.instance;
        displayedPath = build != null && build.currentBuildType == BuildType.Build ? build.currentBlockResourcePath : null;
        displayedMaterial = build != null ? build.CurrentMaterialId : null;
        for (int i = 0; i < views.Length; i++)
        {
            BuildHotbarCatalog.Slot slot = GetSlot(i);
            GameObject prefab = string.IsNullOrEmpty(slot?.resourcePath) ? null : Resources.Load<GameObject>(slot.resourcePath);
            bool structural = prefab != null && prefab.GetComponent<BlockMaterial>() != null;
            views[i].Show(
                slot,
                catalog?.Find(slot?.resourcePath)?.icon,
                !string.IsNullOrEmpty(slot?.resourcePath) && slot.resourcePath == displayedPath && (!structural || slot.materialId == displayedMaterial));
        }

        GameObject current = string.IsNullOrEmpty(displayedPath) ? null : Resources.Load<GameObject>(displayedPath);
        Block block = current != null ? current.GetComponent<Block>() : null;
        BlockMaterialCatalog.Definition definition = current != null && current.GetComponent<BlockMaterial>() != null ? BlockMaterialCatalog.Resolve(displayedMaterial) : null;
        float volume = block != null ? (float)block.x * block.y * block.z : 0f;
        details.text = block == null ? "QUICK BUILD  ·  1–8 SELECT  /  SHIFT + 1–8 PIN CURRENT" : definition != null ? $"{current.name} · {definition.id.ToUpperInvariant()}   HP {volume * definition.durabilityPerCell:0}   MASS {volume * definition.density:0.#}   COINS {volume * definition.coinsPerCell:0}" : $"{current.name}   COINS {block.cost}  ·  MODULE MATERIAL FIXED";
    }
}
