// 职责：组织运行单元的方块、刚体、输入、总耐久与资源归属。
// 入口：ProtectExplosionContacts() / TryGetTotalDurability() / RefreshChildren()。
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ControlUnit : MonoBehaviour
{
    internal BlockConnectionGraph Connections { get; } = new BlockConnectionGraph();

    private bool aiEligibilityInitialized;
    private bool wasAIEligible;
    private static readonly List<ControlUnit> ActiveUnits = new List<ControlUnit>();
    public static IReadOnlyList<ControlUnit> RuntimeUnits => ActiveUnits;

    private static readonly List<ControlUnit> combatUnits = new List<ControlUnit>();
    public static IReadOnlyList<ControlUnit> CombatUnits => combatUnits;

    internal const int DebrisLayer = 9;
    internal const int DebrisMask = 1 << DebrisLayer;
    private int originalRootLayer = -1;
    internal bool IsDebris => originalRootLayer >= 0;

    internal void SetDebris(bool debris)
    {
        if (debris && !IsDebris)
        {
            originalRootLayer = gameObject.layer;
            gameObject.layer = DebrisLayer;
        }
        else if (!debris && IsDebris)
        {
            gameObject.layer = originalRootLayer;
            originalRootLayer = -1;
        }
    }

    private void RefreshCombatMembership()
    {
        if (isActiveAndEnabled && (HasValidCockpit || Building != null))
        {
            if (!combatUnits.Contains(this))
            {
                combatUnits.Add(this);
            }
        }
        else
        {
            combatUnits.Remove(this);
        }
    }

    private readonly List<Durability> damageTargets = new List<Durability>();
    private int damageTargetsFrame = -1;
    private double damageTargetsStep = -1;
    private uint damageTargetsVersion;
    private bool targetBoundsReady;
    private Bounds targetBounds;
    private static readonly Unity.Profiling.ProfilerMarker BoundsMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.SharedBounds");
    // 多座炮塔共享当前物理步的层级读取；位置、阵营、伤势和真实归属仍在每次选择时验证。
    internal IReadOnlyList<Durability> GetDamageTargets()
    {
        if (damageTargetsFrame != Time.frameCount || damageTargetsStep != Time.fixedTimeAsDouble || damageTargetsVersion != Durability.HierarchyVersion)
        {
            GetComponentsInChildren(true, damageTargets);
            damageTargetsFrame = Time.frameCount;
            damageTargetsStep = Time.fixedTimeAsDouble;
            damageTargetsVersion = Durability.HierarchyVersion;
        }

        return damageTargets;
    }

    private readonly Vector3[] hullSamples = new Vector3[6];
    internal Vector3 HullSample(int side) => transform.TransformPoint(hullSamples[side]);
    private uint hullVersion;
    private readonly List<Collider> hullShapes = new List<Collider>();
    internal bool TryGetTargetBounds(out Bounds bounds)
    {
        if (!targetBoundsReady || hullVersion != Durability.HierarchyVersion)
        {
            using var sample = BoundsMarker.Auto();
            hullVersion = Durability.HierarchyVersion;
            GetComponentsInChildren(false, hullShapes);
            targetBoundsReady = false;
            foreach (Collider shape in hullShapes)
            {
                if (!shape.enabled || shape.isTrigger || shape.GetComponentInParent<ControlUnit>() != this)
                {
                    continue;
                }

                Bounds box = shape.bounds;
                Vector3 center = transform.InverseTransformPoint(box.center);
                for (int side = 0; side < 6; side++)
                {
                    int axis = side / 2;
                    bool farther = (side & 1) == 0 ? center[axis] > hullSamples[side][axis] : center[axis] < hullSamples[side][axis];
                    if (!targetBoundsReady || farther)
                    {
                        hullSamples[side] = center;
                    }
                }

                for (int i = 0; i < 8; i++)
                {
                    Vector3 sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                    Vector3 local = transform.InverseTransformPoint(box.center + Vector3.Scale(sign, box.extents));
                    if (!targetBoundsReady)
                    {
                        targetBounds = new Bounds(local, Vector3.zero);
                        targetBoundsReady = true;
                    }
                    else
                    {
                        targetBounds.Encapsulate(local);
                    }
                }
            }

            hullShapes.Clear();
        }

        bounds = targetBounds;
        return targetBoundsReady;
    }

    internal void ApplySurfaceDamage(Collider hit, ControlUnit attacker, UnitFaction targetFaction, float damage)
    {
        if (faction != targetFaction || (!IsOperational && !IsDebris))
        {
            return;
        }

        Durability health = hit.GetComponentInParent<Durability>();
        if (health != null && health.isActiveAndEnabled && health.GetComponentInParent<ControlUnit>() == this)
        {
            health.ApplyDamage(damage, attacker);
        }
    }

    private void OnDrawGizmos()
    {
        DebugManager debug = DebugManager.instance;
        if (debug != null && debug.ShowTurretTargeting && targetBoundsReady)
        {
            Gizmos.color = Color.cyan;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(targetBounds.center, targetBounds.size);
            Gizmos.matrix = previous;
        }
    }

    private void OnTransformChildrenChanged()
    {
        targetBoundsReady = false;
        damageTargetsFrame = -1;
    }

    public static bool IsSceneSimulationActive(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (ControlUnit unit in ActiveUnits)
        {
            if (unit != null && unit.gameObject.scene == scene && unit.IsSimulationActive)
            {
                return true;
            }
        }

        return false;
    }

    internal ExplosionDamageSession DamageSession
    {
        get; set;
    }

    internal event System.Action<ControlUnit, int> FragmentCreated;
    internal void NotifyFragmentCreated(ControlUnit fragment, int blockCount)
    {
        fragment.DamageSession = DamageSession;
        FragmentCreated?.Invoke(fragment, blockCount);
    }

    internal event System.Action<Block> BlockDestroyed;
    internal void NotifyBlockDestroyed(Block block)
    {
        BlockDestroyed?.Invoke(block);
    }

    public BuildingRuntime Building
    {
        get; private set;
    }

    private readonly List<Turret> capabilityWeapons = new List<Turret>();
    /// <summary>仍有完好且电网可供电的武器；不以索敌、瞄准、冷却或瞬时功率分配判断失能。</summary>
    public bool HasCombatCapability
    {
        get
        {
            if (!isActiveAndEnabled || !IsOperational || !IsSimulationActive)
            {
                return false;
            }

            GetComponentsInChildren(false, capabilityWeapons);
            foreach (Turret weapon in capabilityWeapons)
            {
                Durability health = weapon.GetComponent<Durability>();
                Power supply = weapon.GetComponent<Power>();
                if (weapon.isActiveAndEnabled && weapon.IsStarted && weapon.damage > 0f && weapon.range > 0f && weapon.GetComponentInParent<ControlUnit>() == this && health != null && health.isActiveAndEnabled && health.currentDurability > 0f && supply != null && supply.isActiveAndEnabled && supply.PotentialPower > 0f && supply.PotentialPower >= Mathf.Max(0f, supply.minWorkingPower))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsOperational => HasValidCockpit || (Building != null && Building.IsOperational);
    public bool IsSimulationActive => Building != null ? Building.IsSimulationActive : PlayManager.instance != null && PlayManager.instance.playMode;

    public void BindBuilding(BuildingRuntime building)
    {
        Building = building;
        RefreshCombatMembership();
    }

    private void OnEnable()
    {
        if (!ActiveUnits.Contains(this))
        {
            ActiveUnits.Add(this);
        }

        RefreshCombatMembership();
    }

    private void OnDisable()
    {
        ActiveUnits.Remove(this);
        combatUnits.Remove(this);
        SetMovementInput(Vector3.zero);
    }

    // Runtime aggregate for one construct: owns cockpit validity, movement input, and child component references.
    public string runtimeUnitId;
    public HoverFlightController hoverFlightController;
    public Cockpit cockpit;
    public Cockpit[] cockpits;
    public MainThruster[] mainThrusters;
    public HoverThruster[] hoverThrusters;
    public UnitFaction faction = UnitFaction.Player;
    public bool hasValidCockpit;
    public Vector3 movementInput;
    private Vector3 coastingDirection;
    private Thruster[] cachedThrusters = System.Array.Empty<Thruster>();
    public IReadOnlyList<Thruster> Thrusters => cachedThrusters;
    public Vector3 PropulsionDirection => movementInput.sqrMagnitude > 1e-6f ? movementInput.normalized : coastingDirection;
    public float PropulsionThrottle => Mathf.Clamp01(movementInput.magnitude);

    public Transform target;
    public float ExplosionCollisionProtectedUntil
    {
        get; private set;
    }

    public void ProtectExplosionContacts(float duration)
    {
        ExplosionCollisionProtectedUntil = Mathf.Max(ExplosionCollisionProtectedUntil, Time.time + Mathf.Max(0f, duration));
    }

    private readonly CollisionDamageResolver _collisionDamage = new CollisionDamageResolver();
    public bool HasValidCockpit => hasValidCockpit && cockpit != null;
    public bool HasAnyCockpit => cockpits != null && cockpits.Length > 0 ? System.Array.Exists(cockpits, candidate => candidate != null) : GetComponentInChildren<Cockpit>(true) != null;
    public bool IsPlayer => faction == UnitFaction.Player;
    public bool AcceptsPlayerInput => IsPlayer && HasValidCockpit && cockpit.ControlMode == UnitControlMode.Player;
    public Vector3 MovementInput => movementInput;

    public bool TryGetTotalDurability(out float current, out float maximum)
    {
        current = 0f;
        maximum = 0f;
        if (!IsOperational)
        {
            return false;
        }

        IReadOnlyList<Durability> targets = GetDamageTargets();
        for (int i = 0; i < targets.Count; i++)
        {
            Durability durability = targets[i];
            if (durability == null || !durability.isActiveAndEnabled || durability.GetComponentInParent<ControlUnit>() != this)
            {
                continue;
            }

            current += Mathf.Max(0f, durability.currentDurability);
            maximum += Mathf.Max(0f, durability.maxDurability);
        }

        return maximum > 0f;
    }

    private void Start()
    {
        EnsureRuntimeUnitId();
        AssignRuntimeOwnershipToBlocks(false);
        if (PlayManager.instance != null && Building == null)
        {
            PlayManager.instance.RegisterControlUnit(this);
        }

        Invoke(nameof(RefreshChildren), 2f);
    }

    private void Update()
    {
        if (!IsSimulationActive)
        {
            return;
        }

        if (transform.childCount == 0)
        {
            Destroy(gameObject);
            return;
        }

        if (!HasValidCockpit)
        {
            SetMovementInput(Vector3.zero);
            return;
        }

        if (AcceptsPlayerInput)
        {
            SetMovementInput(GetPlayerMovementInput());
        }

        if (AcceptsPlayerInput && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach (HoverThruster thruster in hoverThrusters)
            {
                thruster.isHovered = !thruster.isHovered;
            }

            if (hoverFlightController != null)
            {
                if (!hoverFlightController.setHeight)
                {
                    hoverFlightController.targetHoverHeight = (int)transform.position.y + 10;
                    hoverFlightController.setHeight = true;
                }
                else
                {
                    hoverFlightController.targetHoverHeight = 0;
                    hoverFlightController.setHeight = false;
                }
            }
        }
    }

    public void RefreshChildren()
    {
        targetBoundsReady = false;
        damageTargetsFrame = -1;
        // Refresh after loading or regrouping because child components and Rigidbody ownership may have changed.
        EnsureRuntimeUnitId();
        hoverFlightController = GetComponentInChildren<HoverFlightController>();
        cockpits = GetComponentsInChildren<Cockpit>(true);
        mainThrusters = GetComponentsInChildren<MainThruster>();
        hoverThrusters = GetComponentsInChildren<HoverThruster>();
        Rigidbody unitRigidbody = GetComponent<Rigidbody>();
        Thruster[] allThrusters = GetComponentsInChildren<Thruster>();
        cachedThrusters = allThrusters;
        foreach (Thruster thruster in allThrusters)
        {
            if (thruster == null)
            {
                continue;
            }

            thruster.SetRuntimeReferences(this, unitRigidbody);
        }

        hasValidCockpit = cockpits.Length == 1;
        cockpit = hasValidCockpit ? cockpits[0] : null;
        if (IsDebris && (HasValidCockpit || Building != null))
        {
            SetDebris(false);
            foreach (Block block in GetComponentsInChildren<Block>(true))
            {
                block.SetDebris(false);
            }
        }

        RefreshCombatMembership();
        if (hasValidCockpit)
        {
            faction = cockpit.faction;
        }
        else
        {
            SetMovementInput(Vector3.zero);
            //Debug.LogWarning($"{name} requires exactly one Cockpit, found {cockpits.Length}.");
        }

        if (hoverFlightController == null)
        {
            //Debug.LogWarning($"Cannot find HoverFlightController in {gameObject}");
        }

        if (hoverFlightController != null && hoverThrusters.Length > 0 && hasValidCockpit)
        {
            hoverFlightController.thrusters = hoverThrusters;
            hoverFlightController.enabled = true;
            hoverFlightController.Init();
        }

        // AI 参数由载具根 Prefab 持有；组装后只启停既有组件。
        EnemyController ai = GetComponent<EnemyController>();
        if (ai != null)
        {
            bool eligible = hasValidCockpit && (faction == UnitFaction.Enemy || cockpit.ControlMode == UnitControlMode.Autonomous);
            if (!aiEligibilityInitialized || eligible != wasAIEligible)
            {
                ai.enabled = eligible;
                wasAIEligible = eligible;
                aiEligibilityInitialized = true;
            }
        }
    }

    public void AssignRuntimeOwnershipToBlocks(bool overwriteExisting)
    {
        EnsureRuntimeUnitId();
        Block[] blocks = GetComponentsInChildren<Block>();
        foreach (Block block in blocks)
        {
            if (block == null)
            {
                continue;
            }

            RuntimeUnitMember member = block.GetComponent<RuntimeUnitMember>();
            if (!overwriteExisting && member != null && !string.IsNullOrEmpty(member.ownerUnitId))
            {
                continue;
            }

            RuntimeUnitMember.Ensure(block.gameObject, runtimeUnitId, faction);
        }
    }

    public void EnsureRuntimeUnitId()
    {
        if (string.IsNullOrEmpty(runtimeUnitId))
        {
            runtimeUnitId = System.Guid.NewGuid().ToString();
        }
    }

    public void PlayEnd()
    {
        if (hoverFlightController != null)
        {
            hoverFlightController.enabled = false;
        }

        SetMovementInput(Vector3.zero);
    }

    public void SetMovementInput(Vector3 worldDirection)
    {
        SetPropulsionInput(worldDirection, worldDirection.magnitude);
    }

    public void SetPropulsionInput(Vector3 worldDirection, float throttle)
    {
        // 朝向在滑行时仍有效；零油门不能用瞬时速度误差重新决定喷口朝向。
        coastingDirection = worldDirection.sqrMagnitude > 1e-6f ? worldDirection.normalized : Vector3.zero;
        movementInput = coastingDirection * Mathf.Clamp01(throttle);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private Vector3 GetPlayerMovementInput()
    {
        Vector3 dir = Vector3.zero;
        if (Keyboard.current == null)
        {
            return dir;
        }

        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform == null)
        {
            return dir;
        }

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();
        if (Keyboard.current.wKey.isPressed)
        {
            dir += camForward;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            dir -= camForward;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            dir -= camRight;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            dir += camRight;
        }

        return Vector3.ClampMagnitude(dir, 1f);
    }

    public event System.Action<Collision> CollisionEntered;
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsSimulationActive || collision.contactCount == 0)
        {
            return;
        }

        CollisionEntered?.Invoke(collision);
        _collisionDamage.ApplyContactDamage(this, collision);
    }

    private void OnDestroy()
    {
        if (PlayManager.instance != null)
        {
            PlayManager.instance.UnregisterControlUnit(this);
        }
    }
}
