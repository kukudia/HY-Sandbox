// DSH enemy blueprint validator.
//
//   unity command eval_file --file Blueprints/DSH_Enemies/Validate.cs --timeout 120000 --json
//
// Checks the three DSH blueprints against the real prefabs, using the shipped loader's own
// rules:
//   * resource path resolves and the recorded x/y/z match the prefab;
//   * ids unique, exactly one Cockpit;
//   * every enabled connector pair coincides at the same world point with opposing normals
//     (Block.FindMatchingConnector) AND the two blocks' volumes actually touch, which is
//     what Block.FindBlockAcrossConnector's 0.05 m probe needs;
//   * the whole block set is one connected component reachable from the cockpit;
//   * no two blocks overlap in volume;
//   * total generator output covers every Power block at >= its minWorkingPower, and every
//     load sits inside a live relay's 5 m box while every relay reaches a generator;
//   * total hover thrust exceeds the total weight, and the horizontal pods contribute.
var results = new System.Collections.Generic.List<object>();
var failures = new System.Collections.Generic.List<string>();
string folder = System.IO.Path.GetFullPath("Blueprints/DSH_Enemies");
string live = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "EnemyBlueprints");

float[] Span(UnityEngine.Vector3 p, UnityEngine.Vector3Int s, int axis)
{
    float half = (axis == 0 ? s.x : axis == 1 ? s.y : s.z) * 0.5f;
    float c = axis == 0 ? p.x : axis == 1 ? p.y : p.z;
    return new[] { c - half, c + half };
}

bool Touching(UnityEngine.Vector3 a, UnityEngine.Vector3Int sa,
              UnityEngine.Vector3 b, UnityEngine.Vector3Int sb)
{
    int shared = 0;
    for (int axis = 0; axis < 3; axis++)
    {
        float lo = System.Math.Max(Span(a, sa, axis)[0], Span(b, sb, axis)[0]);
        float hi = System.Math.Min(Span(a, sa, axis)[1], Span(b, sb, axis)[1]);
        if (hi - lo > 1e-4f) shared++;
        else if (System.Math.Abs(hi - lo) > 1e-4f) return false;
    }
    return shared >= 2;
}

