using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Info : MonoBehaviour
{
    [Header("Static")]
    public string blockName;
    public string description;
    public int cost;
    public List<Status> statuses;

    [Header("Dynamic")]
    public ConnectionStatus connectionStatus;
    public DurabilityStatus durabilityStatus;
    public PowerStatus powerStatus;

    public void CheckConnectionStatus()
    {

    }


    public void CheckDurabilityStatus()
    {

    }

    public void CheckPowerStatus()
    {

    }
}

[Serializable]
public class Status
{
    public string name;
    public Sprite icon;
}

public enum ConnectionStatus
{
    Normal,
    NoConnection,
}

public enum DurabilityStatus
{
    Normal,
    Damaged,
    Broken,
}

public enum PowerStatus
{
    Normal,
    UnderPower,
    NoPower,
}

