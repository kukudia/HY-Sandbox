using UnityEngine;

public class Durability : MonoBehaviour
{
    [Header("耐久值设置")]
    public float maxDurability = 100f;
    private float collisionSpeedThreshold = 5f;
    private float damageMultiplier = 0.5f;
    public bool debugLog = true;
    
    public float currentDurability;
    public bool needToRepair => currentDurability < maxDurability;
    
    // 缓存组件引用，避免重复查找
    private Renderer objectRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private static readonly int HealthColorId = Shader.PropertyToID("_HealthColor");
    
    // GUI 相关缓存
    private GUIStyle labelStyle;
    private Vector3 lastScreenPos;
    private string durabilityText;
    private bool isVisible;
    
    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }
    }

    private void OnEnable()
    {
        currentDurability = maxDurability;
        UpdateDurablility(0);
    }

    public void CollisionEnter(Collision collision)
    {
        float collisionSpeed = collision.relativeVelocity.magnitude;

        if (collisionSpeed > collisionSpeedThreshold)
        {
            float damage = Mathf.Min(40, (collisionSpeed - collisionSpeedThreshold) * damageMultiplier);
            UpdateDurablility(-damage);

            if (debugLog)
            {
                // Debug.Log($"{name} 碰撞速度：{collisionSpeed:F1}, 碰撞源：{collision.transform.name}, 伤害：{damage:F1}, 剩余耐久：{currentDurability:F1}");
            }
        }
    }

    //public void Repair(float amount)
    //{
    //    currentDurability = Mathf.Min(maxDurability, currentDurability + amount);

    //    if (debugLog)
    //    {
    //        Debug.Log($"{name} 被修复：+{amount:F1}, 当前耐久度：{currentDurability:F1}/{maxDurability}");
    //    }
    //}

    public void UpdateDurablility(float value)
    {
        currentDurability += value;

        if (currentDurability > maxDurability)
        {
            currentDurability = maxDurability;
            MainUIPanels.instance.UpdateHealthBar(gameObject, currentDurability, maxDurability);
        }
        else if (currentDurability <= 0)
        {
            currentDurability = 0;
            MainUIPanels.instance.UpdateHealthBar(gameObject, currentDurability, maxDurability);
            DestroyManager.Instance.DestroyGameObject(gameObject);
        }
        else
        {
            MainUIPanels.instance.UpdateHealthBar(gameObject, currentDurability, maxDurability);
        }
    }

    private void LateUpdate()
    {
        if (!ShouldShowDebugLabel() || currentDurability >= maxDurability)
        {
            isVisible = false;
            return;
        }
        
        Camera camera = PlayManager.instance != null && PlayManager.instance.mainCamera != null
            ? PlayManager.instance.mainCamera
            : Camera.main;
        if (camera == null) return;
        
        lastScreenPos = camera.WorldToScreenPoint(transform.position);
        isVisible = lastScreenPos.z > 0;
        durabilityText = $"{currentDurability:F1}/{maxDurability}";
    }

    private void OnGUI()
    {
        if (!ShouldShowDebugLabel() || !isVisible || currentDurability >= maxDurability) return;
        
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
        }
        
        float healthRatio = currentDurability / maxDurability;
        labelStyle.normal.textColor = Color.Lerp(Color.red, Color.green, healthRatio);
        
        Rect labelRect = new Rect(lastScreenPos.x - 50, Screen.height - lastScreenPos.y - 10, 100, 20);
        GUI.Label(labelRect, durabilityText, labelStyle);
    }

    private bool ShouldShowDebugLabel()
    {
        return debugLog
            && PlayManager.instance != null
            && PlayManager.instance.showLabel;
    }
}
