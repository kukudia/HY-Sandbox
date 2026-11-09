// 职责：共享机库起飞/返航导引，以有电推进能力限定刚体运动，逐舰入库或报废。
// 入口：BeginLaunch() / BeginReturn()；宿主在 FixedUpdate 调用 Tick()，退出调用 Stop()。
// 约束：自动导引暂时接管姿态与推力；不传送舰艇、不在报废前发放返航奖励。
using System.Collections.Generic;
using UnityEngine;

public sealed class SiegeReturnFlight
{
    private sealed class Flight
    {
        public int slot;
        public SiegeFleetEntry entry;
        public HangarModule hangar;
        public bool requiresHangar;
        public ControlUnit unit;
        public Rigidbody body;
        public Thruster[] engines;
        public bool[] hoverStates;
        public HoverFlightController hoverController;
        public bool hoverControllerEnabled;
        public Quaternion dockRotation;
        public Vector3[] points;
        public int point;
        public float elapsed;
        public float disabledFor;
        public bool finished;
    }

    private readonly SiegeFleet fleet;
    private readonly SiegeFlightSettings settings;
    private readonly Object powerRequester;
    private readonly List<Flight> flights = new List<Flight>();
    public bool IsLaunching
    {
        get; private set;
    }
    public bool IsReturning
    {
        get; private set;
    }
    public int CompletedCount
    {
        get; private set;
    }
    public int FailedCount
    {
        get; private set;
    }
    public bool IsComplete => CompletedCount + FailedCount >= flights.Count;
    public int TotalCount => flights.Count;
    public string LastFailureReason
    {
        get; private set;
    }

    public SiegeReturnFlight(SiegeFleet fleet, SiegeFlightSettings settings, Object powerRequester)
    {
        this.fleet = fleet;
        this.settings = settings ?? new SiegeFlightSettings();
        this.powerRequester = powerRequester;
    }

    public bool IsGuiding(SiegeFleetEntry entry)
    {
        return flights.Exists(flight => !flight.finished && flight.entry == entry);
    }

    public void BeginLaunch()
    {
        Begin(false);
    }

    public void BeginReturn()
    {
        Begin(true);
    }

    private void Begin(bool returning)
    {
        Stop();
        CompletedCount = 0;
        FailedCount = 0;
        LastFailureReason = null;
        IsReturning = returning;
        IsLaunching = !returning;
        for (int slot = 0; slot < fleet.Ships.Length; slot++)
        {
            SiegeFleetEntry entry = fleet.Ships[slot];
            if (entry == null)
            {
                continue;
            }

            if (!entry.IsAlive || entry.Unit == null)
            {
                if (returning)
                {
                    fleet.Scrap(slot);
                }

                continue;
            }

            HangarModule hangar = fleet.GetHangar(slot);
            var flight = new Flight
            {
                slot = slot,
                entry = entry,
                hangar = hangar,
                requiresHangar = fleet.UsesHangars
            };
            BindUnit(flight);
            flights.Add(flight);
            hangar?.SetDoorOpen(true);
            BuildRoute(flight, returning);
        }
    }

    private void BindUnit(Flight flight)
    {
        ReleasePropulsion(flight);
        flight.unit = flight.entry.Unit;
        flight.body = flight.unit != null ? flight.unit.GetComponent<Rigidbody>() : null;
        if (flight.unit == null || flight.body == null)
        {
            return;
        }

        flight.unit.RefreshChildren();
        flight.engines = flight.unit.GetComponentsInChildren<Thruster>();
        flight.hoverStates = new bool[flight.engines.Length];
        for (int index = 0; index < flight.engines.Length; index++)
        {
            Thruster engine = flight.engines[index];
            flight.hoverStates[index] = engine is HoverThruster hover && hover.isHovered;
            engine.thrust = 0f;
            engine.lastThrustValue = 0f;
        }

        flight.hoverController = flight.unit.hoverFlightController;
        flight.hoverControllerEnabled = flight.hoverController != null && flight.hoverController.enabled;
        TakeGuidanceControl(flight);
        flight.body.isKinematic = false;
    }

