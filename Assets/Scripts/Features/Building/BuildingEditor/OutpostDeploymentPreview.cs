// 职责：在建筑旁展示完整敌机模型、部署线框、朝向及保守包围盒重叠提示。
// 入口：构造后读取Hint；Dispose释放显示对象与临时网格/材质，不实例化敌机玩法组件。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

internal sealed class OutpostDeploymentPreview : IDisposable
{
    private static readonly int[] Edges =
    {
        0,
        1,
        1,
        3,
        3,
        2,
        2,
        0,
        4,
        5,
        5,
        7,
        7,
        6,
        6,
        4,
        0,
        4,
        1,
        5,
        2,
        6,
        3,
        7
    };
    private readonly GameObject root;
    private readonly Material material;
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Transform> labels = new List<Transform>();
    internal string Hint
    {
        get;
    }

    internal OutpostDeploymentPreview(Transform parent, BlockDataList building, int selected)
    {
        root = new GameObject("Outpost deployment preview");
        root.transform.SetParent(parent, false);
        // The building editor saves block poses in world authoring coordinates; its parent
        // prefab has an offset. Match the foundation at world zero instead of inheriting it.
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.name = "Outpost bounds preview (runtime)";
        EnemyOutpostData data = building.outpost;
        var bounds = new Bounds[data.garrison.Count];
        for (int i = 0; i < bounds.Length; i++)
        {
            OutpostGarrisonData guard = data.garrison[i];
            GameObject model = BlueprintModelPreview.Create(guard.blueprint.blocks, root.transform, "Resident model " + guard.name);
            model.transform.SetLocalPositionAndRotation(guard.position, Quaternion.Euler(0f, guard.yaw, 0f));
            bounds[i] = CalculateBounds(
                guard.blueprint.blocks,
                Matrix4x4.TRS(guard.position, Quaternion.Euler(0f, guard.yaw, 0f), Vector3.one));
        }

        OutpostGarrisonData selectedGuard = data.garrison[selected];
        Matrix4x4 selectedPose = Matrix4x4.TRS(selectedGuard.position, Quaternion.Euler(0f, selectedGuard.yaw, 0f), Vector3.one);
        bool overlapsBuilding = OverlapsBuilding(selectedGuard, selectedPose, building.blocks);
        bool overlapsGuard = false;
        for (int i = 0; i < bounds.Length; i++)
        {
            overlapsGuard |= i != selected && bounds[selected].Intersects(bounds[i]);
        }

        for (int i = 0; i < bounds.Length; i++)
        {
            Color color = i == selected ? overlapsBuilding || overlapsGuard ? new Color(1f, 0.35f, 0.2f) : Color.cyan : new Color(0.55f, 0.65f, 0.75f);
            DrawGuard(data.garrison[i], bounds[i], i, i == selected, color);
        }

        Vector3 size = bounds[selected].size;
        Hint = $"Enemy {size.x:0.#} x {size.y:0.#} x {size.z:0.#}m.\nFull models + base; cyan: selected resident.";
        if (overlapsBuilding || overlapsGuard)
        {
            Hint = "Orange: bounds overlap " + (overlapsBuilding ? "building" : "another guard") + " (approximate).\n" + Hint;
        }
    }

    private void DrawGuard(OutpostGarrisonData guard, Bounds bounds, int index, bool selected, Color color)
    {
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        Matrix4x4 deployment = Matrix4x4.TRS(guard.position, Quaternion.Euler(0f, guard.yaw, 0f), Vector3.one);
        AddBox(vertices, indices, Matrix4x4.TRS(bounds.center, Quaternion.identity, bounds.size));
        Vector3 forward = deployment.MultiplyVector(Vector3.forward);
        Vector3 right = deployment.MultiplyVector(Vector3.right);
        Vector3 tip = guard.position + forward * 5f;
        AddLine(vertices, indices, guard.position, tip);
        AddLine(vertices, indices, tip, tip - forward * 1.5f + right);
        AddLine(vertices, indices, tip, tip - forward * 1.5f - right);
        var mesh = new Mesh
        {
            name = "Defender deployment bounds"
        };
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
        meshes.Add(mesh);
        var geometry = new GameObject("Defender " + (index + 1) + " bounds", typeof(MeshFilter), typeof(MeshRenderer));
        geometry.transform.SetParent(root.transform, false);
        geometry.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = geometry.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(properties);
        var label = new GameObject("Defender " + (index + 1) + " label");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = bounds.center + Vector3.up * (bounds.extents.y + 1f);
        TextMesh text = label.AddComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        text.richText = false;
        text.text = (index + 1) + " " + guard.name;
        text.characterSize = 0.2f;
        text.fontSize = 32;
        text.anchor = TextAnchor.LowerCenter;
        text.color = color;
        labels.Add(label.transform);
    }

    internal void FaceLabels(Quaternion rotation)
    {
        foreach (Transform label in labels)
        {
            label.rotation = rotation;
        }
    }

    private static bool OverlapsBuilding(OutpostGarrisonData guard, Matrix4x4 pose, List<BlockData> building)
    {
        var parts = new List<Bounds>();
        foreach (BlockData block in building)
        {
            parts.Add(CalculateBounds(new List<BlockData> { block }, Matrix4x4.identity));
        }

        foreach (BlockData block in guard.blueprint.blocks)
        {
            Bounds bounds = CalculateBounds(new List<BlockData> { block }, pose);
            if (parts.Exists(part => part.Intersects(bounds)))
            {
                return true;
            }
        }

        return false;
    }

    private static Bounds CalculateBounds(List<BlockData> blocks, Matrix4x4 deployment)
    {
        Bounds bounds = default;
        bool first = true;
        foreach (BlockData block in blocks)
        {
            Matrix4x4 transform = deployment * BlockTransform(block);
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = transform.MultiplyPoint3x4(Corner(i));
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        return bounds;
    }

    private static Matrix4x4 BlockTransform(BlockData block)
    {
        return Matrix4x4.TRS(
            new Vector3(block.posX, block.posY, block.posZ),
            new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW),
            new Vector3(block.x, block.y, block.z));
    }

    private static Vector3 Corner(int index)
    {
        return new Vector3((index & 1) == 0 ? -0.5f : 0.5f, (index & 2) == 0 ? -0.5f : 0.5f, (index & 4) == 0 ? -0.5f : 0.5f);
    }

    private static void AddBox(List<Vector3> vertices, List<int> indices, Matrix4x4 transform)
    {
        int start = vertices.Count;
        for (int i = 0; i < 8; i++)
        {
            vertices.Add(transform.MultiplyPoint3x4(Corner(i)));
        }

        foreach (int edge in Edges)
        {
            indices.Add(start + edge);
        }
    }

    private static void AddLine(List<Vector3> vertices, List<int> indices, Vector3 from, Vector3 to)
    {
        indices.Add(vertices.Count);
        vertices.Add(from);
        indices.Add(vertices.Count);
        vertices.Add(to);
    }

    public void Dispose()
    {
        root.SetActive(false);
        Object.Destroy(root);
        foreach (Mesh mesh in meshes)
        {
            Object.Destroy(mesh);
        }

        Object.Destroy(material);
    }
}
