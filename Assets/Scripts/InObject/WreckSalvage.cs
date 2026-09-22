using UnityEngine;

public class WreckSalvage : MonoBehaviour
{
    private bool _converted;
    public static SalvageSettings Settings => Resources.Load<SalvageSettings>("Salvage/Settings");

    public static void Convert(Block block)
    {
        if (block == null || PlayManager.instance == null || !PlayManager.instance.playMode) return;
        WreckSalvage marker = block.GetComponent<WreckSalvage>();
        if (marker == null) marker = block.gameObject.AddComponent<WreckSalvage>();
        if (marker._converted) return;
        marker._converted = true;
        foreach (Bot bot in block.GetComponentsInChildren<Bot>(true)) bot.PrepareForHomeDestruction();
        RuntimeUnitMember member = block.GetComponent<RuntimeUnitMember>();
        ControlUnit unit = block.GetComponentInParent<ControlUnit>();
        UnitFaction faction = member != null ? member.ownerFaction : unit != null ? unit.faction : UnitFaction.Player;
        CargoHold cargo = block.GetComponent<CargoHold>();
        if (cargo != null) cargo.ReleaseContents(faction);
        if (faction != UnitFaction.Enemy) return;
        SalvageSettings settings = Settings;
        string path = string.IsNullOrEmpty(block.resourcePath) ? string.Empty : BuildManager.ConvertToResourcesPath(block.resourcePath);
        bool special = settings != null && settings.specialPartResources != null
            && System.Array.Exists(settings.specialPartResources, p => BuildManager.ConvertToResourcesPath(p) == path)
            && Random.value < settings.specialPartChance;
        LootDrop.Spawn(new CargoItem { kind = special ? CargoKind.SpecialPart : CargoKind.Coins,
            resourcePath = special ? path : string.Empty, amount = special ? 1 : settings != null ? Mathf.Max(1, settings.coinsPerBlock) : 2 }, block.transform.position);
    }

    public static void ConvertGroup(ControlUnit unit)
    {
        if (unit == null || unit.HasAnyCockpit) return;
        foreach (Block block in unit.GetComponentsInChildren<Block>()) Convert(block);
    }
}
