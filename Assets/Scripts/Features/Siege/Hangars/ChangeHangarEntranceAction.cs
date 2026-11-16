// 职责：撤销和重做机库开口方向，同时刷新几何与编辑中的蓝图。
// 入口：Undo() / Redo()；由机库编辑命令创建。
internal sealed class ChangeHangarEntranceAction : IBlockAction
{
    private readonly HangarModule hangar;
    private readonly HangarEntrance before;
    private readonly HangarEntrance after;
    public string ActionName => "Change Hangar Door";

    internal ChangeHangarEntranceAction(HangarModule hangar, HangarEntrance before, HangarEntrance after)
    {
        this.hangar = hangar;
        this.before = before;
        this.after = after;
    }

    public void Undo()
    {
        Apply(before);
    }

    public void Redo()
    {
        Apply(after);
    }

    private void Apply(HangarEntrance value)
    {
        if (hangar != null)
        {
            hangar.RestoreEntrance(value);
            BuildManager.instance?.SaveBlock(hangar.GetComponent<Block>());
        }
    }
}
