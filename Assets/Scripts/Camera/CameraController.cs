using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public CameraMode currentMode = CameraMode.FirstPerson;

    [Header("General Settings")]
    public float mouseSensitivity = 2f;
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;

    [Header("First Person Settings")]
    public Transform playerBody;
    private float xRotation = 0f;

    [Header("Free Fly Settings")]
    public float freeFlySpeed = 10f;

    [Header("Third Person Settings")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 2f, -5f);
    public float thirdPersonSmooth = 10f;
    private float tpYaw = 0f;
    private float tpPitch = 15f;
    private Vector3 camVelocity = Vector3.zero;

    [Header("Third Person Zoom")]
    public float zoomSpeed = 2f;
    public float minZoom = -2f;
    public float maxZoom = -10f;
    private Coroutine focusCoroutine;

    void Update()
    {
        HandleModeSwitch();

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (BuildManager.instance?.selectedBlock !=  null)
            {
                FocusCameraOnBlock(BuildManager.instance.selectedBlock.gameObject);
            }
            else
            {
                FocusCameraOnBlock(PlayManager.instance.blocksParent.gameObject);
            }
        }

        if (!BuildManager.instance.lockView && !PlayManager.instance.playMode)
        {
            currentMode = CameraMode.FreeFly;
        }
        else if (PlayManager.instance.playMode)
        {
            currentMode = CameraMode.ThirdPerson;
        }
        else
        {
            currentMode = CameraMode.Lock;
        }
    }

    private void LateUpdate()
    {
        if (currentMode != CameraMode.Lock)
        {
            HandleLook();
            HandleMovement();
        }
    }

    void HandleModeSwitch()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // ������ģʽ֮��ѭ���л�
            if (currentMode == CameraMode.FirstPerson)
                currentMode = CameraMode.ThirdPerson;
            else if (currentMode == CameraMode.ThirdPerson)
                currentMode = CameraMode.FreeFly;
            else
                currentMode = CameraMode.FirstPerson;
        }
    }

    void HandleLook()
    {
        if (Mouse.current == null) return;

        float mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime * 100f;
        float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime * 100f;

        if (currentMode == CameraMode.FirstPerson)
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }
        else if (currentMode == CameraMode.FreeFly)
        {
            transform.Rotate(Vector3.up * mouseX, Space.World);
            transform.Rotate(Vector3.right * -mouseY, Space.Self);
        }
        else if (currentMode == CameraMode.ThirdPerson)
        {
            if (playerBody == null) return;

            tpYaw += mouseX;
            tpPitch -= mouseY;
            tpPitch = Mathf.Clamp(tpPitch, -30f, 60f);

            float scroll = Mouse.current.scroll.ReadValue().y * zoomSpeed * Time.deltaTime;
            thirdPersonOffset.z = Mathf.Clamp(thirdPersonOffset.z + scroll, maxZoom, minZoom);

            Quaternion rotation = Quaternion.Euler(tpPitch, tpYaw, 0f);
            Vector3 desiredPos = playerBody.position + rotation * thirdPersonOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPos,
                ref camVelocity,
                0.05f // ƽ��ʱ�䣬��ֵԽСԽ�����
            );
            transform.LookAt(playerBody.position + Vector3.up * 1.5f); // �����ɫͷ��λ��
        }
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        float speed;
        Vector3 move = Vector3.zero;

        if (currentMode == CameraMode.FreeFly)
        {
            float x = (Keyboard.current.aKey.isPressed ? -1 : 0) + (Keyboard.current.dKey.isPressed ? 1 : 0);
            float z = (Keyboard.current.sKey.isPressed ? -1 : 0) + (Keyboard.current.wKey.isPressed ? 1 : 0);
            float y = (Keyboard.current.qKey.isPressed ? -1 : 0) + (Keyboard.current.eKey.isPressed ? 1 : 0);

            move = transform.right * x + transform.forward * z + transform.up * y;
            speed = Keyboard.current.leftShiftKey.isPressed ? freeFlySpeed * sprintMultiplier : freeFlySpeed;
            transform.position += move * speed * Time.deltaTime;
        }
    }

    public void FocusCameraOnBlock(GameObject obj)
    {
        if (obj == null) return;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }

        if (!TryGetFocusPose(obj, out Vector3 targetPosition, out Quaternion targetRotation)) return;

        // ���������λ�ú���ת
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    public void SmoothFocusCameraOnBlock(GameObject obj, float duration)
    {
        if (obj == null) return;
        if (!TryGetFocusPose(obj, out Vector3 targetPosition, out Quaternion targetRotation)) return;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
        }

        focusCoroutine = StartCoroutine(SmoothFocusRoutine(targetPosition, targetRotation, duration));
    }

    public void SmoothFocusCameraOnBlockFramedBy(GameObject lookObj, GameObject frameObj, float duration)
    {
        if (lookObj == null || frameObj == null) return;
        if (!TryCalculateBlockBounds(lookObj, out Bounds lookBounds)) return;
        if (!TryCalculateBlockBounds(frameObj, out Bounds frameBounds)) return;
        if (!TryGetFocusPose(frameBounds, lookBounds.center, out Vector3 targetPosition, out Quaternion targetRotation)) return;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
        }

        focusCoroutine = StartCoroutine(SmoothFocusRoutine(targetPosition, targetRotation, duration));
    }

    public void SmoothOrbitCameraAroundBlock(GameObject frameObj, float yawDegrees, float pitchDegrees, float duration)
    {
        SmoothOrbitCameraAroundBlock(frameObj, yawDegrees, pitchDegrees, 1f, duration);
    }

    public void SmoothOrbitCameraAroundBlock(GameObject frameObj, float yawDegrees, float pitchDegrees, float radiusMultiplier, float duration)
    {
        if (frameObj == null) return;
        if (!TryCalculateBlockBounds(frameObj, out Bounds frameBounds)) return;

        SmoothOrbitCameraAroundBlock(frameObj, frameBounds.center, yawDegrees, pitchDegrees, radiusMultiplier, duration);
    }

    public void SmoothOrbitCameraAroundBlock(GameObject frameObj, Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radiusMultiplier, float duration)
    {
        if (frameObj == null) return;
        if (!TryCalculateBlockBounds(frameObj, out Bounds frameBounds)) return;

        float targetRadius = CalculateFramingDistance(frameBounds, orbitCenter) * Mathf.Max(radiusMultiplier, 0.1f);
        float targetPitch = Mathf.Clamp(pitchDegrees, -75f, 75f);

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
        }

        focusCoroutine = StartCoroutine(SmoothOrbitRoutine(orbitCenter, yawDegrees, targetPitch, targetRadius, duration));
    }

    public void StartContinuousOrbitCameraAroundBlock(GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)
    {
        if (frameObj == null) return;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
        }

        focusCoroutine = StartCoroutine(ContinuousOrbitRoutine(
            frameObj,
            orbitCenter,
            startYawDegrees,
            orbitDegreesPerSecond,
            Mathf.Clamp(pitchDegrees, -75f, 75f),
            Mathf.Max(0f, radiusVariation),
            Mathf.Max(1f, radiusWaveDegrees),
            Mathf.Max(0.01f, radiusSmoothTime)
        ));
    }

    public void StopCameraMotion()
    {
        if (focusCoroutine == null) return;

        StopCoroutine(focusCoroutine);
        focusCoroutine = null;
    }

    private IEnumerator SmoothFocusRoutine(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        if (duration <= 0f)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            focusCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        focusCoroutine = null;
    }

    private IEnumerator ContinuousOrbitRoutine(GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)
    {
        float yaw = startYawDegrees;
        float currentRadius = 0f;
        float radiusVelocity = 0f;
        bool hasRadius = false;

        while (frameObj != null)
        {
            if (TryCalculateBlockBounds(frameObj, out Bounds frameBounds))
            {
                yaw += orbitDegreesPerSecond * Time.deltaTime;
                float radiusMultiplier = CalculateOrbitRadiusMultiplier(yaw, radiusVariation, radiusWaveDegrees);
                float targetRadius = CalculateFramingDistance(frameBounds, orbitCenter) * radiusMultiplier;

                if (!hasRadius)
                {
                    currentRadius = targetRadius;
                    hasRadius = true;
                }
                else
                {
                    currentRadius = Mathf.SmoothDamp(currentRadius, targetRadius, ref radiusVelocity, radiusSmoothTime);
                }

                SetOrbitPose(orbitCenter, yaw, pitchDegrees, currentRadius);
            }

            yield return null;
        }

        focusCoroutine = null;
    }

    private float CalculateOrbitRadiusMultiplier(float yawDegrees, float radiusVariation, float radiusWaveDegrees)
    {
        if (radiusVariation <= 0f) return 1f;

        float phase = yawDegrees / radiusWaveDegrees * Mathf.PI * 2f;
        float wave = (Mathf.Sin(phase) + 1f) * 0.5f;
        return 1f + wave * radiusVariation;
    }

    private IEnumerator SmoothOrbitRoutine(Vector3 orbitCenter, float targetYaw, float targetPitch, float targetRadius, float duration)
    {
        Vector3 startOffset = transform.position - orbitCenter;
        float startRadius = startOffset.magnitude;
        float startYaw = targetYaw;
        float startPitch = targetPitch;

        if (startRadius > 0.001f)
        {
            startYaw = Mathf.Atan2(startOffset.x, startOffset.z) * Mathf.Rad2Deg;
            float horizontalDistance = new Vector2(startOffset.x, startOffset.z).magnitude;
            startPitch = Mathf.Atan2(startOffset.y, horizontalDistance) * Mathf.Rad2Deg;
        }
        else
        {
            startRadius = targetRadius;
        }

        if (duration <= 0f)
        {
            SetOrbitPose(orbitCenter, targetYaw, targetPitch, targetRadius);
            focusCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            float yaw = Mathf.LerpAngle(startYaw, targetYaw, t);
            float pitch = Mathf.Lerp(startPitch, targetPitch, t);
            float radius = Mathf.Lerp(startRadius, targetRadius, t);
            SetOrbitPose(orbitCenter, yaw, pitch, radius);
            yield return null;
        }

        SetOrbitPose(orbitCenter, targetYaw, targetPitch, targetRadius);
        focusCoroutine = null;
    }

    private void SetOrbitPose(Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radius)
    {
        float yaw = yawDegrees * Mathf.Deg2Rad;
        float pitch = Mathf.Clamp(pitchDegrees, -75f, 75f) * Mathf.Deg2Rad;
        float cosPitch = Mathf.Cos(pitch);
        Vector3 cameraDirection = new Vector3(
            Mathf.Sin(yaw) * cosPitch,
            Mathf.Sin(pitch),
            Mathf.Cos(yaw) * cosPitch
        ).normalized;

        transform.position = orbitCenter + cameraDirection * Mathf.Max(radius, 0.1f);
        Vector3 lookDirection = orbitCenter - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private bool TryGetFocusPose(GameObject obj, out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        if (!TryCalculateBlockBounds(obj, out Bounds bounds)) return false;
        return TryGetFocusPose(bounds, bounds.center, out targetPosition, out targetRotation);
    }

    private bool TryGetFocusPose(Bounds frameBounds, Vector3 lookPoint, out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        Vector3 cameraDirection = transform.position - lookPoint;
        if (frameBounds.Contains(transform.position) || cameraDirection.sqrMagnitude < 0.001f)
        {
            cameraDirection = new Vector3(1f, 0.6f, -1f);
        }

        cameraDirection.Normalize();
        float distance = CalculateFramingDistance(frameBounds, lookPoint);
        targetPosition = lookPoint + cameraDirection * distance;

        Vector3 lookDirection = lookPoint - targetPosition;
        if (lookDirection.sqrMagnitude < 0.001f) return false;

        targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        return true;
    }

    private bool TryGetOrbitPose(Bounds frameBounds, float yawDegrees, float pitchDegrees, float radiusMultiplier, out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        Vector3 lookPoint = frameBounds.center;
        float yaw = yawDegrees * Mathf.Deg2Rad;
        float pitch = Mathf.Clamp(pitchDegrees, -75f, 75f) * Mathf.Deg2Rad;
        float cosPitch = Mathf.Cos(pitch);
        Vector3 cameraDirection = new Vector3(
            Mathf.Sin(yaw) * cosPitch,
            Mathf.Sin(pitch),
            Mathf.Cos(yaw) * cosPitch
        ).normalized;

        float distance = CalculateFramingDistance(frameBounds, lookPoint);
        distance *= Mathf.Max(radiusMultiplier, 0.1f);
        targetPosition = lookPoint + cameraDirection * distance;

        Vector3 lookDirection = lookPoint - targetPosition;
        if (lookDirection.sqrMagnitude < 0.001f) return false;

        targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        return true;
    }

    private float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint)
    {
        Vector3 extents = bounds.extents;
        Vector3 center = bounds.center;
        float radius = 0f;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    radius = Mathf.Max(radius, Vector3.Distance(lookPoint, corner));
                }
            }
        }

        Camera cameraComponent = GetComponent<Camera>();
        float verticalFov = cameraComponent != null ? cameraComponent.fieldOfView : 60f;
        float aspect = cameraComponent != null ? cameraComponent.aspect : 16f / 9f;
        float horizontalFov = Mathf.Rad2Deg * 2f * Mathf.Atan(Mathf.Tan(verticalFov * Mathf.Deg2Rad * 0.5f) * aspect);
        float fitFov = Mathf.Min(verticalFov, horizontalFov) * Mathf.Deg2Rad;
        float distance = radius / Mathf.Sin(Mathf.Max(fitFov * 0.5f, 0.01f));

        return Mathf.Max(distance * 1.15f, radius + 1f, 1f);
    }

    private bool TryCalculateBlockBounds(GameObject obj, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        Block block = obj.GetComponent<Block>();
        if (block != null)
        {
            Vector3 size = new Vector3(block.x, block.y, block.z) * BuildManager.instance.gridSize;
            bounds = new Bounds(block.transform.position, size);
            return true;
        }

        if (renderers.Length == 0) return false;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return true;
    }
}

public enum CameraMode
{
    FirstPerson,
    ThirdPerson,
    FreeFly,
    Lock
}
