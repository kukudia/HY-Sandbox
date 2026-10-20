// 职责：在 Main Play Mode 逐艘验证 DSH 炮塔供电、转向锁定与弹道出膛，写出 Armament-summary.json。
// 入口：进入 Main Play Mode 后 eval_file；探针结束后自动退出 Play Mode。
// DSH armament probe.
//
//   unity command editor_play --project-path D:/git_projects/Unity/HY-Sandbox
//   unity command eval_file --project-path D:/git_projects/Unity/HY-Sandbox --json -- --file "D:/git_projects/Unity/HY-Sandbox/Blueprints/DSH_Enemies/ArmamentProbe.cs" --timeout 60000
//
// Each hull is spawned alone 30 m from a pinned player cockpit.  Its EnemyController and thrusters
// are held still, but every turret stays live: the probe counts how many turrets rotate away from
// their rest pose (engaged), how many have power, and whether any projectile left a muzzle.  The
// target cockpit is given a large durability so twelve guns cannot end the test early.
if (!UnityEditor.EditorApplication.isPlaying || BuildManager.instance == null)
{
    throw new System.Exception("Enter Main Play Mode first");
}

bool oldBackground = UnityEngine.Application.runInBackground;
UnityEngine.Application.runInBackground = true;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
typeof(BuildManager).GetMethod("StopActiveBlockLoad", flags).Invoke(BuildManager.instance, null);
BuildManager.instance.enabled = false;
foreach (var existing in UnityEngine.Object.FindObjectsByType<ControlUnit>(UnityEngine.FindObjectsSortMode.None))
{
    UnityEngine.Object.Destroy(existing.gameObject);
}

var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
if (spawner == null)
{
    spawner = new UnityEngine.GameObject("DSH armament probe spawner").AddComponent<EnemySpawner>();
}

