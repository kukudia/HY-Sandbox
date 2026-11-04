// 职责：记录创建动作，委托共享方块状态完成删除与恢复。
// 入口：Undo() / Redo()；撤销时捕获当前货仓，重做沿用同一方块身份。
public class CreateBlockAction : IBlockAction
{
    public string ActionName => "Create";

    private readonly BlockActionState state;
    public CreateBlockAction(Block block)
    {
        state = new BlockActionState(block);
    }

    public void Undo()
    {
        state.Remove(true);
    }

    public void Redo()
    {
        state.Restore();
    }
}
