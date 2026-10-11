// 职责：定义回收物品类型、存储条目及货仓分配规则。
// 入口：Copy()。
using System;
using UnityEngine;

public enum CargoKind
{
    SpecialPart,
    Coins,
    Technology
}

[Serializable]
public class CargoItem
{
    public CargoKind kind;
    public string resourcePath;
    [Min(1)]
    public int amount = 1;
    public CargoItem Copy(int count = -1)
    {
        return new CargoItem
        {
            kind = kind,
            resourcePath = resourcePath,
            amount = count < 0 ? amount : count
        };
    }

    public bool IsValid => amount > 0 && (kind != CargoKind.SpecialPart || !string.IsNullOrWhiteSpace(resourcePath));
}
