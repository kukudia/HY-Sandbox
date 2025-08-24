using UnityEngine;

public class MoveBlockAction : IBlockAction
{
    private Block block;
    private Vector3 oldPos;
    private Vector3 newPos;

    public MoveBlockAction(Block block, Vector3 oldPos, Vector3 newPos)
    {
        this.block = block;
        this.oldPos = oldPos;
        this.newPos = newPos;
    }

    public void Undo()
    {
        if (block != null) block.transform.position = oldPos;
    }

    public void Redo()
    {
        if (block != null) block.transform.position = newPos;
    }
}
