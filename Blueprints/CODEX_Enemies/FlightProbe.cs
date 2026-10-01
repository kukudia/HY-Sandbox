// Run in Main Play Mode using eval_file. Uses the real EnemySpawner and EnemyController.
// Disposable runtime changes only. Test ends by exiting Play Mode; never run during unsaved runtime editing.
if(!UnityEditor.EditorApplication.isPlaying || BuildManager.instance==null) throw new System.Exception("Enter Main Play Mode first");
bool oldBackground=UnityEngine.Application.runInBackground;
UnityEngine.Application.runInBackground=true;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(BuildManager).GetMethod("StopActiveBlockLoad",flags).Invoke(BuildManager.instance,null);
BuildManager.instance.enabled=false;
foreach(var u in UnityEngine.Object.FindObjectsByType<ControlUnit>(UnityEngine.FindObjectsSortMode.None)) UnityEngine.Object.Destroy(u.gameObject);
var spawner=UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
if(spawner==null) spawner=new UnityEngine.GameObject("CODEX test spawner").AddComponent<EnemySpawner>();
spawner.enabled=false;
var spawn=typeof(EnemySpawner).GetMethod("SpawnBlockData",flags);
var enemies=new System.Collections.Generic.List<ControlUnit>();
var targets=new System.Collections.Generic.List<UnityEngine.Transform>();
var starts=new System.Collections.Generic.List<UnityEngine.Vector3>();
var checks=new System.Collections.Generic.List<object>();
int index=0;
foreach(var file in System.IO.Directory.GetFiles("Blueprints/CODEX_Enemies","CODEX_E*.json").OrderBy(p=>p))
{
    var data=UnityEngine.JsonUtility.FromJson<BlockDataList>(System.IO.File.ReadAllText(file));
    var origin=new UnityEngine.Vector3(index*240,63,0);
    var unit=(ControlUnit)spawn.Invoke(spawner,new object[]{data,System.IO.Path.GetFileNameWithoutExtension(file),origin,UnityEngine.Quaternion.Euler(0,index*47,0)});
    if(unit==null) throw new System.Exception("Spawn failed");
    var groups=BlockGroupManager.GroupBlocks(unit.GetComponentsInChildren<Block>().ToList());
    if(groups.Count!=1) throw new System.Exception("Disconnected blueprint "+unit.name);
    // Isolate flight from battle damage; enemy AI and original mass, thrust, power and PID remain unchanged.
    foreach(var gun in unit.GetComponentsInChildren<TurretWeapon>()) gun.enabled=false;
    var target=UnityEngine.Object.Instantiate(UnityEngine.Resources.Load<UnityEngine.GameObject>("BlocksParent/BlocksParent"));
    target.name="CODEX stationary test target "+index;
    target.transform.position=origin+new UnityEngine.Vector3(110,-3,0);
    var cockpit=UnityEngine.Object.Instantiate(UnityEngine.Resources.Load<UnityEngine.GameObject>("Blocks/Cockpit"),target.transform);
    cockpit.transform.localPosition=UnityEngine.Vector3.zero;
    var player=target.GetComponent<ControlUnit>(); player.RefreshChildren(); player.enabled=false;
    PlayManager.instance.RegisterControlUnit(player);
    var targetBody=target.GetComponent<UnityEngine.Rigidbody>();
    targetBody.isKinematic=true;
    targetBody.position=origin+new UnityEngine.Vector3(110,-3,0);
    UnityEngine.Physics.SyncTransforms();
    enemies.Add(unit);targets.Add(target.transform);starts.Add(origin);
    checks.Add(new{name=unit.name,groups=groups.Count,blocks=data.blocks.Count,faction=unit.faction.ToString()});
    index++;
}
PlayManager.instance.playMode=true;
// Test lanes are separated to prevent shared power/target interactions; retain the distant lanes for this test.
PlayManager.instance.groupCleanupDistance=0;
PlayManager.instance.blocksParent=targets[0];
string output=System.IO.Path.GetFullPath("Blueprints/CODEX_Enemies/Flight.csv");
System.IO.File.WriteAllText(output,"name,seconds,heightError,tiltDegrees,verticalSpeed,horizontalSpeed,unpowered,blocks,targetDistance\n");
System.IO.File.WriteAllText("Blueprints/CODEX_Enemies/Flight.completed","Running; not completed yet.\n");
double began=UnityEngine.Time.timeAsDouble;
int previous=-1;
bool acquired=false, moved=false, lost=false;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    try
    {
        if(!UnityEditor.EditorApplication.isPlaying){UnityEngine.Application.runInBackground=oldBackground;UnityEditor.EditorApplication.update-=tick;return;}
        float elapsed=(float)(UnityEngine.Time.timeAsDouble-began);
        if(elapsed>=15 && !acquired){for(int i=0;i<targets.Count;i++) targets[i].GetComponent<UnityEngine.Rigidbody>().position=starts[i]+new UnityEngine.Vector3(35,-3,0); UnityEngine.Physics.SyncTransforms();acquired=true;}
        if(elapsed>=45 && !moved){for(int i=0;i<targets.Count;i++) targets[i].GetComponent<UnityEngine.Rigidbody>().position=starts[i]+new UnityEngine.Vector3(-25,-3,25); UnityEngine.Physics.SyncTransforms();moved=true;}
        if(elapsed>=75 && !lost){for(int i=0;i<targets.Count;i++) targets[i].GetComponent<UnityEngine.Rigidbody>().position=enemies[i].transform.position+new UnityEngine.Vector3(0,0,150); UnityEngine.Physics.SyncTransforms();lost=true;}
        if((int)(elapsed*5)!=previous)
        {
            previous=(int)(elapsed*5);
            foreach(var unit in enemies)
            {
                if(unit==null || !unit.HasValidCockpit) throw new System.Exception("Lost enemy");
                var h=unit.hoverFlightController;var rb=unit.GetComponent<UnityEngine.Rigidbody>();
                float horizontal=new UnityEngine.Vector2(rb.linearVelocity.x,rb.linearVelocity.z).magnitude;
                string line=string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:F3},{2:F4},{3:F4},{4:F4},{5:F4},{6},{7},{8:F4}\n",unit.name,elapsed,h.CurrentHeight-h.TargetHeight,UnityEngine.Vector3.Angle(unit.transform.up,UnityEngine.Vector3.up),rb.linearVelocity.y,horizontal,unit.GetComponentsInChildren<Power>().Count(p=>!p.isWorking),unit.GetComponentsInChildren<Block>().Length,unit.target!=null?UnityEngine.Vector3.Distance(unit.transform.position,unit.target.position):-1);
                System.IO.File.AppendAllText(output,line);
            }
        }
        if(elapsed>=90){System.IO.File.WriteAllText("Blueprints/CODEX_Enemies/Flight.completed","90 seconds: idle, pursuit, turn, target loss; all three enemies remained valid. Assess Flight.csv thresholds separately.\n");UnityEditor.EditorApplication.update-=tick;UnityEngine.Application.runInBackground=oldBackground;UnityEditor.EditorApplication.isPlaying=false;}
    }
    catch(System.Exception ex){System.IO.File.AppendAllText(output,"ERROR: "+ex+"\n");UnityEditor.EditorApplication.update-=tick;UnityEngine.Application.runInBackground=oldBackground;UnityEditor.EditorApplication.isPlaying=false;}
};
UnityEditor.EditorApplication.update+=tick;
return new{checks,output,duration=90,targetAcquiredAt=15,targetRelocationAt=45,targetLostAt=75};
