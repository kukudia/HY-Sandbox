// 职责：管理叠加镜头偏移及协程，取消或销毁宿主时移除残留偏移。
// 入口：Play() / Cancel()；显式注入宿主与目标 Camera。
using System.Collections;
using UnityEngine;

internal sealed class CameraShake
{
    private readonly MonoBehaviour _runner;
    private Coroutine cameraShakeRoutine;
    private Transform shakenCamera;
    private Vector3 activeCameraOffset;
    public CameraShake(MonoBehaviour runner)
    {
        _runner = runner;
    }

    public void Cancel()
    {
        if (cameraShakeRoutine != null && _runner != null)
        {
            _runner.StopCoroutine(cameraShakeRoutine);
        }

        ClearCameraOffset();
        cameraShakeRoutine = null;
        shakenCamera = null;
    }

    public void Play(Camera camera, float amplitude, float duration)
    {
        if (_runner == null || !_runner.isActiveAndEnabled)
        {
            Cancel();
            return;
        }

        if (amplitude <= 0f || duration <= 0f)
        {
            return;
        }

        if (camera == null)
        {
            return;
        }

        if (cameraShakeRoutine != null)
        {
            _runner.StopCoroutine(cameraShakeRoutine);
            ClearCameraOffset();
        }

        shakenCamera = camera.transform;
        cameraShakeRoutine = _runner.StartCoroutine(CameraShakeRoutine(amplitude, duration));
    }

    private IEnumerator CameraShakeRoutine(float amplitude, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && shakenCamera != null)
        {
            ClearCameraOffset();
            float falloff = 1f - elapsed / duration;
            activeCameraOffset = Random.insideUnitSphere * (amplitude * falloff);
            activeCameraOffset.z *= 0.35f;
            shakenCamera.localPosition += activeCameraOffset;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ClearCameraOffset();
        cameraShakeRoutine = null;
        shakenCamera = null;
    }

    private void ClearCameraOffset()
    {
        if (shakenCamera != null)
        {
            shakenCamera.localPosition -= activeCameraOffset;
        }

        activeCameraOffset = Vector3.zero;
    }
}
