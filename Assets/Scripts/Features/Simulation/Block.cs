// 职责：保存方块尺寸、质量、连接面和编辑器配置；由 BlockConnections 维护运行时连接。
// 入口：GetDisplayInfo() / GetConnectorWorldPosition() / GetConnectorWorldNormal()。
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Block : MonoBehaviour
{
    // Block owns grid dimensions and connector state; grouping and physics are handled by managers.
    public bool canSpawnConnector = false;
    public bool canBeDeleted = true;
    public bool canEditInfo = false;
    public int x = 1, y = 1, z = 1;
    public int cost;
    public float density = 1;
    public float mass = 0;
    [SerializeField, Tooltip("关闭时质量等于 x × y × z；开启后使用手动填写的 mass。density 保留作旧数据兼容。")]
    private bool _useCustomMass;
    [SerializeField, HideInInspector]
    private bool _massSettingsInitialized;
    public bool UseCustomMass => _useCustomMass;

    public float collisionSpeedThreshold = 1f; // 触发耐久减少的最小速度
    public float damageMultiplier = 10f; // 伤害系数（速度越大伤害越高）
    [Header("Explosion Settings")]
    public float explosionForce = 100f; // 爆炸力
    public float explosionRadius = 5f; // 爆炸半径
    [Range(0f, 1f)]
    public float explosionDisconnectProbability = 0.35f;
    public string resourcePath; // 运行时使用的预制体路径
    [TextArea]
    public string info;
    public GameObject connectorPrefab;
    public Transform connectorParent;
    public BoxCollider raycastCollider;
    public List<Connector> connectors = new List<Connector>();
    public List<Block> neighbors = new List<Block>();
    public bool canRotate = true;
    public bool canExplode = false;
    public bool showConnectors = true;
    public bool showLabel = true;
    public string uniqueId;
    private StatusIcon _icon;
    private BlockConnections _connections;
    private BlockConnections Connections => _connections ?? (_connections = new BlockConnections(this));

    private void Awake()
    {
        RefreshMass();
        _icon = GetComponent<StatusIcon>();
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
        }

        Vector3 pos = Vector3.zero;
        pos.x = Mathf.Round(transform.position.x * 2) / 2f;
        pos.y = Mathf.Round(transform.position.y * 2) / 2f;
        pos.z = Mathf.Round(transform.position.z * 2) / 2f;
        transform.position = pos;
        Vector3 euler = transform.rotation.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90) * 90;
        euler.y = Mathf.Round(euler.y / 90) * 90;
        euler.z = Mathf.Round(euler.z / 90) * 90;
        transform.rotation = Quaternion.Euler(euler);
    }

    private void OnValidate()
    {
        // 运行时反序列化也可能触发 OnValidate；制作期刷新会清除已有连接状态。
        if (Application.isPlaying)
        {
            return;
        }

        RefreshAuthoringData();
        var csv = GetComponent<BlockCsvBinding>();
        if (csv != null)
        {
            csv.RequestValidation();
        }
    }

    /// <summary>更新制作期的质量、碰撞体、连接点和自动说明；批量工具在提交参数后调用。</summary>
    public void RefreshAuthoringData()
    {
#if UNITY_EDITOR
        var prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
        if (prefab != null)
        {
            resourcePath = AssetDatabase.GetAssetPath(prefab);
        }

#endif
        if (GetComponent<BoxCollider>() == null)
        {
            raycastCollider = gameObject.AddComponent<BoxCollider>();
        }
        else
        {
            raycastCollider = GetComponent<BoxCollider>();
        }

        if (GetComponent<Durability>() == null)
        {
            gameObject.AddComponent<Durability>();
        }

        raycastCollider.isTrigger = false;
        RefreshRuntimeData(true);
    }

    /// <summary>刷新实例派生数值；尺寸未变化时保留连接对象与状态，不创建组件或修改资源路径。</summary>
    public void RefreshRuntimeData(bool rebuildConnectors = false)
    {
        x = Mathf.Max(1, x);
        y = Mathf.Max(1, y);
        z = Mathf.Max(1, z);
        RefreshMass();
        if (raycastCollider == null)
        {
            raycastCollider = GetComponent<BoxCollider>();
        }

        if (raycastCollider != null)
        {
            raycastCollider.size = new Vector3(x - 0.1f, y - 0.1f, z - 0.1f);
        }

        // 底座是薄平面；制作期重载也必须使用它自己的几何，不能按整格厚度覆盖。
        if (TryGetComponent(out BuildingFoundation foundation))
        {
            foundation.RefreshGeometry(this);
        }

        if (rebuildConnectors && canSpawnConnector)
        {
            GenerateConnectionPoints();
        }

        if (!canEditInfo || string.IsNullOrEmpty(info))
        {
            info = GenerateInfo();
        }
    }

    /// <summary>旧资源首次加载时保留有效的非体积质量，之后显式区分自动和手动模式。</summary>
    public void RefreshMass()
    {
        float volume = (float)Mathf.Max(1, x) * Mathf.Max(1, y) * Mathf.Max(1, z);
        if (!_massSettingsInitialized)
        {
            _useCustomMass = mass > 0f && !float.IsNaN(mass) && !float.IsInfinity(mass) && !Mathf.Approximately(mass, volume);
            _massSettingsInitialized = true;
        }

        mass = _useCustomMass && mass > 0f && !float.IsNaN(mass) && !float.IsInfinity(mass) ? mass : volume;
    }

    /// <summary>Editor 表格与制作工具显式设置质量，避免 OnValidate 覆盖手动值。</summary>
    public void SetMassSettings(bool useCustomMass, float value)
    {
        _massSettingsInitialized = true;
        _useCustomMass = useCustomMass;
        mass = value;
        RefreshMass();
    }

    private void Reset()
    {
        SetMassSettings(false, 0f);
    }

    private void GenerateConnectionPoints()
    {
        connectors.Clear();
        int k = 1;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < z; j++)
            {
                Vector3 pos = new Vector3(i - (x - 1) / 2f, y / 2f, j - (z - 1) / 2f);
                CreateConnectionPoint(ConnectType.Up, pos, Vector3.up, k);
                k++;
            }
        }

        k = 1;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < z; j++)
            {
                Vector3 pos = new Vector3(i - (x - 1) / 2f, -y / 2f, j - (z - 1) / 2f);
                CreateConnectionPoint(ConnectType.Down, pos, Vector3.down, k);
                k++;
            }
        }

        k = 1;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                Vector3 pos = new Vector3(i - (x - 1) / 2f, j - (y - 1) / 2f, z / 2f);
                CreateConnectionPoint(ConnectType.Forward, pos, Vector3.forward, k);
                k++;
            }
        }

        k = 1;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                Vector3 pos = new Vector3(i - (x - 1) / 2f, j - (y - 1) / 2f, -z / 2f);
                CreateConnectionPoint(ConnectType.Back, pos, Vector3.back, k);
                k++;
            }
        }

        k = 1;
        for (int i = 0; i < y; i++)
        {
            for (int j = 0; j < z; j++)
            {
                Vector3 pos = new Vector3(-x / 2f, i - (y - 1) / 2f, j - (z - 1) / 2f);
                CreateConnectionPoint(ConnectType.Left, pos, Vector3.left, k);
                k++;
            }
        }

        k = 1;
        for (int i = 0; i < y; i++)
        {
            for (int j = 0; j < z; j++)
            {
                Vector3 pos = new Vector3(x / 2f, i - (y - 1) / 2f, j - (z - 1) / 2f);
                CreateConnectionPoint(ConnectType.Right, pos, Vector3.right, k);
                k++;
            }
        }
    }

    public string GetDisplayInfo()
    {
        return canEditInfo && !string.IsNullOrWhiteSpace(info) ? info : GenerateInfo();
    }

    private string GenerateInfo()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Size: {x}x{y}x{z}");
        builder.AppendLine($"Mass: {mass:0.##} kg");
        if (cost > 0)
        {
            builder.AppendLine($"Cost: {cost}");
        }

        Durability durability = GetComponent<Durability>();
        if (durability != null)
        {
            builder.AppendLine($"Durability: {durability.maxDurability:0.##}");
        }

        Power power = GetComponent<Power>();
        if (power != null && power.standardWorkingPower > 0f)
        {
            builder.AppendLine($"Power: {power.minWorkingPower:0.##} min / {power.standardWorkingPower:0.##} standard");
        }

        PowerGeneratingUnit generator = GetComponent<PowerGeneratingUnit>();
        if (generator != null)
        {
            builder.AppendLine($"Output: {generator.outputPower:0.##}");
        }

        Thruster thruster = GetComponent<Thruster>();
        if (thruster != null)
        {
            builder.AppendLine($"Max thrust: {thruster.maxThrust:0.##} N");
        }

        CargoHold cargo = GetComponent<CargoHold>();
        if (cargo != null)
        {
            builder.AppendLine($"Capacity: {cargo.Capacity}");
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// 创建一个连接点。
    /// </summary>
    private void CreateConnectionPoint(ConnectType connectType, Vector3 localPos, Vector3 normal, int order)
    {
        Connector connector = new Connector();
        connector.name = $"{connectType} {order}";
        connector.canConnect = true;
        connector.connectType = connectType;
        connector.localPos = localPos;
        connector.normal = normal;
        connector.order = order;
        connectors.Add(connector);
    }

    private Transform ConnectorRoot => connectorParent != null ? connectorParent : transform;

    public Vector3 GetConnectorWorldPosition(Connector connector)
    {
        return ConnectorRoot.TransformPoint(connector.localPos);
    }

    public Vector3 GetConnectorWorldNormal(Connector connector)
    {
        return ConnectorRoot.TransformDirection(connector.normal).normalized;
    }

    public bool IsConnectorAvailableForPlacement(Connector connector)
    {
        return Connections.IsConnectorAvailableForPlacement(connector);
    }

    public void CheckConnection()
    {
        Connections.CheckConnection();
    }

    public List<Block> Neighbors()
    {
        Physics.SyncTransforms();
        List<Block> result = new List<Block>();
        CollectNeighbors(result);
        return result;
    }

    internal void CollectNeighbors(List<Block> result)
    {
        Connections.CollectNeighbors(result);
    }

    public void DisConnectAllConnectors(bool refreshNeighbors = true)
    {
        Connections.DisConnectAllConnectors(refreshNeighbors);
    }

    internal void RefreshConnectionStatus()
    {
        if (_icon != null)
        {
            _icon.CheckConnectionStatus();
        }
    }

    public bool IsBlockedGhost()
    {
        Transform parent = connectorParent != null ? connectorParent : transform;
        for (int i = 0; i < connectors.Count; i++)
        {
            Connector c = connectors[i];
            Vector3 worldPos = parent.TransformPoint(c.localPos);
            Vector3 worldNormal = parent.TransformDirection(c.normal);
            c.isConnected = false;
            // 用小射线探测相邻是否有方块
            if (Physics.Raycast(worldPos, -worldNormal, out RaycastHit hit, 0.1f, BuildManager.instance.blockLayer))
            {
                Block otherBlock = hit.collider.GetComponentInParent<Block>();
                if (otherBlock != null && otherBlock != this)
                {
                    return true;
                }
            }
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform parent = connectorParent != null ? connectorParent : transform;
        for (int i = 0; i < connectors.Count; i++)
        {
            Vector3 worldPos = parent.TransformPoint(connectors[i].localPos);
            if (showConnectors)
            {
                if (connectors[i].canConnect)
                {
                    Gizmos.color = connectors[i].isConnected ? Color.green : Color.yellow;
                    Gizmos.DrawWireSphere(worldPos, 0.1f);
                    Vector3 worldNormal = parent.TransformDirection(connectors[i].normal);
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(worldPos, worldPos + worldNormal * 0.2f);
                }
                else
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireSphere(worldPos, 0.1f);
                }
            }

            if (showLabel)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 14;
                Handles.Label(worldPos, connectors[i].name, style);
            }
        }
    }
#endif
}

[System.Serializable]
public class Connector
{
    public string name;
    public bool canConnect;
    public bool isConnected;
    public ConnectType connectType;
    public Vector3 localPos;
    public Vector3 normal;
    public int order;
    public GameObject connector;
}

public enum ConnectType
{
    Up,
    Down,
    Forward,
    Back,
    Left,
    Right
}
