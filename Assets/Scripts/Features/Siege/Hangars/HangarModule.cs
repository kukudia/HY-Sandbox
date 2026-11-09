// 职责：按方块存档尺寸维护机库外壳、净空、泊位和安全滑门，不保存战斗损伤到设计。
// 入口：RefreshGeometry() / TryResize() / Fits() / GetDockPose() / SetCombat() / SetDoorOpen()。
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Block))]
public sealed class HangarModule : MonoBehaviour
{
    [SerializeField]
    private Transform floor;
    [SerializeField]
    private Transform roof;
    [SerializeField]
    private Transform leftWall;
    [SerializeField]
    private Transform rightWall;
    [SerializeField]
    private Transform backWall;
    [SerializeField]
    private Transform leftDoor;
    [SerializeField]
    private Transform rightDoor;
    [SerializeField]
    private Transform capacity;
    [SerializeField]
    private Transform sizeLabel;
    [SerializeField]
    private Text sizeText;
    [SerializeField]
    private HangarFacePanel[] faces;
    [SerializeField, Min(0.1f)]
    private float doorSeconds = 1.2f;
    private Block block;
    private bool combat;
    private bool doorRequested;
    private float doorFraction;
    private Vector3Int connectionSize;
    private readonly Collider[] doorwayOccupants = new Collider[64];
    public Vector3 InnerSize
    {
        get
        {
            Block current = block != null ? block : GetComponent<Block>();
            return new Vector3(Mathf.Max(1, current.x - 2), Mathf.Max(1, current.y - 1), Mathf.Max(1, current.z - 2));
        }
    }

    public bool DoorIsOpen => doorFraction >= 0.99f;
    public bool IsOperational => isActiveAndEnabled && TryGetComponent(out Durability health) && health.currentDurability > 0f;
    public bool IsCombat => combat;
    public float DoorFraction => doorFraction;

    private void OnEnable()
    {
        block = GetComponent<Block>();
        RefreshGeometry();
        // 恢复建筑组件时自动使用战斗展示；独立预研泊位由会话显式切换。
        if (GetComponentInParent<BuildingRuntime>() != null)
        {
            SetCombat(true);
        }
    }

    public void RefreshGeometry()
    {
        block = GetComponent<Block>();
        block.x = Mathf.Clamp(block.x, 3, 64);
        block.y = Mathf.Clamp(block.y, 2, 64);
        block.z = Mathf.Clamp(block.z, 3, 64);
        Vector3 outer = new Vector3(block.x, block.y, block.z);
        Vector3 inner = InnerSize;
        SetShape(floor, new Vector3(0f, -outer.y * 0.5f + 0.25f, 0f), new Vector3(outer.x, 0.5f, outer.z));
        SetShape(roof, new Vector3(0f, outer.y * 0.5f - 0.25f, 0f), new Vector3(outer.x, 0.5f, outer.z));
        SetShape(leftWall, new Vector3(-(outer.x - 1f) * 0.5f, 0f, 0f), new Vector3(1f, inner.y, outer.z));
        SetShape(rightWall, new Vector3((outer.x - 1f) * 0.5f, 0f, 0f), new Vector3(1f, inner.y, outer.z));
        SetShape(backWall, new Vector3(0f, 0f, -(outer.z - 1f) * 0.5f), new Vector3(inner.x, inner.y, 1f));
        SetShape(capacity, Vector3.zero, inner);
        if (sizeLabel != null)
        {
            sizeLabel.localPosition = Vector3.zero;
        }

        if (sizeText != null)
        {
            sizeText.text = $"HANGAR\n{inner.x:0} × {inner.y:0} × {inner.z:0} m\nWIDTH · HEIGHT · DEPTH";
        }

        if (faces != null)
        {
            foreach (HangarFacePanel face in faces)
            {
                face.ConfigureGeometry(outer);
            }
        }

        block.canSpawnConnector = false;
        block.connectorParent = transform;
        // 只有底板向下连接，避免把净空或可开合的门当成承重连接面。
        Vector3Int dimensions = new Vector3Int(block.x, block.y, block.z);
        if (connectionSize != dimensions || block.connectors.Count != block.x * block.z)
        {
            block.connectors.Clear();
            connectionSize = dimensions;
            for (int x = 0; x < block.x; x++)
            {
                for (int z = 0; z < block.z; z++)
                {
                    block.connectors.Add(new Connector { name = $"Down {x * block.z + z + 1}", canConnect = true, connectType = ConnectType.Down, localPos = new Vector3(x - (block.x - 1) * 0.5f, -block.y * 0.5f, z - (block.z - 1) * 0.5f), normal = Vector3.down, order = x * block.z + z + 1 });
                }
            }
        }

        if (block.raycastCollider != null)
        {
            block.raycastCollider.size = outer - Vector3.one * 0.1f;
            block.raycastCollider.enabled = !combat;
        }

        ApplyDoorPose();
        ApplyPresentation();
    }

