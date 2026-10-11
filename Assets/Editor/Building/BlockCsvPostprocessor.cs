// 职责：CSV 文件被 Unity 导入后请求延迟同步，避免在 AssetPostprocessor 内递归写资产。
// 入口：OnPostprocessAllAssets()；Prefab 自身重导入不会触发 CSV 同步循环。
using System.Linq;
using UnityEditor;

public sealed class BlockCsvPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Contains(BlockCsvDocument.AssetPath) || moved.Contains(BlockCsvDocument.AssetPath))
        {
            BlockCsvAuthoring.QueueBake();
        }
    }
}
