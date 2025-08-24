using UnityEngine;
using UnityEngine.InputSystem; // 新输入系统命名空间

public class CameraController : MonoBehaviour
{
    public enum CameraMode { FirstPerson, FreeFly }
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

    public bool isBuilt;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // 锁定鼠标
    }

    void Update()
    {
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            isBuilt = !isBuilt;
            Cursor.lockState = isBuilt ? CursorLockMode.Confined : CursorLockMode.Locked;
        }

        HandleModeSwitch();

        if (!isBuilt)
        {
            BuildManager.instance.DeselectBlock();
            HandleLook();
            HandleMovement();
        }
    }

    void HandleModeSwitch()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            currentMode = (currentMode == CameraMode.FirstPerson) ? CameraMode.FreeFly : CameraMode.FirstPerson;
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
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        float speed;
        Vector3 move = Vector3.zero;

        if (currentMode == CameraMode.FirstPerson)
        {
            float x = (Keyboard.current.aKey.isPressed ? -1 : 0) + (Keyboard.current.dKey.isPressed ? 1 : 0);
            float z = (Keyboard.current.sKey.isPressed ? -1 : 0) + (Keyboard.current.wKey.isPressed ? 1 : 0);

            move = playerBody.right * x + playerBody.forward * z;
            speed = Keyboard.current.leftShiftKey.isPressed ? moveSpeed * sprintMultiplier : moveSpeed;
            playerBody.position += move * speed * Time.deltaTime;
        }
        else if (currentMode == CameraMode.FreeFly)
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
