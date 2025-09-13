using UnityEngine;

public class Rack : MonoBehaviour
{
    public void DisConnectAllConnectors()
    {
        LayerMask rackLayer = 0;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.gameObject.layer = rackLayer;
        }

        Block block = GetComponent<Block>();

        foreach (Connector connector in block.connectors)
        {

        }

        foreach (Block neighbor in block.neighbors)
        {
            neighbor.CheckConnection();
        }
        block.neighbors.Clear();
    }
}
