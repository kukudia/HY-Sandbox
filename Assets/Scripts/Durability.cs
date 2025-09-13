using UnityEngine;

public class Durability : MonoBehaviour
{
    [Header("耐久值设置")]
    public float maxDurability = 100f;
    public float collisionSpeedThreshold = 5f;
    public float damageMultiplier = 1f;
    public bool debugLog = true;

    private float currentDurability;
    private Material originalMaterial;
    private Renderer objectRenderer;

    void Start()
    {
        currentDurability = maxDurability;

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
            float damage = (collisionSpeed - collisionSpeedThreshold) * damageMultiplier;
            TakeDamage(damage);

            if (debugLog)
            {
                Debug.Log($"{name} 碰撞速度: {collisionSpeed:F1}, 伤害: {damage:F1}, 剩余耐久: {currentDurability:F1}");
            }
        }
    }

    public void TakeDamage(float damage)
    {
        currentDurability -= damage;

        // 检查是否销毁物体
        if (currentDurability <= 0)
        {
            DestroyObject();
        }
    }

    void DestroyObject()
    {
        Debug.Log($"{name} 耐久值为0，物体被销毁");

        // 销毁当前子物体
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (PlayManager.instance.playMode)
        {
            DestroyManager.Instance.NotifyObjectDestroyed();

            if (GetComponent<Cockpit>() != null)
            {
                MainUIPanels.instance.PlayEnd();
            }
        }

    }

    // 在编辑器中显示当前耐久值（调试用）
    void OnGUI()
    {
        if (debugLog)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 200, 20),
                     $"{name}: {currentDurability:F1}/{maxDurability}");
        }
    }
}