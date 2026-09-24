using UnityEngine;

public sealed class EnemyIdentity : MonoBehaviour
{
    [SerializeField] private string _displayName;

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? "Enemy Unit" : _displayName;

    public void SetDisplayName(string displayName)
    {
        _displayName = string.IsNullOrWhiteSpace(displayName) ? "Enemy Unit" : displayName;
    }
}
