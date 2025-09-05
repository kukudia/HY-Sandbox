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

    void Update()
    {
        HandleModeSwitch();

        if (!BuildManager.instance.buildMode)
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
}

public enum CameraMode
{
    FirstPerson,
    ThirdPerson,
    FreeFly,
    Lock
}
