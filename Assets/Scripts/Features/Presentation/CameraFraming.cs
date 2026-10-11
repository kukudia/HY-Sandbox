// 职责：计算目标包围盒、视锥取景距离及环绕姿态，不处理输入或协程。
// 入口：TryGetFocusPose() / CalculateFramingDistance()；镜头和网格尺寸由调用者提供。
using System;
using UnityEngine;

internal sealed class CameraFraming
{
    private readonly Transform _transform;
    private readonly Camera _camera;
    private readonly Func<float> _gridSize;
    public CameraFraming(Transform transform, Camera camera, Func<float> gridSize)
    {
        _transform = transform;
        _camera = camera;
        _gridSize = gridSize;
    }

    public Vector3 GetOrbitCameraDirection(float yawDegrees, float pitchDegrees)
    {
        float yaw = yawDegrees * Mathf.Deg2Rad;
        float pitch = Mathf.Clamp(pitchDegrees, -75f, 75f) * Mathf.Deg2Rad;
        float cosPitch = Mathf.Cos(pitch);
        return new Vector3(Mathf.Sin(yaw) * cosPitch, Mathf.Sin(pitch), Mathf.Cos(yaw) * cosPitch).normalized;
    }

    public bool TryGetFocusPose(GameObject obj, out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = _transform.position;
        targetRotation = _transform.rotation;
        if (!TryCalculateBlockBounds(obj, out Bounds bounds))
        {
            return false;
        }

        return TryGetFocusPose(bounds, bounds.center, out targetPosition, out targetRotation);
    }

    public bool TryGetFocusPose(Bounds frameBounds, Vector3 lookPoint, out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = _transform.position;
        targetRotation = _transform.rotation;
        Vector3 cameraDirection = _transform.position - lookPoint;
        if (frameBounds.Contains(_transform.position) || cameraDirection.sqrMagnitude < 0.001f)
        {
            cameraDirection = new Vector3(1f, 0.6f, -1f);
        }

        cameraDirection.Normalize();
        float distance = CalculateFramingDistance(frameBounds, lookPoint, cameraDirection);
        targetPosition = lookPoint + cameraDirection * distance;
        Vector3 lookDirection = lookPoint - targetPosition;
        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        return true;
    }

    public bool TryGetOrbitPose(
        Bounds frameBounds,
        float yawDegrees,
        float pitchDegrees,
        float radiusMultiplier,
        out Vector3 targetPosition,
        out Quaternion targetRotation)
    {
        targetPosition = _transform.position;
        targetRotation = _transform.rotation;
        Vector3 lookPoint = frameBounds.center;
        float yaw = yawDegrees * Mathf.Deg2Rad;
        float pitch = Mathf.Clamp(pitchDegrees, -75f, 75f) * Mathf.Deg2Rad;
        float cosPitch = Mathf.Cos(pitch);
        Vector3 cameraDirection = new Vector3(Mathf.Sin(yaw) * cosPitch, Mathf.Sin(pitch), Mathf.Cos(yaw) * cosPitch).normalized;
        float distance = CalculateFramingDistance(frameBounds, lookPoint, cameraDirection);
        distance *= Mathf.Max(radiusMultiplier, 0.1f);
        targetPosition = lookPoint + cameraDirection * distance;
        Vector3 lookDirection = lookPoint - targetPosition;
        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        return true;
    }

    public float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint)
    {
        return CalculateFramingDistance(bounds, lookPoint, _transform.position - lookPoint);
    }

    public float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint, Vector3 cameraDirection)
    {
        if (cameraDirection.sqrMagnitude < 0.001f)
        {
            cameraDirection = new Vector3(1f, 0.6f, -1f);
        }

        cameraDirection.Normalize();
        Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraDirection);
        if (cameraRight.sqrMagnitude < 0.001f)
        {
            cameraRight = Vector3.Cross(Vector3.forward, cameraDirection);
        }

        cameraRight.Normalize();
        Vector3 cameraUp = Vector3.Cross(cameraDirection, cameraRight).normalized;
        Camera cameraComponent = _camera;
        float verticalFov = cameraComponent != null ? cameraComponent.fieldOfView : 60f;
        float aspect = cameraComponent != null ? cameraComponent.aspect : 16f / 9f;
        float verticalTan = Mathf.Tan(verticalFov * Mathf.Deg2Rad * 0.5f);
        float horizontalTan = verticalTan * aspect;
        Vector3 extents = bounds.extents;
        Vector3 center = bounds.center;
        float requiredDistance = 0f;
        float nearestDepth = 0f;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 relative = corner - lookPoint;
                    float depthTowardCamera = Vector3.Dot(relative, cameraDirection);
                    float distanceForWidth = depthTowardCamera + Mathf.Abs(Vector3.Dot(relative, cameraRight)) / Mathf.Max(horizontalTan, 0.01f);
                    float distanceForHeight = depthTowardCamera + Mathf.Abs(Vector3.Dot(relative, cameraUp)) / Mathf.Max(verticalTan, 0.01f);
                    requiredDistance = Mathf.Max(requiredDistance, distanceForWidth, distanceForHeight);
                    nearestDepth = Mathf.Max(nearestDepth, depthTowardCamera);
                }
            }
        }

        return Mathf.Max(requiredDistance * 1.15f, nearestDepth + 1f, 1f);
    }

    public bool TryCalculateBlockBounds(GameObject obj, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Block block = obj.GetComponent<Block>();
        if (block != null)
        {
            Vector3 size = new Vector3(block.x, block.y, block.z) * _gridSize();
            bounds = new Bounds(block.transform.position, size);
            return true;
        }

        if (renderers.Length == 0)
        {
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }
}
