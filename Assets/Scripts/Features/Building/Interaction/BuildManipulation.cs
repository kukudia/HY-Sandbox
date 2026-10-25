// 职责：处理所选方块的键盘移动/旋转、轴拖拽及复制，提交存档和 Undo 动作。
// 入口：Poll() / TryBeginDrag() / CancelDrag()；旋转锁定中心，移动吸附与阻挡复用 BuildPlacementGeometry。
using System;
using UnityEngine;
using UnityEngine.InputSystem;

internal sealed class BuildManipulation
{
    private readonly BuildPlacementGeometry _geometry;
    private readonly Action<Block> _save;
    private readonly Func<GameObject, string, Vector3, Quaternion, Block> _create;
    private Camera mainCamera;
    private GameObject moveAxis;
    private GameObject rotateAxis;
    private Block selectedBlock;
    private MoveAxisHandle activeHandle;
    private Vector3 dragStartPos;
    private Vector3 blockStartPos;
    private RotateAxisHandle activeRotateHandle;
    private Vector3 rotateDragStart;
    private Quaternion blockStartRot;
    private Vector3 blockStartRotatePos;
    private Vector3 rotateLastPos;
    private Quaternion rotateLastRot;
    private Vector3 axisForward;
    private Vector3 axisRight;
    private Vector3 axisUp;
    private const float moveStep = 1f;
    private static readonly Vector3[] dirs =
    {
        Vector3.right,
        -Vector3.right,
        Vector3.up,
        -Vector3.up,
        Vector3.forward,
        -Vector3.forward
    };
    public Vector3 AxisForward => axisForward;
    public Vector3 AxisUp => axisUp;

    public event Action<Block> SelectionChanged;
    public BuildManipulation(
        BuildPlacementGeometry geometry,
        Action<Block> save,
        Func<GameObject, string, Vector3, Quaternion, Block> create)
    {
        _geometry = geometry;
        _save = save;
        _create = create;
    }

    public void Configure(Camera camera, GameObject move, GameObject rotate)
    {
        mainCamera = camera;
        moveAxis = move;
        rotateAxis = rotate;
    }

    public void Poll(Block block, BuildType type)
    {
        selectedBlock = block;
        if (block == null || mainCamera == null || block.TryGetComponent(out BuildingFoundation _))
        {
            return;
        }

        AlignAxisToNearestWorldDir();
        if (type == BuildType.Move)
        {
            HandleMovement();
            HandleMoveAxisDrag();
        }
        else if (type == BuildType.Rotate && block.canRotate)
        {
            HandleRotation();
            HandleRotateAxisDrag();
        }
    }

    public bool TryBeginDrag(RaycastHit hit, Block block, BuildType type)
    {
        selectedBlock = block;
        if (block == null || block.TryGetComponent(out BuildingFoundation _))
        {
            return false;
        }

        if (type == BuildType.Move)
        {
            MoveAxisHandle handle = hit.collider.GetComponentInParent<MoveAxisHandle>();
            if (handle != null)
            {
                activeHandle = handle;
                dragStartPos = hit.point;
                blockStartPos = block.transform.position;
                return true;
            }
        }

        if (type == BuildType.Rotate && block.canRotate)
        {
            RotateAxisHandle handle = hit.collider.GetComponentInParent<RotateAxisHandle>();
            if (handle != null)
            {
                activeRotateHandle = handle;
                rotateDragStart = hit.point;
                blockStartRot = block.transform.rotation;
                blockStartRotatePos = block.transform.position;
                rotateLastRot = blockStartRot;
                rotateLastPos = blockStartRotatePos;
                return true;
            }
        }

        return false;
    }

    public void CancelDrag()
    {
        activeHandle = null;
        activeRotateHandle = null;
    }

