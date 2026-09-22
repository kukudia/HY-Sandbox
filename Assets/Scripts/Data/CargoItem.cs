using System;
using UnityEngine;

public enum CargoKind { SpecialPart, Coins, Technology }

[Serializable]
public class CargoItem
{
    public CargoKind kind;
    public string resourcePath;
    [Min(1)] public int amount = 1;

    public CargoItem Copy(int count = -1)
    {
        return new CargoItem { kind = kind, resourcePath = resourcePath, amount = count < 0 ? amount : count };
    }

    public bool IsValid => amount > 0 && (kind != CargoKind.SpecialPart || !string.IsNullOrWhiteSpace(resourcePath));
}
