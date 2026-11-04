// 职责：统一逻辑方块与场景方块的变换包围盒，不包含地基、净空或加载窗口策略。
// 入口：FromBlueprint() / FromConstruct() / Transform()；调用者显式选择输出坐标和是否包含额外原点。
using System.Collections.Generic;
using UnityEngine;

public static class BlockBounds
{
    public static Bounds FromBlueprint(BlockData block, Matrix4x4 localToOutput)
    {
        Matrix4x4 pose = Matrix4x4.TRS(
            new Vector3(block.posX, block.posY, block.posZ),
            new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW),
            Vector3.one);
        return Transform(new Bounds(Vector3.zero, new Vector3(block.x, block.y, block.z)), localToOutput * pose);
    }

    public static Bounds FromBlueprint(IReadOnlyList<BlockData> blocks, Matrix4x4 localToOutput)
    {
        Bounds bounds = default;
        bool found = false;
        foreach (BlockData block in blocks)
        {
            Encapsulate(ref bounds, ref found, FromBlueprint(block, localToOutput));
        }

        return bounds;
    }

    public static Bounds FromConstruct(Transform construct, Matrix4x4 worldToOutput, Vector3 emptyCenter)
    {
        Bounds bounds = new Bounds(emptyCenter, Vector3.zero);
        bool found = false;
        foreach (Block block in construct.GetComponentsInChildren<Block>())
        {
            Bounds local = new Bounds(Vector3.zero, new Vector3(block.x, block.y, block.z));
            Encapsulate(ref bounds, ref found, Transform(local, worldToOutput * block.transform.localToWorldMatrix));
        }

        return bounds;
    }

    public static Bounds Transform(Bounds local, Matrix4x4 localToOutput)
    {
        Bounds bounds = default;
        for (int i = 0; i < 8; i++)
        {
            Vector3 sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
            Vector3 point = localToOutput.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, sign));
            if (i == 0)
            {
                bounds = new Bounds(point, Vector3.zero);
            }
            else
            {
                bounds.Encapsulate(point);
            }
        }

        return bounds;
    }

    public static void Encapsulate(ref Bounds bounds, ref bool found, Bounds next)
    {
        if (!found)
        {
            bounds = next;
            found = true;
        }
        else
        {
            bounds.Encapsulate(next);
        }
    }
}
