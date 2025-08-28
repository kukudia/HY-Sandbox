using UnityEngine;

public class CreateBlockAction : IBlockAction
{
    public string ActionName => "Create";

    private string resourcePath;
    private Vector3 pos;
    private Quaternion rot;
    private int x, y, z;

    private GameObject createdObject;

    public CreateBlockAction(Block block)
    {
        resourcePath = block.resourcePath;
        pos = block.transform.position;
        rot = block.transform.rotation;
        x = block.x;
        y = block.y;
        z = block.z;

        createdObject = block.gameObject;
    }

    public void Undo()
    {
        if (createdObject != null)
        {
            BuildManager.instance.RemoveBlock(createdObject.GetComponent<Block>());
            Object.Destroy(createdObject);
            createdObject = null;
        }
    }

    public void Redo()
    {
        if (createdObject == null && !string.IsNullOrEmpty(resourcePath))
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                createdObject = Object.Instantiate(prefab, pos, rot);
                Block block = createdObject.GetComponent<Block>();
                block.x = x;
                block.y = y;
                block.z = z;
                block.resourcePath = resourcePath;
                BuildManager.instance.SaveBlock(block);
            }
        }
    }
}
