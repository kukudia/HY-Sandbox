using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlUnit : MonoBehaviour
{
    // Runtime aggregate for one construct: owns cockpit validity, movement input, and child component references.
    public string runtimeUnitId;
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
    public bool HasAnyCockpit => cockpits != null && cockpits.Length > 0
        ? System.Array.Exists(cockpits, candidate => candidate != null)
        : GetComponentInChildren<Cockpit>(true) != null;
    public bool IsPlayer => faction == UnitFaction.Player;
    public Vector3 MovementInput => movementInput;

    private void Start()
    {
        EnsureRuntimeUnitId();
        AssignRuntimeOwnershipToBlocks(false);

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
        // Refresh after loading or regrouping because child components and Rigidbody ownership may have changed.
        EnsureRuntimeUnitId();

        hoverFlightController = GetComponentInChildren<HoverFlightController>();
        cockpits = GetComponentsInChildren<Cockpit>(true);
        mainThrusters = GetComponentsInChildren<MainThruster>();
        hoverThrusters = GetComponentsInChildren<HoverThruster>();

        Rigidbody unitRigidbody = GetComponent<Rigidbody>();
        Thruster[] allThrusters = GetComponentsInChildren<Thruster>();
        foreach (Thruster thruster in allThrusters)
        {
            if (thruster == null) continue;

            thruster.SetRuntimeReferences(this, unitRigidbody);
        }

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

        if (hasValidCockpit && faction == UnitFaction.Enemy && GetComponent<EnemyController>() == null)
        {
            gameObject.AddComponent<EnemyController>();
        }
    }

    public void AssignRuntimeOwnershipToBlocks(bool overwriteExisting)
    {
        EnsureRuntimeUnitId();

        Block[] blocks = GetComponentsInChildren<Block>();
        foreach (Block block in blocks)
        {
            if (block == null) continue;

            RuntimeUnitMember member = block.GetComponent<RuntimeUnitMember>();
            if (!overwriteExisting && member != null && !string.IsNullOrEmpty(member.ownerUnitId))
            {
                continue;
            }

            RuntimeUnitMember.Ensure(block.gameObject, runtimeUnitId, faction);
        }
    }

    public void EnsureRuntimeUnitId()
    {
        if (string.IsNullOrEmpty(runtimeUnitId))
        {
            runtimeUnitId = System.Guid.NewGuid().ToString();
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
        if (collision.gameObject.layer == LayerMask.NameToLayer("IgnoreCollision")) return;
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

public class RuntimeUnitMember : MonoBehaviour
{
    public string ownerUnitId;
    public UnitFaction ownerFaction;

    public static RuntimeUnitMember Ensure(GameObject obj, string unitId, UnitFaction faction)
    {
        if (obj == null)
        {
            return null;
        }

        RuntimeUnitMember member = obj.GetComponent<RuntimeUnitMember>();
        if (member == null)
        {
            member = obj.AddComponent<RuntimeUnitMember>();
        }

        member.ownerUnitId = unitId;
        member.ownerFaction = faction;
        return member;
    }
}
