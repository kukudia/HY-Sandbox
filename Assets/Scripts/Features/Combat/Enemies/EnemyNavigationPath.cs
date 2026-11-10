// 职责：把 NavMesh 地面走廊转换为物理舰船的水平驾驶方向，不使用 Agent 改写 Transform。
// 入口：TryGetDirection()；EnemyController 按移动决策周期重算，缺少导航覆盖时回退局部避障。
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

internal sealed class EnemyNavigationPath
{
    // Unity 原生对象必须在主线程决策时创建，不能经 MonoBehaviour 字段初始化触发。
    private NavMeshPath path;
    private Vector3[] corners = new Vector3[32];
    private int cornerCount;
    internal bool NeedsDetour
    {
        get; private set;
    }
    internal float SurfaceHeight
    {
        get; private set;
    }

    internal bool TryGetDirection(
        Scene scene,
        Vector3 position,
        Vector3 destination,
        float sampleDistance,
        float cornerDistance,
        out Vector3 direction)
    {
        direction = Vector3.zero;
        cornerCount = 0;
        NeedsDetour = false;
        EnemyNavigationWorld world = EnemyNavigationWorld.Find(scene);
        if (world == null)
        {
            return false;
        }

        path ??= new NavMeshPath();
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = world.AgentTypeId,
            areaMask = NavMesh.AllAreas
        };
        if (!NavMesh.SamplePosition(position, out NavMeshHit start, sampleDistance, filter) || !NavMesh.SamplePosition(destination, out NavMeshHit end, sampleDistance, filter))
        {
            return false;
        }

        // 不把高空/网格边缘的单位横向吸附到远处的走廊。
        Vector3 startOffset = start.position - position;
        Vector3 endOffset = end.position - destination;
        startOffset.y = 0f;
        endOffset.y = 0f;
        if (startOffset.sqrMagnitude > 64f || endOffset.sqrMagnitude > 64f)
        {
            return false;
        }

        if (!NavMesh.CalculatePath(start.position, end.position, filter, path) || path.status == NavMeshPathStatus.PathInvalid)
        {
            // 覆盖内的断路不允许退回直冲墙壁；覆盖外才使用局部避障。
            NeedsDetour = true;
            return true;
        }

        NeedsDetour = path.status != NavMeshPathStatus.PathComplete || NavMesh.Raycast(start.position, end.position, out _, filter);
        cornerCount = path.GetCornersNonAlloc(corners);
        if (cornerCount == corners.Length)
        {
            corners = path.corners;
            cornerCount = corners.Length;
        }

        SurfaceHeight = start.position.y;
        for (int i = 1; i < cornerCount; i++)
        {
            Vector3 offset = corners[i] - position;
            offset.y = 0f;
            SurfaceHeight = Mathf.Max(start.position.y, corners[i].y);
            if (offset.sqrMagnitude > cornerDistance * cornerDistance)
            {
                direction = offset.normalized;
                return true;
            }
        }

        return true;
    }

    internal void Clear()
    {
        cornerCount = 0;
        NeedsDetour = false;
        SurfaceHeight = 0f;
    }

    internal void Draw(float height)
    {
        Gizmos.color = Color.blue;
        for (int i = 1; i < cornerCount; i++)
        {
            Vector3 from = corners[i - 1];
            Vector3 to = corners[i];
            from.y = Mathf.Max(height, from.y);
            to.y = Mathf.Max(height, to.y);
            Gizmos.DrawLine(from, to);
        }
    }
}
