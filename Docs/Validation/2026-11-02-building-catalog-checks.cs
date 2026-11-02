// Isolated checks use a temporary injected BuildingFiles store, never the user's blueprint directory.
var checks = new System.Collections.Generic.List<string>();
var directory = System.IO.Path.Combine(Application.temporaryCachePath, "BuildingCatalog_" + System.Guid.NewGuid().ToString("N"));
System.IO.Directory.CreateDirectory(directory);
var previous = SaveManager.instance;
var host = new GameObject("Isolated building catalog checks");
host.SetActive(false);
var saves = host.AddComponent<SaveManager>();
var binding = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var assembly = typeof(SaveManager).Assembly;
var storeType = assembly.GetType("BlueprintFileStore");
var libraryType = assembly.GetType("WorldBuildingLibrary");
var store = System.Activator.CreateInstance(storeType, new object[] { directory });
var source = Resources.LoadAll<TextAsset>("WorldBlueprints/Buildings").First(asset => JsonUtility.FromJson<BlockDataList>(asset.text).blocks.Count > 0);
var hash = new System.Func<string, string>(path => System.Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.IO.File.ReadAllBytes(path))));
try
{
    SaveManager.instance = saves;
    typeof(SaveManager).GetField("_buildingFiles", binding).SetValue(saves, store);
    var getStore = typeof(SaveManager).GetMethod("GetBuildingBlueprintFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    if (!object.ReferenceEquals(getStore.Invoke(null, null), store)) throw new System.Exception("Injected store bypassed");
    checks.Add("shared accessor preserves SaveManager injected BuildingFiles");
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "UserBuilding.json"), source.text);
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, source.name + ".json"), source.text);
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "Broken.json"), "{broken");
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "Invalid.json"), "{\"foundation\":{\"width\":-1,\"length\":4}}");
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "Oversized.json"), new string(' ', 4 * 1024 * 1024 + 1));
    string lockedPath = System.IO.Path.Combine(directory, "Locked.json");
    System.IO.File.WriteAllText(lockedPath, source.text);
    var baseline = System.IO.Directory.GetFiles(directory).ToDictionary(path => path, hash);
    saves.GetAllBuildingBlueprintNames();
    if (!saves.buildingBlueprints.Contains("UserBuilding")) throw new System.Exception("Name list bypassed injected store");
    checks.Add("SaveManager name list reads the injected store");
    var library = System.Activator.CreateInstance(libraryType, true);
    var load = libraryType.GetMethod("Load", binding);
    using (var locked = new System.IO.FileStream(lockedPath, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None))
    {
        load.Invoke(library, new object[] { getStore.Invoke(null,null), new TextAsset[] { source } });
    }
    var names = (System.Collections.Generic.List<string>)libraryType.GetField("Names", binding).GetValue(library);
    var errors = (System.Collections.Generic.List<string>)libraryType.GetField("Errors", binding).GetValue(library);
    if (!names.Contains("UserBuilding") || names.Count(name=>name==source.name)!=1) throw new System.Exception("Merge failed");
    checks.Add("user template and bundled template merge with one entry per name");
    if (errors.Count != 4 || names.Contains("Broken") || names.Contains("Invalid") || names.Contains("Oversized") || names.Contains("Locked")) throw new System.Exception("Invalid-file isolation failed: "+string.Join(";",errors));
    checks.Add("malformed, invalid, oversized and locked files are isolated");
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory,source.name+".json"), "{broken");
    load.Invoke(library, new object[] { store, new TextAsset[] {source} });
    if (!names.Contains(source.name)) throw new System.Exception("Invalid override removed bundled template");
    checks.Add("invalid user override preserves bundled fallback");
    var below = JsonUtility.FromJson<BlockDataList>(source.text);
    below.blocks[0].posY = -100;
    System.IO.File.WriteAllText(System.IO.Path.Combine(directory,source.name+".json"),JsonUtility.ToJson(below));
    var worldRead=(System.Collections.IEnumerable)libraryType.GetMethod("ReadSources",binding).Invoke(library,new object[]{store,new TextAsset[]{source},true,null});
    foreach(var step in worldRead) {}
    var worldTemplates=(System.Collections.Generic.List<BlockDataList>)libraryType.GetField("Templates",binding).GetValue(library);
    if(!names.Contains(source.name) || worldTemplates[names.IndexOf(source.name)].blocks[0].posY<0)throw new System.Exception("Invalid world override removed bundled fallback");
    checks.Add("world-only platform rejection preserves valid bundled fallback");

    System.IO.File.WriteAllText(System.IO.Path.Combine(directory,source.name+".json"), source.text);
    var manager = host.AddComponent<WorldBuildingManager>();
    var catalog = ScriptableObject.CreateInstance<WorldBuildingCatalog>();
    catalog.blueprints = new TextAsset[] {source};
    typeof(WorldBuildingManager).GetField("catalog",binding).SetValue(manager,catalog);
    var palette = host.AddComponent<WorldBuildingPalette>();
    typeof(WorldBuildingPalette).GetField("manager",binding).SetValue(palette,manager);
    palette.RefreshCatalog();
    var paletteNames=(System.Collections.Generic.List<string>)typeof(WorldBuildingPalette).GetField("names",binding).GetValue(palette);
    if(!paletteNames.Contains("UserBuilding") || paletteNames.Count(name=>name==source.name)!=1)throw new System.Exception("Palette did not share catalog");
    checks.Add("WorldLab palette uses the same injected file store and merge policy");
    UnityEngine.Object.DestroyImmediate(catalog);
    if (!baseline.All(pair=>hash(pair.Key)==pair.Value)) throw new System.Exception("Source file changed");
    checks.Add("all fixture source bytes preserved by reads");
    SaveManager.instance=null;
    if(getStore.Invoke(null,null)==null) throw new System.Exception("Standalone fallback missing");
    checks.Add("standalone WorldLab resolves the same store type without a SaveManager");
}
finally
{
    SaveManager.instance=previous;
    UnityEngine.Object.DestroyImmediate(host);
    System.IO.Directory.Delete(directory,true);
}
System.IO.File.WriteAllText("Temp/BuildingCatalog/isolated.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
return checks;
