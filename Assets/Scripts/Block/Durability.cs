using UnityEngine;

public class Durability : MonoBehaviour
{
    [Header("耐久值设置")]
    public float maxDurability = 100f;
    [SerializeField, Min(0f)] private float collisionSpeedThreshold = 5f;
    [SerializeField, Min(0f), Tooltip("Durability lost per joule of impact energy.")] private float damageMultiplier = 0.001f;
    [SerializeField, Range(0f, 1f)] private float _collisionDamageScale = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _maximumCollisionHealthFraction = 0.08f;
    [SerializeField, Min(0f)] private float _collisionDamageCooldown = 0.35f;
    private float _nextCollisionDamage;
    private float _explosionProtectionUntil;
    public bool DestroyedByCollision { get; private set; }
    public bool debugLog = true;
    
    public float currentDurability;
    public bool needToRepair => currentDurability < maxDurability;
    public ControlUnit LastAttacker { get; private set; }
    
    // 缓存组件引用，避免重复查找
    private Renderer objectRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private StatusIcon _icon;
    private static readonly int HealthColorId = Shader.PropertyToID("_HealthColor");
    
    // GUI 相关缓存
    private GUIStyle labelStyle;
    private Vector3 lastScreenPos;
    private string durabilityText;
    private bool isVisible;
    
    private void Awake()
    {
        _icon = GetComponent<StatusIcon>();
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }
    }

    private void OnEnable()
    {
        LastAttacker = null;
        DestroyedByCollision = false;
        _nextCollisionDamage = _explosionProtectionUntil = 0f;
        currentDurability = maxDurability;
        UpdateDurablility(0);
    }

    public void ApplyDamage(float amount, ControlUnit attacker)
    {
        if (amount <= 0f || !enabled) return;
        LastAttacker = attacker;
        ChangeDurability(-amount);
    }

    public void ApplyCollisionEnergy(float reducedMass, float normalSpeed, float share)
    {
        if (!enabled || currentDurability <= 0f || Time.time < _nextCollisionDamage || Time.time < _explosionProtectionUntil) return;
        float energy = ImpactPhysics.DamageEnergy(reducedMass, normalSpeed, collisionSpeedThreshold);
        if (energy <= 0f) return;
        float damage = Mathf.Min(energy * Mathf.Clamp01(share) * damageMultiplier * _collisionDamageScale,
            maxDurability * _maximumCollisionHealthFraction);
        if (damage <= 0f) return;
        _nextCollisionDamage = Time.time + _collisionDamageCooldown;
        DestroyedByCollision = damage >= currentDurability;
        UpdateDurablility(-damage);
    }

    public void ProtectFromExplosionCollisions(float duration)
    {
        _explosionProtectionUntil = Mathf.Max(_explosionProtectionUntil, Time.time + Mathf.Max(0f, duration));
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
        if (value < 0f) LastAttacker = null;
        ChangeDurability(value);
    }

    private void ChangeDurability(float value)
    {
        if (!enabled) return;
        currentDurability += value;

        if (currentDurability > maxDurability)
        {
            currentDurability = maxDurability;
            MainUIPanels.instance?.UpdateHealthBar(gameObject, currentDurability, maxDurability);
        }
        else if (currentDurability <= 0)
        {
            currentDurability = 0;
            MainUIPanels.instance?.UpdateHealthBar(gameObject, currentDurability, maxDurability);
            DestroyManager.instance.DestroyGameObject(gameObject);
        }
        else
        {
            MainUIPanels.instance?.UpdateHealthBar(gameObject, currentDurability, maxDurability);
        }

        if (_icon != null)
        {
            _icon.CheckDurabilityStatus();
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
