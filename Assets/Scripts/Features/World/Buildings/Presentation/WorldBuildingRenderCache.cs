// 职责：将世界建筑同父刚性装饰网格按材质合并，并在本会话的同类模块间共享。
// 入口：Apply()/Clear()；只替换表现，保留方块、碰撞、损坏及可移动炮塔/机械层级。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class WorldBuildingRenderCache
{
    private readonly Dictionary<string, Visual> visuals = new Dictionary<string, Visual>();
    internal void Apply(Block block)
    {
        if (block.GetComponentInChildren<Animator>(true) != null || block.GetComponentInChildren<Animation>(true) != null || block.GetComponentInChildren<LODGroup>(true) != null)
        {
            return;
        }

        Transform model = block.transform.Find("Model");
        if (model == null)
        {
            return;
        }

        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>(true);
        IndustrialPartMotion[] motions = model.GetComponentsInChildren<IndustrialPartMotion>(true);
        ProjectileTurret projectile = block.GetComponent<ProjectileTurret>();
        var groups = new Dictionary<Transform, List<MeshRenderer>>();
        foreach (MeshRenderer renderer in renderers)
        {
            bool controlled = projectile != null && projectile.MovesVisual(renderer.transform);
            foreach (IndustrialPartMotion motion in motions)
            {
                controlled |= motion.ControlsVisual(renderer.transform, renderer);
            }

            // 仅合并同一父节点下的刚性兄弟，炮塔轴/炮管、旋转器和发光Renderer保留原身份。
            if (renderer.transform == model || controlled || !renderer.enabled || renderer.HasPropertyBlock() || !IsVisible(renderer.transform, model))
            {
                continue;
            }

            if (!groups.TryGetValue(renderer.transform.parent, out List<MeshRenderer> siblings))
            {
                siblings = new List<MeshRenderer>();
                groups.Add(renderer.transform.parent, siblings);
            }

            siblings.Add(renderer);
        }

        foreach (KeyValuePair<Transform, List<MeshRenderer>> group in groups)
        {
            if (group.Value.Count < 2)
            {
                continue;
            }

            string key = block.resourcePath + ":" + block.x + ":" + block.y + ":" + block.z + ":" + RelativePath(group.Key, model);
            if (!visuals.TryGetValue(key, out Visual visual))
            {
                visual = Build(group.Key, group.Value.ToArray());
                visuals.Add(key, visual);
            }

            if (visual == null)
            {
                continue;
            }

            foreach (MeshRenderer renderer in group.Value)
            {
                renderer.enabled = false;
            }

            var combined = new GameObject("World combined visual");
            combined.layer = group.Value[0].gameObject.layer;
            combined.transform.SetParent(group.Key, false);
            combined.AddComponent<MeshFilter>().sharedMesh = visual.Mesh;
            MeshRenderer output = combined.AddComponent<MeshRenderer>();
            output.sharedMaterials = visual.Materials;
            output.shadowCastingMode = group.Value[0].shadowCastingMode;
            output.receiveShadows = group.Value[0].receiveShadows;
        }
    }

    private static bool IsVisible(Transform part, Transform model)
    {
        for (Transform current = part; current != model.parent; current = current.parent)
        {
            if (!current.gameObject.activeSelf)
            {
                return false;
            }
        }

        return true;
    }

    private static string RelativePath(Transform parent, Transform model)
    {
        string path = "";
        for (Transform current = parent; current != model; current = current.parent)
        {
            path = current.GetSiblingIndex() + "/" + path;
        }

        return path;
    }

    private static Visual Build(Transform model, MeshRenderer[] renderers)
    {
        var groups = new Dictionary<Material, List<CombineInstance>>();
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.shadowCastingMode != renderers[0].shadowCastingMode || renderer.receiveShadows != renderers[0].receiveShadows || renderer.renderingLayerMask != renderers[0].renderingLayerMask)
            {
                return null;
            }

            if (!renderer.enabled || !renderer.gameObject.activeSelf || !renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                continue;
            }

            Mesh source = filter.sharedMesh;
            if (!source.isReadable || renderer.HasPropertyBlock())
            {
                return null;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials.Length < source.subMeshCount)
            {
                return null;
            }

            for (int i = 0; i < source.subMeshCount; i++)
            {
                if (materials[i] == null)
                {
                    return null;
                }

                if (!groups.TryGetValue(materials[i], out List<CombineInstance> entries))
                {
                    entries = new List<CombineInstance>();
                    groups.Add(materials[i], entries);
                }

                entries.Add(new CombineInstance { mesh = source, subMeshIndex = i, transform = model.worldToLocalMatrix * renderer.localToWorldMatrix });
            }
        }

        if (groups.Count == 0)
        {
            return null;
        }

        var parts = new List<CombineInstance>();
        var outputMaterials = new List<Material>();
        foreach (KeyValuePair<Material, List<CombineInstance>> group in groups)
        {
            var part = new Mesh
            {
                indexFormat = IndexFormat.UInt32
            };
            part.CombineMeshes(group.Value.ToArray(), true, true);
            parts.Add(new CombineInstance { mesh = part, transform = Matrix4x4.identity });
            outputMaterials.Add(group.Key);
        }

        var mesh = new Mesh
        {
            name = "World shared module visual",
            indexFormat = IndexFormat.UInt32
        };
        mesh.CombineMeshes(parts.ToArray(), false, false);
        foreach (CombineInstance part in parts)
        {
            Object.Destroy(part.mesh);
        }

        return new Visual
        {
            Mesh = mesh,
            Materials = outputMaterials.ToArray()
        };
    }

    internal void Clear()
    {
        foreach (Visual visual in visuals.Values)
        {
            if (visual != null)
            {
                Object.Destroy(visual.Mesh);
            }
        }

        visuals.Clear();
    }

    private sealed class Visual
    {
        internal Mesh Mesh;
        internal Material[] Materials;
    }
}