    public bool IsCapacityRenderer(Renderer renderer)
    {
        return capacity != null && renderer != null && renderer.transform == capacity;
    }

    public bool Fits(BlockDataList blueprint)
    {
        if (blueprint?.blocks == null || blueprint.blocks.Count == 0)
        {
            return false;
        }

        Vector3 size = BlockBounds.FromBlueprint(blueprint.blocks, Matrix4x4.identity).size;
        Vector3 inner = InnerSize;
        return size.x <= inner.x + 0.001f && size.y <= inner.y + 0.001f && size.z <= inner.z + 0.001f;
    }

    public Pose GetDockPose(BlockDataList blueprint)
    {
        Bounds bounds = BlockBounds.FromBlueprint(blueprint.blocks, Matrix4x4.identity);
        // 蓝图原点可以远离船体；以实际包围盒居中，船底与底板留出碰撞体的内缩间隙。
        Vector3 local = new Vector3(-bounds.center.x, -InnerSize.y * 0.5f - bounds.min.y, -bounds.center.z);
        return new Pose(transform.TransformPoint(local), transform.rotation);
    }

    public void SetCombat(bool active)
    {
        combat = active;
        if (block == null)
        {
            block = GetComponent<Block>();
        }

        if (block.raycastCollider != null)
        {
            block.raycastCollider.enabled = !active;
        }

        ApplyPresentation();
    }

    private void ApplyPresentation()
    {
        if (capacity != null)
        {
            capacity.gameObject.SetActive(!combat);
        }

        if (sizeLabel != null)
        {
            sizeLabel.gameObject.SetActive(!combat);
        }

        Transform[] shell =
        {
            roof,
            leftWall,
            rightWall,
            backWall,
            leftDoor,
            rightDoor
        };
        foreach (Transform piece in shell)
        {
            if (piece != null && piece.TryGetComponent(out Renderer renderer))
            {
                renderer.enabled = combat;
            }
        }
    }

    public void SetDoorOpen(bool open)
    {
        doorRequested = open;
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        bool safeToClose = doorFraction <= 0f || IsDoorwayClear();
        float target = doorRequested || !safeToClose ? 1f : 0f;
        doorFraction = Mathf.MoveTowards(doorFraction, target, Time.fixedDeltaTime / Mathf.Max(0.1f, doorSeconds));
        ApplyDoorPose();
    }

    private void LateUpdate()
    {
        if (!combat && sizeLabel != null && Camera.main != null)
        {
            sizeLabel.rotation = Camera.main.transform.rotation;
        }
    }

