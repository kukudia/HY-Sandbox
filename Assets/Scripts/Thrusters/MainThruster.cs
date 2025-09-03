using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MainThruster : Thruster
{
    public KeyControl KeyControl { get; private set; }

    public override bool ShouldActivate()
    {
        if (PlayManager.instance.playMode && Keyboard.current.wKey.isPressed)
        {
            return true;
        }
        return false;
    }
}

