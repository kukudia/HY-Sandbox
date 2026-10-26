// 职责：在HUD边缘标出不在视野内的来袭敌舰，给出方向与距离，夜色场景里仍能读到威胁。
// 入口：Start()建立有界标记池，Update()按间隔刷新；只读ControlUnit公开状态，不参与规则或物理。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeThreatMarkers : MonoBehaviour
{
    [SerializeField]
    private SiegeSession session;
    [SerializeField, Tooltip("用于投影的相机；留空时使用Camera.main。")]
    private Camera worldCamera;
    [SerializeField, Min(1), Tooltip("同时显示的最大标记数；应与单波同时在场上限一致。")]
    private int poolSize = 6;
    [SerializeField, Min(0f), Tooltip("标记离屏幕边缘的像素距离。")]
    private float edgeMargin = 96f;
    [SerializeField, Min(0.02f)]
    private float updateInterval = 0.1f;
    [SerializeField, Min(0f), Tooltip("超过该距离的敌舰即使入镜也标记，避免夜色里丢失目标。")]
    private float alwaysMarkBeyond = 90f;
    [SerializeField]
    private Color markerColor = new Color(1f, 0.46f, 0.22f, 0.92f);
    [SerializeField]
    private Color labelColor = new Color(1f, 0.72f, 0.55f, 0.92f);
    [SerializeField]
    private Vector2 markerSize = new Vector2(18f, 18f);
    [SerializeField, Min(8)]
    private int labelSize = 16;
    private readonly List<RectTransform> markers = new List<RectTransform>();
    private readonly List<Text> labels = new List<Text>();
    private readonly List<int> shownDistances = new List<int>();
    private RectTransform canvasRect;
    private float nextRefresh;
    private void Awake()
    {
        canvasRect = transform as RectTransform;
        Build();
    }

    private void Start()
    {
        if (session == null)
        {
            session = Object.FindFirstObjectByType<SiegeSession>();
        }
    }

    /// <summary>建立固定数量的标记；运行时只改位置、旋转、文字与可见性。</summary>
    private void Build()
    {
        if (markers.Count > 0 || canvasRect == null)
        {
            return;
        }

        Font font = null;
        foreach (Text existing in GetComponentsInChildren<Text>(true))
        {
            font = existing.font;
            if (font != null)
            {
                break;
            }
        }

        for (int index = 0; index < poolSize; index++)
        {
            var root = new GameObject("Threat Marker " + (index + 1), typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)root.transform;
            rect.SetParent(canvasRect, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = markerSize;
            Image diamond = root.GetComponent<Image>();
            diamond.color = markerColor;
            diamond.raycastTarget = false;
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            root.SetActive(false);
            var labelObject = new GameObject("Distance", typeof(RectTransform), typeof(Text));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -6f);
            labelRect.sizeDelta = new Vector2(90f, 22f);
            Text label = labelObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = labelSize;
            label.alignment = TextAnchor.UpperCenter;
            label.color = labelColor;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            markers.Add(rect);
            labels.Add(label);
            shownDistances.Add(-1);
        }
    }

    private void Update()
    {
        if (session == null || session.Match == null || markers.Count == 0 || Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + Mathf.Max(0.02f, updateInterval);
        Camera camera = worldCamera != null ? worldCamera : Camera.main;
        if (camera == null)
        {
            HideAll();
            return;
        }

        int used = 0;
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (used >= markers.Count)
            {
                break;
            }

            if (unit == null || unit.faction != UnitFaction.Enemy || unit.cockpit == null || !unit.cockpit.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 world = unit.cockpit.transform.position;
            Vector3 viewport = camera.WorldToViewportPoint(world);
            float distance = Vector3.Distance(camera.transform.position, world);
            bool onScreen = viewport.z > 0f && viewport.x > 0.06f && viewport.x < 0.94f && viewport.y > 0.06f && viewport.y < 0.94f && distance <= alwaysMarkBeyond;
            if (onScreen)
            {
                continue;
            }

            PlaceMarker(used, camera, world, viewport, distance);
            used++;
        }

        for (int index = used; index < markers.Count; index++)
        {
            if (markers[index].gameObject.activeSelf)
            {
                markers[index].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>把世界坐标压到屏幕边缘：视野内但过远时按真实方向，视野外时按画布方向夹取。</summary>
    private void PlaceMarker(int index, Camera camera, Vector3 world, Vector3 viewport, float distance)
    {
    RectTransform marker = markers[index];
        if (!marker.gameObject.activeSelf)
        {
            marker.gameObject.SetActive(true);
        }

        Vector2 screenPoint = camera.WorldToScreenPoint(world);
        // 覆盖层画布与屏幕分辨率不同（CanvasScaler），必须先把屏幕点换算到画布本地坐标再夹取。
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local))
        {
            marker.gameObject.SetActive(false);
            return;
        }

        if (viewport.z <= 0f)
        {
            // 目标在相机后方时方向翻转，否则标记会挤在错误的一侧。
            local = -local;
        }

        if (local.sqrMagnitude < 0.0001f)
        {
            local = Vector2.up;
        }

        Vector2 half = canvasRect.rect.size * 0.5f - new Vector2(edgeMargin, edgeMargin);
        float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(local.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(local.y)));
        scale = Mathf.Min(1f, scale);
        marker.anchoredPosition = local * scale;
        int rounded = Mathf.Max(1, Mathf.RoundToInt(distance));
        if (shownDistances[index] != rounded)
        {
            shownDistances[index] = rounded;
            labels[index].text = rounded + " m";
        }
    }

    private void HideAll()
    {
        foreach (RectTransform marker in markers)
        {
            if (marker.gameObject.activeSelf)
            {
                marker.gameObject.SetActive(false);
            }
        }
    }
}
