// DSH enemy flight probe.
//
//   unity command editor_play --project-path D:/git_projects/Unity/HY-Sandbox
//   unity command eval_file --file Blueprints/DSH_Enemies/FlightProbe.cs --timeout 180000 --json
//
// Runs in Main Play Mode.  Uses the real EnemySpawner.SpawnBlockData and the real
// EnemyController, so the craft fly exactly as they will in game: hover is forced on by the
// AI, target height is driven by the AI, and thrust comes from the stock HoverFlightController.
// Turrets are disabled so only flight is measured.  Results go to Flight.csv and
// Flight-summary.json next to this script.
//
// The probe is disposable: it only touches runtime objects and ends by leaving Play Mode, so
// the scene is never saved.
if (!UnityEditor.EditorApplication.isPlaying || BuildManager.instance == null)
    throw new System.Exception("Enter Main Play Mode first");

bool oldBackground = UnityEngine.Application.runInBackground;
UnityEngine.Application.runInBackground = true;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
typeof(BuildManager).GetMethod("StopActiveBlockLoad", flags).Invoke(BuildManager.instance, null);
BuildManager.instance.enabled = false;
foreach (var u in UnityEngine.Object.FindObjectsByType<ControlUnit>(UnityEngine.FindObjectsSortMode.None))
    UnityEngine.Object.Destroy(u.gameObject);

var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
if (spawner == null) spawner = new UnityEngine.GameObject("DSH flight probe spawner").AddComponent<EnemySpawner>();
spawner.enabled = false;
var spawn = typeof(EnemySpawner).GetMethod("SpawnBlockData", flags);

string folder = System.IO.Path.GetFullPath("Blueprints/DSH_Enemies");
var files = System.IO.Directory.GetFiles(folder, "DSH_E*.json").OrderBy(p => p).ToArray();
var enemies = new System.Collections.Generic.List<ControlUnit>();
var targets = new System.Collections.Generic.List<UnityEngine.Transform>();
var starts = new System.Collections.Generic.List<UnityEngine.Vector3>();
var checks = new System.Collections.Generic.List<object>();

int index = 0;
foreach (var file in files)
{
    var data = UnityEngine.JsonUtility.FromJson<BlockDataList>(System.IO.File.ReadAllText(file));
    // Spawn well above the terrain: a HoverThrusterBig needs 2 s to reach full thrust, and at
    // v = g*t that is ~20 m of fall, so a low spawn would reach the ground before the height
    // PID has any authority and the resulting impact would dominate every later sample.
    var origin = new UnityEngine.Vector3(index * 300f, 600f, 0f);
    var name = System.IO.Path.GetFileNameWithoutExtension(file);
    var unit = (ControlUnit)spawn.Invoke(spawner, new object[]
    {
        data, name, origin, UnityEngine.Quaternion.Euler(0f, index * 41f, 0f)
    });
    if (unit == null) throw new System.Exception("Spawn failed for " + name);
    // Block.CollectNeighbors probes colliders, so the physics scene has to see the freshly
    // instantiated transforms before the graph is walked -- otherwise every block looks
    // isolated purely because the queries ran against a stale broadphase.
    UnityEngine.Physics.SyncTransforms();
    var groups = BlockGroupManager.GroupBlocks(unit.GetComponentsInChildren<Block>().ToList());
    if (groups.Count != 1)
        throw new System.Exception(name + " spawned as " + groups.Count + " groups");

    // Isolate flight from battle damage; mass, thrust, power and PID stay untouched.
    foreach (var gun in unit.GetComponentsInChildren<TurretWeapon>()) gun.enabled = false;

    var target = UnityEngine.Object.Instantiate(
        UnityEngine.Resources.Load<UnityEngine.GameObject>("BlocksParent/BlocksParent"));
    target.name = "DSH flight probe target " + index;
    var cockpit = UnityEngine.Object.Instantiate(
        UnityEngine.Resources.Load<UnityEngine.GameObject>("Blocks/Cockpit"), target.transform);
    cockpit.transform.localPosition = UnityEngine.Vector3.zero;
    var player = target.GetComponent<ControlUnit>();
    player.RefreshChildren();
    player.enabled = false;
    PlayManager.instance.RegisterControlUnit(player);
    var body = target.GetComponent<UnityEngine.Rigidbody>();
    body.isKinematic = true;
    body.position = origin + new UnityEngine.Vector3(120f, -8f, 0f);
    UnityEngine.Physics.SyncTransforms();

    enemies.Add(unit);
    targets.Add(target.transform);
    starts.Add(origin);
    checks.Add(new
    {
        name,
        blocks = data.blocks.Count,
        groups = groups.Count,
        faction = unit.faction.ToString(),
        hoverThrusters = unit.hoverThrusters.Length,
        rigidbodyMass = unit.GetComponent<UnityEngine.Rigidbody>().mass,
    });
    index++;
}

PlayManager.instance.playMode = true;
PlayManager.instance.groupCleanupDistance = 0f;
PlayManager.instance.blocksParent = targets[0];

string csv = System.IO.Path.Combine(folder, "Flight.csv");
System.IO.File.WriteAllText(csv,
    "name,seconds,heightError,tiltDegrees,verticalSpeed,horizontalSpeed,unpowered,blocks,"
    + "targetDistance,unpoweredDetail\n");
System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "Flight.completed"), "running\n");

