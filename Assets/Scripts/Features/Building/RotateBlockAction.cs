// 职责：保存旋转前后姿态及位置，撤销/重做后同步存档。
// 入口：Undo() / Redo()。
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
            block.transform.position = oldPos;
            block.transform.rotation = oldRot;
            BuildManager.instance.SaveBlock(block);
            VisualEffectsManager.TryPlayBlockRotated(block);
        }
    }

    public void Redo()
    {
        if (block != null)
        {
            block.transform.position = newPos;
            block.transform.rotation = newRot;
            BuildManager.instance.SaveBlock(block);
            VisualEffectsManager.TryPlayBlockRotated(block);
        }
    }
}
