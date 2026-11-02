// Isolated Editor checks; never copy packaged templates into the real user directory.
var checks = new System.Collections.Generic.List<string>();
var directory = System.IO.Path.Combine(Application.temporaryCachePath, "BuildingSource_" + System.Guid.NewGuid().ToString("N"));
System.IO.Directory.CreateDirectory(directory);
var previous = SaveManager.instance;
var host = new GameObject("Isolated building source checks");
host.SetActive(false);
var saves = host.AddComponent<SaveManager>();
var binding = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var assembly = typeof(SaveManager).Assembly;
var storeType = assembly.GetType("BlueprintFileStore");
var libraryType = assembly.GetType("WorldBuildingLibrary");
var store = System.Activator.CreateInstance(storeType, new object[] { directory });
var source = Resources.LoadAll<TextAsset>("WorldBlueprints/Buildings").First(asset => JsonUtility.FromJson<BlockDataList>(asset.text).blocks.Count > 0);
var library = System.Activator.CreateInstance(libraryType, true);
var load = libraryType.GetMethod("Load", binding);
var names = (System.Collections.Generic.List<string>)libraryType.GetField("Names", binding).GetValue(library);
var errors = (System.Collections.Generic.List<string>)libraryType.GetField("Errors", binding).GetValue(library);
var catalog = ScriptableObject.CreateInstance<WorldBuildingCatalog>();
System.Action<bool,string> assert = (ok,message) => { if (!ok) throw new System.Exception(message); checks.Add(message); };
try
{
    SaveManager.instance = saves;
    typeof(SaveManager).GetField("_buildingFiles", binding).SetValue(saves, store);
    load.Invoke(library, new object[] { store });
    assert(names.Count == 0, "Empty shared store ignores all packaged templates");
    var routine = (System.Collections.IEnumerator)libraryType.GetMethod("LoadRoutine", binding).Invoke(library,new object[]{store});
    while(routine.MoveNext()) {}
    assert(names.Count == 0, "Empty world generation routine has no fallback buildings");
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "UserOnly.json"), source.text);
    load.Invoke(library, new object[] { store });
    assert(names.SequenceEqual(new[]{"UserOnly"}), "Library only contains the file explicitly present in BuildingBlueprints");
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, source.name + ".json"), "{broken");
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "Invalid.json"), "{\"foundation\":{\"width\":-1,\"length\":4}}");
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "Oversized.json"), new string(' ',4*1024*1024+1));
    string lockedPath = System.IO.Path.Combine(directory,"Locked.json");
    System.IO.File.WriteAllText(lockedPath, source.text);
    using (var locked = new System.IO.FileStream(lockedPath,System.IO.FileMode.Open,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None))
    {
        load.Invoke(library,new object[]{store});
    }
    assert(errors.Count == 4 && names.SequenceEqual(new[]{"UserOnly"}), "Malformed, invalid, oversized and locked files are skipped without packaged fallback");
    var below=JsonUtility.FromJson<BlockDataList>(source.text);
    below.blocks[0].posY=-100;
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory,source.name+".json"),JsonUtility.ToJson(below));
    var read=(System.Collections.IEnumerable)libraryType.GetMethod("ReadSources",binding).Invoke(library,new object[]{store,true,null});
    foreach(var step in read) {}
    assert(!names.Contains(source.name) && names.Contains("UserOnly"), "World platform rejection does not resurrect same-name packaged template");
    System.IO.File.Delete(lockedPath);
    System.IO.File.Delete(System.IO.Path.Combine(directory,source.name+".json"));
    var manager=host.AddComponent<WorldBuildingManager>();
    catalog.blueprints=new[]{source};
    catalog.initialBuildings=new[]{new WorldBuildingCatalog.InitialBuilding{blueprint=source}};
    typeof(WorldBuildingManager).GetField("catalog",binding).SetValue(manager,catalog);
    var palette=host.AddComponent<WorldBuildingPalette>();
    typeof(WorldBuildingPalette).GetField("manager",binding).SetValue(palette,manager);
    palette.RefreshCatalog();
    var paletteNames=(System.Collections.Generic.List<string>)typeof(WorldBuildingPalette).GetField("names",binding).GetValue(palette);
    assert(paletteNames.SequenceEqual(new[]{"UserOnly"}), "WorldLab palette reads the shared store and ignores Catalog blueprint content");
    System.IO.File.Delete(System.IO.Path.Combine(directory,source.name+".json"));
    manager.ResetBuildings();
    assert(manager.Buildings.Count==0 && manager.LastMessage.Contains("not found in BuildingBlueprints"), "Initial layout skips packaged blueprint missing from shared store");
    System.IO.File.Delete(System.IO.Path.Combine(directory,"UserOnly.json"));
    palette.RefreshCatalog();
    assert(paletteNames.Count==0, "Refreshing after deleting the last valid file leaves WorldLab palette empty");
    load.Invoke(library,new object[]{store});
    assert(names.Count==0, "Reusing the library after deletion clears previous templates");
    routine=(System.Collections.IEnumerator)libraryType.GetMethod("LoadRoutine",binding).Invoke(library,new object[]{store});
    int steps=0;
    while(routine.MoveNext()) { if(++steps>1000) throw new System.Exception("Unexpected warmup"); }
    assert(names.Count==0 && (float)libraryType.GetProperty("Progress",binding).GetValue(library)==1f,"Reentering world generation after deletion completes empty without builtin fallback");
    SaveManager.instance=null;
    var getStore=typeof(SaveManager).GetMethod("GetBuildingBlueprintFiles",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
    var fallback=getStore.Invoke(null,null);
    string actual=(string)storeType.GetMethod("GetPath").Invoke(fallback,new object[]{"test"});
    assert(System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(actual))==System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.persistentDataPath,"BuildingBlueprints")),"Standalone WorldLab resolves the same BuildingBlueprints directory");
}
finally
{
    SaveManager.instance=previous;
    UnityEngine.Object.DestroyImmediate(host);
    UnityEngine.Object.DestroyImmediate(catalog);
    System.IO.Directory.Delete(directory,true);
}
System.IO.File.WriteAllText("Temp/BuildingSource/isolated.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
return checks;
