using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // 新输入系统命名空间

public class CameraController : MonoBehaviour
{
    public CameraMode currentMode = CameraMode.FirstPerson;

    [Header("General Settings")]
    public float mouseSensitivity = 2f;
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;

    [Header("First Person Settings")]
    public Transform playerBody;   // 角色身体（通常是一个胶囊体）
    private float xRotation = 0f;

    [Header("Free Fly Settings")]
    public float freeFlySpeed = 10f;

    [Header("Third Person Settings")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 2f, -5f);
    public float thirdPersonSmooth = 10f;
    private float tpYaw = 0f;
    private float tpPitch = 15f; // 稍微俯视
    private Vector3 camVelocity = Vector3.zero;

    [Header("Third Person Zoom")]
    public float zoomSpeed = 2f;
    public float minZoom = -2f;   // 最近
    public float maxZoom = -10f;  // 最远
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
            // 在三种模式之间循环切换
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

            // 旋转控制
            tpYaw += mouseX;
            tpPitch -= mouseY;
            tpPitch = Mathf.Clamp(tpPitch, -30f, 60f);

            // 滚轮缩放
            float scroll = Mouse.current.scroll.ReadValue().y * zoomSpeed * Time.deltaTime;
            thirdPersonOffset.z = Mathf.Clamp(thirdPersonOffset.z + scroll, maxZoom, minZoom);

            // 计算相机位置
            Quaternion rotation = Quaternion.Euler(tpPitch, tpYaw, 0f);
            Vector3 desiredPos = playerBody.position + rotation * thirdPersonOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPos,
                ref camVelocity,
                0.05f // 平滑时间，数值越小越跟随紧
            );
            transform.LookAt(playerBody.position + Vector3.up * 1.5f); // 看向角色头部位置
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

    // 新增：摄像机聚焦方法
    public void FocusCameraOnBlock(GameObject obj)
    {
        if (obj == null) return;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }

        if (!TryGetFocusPose(obj, out Vector3 targetPosition, out Quaternion targetRotation)) return;

        // 设置摄像机位置和旋转
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
        if (frameObj == null) return;
        if (!TryCalculateBlockBounds(frameObj, out Bounds frameBounds)) return;
        if (!TryGetOrbitPose(frameBounds, yawDegrees, pitchDegrees, out Vector3 targetPosition, out Quaternion targetRotation)) return;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
        }

        focusCoroutine = StartCoroutine(SmoothFocusRoutine(targetPosition, targetRotation, duration));
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

    private bool TryGetFocusPose(GameObject obj, out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        // 获取方块的包围盒
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

    private bool TryGetOrbitPose(Bounds frameBounds, float yawDegrees, float pitchDegrees, out Vector3 targetPosition, out Quaternion targetRotation)
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

    // 新增：计算方块包围盒
    private bool TryCalculateBlockBounds(GameObject obj, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        Block block = obj.GetComponent<Block>();
        if (block != null)
        {
            //使用默认尺寸估算
            Vector3 size = new Vector3(block.x, block.y, block.z) * BuildManager.instance.gridSize;
            bounds = new Bounds(block.transform.position, size);
            return true;
        }

        if (renderers.Length == 0) return false;

        // 计算所有渲染器的总包围盒
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
