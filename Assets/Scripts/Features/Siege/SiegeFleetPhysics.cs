// 职责：在准备和战斗边界更新友舰刚体、悬浮和AI；不拥有舰船身份或镜头。
// 入口：Park() / Activate()。
using UnityEngine;

public static class SiegeFleetPhysics
{
    public static void Park(ControlUnit unit)
    {
        Rigidbody body = unit.GetComponent<Rigidbody>();
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        unit.SetMovementInput(Vector3.zero);
        EnemyController ai = unit.GetComponent<EnemyController>();
        if (ai != null)
        {
            ai.enabled = false;
        }
    }

    public static void Activate(ControlUnit unit)
    {
        unit.GetComponent<Rigidbody>().isKinematic = false;
        foreach (HoverThruster thruster in unit.hoverThrusters)
        {
            thruster.isHovered = true;
        }

        if (unit.hoverFlightController != null)
        {
            unit.hoverFlightController.setHeight = true;
            unit.hoverFlightController.targetHoverHeight = Mathf.Max(8f, unit.transform.position.y);
        }

        EnemyController ai = unit.GetComponent<EnemyController>();
        if (ai != null)
        {
            ai.enabled = !unit.AcceptsPlayerInput;
        }
    }
}
