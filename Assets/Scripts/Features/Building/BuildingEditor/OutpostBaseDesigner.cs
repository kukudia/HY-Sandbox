// 职责：按敌机模块轮廓制作可编辑停机底座，敌机与建筑使用同一蓝图局部坐标。
// 入口：Create()生成独立组合蓝图；BoundsOf()用于尺寸适配、净空与验证。
using System;
using System.Collections.Generic;
using UnityEngine;

public static class OutpostBaseDesigner
{
    public static Bounds BoundsOf(IReadOnlyList<BlockData> blocks, Quaternion rotation)
    {
        Bounds result = default;
        bool first = true;
        foreach (BlockData block in blocks)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(
                rotation * new Vector3(block.posX, block.posY, block.posZ),
                rotation * new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW),
                new Vector3(block.x, block.y, block.z));
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = matrix.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? -.5f : .5f, (i & 2) == 0 ? -.5f : .5f, (i & 4) == 0 ? -.5f : .5f));
                if (first)
                {
                    result = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    result.Encapsulate(point);
                }
            }
        }

        return result;
    }

    public static BlockDataList Create(BlockDataList enemy, string name)
    {
        if (!EnemyBlueprintFactory.IsValidBlueprint(enemy, name))
        {
            throw new InvalidOperationException("Select a valid enemy blueprint first.");
        }

        Bounds bounds = BoundsOf(enemy.blocks, Quaternion.identity);
        int width = Mathf.CeilToInt((bounds.size.x + 8f) / 4f) * 4;
        int length = Mathf.CeilToInt((bounds.size.z + 8f) / 2f) * 2;
        if (width > BuildingFoundationData.MaximumSize || length > BuildingFoundationData.MaximumSize)
        {
            throw new InvalidOperationException("Enemy plus service clearance exceeds the 64m building limit.");
        }

        var data = new BlockDataList
        {
            foundation = new BuildingFoundationData
            {
                width = width,
                length = length
            },
            outpost = new EnemyOutpostData
            {
                enabled = true
            }
        };
        Vector3 position = new Vector3(width * .5f - 2f - bounds.center.x, 1.15f - bounds.min.y, length * .5f - 2f - bounds.center.z);
        var occupied = new HashSet<Vector2Int>();
        // A narrow centre spine joins the silhouette wings. Every tile touches the foundation.
        for (int z = 0; z < length; z += 2)
        {
            for (int x = 0; x < width; x += 4)
            {
                Vector3 center = new Vector3(x, .5f, z - 1f);
                bool serviceEnd = z == 0 || z == length - 2;
                bool spine = Mathf.Abs(center.x - (width * .5f - 2f)) <= 2f;
                bool covered = serviceEnd || spine;
                foreach (BlockData block in enemy.blocks)
                {
                    Bounds part = BoundsOf(new[] { block }, Quaternion.identity);
                    part.center += position;
                    if (center.x + 2f >= part.min.x - 2f && center.x - 2f <= part.max.x + 2f && center.z + 1f >= part.min.z - 2f && center.z - 1f <= part.max.z + 2f)
                    {
                        covered = true;
                        break;
                    }
                }

                if (covered)
                {
                    Add(data, "WorldBlueprints/Parts/Structure_4x1x2", center);
                    occupied.Add(new Vector2Int(x, z));
                }
            }
        }

        // Short perimeter bollards frame the silhouette without enclosing the aircraft or its launch path.
        foreach (Vector2Int tile in occupied)
        {
            if (!occupied.Contains(tile + Vector2Int.left * 4))
            {
                Add(data, "Blocks/1x1x1", new Vector3(tile.x - 1.5f, 1.5f, tile.y - 1.5f));
            }

            if (!occupied.Contains(tile + Vector2Int.right * 4))
            {
                Add(data, "Blocks/1x1x1", new Vector3(tile.x + 1.5f, 1.5f, tile.y - 1.5f));
            }
        }

        Add(data, "Blocks/PowerGeneratingUnit", new Vector3(0f, 2f, -1f));
        Add(data, "Blocks/PowerTransmissionDevice", new Vector3(3.5f, 1.5f, -1.5f));
        Add(data, "Blocks/CoinHold", new Vector3(width - 4.5f, 1.5f, -1.5f));
        data.blocks[data.blocks.Count - 1].cargo = new List<CargoItem>
        {
            new CargoItem
            {
                kind = CargoKind.Coins,
                amount = 100
            }
        };
        Add(data, "Blocks/TechnologyHold", new Vector3(width - 5.5f, 1.5f, -1.5f));
        data.blocks[data.blocks.Count - 1].cargo = new List<CargoItem>
        {
            new CargoItem
            {
                kind = CargoKind.Technology,
                amount = 20
            }
        };
        data.outpost.garrison.Add(new OutpostGarrisonData { id = "resident-0", name = name, position = position, yaw = 0f, blueprint = new OutpostEnemyBlueprint { blocks = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(enemy)).blocks } });
        WorldBuildingValidation.ValidateBlueprint(data);
        return data;
    }

    private static void Add(BlockDataList data, string resource, Vector3 position)
    {
        Block prefab = Resources.Load<GameObject>(resource).GetComponent<Block>();
        data.blocks.Add(new BlockData(prefab) { id = "base-" + data.blocks.Count, resourcePath = resource, posX = position.x, posY = position.y, posZ = position.z, rotX = 0f, rotY = 0f, rotZ = 0f, rotW = 1f });
    }
}
