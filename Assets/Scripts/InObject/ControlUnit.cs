using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlUnit : MonoBehaviour
{
    public HoverFlightController hoverFlightController;
    public Cockpit cockpit;
    public Cockpit[] cockpits;
    public MainThruster[] mainThrusters;
    public HoverThruster[] hoverThrusters;
    public UnitFaction faction = UnitFaction.Player;
    public bool hasValidCockpit;
    public Vector3 movementInput;
    public Transform target;

    private float cooldownTime = 0.2f;
    private bool _isOnCooldown;

    public bool HasValidCockpit => hasValidCockpit && cockpit != null;
    public bool IsPlayer => faction == UnitFaction.Player;
    public Vector3 MovementInput => movementInput;

    private void Start()
    {
        if (PlayManager.instance != null)
        {
            PlayManager.instance.RegisterControlUnit(this);
        }

        Invoke(nameof(RefreshChildren), 2f);
    }

    private void Update()
    {
        if (!PlayManager.instance.playMode) return;

        if (transform.childCount == 0)
        {
            Destroy(gameObject);
            return;
        }

        if (!HasValidCockpit)
        {
            movementInput = Vector3.zero;
            return;
        }

        if (IsPlayer)
        {
            movementInput = GetPlayerMovementInput();
        }

        if (IsPlayer && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach (HoverThruster thruster in hoverThrusters)
            {
                thruster.isHovered = !thruster.isHovered;
            }

            if (hoverFlightController != null)
            {
                if (!hoverFlightController.setHeight)
                {
                    hoverFlightController.targetHoverHeight = (int)transform.position.y + 10;
                    hoverFlightController.setHeight = true;
                }
                else
                {
                    hoverFlightController.targetHoverHeight = 0;
                    hoverFlightController.setHeight = false;
                }
            }
        }
    }

    public void RefreshChildren()
    {
        hoverFlightController = GetComponentInChildren<HoverFlightController>();
        cockpits = GetComponentsInChildren<Cockpit>(true);
        mainThrusters = GetComponentsInChildren<MainThruster>();
        hoverThrusters = GetComponentsInChildren<HoverThruster>();

        hasValidCockpit = cockpits.Length == 1;
        cockpit = hasValidCockpit ? cockpits[0] : null;

        if (hasValidCockpit)
        {
            faction = cockpit.faction;
        }
        else
        {
            movementInput = Vector3.zero;
            //Debug.LogWarning($"{name} requires exactly one Cockpit, found {cockpits.Length}.");
        }

        if (hoverFlightController == null)
        {
            //Debug.LogWarning($"Cannot find HoverFlightController in {gameObject}");
        }

        if (hoverFlightController != null && hoverThrusters.Length > 0 && hasValidCockpit)
        {
            hoverFlightController.thrusters = hoverThrusters;
            hoverFlightController.enabled = true;
            hoverFlightController.showUI = true;
            hoverFlightController.Init();
        }

        if (hasValidCockpit && faction == UnitFaction.Enemy && GetComponent<ModularEnemyController>() == null)
        {
            gameObject.AddComponent<ModularEnemyController>();
        }
    }

    public void PlayEnd()
    {
        if (hoverFlightController != null)
        {
            hoverFlightController.enabled = false;
        }

        movementInput = Vector3.zero;
    }

    public void SetMovementInput(Vector3 worldDirection)
    {
        movementInput = Vector3.ClampMagnitude(worldDirection, 1f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private Vector3 GetPlayerMovementInput()
    {
        Vector3 dir = Vector3.zero;
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform == null) return dir;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        if (Keyboard.current.wKey.isPressed) dir += camForward;
        if (Keyboard.current.sKey.isPressed) dir -= camForward;
        if (Keyboard.current.aKey.isPressed) dir -= camRight;
        if (Keyboard.current.dKey.isPressed) dir += camRight;

        return Vector3.ClampMagnitude(dir, 1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!PlayManager.instance.playMode) return;
        if (_isOnCooldown) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            Collider thisCollider = contact.thisCollider;
            var childHandler = thisCollider.GetComponent<Durability>();
            if (childHandler != null)
            {
                childHandler.CollisionEnter(collision);
            }
        }

        StartCoroutine(StartCooldown());
    }

    private void OnDestroy()
    {
        if (PlayManager.instance != null)
        {
            PlayManager.instance.UnregisterControlUnit(this);
        }
    }

    private IEnumerator StartCooldown()
    {
        _isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        _isOnCooldown = false;
    }
}
