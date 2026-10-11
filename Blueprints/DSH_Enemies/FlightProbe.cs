// 职责：在 Main Play Mode 逐艘测量悬停、追踪和冲量恢复，记录 Flight.csv。
// 入口：unity command eval_file --file Blueprints/DSH_Enemies/FlightProbe.cs --json；探针需先进入 Main Play Mode。
// DSH enemy flight probe.
//
//   unity command editor_play --project-path D:/git_projects/Unity/HY-Sandbox
//   unity command eval_file --file Blueprints/DSH_Enemies/FlightProbe.cs --timeout 60 --json
//
// The Editor window must stay focused for the whole run.  Unity sets runInBackground false
// during Play Mode when the window is unfocused and then throttles the player loop almost to a
// stop (measured: 2 frames in 145 s), so every sample becomes meaningless -- craft appear to
// fall at terminal velocity purely because no physics ticks are happening.
//
// Each blueprint is measured on its own, in this order per craft:
//   1. active and idle for 20 s   -> hover stability (no target, AI silently, no input)
//   2. active and chasing for 20 s -> mobility against a stationary player construct
//   3. shoved with an impulse, then 20 s to recover
// The other two craft are deactivated so they cannot interfere; doing this in one Play session
// keeps the comparison under identical conditions.  Where the AI *is* allowed to act, it drives
// the altitude setpoint exactly as in game, and stock mass, thrust, power and PID are untouched.
//
// The probe only touches runtime objects and ends by leaving Play Mode, so the scene is never
// saved.
if (!UnityEditor.EditorApplication.isPlaying || BuildManager.instance == null)
{
    throw new System.Exception("Enter Main Play Mode first");
}

bool oldBackground = UnityEngine.Application.runInBackground;
UnityEngine.Application.runInBackground = true;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
typeof(BuildManager).GetMethod("StopActiveBlockLoad", flags).Invoke(BuildManager.instance, null);
BuildManager.instance.enabled = false;
foreach (var u in UnityEngine.Object.FindObjectsByType<ControlUnit>(UnityEngine.FindObjectsSortMode.None))
{
    UnityEngine.Object.Destroy(u.gameObject);
}

var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
if (spawner == null)
{
    spawner = new UnityEngine.GameObject("DSH flight probe spawner").AddComponent<EnemySpawner>();
}

