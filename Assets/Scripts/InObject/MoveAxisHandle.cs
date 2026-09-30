using UnityEngine;

public class MoveAxisHandle : MonoBehaviour
{
    public Vector3 axis; // (1,0,0) / (0,1,0) / (0,0,1)

    public Vector3 WorldAxis => (transform.parent != null
        ? transform.parent.TransformDirection(GetLocalAxis())
        : transform.TransformDirection(GetLocalAxis())).normalized;

    private void Awake()
    {
        if (axis.sqrMagnitude < 0.001f)
        {
            axis = GetLocalAxis();
        }
    }

    private Vector3 GetLocalAxis()
    {
        Vector3 localPosition = transform.localPosition;
        if (localPosition.sqrMagnitude > 0.001f)
        {
            return localPosition.normalized;
        }

        string handleName = name.ToLowerInvariant();
        if (handleName.Contains("forward")) return Vector3.forward;
        if (handleName.Contains("right")) return Vector3.right;
        if (handleName.Contains("up")) return Vector3.up;
        return Vector3.right;
    }
}
