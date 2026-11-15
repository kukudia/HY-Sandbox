// 职责：聚合测试用只读蓝图目录，复用正式部署校验，保存明确的来源与JSON快照。
// 入口：Refresh()；包含内置敌舰、玩家Saves和EnemyBlueprints，不写源目录。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal sealed class EnemyLabBlueprints
{
    internal sealed class Option
    {
        internal string Label;
        internal string Json;
    }

    internal readonly List<Option> Options = new List<Option>();
    internal string Message
    {
        get; private set;
    }

    internal void Refresh()
    {
        Options.Clear();
        TextAsset[] examples = Resources.LoadAll<TextAsset>("WorldBlueprints/Enemies");
        Array.Sort(examples, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        foreach (TextAsset example in examples)
        {
            if (ModularUnitFactory.IsValidBlueprint(JsonUtility.FromJson<BlockDataList>(example.text), example.name))
            {
                Options.Add(new Option { Label = "内置 / " + example.name, Json = example.text });
            }
        }

        var messages = new List<string>();
        foreach (string folder in new[]
        {
            "Saves",
            "EnemyBlueprints"
        }

        )
        {
            SiegeShipOption[] ships = SiegeBlueprintCatalog.Load(Path.Combine(Application.persistentDataPath, folder), 1, out string message);
            messages.Add(message);
            foreach (SiegeShipOption ship in ships)
            {
                Options.Add(new Option { Label = folder + " / " + ship.DisplayName, Json = ship.BlueprintJson });
            }
        }

        Message = string.Join("\n", messages);
    }
}
