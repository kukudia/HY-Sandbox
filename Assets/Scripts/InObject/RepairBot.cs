using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RepairBot : MonoBehaviour
{
    public Transform home;
    public Transform outside;
    public Transform navigateTarget;
    public Vector3 homeOffset = Vector3.zero;

    [Header("修复设置")]
    public float repairAmount = 10f;
    public float repairCooldown = 1f;
    public float findTargetInterval = 1f;
    public float detectionRange = 50f;
    public float movementSpeed = 5f;
    public float rotationSpeed = 2f;

    [Header("避障设置")]
    public LayerMask obstacleMask;
    public float avoidanceRange = 8f;
    public float avoidanceForce = 5f;
    public float basicDirectionOffset = 2f;
    public int avoidanceRays = 8; // 用于可视化射线数量

    [Header("可视化设置")]
    [Tooltip("是否显示移动轨迹")]
    public bool showTrail = true;
    [Tooltip("轨迹保留时间(秒)")]
    public float trailDuration = 5f;
    [Tooltip("是否显示避障射线")]
    public bool showAvoidanceRays = true;
    [Tooltip("是否显示方向向量")]
    public bool showDirectionVectors = true;

    [Header("修复效果")]
    public LineRenderer repairBeam;
    public Gradient repairBeamGradient;
    public float beamWidth = 0.2f;

    public Durability currentTarget;
    public float lastRepairTime;
    public float lastFindTime;
    public bool isRepairing;
    private Vector3 avoidanceDirection;
    private Rigidbody rb;

    // ===== 可视化增强新增字段 =====
    private TrailRenderer trailRenderer;
    private List<RaycastHit> debugRaycastHits = new List<RaycastHit>(); // 存储避障射线命中结果用于绘制

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.linearDamping = 2f;
        rb.angularDamping = 3f;

        // 初始化修复光束
        if (repairBeam == null)
        {
            repairBeam = gameObject.AddComponent<LineRenderer>();
            repairBeam.material = new Material(Shader.Find("Sprites/Default"));
            repairBeam.colorGradient = repairBeamGradient;
            repairBeam.startWidth = beamWidth;
            repairBeam.endWidth = beamWidth * 0.5f;
            repairBeam.enabled = false;
        }

        // 初始化轨迹渲染器
        if (showTrail)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
                trailRenderer.startColor = new Color(0.2f, 0.8f, 1f, 0.8f);
                trailRenderer.endColor = new Color(0.2f, 0.8f, 1f, 0.1f);
                trailRenderer.startWidth = 0.15f;
                trailRenderer.endWidth = 0.05f;
                trailRenderer.time = trailDuration;
                trailRenderer.enabled = true;
            }
        }

        home = transform.parent;
        homeOffset = transform.localPosition;
    }

    void FixedUpdate()
    {
        // 更新避障射线用于可视化（即使不避障也更新用于绘制）
        if (showAvoidanceRays)
        {
            UpdateAvoidanceRaycasts();
        }

        if (currentTarget == null)
        {
            FindDamagedBlock();
            UpdateRepairBeam(false);

            if (currentTarget == null && home != null)
            {
                NavigateToTarget(home);

                float distanceToHome = Vector3.Distance(transform.position, home.position);
                if (distanceToHome < 5f)
                {
                    ReturnHome();
                }
            }
        }
        else
        {
            NavigateToTarget(currentTarget.transform);
            CheckAndRepair();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.GetMask("Default"))
        {
            // 简单的碰撞反弹效果
            Vector3 reflectDir = (rb.linearVelocity - collision.contacts[0].point).normalized;
            rb.AddForce(reflectDir * movementSpeed * 10, ForceMode.VelocityChange);
        }
    }

    // ===== 避障射线可视化专用方法 =====
    void UpdateAvoidanceRaycasts()
    {
        debugRaycastHits.Clear();
        float angleStep = 360f / avoidanceRays;

        for (int i = 0; i < avoidanceRays; i++)
        {
            Quaternion rotation = Quaternion.AngleAxis(angleStep * i, Vector3.up);
            Vector3 direction = rotation * Vector3.forward;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f; // 抬高避免地面干扰

            if (Physics.Raycast(rayStart, direction, out RaycastHit hit, avoidanceRange, obstacleMask))
            {
                debugRaycastHits.Add(hit);
            }
        }
    }

    void ReturnHome()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.detectCollisions = false;

        if (transform.parent != home)
        {
            transform.parent = home;
        }

        transform.localPosition = homeOffset;
        transform.localRotation = Quaternion.identity;

        // 返回基地时清除轨迹
        if (trailRenderer != null) trailRenderer.Clear();
    }

    void LeaveHome()
    {
        transform.parent = outside;
        rb.isKinematic = false;
        rb.detectCollisions = true;
    }

    void FindDamagedBlock()
    {
        if (Time.time - lastFindTime < findTargetInterval) return;
        if (PlayManager.instance?.blocksParent == null) return;

        Durability[] allBlocks = PlayManager.instance.blocksParent.GetComponentsInChildren<Durability>();
        List<Durability> damagedBlocks = new List<Durability>();

        foreach (Durability block in allBlocks)
        {
            if (block.needToRepair)
            {
                damagedBlocks.Add(block);
            }
        }

        if (damagedBlocks.Count == 0)
            return;

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
        lastFindTime = Time.time; // 更新查找时间
    }

    void NavigateToTarget(Transform target)
    {
        navigateTarget = target;

        if (transform.parent == home)
        {
            LeaveHome();
        }

        Vector3 direction = (target.position - transform.position).normalized;

        if (target != home)
        {
            avoidanceDirection = CalculateAvoidanceDirection();
        }

        if (avoidanceDirection != Vector3.zero)
        {
            direction = (direction * basicDirectionOffset + avoidanceDirection * avoidanceForce).normalized;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        rb.AddForce(transform.forward * movementSpeed, ForceMode.Acceleration);
    }

    Vector3 CalculateAvoidanceDirection()
    {
        Vector3 avoidanceDir = Vector3.zero;
        int hitCount = 0;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, avoidanceRange, obstacleMask);

        foreach (var hitCollider in hitColliders)
        {
            // 忽略自身和目标物体
            if (hitCollider.transform == transform ||
                (currentTarget != null && hitCollider.transform == currentTarget.transform))
                continue;

            Vector3 dirToObstacle = transform.position - hitCollider.transform.position;
            float distance = dirToObstacle.magnitude;

            if (distance > 0.1f) // 避免除零
            {
                float force = (avoidanceRange - distance) / avoidanceRange;
                avoidanceDir += dirToObstacle.normalized * force;
                hitCount++;
            }
        }

        return hitCount > 0 ? avoidanceDir / hitCount : Vector3.zero;
    }

    void CheckAndRepair()
    {
        if (currentTarget == null)
        {
            UpdateRepairBeam(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance < 10f)
        {
            isRepairing = true;
            //rb.linearVelocity = Vector3.zero;
            rb.linearVelocity *= 0.8f;
            rb.angularVelocity = Vector3.zero;
            UpdateRepairBeam(true);

            if (Time.time - lastRepairTime >= repairCooldown)
            {
                currentTarget.UpdateDurablility(repairAmount);
                lastRepairTime = Time.time;

                if (currentTarget.currentDurability >= currentTarget.maxDurability)
                {
                    currentTarget = null;
                    isRepairing = false;
                    UpdateRepairBeam(false);

                    // 完成修复后清除轨迹起点
                    if (trailRenderer != null) trailRenderer.Clear();
                }
            }
        }
        else if (distance >= 15f)
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
            repairBeam.SetPosition(0, transform.position + Vector3.up * 0.5f); // 抬高光束起点
            repairBeam.SetPosition(1, currentTarget.transform.position + Vector3.up * 0.5f);

            float durabilityRatio = Mathf.Clamp01(currentTarget.currentDurability / currentTarget.maxDurability);
            Color beamColor = repairBeamGradient.Evaluate(durabilityRatio);

            Gradient newGradient = new Gradient();
            newGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(beamColor, 0f),
                    new GradientColorKey(Color.white, 1f) // 尖端白色增强视觉效果
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.5f, 1f)
                }
            );

            repairBeam.colorGradient = newGradient;
        }
    }

    public void ClearTarget()
    {
        currentTarget = null;
        isRepairing = false;
        UpdateRepairBeam(false);
        if (trailRenderer != null) trailRenderer.Clear();
    }

    // ===== 增强版可视化绘制 =====
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return; // 仅在运行时绘制动态数据

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // 1. 检测范围（黄色）
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(Vector3.zero, detectionRange);

        // 2. 避障范围（蓝色半透明）
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
        Gizmos.DrawSphere(Vector3.zero, avoidanceRange);

        // 3. 避障射线可视化
        if (showAvoidanceRays && debugRaycastHits.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (var hit in debugRaycastHits)
            {
                Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
                Gizmos.DrawLine(Vector3.up * 0.5f, localHitPoint); // 从抬高的起点绘制

                // 绘制命中点小球
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(localHitPoint, 0.3f);
            }

            // 绘制未命中的射线（绿色）
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            float angleStep = 360f / avoidanceRays;
            for (int i = 0; i < avoidanceRays; i++)
            {
                bool hitExists = false;
                foreach (var hit in debugRaycastHits)
                {
                    Vector3 dir = (hit.point - (transform.position + Vector3.up * 0.5f)).normalized;
                    Quaternion rotation = Quaternion.AngleAxis(angleStep * i, Vector3.up);
                    if (Vector3.Angle(dir, rotation * Vector3.forward) < angleStep / 2)
                    {
                        hitExists = true;
                        break;
                    }
                }

                if (!hitExists)
                {
                    Quaternion rotation = Quaternion.AngleAxis(angleStep * i, Vector3.up);
                    Gizmos.DrawRay(Vector3.up * 0.5f, rotation * Vector3.forward * avoidanceRange);
                }
            }
        }

        // 4. 方向向量可视化
        if (showDirectionVectors && navigateTarget != null)
        {
            Vector3 targetDir = (navigateTarget.position - transform.position).normalized;
            Vector3 localTargetDir = transform.InverseTransformDirection(targetDir);

            // 目标方向（青色）
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(Vector3.zero, localTargetDir * 8f);

            // 避障方向（品红）
            if (avoidanceDirection != Vector3.zero)
            {
                Vector3 localAvoidanceDir = transform.InverseTransformDirection(avoidanceDirection.normalized);
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(Vector3.zero, localAvoidanceDir * 5f);
            }

            // 实际移动方向（白色）
            Gizmos.color = Color.white;
            Gizmos.DrawRay(Vector3.zero, Vector3.forward * 6f);

            // 速度方向（黄色）
            if (rb != null)
            {
                Vector3 velocityDir = rb.linearVelocity.normalized;
                if (velocityDir.magnitude > 0.1f)
                {
                    Vector3 localVelocityDir = transform.InverseTransformDirection(velocityDir);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(Vector3.zero, localVelocityDir * Mathf.Min(rb.linearVelocity.magnitude * 0.5f, 10f));
                }
            }
        }

        // 5. 重置矩阵
        Gizmos.matrix = Matrix4x4.identity;

        // 6. 目标连线
        if (currentTarget != null)
        {
            Gizmos.color = isRepairing ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);

            // 绘制目标点标记
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentTarget.transform.position, 0.5f);
        }
        else if (home != null && navigateTarget == home)
        {
            // 返回基地时的连线
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.7f);
            Gizmos.DrawLine(transform.position, home.position);
        }

        // 7. 基地位置标记
        if (home != null)
        {
            Gizmos.color = new Color(0.3f, 0.3f, 1f, 0.5f);
            Gizmos.DrawSphere(home.position, 1.5f);
        }
    }

    // 编辑器下静态可视化（非运行时）
    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        // 始终显示检测/避障范围
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(0f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, avoidanceRange);

        // 显示设置的目标点
        if (navigateTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, navigateTarget.position);
            Gizmos.DrawWireSphere(navigateTarget.position, 1f);
        }
    }
}