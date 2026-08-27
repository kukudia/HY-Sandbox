using UnityEngine;

public class Power : MonoBehaviour
{
    public float currentPower = 0f;
    public float minWorkingPower = 50f;
    public float standardWorkingPower = 100f;
    public bool isWorking => currentPower >= minWorkingPower;
    public float efficiency => Mathf.Clamp01(currentPower / standardWorkingPower);
}
