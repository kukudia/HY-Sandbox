using UnityEngine;
using UnityEngine.InputSystem;

public class ControlUnit : MonoBehaviour
{
    public HoverFlightController hoverFlightController;
    public Cockpit cockpit;
    public MainThruster[] mainThrusters;
    public HoverThruster[] hoverThrusters;

    private void Update()
    {
        if (!PlayManager.instance.playMode) return;

        if (transform.childCount == 0) Destroy(gameObject);

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach (HoverThruster thruster in hoverThrusters)
            {
                thruster.isHovered = !thruster.isHovered;
            }

            if (hoverFlightController != null)
            {
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
    }

    public void RefreshChildren()
    {
        hoverFlightController = GetComponentInChildren<HoverFlightController>();
        cockpit = GetComponentInChildren<Cockpit>();
        mainThrusters = GetComponentsInChildren<MainThruster>();
        hoverThrusters = GetComponentsInChildren<HoverThruster>();

        if (hoverFlightController != null && hoverThrusters.Length > 0)
        {
            hoverFlightController.thrusters = hoverThrusters;
            hoverFlightController.enabled = true;
            hoverFlightController.showUI = true;
            hoverFlightController.Init();
        }
    }

    public void PlayEnd()
    {
        if (hoverFlightController != null)
        {
            hoverFlightController.enabled = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!PlayManager.instance.playMode) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            Collider thisCollider = contact.thisCollider;   // 我方子Collider
            Collider otherCollider = contact.otherCollider; // 对方Collider

            //Debug.Log($"子项 [{thisCollider.name}] 碰到了 [{otherCollider.name}]，碰撞点 {contact.point}");

            // 如果子项上有 ChildCollisionHandler，转发消息
            var childHandler = thisCollider.GetComponent<Durability>();
            if (childHandler != null)
            {
                childHandler.CollisionEnter(collision);
            }
        }
    }
}
