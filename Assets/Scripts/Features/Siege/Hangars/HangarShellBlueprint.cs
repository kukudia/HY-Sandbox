// 职责：将逻辑机库展开成可独立损毁的 2×2×1 建筑模块；版本标记防止读档补回已毁墙体。
// 入口：Expand() / Tiles() / EvenSize()；不写入玩家蓝图文件。
using System;
using System.Collections.Generic;
using UnityEngine;

public static class HangarShellBlueprint
{
    public const int Version = 1;
    public const string TileResource = "Blocks/2x2x1";
    public static int EvenSize(int value)
    {
        return Mathf.Clamp((value + 1) / 2 * 2, 4, 64);
    }

    public static List<Pose> Tiles(Vector3Int size, HangarEntrance entrance = HangarEntrance.Front)
    {
        if (entrance == HangarEntrance.Roof)
        {
            // Rotate the same non-overlapping tiling so its open face becomes the roof.
            // Front snapshots keep their original ordering and stable tile identities.
            List<Pose> roof = Tiles(new Vector3Int(size.x, size.z, size.y));
            Quaternion turn = Quaternion.Euler(-90f, 0f, 0f);
            for (int i = 0; i < roof.Count; i++)
            {
                roof[i] = new Pose(turn * roof[i].position, turn * roof[i].rotation);
            }

            return roof;
        }

        var tiles = new List<Pose>();
        // Floor/roof cover the full footprint. Side walls fill between them; the back
        // fills between the sides. No overlapping corner blocks or permanent front wall.
        for (int x = 0; x < size.x; x += 2)
        {
            for (int z = 0; z < size.z; z += 2)
            {
                tiles.Add(new Pose(
                    new Vector3(x + 1 - size.x * .5f, -.5f * size.y + .5f, z + 1 - size.z * .5f),
                    Quaternion.Euler(90, 0, 0)));
                tiles.Add(new Pose(
                    new Vector3(x + 1 - size.x * .5f, .5f * size.y - .5f, z + 1 - size.z * .5f),
                    Quaternion.Euler(90, 0, 0)));
            }
        }

        for (int y = 0; y < size.y - 2; y += 2)
        {
            float height = y + 2 - size.y * .5f;
            for (int z = 0; z < size.z; z += 2)
            {
                tiles.Add(new Pose(new Vector3(-size.x * .5f + .5f, height, z + 1 - size.z * .5f), Quaternion.Euler(0, 90, 0)));
                tiles.Add(new Pose(new Vector3(size.x * .5f - .5f, height, z + 1 - size.z * .5f), Quaternion.Euler(0, 90, 0)));
            }

            for (int x = 0; x < size.x - 2; x += 2)
            {
                tiles.Add(new Pose(new Vector3(x + 2 - size.x * .5f, height, -size.z * .5f + .5f), Quaternion.identity));
            }
        }

        return tiles;
    }

    public static int TileCount(Vector3Int size, HangarEntrance entrance = HangarEntrance.Front)
    {
        if (entrance == HangarEntrance.Roof)
        {
            size = new Vector3Int(size.x, size.z, size.y);
        }

        return size.x * size.z / 2 + (size.y - 2) * size.z / 2 + (size.x - 2) * (size.y - 2) / 4;
    }

    public static BlockData Normalize(BlockData source)
    {
        if (source == null || !HangarRules.IsHangar(source))
        {
            return source;
        }

        Vector3Int size = new Vector3Int(EvenSize(source.x), EvenSize(source.y), EvenSize(source.z));
        if (source.x == size.x && source.y == size.y && source.z == size.z)
        {
            return source;
        }

        BlockData result = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(source));
        Quaternion rotation = new Quaternion(source.rotX, source.rotY, source.rotZ, source.rotW);
        Vector3 origin = new Vector3(source.posX, source.posY, source.posZ) + rotation * ((Vector3)size - new Vector3(source.x, source.y, source.z)) * .5f;
        result.x = size.x;
        result.y = size.y;
        result.z = size.z;
        // Even-width walls require an integer horizontal centre to keep their downward
        // connectors on the foundation half-grid. A legacy odd bay must not shift by .5m.
        result.posX = Mathf.Floor(origin.x + .001f);
        result.posY = origin.y;
        result.posZ = Mathf.Floor(origin.z + .001f);
        return result;
    }

    public static BlockDataList Expand(BlockDataList source)
    {
        if (source == null || source.hangarShellVersion == Version)
        {
            return source;
        }

        if (source.hangarShellVersion != 0)
        {
            throw new InvalidOperationException("Unsupported hangar shell snapshot version.");
        }

        BlockDataList result = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(source));
        for (int i = 0; i < result.blocks.Count; i++)
        {
            result.blocks[i] = Normalize(result.blocks[i]);
        }

        var added = new List<BlockData>();
        var ids = new HashSet<string>();
        foreach (BlockData data in result.blocks)
        {
            ids.Add(data.id);
        }

        foreach (BlockData hangar in result.blocks)
        {
            if (!HangarRules.IsHangar(hangar))
            {
                continue;
            }

            Vector3Int size = new Vector3Int(hangar.x, hangar.y, hangar.z);
            Quaternion rotation = new Quaternion(hangar.rotX, hangar.rotY, hangar.rotZ, hangar.rotW);
            Vector3 origin = new Vector3(hangar.posX, hangar.posY, hangar.posZ);
            List<Pose> tiles = Tiles(size, hangar.hangarEntrance);
            for (int i = 0; i < tiles.Count; i++)
            {
                string id = hangar.id + "/shell/" + i;
                if (!ids.Add(id))
                {
                    throw new InvalidOperationException("Hangar shell identity collides with an existing module: " + id);
                }

                Vector3 position = origin + rotation * tiles[i].position;
                Quaternion pose = rotation * tiles[i].rotation;
                BlockData tile = JsonUtility.FromJson<BlockData>("{}");
                tile.id = id;
                tile.x = 2;
                tile.y = 2;
                tile.z = 1;
                tile.posX = position.x;
                tile.posY = position.y;
                tile.posZ = position.z;
                tile.rotX = pose.x;
                tile.rotY = pose.y;
                tile.rotZ = pose.z;
                tile.rotW = pose.w;
                tile.resourcePath = TileResource;
                tile.materialId = "metal";
                tile.hangarOwnerId = hangar.id;
                added.Add(tile);
            }
        }

        result.blocks.AddRange(added);
        result.hangarShellVersion = Version;
        return result;
    }

    public static BlueprintStructureState ExpandState(BlueprintStructureState source)
    {
        if (source.blueprint.hangarShellVersion == Version)
        {
            return source;
        }

        BlueprintStructureState result = JsonUtility.FromJson<BlueprintStructureState>(JsonUtility.ToJson(source));
        result.blueprint = Expand(result.blueprint);
        float[] health = new float[result.blueprint.blocks.Count];
        Array.Copy(source.durability, health, source.durability.Length);
        for (int i = source.durability.Length; i < health.Length; i++)
        {
            health[i] = float.MaxValue;
        }

        result.durability = health;
        return result;
    }
}
