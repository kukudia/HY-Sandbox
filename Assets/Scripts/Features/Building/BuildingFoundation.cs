// 职责：配置建筑薄平面底座、碰撞体及每格向上连接点；底座不进入普通方块存档或操作栈。
// 入口：Configure()；网格原点固定在 (-2, 0, -2)，扩缩尺寸不移动既有建筑。
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
    public void Configure(BuildingFoundationData data)
    {
        Block block = GetComponent<Block>();
        block.DisConnectAllConnectors();
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
                block.connectors.Add(new Connector { name = $"Up {order}", canConnect = true, connectType = ConnectType.Up, normal = Vector3.up, localPos = new Vector3(x - 1.5f, 0f, z - 1.5f), order = order });
            }
        }

        RefreshGeometry(block);
        Physics.SyncTransforms();
    }

    internal void RefreshGeometry(Block block)
    {
        Vector3 size = new Vector3(block.x, _thickness, block.z);
        Vector3 center = new Vector3(block.x * 0.5f - 2f, -_thickness * 0.5f, block.z * 0.5f - 2f);
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
                _runtimeMaterial.mainTextureScale = new Vector2(block.x, block.z);
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
