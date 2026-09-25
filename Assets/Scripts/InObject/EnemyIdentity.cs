using UnityEngine;

public sealed class EnemyIdentity : MonoBehaviour
{
    [SerializeField] private string _displayName;
    private float _spawnMaxHealth;

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? "Enemy Unit" : _displayName;
    public float SpawnMaxHealth => _spawnMaxHealth;

    public void SetDisplayName(string displayName)
    {
        _displayName = string.IsNullOrWhiteSpace(displayName) ? "Enemy Unit" : displayName;
    }

    public void SetSpawnMaxHealth(float maximum)
    {
        if (_spawnMaxHealth > 0f) return;
        _spawnMaxHealth = Mathf.Max(0f, maximum);
    }
}