double began = UnityEngine.Time.timeAsDouble;
int previous = -1;
bool acquired = false, moved = false, lost = false;
var peakSpeed = new float[enemies.Count];
var peakTilt = new float[enemies.Count];
var peakHeightError = new float[enemies.Count];
var minUnpowered = new int[enemies.Count];
for (int i = 0; i < minUnpowered.Length; i++) minUnpowered[i] = int.MaxValue;

UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            UnityEngine.Application.runInBackground = oldBackground;
            UnityEditor.EditorApplication.update -= tick;
            return;
        }
        float elapsed = (float)(UnityEngine.Time.timeAsDouble - began);
        if (elapsed >= 15f && !acquired)
        {
            for (int i = 0; i < targets.Count; i++)
                targets[i].GetComponent<UnityEngine.Rigidbody>().position =
                    starts[i] + new UnityEngine.Vector3(30f, -8f, 0f);
            UnityEngine.Physics.SyncTransforms();
            acquired = true;
        }
        if (elapsed >= 45f && !moved)
        {
            for (int i = 0; i < targets.Count; i++)
                targets[i].GetComponent<UnityEngine.Rigidbody>().position =
                    starts[i] + new UnityEngine.Vector3(-25f, -8f, 30f);
            UnityEngine.Physics.SyncTransforms();
            moved = true;
        }
        if (elapsed >= 75f && !lost)
        {
            for (int i = 0; i < targets.Count; i++)
                targets[i].GetComponent<UnityEngine.Rigidbody>().position =
                    enemies[i].transform.position + new UnityEngine.Vector3(0f, 0f, 170f);
            UnityEngine.Physics.SyncTransforms();
            lost = true;
        }
        if ((int)(elapsed * 5f) != previous)
        {
            previous = (int)(elapsed * 5f);
            for (int i = 0; i < enemies.Count; i++)
            {
                var unit = enemies[i];
                if (unit == null || !unit.HasValidCockpit) throw new System.Exception("Lost " + unit);
                var hover = unit.hoverFlightController;
                var rb = unit.GetComponent<UnityEngine.Rigidbody>();
                float horizontal = new UnityEngine.Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
                float tilt = UnityEngine.Vector3.Angle(unit.transform.up, UnityEngine.Vector3.up);
                float heightError = hover.CurrentHeight - hover.TargetHeight;
                int unpowered = 0;
                var detail = new System.Text.StringBuilder();
                foreach (var p in unit.GetComponentsInChildren<Power>())
                {
                    if (p.isWorking) continue;
                    unpowered++;
                    if (detail.Length < 400)
                    {
                        var lp = p.transform.localPosition;
                        detail.Append(lp.x.ToString("F1")).Append('/')
                              .Append(lp.y.ToString("F1")).Append('/')
                              .Append(lp.z.ToString("F1")).Append('|');
                    }
                }
                if (elapsed >= 20f)
                {
                    peakSpeed[i] = UnityEngine.Mathf.Max(peakSpeed[i], horizontal);
                    peakTilt[i] = UnityEngine.Mathf.Max(peakTilt[i], tilt);
                    peakHeightError[i] = UnityEngine.Mathf.Max(peakHeightError[i],
                        UnityEngine.Mathf.Abs(heightError));
                    minUnpowered[i] = UnityEngine.Mathf.Min(minUnpowered[i], unpowered);
                }
                System.IO.File.AppendAllText(csv, string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:F3},{2:F4},{3:F4},{4:F4},{5:F4},{6},{7},{8:F4},{9}\n",
                    unit.name, elapsed, heightError, tilt, rb.linearVelocity.y, horizontal, unpowered,
                    unit.GetComponentsInChildren<Block>().Length,
                    unit.target != null
                        ? UnityEngine.Vector3.Distance(unit.transform.position, unit.target.position)
                        : -1f,
                    detail.ToString()));
            }
        }
        if (elapsed >= 90f)
        {
            var text = new System.Text.StringBuilder();
            text.Append("{\n  \"blueprints\": [\n");
            for (int i = 0; i < enemies.Count; i++)
            {
                text.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "    {{ \"name\": \"{0}\", \"peakHorizontalSpeed\": {1:F2}, "
                    + "\"peakTiltDegrees\": {2:F2}, \"peakHeightError\": {3:F3}, "
                    + "\"minWorkingPowerBlocks\": {4}, \"blocksAtEnd\": {5} }}{6}\n",
                    enemies[i].name, peakSpeed[i], peakTilt[i], peakHeightError[i],
                    minUnpowered[i], enemies[i].GetComponentsInChildren<Block>().Length,
                    i + 1 < enemies.Count ? "," : ""));
            }
            text.Append("  ],\n  \"durationSeconds\": 90,\n  \"targetAcquiredAt\": 15,\n"
                        + "  \"targetRelocatedAt\": 45,\n  \"targetLostAt\": 75,\n"
                        + "  \"note\": \"peak values are taken from t=20s onward; turrets disabled to "
                        + "isolate flight; stock mass, thrust, power and PID unchanged\"\n}\n");
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "Flight-summary.json"),
                text.ToString());
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "Flight.completed"),
                "90 s: idle hover, target acquisition at 15 s, relocation at 45 s, target loss at 75 s.\n");
            UnityEditor.EditorApplication.update -= tick;
            UnityEngine.Application.runInBackground = oldBackground;
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
    catch (System.Exception ex)
    {
        System.IO.File.AppendAllText(csv, "ERROR: " + ex + "\n");
        UnityEditor.EditorApplication.update -= tick;
        UnityEngine.Application.runInBackground = oldBackground;
        UnityEditor.EditorApplication.isPlaying = false;
    }
};
UnityEditor.EditorApplication.update += tick;
return new { checks, csv, duration = 90 };
