using Unity.VisualScripting;
using UnityEngine;

public class Durability : MonoBehaviour
{
    [Header("耐久值设置")]
    public float maxDurability = 100f;
    public float collisionSpeedThreshold = 20f;
    public float damageMultiplier = 0.5f;
    public bool debugLog = true;

    public float currentDurability;
    private Material originalMaterial;
    private Renderer objectRenderer;

    void Start()
    {
        // 获取渲染器和原始材质
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
    }

    public void CollisionEnter(Collision collision)
    {
        // 获取碰撞相对速度
        float collisionSpeed = collision.relativeVelocity.magnitude;

        if (collisionSpeed > collisionSpeedThreshold)
        {
            // 计算伤害
            float damage = Mathf.Max(50, (collisionSpeed - collisionSpeedThreshold) * damageMultiplier);
            TakeDamage(damage);

            if (debugLog)
            {
                Debug.Log($"{name} 碰撞速度: {collisionSpeed:F1}, 伤害: {damage:F1}, 剩余耐久: {currentDurability:F1}");
            }
        }
    }

    // 在Durability类中添加以下方法
    public void Repair(float amount)
    {
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);

        if (debugLog)
        {
            Debug.Log($"{name} 被修复: +{amount:F1}, 当前耐久度: {currentDurability:F1}/{maxDurability}");
        }
    }

    // 修改TakeDamage方法以支持修复（负伤害）
    public void TakeDamage(float damage)
    {
        currentDurability -= damage;

        // 检查是否被破坏
        if (currentDurability <= 0)
        {
            DestroyManager.Instance.DestroyGameObject(gameObject);
        }
    }

    // 在编辑器中显示当前耐久值（调试用）
    void OnGUI()
    {
        if (debugLog)
        {
            if (currentDurability == maxDurability) return;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            GUIStyle style = GUI.skin.label;
            style.normal.textColor = Color.Lerp(Color.red, Color.green, currentDurability / maxDurability);
            GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 200, 20), $"{currentDurability:F1}/{maxDurability}", style);
        }
    }
}