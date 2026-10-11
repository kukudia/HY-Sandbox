// 职责：将多项建造动作组成一次可撤销/重做的事务。
// 入口：Undo() / Redo()。
using System.Collections.Generic;

public class GroupAction : IBlockAction
{
    public string ActionName => "Group";

    private List<IBlockAction> actions = new List<IBlockAction>();
    public GroupAction(IEnumerable<IBlockAction> actions)
    {
        this.actions.AddRange(actions);
    }

    public void Undo()
    {
        // 逆序撤销，避免依赖关系出错
        for (int i = actions.Count - 1; i >= 0; i--)
        {
            actions[i].Undo();
        }
    }

    public void Redo()
    {
        foreach (var action in actions)
        {
            action.Redo();
        }
    }
}
