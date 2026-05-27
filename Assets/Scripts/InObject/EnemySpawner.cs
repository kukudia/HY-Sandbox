using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject blocksParentPrefab;
    public bool spawnOnStart = true;
    public bool useSavedBlueprint = true;
    public float spawnInterval = 5f;
    public List<BlockPlacement> blueprint = new List<BlockPlacement>
    {
        new BlockPlacement { blockResourcePath = "Blocks/Cockpit", localPosition = Vector3.zero },
        new BlockPlacement { blockResourcePath = "Blocks/Turret", localPosition = new Vector3(0f, 1.5f, 0f) }
    };

    private void Start()
    {
        
    }

    

    

    
}

[System.Serializable]
public class BlockPlacement
{
    public string blockResourcePath;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
}
