// 职责：统一移动/旋转操作轴的方向推断；保持既有本地位置及名称回退语义。
// 入口：Local() / World()；不拥有组件身份或序列化字段。
using UnityEngine;

internal static class BuildHandleAxis
{
    internal static Vector3 World(Transform handle)
    {
        Transform space = handle.parent != null ? handle.parent : handle;
        return space.TransformDirection(Local(handle)).normalized;
    }

    internal static Vector3 Local(Transform handle)
    {
        Vector3 localPosition = handle.localPosition;
        if (localPosition.sqrMagnitude > 0.001f)
        {
            return localPosition.normalized;
        }

        string name = handle.name.ToLowerInvariant();
        if (name.Contains("forward"))
        {
            return Vector3.forward;
        }

        if (name.Contains("right"))
        {
            return Vector3.right;
        }

        if (name.Contains("up"))
        {
            return Vector3.up;
        }

        return Vector3.right;
    }
}
