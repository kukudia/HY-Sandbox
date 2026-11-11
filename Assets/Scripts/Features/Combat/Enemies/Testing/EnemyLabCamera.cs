// 职责：提供试验场透视自由飞行及正交总览，暂停时仍可观察，不接管玩家相机。
// 入口：Update() / SetFreeView() / ResetOverview()；组件由制作工具预挂在Lab Camera上。
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public sealed class EnemyLabCamera : MonoBehaviour
{
    [SerializeField]
    private EnemyAvoidanceLab lab;
    [SerializeField, Min(1f)]
    private float moveSpeed = 35f;
    [SerializeField, Min(1f)]
    private float fastMultiplier = 3f;
    [SerializeField, Range(0.02f, 1f)]
    private float sensitivity = 0.15f;
    [SerializeField]
    private Vector3 overviewPosition = new Vector3(0f, 220f, -120f);
    [SerializeField]
    private Vector3 freePosition = new Vector3(-105f, 65f, -125f);
    private Camera view;
    private float yaw;
    private float pitch;
    private float nextRecord;
    private bool captured;
    private CursorLockMode previousLock;
    private bool previousVisible;
    public bool FreeView => view != null && !view.orthographic;
    public bool Looking => captured;

    private void Awake()
    {
        view = GetComponent<Camera>();
        SyncAngles();
    }

    public void SetFreeView(bool enabled)
    {
        if (view == null)
        {
            view = GetComponent<Camera>();
        }

        ReleasePointer();
        view.orthographic = !enabled;
        if (enabled)
        {
            transform.SetPositionAndRotation(freePosition, Quaternion.Euler(24f, 40f, 0f));
            view.fieldOfView = 65f;
        }
        else
        {
            transform.SetPositionAndRotation(overviewPosition, Quaternion.Euler(65f, 0f, 0f));
            view.orthographicSize = 160f;
        }

        SyncAngles();
        Record(enabled ? "camera_free" : "camera_overview");
    }

    public void ResetOverview()
    {
        SetFreeView(false);
    }

    private void SyncAngles()
    {
        yaw = transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null || !Application.isFocused)
        {
            ReleasePointer();
            return;
        }

        bool insideField = mouse.position.ReadValue().x >= 375f;
        if (FreeView && mouse.rightButton.wasPressedThisFrame && insideField)
        {
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            captured = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (!mouse.rightButton.isPressed || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
        {
            ReleasePointer();
        }

        Vector3 oldPosition = transform.position;
        Quaternion oldRotation = transform.rotation;
        if (captured)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -85f, 85f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            if (keyboard != null)
            {
                Vector3 movement = Vector3.zero;
                if (keyboard.wKey.isPressed)
                {
                    movement += transform.forward;
                }

                if (keyboard.sKey.isPressed)
                {
                    movement -= transform.forward;
                }

                if (keyboard.dKey.isPressed)
                {
                    movement += transform.right;
                }

                if (keyboard.aKey.isPressed)
                {
                    movement -= transform.right;
                }

                if (keyboard.eKey.isPressed)
                {
                    movement += Vector3.up;
                }

                if (keyboard.qKey.isPressed)
                {
                    movement -= Vector3.up;
                }

                float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? fastMultiplier : 1f);
                transform.position += Vector3.ClampMagnitude(movement, 1f) * speed * Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            }
        }
        else if (insideField)
        {
            if (mouse.middleButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                float scale = view.orthographic ? view.orthographicSize * 2f / Mathf.Max(1, Screen.height) : 0.1f;
                transform.position -= (transform.right * delta.x + transform.up * delta.y) * scale;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                if (view.orthographic)
                {
                    view.orthographicSize = Mathf.Clamp(view.orthographicSize * Mathf.Exp(-scroll * 0.001f), 15f, 240f);
                }
                else
                {
                    transform.position += transform.forward * scroll * 0.05f;
                }

                Record("camera_zoom");
            }
        }

        if ((oldPosition != transform.position || oldRotation != transform.rotation) && Time.unscaledTime >= nextRecord)
        {
            nextRecord = Time.unscaledTime + 0.1f;
            Record("camera_pose");
        }
    }

    private void Record(string action)
    {
        if (lab != null && lab.isActiveAndEnabled)
        {
            lab.RecordOperation(action, JsonUtility.ToJson(new Pose(transform.position, transform.rotation)));
        }
    }

    private void ReleasePointer()
    {
        if (!captured)
        {
            return;
        }

        captured = false;
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
    }

    private void OnDisable()
    {
        ReleasePointer();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            ReleasePointer();
        }
    }
}
