public enum BuildTargetKind
{
    PlayerSave,
    EnemyBlueprint
}

public readonly struct BuildTargetContext
{
    public readonly BuildTargetKind Kind;
    public readonly string SaveName;
    public readonly string EnemyBlueprintName;

    public BuildTargetContext(BuildTargetKind kind, string saveName, string enemyBlueprintName)
    {
        Kind = kind;
        SaveName = NormalizeName(saveName, "default");
        EnemyBlueprintName = NormalizeName(enemyBlueprintName, "default_enemy");
    }

    public bool IsEnemyBlueprint => Kind == BuildTargetKind.EnemyBlueprint;
    public string Name => IsEnemyBlueprint ? EnemyBlueprintName : SaveName;
    public UnitFaction Faction => IsEnemyBlueprint ? UnitFaction.Enemy : UnitFaction.Player;

    public string GetSavePath(SaveManager saveManager)
    {
        return IsEnemyBlueprint
            ? saveManager.GetEnemyBlueprintPath(EnemyBlueprintName)
            : saveManager.GetSavePath(SaveName);
    }

    public static BuildTargetContext PlayerSave(string saveName, string enemyBlueprintName)
    {
        return new BuildTargetContext(BuildTargetKind.PlayerSave, saveName, enemyBlueprintName);
    }

    public static BuildTargetContext EnemyBlueprint(string saveName, string enemyBlueprintName)
    {
        return new BuildTargetContext(BuildTargetKind.EnemyBlueprint, saveName, enemyBlueprintName);
    }

    private static string NormalizeName(string name, string fallback)
    {
        return string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
    }
}
