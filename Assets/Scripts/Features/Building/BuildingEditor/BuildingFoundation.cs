// 职责：配置建筑薄平面底座、碰撞体及每格向上连接点；底座不进入普通方块存档或操作栈。
// 入口：Configure()；显示自动裁切网格，范围以外仍可在虚拟地面放置，不移动既有建筑。
using UnityEngine;

[RequireComponent(typeof(Block), typeof(BoxCollider))]
public sealed class BuildingFoundation : MonoBehaviour
{
    [SerializeField]
    private Transform _model;
    [SerializeField]
    private Renderer _surface;
    [SerializeField, Min(0.01f)]
    private float _thickness = 0.1f;
    private Material _runtimeMaterial;
    private Vector2 _minimum = new Vector2(-2f, -2f);
    private BuildingFoundationData _configured;
    public void Configure(BuildingFoundationData data)
    {
        if (_configured != null && _configured.width == data.width && _configured.length == data.length && _configured.offsetX == data.offsetX && _configured.offsetZ == data.offsetZ)
        {
            return;
        }

        _configured = data.Copy();
        _minimum = new Vector2(data.MinX, data.MinZ);
        Block block = GetComponent<Block>();
        block.DisConnectAllConnectors();
        block.RestoreConnectionParticipation();
        // 普通拆除流程会移出 Block 层；底座重建后立即恢复参与连接探测。
        gameObject.layer = LayerMask.NameToLayer("Block");
        block.canSpawnConnector = false;
        block.canBeDeleted = false;
        block.canRotate = false;
        block.x = Mathf.Clamp(data.width, 1, BuildingFoundationData.MaximumSize);
        block.y = 1;
        block.z = Mathf.Clamp(data.length, 1, BuildingFoundationData.MaximumSize);
        block.RefreshMass();
        block.connectors.Clear();
        block.connectorParent = transform;
        block.showLabel = false;
        // 固定左下角，避免奇偶尺寸变化使所有连接点平移半格。
        for (int x = 0; x < block.x; x++)
        {
            for (int z = 0; z < block.z; z++)
            {
                int order = x * block.z + z + 1;
                block.connectors.Add(new Connector { name = $"Up {order}", canConnect = true, connectType = ConnectType.Up, normal = Vector3.up, localPos = new Vector3(_minimum.x + x + .5f, 0f, _minimum.y + z + .5f), order = order });
            }
        }

        RefreshGeometry(block);
        Physics.SyncTransforms();
    }

    internal void RefreshGeometry(Block block)
    {
        // Oversized drafts remain visible and editable; only connector allocation and deployment have a cap.
        int width = _configured?.width ?? block.x;
        int length = _configured?.length ?? block.z;
        Vector3 size = new Vector3(width, _thickness, length);
        Vector3 center = new Vector3(_minimum.x + width * .5f, -_thickness * .5f, _minimum.y + length * .5f);
        BoxCollider collider = GetComponent<BoxCollider>();
        collider.size = size;
        // 与普通方块的 0.05m 面内缩保持相同间隙，避免预览的整格查询把贴面视为穿透。
        collider.center = center - Vector3.up * 0.05f;
        block.raycastCollider = collider;
        if (_model != null)
        {
            _model.localPosition = center;
            _model.localScale = size;
        }

        if (_surface != null && _surface.sharedMaterial != null)
        {
            if (Application.isPlaying)
            {
                if (_runtimeMaterial == null)
                {
                    _runtimeMaterial = new Material(_surface.sharedMaterial);
                }

                _surface.sharedMaterial = _runtimeMaterial;
                _runtimeMaterial.mainTextureScale = new Vector2(width, length);
            }
        }
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
        }
    }
}
