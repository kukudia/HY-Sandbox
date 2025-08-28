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
        // ÄæÐò³·Ïú£¬±ÜÃâÒÀÀµ¹ØÏµ³ö´í
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
