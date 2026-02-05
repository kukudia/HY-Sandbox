using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RepairBot : MonoBehaviour
{
    public Transform home;
    public Vector3 homeOffset = new Vector3(0, 0, 0);

    [Header("修复设置")]
    public float repairAmount = 10f; // 每次修复量
    public float repairCooldown = 1f; // 修复冷却时间
    public float detectionRange = 50f; // 检测范围
    public float movementSpeed = 5f; // 移动速度
    public float rotationSpeed = 2f; // 旋转速度
    public LayerMask obstacleMask; // 障碍物层

    [Header("避障设置")]
    public float avoidanceRange = 8f; // 避障检测范围
    public float avoidanceForce = 5f; // 避障力度
    public int avoidanceRays = 8; // 避障射线数量

    [Header("修复效果")]
    public LineRenderer repairBeam;
    public Gradient repairBeamGradient;
    public float beamWidth = 0.2f;

    public Durability currentTarget;
    public float lastRepairTime;
    public bool isRepairing;
    private Vector3 avoidanceDirection;
    private Rigidbody rb; // 刚体组件

    void Start()
    {
        // 获取或添加Rigidbody组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false; // 禁用重力
        rb.linearDamping = 2f; // 设置阻力
        rb.angularDamping = 3f; // 设置角阻力

        // 初始化修复光束
        if (repairBeam == null)
        {
            repairBeam = gameObject.AddComponent<LineRenderer>();
            repairBeam.material = new Material(Shader.Find("Sprites/Default"));
            repairBeam.colorGradient = repairBeamGradient; // 使用渐变颜色
            repairBeam.startWidth = beamWidth;
            repairBeam.endWidth = beamWidth * 0.5f;
            repairBeam.enabled = false;
        }

        home = transform.parent; // 设置基地为父对象
        homeOffset = transform.localPosition; // 设置基地偏移
    }

    void FixedUpdate()
    {
        // 如果没有当前目标，寻找可修复的方块
        if (currentTarget == null)
        {
            FindDamagedBlock();
            UpdateRepairBeam(false);

            // 如果没有找到目标且基地不为空，返回基地
            if (currentTarget == null && home != null)
            {
                NavigateToTarget(home);

                // 检查是否到达基地
                float distanceToHome = Vector3.Distance(transform.position, home.position);
                if (distanceToHome < 2f) // 阈值
                {
                    ReturnHome();
                }
            }
            else
            {

            }
        }
        else
        {
            // 有目标时导航到目标
            NavigateToTarget(currentTarget.transform);

            // 检查是否到达目标并修复
            CheckAndRepair();
        }
    }

    // 返回基地函数
    void ReturnHome()
    {
        // 停止移动
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; // 设置为运动学状态
        rb.detectCollisions = false;

        // 设置为基地的子对象
        if (transform.parent != home)
        {
            transform.parent = home;
        }

        // 重置位置和旋转
        transform.localPosition = homeOffset;
        transform.localRotation = Quaternion.identity;
    }

    void LeaveHome()
    {
        // 离开基地
        transform.parent = null;
        rb.isKinematic = false; // 取消运动学状态
        rb.detectCollisions = true;
    }

    void FindDamagedBlock()
    {
        if (PlayManager.instance.blocksParent == null) return;

        // 找到所有带有Durability组件的方块
        Durability[] allBlocks = PlayManager.instance.blocksParent.GetComponentsInChildren<Durability>();
        List<Durability> damagedBlocks = new List<Durability>();

        // 筛选出损坏的方块
        foreach (Durability block in allBlocks)
        {
            if (block.currentDurability < block.maxDurability)
            {
                damagedBlocks.Add(block);
            }
        }

        // 如果没有损坏方块，返回
        if (damagedBlocks.Count == 0)
            return;

        // 找到最近的损坏方块
        float closestDistance = Mathf.Infinity;
        Durability closestBlock = null;

        foreach (Durability block in damagedBlocks)
        {
            float distance = Vector3.Distance(transform.position, block.transform.position);
            if (distance < closestDistance && distance <= detectionRange)
            {
                closestDistance = distance;
                closestBlock = block;
            }
        }

        currentTarget = closestBlock;
    }

    void NavigateToTarget(Transform target)
    {
        if (transform.parent == home)
        {
            LeaveHome();
        }

        Vector3 direction = (target.position - transform.position).normalized;

        if (target != home)
        {
            // 使用避障算法计算避障方向
            avoidanceDirection = CalculateAvoidanceDirection();
        }

        // 结合目标方向和避障方向
        if (avoidanceDirection != Vector3.zero)
        {
            direction = (direction + avoidanceDirection * avoidanceForce).normalized;
        }

        // 计算目标旋转
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        // 使用刚体施加力来移动
        rb.AddForce(transform.forward * movementSpeed, ForceMode.Acceleration);
    }

    Vector3 CalculateAvoidanceDirection()
    {
        Vector3 avoidanceDir = Vector3.zero;
        int hitCount = 0;

        // 使用OverlapSphere检测范围内的障碍物
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, avoidanceRange, obstacleMask);

        foreach (var hitCollider in hitColliders)
        {
            Vector3 dirToObstacle = transform.position - hitCollider.transform.position;
            float distance = dirToObstacle.magnitude;

            // 距离越近，避障力越大
            float force = (avoidanceRange - distance) / avoidanceRange;
            avoidanceDir += dirToObstacle.normalized * force;
            hitCount++;
        }

        // 平均避障方向
        if (hitCount > 0)
        {
            avoidanceDir /= hitCount;
        }

        return avoidanceDir;
    }

    void CheckAndRepair()
    {
        if (currentTarget == null)
        {
            UpdateRepairBeam(false);
            return;
        }

        // 检查是否接近目标
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance < 10f) // 修复距离
        {
            // 停止移动，开始修复
            isRepairing = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 更新修复光束
            UpdateRepairBeam(true);

            // 检查冷却时间
            if (Time.time - lastRepairTime >= repairCooldown)
            {
                // 修复目标
                currentTarget.TakeDamage(-repairAmount); // 负值表示修复
                lastRepairTime = Time.time;

                // 检查是否完全修复
                if (currentTarget.currentDurability >= currentTarget.maxDurability)
                {
                    currentTarget = null;
                    isRepairing = false;
                    UpdateRepairBeam(false);
                }
            }
        }
        else
        {
            isRepairing = false;
            UpdateRepairBeam(false);
        }
    }

    void UpdateRepairBeam(bool active)
    {
        if (repairBeam == null) return;

        repairBeam.enabled = active;

        if (active && currentTarget != null)
        {
            // 设置光束位置
            repairBeam.SetPosition(0, transform.position);
            repairBeam.SetPosition(1, currentTarget.transform.position);

            // 根据耐久度比例评估渐变颜色
            float durabilityRatio = currentTarget.currentDurability / currentTarget.maxDurability;
            Color beamColor = repairBeamGradient.Evaluate(durabilityRatio);

            // 创建一个新的渐变，基于当前耐久度
            Gradient newGradient = new Gradient();
            newGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(beamColor, 0f),
                    new GradientColorKey(beamColor, 1f)
                },
                repairBeamGradient.alphaKeys
            );

            repairBeam.colorGradient = newGradient;
        }
    }

    public void ClearTarget()
    {
        currentTarget = null;
        isRepairing = false;
        UpdateRepairBeam(false);
    }

    // 绘制检测范围和避障范围
    void OnDrawGizmos()
    {
        // 检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 避障范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, avoidanceRange);

        if (currentTarget != null)
        {
            Gizmos.color = isRepairing ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // 绘制避障方向
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, avoidanceDirection * 3f);
    }
}