using Unity.VisualScripting;
using UnityEngine;

public class PlayManager : MonoBehaviour
{
    public static PlayManager instance;
    public bool playMode = false;

    private void Awake()
    {
        instance = this;
    }

    public void PlayStart()
    {
        if (BuildManager.instance.blocksParent.GetComponent<Rigidbody>() == null)
        {
            BuildManager.instance.blocksParent.AddComponent<Rigidbody>();
        }

        BuildManager.instance.enabled = false;
        playMode = true;
    }
}
