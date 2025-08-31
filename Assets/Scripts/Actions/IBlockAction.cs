public interface IBlockAction
{
    void Undo();

    void Redo();
    string ActionName { get; }
}