    private void TakeGuidanceControl(Flight flight)
    {
        flight.entry.Cockpit?.SetControlMode(UnitControlMode.Autonomous);
        flight.unit.SetMovementInput(Vector3.zero);
        EnemyController ai = flight.unit.GetComponent<EnemyController>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        if (flight.hoverController != null)
        {
            flight.hoverController.enabled = false;
        }

        foreach (Thruster engine in flight.engines)
        {
            if (engine == null || !engine.isActiveAndEnabled || engine.GetComponentInParent<ControlUnit>() != flight.unit)
            {
                continue;
            }

            if (engine is HoverThruster hover)
            {
                hover.isHovered = false;
            }

            // 使用真实电网请求；空闲时 SupplyEfficiency=1 不代表存在可用电源。
            engine.GetComponent<Power>()?.SetWorkRequested(powerRequester, true);
        }
    }

    private Vector3 DockPosition(Flight flight)
    {
        ControlUnit unit = flight.unit;
        flight.dockRotation = flight.entry.DockRotation;
        if (unit == null || flight.entry.Cockpit == null)
        {
            return flight.entry.DockPosition;
        }

        Block cockpit = flight.entry.Cockpit.GetComponent<Block>();
        BlockData source = cockpit == null ? null : flight.entry.Blueprint.blocks.Find(block => block.id == cockpit.uniqueId);
        if (source == null)
        {
            return flight.entry.DockPosition;
        }

        // 重组后根节点可能改变；按驾驶舱原始模块位置恢复原泊位坐标，不把新根当成旧根。
        Vector3 cockpitOffset = unit.transform.InverseTransformPoint(cockpit.transform.position);
        Vector3 sourceOffset = new Vector3(source.posX, source.posY, source.posZ);
        Quaternion currentCockpitLocal = Quaternion.Inverse(unit.transform.rotation) * cockpit.transform.rotation;
        Quaternion sourceRotation = new Quaternion(source.rotX, source.rotY, source.rotZ, source.rotW);
        flight.dockRotation = flight.entry.DockRotation * sourceRotation * Quaternion.Inverse(currentCockpitLocal);
        return flight.entry.DockPosition + flight.entry.DockRotation * sourceOffset - flight.dockRotation * cockpitOffset;
    }

    private void BuildRoute(Flight flight, bool returning)
    {
        Vector3 dock = DockPosition(flight);
        Bounds hull = BlockBounds.FromBlueprint(flight.entry.Blueprint.blocks, Matrix4x4.identity);
        Vector3 forward = flight.entry.DockRotation * Vector3.forward;
        Block hangarBlock = flight.hangar != null ? flight.hangar.GetComponent<Block>() : null;
        float depth = hangarBlock != null ? hangarBlock.z : 6f;
        float height = hangarBlock != null ? hangarBlock.y : 8f;
        Vector3 approach = dock + forward * (depth * 0.5f + hull.size.z + 3f);
        if (!returning)
        {
            flight.points = new[]
            {
                approach
            };
            return;
        }

        if (Vector3.Distance(flight.body.position, dock) <= Mathf.Max(0.05f, settings.dockingTolerance))
        {
            flight.points = new[]
            {
                dock
            };
            return;
        }

        float cruiseHeight = Mathf.Max(flight.body.position.y, dock.y + height * 0.5f + hull.size.y + settings.approachClearance);
        Vector3 aboveCurrent = new Vector3(flight.body.position.x, cruiseHeight, flight.body.position.z);
        Vector3 aboveApproach = new Vector3(approach.x, cruiseHeight, approach.z);
        bool inside = false;
        if (hangarBlock != null)
        {
            Vector3 physicalCenter = flight.body.position;
            BlockData source = flight.entry.Blueprint.blocks.Find(block => block.id == flight.entry.Cockpit.GetComponent<Block>().uniqueId);
            if (source != null)
            {
                Quaternion sourceRotation = new Quaternion(source.rotX, source.rotY, source.rotZ, source.rotW);
                Vector3 sourcePosition = new Vector3(source.posX, source.posY, source.posZ);
                physicalCenter = flight.entry.Cockpit.transform.TransformPoint(Quaternion.Inverse(sourceRotation) * (hull.center - sourcePosition));
            }

            Vector3 local = flight.hangar.transform.InverseTransformPoint(physicalCenter);
            inside = Mathf.Abs(local.x) < hangarBlock.x * 0.5f && Mathf.Abs(local.z) < hangarBlock.z * 0.5f && Mathf.Abs(local.y) < hangarBlock.y * 0.5f;
        }

        // 正在门内的舰艇先沿门轴退出；避免直接爬升撞到库顶。
        flight.points = inside ? new[]
        {
            approach,
            aboveApproach,
            approach,
            dock
        }

        : new[]
        {
            aboveCurrent,
            aboveApproach,
            approach,
            dock
        };
    }