spawner.enabled = false;
var spawn = typeof(EnemySpawner).GetMethod("SpawnBlockData", flags);
string folder = System.IO.Path.GetFullPath("Blueprints/DSH_Enemies");
string csv = System.IO.Path.Combine(folder, "Flight.csv");
System.IO.File.WriteAllText(csv, "name,phase,seconds,heightError,tiltDegrees,verticalSpeed,horizontalSpeed,unpowered,blocks," + "playerDistance,setpoint,unpoweredAt\n");
System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "Flight.completed"), "running\n");
var files = System.IO.Directory.GetFiles(folder, "DSH_E*.json").OrderBy(p => p).ToArray();
var units = new System.Collections.Generic.List<ControlUnit>();
var players = new System.Collections.Generic.List<Transform>();
var checks = new System.Collections.Generic.List<object>();
var groupCount = new System.Collections.Generic.List<int>();
var peakSpeed = new System.Collections.Generic.List<float>();
var peakTilt = new System.Collections.Generic.List<float>();
var peakIdleError = new System.Collections.Generic.List<float>();
var peakChaseError = new System.Collections.Generic.List<float>();
var peakRecoverError = new System.Collections.Generic.List<float>();
var minUnpowered = new System.Collections.Generic.List<int>();
var turretCount = new System.Collections.Generic.List<int>();
var turretsEngaged = new System.Collections.Generic.List<int>();
var repairBays = new System.Collections.Generic.List<int>();
var repairAway = new System.Collections.Generic.List<int>();
var restRotations = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<int, UnityEngine.Quaternion>>();
var engaged = new System.Collections.Generic.List<System.Collections.Generic.HashSet<int>>();
for (int i = 0; i < files.Length; i++)
{
    var data = UnityEngine.JsonUtility.FromJson<BlockDataList>(System.IO.File.ReadAllText(files[i]));
    string name = System.IO.Path.GetFileNameWithoutExtension(files[i]);
    var origin = new UnityEngine.Vector3(0f, 800f, 0f);
    var unit = (ControlUnit)spawn.Invoke(spawner, new object[] { data, name, origin, UnityEngine.Quaternion.Euler(0f, i * 41f, 0f) });
    if (unit == null)
    {
        throw new System.Exception("Spawn failed for " + name);
    }

    // HoverFlightController defaults to targetHoverHeight 10, so a craft spawned at altitude
    // would open with a ~790 m error and free-fall while the thrust ramp catches up.  Aligning
    // the setpoint to the spawn altitude is what the AI does on its own first FixedUpdate.
    var hover = unit.hoverFlightController;
    if (hover != null)
    {
        hover.targetHoverHeight = origin.y;
        hover.setHeight = true;
    }

    // Guns stay off for every craft in this probe.  Firing changes the target's altitude (the
    // AI derives its setpoint from it) and adds beampoint/impact work, which would make the
    // three designs' hover and mobility numbers non-comparable.  Turret engagement is verified
    // separately in Armament.cs, where only that question is being asked.
    var guns = unit.GetComponentsInChildren<TurretWeapon>();
    foreach (var gun in guns)
    {
        gun.enabled = false;
    }

    // A stationary player construct near the lane; nothing chases it while the craft is idle.
    var player = UnityEngine.Object.Instantiate(UnityEngine.Resources.Load<UnityEngine.GameObject>("BlocksParent/BlocksParent"));
    player.name = "DSH probe player " + i;
    var cockpit = UnityEngine.Object.Instantiate(UnityEngine.Resources.Load<UnityEngine.GameObject>("Blocks/Cockpit"), player.transform);
    cockpit.transform.localPosition = UnityEngine.Vector3.zero;
    var playerUnit = player.GetComponent<ControlUnit>();
    playerUnit.RefreshChildren();
    playerUnit.enabled = false;
    PlayManager.instance.RegisterControlUnit(playerUnit);
    // Pin the player rigidbody completely.  Leaving useGravity on makes it sink at its terminal
    // velocity, and because the AI derives its altitude setpoint from the target's altitude the
    // craft then tracks a setpoint that is itself descending -- which shows up as a bogus
    // vertical-speed reading in the idle phase even though the craft is tracking correctly.
    var body = player.GetComponent<UnityEngine.Rigidbody>();
    body.isKinematic = true;
    body.useGravity = false;
    body.linearVelocity = UnityEngine.Vector3.zero;
    body.angularVelocity = UnityEngine.Vector3.zero;
    body.position = origin + new UnityEngine.Vector3(60f, -12f, 0f);
    UnityEngine.Physics.SyncTransforms();
    // Only one craft runs at a time.  Deactivating the GameObject stops its physics and its
    // FixedUpdate, which is the cleanest way to keep an idle craft from interacting.
    unit.gameObject.SetActive(false);
    units.Add(unit);
    players.Add(player.transform);
    groupCount.Add(-1);
    peakSpeed.Add(0f);
    peakTilt.Add(0f);
    peakIdleError.Add(0f);
    peakChaseError.Add(0f);
    peakRecoverError.Add(0f);
    minUnpowered.Add(int.MaxValue);
    turretCount.Add(guns.Length);
    turretsEngaged.Add(0);
    int bays = 0;
    foreach (var bay in unit.GetComponentsInChildren<RepairBot>())
    {
        bays++;
    }

    repairBays.Add(bays);
    repairAway.Add(0);
    var rest = new System.Collections.Generic.Dictionary<int, UnityEngine.Quaternion>();
    foreach (var gun in guns)
    {
        var axis = gun.horizontalAxis != null ? gun.horizontalAxis : gun.transform;
        rest[gun.GetInstanceID()] = axis.localRotation;
    }

    restRotations.Add(rest);
    engaged.Add(new System.Collections.Generic.HashSet<int>());
    checks.Add(new
    {
        name,
        blocks = data.blocks.Count,
        faction = unit.faction.ToString(),
        hoverThrusters = unit.hoverThrusters.Length,
        rigidbodyMass = unit.GetComponent<UnityEngine.Rigidbody>().mass,
        turrets = guns.Length,
        turretsLive = false,
        repairBays = bays,
    });
}

