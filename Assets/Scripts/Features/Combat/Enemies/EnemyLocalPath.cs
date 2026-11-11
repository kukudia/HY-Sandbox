// 职责：在有限三维栅格中规划整舰可通行路线，缓存并简化航点，不直接操纵刚体。
// 入口：TryFollow()；静态障碍按舰体尺寸膨胀，失败重试和成功重规划均限频。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class EnemyLocalPath
{
    private readonly List<Vector3> path = new List<Vector3>();
    private readonly List<Bounds> obstacles = new List<Bounds>();
    private readonly Dictionary<Vector3Int, Node> nodes = new Dictionary<Vector3Int, Node>();
    private readonly List<Vector3Int> open = new List<Vector3Int>();
    private Collider[] overlaps = new Collider[128];
    private Vector3 destination;
    private Vector3 origin;
    private Vector3 offset;
    private float nextPlan;
    private int waypoint;
    internal string ReplanReason { get; private set; } = "Initial";
    internal float PlanMilliseconds
    {
        get; private set;
    }
    internal int Revision
    {
        get; private set;
    }
    internal int Waypoint => waypoint;
    internal string Status { get; private set; } = "Idle";
    internal bool Active => waypoint < path.Count;
    internal IReadOnlyList<Vector3> Points => path;
    internal int Expanded
    {
        get; private set;
    }

    private static readonly Vector3Int[] Steps =
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.right,
        Vector3Int.left,
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1)
    };
    private struct Node
    {
        internal float Cost;
        internal Vector3Int Parent;
        internal bool Closed;
    }

    internal void Clear()
    {
        path.Clear();
        waypoint = 0;
        nextPlan = 0f;
        Revision++;
        Status = "Cleared";
        Expanded = 0;
        PlanMilliseconds = 0f;
        ReplanReason = "Initial";
    }

    internal bool TryFollow(
        ControlUnit owner,
        Bounds hull,
        Vector3 goal,
        LayerMask layers,
        float cellSize,
        float radius,
        int budget,
        float retry,
        float minHeight,
        float maxHeight,
        out Vector3 point)
    {
        Vector3 position = owner.transform.position;
        point = position;
        cellSize = Mathf.Max(2f, cellSize);
        radius = Mathf.Max(cellSize * 2f, radius);
        Vector3[] fallback = null;
        int fallbackWaypoint = waypoint;
        if (Active && Time.time >= nextPlan && (goal - destination).sqrMagnitude > cellSize * cellSize * 4f)
        {
            Clear();
            ReplanReason = "GoalMoved";
        }

        if (Active)
        {
            // 提前切换中间航点，不要求逐点减速至零；验证跳点整段舰体通道。
            Capture(owner, hull, layers, radius);
            Vector3 forwardGoal = position + Vector3.ClampMagnitude(goal - position, radius * 0.8f);
            if (ClearFlight(position, forwardGoal))
            {
                Clear();
                Status = "Direct";
                ReplanReason = "GoalVisible";
                return false;
            }

            if (!ClearFlight(position, path[waypoint]))
            {
                path.Clear();
                waypoint = 0;
                Revision++;
                Status = "Blocked";
                ReplanReason = "GeometryChanged";
            }
            else
            {
                while (waypoint + 1 < path.Count && ClearFlight(position, path[waypoint + 1]))
                {
                    waypoint++;
                }

                if (Vector3.Distance(position, path[waypoint]) < Mathf.Max(2f, cellSize * 0.6f))
                {
                    waypoint++;
                }

                if (!Active)
                {
                    Revision++;
                    Status = "Completed";
                }

                if (Active)
                {
                    point = path[waypoint];
                    bool rolling = (destination - path[path.Count - 1]).sqrMagnitude > cellSize * cellSize * 4f && waypoint == path.Count - 1 && (position - path[path.Count - 1]).sqrMagnitude < radius * radius * 0.1225f;
                    if (!rolling || Time.time < nextPlan)
                    {
                        return true;
                    }

                    // 前移局部搜索窗口；替换失败仍沿已验证的旧路线飞行，不等待窗口终点。
                    fallback = path.ToArray();
                    fallbackWaypoint = waypoint;
                    ReplanReason = "RollingWindow";
                }
            }
        }

        if (Time.time < nextPlan)
        {
            return false;
        }

        double planStarted = Time.realtimeSinceStartupAsDouble;
        nextPlan = Time.time + Mathf.Max(0.5f, retry);
        destination = goal;
        Capture(owner, hull, layers, radius);
        Vector3 localGoal = position + Vector3.ClampMagnitude(goal - position, radius * 0.8f);
        if (ClearSegment(position, localGoal))
        {
            path.Clear();
            waypoint = 0;
            Revision++;
            Expanded = 0;
            Status = "Direct";
            PlanMilliseconds = (float)((Time.realtimeSinceStartupAsDouble - planStarted) * 1000.0);
            return false;
        }

        Revision++;
        Status = "Searching";
        origin = position;
        // 接敌高度附近的悬浮误差不应变成新的低空航层；否则每次规划都会继续降低设定高度。
        Vector3 levelOrigin = new Vector3(position.x, goal.y, position.z);
        if (Mathf.Abs(position.y - goal.y) < 2f && ClearSegment(position, levelOrigin))
        {
            origin = levelOrigin;
        }

        nodes.Clear();
        open.Clear();
        nodes[Vector3Int.zero] = new Node
        {
            Cost = 0f
        };
        open.Add(Vector3Int.zero);
        Expanded = 0;
        while (open.Count > 0 && Expanded < Mathf.Max(64, budget))
        {
            int best = 0;
            float score = float.PositiveInfinity;
            for (int i = 0; i < open.Count; i++)
            {
                float candidate = nodes[open[i]].Cost + Vector3.Distance(World(open[i], cellSize), localGoal);
                if (candidate < score)
                {
                    score = candidate;
                    best = i;
                }
            }

            Vector3Int key = open[best];
            open.RemoveAt(best);
            Node current = nodes[key];
            current.Closed = true;
            nodes[key] = current;
            Expanded++;
            Vector3 world = World(key, cellSize);
            if (ClearFlight(world, localGoal))
            {
                path.Clear();
                path.Add(localGoal);
                while (key != Vector3Int.zero)
                {
                    path.Add(World(key, cellSize));
                    key = nodes[key].Parent;
                }

                path.Reverse();
                if ((goal - position).sqrMagnitude > radius * radius * 0.64f && path.Count > 1)
                {
                    // 窗口终点并非任务终点。保留本段最高航层，避免在下一堵墙前先下降再爬升。
                    float peak = localGoal.y;
                    foreach (Vector3 routePoint in path)
                    {
                        peak = Mathf.Max(peak, routePoint.y);
                    }

                    Vector3 exit = path[path.Count - 1];
                    exit.y = peak;
                    if (ClearFlight(path[path.Count - 2], exit))
                    {
                        path[path.Count - 1] = exit;
                    }
                }

                waypoint = 0;
                point = path[0];
                Status = "Following";
                PlanMilliseconds = (float)((Time.realtimeSinceStartupAsDouble - planStarted) * 1000.0);
                return true;
            }

            foreach (Vector3Int step in Steps)
            {
                Vector3Int next = key + step;
                Vector3 nextWorld = World(next, cellSize);
                if ((nextWorld - origin).sqrMagnitude > radius * radius || nextWorld.y < minHeight || nextWorld.y > maxHeight)
                {
                    continue;
                }

                if (nodes.TryGetValue(next, out Node previous) && previous.Closed)
                {
                    continue;
                }

                if (!ClearSegment(world, nextWorld))
                {
                    continue;
                }

                // 可通行路线中略偏好抬升，再下降；水平绕行保留给确实需要的布局。
                float cost = current.Cost + cellSize * (step.y > 0 ? 1f : step.y < 0 ? 1.05f : 1.15f);
                if (nodes.ContainsKey(next) && cost >= previous.Cost)
                {
                    continue;
                }

                bool exists = nodes.ContainsKey(next);
                nodes[next] = new Node
                {
                    Cost = cost,
                    Parent = key
                };
                if (!exists)
                {
                    open.Add(next);
                }
            }
        }

        Status = open.Count == 0 ? "NoRoute" : "BudgetExceeded";
        PlanMilliseconds = (float)((Time.realtimeSinceStartupAsDouble - planStarted) * 1000.0);
        if (fallback != null)
        {
            path.Clear();
            path.AddRange(fallback);
            waypoint = fallbackWaypoint;
            point = path[waypoint];
            Status = "FollowingRetained";
            return true;
        }

        return false;
    }

    private Vector3 World(Vector3Int cell, float size)
    {
        return origin + (Vector3)cell * size;
    }

    private void Capture(ControlUnit owner, Bounds hull, LayerMask layers, float radius)
    {
        obstacles.Clear();
        offset = hull.center - owner.transform.position;
        // 航向改变会旋转机翼，水平使用保守圆包络，防止只验证当前朝向而切角。
        float horizontal = new Vector2(hull.extents.x, hull.extents.z).magnitude + 0.75f;
        Vector3 padding = new Vector3(horizontal, hull.extents.y + 0.75f, horizontal);
        PhysicsScene physics = owner.gameObject.scene.GetPhysicsScene();
        int count;
        do
        {
            count = physics.OverlapBox(
                owner.transform.position,
                Vector3.one * (radius + horizontal),
                overlaps,
                Quaternion.identity,
                layers,
                QueryTriggerInteraction.Ignore);
            if (count < overlaps.Length || overlaps.Length >= 4096)
            {
                break;
            }

            System.Array.Resize(ref overlaps, overlaps.Length * 2);
        }
        while (true);
        for (int i = 0; i < count; i++)
        {
            Collider collider = overlaps[i];
            ControlUnit unit = collider.GetComponentInParent<ControlUnit>();
            if (unit == owner || (unit != null && unit.Building == null) || (collider.attachedRigidbody != null && !collider.attachedRigidbody.isKinematic))
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            bounds.Expand(padding * 2f);
            obstacles.Add(bounds);
        }

        // 饱和时保守阻止规划，避免把漏掉的障碍当作可通行区域。
        if (count == overlaps.Length)
        {
            obstacles.Add(new Bounds(owner.transform.position, Vector3.one * radius * 4f));
        }
    }

    private bool ClearFlight(Vector3 from, Vector3 to)
    {
        Vector3 elevated = new Vector3(from.x, to.y, from.z);
        return ClearSegment(from, elevated) && ClearSegment(elevated, to);
    }

    private bool ClearSegment(Vector3 from, Vector3 to)
    {
        from += offset;
        to += offset;
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        Ray ray = new Ray(from, distance > 0.001f ? delta / distance : Vector3.up);
        foreach (Bounds obstacle in obstacles)
        {
            if (obstacle.Contains(from) || obstacle.Contains(to) || (obstacle.IntersectRay(ray, out float hit) && hit <= distance))
            {
                return false;
            }
        }

        return true;
    }
}