    public void Tick(float deltaTime)
    {
        if ((!IsLaunching && !IsReturning) || deltaTime <= 0f)
        {
            return;
        }

        foreach (Flight flight in flights)
        {
            if (!flight.finished)
            {
                TickFlight(flight, deltaTime);
            }
        }
    }

    private void TickFlight(Flight flight, float dt)
    {
        flight.elapsed += dt;
        if (!flight.entry.IsAlive || flight.entry.Unit == null || (flight.requiresHangar && (flight.hangar == null || !flight.hangar.IsOperational)))
        {
            Finish(flight, false, flight.entry.IsAlive ? "Assigned hangar destroyed." : "Ship cockpit lost.");
            return;
        }

        if (flight.entry.Unit != flight.unit)
        {
            BindUnit(flight);
            BuildRoute(flight, IsReturning);
            flight.point = 0;
        }

        if (flight.body == null || flight.elapsed > Mathf.Max(1f, settings.timeout))
        {
            Finish(flight, false, flight.body == null ? "Ship body unavailable." : "Navigation timed out.");
            return;
        }

        TakeGuidanceControl(flight);
        Vector3 target = flight.points[flight.point];
        bool final = flight.point == flight.points.Length - 1;
        float distance = Vector3.Distance(flight.body.position, target);
        float angle = Quaternion.Angle(flight.body.rotation, flight.dockRotation);
        bool enteringNext = IsReturning && flight.point == flight.points.Length - 2;
        float tolerance = enteringNext ? 0.05f : final ? Mathf.Max(0.05f, settings.dockingTolerance) : 0.6f;
        // 在门外先精确对准再进入，防止恰好等宽的蓝图带着横向余速刮碰门框。
        float allowedSpeed = enteringNext ? 0.15f : final ? 0.3f : 1f;
        float allowedAngle = enteringNext || final ? 0.5f : 4f;
        if (distance <= tolerance && flight.body.linearVelocity.magnitude < allowedSpeed && angle < allowedAngle)
        {
            flight.point++;
            if (final)
            {
                Finish(flight, true);
                return;
            }

            target = flight.points[flight.point];
        }

        ReadPropulsion(flight, out float driveForce, out float liftForce);
        float mass = Mathf.Max(0.01f, flight.body.mass);
        bool powered = driveForce > 0.01f && (!flight.body.useGravity || liftForce >= mass * Physics.gravity.magnitude);
        flight.disabledFor = powered ? 0f : flight.disabledFor + dt;
        if (flight.disabledFor >= Mathf.Max(0.1f, settings.disabledGrace))
        {
            Finish(flight, false, "Insufficient powered propulsion or lift.");
            return;
        }

        if (flight.hangar != null && !flight.hangar.DoorIsOpen)
        {
            // 等待实体门打开期间在原处制动，保持电力与失能计时，避免冲撞门板。
            target = flight.body.position;
        }

        ApplyGuidance(flight, target, driveForce / mass, liftForce / mass, dt);
    }

    private static void ReadPropulsion(Flight flight, out float driveForce, out float liftForce)
    {
        driveForce = 0f;
        liftForce = 0f;
        foreach (Thruster engine in flight.engines)
        {
            if (engine == null || !engine.isActiveAndEnabled || engine.GetComponentInParent<ControlUnit>() != flight.unit)
            {
                continue;
            }

            float force = Mathf.Max(0f, engine.maxThrust) * engine.PowerEfficiency;
            if (engine is HoverThruster)
            {
                liftForce += force;
            }
            else
            {
                driveForce += force;
            }
        }
    }

