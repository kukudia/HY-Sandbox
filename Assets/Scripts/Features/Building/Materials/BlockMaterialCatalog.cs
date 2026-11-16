// 职责：保存结构材质稳定ID、每立方米物理/造价参数及共享表现资产。
// 入口：Find()/Instance；空ID代表旧蓝图原始Prefab参数，未知ID必须拒绝。
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/Block Material Catalog")]
public sealed class BlockMaterialCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Definition
    {
        public string id;
        public string displayName;
        [Min(0.01f)]
        public float density = 1f;
        [Min(1f)]
        public float durabilityPerCell = 100f;
        [Min(1)]
        public int coinsPerCell = 5;
        public Material surface;
        public MaterialBreakEffect breakEffect;
    }

    [SerializeField]
    private Definition[] materials = Array.Empty<Definition>();
    public Definition[] Materials => materials;

    private static BlockMaterialCatalog instance;
    public static BlockMaterialCatalog Instance => instance != null ? instance : instance = Resources.Load<BlockMaterialCatalog>("Building/BlockMaterials");

    public Definition Find(string id)
    {
        foreach (Definition item in materials)
        {
            if (item != null && string.Equals(item.id, id, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    public static Definition Resolve(string id)
    {
        return string.IsNullOrEmpty(id) ? null : Instance != null ? Instance.Find(id) : null;
    }

    public static long Price(BlockData block)
    {
        Definition material = Resolve(block.materialId);
        if (!string.IsNullOrEmpty(block.materialId) && material == null)
        {
            throw new ArgumentException("Unknown structural material: " + block.materialId);
        }

        return material != null ? (long)block.x * block.y * block.z * material.coinsPerCell : 0;
    }
}