PlayManager.instance.playMode = true;
PlayManager.instance.groupCleanupDistance = 0f;
PlayManager.instance.blocksParent = players[0];
// Phase plan per craft, in seconds: idle hover, chase, impulse, recovery.  The idle window is
// the long one because the stock height integral (a fixed gain of 0.1, not scaled by mass) pulls
// the opening 9 m descent out slowly: E01/E03 are still creeping toward their final ~0.1 m offset
// at 20 s, so a shorter window measures the tail of that approach rather than steady hover.
const float IdleFor = 40f, ChaseFor = 20f, RecoverFor = 20f;
const float PerCraft = IdleFor + ChaseFor + RecoverFor;
// Double precision on purpose: the float Time.time loses resolution once the Editor has been
// running a while, which skewed the phase arithmetic in earlier versions of this probe.
double began = UnityEngine.Time.timeAsDouble;
int current = -1;
var shoved = new System.Collections.Generic.List<bool>();
for (int i = 0; i < units.Count; i++)
{
    shoved.Add(false);
}

int previous = -1;
var groupChecked = new System.Collections.Generic.List<bool>();
for (int i = 0; i < units.Count; i++)
{
    groupChecked.Add(false);
}

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
        int slot = (int)(elapsed / PerCraft);
        if (slot >= units.Count)
        {
            // Wrap up.
            for (int i = 0; i < units.Count; i++)
            {
                turretsEngaged[i] = engaged[i].Count;
            }

            var text = new System.Text.StringBuilder();
            text.Append("{\n  \"blueprints\": [\n");
            for (int i = 0; i < units.Count; i++)
            {
                text.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "    {{ \"name\": \"{0}\", \"peakHorizontalSpeed\": {1:F2}, " + "\"peakTiltDegrees\": {2:F2}, \"idleHoverError\": {3:F3}, " + "\"chaseHoverError\": {4:F3}, \"recoveryHoverError\": {5:F3}, " + "\"minWorkingPowerBlocks\": {6}, \"blocksAtEnd\": {7}, " + "\"connectedGroups\": {8}, \"turrets\": {9}, \"turretsThatEngaged\": {10}, " + "\"repairBays\": {11}, \"repairDronesAway\": {12} }}{13}\n", units[i].name, peakSpeed[i], peakTilt[i], peakIdleError[i], peakChaseError[i], peakRecoverError[i], minUnpowered[i], units[i].GetComponentsInChildren<Block>().Length, groupCount[i], turretCount[i], turretsEngaged[i], repairBays[i], repairAway[i], i + 1 < units.Count ? "," : ""));
            }

            text.Append("  ],\n  \"perCraftSeconds\": 80,\n  \"phases\": \"idle 40 s, chase 20 s, " + "impulse then recover 20 s\",\n" + "  \"note\": \"one craft active at a time, all weapons disabled so the " + "three designs' numbers are comparable; stock mass, thrust, power and PID " + "unchanged; the Editor window must stay focused or the player loop is " + "throttled and every reading is meaningless\"\n}\n");
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "Flight-summary.json"), text.ToString());
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "Flight.completed"), "80 s per craft: idle hover, chase, impulse recovery.\n");
            UnityEditor.EditorApplication.update -= tick;
            UnityEngine.Application.runInBackground = oldBackground;
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }

        // Hand over to the next craft.
        if (slot != current)
        {
            if (current >= 0)
            {
                units[current].gameObject.SetActive(false);
            }

            current = slot;
            units[current].gameObject.SetActive(true);
            UnityEngine.Physics.SyncTransforms();
        }

        var unit = units[current];
        var hover = unit.hoverFlightController;
        var rb = unit.GetComponent<UnityEngine.Rigidbody>();
        float inPhase = elapsed - slot * PerCraft;
        string phase = inPhase < IdleFor ? "idle" : inPhase < IdleFor + ChaseFor ? "chase" : "recover";
        // The EnemyController must keep running in every phase: it is what forces isHovered on
        // the hover thrusters (ConfigureHoverThrusters) and what feeds the controller its target
        // altitude, so muting it makes the craft fall rather than hover.  To measure the
        // controller's own hover error without the AI flying the craft somewhere, only the
        // movement intent is overridden, and only outside the chase window.
        if (phase != "chase")
        {
            unit.SetMovementInput(UnityEngine.Vector3.zero);
        }

        // Connectivity is checked once per craft, late in its own window, while it is the only
        // active unit: BlockGroupManager walks active blocks only, so deactivating the others is
        // what makes each craft's own graph measurable, and the physics scene needs a moment
        // after activation before its collider queries answer correctly.
        if (!groupChecked[current] && inPhase >= PerCraft - 2f)
        {
            groupChecked[current] = true;
            UnityEngine.Physics.SyncTransforms();
            groupCount[current] = BlockGroupManager.GroupBlocks(unit.GetComponentsInChildren<Block>().ToList()).Count;
        }

        if (phase == "recover" && !shoved[current])
        {
            shoved[current] = true;
            rb.AddForce(new UnityEngine.Vector3(1500f, 0f, 1200f), UnityEngine.ForceMode.Impulse);
        }

        if ((int)(elapsed * 5f) != previous)
        {
            previous = (int)(elapsed * 5f);
            if (unit == null || !unit.HasValidCockpit)
            {
                throw new System.Exception("Lost " + unit.name);
            }

            float horizontal = new UnityEngine.Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
            float tilt = UnityEngine.Vector3.Angle(unit.transform.up, UnityEngine.Vector3.up);
            float heightError = hover.CurrentHeight - hover.TargetHeight;
            int unpowered = 0;
            var unpoweredAt = new System.Text.StringBuilder();
            foreach (var p in unit.GetComponentsInChildren<Power>())
            {
                if (p.isWorking)
                {
                    continue;
                }

                unpowered++;
                if (unpoweredAt.Length < 300)
                {
                    var lp = p.transform.localPosition;
                    unpoweredAt.Append(lp.x.ToString("F1")).Append('/').Append(lp.y.ToString("F1")).Append('/').Append(lp.z.ToString("F1")).Append("=p").Append(p.currentPower.ToString("F0")).Append("/min").Append(p.minWorkingPower.ToString("F0")).Append('|');
                }
            }

            foreach (var gun in unit.GetComponentsInChildren<TurretWeapon>())
            {
                var axis = gun.horizontalAxis != null ? gun.horizontalAxis : gun.transform;
                if (UnityEngine.Quaternion.Angle(axis.localRotation, restRotations[current][gun.GetInstanceID()]) > 2f)
                {
                    engaged[current].Add(gun.GetInstanceID());
                }
            }

            int away = 0;
            foreach (var bay in unit.GetComponentsInChildren<RepairBot>())
            {
                if (bay.home != null && bay.transform.parent != bay.home)
                {
                    away++;
                }
            }

            repairAway[current] = UnityEngine.Mathf.Max(repairAway[current], away);
            // Skip the first 5 s of each phase: the thrust ramp alone takes 2 s at full rate.
            if (inPhase >= 5f && inPhase <= IdleFor + 0.01f)
            {
                peakIdleError[current] = UnityEngine.Mathf.Max(peakIdleError[current], UnityEngine.Mathf.Abs(heightError));
            }
            else if (phase == "chase" && inPhase >= IdleFor + 5f)
            {
                peakChaseError[current] = UnityEngine.Mathf.Max(peakChaseError[current], UnityEngine.Mathf.Abs(heightError));
            }
            else if (phase == "recover" && inPhase >= IdleFor + ChaseFor + 5f)
            {
                peakRecoverError[current] = UnityEngine.Mathf.Max(peakRecoverError[current], UnityEngine.Mathf.Abs(heightError));
            }

            peakSpeed[current] = UnityEngine.Mathf.Max(peakSpeed[current], horizontal);
            peakTilt[current] = UnityEngine.Mathf.Max(peakTilt[current], tilt);
            minUnpowered[current] = UnityEngine.Mathf.Min(minUnpowered[current], unpowered);
            System.IO.File.AppendAllText(csv, string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2:F3},{3:F4},{4:F4},{5:F4},{6:F4},{7},{8},{9:F4},{10:F1},{11}\n", unit.name, phase, elapsed, heightError, tilt, rb.linearVelocity.y, horizontal, unpowered, unit.GetComponentsInChildren<Block>().Length, unit.target != null ? UnityEngine.Vector3.Distance(unit.transform.position, unit.target.position) : -1f, hover.TargetHeight, unpoweredAt.ToString()));
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
return new
{
    checks,
    csv,
    perCraftSeconds = PerCraft,
    totalSeconds = PerCraft * units.Count
};
