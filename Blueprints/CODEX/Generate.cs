// Run in the connected HY-Sandbox Editor: unity command eval_file --file Blueprints/CODEX/Generate.cs --timeout 60000 --json
// Uses shipped prefab dimensions and stock stats. Existing different saves are backed up, never silently overwritten.
var output = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Blueprints/CODEX");
var saves = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "Saves");
System.IO.Directory.CreateDirectory(output);
System.IO.Directory.CreateDirectory(saves);
var names = new[] { "CODEX_01_Bastion", "CODEX_02_Manta", "CODEX_03_Catamaran", "CODEX_04_Crossguard", "CODEX_05_Lance" };
var reports = new System.Collections.Generic.List<object>();
for (int design = 0; design < names.Length; design++)
{
    var cells = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>();
    for (int x = -12; x < 12; x++) for (int z = -14; z < 14; z++)
    {
        float a = UnityEngine.Mathf.Abs(x + .5f), b = UnityEngine.Mathf.Abs(z + .5f);
        bool core = a < 5 && b < 5;
        bool shape = design == 0 ? a < 7 && b < 7 && a + b < 12 :
            design == 1 ? b < 6 && a < 12 - b :
            design == 2 ? (a >= 6 && a < 10 && b < 10) || (a < 10 && b >= 2 && b < 4) :
            design == 3 ? (a < 3 && b < 11) || (b < 3 && a < 11) :
            b < 12 && a < 6 - UnityEngine.Mathf.Max(0, b - 6) * .5f;
        if (core || shape) cells.Add(new UnityEngine.Vector2Int(x, z));
    }
    var parts = new System.Collections.Generic.List<BlockData>();
    var occupied = new System.Collections.Generic.HashSet<UnityEngine.Vector3Int>();
    System.Action<string, float, float, float, float> add = (type, px, py, pz, yaw) =>
    {
        var prefab = UnityEngine.Resources.Load<UnityEngine.GameObject>("Blocks/" + type);
        if (prefab == null) throw new System.Exception("Missing prefab: " + type);
        var block = prefab.GetComponent<Block>();
        var rot = UnityEngine.Quaternion.Euler(0, yaw, 0);
        var data = UnityEngine.JsonUtility.FromJson<BlockData>("{}");
        data.id = names[design] + "_" + parts.Count.ToString("D5");
        data.x = block.x; data.y = block.y; data.z = block.z;
        data.posX = px; data.posY = py; data.posZ = pz;
        data.rotX = rot.x; data.rotY = rot.y; data.rotZ = rot.z; data.rotW = rot.w;
        data.resourcePath = "Assets/Resources/Blocks/" + type + ".prefab";
        // All rotated parts in these designs are 1x1x1; larger parts stay axis-aligned.
        for (int ix = 0; ix < block.x; ix++) for (int iy = 0; iy < block.y; iy++) for (int iz = 0; iz < block.z; iz++)
        {
            var cell = new UnityEngine.Vector3Int(UnityEngine.Mathf.FloorToInt(px - block.x / 2f + ix + .5f), UnityEngine.Mathf.FloorToInt(py - block.y / 2f + iy + .5f), UnityEngine.Mathf.FloorToInt(pz - block.z / 2f + iz + .5f));
            if (!occupied.Add(cell)) throw new System.Exception("Overlapping cell " + cell + " in " + names[design]);
        }
        parts.Add(data);
    };
    add("Cockpit", 0, 5, 0, 0);
    foreach (int x in new[] { -3, 3 }) foreach (int z in new[] { -3, 3 }) add("PowerGeneratingUnit", x, 5, z, 0);
    add("HoverFlightController", -.5f, 4.5f, 1.5f, 0);
    // Overlapping wireless coverage, mounted to the continuous lower deck.
    foreach (var cell in cells.OrderBy(c => c.x).ThenBy(c => c.y))
    {
        if ((UnityEngine.Mathf.Abs(cell.x + .5f) == 1.5f || UnityEngine.Mathf.Abs(cell.x + .5f) == 5.5f || UnityEngine.Mathf.Abs(cell.x + .5f) == 9.5f)
            && (UnityEngine.Mathf.Abs(cell.y + .5f) == 1.5f || UnityEngine.Mathf.Abs(cell.y + .5f) == 5.5f || UnityEngine.Mathf.Abs(cell.y + .5f) == 9.5f))
            add("PowerTransmissionDevice", cell.x + .5f, 4.5f, cell.y + .5f, 0);
    }
    // Symmetric distributed lift: every selected 2x2 mount has four real upper connectors.
    for (int x = -11; x <= 11; x += 2) for (int z = -13; z <= 13; z += 2)
    {
        if ((UnityEngine.Mathf.Abs(x) + UnityEngine.Mathf.Abs(z)) % 4 != 2) continue;
        if (cells.Contains(new UnityEngine.Vector2Int(x - 1, z - 1)) && cells.Contains(new UnityEngine.Vector2Int(x, z - 1)) && cells.Contains(new UnityEngine.Vector2Int(x - 1, z)) && cells.Contains(new UnityEngine.Vector2Int(x, z)))
            add("HoverThrusterBig", x, 2, z, 0);
    }
    // Four independent vectoring drives; all horizontal directions remain available after one is lost.
    foreach (int x in new[] { -3, 3 }) foreach (int z in new[] { -3, 3 }) add("UniversalThrusterBig", x, 8, z, 0);
    // Opposed fixed drives embedded at the centre-of-mass height improve acceleration without relying on vector slew.
    foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
        foreach (float x in new[] { -4.5f, 4.5f })
        {
            float z = yaw == 0 ? -1.5f : yaw == 90 ? -.5f : yaw == 180 ? .5f : 1.5f;
            add("MainThruster", x, 5.5f, z, yaw);
        }
    // Unobstructed top-deck repair bays, not buried inside the armour.
    foreach (float x in new[] { -1.5f, 1.5f }) foreach (float z in new[] { -3.5f, -.5f, 3.5f }) add("RepairBotContianer", x, 7.5f, z, 0);
    foreach (float x in new[] { -4.5f, 4.5f }) foreach (float z in new[] { -4.5f, -.5f, 4.5f }) add("Turret", x, 7.5f, z, 0);
    // Four full cell layers protect the cockpit; broad decks give multiple paths through the connection graph.
    foreach (var cell in cells.OrderBy(c => c.x).ThenBy(c => c.y)) for (int y = 3; y <= 6; y++)
        if (!occupied.Contains(new UnityEngine.Vector3Int(cell.x, y, cell.y))) add("1x1x1", cell.x + .5f, y + .5f, cell.y + .5f, 0);
    var list = new BlockDataList(); list.blocks = parts;
    string json = UnityEngine.JsonUtility.ToJson(list, true);
    foreach (var folder in new[] { output, saves })
    {
        string path = System.IO.Path.Combine(folder, names[design] + ".json");
        if (System.IO.File.Exists(path) && System.IO.File.ReadAllText(path) != json)
        {
            string backup = path + "." + System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff") + ".bak";
            System.IO.File.Copy(path, backup, false);
        }
        System.IO.File.WriteAllText(path, json, new System.Text.UTF8Encoding(false));
    }
    float mass = 0, demand = 0, supply = 0, lift = 0;
    foreach (var part in parts)
    {
        var prefab = UnityEngine.Resources.Load<UnityEngine.GameObject>(BuildManager.ConvertToResourcesPath(part.resourcePath));
        mass += prefab.GetComponent<Block>().mass;
        if (prefab.GetComponent<Power>() != null) demand += prefab.GetComponent<Power>().standardWorkingPower;
        if (prefab.GetComponent<PowerGeneratingUnit>() != null) supply += prefab.GetComponent<PowerGeneratingUnit>().outputPower;
        if (prefab.GetComponent<HoverThruster>() != null) lift += prefab.GetComponent<HoverThruster>().maxThrust;
    }
    reports.Add(new { name = names[design], blocks = parts.Count, mass, demand, supply, lift, liftWeightRatio = lift / (mass * 9.81f), counts = parts.GroupBy(p => System.IO.Path.GetFileNameWithoutExtension(p.resourcePath)).ToDictionary(g => g.Key, g => g.Count()) });
}
return new { saves, reports };