spawner.enabled = false;
var spawn = typeof(EnemySpawner).GetMethod("SpawnBlockData", flags);
string folder = System.IO.Path.GetFullPath("Blueprints/DSH_Enemies");
var files = System.IO.Directory.GetFiles(folder, "DSH_E*.json").OrderBy(p => p).ToArray();
var recordLines = new System.Collections.Generic.List<string>();
// Pinned target: a single cockpit with a huge durability, no thrusters, no input.
var player = UnityEngine.Object.Instantiate(UnityEngine.Resources.Load<UnityEngine.GameObject>("BlocksParent/BlocksParent"));
player.name = "DSH armament target";
var playerCockpit = UnityEngine.Object.Instantiate(UnityEngine.Resources.Load<UnityEngine.GameObject>("Blocks/Cockpit"), player.transform);
playerCockpit.transform.localPosition = UnityEngine.Vector3.zero;
var playerUnit = player.GetComponent<ControlUnit>();
playerUnit.RefreshChildren();
playerUnit.enabled = true;
playerUnit.cockpit.faction = UnitFaction.Player;
var targetDurability = playerCockpit.GetComponent<Durability>();
targetDurability.maxDurability = 100000f;
targetDurability.currentDurability = 100000f;
var playerBody = player.GetComponent<UnityEngine.Rigidbody>();
playerBody.isKinematic = true;
playerBody.useGravity = false;
playerBody.position = new UnityEngine.Vector3(0f, 800f, 30f);
UnityEngine.Physics.SyncTransforms();
PlayManager.instance.RegisterControlUnit(playerUnit);
PlayManager.instance.playMode = true;
ControlUnit unit = null;
System.Collections.Generic.Dictionary<int, UnityEngine.Quaternion> restRotations = null;
System.Collections.Generic.List<Turret> guns = null;
int index = -1;
double phaseStart = 0d;
int maxEngaged = 0, maxPowered = 0, maxRoofEngaged = 0, maxRoofPowered = 0, maxProjectiles = 0;
int roofTotal = 0;
float damageBefore = 0f;
const double PerCraft = 12d;
System.Action spawnNext = () =>
{
    if (unit != null)
    {
        UnityEngine.Object.Destroy(unit.gameObject);
    }

    index++;
    if (index >= files.Length)
    {
        return;
    }

    var data = UnityEngine.JsonUtility.FromJson<BlockDataList>(System.IO.File.ReadAllText(files[index]));
    string name = System.IO.Path.GetFileNameWithoutExtension(files[index]);
    unit = (ControlUnit)spawn.Invoke(spawner, new object[] { data, name, new UnityEngine.Vector3(0f, 800f, 0f), UnityEngine.Quaternion.identity });
    if (unit == null)
    {
        throw new System.Exception("Spawn failed for " + name);
    }

    var enemyController = unit.GetComponent<EnemyController>();
    if (enemyController != null)
    {
        enemyController.enabled = false;
    }

    foreach (var hover in unit.hoverThrusters)
    {
        hover.isHovered = true;
    }

    if (unit.hoverFlightController != null)
    {
        unit.hoverFlightController.setHeight = true;
        unit.hoverFlightController.targetHoverHeight = 800f;
    }

    guns = unit.GetComponentsInChildren<Turret>().ToList();
    restRotations = new System.Collections.Generic.Dictionary<int, UnityEngine.Quaternion>();
    roofTotal = 0;
    foreach (var gun in guns)
    {
        var axis = gun.horizontalAxis != null ? gun.horizontalAxis : gun.transform;
        restRotations[gun.GetInstanceID()] = axis.localRotation;
        if (gun.transform.localPosition.y > 4.5f)
        {
            roofTotal++;
        }
    }

    maxEngaged = maxPowered = maxRoofEngaged = maxRoofPowered = maxProjectiles = 0;
    damageBefore = targetDurability.currentDurability;
    phaseStart = UnityEngine.Time.timeAsDouble;
    UnityEngine.Physics.SyncTransforms();
};
spawnNext();
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!UnityEditor.EditorApplication.isPlaying)
    {
        UnityEngine.Application.runInBackground = oldBackground;
        UnityEditor.EditorApplication.update -= tick;
        return;
    }

    if (unit == null)
    {
        return;
    }

    int engaged = 0, powered = 0, roofEngaged = 0, roofPowered = 0;
    foreach (var gun in guns)
    {
        var axis = gun.horizontalAxis != null ? gun.horizontalAxis : gun.transform;
        bool aimed = UnityEngine.Quaternion.Angle(axis.localRotation, restRotations[gun.GetInstanceID()]) > 2f;
        var power = gun.GetComponent<Power>();
        bool supplied = power != null && power.isWorking && power.currentPower > 0f;
        if (aimed)
        {
            engaged++;
        }

        if (supplied)
        {
            powered++;
        }

        if (gun.transform.localPosition.y > 4.5f)
        {
            if (aimed)
            {
                roofEngaged++;
            }

            if (supplied)
            {
                roofPowered++;
            }
        }
    }

    maxEngaged = UnityEngine.Mathf.Max(maxEngaged, engaged);
    maxPowered = UnityEngine.Mathf.Max(maxPowered, powered);
    maxRoofEngaged = UnityEngine.Mathf.Max(maxRoofEngaged, roofEngaged);
    maxRoofPowered = UnityEngine.Mathf.Max(maxRoofPowered, roofPowered);
    maxProjectiles = UnityEngine.Mathf.Max(maxProjectiles, UnityEngine.Object.FindObjectsByType<TurretProjectile>(UnityEngine.FindObjectsSortMode.None).Length);
    if (UnityEngine.Time.timeAsDouble - phaseStart < PerCraft)
    {
        return;
    }

    recordLines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{{ \"name\": \"{0}\", \"turrets\": {1}, \"turretsEngaged\": {2}, \"turretsPowered\": {3}, \"roofTurrets\": {4}, \"roofTurretsEngaged\": {5}, \"roofTurretsPowered\": {6}, \"projectilesInFlight\": {7}, \"targetDamage\": {8:F1} }}", unit.name, guns.Count, maxEngaged, maxPowered, roofTotal, maxRoofEngaged, maxRoofPowered, maxProjectiles, damageBefore - targetDurability.currentDurability));
    spawnNext();
    if (index >= files.Length)
    {
        var text = new System.Text.StringBuilder();
        text.Append("{\n  \"blueprints\": [\n");
        for (int i = 0; i < recordLines.Count; i++)
        {
            text.Append("    " + recordLines[i] + (i + 1 < recordLines.Count ? ",\n" : "\n"));
        }

        text.Append("  ],\n  \"perCraftSeconds\": " + PerCraft + ",\n  \"note\": \"stationary hull, pinned player cockpit 30 m ahead, EnemyController and thrusters held still; engaged means the turret yaw axis moved more than 2 degrees from its rest pose; projectilesInFlight is the peak count of active projectile instances\"\n}\n");
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "Armament-summary.json"), text.ToString());
        UnityEngine.Object.Destroy(player);
        UnityEngine.Application.runInBackground = oldBackground;
        UnityEditor.EditorApplication.update -= tick;
        UnityEditor.EditorApplication.isPlaying = false;
    }
};
UnityEditor.EditorApplication.update += tick;
return new
{
    folder,
    blueprints = files.Length,
    perCraftSeconds = PerCraft
};
