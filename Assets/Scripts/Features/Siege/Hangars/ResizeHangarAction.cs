// 职责：以原尺寸和底部锚定姿态撤销/重做机库扩缩，并同步蓝图。
// 入口：Undo() / Redo()；由 HangarModule.TryResize() 提交。
using UnityEngine;

internal sealed class ResizeHangarAction : IBlockAction
{
    private readonly HangarModule hangar;
    private readonly Vector3Int before;
    private readonly Vector3Int after;
    private readonly Vector3 beforePosition;
    private readonly Vector3 afterPosition;
    public string ActionName => "Resize Hangar";

    internal ResizeHangarAction(
        HangarModule hangar,
        Vector3Int before,
        Vector3 beforePosition,
        Vector3Int after,
        Vector3 afterPosition)
    {
        this.hangar = hangar;
        this.before = before;
        this.beforePosition = beforePosition;
        this.after = after;
        this.afterPosition = afterPosition;
    }

    public void Undo()
    {
        if (hangar != null)
        {
            hangar.ApplySize(before, beforePosition);
        }
    }

    public void Redo()
    {
        if (hangar != null)
        {
            hangar.ApplySize(after, afterPosition);
        }
    }
}
