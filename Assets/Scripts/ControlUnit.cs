using UnityEngine;
using UnityEngine.InputSystem;

public class ControlUnit : MonoBehaviour
{
    public HoverFlightController hoverFlightController;
    public Cockpit[] cockpits;
    public MainThruster[] mainThrusters;
    public HoverThruster[] hoverThrusters;

    private void Awake()
    {
        hoverFlightController = GetComponent<HoverFlightController>();
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach (HoverThruster thruster in hoverThrusters)
            {
                thruster.isHovered = !thruster.isHovered;
            }

            if (!hoverFlightController.setHeight)
            {
                hoverFlightController.targetHoverHeight = (int)transform.position.y + 10;
                hoverFlightController.setHeight = true;
            }
            else
            {
                hoverFlightController.targetHoverHeight = 0;
                hoverFlightController.setHeight = false;
            }
        }
    }

    public void PlayStart()
    {
        cockpits = GetComponentsInChildren<Cockpit>();
        mainThrusters = GetComponentsInChildren<MainThruster>();
        hoverThrusters = GetComponentsInChildren<HoverThruster>();

        if (hoverThrusters.Length > 0 && HasCockpit())
        {
            hoverFlightController.thrusters = hoverThrusters;
            hoverFlightController.enabled = true;
            hoverFlightController.Init();
        }
    }

    public void PlayEnd()
    {
        hoverFlightController.enabled = false;
    }

    public bool HasCockpit()
    {
        if (cockpits.Length > 0)
        {
            return true;
        }
        return false;
    }
}
