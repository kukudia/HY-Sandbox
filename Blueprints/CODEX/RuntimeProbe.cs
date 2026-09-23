// Optional short automated Play Mode smoke test. Requires the Main scene already in Editor Play Mode.
// Changes only temporary runtime state; does not save a scene or modify any existing player blueprint.
if (!UnityEditor.EditorApplication.isPlaying || SaveManager.instance == null) throw new System.Exception("Enter Play Mode in Main first.");
var names = new[] { "CODEX_01_Bastion", "CODEX_02_Manta", "CODEX_03_Catamaran", "CODEX_04_Crossguard", "CODEX_05_Lance" };
string report = System.IO.Path.GetFullPath("Blueprints/CODEX/Runtime.tsv");
if (System.IO.File.Exists(report)) System.IO.File.Copy(report, report + "." + System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff") + ".bak");
System.IO.File.WriteAllText(report, "name\tphase\tseconds\tblocks\tcockpitHP\tunpowered\tposition\tspeed\tenemies\n");
float previousInterval = BuildManager.instance.BlockLoadIntervalSeconds;
BuildManager.instance.BlockLoadIntervalSeconds = 0f;
int index = 0, stage = 0, lastSample = -1;
float start = 0;
double loadStart = UnityEditor.EditorApplication.timeSinceStartup;
UnityEngine.Vector3 origin = UnityEngine.Vector3.zero;
UnityEditor.EditorApplication.CallbackFunction tick = null;
System.Action release = () => { if (UnityEngine.InputSystem.Keyboard.current != null) UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); };
System.Action finish = () => { release(); UnityEditor.EditorApplication.update -= tick; if (BuildManager.instance != null) BuildManager.instance.BlockLoadIntervalSeconds = previousInterval; UnityEditor.EditorApplication.isPlaying = false; };
tick = () =>
{
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying || SaveManager.instance == null) { finish(); return; }
        if (stage == 0)
        {
            PlayManager.instance.playMode = false;
            BuildManager.instance.enabled = true;
            SaveManager.instance.LoadSave(names[index]);
            loadStart = UnityEditor.EditorApplication.timeSinceStartup;
            stage = 1;
            return;
        }
        if (stage == 1)
        {
            if (BuildManager.instance.IsLoadingBlocks)
            {
                if (UnityEditor.EditorApplication.timeSinceStartup - loadStart > 180) throw new System.Exception("Load timeout " + names[index]);
                return;
            }
            var all = GameManager.instance.blocksParent.GetComponentsInChildren<Block>().ToList();
            var groups = BlockGroupManager.GroupBlocks(all);
            if (groups.Count != 1) throw new System.Exception("Disconnected groups: " + groups.Count);
            string reason;
            if (!PlayManager.instance.CanStartPlay(out reason)) throw new System.Exception(reason);
            var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) { spawner.spawnInterval = 5f; spawner.spawnOnPlayStart = true; }
            MainUIPanels.instance.PlayStart();
            // Same hover state transition as the player's first Space press.
            var unit = GameManager.instance.blocksParent.GetComponent<ControlUnit>();
            foreach (var thruster in unit.hoverThrusters) thruster.isHovered = true;
            unit.hoverFlightController.targetHoverHeight = (int)unit.transform.position.y + 10;
            unit.hoverFlightController.setHeight = true;
            // The Editor may discard synthetic keyboard events when Game View lacks focus.
            // Drive the public movement-input API instead; physical thrusters remain unmodified.
            unit.enabled = false;
            origin = GameManager.instance.blocksParent.position;
            start = UnityEngine.Time.time; lastSample = -1; stage = 2;
            return;
        }
        float elapsed = UnityEngine.Time.time - start;
        int second = UnityEngine.Mathf.FloorToInt(elapsed);
        var parent = GameManager.instance.blocksParent;
        var cockpit = parent != null ? parent.GetComponentInChildren<Cockpit>() : null;
        if (parent != null)
        {
            var unit = parent.GetComponent<ControlUnit>();
            unit.enabled = false;
            unit.SetMovementInput(elapsed < 4 ? UnityEngine.Vector3.zero : elapsed < 8 ? UnityEngine.Vector3.forward : elapsed < 12 ? UnityEngine.Vector3.left : elapsed < 16 ? UnityEngine.Vector3.back : elapsed < 20 ? UnityEngine.Vector3.right : UnityEngine.Vector3.zero);
        }
        if (second != lastSample)
        {
            lastSample = second;
            var blocks = parent != null ? parent.GetComponentsInChildren<Block>() : new Block[0];
            var rb = parent != null ? parent.GetComponent<UnityEngine.Rigidbody>() : null;
            int unpowered = parent != null ? parent.GetComponentsInChildren<Power>().Count(p => !p.isWorking) : -1;
            int enemies = UnityEngine.Object.FindObjectsByType<ControlUnit>(UnityEngine.FindObjectsSortMode.None).Count(u=>u.faction==UnitFaction.Enemy && u.HasValidCockpit);
            System.IO.File.AppendAllText(report, names[index] + "\tflight\t" + elapsed.ToString("F2",System.Globalization.CultureInfo.InvariantCulture) + "\t" + blocks.Length + "\t" + (cockpit != null ? cockpit.GetComponent<Durability>().currentDurability : 0) + "\t" + unpowered + "\t" + (parent != null ? parent.position.ToString("F2") : "lost") + "\t" + (rb != null ? rb.linearVelocity.magnitude.ToString("F2") : "0") + "\t" + enemies + "\n");
        }
        if (elapsed >= 24 || cockpit == null || !PlayManager.instance.playMode)
        {
            release();
            System.IO.File.AppendAllText(report,names[index]+"\t"+(cockpit!=null && elapsed>=24?"completed":"lost")+"\t"+elapsed.ToString("F2")+"\n");
            index++;
            if (index >= names.Length) { finish(); return; }
            stage=0;
        }
    }
    catch (System.Exception ex)
    {
        System.IO.File.AppendAllText(report,"ERROR\t"+ex.Message+"\n");
        finish();
    }
};
UnityEditor.EditorApplication.update += tick;
return new { report, status="started", durationPerBlueprint=24, enemySpawnInterval=5 };
