// 职责：约定建造动作的撤销、重做和计数名称。
public interface IBlockAction
{
    void Undo();
    void Redo();
    string ActionName
    {
        get;
    }
}
