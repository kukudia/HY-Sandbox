using System.Collections.Generic;
using UnityEngine;

public class Power : MonoBehaviour
{
    private static readonly HashSet<Power> ActivePowerBlocks = new HashSet<Power>();

    public float currentPower = 0f;
    public float minWorkingPower = 50f;
    public float standardWorkingPower = 100f;
    private Info info;
    public bool isWorking => currentPower >= minWorkingPower;
    public float efficiency => standardWorkingPower > 0f
        ? Mathf.Clamp01(currentPower / standardWorkingPower)
        : 0f;

    public static IEnumerable<Power> ActiveBlocks => ActivePowerBlocks;

    private void OnEnable()
    {
        info = GetComponent<Info>();
        ActivePowerBlocks.Add(this);
        info?.CheckPowerStatus();
    }

    private void OnDisable()
    {
        ActivePowerBlocks.Remove(this);
        currentPower = 0f;
        info?.CheckPowerStatus();
    }

    public void ResetPower()
    {
        currentPower = 0f;
    }

    public void ReceivePower(float suppliedPower)
    {
        currentPower += Mathf.Max(0f, suppliedPower);
    }

    public void CommitPowerStatus()
    {
        info?.CheckPowerStatus();
    }
}
