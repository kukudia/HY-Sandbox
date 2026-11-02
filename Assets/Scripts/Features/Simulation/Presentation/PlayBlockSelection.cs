// 职责：保存并恢复选中方块的材质，维护选中视觉效果；不参与连接或组重建。
// 入口：Highlight() / Clear()；由 PlayManager 负责选中状态及 Rack 的玩法副作用。
using System.Collections.Generic;
using UnityEngine;

internal sealed class PlayBlockSelection
{
    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
    internal void Highlight(Block block, Material highlightMaterial)
    {
        if (highlightMaterial != null)
        {
            Transform modelRoot = block.transform.Find("Model");
            Renderer[] renderers = modelRoot != null ? modelRoot.GetComponentsInChildren<Renderer>(true) : block.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                Material[] originalMaterials = renderer.sharedMaterials;
                _originalMaterials[renderer] = originalMaterials;
                Material[] highlightedMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < highlightedMaterials.Length; i++)
                {
                    highlightedMaterials[i] = highlightMaterial;
                }

                renderer.sharedMaterials = highlightedMaterials;
            }
        }

        VisualEffectsManager.TryShowBlockSelection(block);
    }

    internal void Clear(Block block)
    {
        VisualEffectsManager.TryClearBlockSelection(block);
        foreach (KeyValuePair<Renderer, Material[]> entry in _originalMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.sharedMaterials = entry.Value;
            }
        }

        _originalMaterials.Clear();
    }
}
