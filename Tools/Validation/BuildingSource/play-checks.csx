if(!EditorApplication.isPlaying) throw new System.Exception("Play Mode required");
var session=UnityEngine.Object.FindFirstObjectByType<WorldSessionManager>();
var saves=SaveManager.instance;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var field=typeof(SaveManager).GetField("_buildingFiles",flags);
var original=field.GetValue(saves);
var storeType=typeof(SaveManager).Assembly.GetType("BlueprintFileStore");
string directory=System.IO.Path.GetFullPath("Temp/BuildingSource/PlayFiles");
System.IO.Directory.CreateDirectory(directory);
string path=System.IO.Path.Combine(directory,"OnlyDiskBuilding.json");
var source=Resources.Load<TextAsset>("WorldBlueprints/Buildings/Field Workshop");
System.IO.File.WriteAllText(path,source.text);
var store=System.Activator.CreateInstance(storeType,new object[]{directory});
var checks=new System.Collections.Generic.List<string>();
var errors=new System.Collections.Generic.List<string>();
var realNames=System.IO.Directory.GetFiles(System.IO.Path.Combine(Application.persistentDataPath,"BuildingBlueprints"),"*.json").Select(System.IO.Path.GetFileNameWithoutExtension).ToArray();
if(session.State!=WorldSessionState.Ready || session.BuildingTemplates.Any(n=>!realNames.Contains(n))) throw new System.Exception("Real session includes missing file or is not ready");
checks.Add("Real Main world session contains only names in the actual BuildingBlueprints directory");
field.SetValue(saves,store);
bool originalBackground=Application.runInBackground;
Application.runInBackground=true;
session.BeginPreparation();
int phase=0;
double deadline=EditorApplication.timeSinceStartup+120;
EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool done=false;
    try
    {
        if(EditorApplication.timeSinceStartup>deadline) throw new System.Exception("World preparation timeout");
        if(session.State==WorldSessionState.Failed) throw new System.Exception(session.Status);
        if(session.State!=WorldSessionState.Ready) return;
        if(phase==0)
        {
            if(!session.BuildingTemplates.SequenceEqual(new[]{"OnlyDiskBuilding"})) throw new System.Exception("Unexpected world template");
            checks.Add("Actual world preparation selects only the isolated on-disk blueprint");
            if(session.Buildings.Buildings.Count==0 || session.Buildings.Buildings.Any(b=>b.Capture().blueprintName!="OnlyDiskBuilding")) throw new System.Exception("Unexpected generated building");
            checks.Add("Real generated buildings all use the sole on-disk blueprint");
            System.IO.File.Delete(path);
            session.BeginPreparation();
            phase=1;
        }
        else
        {
            if(session.BuildingTemplates.Count!=0 || session.Buildings.Buildings.Count!=0 || session.DormantBuildingCount!=0) throw new System.Exception("Deleted template survived the new session");
            checks.Add("After deleting the last file and rebuilding the session, templates, live and dormant buildings are all empty");
            done=true;
        }
    }
    catch(System.Exception error) { errors.Add(error.ToString()); done=true; }
    if(done)
    {
        EditorApplication.update-=tick;
        field.SetValue(saves,original);
        Application.runInBackground=originalBackground;
        System.IO.File.WriteAllText("Temp/BuildingSource/play.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,unity=Application.unityVersion,checks,errors},Newtonsoft.Json.Formatting.Indented));
        session.EndSession();
        EditorApplication.isPlaying=false;
    }
};
EditorApplication.update+=tick;
return "started isolated real world session regression";
