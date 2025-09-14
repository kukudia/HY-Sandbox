using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class Block : MonoBehaviour
{
    public bool canSpawnConnector = false;

    public bool canBeDeleted = true;

    public int x = 1, y = 1, z = 1;

    public float density = 1;

    public float mass;

    public float maxDurability = 100f; // 最大耐久值

    public float currentDurability;

    public float collisionSpeedThreshold = 1f; // 触发耐久减少的最小速度

    public float damageMultiplier = 10f; // 伤害系数（速度越大伤害越高）

    public string resourcePath; // 运行时使用的预制体路径

    public GameObject connectorPrefab;

    public Transform connectorParent;

    public BoxCollider raycastCollider;

    public List<Connector> connectors= new List<Connector>();

    public List<Block> neighbors = new List<Block>();

    public bool canRotate = true;
    public bool showConnectors = true;
    public bool showLabel = true;

    public string uniqueId;

    void Start()
    {
        currentDurability = maxDurability; // 初始化耐久值
    }

    // 碰撞检测
    

    private void Awake()
    {
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
#if UNITY_EDITOR
        var prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
        if (prefab != null)
        {
            resourcePath = AssetDatabase.GetAssetPath(prefab);
        }
#endif

        // 防止负数
        x = Mathf.Max(1, x);
        y = Mathf.Max(1, y);
        z = Mathf.Max(1, z);
        mass = x * y * z * density;

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
        raycastCollider.size = new Vector3(x - 0.1f, y - 0.1f, z - 0.1f);

        if (canSpawnConnector)
        {
            GenerateConnectionPoints();
        }
    }

    void GenerateConnectionPoints()
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

    /// <summary>
    /// 创建一个连接点
    /// </summary>
    void CreateConnectionPoint(ConnectType connectType, Vector3 localPos, Vector3 normal, int order)
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

    public void CheckConnection()
    {
        //Transform parent = connectorParent;

        //if (parent == null) return;

        foreach (Connector c in connectors)
        {
            if (!c.canConnect) continue;
            Vector3 worldPos = transform.TransformPoint(c.localPos);
            Vector3 worldNormal = transform.TransformDirection(c.normal);

            c.isConnected = false;

            if (Physics.Raycast(worldPos, worldNormal, out RaycastHit hit, 0.25f, BuildManager.instance.blockLayer))
            {
                Block otherBlock = hit.collider.GetComponentInParent<Block>();
                if (otherBlock != null && otherBlock != this)
                {
                    // 反向检测：找最近的对方 connector
                    Transform otherParent = otherBlock.connectorParent;

                    foreach (Connector otherC in otherBlock.connectors)
                    {
                        if (!otherC.canConnect) continue;

                        Vector3 otherWorldPos = otherParent.TransformPoint(otherC.localPos);
                        Vector3 otherWorldNormal = otherParent.TransformDirection(otherC.normal);

                        // 判断位置是否接近 + 法向是否相反
                        if (Vector3.Distance(otherWorldPos, worldPos) < 0.25f && Vector3.Dot(otherWorldNormal, -worldNormal) > 0.75f) // 方向接近相反
                        {
                            c.isConnected = true;
                            otherC.isConnected = true;

                            if (c.connector == null && otherC.connector == null && c.isConnected)
                            {
                                GameObject connector = Instantiate(connectorPrefab, connectorParent);
                                connector.transform.localPosition = c.localPos;
                                c.connector = connector;
                                otherC.connector = connector;
                            }

                            if (c.connector == null && otherC.connector != null && c.isConnected)
                            {
                                c.connector = otherC.connector;
                            }

                            if (c.connector != null && otherC.connector == null && otherC.isConnected)
                            {
                                otherC.connector = c.connector;
                                Debug.Log("c.connector != null && otherC.connector == null");
                            }

                            //Debug.Log($"{name} | {c.name} connect with {otherBlock.name} | {otherC.name}");
                            break;
                        }
                    }
                }
            }

            if (c.connector != null && !c.isConnected)
            {
                Destroy(c.connector);
            }
        }
    }

    public List<Block> Neighbors()
    {
        List<Block> neighbors = new List<Block>();
        for (int i = 0; i < connectors.Count; i++)
        {
            Connector c = connectors[i];
            if (!c.canConnect) continue;
            Vector3 worldPos = connectorParent.TransformPoint(c.localPos);
            Vector3 worldNormal = connectorParent.TransformDirection(c.normal);

            if (Physics.Raycast(worldPos, worldNormal, out RaycastHit hit, 0.25f, BuildManager.instance.blockLayer))
            {
                Block otherBlock = hit.collider.GetComponentInParent<Block>();
                if (otherBlock != null && otherBlock != this)
                {
                    if (!neighbors.Contains(otherBlock))
                    {
                        neighbors.Add(otherBlock);
                    }

                    if (!otherBlock.neighbors.Contains(this))
                    {
                        otherBlock.neighbors.Add(this);
                    }
                }
            }
        }
        neighbors.RemoveAll(item => item == null);
        //Debug.Log($"{name} find {neighbors.Count} neighbors.");
        return neighbors;
    }

    public void DisConnectAllConnectors()
    {
        LayerMask rackLayer = 0;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.gameObject.layer = rackLayer;
        }

        foreach (Block neighbor in neighbors)
        {
            neighbor.CheckConnection();
        }
        neighbors.Clear();
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