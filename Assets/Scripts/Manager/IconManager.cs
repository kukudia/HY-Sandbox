using System;
using System.Collections.Generic;
using UnityEngine;

public class IconManager : MonoBehaviour
{
    public static IconManager instance;
    public List<Status> statuses = new List<Status>();
    [Min(0f)] public float pulseSpeed = 5f;
    [Range(0f, 1f)] public float minimumAlpha = 0.35f;
    private readonly List<CanvasGroup> activeIcons = new List<CanvasGroup>();
    public float PulseAlpha => minimumAlpha + (1f - minimumAlpha) *
        (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed));

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        float pulse = PulseAlpha;
        for (int i = activeIcons.Count - 1; i >= 0; i--)
        {
            CanvasGroup icon = activeIcons[i];
            if (icon == null)
            {
                activeIcons.RemoveAt(i);
                continue;
            }

            icon.alpha = pulse;
        }
    }

    public void Register(CanvasGroup icon)
    {
        if (icon != null && !activeIcons.Contains(icon))
        {
            activeIcons.Add(icon);
        }
    }

    public void Unregister(CanvasGroup icon)
    {
        activeIcons.Remove(icon);
    }
}

[Serializable]
public class Status
{
    public string name;
    public Sprite icon;
}
