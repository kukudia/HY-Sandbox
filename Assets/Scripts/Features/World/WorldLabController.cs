// 职责：独立世界实验场的自由镜头、采掘/爆破/填补输入及性能读数。
// 入口：Update()读取Input System；不接管Main场景的玩家、蓝图与存档。
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

[RequireComponent(typeof(Camera))]
public sealed class WorldLabController : MonoBehaviour
{
    [SerializeField]
    private ProceduralVoxelWorld world;
    [SerializeField, Min(1f)]
    private float flySpeed = 18f;
    [SerializeField, Min(1f)]
    private float interactionDistance = 180f;
    [SerializeField, Range(1f, 12f)]
    private float blastRadius = 6f;
    private Camera view;
    private float yaw;
    private float pitch;
    private float nextMineAt;
    private float lastActionMilliseconds;
    private Vector3 home;
    private Quaternion homeRotation;
    private GUIStyle textStyle;
    private bool previousBackground;
    private int previousFrameRate;
    private string fileMessage = "F5 save edits / F9 load edits";
    private void Awake()
    {
        view = GetComponent<Camera>();
        home = transform.position;
        homeRotation = transform.rotation;
        ResetView();
    }

    private void OnEnable()
    {
        previousBackground = Application.runInBackground;
        previousFrameRate = Application.targetFrameRate;
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
    }

    private void OnDisable()
    {
        Application.runInBackground = previousBackground;
        Application.targetFrameRate = previousFrameRate;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null || world == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        MoveView(keyboard, mouse);
        if (keyboard.f5Key.wasPressedThisFrame || keyboard.f9Key.wasPressedThisFrame)
        {
            TransferEdits(keyboard.f5Key.wasPressedThisFrame);
        }

        if (keyboard.fKey.wasPressedThisFrame)
        {
            ResetView();
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            world.Generate();
        }

        bool overHud = mouse.position.ReadValue().x < 620 && mouse.position.ReadValue().y > Screen.height - 220;
        if (overHud && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        bool mine = mouse.leftButton.isPressed && Time.unscaledTime >= nextMineAt;
        bool blast = keyboard.eKey.wasPressedThisFrame;
        bool fill = mouse.middleButton.wasPressedThisFrame;
        if (!mine && !blast && !fill)
        {
            return;
        }

        Vector3 screen = Cursor.lockState == CursorLockMode.Locked ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f) : (Vector3)mouse.position.ReadValue();
        if (!Physics.Raycast(
            view.ScreenPointToRay(screen),
            out RaycastHit hit,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) || hit.collider.GetComponent<VoxelWorldChunk>() == null)
        {
            return;
        }

        float began = Time.realtimeSinceStartup;
        if (fill)
        {
            world.SetCell(world.WorldToCell(hit.point + hit.normal * world.CellSize * 0.1f), 3);
        }
        else
        {
            Vector3 center = hit.point - hit.normal * world.CellSize * 0.1f;
            world.DamageSphere(center, blast ? blastRadius : world.CellSize * 0.85f, blast ? 500f : 140f);
            nextMineAt = Time.unscaledTime + 0.15f;
        }

        lastActionMilliseconds = (Time.realtimeSinceStartup - began) * 1000f;
    }

    private void MoveView(Keyboard keyboard, Mouse mouse)
    {
        bool looking = mouse.rightButton.isPressed;
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !looking;
        if (looking)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * 0.12f;
            pitch = Mathf.Clamp(pitch - delta.y * 0.12f, -85f, 85f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        Vector3 move = Vector3.zero;
        move.z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
        move.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
        move.y = (keyboard.spaceKey.isPressed ? 1 : 0) - (keyboard.leftCtrlKey.isPressed ? 1 : 0);
        float speed = flySpeed * (keyboard.leftShiftKey.isPressed ? 3f : 1f);
        transform.position += transform.TransformDirection(Vector3.ClampMagnitude(move, 1f)) * speed * Time.unscaledDeltaTime;
    }

    private void ResetView()
    {
        transform.SetPositionAndRotation(home, homeRotation);
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        if (pitch > 180f)
        {
            pitch -= 360f;
        }
    }

    private void TransferEdits(bool save)
    {
        string path = Path.Combine(Application.persistentDataPath, "WorldLab", "seed-" + world.Seed + ".json");
        try
        {
            if (save)
            {
                VoxelWorldEdits edits = world.CaptureEdits();
                WorldLabPersistence.Save(path, edits);
                fileMessage = "Saved " + edits.edits.Length + " edits · " + Path.GetFileName(path);
            }
            else
            {
                world.RestoreEdits(WorldLabPersistence.Load(path));
                fileMessage = "Loaded edits · chunks rebuilding";
            }
        }
        catch (System.Exception error) when (error is IOException || error is System.ArgumentException || error is System.InvalidOperationException || error is System.UnauthorizedAccessException)
        {
            fileMessage = "Save/load failed: " + error.Message;
        }
    }

    private void OnGUI()
    {
        if (world == null)
        {
            return;
        }

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                richText = true
            };
            textStyle.normal.textColor = new Color(0.8f, 0.95f, 1f);
        }

        GUI.Box(new Rect(18, 18, 590, 210), GUIContent.none);
        GUI.Label(
            new Rect(34, 28, 560, 195),
            "<b>WORLD LAB / PROCEDURAL DESTRUCTION</b>\n" + "RMB look · WASD fly · Space/Ctrl up/down · Shift sprint\n" + "LMB mine · E blast · MMB place metal · R regenerate · F home\n" + $"Seed {world.Seed}  |  solids {world.Data.SolidCount:N0}  |  removed {world.RemovedCells}\n" + $"Dirty chunks {world.PendingChunks}  |  edit {lastActionMilliseconds:0.00} ms (mesh rebuild excluded)\n" + fileMessage + "\nSmall metal debris only · F5 save / F9 load · No streaming",
            textStyle);
        GUI.Label(new Rect(Screen.width * 0.5f - 8, Screen.height * 0.5f - 12, 24, 24), "+", textStyle);
    }
}
