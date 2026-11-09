// 职责：定义玩家、敌方和建筑蓝图目标、名称、阵营及各自独立的存档路径。
// 入口：GetSavePath() / PlayerSave() / EnemyBlueprint()。
public enum BuildTargetKind
{
    PlayerSave,
    EnemyBlueprint,
    BuildingBlueprint,
    BaseBlueprint
}

public readonly struct BuildTargetContext
{
    public readonly BuildTargetKind Kind;
    public readonly string SaveName;
    public readonly string EnemyBlueprintName;
    public readonly string BuildingBlueprintName;
    public BuildTargetContext(
        BuildTargetKind kind,
        string saveName,
        string enemyBlueprintName,
        string buildingBlueprintName = null)
    {
        Kind = kind;
        SaveName = NormalizeName(saveName, "default");
        EnemyBlueprintName = NormalizeName(enemyBlueprintName, "default_enemy");
        BuildingBlueprintName = string.IsNullOrWhiteSpace(buildingBlueprintName) ? string.Empty : buildingBlueprintName.Trim();
    }

    public bool IsEnemyBlueprint => Kind == BuildTargetKind.EnemyBlueprint;
    public bool IsBaseBlueprint => Kind == BuildTargetKind.BaseBlueprint;
    public bool IsBuildingBlueprint => Kind == BuildTargetKind.BuildingBlueprint || IsBaseBlueprint;
    public bool IsDeveloperBlueprint => Kind != BuildTargetKind.PlayerSave;
    public string Name => IsBuildingBlueprint ? BuildingBlueprintName : IsEnemyBlueprint ? EnemyBlueprintName : SaveName;
    public UnitFaction Faction => IsEnemyBlueprint ? UnitFaction.Enemy : UnitFaction.Player;

    public string GetSavePath(SaveManager saveManager)
    {
        return IsBaseBlueprint ? saveManager.GetBaseBlueprintPath(BuildingBlueprintName) : IsBuildingBlueprint ? saveManager.GetBuildingBlueprintPath(BuildingBlueprintName) : IsEnemyBlueprint ? saveManager.GetEnemyBlueprintPath(EnemyBlueprintName) : saveManager.GetSavePath(SaveName);
    }

    public static BuildTargetContext PlayerSave(string saveName, string enemyBlueprintName)
    {
        return new BuildTargetContext(BuildTargetKind.PlayerSave, saveName, enemyBlueprintName);
    }

    public static BuildTargetContext EnemyBlueprint(string saveName, string enemyBlueprintName)
    {
        return new BuildTargetContext(BuildTargetKind.EnemyBlueprint, saveName, enemyBlueprintName);
    }

    public static BuildTargetContext BuildingBlueprint(string saveName, string enemyBlueprintName, string buildingBlueprintName)
    {
        return new BuildTargetContext(BuildTargetKind.BuildingBlueprint, saveName, enemyBlueprintName, buildingBlueprintName);
    }

    public static BuildTargetContext BaseBlueprint(string saveName, string enemyBlueprintName, string name)
    {
        return new BuildTargetContext(BuildTargetKind.BaseBlueprint, saveName, enemyBlueprintName, name);
    }

    private static string NormalizeName(string name, string fallback)
    {
        return string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
    }
}
