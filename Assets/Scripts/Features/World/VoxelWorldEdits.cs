// 职责：将体素修改保存为有版本/种子/基线指纹的差量；校验全部通过才返回新数据。
// 入口：Capture() / Restore()；不读写磁盘、不修改传入世界，拒绝不兼容或重复单元。
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VoxelWorldEdits
{
    public int version = 1;
    public int seed;
    public Vector3Int dimensions;
    public string baselineFingerprint;
    public CellEdit[] edits;
    [Serializable]
    public struct CellEdit
    {
        public Vector3Int cell;
        public int material;
    }

    public static VoxelWorldEdits Capture(VoxelWorldData current, int worldSeed)
    {
        var baseline = new VoxelWorldData();
        baseline.Generate(current.Size, worldSeed);
        var changes = new List<CellEdit>();
        for (int z = 0; z < current.Size.z; z++)
        {
            for (int y = 0; y < current.Size.y; y++)
            {
                for (int x = 0; x < current.Size.x; x++)
                {
                    var cell = new Vector3Int(x, y, z);
                    if (current.Get(cell) != baseline.Get(cell))
                    {
                        changes.Add(new CellEdit { cell = cell, material = current.Get(cell) });
                    }
                }
            }
        }

        return new VoxelWorldEdits
        {
            seed = worldSeed,
            dimensions = current.Size,
            baselineFingerprint = baseline.Fingerprint().ToString("X8"),
            edits = changes.ToArray()
        };
    }

    /// <summary>从种子恢复独立数据；任何版本/身份/材质/边界错误均抛出异常，不改活动世界。</summary>
    public VoxelWorldData Restore(int expectedSeed, Vector3Int expectedSize)
    {
        if (version != 1 || seed != expectedSeed || dimensions != expectedSize || edits == null)
        {
            throw new InvalidOperationException("World edit format or seed/dimensions do not match this world.");
        }

        var restored = new VoxelWorldData();
        restored.Generate(dimensions, seed);
        if (restored.Fingerprint().ToString("X8") != baselineFingerprint || edits.Length > dimensions.x * dimensions.y * dimensions.z)
        {
            throw new InvalidOperationException("World generator changed or edit count is invalid.");
        }

        var visited = new HashSet<Vector3Int>();
        foreach (CellEdit edit in edits)
        {
            if (!restored.Contains(edit.cell) || edit.material < 0 || edit.material > 3 || !visited.Add(edit.cell))
            {
                throw new InvalidOperationException("World edit contains invalid or duplicate cells.");
            }

            restored.Set(edit.cell, (byte)edit.material);
        }

        return restored;
    }
}
