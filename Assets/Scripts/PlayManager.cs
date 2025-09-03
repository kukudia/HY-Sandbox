using Unity.VisualScripting;
using UnityEngine;

public class PlayManager : MonoBehaviour
{
    public static PlayManager instance;
    public bool playMode = false;
    public Transform blocksParent;
    private Rigidbody rb;

    private void Awake()
    {
        instance = this;
    }

    public void PlayStart()
    {
        blocksParent = BuildManager.instance.blocksParent;
        if (blocksParent.GetComponent<Rigidbody>() == null)
        {
            rb = blocksParent.AddComponent<Rigidbody>();
        }
        SetRigibody();
        CalculateMass();

        BuildManager.instance.enabled = false;
        playMode = true;
    }

    void SetRigibody()
    {
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void CalculateMass()
    {
        Block[] blocks = blocksParent.GetComponentsInChildren<Block>();
        float mass = 0;
        foreach (Block block in blocks)
        {
            mass += block.mass;
        }
        rb.mass = mass;
        Debug.Log($"{blocksParent.name} mass: {mass}");
    }
}
