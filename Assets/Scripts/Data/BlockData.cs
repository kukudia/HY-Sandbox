using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockData
{
    public string id;
    public int x, y, z;
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ, rotW;
    public string resourcePath;

    public BlockData(Block block)
    {
        id = block.uniqueId;
        x = block.x;
        y = block.y;
        z = block.z;

        var t = block.transform;
        posX = Mathf.Round(t.position.x * 2) / 2f;
        posY = Mathf.Round(t.position.y * 2) / 2f;
        posZ = Mathf.Round(t.position.z * 2) / 2f;

        Vector3 euler = t.rotation.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90) * 90;
        euler.y = Mathf.Round(euler.y / 90) * 90;
        euler.z = Mathf.Round(euler.z / 90) * 90;

        Quaternion snappedRot = Quaternion.Euler(euler);
        rotX = snappedRot.x;
        rotY = snappedRot.y;
        rotZ = snappedRot.z;
        rotW = snappedRot.w;

        resourcePath = block.resourcePath;
    }
}

[System.Serializable]
public class BlockDataList
{
    public List<BlockData> blocks = new List<BlockData>();
}