    private bool IsDoorwayClear()
    {
        Vector3 inner = InnerSize;
        Vector3 center = transform.TransformPoint(new Vector3(0f, 0f, (block.z - 1f) * 0.5f));
        int count = Physics.OverlapBoxNonAlloc(
            center,
            new Vector3(inner.x * 0.5f, inner.y * 0.5f - 0.05f, 0.65f),
            doorwayOccupants,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore);
        if (count == doorwayOccupants.Length)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            Collider occupant = doorwayOccupants[i];
            if (occupant != null && !occupant.transform.IsChildOf(transform))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyDoorPose()
    {
        if (block == null)
        {
            return;
        }

        Vector3 inner = InnerSize;
        // 伸缩门收进两侧墙槽，开门后不侵入相邻机库的建筑占格。
        float width = inner.x * 0.5f * (1f - doorFraction);
        float offset = inner.x * 0.5f - width * 0.5f;
        Vector3 size = new Vector3(Mathf.Max(0.001f, width), inner.y, 1f);
        SetShape(leftDoor, new Vector3(-offset, 0f, (block.z - 1f) * 0.5f), size);
        SetShape(rightDoor, new Vector3(offset, 0f, (block.z - 1f) * 0.5f), size);
    }

    public static void GetShellBoxes(Vector3 outer, List<Bounds> boxes)
    {
        boxes.Clear();
        Vector3 inner = new Vector3(outer.x - 2f, outer.y - 1f, outer.z - 2f);
        boxes.Add(new Bounds(new Vector3(0f, -outer.y * 0.5f + 0.25f, 0f), new Vector3(outer.x, 0.5f, outer.z)));
        boxes.Add(new Bounds(new Vector3(0f, outer.y * 0.5f - 0.25f, 0f), new Vector3(outer.x, 0.5f, outer.z)));
        boxes.Add(new Bounds(new Vector3(-(outer.x - 1f) * 0.5f, 0f, 0f), new Vector3(1f, inner.y, outer.z)));
        boxes.Add(new Bounds(new Vector3((outer.x - 1f) * 0.5f, 0f, 0f), new Vector3(1f, inner.y, outer.z)));
        boxes.Add(new Bounds(new Vector3(0f, 0f, -(outer.z - 1f) * 0.5f), new Vector3(inner.x, inner.y, 1f)));
        boxes.Add(new Bounds(new Vector3(0f, 0f, (outer.z - 1f) * 0.5f), new Vector3(inner.x, inner.y, 1f)));
    }

    public bool TryResize(int axis, int delta, out string reason)
    {
        reason = "Resize a selected hangar in the base editor.";
        BuildManager build = BuildManager.instance;
        if (combat || build == null || !build.IsEditingBaseBlueprint || axis < 0 || axis > 2 || delta == 0)
        {
            return false;
        }

        Vector3Int before = new Vector3Int(block.x, block.y, block.z);
        Vector3Int after = before;
        after[axis] += delta;
        if (after.x < 3 || after.y < 2 || after.z < 3 || after.x > 64 || after.y > 64 || after.z > 64)
        {
            reason = "Interior dimensions must be at least 1 m; the building safety limit is 64 m outside.";
            return false;
        }

        Vector3 oldPosition = transform.position;
        // Keep the lower minimum corner on the building grid when changing odd/even dimensions.
        Vector3 nextPosition = oldPosition + transform.TransformDirection((Vector3)(after - before) * .5f);
        Collider[] hits = Physics.OverlapBox(
            nextPosition,
            (Vector3)after * 0.5f - Vector3.one * 0.06f,
            transform.rotation,
            build.blockLayer,
            QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit != null && !hit.transform.IsChildOf(transform))
            {
                reason = "The new hangar volume overlaps another module.";
                return false;
            }
        }

        ApplySize(after, nextPosition);
        ActionManager.instance?.Push(new ResizeHangarAction(this, before, oldPosition, after, nextPosition));
        reason = "Hangar size saved.";
        return true;
    }

    internal void ApplySize(Vector3Int dimensions, Vector3 position)
    {
        transform.position = position;
        block.x = dimensions.x;
        block.y = dimensions.y;
        block.z = dimensions.z;
        block.RefreshRuntimeData();
        Physics.SyncTransforms();
        BuildManager.instance?.SaveBlock(block);
    }

    private static void SetShape(Transform piece, Vector3 position, Vector3 size)
    {
        if (piece != null)
        {
            piece.localPosition = position;
            piece.localScale = size;
        }
    }
}
