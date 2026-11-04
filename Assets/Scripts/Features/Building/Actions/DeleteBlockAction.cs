// 职责：记录删除动作，委托共享方块状态完成恢复与删除。
// 入口：Undo() / Redo()；货仓与姿态来自删除时快照，撤销沿用同一方块身份。
public class DeleteBlockAction : IBlockAction
{
    public string ActionName => "Delete";

    private readonly BlockActionState state;
    public DeleteBlockAction(Block deletedBlock)
    {
        state = new BlockActionState(deletedBlock);
    }

    public void Undo()
    {
        state.Restore();
    }

    public void Redo()
    {
        state.Remove(false);
    }
}
