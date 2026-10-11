using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public int x = 5, y = 5, z = 5;

    public List<Grid> grids;

    private void Start()
    {
        SpawnGrids();
    }

    void SpawnGrids()
    {
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                for (int k = 0; k < z; k++)
                {
                    Grid grid = new Grid();
                    grid.position.x = i;
                    grid.position.y = j;
                    grid.position.z = k;
                    grids.Add(grid);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        foreach (Grid grid in grids)
        {
            Gizmos.color = grid.isOccupied ? Color.red : Color.green;
            Gizmos.DrawWireCube(grid.position, Vector3.one);
        }
    }
}

[System.Serializable]
public class Grid
{
    public bool isOccupied;
    public Vector3 position;
}