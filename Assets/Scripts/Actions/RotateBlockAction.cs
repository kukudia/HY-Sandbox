using UnityEngine;

public class RotateBlockAction : IBlockAction
{
    public string ActionName => "Rotate";

    private Block block;
    private Vector3 oldPos;
    private Vector3 newPos;
    private Quaternion oldRot;
    private Quaternion newRot;

    public RotateBlockAction(Block block, Vector3 oldPos, Vector3 newPos, Quaternion oldRot, Quaternion newRot)
    {
        this.block = block;
        this.oldPos = oldPos;
        this.newPos = newPos;
        this.oldRot = oldRot;
        this.newRot = newRot;
    }

    public void Undo()
    {
        if (block != null)
        {
            block.transform.rotation = oldRot;
            BuildManager.instance.SaveBlock(block);
        }
    }

    public void Redo()
    {
        if (block != null)
        {
            block.transform.rotation = newRot;
            BuildManager.instance.SaveBlock(block);
        }
    }

}
