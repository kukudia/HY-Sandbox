// 职责：验证炮塔目录按钮和已安装 CODEX 敌机的真实生成路径。
// 入口：Main Play Mode 中 unity command eval_file --file Blueprints/CODEX_Enemies/TurretProbe.cs --json。
// 仅用于临时验证：会切换当前建造资源并临时禁用 EnemySpawner，完成后退出 Play Mode 恢复。
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play required");
var checks = new System.Collections.Generic.List<object>();
foreach (var item in UnityEngine.Object.FindObjectsByType<BuildPaletteItem>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None).Where(i => i.BlockName.StartsWith("ProjectileTurret")))
{
 var source=UnityEngine.Resources.Load<UnityEngine.GameObject>("BuildPaletteEntries/"+item.BlockName).GetComponent<BuildPaletteItem>();
 var button=item.GetComponent<UnityEngine.UI.Button>();
 bool style=button.colors.Equals(source.GetComponent<UnityEngine.UI.Button>().colors) && button.targetGraphic==item.GetComponent<UnityEngine.UI.Image>() && item.GetComponent<UIInteractionFeedback>()!=null;
 item.BindPalette(item.GetComponentInParent<BuildPalette>()); button.onClick.Invoke();
 bool passed=style && !item.name.Contains("(Clone)") && BuildManager.instance.currentBlockResourcePath=="Blocks/"+item.BlockName && BuildManager.instance.currentBuildType==BuildType.Build;
 if(!passed) throw new System.Exception("Palette failed "+item.name);
 checks.Add(new {item.name,style,click=true});
}
var spawner=UnityEngine.Object.FindFirstObjectByType<EnemySpawner>(); spawner.enabled=false;
var spawn=typeof(EnemySpawner).GetMethod("SpawnBlockData",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
int index=0;
SaveManager.instance.GetAllEnemyBlueprintNames();
foreach(var name in SaveManager.instance.enemyBlueprints.Where(n=>n.StartsWith("CODEX_E")))
{
 var data=UnityEngine.JsonUtility.FromJson<BlockDataList>(System.IO.File.ReadAllText(SaveManager.instance.GetEnemyBlueprintPath(name)));
 var unit=(ControlUnit)spawn.Invoke(spawner,new object[]{data,name,new UnityEngine.Vector3(500+index++*200,100,0),UnityEngine.Quaternion.identity});
 var guns=unit.GetComponentsInChildren<ProjectileTurret>();
 int groups=BlockGroupManager.GroupBlocks(unit.GetComponentsInChildren<Block>().ToList()).Count;
 if(guns.Length!=2||groups!=1||unit.faction!=UnitFaction.Enemy) throw new System.Exception("Enemy failed "+name);
 checks.Add(new {name,projectileTurrets=guns.Length,barrels=guns.Sum(g=>g.Muzzles.Length),lasers=unit.GetComponentsInChildren<LaserTurret>().Length,groups,enemy=true});
 UnityEngine.Object.Destroy(unit.gameObject);
}
return checks;
