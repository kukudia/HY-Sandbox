// 职责：菜单临时选择其他探索载具时保留工坊对象、蓝图上下文、缓存和底座。
// 入口：Park() / Restore()；只暂停原稿，绝不把运行结果写回原稿文件。
using System.Collections.Generic;
using UnityEngine;

internal sealed class MenuPlayDraft
{
    private readonly PlayDraft objects = new PlayDraft();
    private BuildTargetContext context;
    private BuildingFoundation foundation;
    private BlockDataList cache;
    private List<Block> blocks;
    private string playerName;
    private string enemyName;
    private string buildingName;
    private string baseName;
    private (IBlockAction[] Undo, IBlockAction[] Redo) history;
    public bool Parked => objects.HasDraft;

    public void Park()
    {
        BuildManager build = BuildManager.instance;
        SaveManager saves = SaveManager.instance;
        context = build.WorkshopContext;
        foundation = build.Foundation;
        cache = saves.cachedData;
        blocks = new List<Block>(saves.blocks);
        playerName = saves.currentSaveName;
        enemyName = saves.currentEnemyBlueprintName;
        buildingName = saves.currentBuildingBlueprintName;
        baseName = saves.currentBaseBlueprintName;
        history = ActionManager.instance.CaptureHistory();
        build.DeselectBlock();
        objects.Park();
    }

    public void Restore()
    {
        if (!Parked)
        {
            return;
        }

        BuildManager.instance.CancelBlueprintLoad();
        Transform temporary = GameManager.instance.blocksParent;
        if (temporary != null)
        {
            temporary.gameObject.SetActive(false);
            Object.Destroy(temporary.gameObject);
        }

        GameManager.instance.blocksParent = null;
        PlayManager.instance.blocksParent = null;
        SaveManager saves = SaveManager.instance;
        saves.cachedData = cache;
        saves.blocks.Clear();
        saves.blocks.AddRange(blocks);
        saves.currentSaveName = playerName;
        saves.currentEnemyBlueprintName = enemyName;
        saves.currentBuildingBlueprintName = buildingName;
        saves.currentBaseBlueprintName = baseName;
        BuildManager.instance.RestoreWorkshopContext(context, foundation);
        objects.Restore();
        PlayManager.instance.blocksParent = GameManager.instance.blocksParent;
        ActionManager.instance.RestoreHistory(history);
        MainUIPanels.instance.RestoreBuildPanels();
    }
}
