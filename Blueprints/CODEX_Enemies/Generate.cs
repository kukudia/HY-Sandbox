// Execute using unity command eval_file --file Blueprints/CODEX_Enemies/Generate.cs --json
// Stock parts only; deterministic IDs, no stat overrides. Refuses to overwrite changed blueprints.
string folder = System.IO.Path.GetFullPath("Blueprints/CODEX_Enemies");
string destination = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "EnemyBlueprints");
System.IO.Directory.CreateDirectory(folder);
System.IO.Directory.CreateDirectory(destination);
var reports = new System.Collections.Generic.List<object>();
for (int variant = 0; variant < 3; variant++)
{
    string name = new[] { "CODEX_E01_Finch", "CODEX_E02_Pike", "CODEX_E03_Manta" }[variant];
    int width = variant == 2 ? 4 : 2;
    int length = variant == 1 ? 4 : 2;
    var data = new BlockDataList();
    var occupied = new System.Collections.Generic.HashSet<UnityEngine.Vector3Int>();
    float mass = 0, lift = 0, demand = 0;
    UnityEngine.Vector3 moment = UnityEngine.Vector3.zero;
    System.Action<string,float,float,float> add = (type,x,y,z) =>
    {
        var prefab = UnityEngine.Resources.Load<UnityEngine.GameObject>("Blocks/" + type);
        if (prefab == null) throw new System.Exception(type);
        var b = prefab.GetComponent<Block>();
        var d = UnityEngine.JsonUtility.FromJson<BlockData>("{}");
        d.id = name + "_" + data.blocks.Count.ToString("D3");
        d.x = b.x; d.y = b.y; d.z = b.z;
        // Align the flight controller to local Y=0 for a consistent blueprint altitude reference.
        d.posX = x; d.posY = y - 1.5f; d.posZ = z; d.rotW = 1;
        d.resourcePath = "Assets/Resources/Blocks/" + type + ".prefab";
        for (int ix=0;ix<b.x;ix++) for(int iy=0;iy<b.y;iy++) for(int iz=0;iz<b.z;iz++)
            if(!occupied.Add(UnityEngine.Vector3Int.FloorToInt(new UnityEngine.Vector3(x-b.x*.5f+ix+.5f,y-b.y*.5f+iy+.5f,z-b.z*.5f+iz+.5f)))) throw new System.Exception("Overlap " + name);
        data.blocks.Add(d); mass += b.mass; moment += b.mass * new UnityEngine.Vector3(d.posX,d.posY,d.posZ);
        if(prefab.GetComponent<HoverThruster>()!=null) lift+=prefab.GetComponent<HoverThruster>().maxThrust;
        if(prefab.GetComponent<Power>()!=null) demand+=prefab.GetComponent<Power>().standardWorkingPower;
    };
    // Continuous simple 2m-cell deck, all four faces of the central keel remain supported.
    for(int x=-width;x<=width;x+=2) for(int z=-length;z<=length;z+=2) add("2x2x2",x,0,z);
    add("Cockpit",0,2,1); add("PowerGeneratingUnit",0,2,-1);
    add("HoverFlightController",-1.5f,1.5f,.5f); add("1x1x1",1.5f,1.5f,-.5f);
    add("PowerTransmissionDevice",1.5f,1.5f,.5f); add("PowerTransmissionDevice",-1.5f,1.5f,-.5f);
    if(variant==2)
    {
        foreach(int x in new[]{-4,0,4}) foreach(int z in new[]{-2,2}) add("HoverThrusterBig",x,-2,z);
    }
    else
    {
        foreach(int x in new[]{-2,2}) foreach(int z in (variant==1?new[]{-4,0,4}:new[]{-2,2})) add("HoverThrusterBig",x,-2,z);
    }
    foreach(float x in new[]{-width-.5f,width+.5f}) foreach(float z in new[]{-length+.5f,length-.5f}) add("UniversalThruster",x,1.5f,z);
    foreach(float x in new[]{-1.5f,1.5f}) add("Turret",x,1.5f,length+.5f);
    // Rear pair balances weapon mass and provides rear coverage on the larger variants.
    foreach(float x in new[]{-1.5f,1.5f}) add(variant==0?"1x1x1":"Turret",x,1.5f,-length-.5f);
    string json=UnityEngine.JsonUtility.ToJson(data,true);
    foreach(string target in new[]{folder,destination})
    {
        string path=System.IO.Path.Combine(target,name+".json");
        if(System.IO.File.Exists(path) && System.IO.File.ReadAllText(path)!=json) throw new System.Exception("Existing different file, preserve or rename first: "+path);
        System.IO.File.WriteAllText(path,json,new System.Text.UTF8Encoding(false));
    }
    reports.Add(new{name,blocks=data.blocks.Count,mass,centerOfMass=(moment/mass).ToString("F3"),lift,liftWeightRatio=lift/(mass*9.81f),demand,supply=UnityEngine.Resources.Load<UnityEngine.GameObject>("Blocks/PowerGeneratingUnit").GetComponent<PowerGeneratingUnit>().outputPower});
}
return new{destination,reports};
