// 职责：基地战独占共享相机的自由观察输入，保留UI指针并在菜单/失焦/退出时释放捕获。
// 入口：Enter() / Exit() / ResetOverview()；场景预挂，右键拖看、WASD/QE飞行、Shift加速、滚轮调速、F总览。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SiegeObserverCamera : MonoBehaviour
{
    [SerializeField, Min(.1f)]
    private float moveSpeed = 25f;
    [SerializeField, Min(1f)]
    private float sprintMultiplier = 3f;
    [SerializeField, Range(.01f, 1f)]
    private float lookSensitivity = .15f;
    [SerializeField, Min(.1f)]
    private float minimumSpeed = 2f;
    [SerializeField, Min(1f)]
    private float maximumSpeed = 150f;
    [SerializeField]
    private Text hint;
    private CameraController cameraOwner;
    private MainMenuController menu;
    private Transform overview;
    private CameraMode previousMode;
    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private CursorLockMode previousLock;
    private bool previousVisible;
    private float speed;
    private float yaw;
    private float pitch;
    private bool looking;
    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
    private PointerEventData pointerEvent;
    private EventSystem pointerSystem;
    public bool IsActive => cameraOwner != null;
    public bool Looking => looking;
    public float CurrentSpeed => speed;

    public void Enter(CameraController camera, Transform overviewPose)
    {
        Exit();
        if (camera == null || overviewPose == null)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cameraOwner = camera;
        overview = overviewPose;
        menu = Object.FindAnyObjectByType<MainMenuController>();
        previousMode = camera.currentMode;
        previousPosition = camera.transform.position;
        previousRotation = camera.transform.rotation;
        camera.StopCameraMotion();
        // The legacy FreeFly handler consumes all mouse movement without a UI gate.
        // Keep that handler locked while this scene owns observation input.
        camera.currentMode = CameraMode.Lock;
        speed = Mathf.Clamp(moveSpeed, minimumSpeed, maximumSpeed);
        SyncAngles();
        RefreshHint();
    }

    public void ResetOverview()
    {
        ReleasePointer();
        if (cameraOwner == null || overview == null)
        {
            return;
        }

        cameraOwner.StopCameraMotion();
        cameraOwner.transform.SetPositionAndRotation(overview.position, overview.rotation);
        SyncAngles();
    }

    private void SyncAngles()
    {
        yaw = cameraOwner.transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0f, cameraOwner.transform.eulerAngles.x);
    }

    private void Update()
    {
        if (cameraOwner == null || PauseMenu.IsPaused)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (!Application.isFocused || mouse == null || (menu != null && (menu.Busy || menu.MenuVisible)) || IsTyping())
        {
            ReleasePointer();
            return;
        }

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            ReleasePointer();
            return;
        }

        if (keyboard != null && keyboard.fKey.wasPressedThisFrame && !looking)
        {
            ResetOverview();
        }

        if (mouse.rightButton.wasPressedThisFrame && !PointerOverInterface(mouse.position.ReadValue()))
        {
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            looking = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SyncAngles();
        }

        if (!mouse.rightButton.isPressed)
        {
            ReleasePointer();
        }

        if (!looking)
        {
            return;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            speed = Mathf.Clamp(speed * Mathf.Exp(scroll * .001f), minimumSpeed, maximumSpeed);
            RefreshHint();
        }

        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -85f, 85f);
        Transform view = cameraOwner.transform;
        view.rotation = Quaternion.Euler(pitch, yaw, 0f);
        if (keyboard == null)
        {
            return;
        }

        Vector3 movement = view.right * ((keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f)) + view.forward * ((keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)) + Vector3.up * ((keyboard.eKey.isPressed ? 1f : 0f) - (keyboard.qKey.isPressed ? 1f : 0f));
        float multiplier = keyboard.shiftKey.isPressed ? sprintMultiplier : 1f;
        view.position += Vector3.ClampMagnitude(movement, 1f) * speed * multiplier * Mathf.Min(Time.unscaledDeltaTime, .1f);
    }

    private static bool IsTyping()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && (selected.GetComponentInParent<InputField>(true) != null || selected.GetComponentInParent<TMPro.TMP_InputField>(true) != null);
    }

    private bool PointerOverInterface(Vector2 position)
    {
        EventSystem events = EventSystem.current;
        if (events == null)
        {
            return false;
        }

        // Query the press position now: cached IsPointerOverGameObject can still refer
        // to last frame's UI hit, and synthetic/hardware pointers need not share ID -1.
        if (pointerEvent == null || pointerSystem != events)
        {
            pointerSystem = events;
            pointerEvent = new PointerEventData(events);
        }

        pointerEvent.Reset();
        pointerEvent.position = position;
        pointerHits.Clear();
        events.RaycastAll(pointerEvent, pointerHits);
        foreach (RaycastResult hit in pointerHits)
        {
            if (hit.module is GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshHint()
    {
        if (hint != null)
        {
            hint.text = $"RMB + WASD/QE  |  Shift: fast  |  Wheel: {speed:0}  |  F: overview";
        }
    }

    private void ReleasePointer()
    {
        if (!looking)
        {
            return;
        }

        looking = false;
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
    }

    public void Exit()
    {
        ReleasePointer();
        if (cameraOwner != null)
        {
            cameraOwner.StopCameraMotion();
            cameraOwner.transform.SetPositionAndRotation(previousPosition, previousRotation);
            cameraOwner.currentMode = previousMode;
        }

        cameraOwner = null;
        overview = null;
        menu = null;
    }

    private void OnDisable()
    {
        Exit();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            ReleasePointer();
        }
    }
}