    private void ApplyGuidance(Flight flight, Vector3 target, float driveAcceleration, float liftAcceleration, float dt)
    {
        Rigidbody body = flight.body;
        float acceleration = Mathf.Min(Mathf.Max(0.1f, settings.acceleration), driveAcceleration);
        Vector3 offset = target - body.position;
        float speed = Mathf.Min(Mathf.Max(0.1f, settings.speed), Mathf.Sqrt(2f * Mathf.Max(0f, acceleration) * offset.magnitude));
        Vector3 desiredVelocity = offset.sqrMagnitude > 0.0001f ? offset.normalized * Mathf.Min(speed, offset.magnitude * 1.8f) : Vector3.zero;
        Vector3 requested = (desiredVelocity - body.linearVelocity) / Mathf.Max(0.1f, dt);
        Vector3 horizontal = Vector3.ClampMagnitude(new Vector3(requested.x, 0f, requested.z), acceleration);
        float gravity = body.useGravity ? -Physics.gravity.y : 0f;
        float vertical = Mathf.Clamp(requested.y + gravity, 0f, Mathf.Max(0f, liftAcceleration));
        if (!body.useGravity)
        {
            vertical = Mathf.Clamp(requested.y, -acceleration, acceleration);
        }

        // 自动对准是机库的辅助驾驶；推进力仍由存活、有电发动机的能力给出硬上限。
        body.AddForce(new Vector3(horizontal.x, vertical, horizontal.z), ForceMode.Acceleration);
        Quaternion error = flight.dockRotation * Quaternion.Inverse(body.rotation);
        error.ToAngleAxis(out float degrees, out Vector3 axis);
        if (degrees > 180f)
        {
            degrees -= 360f;
        }

        Vector3 targetAngular = axis.sqrMagnitude > 0.001f && !float.IsNaN(axis.x) ? axis.normalized * Mathf.Clamp(
            degrees * Mathf.Deg2Rad * 3f,
            -settings.angularSpeed * Mathf.Deg2Rad,
            settings.angularSpeed * Mathf.Deg2Rad) : Vector3.zero;
        if (driveAcceleration > 0f)
        {
            body.AddTorque(Vector3.ClampMagnitude((targetAngular - body.angularVelocity) * 4f, 4f), ForceMode.Acceleration);
        }
    }

    private void Finish(Flight flight, bool success, string reason = null)
    {
        flight.finished = true;
        ReleasePropulsion(flight);
        if (!success)
        {
            FailedCount++;
            LastFailureReason = reason;
            fleet.Scrap(flight.slot);
        }
        else
        {
            CompletedCount++;
            if (IsReturning)
            {
                // Navigation has already met the position/speed/angle tolerance. Seat the landing
                // gear exactly so an equal-capacity hull cannot leave a door-blocking residual offset.
                Vector3 position = DockPosition(flight);
                flight.body.position = position;
                flight.body.rotation = flight.dockRotation;
                flight.unit.transform.SetPositionAndRotation(position, flight.dockRotation);
                SiegeFleetPhysics.Park(flight.unit);
            }
            else
            {
                flight.entry.Cockpit.SetControlMode(fleet.Driven == flight.entry ? UnitControlMode.Player : UnitControlMode.Autonomous);
                SiegeFleetPhysics.Activate(flight.unit);
            }
        }

        flight.hangar?.SetDoorOpen(false);
    }

    private void ReleasePropulsion(Flight flight)
    {
        if (flight.engines == null)
        {
            return;
        }

        for (int index = 0; index < flight.engines.Length; index++)
        {
            Thruster engine = flight.engines[index];
            if (engine == null)
            {
                continue;
            }

            engine.GetComponent<Power>()?.SetWorkRequested(powerRequester, false);
            if (engine is HoverThruster hover)
            {
                hover.isHovered = flight.hoverStates[index];
            }
        }

        if (flight.hoverController != null)
        {
            flight.hoverController.enabled = flight.hoverControllerEnabled;
        }
    }

    public void Stop()
    {
        foreach (Flight flight in flights)
        {
            ReleasePropulsion(flight);
            flight.hangar?.SetDoorOpen(false);
        }

        flights.Clear();
        IsLaunching = false;
        IsReturning = false;
    }
}
