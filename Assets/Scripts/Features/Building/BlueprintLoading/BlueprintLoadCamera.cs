// 职责：根据完整蓝图的逻辑包围盒控制加载环绕镜头，避免随逐帧生成不断后退。
// 入口：Begin() / Stop()；由 BlueprintLoader 管理生命周期。
using System.Collections.Generic;
using UnityEngine;

internal sealed class BlueprintLoadCamera
{
    private BlueprintLoadSettings _settings;
    private Camera _camera;
    private CameraController loadingCameraController;
    private float loadingCameraOrbitAngle;
    private Vector3 loadingCameraOrbitCenter;
    private Bounds loadingCameraOrbitBounds;
    private bool hasLoadingCameraOrbitBounds;
    public void Begin(GameObject parent, List<BlockData> blocks, BlueprintLoadSettings settings, Camera camera)
    {
        _settings = settings;
        _camera = camera;
        loadingCameraController = GetMainCameraController();
        hasLoadingCameraOrbitBounds = TryCalculateLoadingCameraOrbitBounds(blocks, parent.transform.position, out loadingCameraOrbitBounds);
        loadingCameraOrbitCenter = hasLoadingCameraOrbitBounds ? loadingCameraOrbitBounds.center : parent.transform.position;
        loadingCameraOrbitAngle = GetCameraOrbitAngle(loadingCameraOrbitCenter);
        StartLoadingCameraOrbit(parent, blocks.Count);
    }

    public void Stop()
    {
        // Only release the controller captured by this request; do not stop unrelated camera motion.
        if (loadingCameraController != null)
        {
            loadingCameraController.StopCameraMotion();
        }

        loadingCameraController = null;
    }

    private void StartLoadingCameraOrbit(GameObject frameObject, int blockCount)
    {
        if (!_settings.MoveCamera || frameObject == null)
        {
            return;
        }

        if (blockCount < _settings.MinimumOrbitBlocks)
        {
            return;
        }

        CameraController cameraController = loadingCameraController != null ? loadingCameraController : GetMainCameraController();
        if (cameraController == null)
        {
            return;
        }

        float orbitDegreesPerSecond = CalculateLoadingCameraOrbitDegreesPerSecond(blockCount);
        if (hasLoadingCameraOrbitBounds)
        {
            // Use the complete saved layout from the first frame so loading progress cannot
            // continuously expand the renderer bounds and push the camera farther away.
            cameraController.StartContinuousOrbitCameraAroundBounds(
                loadingCameraOrbitBounds,
                loadingCameraOrbitCenter,
                loadingCameraOrbitAngle,
                orbitDegreesPerSecond,
                _settings.CameraPitch,
                _settings.CameraRadiusVariation,
                _settings.CameraRadiusWaveDegrees,
                Mathf.Max(0.01f, _settings.CameraMoveDuration));
        }
        else
        {
            cameraController.StartContinuousOrbitCameraAroundBlock(
                frameObject,
                loadingCameraOrbitCenter,
                loadingCameraOrbitAngle,
                orbitDegreesPerSecond,
                _settings.CameraPitch,
                _settings.CameraRadiusVariation,
                _settings.CameraRadiusWaveDegrees,
                Mathf.Max(0.01f, _settings.CameraMoveDuration));
        }
    }

    private float CalculateLoadingCameraOrbitDegreesPerSecond(int blockCount)
    {
        float expectedLoadDuration = Mathf.Max(_settings.CameraReferenceSecondsPerBlock * Mathf.Max(blockCount - 1, 1), 0.01f);
        return 360f / expectedLoadDuration;
    }

    private CameraController GetMainCameraController()
    {
        Camera camera = _camera != null ? _camera : Camera.main;
        return camera != null ? camera.GetComponent<CameraController>() : null;
    }

    private float GetCameraOrbitAngle(Vector3 orbitCenter)
    {
        if (loadingCameraController == null)
        {
            return 135f;
        }

        Vector3 cameraOffset = loadingCameraController.transform.position - orbitCenter;
        cameraOffset.y = 0f;
        if (cameraOffset.sqrMagnitude < 0.001f)
        {
            return 135f;
        }

        return Mathf.Atan2(cameraOffset.x, cameraOffset.z) * Mathf.Rad2Deg;
    }

    private bool TryCalculateLoadingCameraOrbitBounds(List<BlockData> blocksToLoad, Vector3 fallbackCenter, out Bounds bounds)
    {
        bounds = new Bounds(fallbackCenter, Vector3.zero);
        if (blocksToLoad == null || blocksToLoad.Count == 0)
        {
            return false;
        }

        bool hasBounds = false;
        foreach (BlockData data in blocksToLoad)
        {
            if (data == null)
            {
                continue;
            }

            Bounds blockBounds = CalculateBlockDataBounds(data);
            if (!hasBounds)
            {
                bounds = blockBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(blockBounds);
            }
        }

        return hasBounds;
    }

    private Bounds CalculateBlockDataBounds(BlockData data)
    {
        Vector3 center = new Vector3(data.posX, data.posY, data.posZ);
        Vector3 halfSize = new Vector3(
            Mathf.Max(data.x * _settings.GridSize, _settings.GridSize),
            Mathf.Max(data.y * _settings.GridSize, _settings.GridSize),
            Mathf.Max(data.z * _settings.GridSize, _settings.GridSize)) * 0.5f;
        Quaternion rotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);
        float rotationMagnitude = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
        if (rotationMagnitude > 0.0001f)
        {
            rotation = new Quaternion(
                rotation.x / rotationMagnitude,
                rotation.y / rotationMagnitude,
                rotation.z / rotationMagnitude,
                rotation.w / rotationMagnitude);
        }
        else
        {
            rotation = Quaternion.identity;
        }

        Bounds bounds = new Bounds(center, Vector3.zero);
        bool hasCorner = false;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localCorner = Vector3.Scale(halfSize, new Vector3(x, y, z));
                    Vector3 worldCorner = center + rotation * localCorner;
                    if (!hasCorner)
                    {
                        bounds = new Bounds(worldCorner, Vector3.zero);
                        hasCorner = true;
                    }
                    else
                    {
                        bounds.Encapsulate(worldCorner);
                    }
                }
            }
        }

        return bounds;
    }
}
