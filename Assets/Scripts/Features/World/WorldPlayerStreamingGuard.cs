// 职责：在地形碰撞尚未就绪时暂存载具姿态/速度，限制世界边界并在区块完成后恢复。
// 入口：Tick() / Release()；WorldSessionManager在FixedUpdate调用，退出或销毁必须释放。
using UnityEngine;

internal sealed class WorldPlayerStreamingGuard
{
    private readonly ProceduralVoxelWorld world;
    private readonly WorldGenerationSettings settings;
    private Rigidbody heldBody;
    private bool heldWasKinematic;
    private Vector3 heldVelocity;
    private Vector3 heldAngularVelocity;
    private Vector3 lastSafePosition;
    private Vector3 heldTarget;
    private Transform tracked;
    private Bounds localBounds;
    public Vector3 Focus
    {
        get; private set;
    }
    public bool IsWaiting => heldBody != null;
    public float Progress { get; private set; } = 1f;

    internal WorldPlayerStreamingGuard(ProceduralVoxelWorld terrain, WorldGenerationSettings configuration)
    {
        world = terrain;
        settings = configuration;
        Focus = world.SpawnPosition;
    }

    internal void Tick(Transform player)
    {
        if (tracked != player)
        {
            Release();
            tracked = player;
            lastSafePosition = player.position;
            localBounds = CaptureLocalBounds(player);
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        Vector3 target = IsWaiting ? heldTarget : player.position;
        Vector3 local = world.transform.InverseTransformPoint(target);
        float span = settings.ChunkSize * settings.CellSize;
        float minimum = -settings.WorldRadiusInChunks * span + span + localBounds.extents.magnitude;
        float maximum = (settings.WorldRadiusInChunks + 1) * span - span - localBounds.extents.magnitude;
        local.x = Mathf.Clamp(local.x, minimum, maximum);
        local.z = Mathf.Clamp(local.z, minimum, maximum);
        Vector3 bounded = world.transform.TransformPoint(local);
        if ((bounded - target).sqrMagnitude > 0.001f)
        {
            target = bounded;
            if (IsWaiting)
            {
                heldTarget = bounded;
            }
            else
            {
                player.position = bounded;
            }
        }

        Vector3 velocity = IsWaiting ? heldVelocity : body != null && !body.isKinematic ? body.linearVelocity : Vector3.zero;
        // 单物理步的路径必须能放进有界加载窗口；只约束极端速度，不增加无限长的预加载走廊。
        float horizontalRadius = new Vector2(localBounds.extents.x, localBounds.extents.z).magnitude;
        float maximumStep = Mathf.Max(1f, span * (settings.LoadRadius * 2 - 1) - horizontalRadius * 2f - world.CellSize * 4f);
        Vector3 horizontal = Vector3.ClampMagnitude(new Vector3(velocity.x, 0f, velocity.z), maximumStep / Time.fixedDeltaTime);
        velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        if (!IsWaiting && body != null && !body.isKinematic)
        {
            body.linearVelocity = velocity;
        }

        Vector3 prediction = velocity * Time.fixedDeltaTime;
        Focus = target + prediction * 0.5f;
        bool ready = HasTerrainForBounds(player, target, prediction);
        if (!ready && body != null && !IsWaiting)
        {
            heldBody = body;
            heldWasKinematic = body.isKinematic;
            heldVelocity = velocity;
            heldAngularVelocity = body.isKinematic ? Vector3.zero : body.angularVelocity;
            heldTarget = target;
            body.isKinematic = true;
            player.position = lastSafePosition;
        }
        else if (ready)
        {
            if (IsWaiting)
            {
                player.position = heldTarget;
                Release();
            }

            lastSafePosition = player.position;
        }
    }

    private bool HasTerrainForBounds(Transform player, Vector3 target, Vector3 prediction)
    {
        Bounds bounds = new Bounds(target, Vector3.zero);
        Vector3 delta = target - player.position;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = localBounds.center + Vector3.Scale(
                localBounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            Vector3 point = player.TransformPoint(corner) + delta;
            bounds.Encapsulate(point);
            bounds.Encapsulate(point + prediction);
        }

        bounds.Expand(world.CellSize * 2f);
        Vector2Int minimum = world.ColumnAt(bounds.min);
        Vector2Int maximum = world.ColumnAt(bounds.max);
        int total = (maximum.x - minimum.x + 1) * (maximum.y - minimum.y + 1);
        int loaded = 0;
        for (int z = minimum.y; z <= maximum.y; z++)
        {
            for (int x = minimum.x; x <= maximum.x; x++)
            {
                if (world.HasColumn(new Vector2Int(x, z)))
                {
                    loaded++;
                }
            }
        }

        Progress = (float)loaded / Mathf.Max(1, total);
        return loaded == total;
    }

    internal static Bounds CaptureLocalBounds(Transform construct)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool found = false;
        foreach (Block block in construct.GetComponentsInChildren<Block>())
        {
            Vector3 half = new Vector3(block.x, block.y, block.z) * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = construct.InverseTransformPoint(block.transform.TransformPoint(Vector3.Scale(half, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f))));
                if (!found)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        return bounds;
    }

    internal void Release()
    {
        if (heldBody == null)
        {
            return;
        }

        heldBody.isKinematic = heldWasKinematic;
        if (!heldWasKinematic)
        {
            heldBody.linearVelocity = heldVelocity;
            heldBody.angularVelocity = heldAngularVelocity;
        }

        heldBody = null;
    }
}