    private void AlignAxisToNearestWorldDir()
    {
        Vector3 camForward = mainCamera.transform.forward.normalized;
        Vector3 camUp = mainCamera.transform.up.normalized;
        // 找到与相机 forward 最接近的方向
        Vector3 nearestForward = dirs[0];
        Vector3 nearestUp = dirs[0];
        float maxDotForward = Vector3.Dot(camForward, nearestForward);
        float maxDotUp = Vector3.Dot(camForward, nearestUp);
        for (int i = 1; i < dirs.Length; i++)
        {
            float dotForward = Vector3.Dot(camForward, dirs[i]);
            float dotUp = Vector3.Dot(camUp, dirs[i]);
            if (dotForward > maxDotForward)
            {
                maxDotForward = dotForward;
                nearestForward = dirs[i];
            }

            if (dotUp > maxDotUp)
            {
                maxDotUp = dotUp;
                nearestUp = dirs[i];
            }
        }

        // 保存坐标系三方向，供移动用
        axisForward = nearestForward;
        axisRight = Vector3.Cross(nearestUp, axisForward).normalized;
        axisUp = Vector3.Cross(axisForward, axisRight).normalized;
        if (moveAxis != null)
        {
            moveAxis.transform.rotation = Quaternion.LookRotation(nearestForward, nearestUp);
        }

        if (rotateAxis != null)
        {
            rotateAxis.transform.rotation = Quaternion.LookRotation(nearestForward, nearestUp);
        }
    }

