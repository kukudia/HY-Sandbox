using UnityEngine;
using UnityEngine.UI;

public class Status : MonoBehaviour
{
    public Image icon;
}

public enum BlockStatus
{
    Normal,
    Damaged,
    Broken,
    LowPower,
    WithoutPower,
}
