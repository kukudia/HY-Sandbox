using UnityEditor;
using UnityEngine;

public class DeleteBlockAction : IBlockAction
{
    private string resourcePath;
    private Block deletedBlock;
    private Vector3 pos;
    private Quaternion rot;
    private int x, y, z;

    public DeleteBlockAction(Block deletedBlock)
    {
        this.deletedBlock = deletedBlock;
        pos = deletedBlock.transform.position;
        rot = deletedBlock.transform.rotation;
        x = deletedBlock.x;
        y = deletedBlock.y;
        z = deletedBlock.z;

        resourcePath = ConvertToResourcesPath(deletedBlock.resourcePath); // 保存运行时资源路径
    }

    public void Undo()
    {
        if (deletedBlock == null && !string.IsNullOrEmpty(resourcePath))
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                GameObject obj = Object.Instantiate(prefab, pos, rot);
                obj.transform.parent = BuildManager.instance.blocksParent;
                deletedBlock = obj.GetComponent<Block>();
                deletedBlock.x = x;
                deletedBlock.y = y;
                deletedBlock.z = z;
                deletedBlock.resourcePath = resourcePath; // 继续保留路径
            }
            else
            {
                Debug.LogWarning($"Prefab not found at {resourcePath}");
            }
        }
    }

    public void Redo()
    {
        if (deletedBlock != null)
        {
            Object.Destroy(deletedBlock.gameObject);
        }
    }

    string ConvertToResourcesPath(string fullPath)
    {
        if (fullPath.StartsWith("Assets/Resources/"))
        {
            fullPath = fullPath.Substring("Assets/Resources/".Length);
        }

        if (fullPath.EndsWith(".prefab"))
        {
            fullPath = fullPath.Substring(0, fullPath.Length - ".prefab".Length);
        }

        return fullPath;
    }
}
