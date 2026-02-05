using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlUnit : MonoBehaviour
{
    public HoverFlightController hoverFlightController;
    public Cockpit cockpit;
    public MainThruster[] mainThrusters;
    public HoverThruster[] hoverThrusters;
    private float cooldownTime = 0.2f;
    private bool _isOnCooldown;

    private void Start()
    {
        // 在ControlUnit启动时向PlayManager注册
        if (PlayManager.instance != null)
            PlayManager.instance.RegisterControlUnit(this);

        Invoke("RefreshChildren", 2f);
    }

    private void Update()
    {
        if (!PlayManager.instance.playMode) return;

        if (transform.childCount == 0)
        {
            Destroy(gameObject);
        }

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
        // 只有在缓存为空或需要强制刷新时才重新获取
        if (hoverFlightController == null)
            hoverFlightController = GetComponentInChildren<HoverFlightController>();

        if (cockpit ==  null)
            cockpit = GetComponentInChildren<Cockpit>();

        if (mainThrusters == null)
            mainThrusters = GetComponentsInChildren<MainThruster>();

        if (hoverThrusters != null)
            hoverThrusters = GetComponentsInChildren<HoverThruster>();

        if (hoverFlightController == null)
        {
            Debug.LogWarning($"Cannot find HoverFlightController in {gameObject}");
        }

        if (hoverFlightController != null && hoverThrusters.Length > 0)
        {
            hoverFlightController.thrusters = hoverThrusters;
            hoverFlightController.enabled = true;
            hoverFlightController.showUI = true;
            hoverFlightController.Init();
        }

        //Collider[] siblingColliders = GetComponentsInChildren<Collider>();

        //// 双重循环遍历所有碰撞体组合
        //for (int i = 0; i < siblingColliders.Length; i++)
        //{
        //    for (int j = i + 1; j < siblingColliders.Length; j++)
        //    {
        //        // 忽略两个碰撞体之间的碰撞
        //        Physics.IgnoreCollision(siblingColliders[i], siblingColliders[j], true);
        //    }
        //}
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

        if (_isOnCooldown) return;

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

        StartCoroutine(StartCooldown());
    }

    private void OnDestroy()
    {
        // 在销毁时反注册
        if (PlayManager.instance != null)
            PlayManager.instance.UnregisterControlUnit(this);
    }

    private IEnumerator StartCooldown()
    {
        // 设置状态为冷却中
        _isOnCooldown = true;
        // 等待指定的冷却时间
        yield return new WaitForSeconds(cooldownTime);
        // 冷却结束，重置状态
        _isOnCooldown = false;
    }
}
