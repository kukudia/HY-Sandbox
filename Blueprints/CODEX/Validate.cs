// unity command eval_file --file Blueprints/CODEX/Validate.cs --timeout 60000 --json
// Exact stock connector geometry and capability checks, plus wireless coverage and duplicate-ID checks.
var results = new System.Collections.Generic.List<object>();
foreach (var file in System.IO.Directory.GetFiles("Blueprints/CODEX", "CODEX_*.json").OrderBy(p => p))
{
    var data = UnityEngine.JsonUtility.FromJson<BlockDataList>(System.IO.File.ReadAllText(file));
    var graph = data.blocks.Select(b => new System.Collections.Generic.HashSet<int>()).ToArray();
    var sockets = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, System.Collections.Generic.List<System.Tuple<int, UnityEngine.Vector3>>>();
    var relays = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var loads = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var gens = new System.Collections.Generic.List<UnityEngine.Vector3>();
    int cockpit = -1, cockpitCount = 0;
    for (int i = 0; i < data.blocks.Count; i++)
    {
        var d = data.blocks[i];
        var prefab = UnityEngine.Resources.Load<UnityEngine.GameObject>(BuildManager.ConvertToResourcesPath(d.resourcePath));
        if (prefab == null) throw new System.Exception("Missing " + d.resourcePath);
        var b = prefab.GetComponent<Block>();
        var p = new UnityEngine.Vector3(d.posX, d.posY, d.posZ);
        var q = new UnityEngine.Quaternion(d.rotX, d.rotY, d.rotZ, d.rotW);
        if (prefab.GetComponent<Cockpit>() != null) { cockpit = i; cockpitCount++; }
        if (prefab.GetComponent<PowerTransmissionDevice>() != null) relays.Add(p);
        if (prefab.GetComponent<Power>() != null) loads.Add(p);
        if (prefab.GetComponent<PowerGeneratingUnit>() != null) gens.Add(p);
        if (d.x != b.x || d.y != b.y || d.z != b.z) throw new System.Exception("Size mismatch");
        foreach (var c in b.connectors.Where(c => c.canConnect))
        {
            var wp = p + q * c.localPos;
            var key = UnityEngine.Vector3Int.RoundToInt(wp * 2);
            if (!sockets.ContainsKey(key)) sockets[key] = new System.Collections.Generic.List<System.Tuple<int, UnityEngine.Vector3>>();
            sockets[key].Add(System.Tuple.Create(i, q * c.normal));
        }
    }
    foreach (var socket in sockets.Values) foreach (var a in socket) foreach (var b in socket)
        if (a.Item1 != b.Item1 && UnityEngine.Vector3.Dot(a.Item2, b.Item2) < -.75f) graph[a.Item1].Add(b.Item1);
    var visited = new System.Collections.Generic.HashSet<int>();
    var queue = new System.Collections.Generic.Queue<int>(); queue.Enqueue(cockpit); visited.Add(cockpit);
    while (queue.Count > 0) foreach (int n in graph[queue.Dequeue()]) if (visited.Add(n)) queue.Enqueue(n);
    System.Func<UnityEngine.Vector3, UnityEngine.Vector3, float, bool> within = (a,b,r) => UnityEngine.Mathf.Abs(a.x-b.x)<=r && UnityEngine.Mathf.Abs(a.y-b.y)<=r && UnityEngine.Mathf.Abs(a.z-b.z)<=r;
    var liveRelays = new System.Collections.Generic.HashSet<int>();
    for (int i=0;i<relays.Count;i++) if(gens.Any(g=>within(g,relays[i],10))) liveRelays.Add(i);
    bool changed = true;
    while(changed) { changed=false; for(int i=0;i<relays.Count;i++) if(!liveRelays.Contains(i) && liveRelays.Any(j=>within(relays[i],relays[j],10))) {liveRelays.Add(i);changed=true;} }
    int uncovered=loads.Count(p=>!liveRelays.Any(i=>within(p,relays[i],5)));
    bool unique=data.blocks.Select(b=>b.id).Distinct().Count()==data.blocks.Count;
    bool identical=System.IO.File.ReadAllText(file)==System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath,"Saves",System.IO.Path.GetFileName(file)));
    bool passed=cockpitCount==1 && visited.Count==data.blocks.Count && uncovered==0 && unique && identical;
    results.Add(new { name=System.IO.Path.GetFileNameWithoutExtension(file), passed, blocks=data.blocks.Count, connected=visited.Count, uncovered, liveRelays=liveRelays.Count, unique, identical });
    if (!passed) throw new System.Exception("Validation failed: " + file + "; connected=" + visited.Count + "/" + data.blocks.Count + "; uncovered=" + uncovered);
}
return results;
