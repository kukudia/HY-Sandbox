// 职责：复制蓝图模块的静态模型与材质，显示完整外形而不实例化战斗、连接或物理脚本。
// 入口：Create()；返回根对象由调用者销毁，网格和材质共享原资源，不拥有资源本体。
using System.Collections.Generic;
using UnityEngine;

public static class BlueprintModelPreview
{
    public static GameObject Create(IReadOnlyList<BlockData> blocks, Transform parent, string name)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        foreach (BlockData block in blocks)
        {
            GameObject prefab = Resources.Load<GameObject>(BlueprintBlockFactory.ToResourcePath(block.resourcePath));
            if (prefab == null)
            {
                continue;
            }

            var part = new GameObject(block.id);
            part.transform.SetParent(root.transform, false);
            part.transform.SetLocalPositionAndRotation(
                new Vector3(block.posX, block.posY, block.posZ),
                new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW));
            CopyVisuals(prefab.transform, part.transform, true);
        }

        return root;
    }

    private static void CopyVisuals(Transform source, Transform target, bool root)
    {
        // Ignore connectors, labels and disabled effect geometry. Never run prefab Awake/OnEnable.
        if (!root && (!source.gameObject.activeSelf || source.name == "Connectors"))
        {
            return;
        }

        Transform copy = target;
        if (!root)
        {
            copy = new GameObject(source.name).transform;
            copy.SetParent(target, false);
            copy.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
            copy.localScale = source.localScale;
        }

        MeshFilter mesh = source.GetComponent<MeshFilter>();
        MeshRenderer renderer = source.GetComponent<MeshRenderer>();
        if (mesh != null && renderer != null && renderer.enabled && mesh.sharedMesh != null)
        {
            copy.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            MeshRenderer visible = copy.gameObject.AddComponent<MeshRenderer>();
            visible.sharedMaterials = renderer.sharedMaterials;
            visible.shadowCastingMode = renderer.shadowCastingMode;
            visible.receiveShadows = renderer.receiveShadows;
        }

        foreach (Transform child in source)
        {
            CopyVisuals(child, copy, false);
        }
    }
}
