using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Block : MonoBehaviour
{
    public bool canSpawnConnector = false;

    public bool canBeDeleted = true;

    public int x = 1, y = 1, z = 1;

    public string resourcePath; // 运行时使用的预制体路径

    public GameObject connectorPrefab;

    public Transform connectorParent;

    public List<Connector> connectors= new List<Connector>();

    public bool showCube = true;
    public bool showConnectors = true;
    public bool showLabel = true;

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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (showCube)
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(x, y, z));
        }

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