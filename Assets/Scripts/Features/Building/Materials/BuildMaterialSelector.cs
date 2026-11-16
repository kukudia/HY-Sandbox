// 职责：将已保存材质下拉框连接到当前建造选择，显示可用结构材质及成本提示。
// 入口：Select()；只影响随后放置的方块，不改已放置实体。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
public sealed class BuildMaterialSelector : MonoBehaviour
{
    private Dropdown dropdown;
    private string[] ids;
    private void OnEnable()
    {
        dropdown = GetComponent<Dropdown>();
        BlockMaterialCatalog catalog = BlockMaterialCatalog.Instance;
        if (catalog == null)
        {
            dropdown.interactable = false;
            return;
        }

        var labels = new List<string>();
        ids = new string[catalog.Materials.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            var item = catalog.Materials[i];
            ids[i] = item.id;
            labels.Add($"{item.id.ToUpperInvariant()}  ·  {item.coinsPerCell}/m³");
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(labels);
        int selected = BuildManager.instance != null ? System.Array.IndexOf(ids, BuildManager.instance.CurrentMaterialId) : 0;
        dropdown.SetValueWithoutNotify(Mathf.Max(0, selected));
        dropdown.onValueChanged.AddListener(Select);
    }

    private void OnDisable()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(Select);
        }
    }

    public void Select(int index)
    {
        if (ids != null && index >= 0 && index < ids.Length)
        {
            BuildManager.instance?.SelectMaterial(ids[index]);
        }
    }
}
