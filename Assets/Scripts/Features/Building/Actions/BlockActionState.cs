// 职责：保存建造动作的稳定方块身份、精确世界姿态和货仓，统一删除与蓝图工厂恢复。
// 入口：Remove() / Restore()；创建和删除动作只选择操作方向，当前建造目标切换时由ActionManager清空。
using UnityEngine;

internal sealed class BlockActionState
{
    private readonly BlockData snapshot;
    private Block instance;
    internal BlockActionState(Block block)
    {
        instance = block;
        snapshot = new BlockData(block);
        // 文件存档会吸附网格；撤销状态保留操作当时的精确世界姿态。
        Vector3 position = block.transform.position;
        Quaternion rotation = block.transform.rotation;
        snapshot.posX = position.x;
        snapshot.posY = position.y;
        snapshot.posZ = position.z;
        snapshot.rotX = rotation.x;
        snapshot.rotY = rotation.y;
        snapshot.rotZ = rotation.z;
        snapshot.rotW = rotation.w;
    }

    internal void Remove(bool captureCargo)
    {
        if (instance == null)
        {
            return;
        }

        if (captureCargo)
        {
            snapshot.cargo = instance.GetComponent<CargoHold>()?.CaptureContents();
        }

        VisualEffectsManager.TryPlayBlockRemoved(instance);
        BuildManager.instance.RemoveBlock(instance);
        Object.Destroy(instance.gameObject);
        instance = null;
    }

    internal void Restore()
    {
        if (instance != null || string.IsNullOrEmpty(snapshot.resourcePath))
        {
            return;
        }

        BuildManager build = BuildManager.instance;
        if (!BlueprintBlockFactory.TryRestore(snapshot, GameManager.instance.blocksParent, build.CurrentBuildFaction, out Block block))
        {
            Debug.LogWarning($"Could not restore block at {snapshot.resourcePath}");
            return;
        }

        // Block.Awake会执行编辑器吸附；工厂完成尺寸/身份/货物恢复后还原动作原姿态。
        block.transform.SetPositionAndRotation(
            new Vector3(snapshot.posX, snapshot.posY, snapshot.posZ),
            new Quaternion(snapshot.rotX, snapshot.rotY, snapshot.rotZ, snapshot.rotW));
        instance = block;
        build.SaveBlock(block);
        VisualEffectsManager.TryPlayBlockPlaced(block);
    }
}