    private void HandleMovement()
    {
        if (selectedBlock == null)
        {
            return;
        }

        Vector3 moveDir = Vector3.zero;
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            moveDir += axisForward;
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            moveDir -= axisForward;
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            moveDir += axisRight;
        }

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            moveDir -= axisRight;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            moveDir += axisUp;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            moveDir -= axisUp;
        }

        if (moveDir != Vector3.zero)
        {
            Debug.Log($"Move direction: {moveDir}");
            //if (Keyboard.current.shiftKey.isPressed)
            //{
            //    moveStep
            //}
            Vector3 oldPos = selectedBlock.transform.position;
            Vector3 newPos = _geometry.SnapCenterByMinCorner(oldPos + moveDir * moveStep, selectedBlock.transform.rotation, selectedBlock);
            // 检查是否被阻挡
            if (!_geometry.IsBlocked(newPos, selectedBlock.transform.rotation, selectedBlock))
            {
                if (Keyboard.current.shiftKey.isPressed)
                {
                    HandleDuplicate(newPos, selectedBlock.transform.rotation);
                }
                else
                {
                    selectedBlock.transform.position = newPos;
                    _save(selectedBlock);
                    // 记录操作到 Undo 栈
                    var action = new MoveBlockAction(selectedBlock, oldPos, newPos);
                    ActionManager.instance.Push(action);
                    VisualEffectsManager.TryPlayBlockMoved(selectedBlock, oldPos, newPos);
                }
            }
            else
            {
            }
        }
    }

    private void HandleRotation()
    {
        if (selectedBlock == null)
        {
            return;
        }

        Vector3 moveEuler = Vector3.zero;
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            moveEuler += axisRight * 90;
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            moveEuler -= axisRight * 90;
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            moveEuler += axisUp * 90;
        }

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            moveEuler -= axisUp * 90;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            moveEuler += axisForward * 90;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            moveEuler -= axisForward * 90;
        }

        if (moveEuler != Vector3.zero)
        {
            Quaternion oldRot = selectedBlock.transform.rotation;
            // 操作轴来自相机对齐后的世界坐标系，必须在已有姿态左侧叠加。
            Quaternion newRot = Quaternion.Euler(moveEuler) * oldRot;
            Vector3 oldPos = selectedBlock.transform.position;
            // 放置连接点确定的中心（含偶数尺寸半格偏移）在旋转期间保持不变。
            Vector3 newPos = oldPos;
            // 检查是否被阻挡
            if (!_geometry.IsBlocked(newPos, newRot, selectedBlock))
            {
                selectedBlock.transform.position = newPos;
                selectedBlock.transform.rotation = newRot;
                _save(selectedBlock);
                // 记录操作到 Undo 栈
                var action = new RotateBlockAction(selectedBlock, oldPos, newPos, oldRot, newRot);
                ActionManager.instance.Push(action);
                VisualEffectsManager.TryPlayBlockRotated(selectedBlock);
            }
            else
            {
            }
        }
    }

    private void HandleMoveAxisDrag()
    {
        if (Mouse.current.leftButton.isPressed && activeHandle != null)
        {
            Vector3 dir = activeHandle.WorldAxis;
            if (dir.sqrMagnitude < 0.001f)
            {
                return;
            }

            // 构建一个拖拽平面：法线 = 相机方向 × 拖拽方向
            Vector3 planeNormal = Vector3.Cross(dir, mainCamera.transform.up);
            if (planeNormal == Vector3.zero)
            {
                planeNormal = Vector3.Cross(dir, mainCamera.transform.right);
            }

            Plane dragPlane = new Plane(planeNormal, blockStartPos);
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 delta = hitPoint - dragStartPos;
                // 投影到拖拽方向
                float moveAmount = Vector3.Dot(delta, dir.normalized);
                Vector3 oldPos = selectedBlock.transform.position;
                // 步进对齐
                Vector3 newPos = blockStartPos + dir.normalized * Mathf.Round(moveAmount / moveStep) * moveStep;
                if (newPos != oldPos)
                {
                    // 检查是否被阻挡
                    if (!_geometry.IsBlocked(newPos, selectedBlock.transform.rotation, selectedBlock))
                    {
                        if (Keyboard.current.shiftKey.isPressed)
                        {
                            HandleDuplicate(newPos, selectedBlock.transform.rotation);
                        }
                        else
                        {
                            selectedBlock.transform.position = newPos;
                            _save(selectedBlock);
                            // 记录操作到 Undo 栈
                            var action = new MoveBlockAction(selectedBlock, oldPos, newPos);
                            ActionManager.instance.Push(action);
                            VisualEffectsManager.TryPlayBlockMoved(selectedBlock, oldPos, newPos);
                        }
                    }
                    else
                    {
                    }
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && activeHandle != null)
        {
            _save(selectedBlock);
            activeHandle = null;
        }
    }

    private void HandleRotateAxisDrag()
    {
        if (Mouse.current.leftButton.isPressed && activeRotateHandle != null)
        {
            // 旋转轴（世界空间）
            Vector3 axis = activeRotateHandle.WorldAxis;
            if (axis.sqrMagnitude < 0.001f)
            {
                return;
            }

            // 从相机发射射线，与一个垂直于旋转轴的平面相交
            Plane dragPlane = new Plane(axis, blockStartRotatePos);
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                // 起点和当前点在平面上的向量
                Vector3 from = (rotateDragStart - blockStartRotatePos).normalized;
                Vector3 to = (hitPoint - blockStartRotatePos).normalized;
                // 计算旋转角度
                float angle = Vector3.SignedAngle(from, to, axis);
                // 步进（比如 15°/45°）
                float step = 15f;
                float snappedAngle = Mathf.Round(angle / step) * step;
                Quaternion newRot = Quaternion.AngleAxis(snappedAngle, axis) * blockStartRot;
                // 检查是否阻挡
                Vector3 newPos = blockStartRotatePos;
                if (!_geometry.IsBlocked(newPos, newRot, selectedBlock))
                {
                    selectedBlock.transform.position = newPos;
                    selectedBlock.transform.rotation = newRot;
                    rotateLastPos = newPos;
                    rotateLastRot = newRot;
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && activeRotateHandle != null)
        {
            if (selectedBlock != null && (rotateLastRot != blockStartRot || rotateLastPos != blockStartRotatePos))
            {
                _save(selectedBlock);
                ActionManager.instance.Push(new RotateBlockAction(selectedBlock, blockStartRotatePos, rotateLastPos, blockStartRot, rotateLastRot));
                VisualEffectsManager.TryPlayBlockRotated(selectedBlock);
            }

            activeRotateHandle = null;
        }
    }

    private void HandleDuplicate(Vector3 newPos, Quaternion newRot)
    {
        string resourcePath = selectedBlock.resourcePath;
        Debug.Log(resourcePath);
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        Vector3 moveDir = (newPos - selectedBlock.transform.position).normalized;
        float step = _geometry.GetMoveStep(selectedBlock, moveDir);
        // 复制时对齐到步长
        newPos = selectedBlock.transform.position + moveDir * step;
        SelectionChanged?.Invoke(null);
        Block duplicate = _create(prefab, resourcePath, newPos, newRot);
        if (duplicate != null)
        {
            SelectionChanged?.Invoke(duplicate);
        }
    }
}
