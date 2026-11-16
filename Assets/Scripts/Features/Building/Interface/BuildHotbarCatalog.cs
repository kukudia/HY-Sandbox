// 职责：保存快捷栏可选部件、稳定Resources路径、图标与默认槽位；个人配置独立保存。
// 入口：Find() / Entries / Defaults；不代表有限库存或货仓物品。
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/Build Hotbar Catalog")]
public sealed class BuildHotbarCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string resourcePath;
        public Sprite icon;
    }

    [Serializable]
    public sealed class Slot
    {
        public string resourcePath;
        public string materialId;
    }

    [SerializeField]
    private Entry[] entries = Array.Empty<Entry>();
    [SerializeField]
    private Slot[] defaults = Array.Empty<Slot>();
    public Entry[] Entries => entries;
    public Slot[] Defaults => defaults;

    public Entry Find(string path)
    {
        return Array.Find(entries, item => item != null && item.resourcePath == path);
    }
}