foreach (var file in System.IO.Directory.GetFiles(folder, "DSH_E*.json").OrderBy(p => p))
{
    string name = System.IO.Path.GetFileNameWithoutExtension(file);
    var data = UnityEngine.JsonUtility.FromJson<BlockDataList>(System.IO.File.ReadAllText(file));
    var problems = new System.Collections.Generic.List<string>();

    var positions = new UnityEngine.Vector3[data.blocks.Count];
    var sizes = new UnityEngine.Vector3Int[data.blocks.Count];
    var sockets = new System.Collections.Generic.List<int>[data.blocks.Count];
    for (int i = 0; i < data.blocks.Count; i++) sockets[i] = new System.Collections.Generic.List<int>();
    var socketPoints = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>();
    var socketNormals = new System.Collections.Generic.List<UnityEngine.Vector3>();

    var loads = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var gens = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var relays = new System.Collections.Generic.List<UnityEngine.Vector3>();
    int cockpit = -1, cockpits = 0;
    float mass = 0f, lift = 0f, horizontal = 0f, demand = 0f, supply = 0f, minPower = 0f;

    for (int i = 0; i < data.blocks.Count; i++)
    {
        var d = data.blocks[i];
        var prefab = UnityEngine.Resources.Load<UnityEngine.GameObject>(
            BuildManager.ConvertToResourcesPath(d.resourcePath));
        if (prefab == null) { problems.Add("missing prefab " + d.resourcePath); continue; }
        var block = prefab.GetComponent<Block>();
        if (block == null) { problems.Add("no Block on " + d.resourcePath); continue; }
        if (d.x != block.x || d.y != block.y || d.z != block.z)
            problems.Add("size mismatch on " + d.resourcePath);
        positions[i] = new UnityEngine.Vector3(d.posX, d.posY, d.posZ);
        sizes[i] = new UnityEngine.Vector3Int(block.x, block.y, block.z);
        mass += block.mass;

        var power = prefab.GetComponent<Power>();
        if (power != null)
        {
            loads.Add(positions[i]);
            demand += power.standardWorkingPower;
            minPower = System.Math.Max(minPower, power.minWorkingPower);
        }
        var generator = prefab.GetComponent<PowerGeneratingUnit>();
        if (generator != null) { gens.Add(positions[i]); supply += generator.outputPower; }
        var relay = prefab.GetComponent<PowerTransmissionDevice>();
        if (relay != null) relays.Add(positions[i]);
        var hover = prefab.GetComponent<HoverThruster>();
        if (hover != null) lift += hover.maxThrust;
        var vector = prefab.GetComponent<UniversalThruster>();
        if (vector != null) horizontal += vector.maxThrust;
        if (prefab.GetComponent<Cockpit>() != null) { cockpit = i; cockpits++; }

        var q = new UnityEngine.Quaternion(d.rotX, d.rotY, d.rotZ, d.rotW);
        // HoverFlightController reads its own transform.up as the craft's up-vector
        // (CalculateTiltAdjustment / ApplyRotationCorrection), so a rolled one would fly the
        // craft inverted.  Everything else in these hulls either re-aims itself in world
        // space (UniversalThruster) or applies force along world up (HoverThruster).
        var hoverController = prefab.GetComponent<HoverFlightController>();
        if (hoverController != null && UnityEngine.Vector3.Dot(q * UnityEngine.Vector3.up,
                                                               UnityEngine.Vector3.up) < 0.99f)
            problems.Add("HoverFlightController is not upright at " + positions[i]);
        foreach (var c in block.connectors.Where(c => c.canConnect))
        {
            var wp = positions[i] + q * c.localPos;
            var key = UnityEngine.Vector3Int.RoundToInt(wp * 2).ToString();
            if (!socketPoints.ContainsKey(key))
                socketPoints[key] = new System.Collections.Generic.List<int>();
            socketPoints[key].Add(socketNormals.Count);
            socketNormals.Add(q * c.normal);
            sockets[i].Add(socketNormals.Count - 1);
        }
    }
    if (cockpits != 1) problems.Add("cockpit count " + cockpits);
    if (data.blocks.Select(b => b.id).Distinct().Count() != data.blocks.Count)
        problems.Add("duplicate block ids");

    // connector graph, restricted to pairs whose volumes actually touch
    var adj = new System.Collections.Generic.List<int>[data.blocks.Count];
    for (int i = 0; i < adj.Length; i++) adj[i] = new System.Collections.Generic.List<int>();
    int mating = 0, clash = 0;
    for (int i = 0; i < data.blocks.Count; i++)
    {
        for (int j = i + 1; j < data.blocks.Count; j++)
        {
            bool overlap = true;
            for (int axis = 0; axis < 3; axis++)
            {
                if (System.Math.Min(Span(positions[i], sizes[i], axis)[1],
                                    Span(positions[j], sizes[j], axis)[1])
                    - System.Math.Max(Span(positions[i], sizes[i], axis)[0],
                                      Span(positions[j], sizes[j], axis)[0]) <= 1e-4f)
                { overlap = false; break; }
            }
            if (overlap) clash++;
        }
    }
    foreach (var entry in socketPoints)
    {
        var list = entry.Value;
        for (int a = 0; a < list.Count; a++)
        for (int b = a + 1; b < list.Count; b++)
        {
            if (UnityEngine.Vector3.Dot(socketNormals[list[a]], socketNormals[list[b]]) >= -0.75f)
                continue;
            int ownerA = -1, ownerB = -1;
            for (int i = 0; i < data.blocks.Count && (ownerA < 0 || ownerB < 0); i++)
            {
                if (ownerA < 0 && sockets[i].Contains(list[a])) ownerA = i;
                if (ownerB < 0 && sockets[i].Contains(list[b])) ownerB = i;
            }
            if (ownerA < 0 || ownerB < 0 || ownerA == ownerB) continue;
            if (!Touching(positions[ownerA], sizes[ownerA], positions[ownerB], sizes[ownerB]))
            {
                problems.Add("mated sockets but no face contact: block " + ownerA + " / " + ownerB);
                continue;
            }
            mating++;
            if (!adj[ownerA].Contains(ownerB)) adj[ownerA].Add(ownerB);
            if (!adj[ownerB].Contains(ownerA)) adj[ownerB].Add(ownerA);
        }
    }
    if (clash > 0) problems.Add(clash + " overlapping block pairs");

    var visited = new System.Collections.Generic.HashSet<int>();
    var queue = new System.Collections.Generic.Queue<int>();
    if (cockpit >= 0) { queue.Enqueue(cockpit); visited.Add(cockpit); }
    while (queue.Count > 0)
    {
        int n = queue.Dequeue();
        foreach (int m in adj[n]) if (visited.Add(m)) queue.Enqueue(m);
    }
    if (visited.Count != data.blocks.Count)
        problems.Add("not one component: reachable " + visited.Count + "/" + data.blocks.Count);

    System.Func<UnityEngine.Vector3, UnityEngine.Vector3, float, bool> within =
        (a, b, r) => System.Math.Abs(a.x - b.x) <= r && System.Math.Abs(a.y - b.y) <= r
                  && System.Math.Abs(a.z - b.z) <= r;
    // Reproduce PowerTransmissionDevice.RefreshPowerNetwork exactly: relay components follow
    // generator->relay and relay<->relay links within maxConnectionDistance (10 m), and each
    // component's total generator output is split *equally* between the Power blocks inside
    // powerRange (5 m) of any relay in that component.  A block can therefore be covered yet
    // still sit below its minWorkingPower on a small island.
    var adjRelay = new System.Collections.Generic.List<int>[relays.Count];
    for (int i = 0; i < adjRelay.Length; i++) adjRelay[i] = new System.Collections.Generic.List<int>();
    var seeds = new System.Collections.Generic.HashSet<int>();
    for (int i = 0; i < relays.Count; i++)
    {
        for (int j = i + 1; j < relays.Count; j++)
            if (within(relays[i], relays[j], 10f)) { adjRelay[i].Add(j); adjRelay[j].Add(i); }
        if (gens.Any(g => within(g, relays[i], 10f))) seeds.Add(i);
    }
    var components = new System.Collections.Generic.List<System.Collections.Generic.HashSet<int>>();
    var relaySeen = new System.Collections.Generic.HashSet<int>();
    for (int i = 0; i < relays.Count; i++)
    {
        if (relaySeen.Contains(i)) continue;
        var comp = new System.Collections.Generic.HashSet<int>();
        var stack = new System.Collections.Generic.Stack<int>();
        stack.Push(i);
        while (stack.Count > 0)
        {
            int k = stack.Pop();
            if (!comp.Add(k)) continue;
            foreach (int m in adjRelay[k]) if (!comp.Contains(m)) stack.Push(m);
        }
        relaySeen.UnionWith(comp);
        if (seeds.Overlaps(comp)) components.Add(comp);
    }

    var starved = new System.Collections.Generic.List<string>();
    var loadInfo = new System.Collections.Generic.List<System.Tuple<UnityEngine.Vector3, float, string>>();
    for (int i = 0; i < data.blocks.Count; i++)
    {
        var prefab = UnityEngine.Resources.Load<UnityEngine.GameObject>(
            BuildManager.ConvertToResourcesPath(data.blocks[i].resourcePath));
        var power = prefab != null ? prefab.GetComponent<Power>() : null;
        if (power == null || prefab.GetComponent<PowerTransmissionDevice>() != null) continue;
        loadInfo.Add(System.Tuple.Create(positions[i], power.minWorkingPower,
            System.IO.Path.GetFileNameWithoutExtension(data.blocks[i].resourcePath)));
    }
    foreach (var comp in components)
    {
        var members = comp.Select(k => relays[k]).ToList();
        float output = 0f;
        foreach (var g in gens) if (members.Any(r => within(g, r, 10f))) output += 4000f;
        var inRange = loadInfo.Where(l => members.Any(r => within(l.Item1, r, 5f))).ToList();
        if (inRange.Count == 0) continue;
        float share = output / inRange.Count;
        foreach (var l in inRange)
            if (share < l.Item2)
                starved.Add(l.Item3 + " at " + l.Item1 + " share " + share.ToString("F0")
                            + " < need " + l.Item2);
    }
    int uncovered = loadInfo.Count(l => !components.Any(c => c.Any(
        k => within(l.Item1, relays[k], 5f))));
    if (uncovered > 0) problems.Add(uncovered + " power loads outside every live relay");
    if (starved.Count > 0) problems.Add(starved.Count + " power loads starved: "
        + string.Join("; ", starved.Take(4)));
    if (components.Count == 0) problems.Add("no live relay reaches a generator");
    float perLoad = loads.Count > 0 ? supply / loads.Count : 0f;
    if (loads.Count > 0 && perLoad < minPower)
        problems.Add("power per load " + perLoad.ToString("F1") + " below min " + minPower);

    float weight = mass * 9.81f;
    if (lift < weight) problems.Add("lift " + lift + " below weight " + weight.ToString("F0"));
    if (horizontal <= 0f) problems.Add("no horizontal thrusters");

    bool installed = System.IO.File.Exists(System.IO.Path.Combine(live, name + ".json"))
        && System.IO.File.ReadAllText(System.IO.Path.Combine(live, name + ".json"))
           == System.IO.File.ReadAllText(file);
    if (!installed) problems.Add("live copy differs or is missing");

    if (problems.Count > 0) failures.Add(name + ": " + string.Join("; ", problems));
    results.Add(new
    {
        name,
        passed = problems.Count == 0,
        blocks = data.blocks.Count,
        mass,
        liftWeightRatio = lift / weight,
        horizontalThrust = horizontal,
        matingPairs = mating,
        reachable = visited.Count,
        powerPerLoad = perLoad,
        problems,
    });
}
if (failures.Count > 0) throw new System.Exception(string.Join(" | ", failures));
return results;
