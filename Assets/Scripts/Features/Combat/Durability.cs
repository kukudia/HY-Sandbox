// 职责：管理当前/最大耐久、损伤反馈及销毁通知。
// 入口：ApplyDamage() / ApplyCollisionEnergy() / ProtectFromExplosionCollisions()。
using UnityEngine;
using System.Collections.Generic;

public class Durability : MonoBehaviour
{
    private static readonly HashSet<Durability> active = new HashSet<Durability>();
    internal static IEnumerable<Durability> Active => active;

    [Header("耐久值设置")]
    public float maxDurability = 100f;
    [SerializeField, Min(0f)]
    private float collisionSpeedThreshold = 2f;
    [SerializeField, Min(0f), Tooltip("Durability lost per joule of impact energy.")]
    private float damageMultiplier = 0.1f;
    [SerializeField, Range(0f, 1f)]
    private float _collisionDamageScale = 1f;
    [SerializeField, Range(0f, 1f)]
    private float _maximumCollisionHealthFraction = 1f;
    [SerializeField, Min(0f)]
    private float _collisionDamageCooldown = 0f;
    private float _nextCollisionDamage;
    private float _explosionProtectionUntil;
    public bool DestroyedByCollision
    {
        get; private set;
    }

    public bool debugLog = true;
    public float currentDurability;
    public bool needToRepair => currentDurability < maxDurability;
    public ControlUnit LastAttacker
    {
        get; private set;
    }

    // 缓存组件引用，避免重复查找
    private Renderer objectRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private StatusIcon _icon;
    private static readonly int HealthColorId = Shader.PropertyToID("_HealthColor");
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
        active.Add(this);
        LastAttacker = null;
        DestroyedByCollision = false;
        _nextCollisionDamage = _explosionProtectionUntil = 0f;
        currentDurability = maxDurability;
        UpdateDurablility(0);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    public void ApplyDamage(float amount, ControlUnit attacker)
    {
        if (amount <= 0f || !enabled)
        {
            return;
        }

        LastAttacker = attacker;
        ChangeDurability(-amount);
    }

    public void ApplyCollisionEnergy(float reducedMass, float normalSpeed, float share)
    {
        if (!enabled || currentDurability <= 0f || Time.time < _nextCollisionDamage || Time.time < _explosionProtectionUntil)
        {
            return;
        }

        float energy = ImpactPhysics.DamageEnergy(reducedMass, normalSpeed, collisionSpeedThreshold);
        if (energy <= 0f)
        {
            return;
        }

        float damage = Mathf.Min(
            energy * Mathf.Clamp01(share) * damageMultiplier * _collisionDamageScale,
            maxDurability * _maximumCollisionHealthFraction);
        if (damage <= 0f)
        {
            return;
        }

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
        if (value < 0f)
        {
            LastAttacker = null;
        }

        ChangeDurability(value);
    }

    private void ChangeDurability(float value)
    {
        if (!enabled)
        {
            return;
        }

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
            BuildingRuntime building = GetComponentInParent<BuildingRuntime>();
            if (building != null)
            {
                building.DestroyBlock(GetComponent<Block>());
            }
            else if (DestroyManager.instance != null)
            {
                DestroyManager.instance.DestroyGameObject(gameObject);
            }
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
}
